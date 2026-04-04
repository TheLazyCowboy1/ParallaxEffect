//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/WarpScreen" //Unlit Transparent Vertex Colored Additive 
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
			//GrabPass
			//{
				//"_ScreenTexture"
			//}
		
			Pass 
			{
				
				
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
//#pragma debug

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
//#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

//sampler2D _ScreenTexture;

sampler2D _TheLazyCowboy1_WarpTex;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    //float2 scrPos : TEXCOORD1;
    //float2  uv2 : TEXCOORD1;
    float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    //o.scrPos = ComputeScreenPos(o.pos);
    //o.uv2 = o.uv-_MainTex_TexelSize*.5*_rimFix;
    o.clr = v.color;
    return o;
}

half4 frag (v2f i) : SV_Target
{
	half4 warp = tex2D(_TheLazyCowboy1_WarpTex, i.uv);
	return tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(round(warp.x * 255 - 128), round(warp.y * 255 - 128)));
	//uint zInt = (uint)round(warp.z * 255);
	//return tex2D(_MainTex, i.uv + _MainTex_TexelSize * half2(floor(warp.x * 1023 + (zInt / 16) * 0.25 - 512), floor(warp.y * 1023 + (zInt % 16) * 0.25 - 512)));
}
ENDCG
				
			}
		} 
	}
}
