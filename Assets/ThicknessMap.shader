//r = dist2,
//g = layer1thick,
//b = layer2dep,
//a = dist1, dir

Shader "TheLazyCowboy1/ThicknessMap"
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		Blend Off
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

		BindChannels 
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord
		}

		SubShader   
		{	
		
			Pass 
			{
				
				
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag

			#pragma multi_compile _ THELAZYCOWBOY1_SIMPLERLAYERS

#if THELAZYCOWBOY1_SIMPLERLAYERS
	#define dirCount 4
	#define dirCountm1 3
#else
	#define dirCount 8
	#define dirCountm1 7
#endif
#define testNum 22 //20 = 1 tile wide; + 2 = extra pixels just in case
#define testNump1 23

#include "UnityCG.cginc"

//sampler2D _MainTex;
Texture2D<float4> _MainTex;
uniform float2 _MainTex_TexelSize;

uniform int TheLazyCowboy1_BackgroundTestNum;
uniform float TheLazyCowboy1_ProjectionMod;
uniform float TheLazyCowboy1_MinObjectDepth;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
	float2  texel : TEXCOORD1;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
	o.texel = o.uv / _MainTex_TexelSize;
    return o;
}

inline int depthOfTexel(int2 pos) {
	float r = _MainTex.Load(int3(pos, 0)).r;
	return (r < 0.997f) ? ((uint)(round(r * 255) - 1) % 30) : 30;
}

half4 frag (v2f i) : SV_Target
{
	/* OUTDATED DESCRIPTION (still better than nothing)
		send out 8 "rays" of 11px each
		each ray searches for the first pixel deeper than the current one
		records the estimated "depth" of the current pixel by taking the max of each opposite pair of rays' sum of distance before finding a deeper pixel
	*/
	/*
		left, right
		up, down
		dul, ddr (diagonal-upleft, diagonal-downright)
		ddl, dur (diagonal-downleft, diagonal-upright)
	*/

	int2 startPos = int2(i.texel);//int2(round(i.texel));
	int origDep = depthOfTexel(startPos);

	if (origDep >= 30) {
		//discard;
		return half4(0, 31/255.0f, 0, 0); //fully thick layer1; otherwise irrelevant data so just 0
	}

	int2 dir[dirCount];
#if THELAZYCOWBOY1_SIMPLERLAYERS
	dir[0] = int2(1, 0);
	dir[1] = int2(0, 1);
	dir[2] = int2(1, 1);
	dir[3] = int2(1, -1); //(equivalent to -1,1)
#else
	dir[0] = int2(2, 0);
	dir[1] = int2(0, 2);
	dir[2] = int2(2, 2);
	dir[3] = int2(2, -2); //(equivalent to -1,1)
	dir[4] = int2(2, 1); //1, 0.5
	dir[5] = int2(1, 2); //0.5, 1
	dir[6] = int2(-1, 2); //-0.5, 1
	dir[7] = int2(2, -1); //1, -0.5
#endif

	int lDist[dirCount], rDist[dirCount], lDep[dirCount], rDep[dirCount];
	[unroll(dirCount)]
	for (uint b = 0; b < dirCount; b++) { lDist[b] = 0; rDist[b] = 0; lDep[b] = 0; rDep[b] = 0; }
	uint bestDir = 8;

	[loop]
	for (int c = 1; c <= TheLazyCowboy1_BackgroundTestNum; c++) {
		int halfC = (int)(TheLazyCowboy1_MinObjectDepth + c * 0.5f * TheLazyCowboy1_ProjectionMod);
		int targetDep = origDep + halfC;
		if (targetDep >= 29) { //no longer possible to find any viable pixels, so just break out of the loop
			break;
		}

		[unroll(dirCount)]
		for (uint d = 0; d < dirCount; d++) {
#if THELAZYCOWBOY1_SIMPLERLAYERS
			int2 offset = dir[d] * c;
#else
			int2 offset = (dir[d] * c) / 2;
#endif
			if (lDist[d] == 0) {
				int dep = depthOfTexel(startPos + offset);
				if (dep > targetDep) {
					lDist[d] = c;
					lDep[d] = dep;
					if (rDist[d] > 0) {
						bestDir = d;
						break;
					}
				}
			}
			if (rDist[d] == 0) {
				int dep = depthOfTexel(startPos - offset);
				if (dep > targetDep) {
					rDist[d] = c;
					rDep[d] = dep;
					if (lDist[d] > 0) {
						bestDir = d;
						break;
					}
				}
			}
		}

		if (bestDir < 8) { //we've already made our pick
			break;
		}
	}

	//if (c > TheLazyCowboy1_BackgroundTestNum) { //did not find ANYTHING for the background
	if (bestDir >= 8) {
		return half4(0, 31/255.0f, 0, 0); //fully thick layer1; otherwise irrelevant data so just 0
	}

	int layer1thick = (int)(TheLazyCowboy1_MinObjectDepth + (lDist[bestDir] + lDist[bestDir]) * 0.5f * TheLazyCowboy1_ProjectionMod);
		//SHOULD PROBABLY HAVE SOME SPECIAL LOGIC IF ONE RAY IS SKY BUT THE OTHER IS NOT
	float totalDist = lDist[bestDir] + rDist[bestDir];
	float layer2dep = round((lDep[bestDir] * rDist[bestDir] + rDep[bestDir] * lDist[bestDir]) / totalDist); //basically a weighted average, where the weight of lDep = rDist

	return half4(
		rDist[bestDir] / 255.0f,
		layer1thick / 255.0f,
		layer2dep / 255.0f,
		(lDist[bestDir] | (bestDir << 5)) / 255.0f
	);

}
ENDCG
				
			}
		} 
	}
}