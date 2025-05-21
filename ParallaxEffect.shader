//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/ParallaxEffect" //Unlit Transparent Vertex Colored Additive 
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
		Blend SrcAlpha OneMinusSrcAlpha 
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

			#pragma multi_compile _ THELAZYCOWBOY1_SINESMOOTHING
			#pragma multi_compile _ THELAZYCOWBOY1_INVSINESMOOTHING
			#pragma multi_compile _ THELAZYCOWBOY1_DEPTHCURVE
			#pragma multi_compile _ THELAZYCOWBOY1_INVDEPTHCURVE
			#pragma multi_compile _ THELAZYCOWBOY1_NOCENTERWARP
			#pragma multi_compile _ THELAZYCOWBOY1_CLOSESTPIXELONLY
			#pragma multi_compile _ THELAZYCOWBOY1_DYNAMICOPTIMIZATION
			#pragma multi_compile _ THELAZYCOWBOY1_WARPMAINTEX

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;


sampler2D _NoiseTex2;

sampler2D _LevelTex;
uniform float2 _LevelTex_TexelSize;

uniform float4 _spriteRect;
//uniform fixed _rimFix;

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

uniform float TheLazyCowboy1_Warp;
//uniform float TheLazyCowboy1_WarpY;
uniform float TheLazyCowboy1_MaxWarp;
//uniform float TheLazyCowboy1_MaxWarpY;
uniform float TheLazyCowboy1_CamPosX;
uniform float TheLazyCowboy1_CamPosY;
uniform uint TheLazyCowboy1_TestNum;
uniform float TheLazyCowboy1_StepSize;
uniform float TheLazyCowboy1_StartOffset;
uniform float TheLazyCowboy1_RedModScale;
uniform float TheLazyCowboy1_MaxXDistance;
uniform float TheLazyCowboy1_BackgroundScale;
uniform float TheLazyCowboy1_AntiAliasingFac;

inline float depthCurve(float d) {
#if THELAZYCOWBOY1_DEPTHCURVE && THELAZYCOWBOY1_INVDEPTHCURVE //why not thrown in a 4th, median option??
	return d*(2 - d); //simple parabola
#elif THELAZYCOWBOY1_DEPTHCURVE
	return d*(d*(d - 3) + 3); //much more severe, cubic curve
#elif THELAZYCOWBOY1_INVDEPTHCURVE
	//return d*d*d*(10 + d*(-15 + 6*d)); //fade curve
	return 0.5f*d * (d*d + 1); //simply average d^3 with d === (d*d*d + d) / 2
#else
	return d; //linear
#endif
}
inline float depthOfPixel(fixed4 col) {
	if (col.r < 0.997f) { // if red <= 254/255
		return depthCurve((((int)(((uint)(round(col.r * 255) - 1)) % 30)) - 5) * 0.04f);
	}
	return 1.0f;
}

inline float sinSmoothCurve(float x) {
#if THELAZYCOWBOY1_SINESMOOTHING && THELAZYCOWBOY1_INVSINESMOOTHING
	return 0.125f*x*(15 + x*x*(-10 + x*x*3)); //extreme option
#elif THELAZYCOWBOY1_SINESMOOTHING
	//return approxSine(x);
	return x*(1.5f - 0.5f*x*x); //this is a really cheap but more than adaquate approximation!
#elif THELAZYCOWBOY1_INVSINESMOOTHING
	//return x + x - approxSine(x);
	return 0.5f*x * (x*x + 1); //simply average d^3 with d === (d*d*d + d) / 2
#else
	return x;
#endif
}

