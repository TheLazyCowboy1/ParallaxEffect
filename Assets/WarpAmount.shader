//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/WarpAmount" //Unlit Transparent Vertex Colored Additive 
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		//Tags { "queue" = "Geometry"}
		//Tags { "queue" = "Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend Off
		//Blend SrcAlpha OneMinusSrcAlpha 
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

			GrabPass { "_ParallaxGrabTex" } //GrabPass to get screen data so that I can then warp it

			Pass 
			{
				//Tags { "LightMode"="ShadowCaster"}
				//ZWrite On
				
CGPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag
//#pragma debug

//#pragma multi_compile _ THELAZYCOWBOY1_TERRAIN

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
//#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

sampler2D _LevelTex;
Texture2D<float4> _PreLevelColorGrab;//sampler2D _PreLevelColorGrab;
Texture2D<float4> _SlopedTerrainMask;//sampler2D _SlopedTerrainMask;

RWTexture2D<fixed> _MyUAV : register(u1);

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
//inline half terrainColor(float2 pos) {
	//return 2 - 3 * tex2D(_SlopedTerrainMask, pos).r;
inline half terrainColor(int2 pos) {
	return 2 - 3 * _SlopedTerrainMask.Load(int3(pos, 0)).r;
}

//void frag (v2f i, out float outputDepth : SV_Depth)
//float frag(v2f i) : SV_Depth
void frag (v2f i)
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

	int2 checkPos = int2(round(i.uv * _screenSize));

//#if THELAZYCOWBOY1_TERRAIN
	uint d = min(depthOfPixel(lev), round(terrainColor(checkPos) * 30));
//#else
//	uint d = depthOfPixel(lev);
//#endif

		//check creature mask if applicable
	if (d > 5) {
		half4 c = _PreLevelColorGrab.Load(int3(checkPos, 0));//tex2D(_PreLevelColorGrab, i.uv);
		if (c.r > 1.0f / 255.0f || c.g > 0 || c.b > 0) {
			d = 5;
		}
	}

	float result = d / 32.0f;
	//_MyUAV[int2((int)round(i.uv.x * _screenSize.x), (int)round(i.uv.y * _screenSize.y))] = float4(result, 0, 0, 1);
	_MyUAV[checkPos] = result;//float4(result, 0, 0, 1);
	//outputDepth = i.uv.x;
	//return result;

	//return half4(result, 0, 0, 0);

}
ENDCG
				
			}
		
			Pass 
			{
				//Tags { "LightMode"="ShadowCaster"}
				//ZWrite On
				
CGPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag
//#pragma debug

#pragma multi_compile _ THELAZYCOWBOY1_SINESMOOTHING
#pragma multi_compile _ THELAZYCOWBOY1_INVSINESMOOTHING
#pragma multi_compile _ THELAZYCOWBOY1_DEPTHCURVE
#pragma multi_compile _ THELAZYCOWBOY1_INVDEPTHCURVE
#pragma multi_compile _ THELAZYCOWBOY1_NOCENTERWARP
#pragma multi_compile _ THELAZYCOWBOY1_CLOSESTPIXELONLY
#pragma multi_compile _ THELAZYCOWBOY1_DYNAMICOPTIMIZATION
//#pragma multi_compile _ THELAZYCOWBOY1_PROCESSLAYER2
//#pragma multi_compile _ THELAZYCOWBOY1_PROCESSLAYER3

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
//#include "_TerrainMask.cginc"
//#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float4 _MainTex_TexelSize;

#if defined(SHADER_API_PSSL)
Texture2D<float4> _ParallaxGrabTex;//sampler2D _ParallaxGrabTex;
#else
Texture2D<float4> _ParallaxGrabTex : register(t0);//sampler2D _ParallaxGrabTex : register(s0);
#endif
uniform float2 _ParallaxGrabTex_TexelSize;

sampler2D _NoiseTex;
uniform float4 _NoiseTex_TexelSize;
//sampler2D _NoiseTex2;

//sampler2D _LevelTex;
//uniform float2 _LevelTex_TexelSize;

//sampler2D _TheLazyCowboy1_ScreenLevelTex;
//uniform float4 _TheLazyCowboy1_ScreenLevelTex_TexelSize;
//uniform float4 _TheLazyCowboy1_ScreenLevelTex_ST;

//sampler2D _CameraDepthTexture;
//uniform float2 _CameraDepthTexture_TexelSize;

RWTexture2D<fixed> _MyUAV : register(u1);
uniform float2 _screenSize;

//uniform float4 _spriteRect;
//uniform fixed _rimFix;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
	float2  nuv : TEXCOORD1;
	float2 suv : TEXCOORD2;
    //float2 grabPos : TEXCOORD1;
    //float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
	o.nuv = o.uv * float2(5, 4);
	o.suv = round(o.uv * _screenSize);
	//o.grabPos = ComputeGrabScreenPos(o.pos);
    //o.scrPos = ComputeScreenPos(o.pos);
    //o.clr = v.color;
    return o;
}

