//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/ParallaxBackgroundBuilder" //Unlit Transparent Vertex Colored Additive 
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Layer2Tex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		//Blend SrcAlpha OneMinusSrcAlpha 
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
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
//#pragma debug

			#pragma multi_compile _ THELAZYCOWBOY1_INCLUDELAYER2
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

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
//#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;
//sampler2D _LevelTex;
//uniform float2 _LevelTex_TexelSize;
#if THELAZYCOWBOY1_INCLUDELAYER2
//sampler2D _TheLazyCowboy1_Layer2Tex;
sampler2D _Layer2Tex;
#endif

uniform float TheLazyCowboy1_ProjectionMod;
uniform float TheLazyCowboy1_MinObjectDepth;

//sampler2D _NoiseTex2;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.clr = v.color;
    return o;
}

inline int depthOfPixel(fixed4 col) {
	//if (col.r < 0.997f) { // if red <= 254/255
	//	return (uint)(round(col.r * 255) - 1) % 30;
	//}
	//return 30; //sky
	return (col.r < 0.997f) ? ((uint)(round(col.r * 255) - 1) % 30) : 30;
}
inline int maxInt(int a, int b) {
	return (a > b) ? a : b;
}
inline int minInt(int a, int b) {
	return (a < b) ? a : b;
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
		dul, ddr (diagnol-upleft, diagnol-downright)
		ddl, dur (diagnol-downleft, diagnol-upright)
	*/

#if THELAZYCOWBOY1_INCLUDELAYER2
	fixed4 origCol = tex2D(_Layer2Tex, i.uv);
#else
	fixed4 origCol = tex2D(_MainTex, i.uv);
#endif
	int origDep = depthOfPixel(origCol);

	if (origDep >= 30) {
		//discard;
		return fixed4(1, 1, 1, 1);
	}

	//const uint dirCount = 4;
	float2 dir[dirCount];
	dir[0] = float2(_MainTex_TexelSize.x, 0); //1,0
	dir[1] = float2(0, _MainTex_TexelSize.y); //0,1
//#if dirCount > 2
	dir[2] = _MainTex_TexelSize; //1,1
	dir[3] = float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y); //1,-1 (equivalent to -1,1)
//#endif
#if !THELAZYCOWBOY1_SIMPLERLAYERS //dirCount > 4
	dir[4] = float2(_MainTex_TexelSize.x, 0.5f * _MainTex_TexelSize.y);
	dir[5] = float2(0.5f * _MainTex_TexelSize.x, _MainTex_TexelSize.y);
	dir[6] = float2(-0.5f * _MainTex_TexelSize.x, _MainTex_TexelSize.y);
	dir[7] = float2(_MainTex_TexelSize.x, -0.5f * _MainTex_TexelSize.y);
