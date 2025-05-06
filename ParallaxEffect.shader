// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

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

inline float approxSine(float x) {
	return x*(1.5f - 0.5f*x*x); //this is a really cheap but more than adaquate approximation!
}
inline float sinSmoothCurve(float x) {
	#if THELAZYCOWBOY1_SINESMOOTHING && THELAZYCOWBOY1_INVSINESMOOTHING
	return 0.125f*x*(15 + x*x*(-10 + x*x*3)); //extreme option
	#elif THELAZYCOWBOY1_SINESMOOTHING
	return approxSine(x);
	#elif THELAZYCOWBOY1_INVSINESMOOTHING
	return x + x - approxSine(x);
	#else
	return x;
	#endif
}

half4 frag (v2f i) : SV_Target
{
	//fixed4 origCol = tex2D(_LevelTex, i.uv);

		//uses the reverse of the calculations used by other shaders using _spriteRect. They use it to convert from scrPos to uv; so I reversed it here
	float2 scrPos = float2(i.uv.x * (_spriteRect.z - _spriteRect.x) + _spriteRect.x, i.uv.y * (_spriteRect.w - _spriteRect.y) + _spriteRect.y);
	//scrPos = scrPos * 0.95f + 0.025f; //slightly shrink scrPos to avoid issues at the edges of the screen

	if (scrPos.x < -0.01 || scrPos.x > 1.01 || scrPos.y < -0.01 || scrPos.y > 1.01) { //inflates rendered size by 2%, just in case
		discard;
	}

	#if THELAZYCOWBOY1_NOCENTERWARP
	float posCamXDiff = sinSmoothCurve(scrPos.x - TheLazyCowboy1_CamPosX) * 2 * abs(TheLazyCowboy1_CamPosX - 0.5f);
	float posCamYDiff = sinSmoothCurve(scrPos.y - TheLazyCowboy1_CamPosY) * 2 * abs(TheLazyCowboy1_CamPosY - 0.5f);
	#else
	float posCamXDiff = sinSmoothCurve(scrPos.x - TheLazyCowboy1_CamPosX);
	float posCamYDiff = sinSmoothCurve(scrPos.y - TheLazyCowboy1_CamPosY);
	#endif

	float2 moveStep = TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize * _LevelTex_TexelSize * float2(
	//float2 moveStep = TheLazyCowboy1_Warp * float2(
		clamp(posCamXDiff, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp),
		clamp(posCamYDiff, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp)
		);

		//optimization attempt; failed: produces concentric rectangles
	//float invStepSize = max(abs(moveStep.x), abs(moveStep.y)) + 1;
	//float stepSize = 1 / invStepSize;
	//float maxTests = TheLazyCowboy1_TestNum * TheLazyCowboy1_StepSize * invStepSize;
	//moveStep = moveStep * stepSize * _LevelTex_TexelSize;
	#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	float invStepSize = 1 / TheLazyCowboy1_StepSize;
	#endif

	fixed4 bestCol = tex2D(_LevelTex, i.uv);
	float bestScore = 20;
	float redColorMod = 0;

	uint totalTests = min(TheLazyCowboy1_TestNum, 300); //lazy way to limit loop size
	float2 grabPos = i.uv + moveStep * totalTests * TheLazyCowboy1_StartOffset;
	uint counter = 0;
	float noiseVal = tex2D(_NoiseTex2, i.uv).x;
	float percentage = TheLazyCowboy1_StartOffset
		+ 0.25f * TheLazyCowboy1_StepSize * max(noiseVal - 0.2f, 0) //a SIGNIFICANT shift (up to 1/5th step) in order to break up straight lines...
		+ 0.0009765625f; //add a very tiny margin of error: 1/1024
	uint notFound = 1; //saves A LOT of processing, unless CLOSESTPIXELONLY is chosen

	while (counter <= totalTests) {
		#if !THELAZYCOWBOY1_CLOSESTPIXELONLY
		if (notFound) {
		#endif
		//if (notFound && counter <= maxTests) {
			fixed4 newCol = tex2D(_LevelTex, grabPos);
			float newDepth = depthOfPixel(newCol);

			#if THELAZYCOWBOY1_CLOSESTPIXELONLY
			float xDistance = abs(percentage - max(newDepth, TheLazyCowboy1_StartOffset));
			float score = (floor(xDistance * invStepSize) + newDepth * 2) * TheLazyCowboy1_StepSize; //newDepth is only a deciding factor if the distances are within 2 steps
			if (score < bestScore) {
			#else
			float xDistance = percentage - max(newDepth, TheLazyCowboy1_StartOffset); //newDepth = amount warped; percentage = amount warped; compare them for closeness, then!
			//float score = (newDepth - oldDepth) + xDistance; //dx - dy
			//float score = newDepth + xDistance; //oldDepth was a useless constant: made it easier to understand, but didn't affect scoring
			float score = percentage;
			if (score < bestScore && xDistance >= 0) {
			#endif
				bestCol = newCol;
				bestScore = score;
				redColorMod = xDistance;
				#if !THELAZYCOWBOY1_CLOSESTPIXELONLY
				notFound = 0;
				#endif
			}

			grabPos = grabPos + moveStep;
			percentage = percentage + TheLazyCowboy1_StepSize;
		#if !THELAZYCOWBOY1_CLOSESTPIXELONLY
		}
		#endif
		counter = counter + 1;
	}

	#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	redColorMod = redColorMod * TheLazyCowboy1_RedModScale * tex2D(_NoiseTex2, i.uv).x;
	#else
	redColorMod = redColorMod * TheLazyCowboy1_RedModScale;
		//add noise to roughen it up a bit
	redColorMod = redColorMod + min(redColorMod, 0.4f) * (noiseVal - 0.5f) * 0.5f; //up to ~5 pixel variation: 0.4 * 0.5 = 0.2; 0.2 * 25px = 5px
	#endif
		//0.092 ~= 23.5 / 255; reduced to 0.09 to be safe
	//bestCol.r = bestCol.r + (clamp(redColorMod, 0, 1 - actualDepthOfPixel(bestCol)) * 0.09f); //(floor(clamp(redColorMod, 0, 1) * 24.9f) / 255.0f);
	float currDepth = (bestCol.r * 255 - 1) % 30;
	float depthShift = max(redColorMod, 0) * 30;
	bestCol.r = bestCol.r + clamp(depthShift, -currDepth, 29 - currDepth) * 0.0039f; //~= 1 / 255

	return bestCol;
}
ENDCG
				
			}
		} 
	}
}