#if THELAZYCOWBOY1_PROCESSLAYER2 || THELAZYCOWBOY1_PROCESSLAYER3
sampler2D _TheLazyCowboy1_Layer2Tex;
#endif
#if THELAZYCOWBOY1_PROCESSLAYER3
sampler2D _TheLazyCowboy1_Layer3Tex;
#endif

uniform float TheLazyCowboy1_Warp;
uniform float TheLazyCowboy1_MaxWarp;
uniform float TheLazyCowboy1_CamPosX;
uniform float TheLazyCowboy1_CamPosY;
uniform uint TheLazyCowboy1_TestNum;
uniform float TheLazyCowboy1_StepSize;
uniform float TheLazyCowboy1_StartOffset;
uniform float TheLazyCowboy1_RedModScale;
uniform float TheLazyCowboy1_MaxXDistance;
uniform float TheLazyCowboy1_BackgroundScale;
uniform float TheLazyCowboy1_AntiAliasingFac;

inline half depthCurve(half d) {
#if THELAZYCOWBOY1_DEPTHCURVE && THELAZYCOWBOY1_INVDEPTHCURVE //why not thrown in a 4th, median option??
	return d*(2 - d); //simple parabola
#elif THELAZYCOWBOY1_DEPTHCURVE
	return d*(d*(d - 3) + 3); //much more severe, cubic curve
#elif THELAZYCOWBOY1_INVDEPTHCURVE
	return 0.5*d * (d*d + 1); //simply average d^3 with d === (d*d*d + d) / 2
#else
	return d; //linear
#endif
}

inline float sinSmoothCurve(float x) {
#if THELAZYCOWBOY1_SINESMOOTHING && THELAZYCOWBOY1_INVSINESMOOTHING
	return 0.125f*x*(15 + x*x*(-10 + x*x*3)); //extreme option
#elif THELAZYCOWBOY1_SINESMOOTHING
	return x*(1.5f - 0.5f*x*x); //this is a really cheap but more than adaquate sine approximation!
#elif THELAZYCOWBOY1_INVSINESMOOTHING
	return 0.5f*x * (x*x + 1); //simply average d^3 with d === (d*d*d + d) / 2
#else
	return x;
#endif
}

inline float highFreqNoise(float2 uv, float2 scale) {
	float2 nuv = fmod(uv * scale, 1);
	float2 rawLerpFac = 2 * (nuv - float2(0.5f, 0.5f));
	rawLerpFac = rawLerpFac * rawLerpFac; //^2 + abs
	float lerpFac = max(rawLerpFac.x, rawLerpFac.y);
	lerpFac = lerpFac * lerpFac; //^4
	lerpFac = lerpFac * lerpFac; //^6
	lerpFac = lerpFac * lerpFac; //^8
	lerpFac = lerpFac * lerpFac; //^10
	float n1 = tex2Dlod(_NoiseTex, float4(nuv, 0, 0)).x;
	float n2 = tex2Dlod(_NoiseTex, float4(nuv + _NoiseTex_TexelSize.xy * 10, 0, 0)).x; //10 pixel buffer
	return lerp(n1, n2, lerpFac * 0.5f);
}


