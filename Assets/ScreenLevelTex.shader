//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/ScreenLevelTex" //Unlit Transparent Vertex Colored Additive 
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend Off
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

		BindChannels 
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord 
			Bind "Color", color 
		}

		SubShader   
		{	
		
			Pass 
			{
				
				
CGPROGRAM
#pragma target 4.5
#pragma require randomwrite
#pragma vertex vert
#pragma fragment frag
//#pragma debug

#pragma multi_compile _ THELAZYCOWBOY1_TERRAIN

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
//#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

RWTexture2D<float4> _MyUAV : register(u1);

sampler2D _LevelTex;
sampler2D _PreLevelColorGrab;
sampler2D _SlopedTerrainMask;

uniform float4 _spriteRect;
uniform float2 _screenSize;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    //float2 scrPos : TEXCOORD1;
    //float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    //o.scrPos = ComputeScreenPos(o.pos);
    //o.clr = v.color;
    return o;
}

inline uint min(uint a, uint b) {
	return (a < b) ? a : b;
}
inline uint depthOfPixel(half r) {
	return (r < 0.997) ? ((uint)round(r * 255) - 1) % 30 : 30;
}
inline half terrainColor(float2 pos) {
	return 2 - 3 * tex2D(_SlopedTerrainMask, pos).r;
}

half4 frag (v2f i) : SV_Target
{
		//map screen pos to level tex coord
	//float2 textCoord = float2(floor(i.uv.x/_MainTex_TexelSize.x)*_MainTex_TexelSize.x, floor(i.uv.y/_MainTex_TexelSize.y)*_MainTex_TexelSize.y);
	//float2 textCoord = float2(floor(i.uv.x*_screenSize.x)/_screenSize.x, floor(i.uv.y*_screenSize.y)/_screenSize.y);
	float2 textCoord = i.uv;
	textCoord.x -= _spriteRect.x;
	textCoord.y -= _spriteRect.y;
	textCoord.x /= _spriteRect.z - _spriteRect.x;
	textCoord.y /= _spriteRect.w - _spriteRect.y;

	half lev = tex2D(_LevelTex, textCoord).r;
#if THELAZYCOWBOY1_TERRAIN
	uint d = min(depthOfPixel(lev), round(terrainColor(i.uv) * 30));
#else
	uint d = depthOfPixel(lev);
#endif

		//check creature mask if applicable
	if (d > 5) {
		half4 c = tex2D(_PreLevelColorGrab, i.uv);
		if (c.r > 1.0f / 255.0f || c.g > 0 || c.b > 0) {
			d = 5;
		}
	}

	half result = d / 30.0f;
	_MyUAV[int2((int)round(i.uv.x * _screenSize.x), (int)round(i.uv.y * _screenSize.y))] = float4(result, 0, 0, 1);
	return half4(result, 0, 0, 0);

}
ENDCG
				
			}
		} 
	}
}