half4 frag (v2f i) : SV_Target
{
		//Moved up here in case discard isn't always discarding...?
	uint notFound = 1; //saves A LOT of processing, unless CLOSESTPIXELONLY is chosen

		//uses the reverse of the calculations used by other shaders using _spriteRect. They use it to convert from scrPos to uv; so I reversed it here
	float2 scrPos = float2(i.uv.x * (_spriteRect.z - _spriteRect.x) + _spriteRect.x, i.uv.y * (_spriteRect.w - _spriteRect.y) + _spriteRect.y);
	//scrPos = scrPos * 0.95f + 0.025f; //slightly shrink scrPos to avoid issues at the edges of the screen

	if (scrPos.x < -0.01f || scrPos.x > 1.01f || scrPos.y < -0.01f || scrPos.y > 1.01f) { //inflates rendered size by 2%, just in case
		notFound = 0;
		discard;
	}

//#if THELAZYCOWBOY1_NOCENTERWARP
	//float posCamXDiff = sinSmoothCurve((scrPos.x - TheLazyCowboy1_CamPosX) * 2 * abs(TheLazyCowboy1_CamPosX - 0.5f));
	//float posCamYDiff = sinSmoothCurve((scrPos.y - TheLazyCowboy1_CamPosY) * 2 * abs(TheLazyCowboy1_CamPosY - 0.5f));
//#else
	float absBackScale = abs(TheLazyCowboy1_BackgroundScale); //prevents ridiculous results when BackgroundScale is < 0, especially: -1 caused division by 0
	float camDiffMod = 1 / (absBackScale + 0.5f * (1 - absBackScale));
	float posCamXDiff = sinSmoothCurve(camDiffMod * (scrPos.x*TheLazyCowboy1_BackgroundScale + 0.5f*(1-TheLazyCowboy1_BackgroundScale) - TheLazyCowboy1_CamPosX));
	float posCamYDiff = sinSmoothCurve(camDiffMod * (scrPos.y*TheLazyCowboy1_BackgroundScale + 0.5f*(1-TheLazyCowboy1_BackgroundScale) - TheLazyCowboy1_CamPosY));
//#endif
#if THELAZYCOWBOY1_NOCENTERWARP
	float camXDiff2 = 4*(TheLazyCowboy1_CamPosX - 0.5f)*(TheLazyCowboy1_CamPosX - 0.5f);
	posCamXDiff = posCamXDiff * camXDiff2 * (2 - camXDiff2); //posCamXDiff *= 2c^2 - c^4; c = 2 * (camPos - 0.5)
	float camYDiff2 = 4*(TheLazyCowboy1_CamPosY - 0.5f)*(TheLazyCowboy1_CamPosY - 0.5f);
	posCamYDiff = posCamYDiff * camYDiff2 * (2 - camYDiff2);
#endif

	//float2 moveStep = TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize * _LevelTex_TexelSize * float2(
	float2 moveStep = float2(
		clamp(posCamXDiff, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp),
		clamp(posCamYDiff, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp)
		);

	//OPTIMIZATION

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	//float maxWarpFac = max(max(abs(moveStep.x), abs(moveStep.y)) / TheLazyCowboy1_MaxWarp, 4.0f / TheLazyCowboy1_TestNum);
	float invWarpFac = min(
		TheLazyCowboy1_MaxWarp / max(abs(moveStep.x), abs(moveStep.y)) * 0.95f, //0.95 == arbitrary constant: optimizes slightly less than full potential
		0.25f * TheLazyCowboy1_TestNum); //can't be less than 4 totalTests
#endif
		//scale moveStep back up to its proper size
	moveStep = moveStep * TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize * _LevelTex_TexelSize;

	uint totalTests = TheLazyCowboy1_TestNum;
	float stepSize = TheLazyCowboy1_StepSize;

		//Do this before scaling values!
	float2 grabPos = i.uv + moveStep * totalTests * TheLazyCowboy1_StartOffset;

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	if (invWarpFac > 1) {
		stepSize = stepSize * invWarpFac;
		moveStep = moveStep * invWarpFac;
		totalTests = ceil(totalTests / invWarpFac);
	}
#endif

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	float bestScore = 20;
	float invStepSize = 1 / stepSize;
#endif

#if THELAZYCOWBOY1_WARPMAINTEX
	float2 bestGrabPos = i.uv;
#else
	fixed4 bestCol = tex2D(_LevelTex, i.uv);
	float redColorMod = 0;
#endif

	uint counter = 0;
	float noiseVal = tex2D(_NoiseTex2, i.uv).x;
	float percentage = TheLazyCowboy1_StartOffset
		+ TheLazyCowboy1_AntiAliasingFac * stepSize * clamp(noiseVal - 0.3f, 0, 0.4f) //a SIGNIFICANT shift (up to 2/5th step) in order to break up straight lines...
		+ 0.0009765625f; //add a very tiny margin of error: 1/1024

	[loop]
	while (notFound && counter <= totalTests) {
		fixed4 newCol = tex2D(_LevelTex, grabPos);
		float newDepth = depthOfPixel(newCol);

		float xDistance = percentage - max(newDepth, TheLazyCowboy1_StartOffset); //newDepth = amount warped; percentage = amount warped; compare them for closeness, then!
#if THELAZYCOWBOY1_CLOSESTPIXELONLY
		if (xDistance >= 0 && xDistance <= TheLazyCowboy1_MaxXDistance) {
#else
		if (xDistance >= 0) {
#endif

#if THELAZYCOWBOY1_WARPMAINTEX
			bestGrabPos = grabPos;
#else
			bestCol = newCol;
			redColorMod = xDistance;
#endif
			notFound = 0; //we found it; no reason to search further!
		}

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
		else {
			xDistance = abs(xDistance);
			float score = (floor(xDistance * invStepSize) + newDepth * 2) * stepSize; //newDepth is only a deciding factor if the distances are within 2 steps
			if (score < bestScore) {
#if THELAZYCOWBOY1_WARPMAINTEX
				bestGrabPos = grabPos;
#else
				bestCol = newCol;
				redColorMod = xDistance;
#endif
				bestScore = score;
			}
		}
#endif

		grabPos = grabPos + moveStep;
		percentage = percentage + stepSize;
		counter = counter + 1;
	}

#if THELAZYCOWBOY1_WARPMAINTEX
	return tex2D(_MainTex, bestGrabPos);
#else

	//apply redColorMod

	if (redColorMod >= stepSize) {
		redColorMod = redColorMod * TheLazyCowboy1_RedModScale;
				//add noise to roughen it up a bit
#if THELAZYCOWBOY1_CLOSESTPIXELONLY
		if (notFound) { //found using closest pixel
			redColorMod = redColorMod * max(noiseVal - 0.4f, 0);
		} else {
			redColorMod = redColorMod + min(redColorMod, 0.4f) * (noiseVal - 0.5f) * 0.5f; //up to ~5 pixel variation: 0.4 * 0.5 = 0.2; 0.2 * 25px = 5px
		}
#else
		redColorMod = redColorMod + min(redColorMod, 0.4f) * (noiseVal - 0.5f) * 0.5f; //up to ~5 pixel variation: 0.4 * 0.5 = 0.2; 0.2 * 25px = 5px
#endif

		float currDepth = ((uint)(bestCol.r * 255 - 1)) % 30;
		float depthShift = redColorMod * 25;
		bestCol.r = bestCol.r + clamp(depthShift, -currDepth, 29 - currDepth) * 0.0039f; //~= 1 / 255
	}

	return bestCol;
#endif
}
ENDCG
				
			}
		} 
	}
}