half4 frag (v2f i) : SV_Target
{
	//return float4(_MyUAV[int2(round(i.uv * _screenSize))], ((int)round(i.uv.x * _screenSize.x))&1, i.uv.x >= 1399.5/1400.0 || i.uv.x <= 0.5/1400.0, 1);
	//float4 depthTex = tex2D(_CameraDepthTexture, i.uv);
	//return float4(depthTex.x, depthTex.z, depthTex.w, 1);
	
	half absBackScale = abs(TheLazyCowboy1_BackgroundScale); //prevents ridiculous results when BackgroundScale is < 0, especially: -1 caused division by 0
	half camDiffMod = 1 / (absBackScale + 0.5 * (1 - absBackScale));
	float posCamXDiff = sinSmoothCurve(camDiffMod * (i.uv.x*TheLazyCowboy1_BackgroundScale + 0.5f*(1-TheLazyCowboy1_BackgroundScale) - TheLazyCowboy1_CamPosX));
	float posCamYDiff = sinSmoothCurve(camDiffMod * (i.uv.y*TheLazyCowboy1_BackgroundScale + 0.5f*(1-TheLazyCowboy1_BackgroundScale) - TheLazyCowboy1_CamPosY));
//#endif
#if THELAZYCOWBOY1_NOCENTERWARP
	float camXDiff2 = 4*(TheLazyCowboy1_CamPosX - 0.5f)*(TheLazyCowboy1_CamPosX - 0.5f);
	posCamXDiff = posCamXDiff * camXDiff2 * (2 - camXDiff2); //posCamXDiff *= 2c^2 - c^4; c = 2 * (camPos - 0.5)
	float camYDiff2 = 4*(TheLazyCowboy1_CamPosY - 0.5f)*(TheLazyCowboy1_CamPosY - 0.5f);
	posCamYDiff = posCamYDiff * camYDiff2 * (2 - camYDiff2);
#endif

	//OPTIMIZATION

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	float2 moveStep = float2(posCamXDiff, posCamYDiff);
	//half maxWarpFac = max(max(abs(moveStep.x), abs(moveStep.y)) / TheLazyCowboy1_MaxWarp, 4.0 / TheLazyCowboy1_TestNum);
	float invWarpFac = min(
			TheLazyCowboy1_MaxWarp / max(abs(moveStep.x), abs(moveStep.y)),
			0.5f * TheLazyCowboy1_TestNum); //can't be less than 2 totalTests
#else
	//half2 moveStep = TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize * _LevelTex_TexelSize * half2(
	float2 moveStep = float2(
		clamp(posCamXDiff, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp), //clamp it to maxWarp
		clamp(posCamYDiff, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp)
		);
#endif

		//scale moveStep back up to its proper size
	moveStep = moveStep * TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize;// / _screenSize;// * uvMapScale;

	uint totalTests = TheLazyCowboy1_TestNum;
	float stepSize = TheLazyCowboy1_StepSize;

		//Do this before scaling values! If it's done afterward, then there will be noticeable lines where totalTests change
	//float2 grabPos = i.uv + moveStep * totalTests * TheLazyCowboy1_StartOffset;
	float2 grabPos = i.suv - moveStep * totalTests; //start at the END and then move BACKWARDS

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	if (invWarpFac > 1) {
		stepSize = stepSize * invWarpFac;
		moveStep = moveStep * invWarpFac;
		totalTests = ceil(totalTests / invWarpFac);
	}
#endif

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	half bestScore = 20;
	half maxXDist = max(TheLazyCowboy1_MaxXDistance, stepSize);
	//bool notFound = true;
#endif

	//half2 initGrabPos = (i.uv + uvMapShift) * uvMapScale;
	//float2 bestGrabPos = i.uv;
	int2 bestGrabPos = int2(i.suv);
	//uint bestLayer = 0;
	//half redColorMod = 0;
	half bestXDist = 1;
	//float2 bestXDistPos = i.uv;
	half bestDep = 1;

	//uint counter = 0;
	//uint notFound = 1; //now only used by closestPixelOnly
	half noiseVal = tex2D(_NoiseTex, i.nuv).x;//highFreqNoise(i.uv, float2(5, 4));//tex2D(_NoiseTex2, i.uv).x;
	float percentage = 0//TheLazyCowboy1_StartOffset
		+ TheLazyCowboy1_AntiAliasingFac * stepSize * clamp(noiseVal - 0.3f, 0, 0.4f) //a SIGNIFICANT shift (up to 2/5th step) in order to break up straight lines...
		//+ 0.000976562f; //add a very tiny margin of error: 1/1024
		+ stepSize * 0.001f; //add 1/1000th of a step just as a VERY tiny margin of error

	//try using int coordinates now!
	//moveStep = moveStep * _screenSize;
	//grabPos = grabPos * _screenSize;
	//bestGrabPos = bestGrabPos * _screenSize;
	//int2 lastCheckedPos = int2(-1000, -1000);
	//int2 nextCheckPos = int2(round(grabPos));

	[loop]
	for (uint c = 0; c <= totalTests; c++) {

		int2 checkPos = int2(round(grabPos));
		//lastCheckedPos = checkPos;

		//int2 checkPos = nextCheckPos;
		//grabPos = grabPos + moveStep;
		//nextCheckPos = int2(round(grabPos));
//#if !THELAZYCOWBOY1_CLOSESTPIXELONLY
//don't check the same pixel twice; although this can have minor visual issues with ClosestPixelOnly
/*
		if (checkPos.x == nextCheckPos.x && checkPos.y == nextCheckPos.y && c < totalTests) { //always check last test
			grabPos = grabPos + moveStep;
			percentage = percentage + stepSize;
			continue; //we already checked this pixel; it's boring
		}
*/
//#endif

		half currDepth = round(_MyUAV[checkPos] * 32) / 30.0f;
		//half currDepth = round(tex2D(_TheLazyCowboy1_ScreenLevelTex, grabPos).r * 30 - 5) * 0.04;//depthOfPixel(tex2D(_LevelTex, grabPos).r);
		//half currDepth = round(tex2D(_CameraDepthTexture, grabPos).r * 30) / 30.0f;
		half newDepth = currDepth >= 1
			? 1
			: depthCurve(currDepth) * TheLazyCowboy1_RedModScale; //multiply by RedModScale so that the background is treated like it's even farther away

		//half xDistance = percentage - max(newDepth, TheLazyCowboy1_StartOffset); //newDepth = amount warped; percentage = amount warped; compare them for closeness, then!
		half xDistance = percentage - newDepth;

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
		//OBVIOUSLY HAS SOME EXTRA LOGIC
		half score;
		if (xDistance >= 0) {
			if (xDistance < maxXDist) {
				bestXDist = xDistance;
				//bestXDistPos = grabPos;
				bestGrabPos = checkPos;
				bestDep = newDepth;
				//notFound = false;
				bestScore = -3;
				break; //we found it! don't run any more code, ideally
			}
			score = xDistance + 2;
		}
		else {
			score = -percentage;
			xDistance = -xDistance;
		}
		if (score < bestScore) {
			bestGrabPos = checkPos;
			bestScore = score;
			bestDep = newDepth;
		}
		if (xDistance < bestXDist) {
			bestXDist = xDistance;
			//bestXDistPos = grabPos;
		}
#else
		//OBVIOUSLY WAY SIMPLER
		if (xDistance >= 0) {
			bestXDist = xDistance;
			//bestXDistPos = grabPos;
			bestGrabPos = checkPos;
			bestDep = newDepth;
			//notFound = 0;
			break;
		}
#endif

		grabPos = grabPos + moveStep;
		percentage = percentage + stepSize;
	}


//APPLY FINAL NOISE
	half4 finalCol = _ParallaxGrabTex.Load(int3(bestGrabPos, 0));
	//finalCol.w = 1;

	if (bestXDist <= stepSize || bestDep >= 1) { //it's close enough; don't add noise. Also don't add noise to the sky
		return finalCol;
	}
	//noiseVal = highFreqNoise(bestXDistPos, float2(5, 4));
#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	noiseVal = highFreqNoise(lerp(bestGrabPos / _screenSize, i.uv, (bestScore <= -3) ? bestDep : saturate(bestDep + bestXDist)), float2(5, 4)); //slightly different logic depending on whether we're making up the pixel or not
#else
	noiseVal = highFreqNoise(lerp(bestGrabPos / _screenSize, i.uv, saturate(bestDep + bestXDist)), float2(5, 4)); //just approximate the right position instead of going through all the processing to find the right point
#endif
	half add = bestXDist * TheLazyCowboy1_AntiAliasingFac * (noiseVal - 0.5h);
	finalCol.x += add;
	finalCol.y += add;
	finalCol.z += add;
	return finalCol;

}
ENDCG
				
			}
		} 
	}
}