#endif

	uint leftDist[dirCount], rightDist[dirCount];
	fixed4 leftCol[dirCount], rightCol[dirCount];
	[unroll(dirCount)]
	for (uint b = 0; b < dirCount; b++) { leftDist[b] = 0; rightDist[b] = 0; leftCol[b] = fixed4(1, 1, 1, 1); rightCol[b] = fixed4(1, 1, 1, 1); }
	int leftDepth[dirCount], rightDepth[dirCount];

	//PROJECT RAYS

		//pre-calculate halfC
	int halfC[testNum];
	[unroll(testNum)]
	for (uint k = 0; k < testNum; k++) {
		halfC[k] = (int)(TheLazyCowboy1_MinObjectDepth + (k+1) * 0.5f * TheLazyCowboy1_ProjectionMod);
	}

	//[unroll(testNum)]
	//for (uint c = 1; c <= testNum; c++) {
	[unroll(dirCount)]
	for (uint d = 0; d < dirCount; d++) {
		//int halfC = (int)(3.5f + c * 0.5f * TheLazyCowboy1_ProjectionMod);
		//[unroll(dirCount)]
		//for (uint d = 0; d < dirCount; d++) {
		uint notDone = 1;
		[unroll(testNum)]
		//[loop] //it has breaks in it
		for (uint c = 1; c <= testNum; c++) {
			//int halfC = (int)(3.5f + c * 0.5f * TheLazyCowboy1_ProjectionMod);
			int targetDep = origDep + halfC[c-1];
			//if (targetDep > 29) { break; }
			if (notDone && targetDep <= 29) {
				//LEFT
				fixed4 col = tex2D(_MainTex, i.uv - c * dir[d]);
				int dep = depthOfPixel(col);
				if (dep > targetDep) {
					leftDist[d] = c;
					leftDepth[d] = dep;
					leftCol[d] = col;
					//break;
					notDone = 0;
				}
#if THELAZYCOWBOY1_INCLUDELAYER2
				if (notDone) {
					col = tex2D(_Layer2Tex, i.uv - c * dir[d]);
					dep = depthOfPixel(col);
					if (dep > targetDep) {
						leftDist[d] = c;
						leftDepth[d] = dep;
						leftCol[d] = col;
						//break;
						notDone = 0;
					}
				}
#endif
			}
		}

		notDone = 1;
		[unroll(testNum)]
		//[loop] //it has breaks in it
		for (uint c = 1; c <= testNum; c++) {
			//int halfC = (int)(3.5f + c * 0.5f * TheLazyCowboy1_ProjectionMod);
			int targetDep = origDep + halfC[c-1];
			//if (targetDep > 29) { break; }
			if (notDone && targetDep <= 29) {
				//RIGHT
				fixed4 col = tex2D(_MainTex, i.uv + c * dir[d]);
				int dep = depthOfPixel(col);
				if (dep > targetDep) {
					rightDist[d] = c;
					rightDepth[d] = dep;
					rightCol[d] = col;
						//break;
						notDone = 0;
				}
#if THELAZYCOWBOY1_INCLUDELAYER2
				if (notDone) {
					col = tex2D(_Layer2Tex, i.uv + c * dir[d]);
					dep = depthOfPixel(col);
					if (dep > targetDep) {
						rightDist[d] = c;
						rightDepth[d] = dep;
						rightCol[d] = col;
						//break;
						notDone = 0;
					}
				}
#endif
			}
		}
	}
	

	//INTERPRET RAYS

	int leftRightDepth[dirCount];
	uint leftRightDist[dirCount];
	fixed4 leftRightCol[dirCount];

	[unroll(dirCount)]
	for (uint a = 0; a < dirCount; a++) {
		//leftRightDist[a] = testNump1;
		leftRightCol[a] = fixed4(1, 1, 1, 1); //sky
		/*if (leftDist[a] == 0 && rightDist[a] == 0) { //neither ray found anything deeper
			leftRightDepth[a] = 31;
		}
		else if (leftDist[a] == 0) { //left wall
			leftRightDepth[a] = maxInt(rightDepth[a] - origDep, 10);
			leftRightDist[a] = rightDist[a];
			leftRightCol[a] = rightCol[a];
		}
		else if (rightDist[a] == 0) { //right wall
			leftRightDepth[a] = maxInt(leftDepth[a] - origDep, 10);
			leftRightDist[a] = leftDist[a];
			leftRightCol[a] = leftCol[a];
		}
		else { //both sides found
		*/
		if (leftDist[a] > 0 && rightDist[a] > 0) {
			leftRightDepth[a] = (int)(TheLazyCowboy1_MinObjectDepth + (leftDist[a] + rightDist[a]) * 0.5f * TheLazyCowboy1_ProjectionMod);
			leftRightDist[a] = minInt(leftDist[a], rightDist[a]);
			if (leftDist[a] < rightDist[a] && leftDepth[a] < 30) { //left closer
				leftRightCol[a] = leftCol[a];
				if (rightDepth[a] < 30) {
					leftRightCol[a].r = leftRightCol[a].r + min(((float)rightDepth[a] - (float)leftDepth[a]) * (float)leftDist[a] / (leftDist[a] + rightDist[a]), 29 - leftDepth[a]) / 255;
				}
			} else if (rightDepth[a] < 30) { //right closer
				leftRightCol[a] = rightCol[a];
				if (leftDepth[a] < 30) {
					leftRightCol[a].r = leftRightCol[a].r + min(((float)leftDepth[a] - (float)rightDepth[a]) * (float)rightDist[a] / (leftDist[a] + rightDist[a]), 29 - rightDepth[a]) / 255;
				}
			}
		}
		else {
			leftRightDist[a] = testNump1;
			leftRightDepth[a] = 31;
		}
	}


	//FIND BEST RESULTS

	fixed4 bestCol = leftRightCol[0];
	uint bestDist = leftRightDist[0];
	[unroll(dirCountm1)]
	for (uint f = 1; f < dirCount; f++) {
		if (leftRightDist[f] < bestDist) {
			bestCol = leftRightCol[f];
			bestDist = leftRightDist[f];
		}
	}

	//ENCODE DEPTH INFORMATION
	int bestDepth = leftRightDepth[0];
	[unroll(dirCountm1)]
	for (uint g = 1; g < dirCount; g++) {
		bestDepth = minInt(bestDepth, leftRightDepth[g]);
	}

	//if it's a useless background, set it to sky color
	//if (depthOfPixel(bestCol) <= origDep + bestDepth) {
	if (depthOfPixel(bestCol) <= origDep) { //should be totally impossible; but just in case...
		bestCol.rgb = fixed3(1, 1, 1);
	}

	bestCol.w = saturate((1 + bestDepth) / 31.0f);//maxInt(1, minInt(31, 1 + bestDepth)) / 31.0f;

	return bestCol;
}
ENDCG
				
			}
		} 
	}
}