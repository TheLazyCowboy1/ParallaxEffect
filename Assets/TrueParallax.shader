//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/TrueParallax"
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
			Bind "Color", color 
		}

		SubShader   
		{	

			GrabPass { "_ParallaxGrabTex" } //GrabPass to get screen data so that I can then warp it

			Pass 
			{
				
CGPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

sampler2D _LevelTex;
Texture2D<float4> _PreLevelColorGrab;
Texture2D<float4> _SlopedTerrainMask;

RWTexture2D<float> _LZC_LevelTex : register(u1);

uniform float4 _spriteRect;
uniform float2 _screenSize;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
	float2  suv : TEXCOORD1;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
	o.suv = o.uv * _screenSize;
    return o;
}

inline uint min(uint a, uint b) {
	return (a < b) ? a : b;
}
inline uint depthOfPixel(half r) {
	return (r < 0.997) ? ((uint)round(r * 255) - 1) % 30 : 30;
}
inline half terrainColor(int2 pos) {
	return 2 - 3 * _SlopedTerrainMask.Load(int3(pos, 0)).r;
}

void frag (v2f i)
{
		//map screen pos to level tex coord
	float2 textCoord = i.uv;
	textCoord.x -= _spriteRect.x;
	textCoord.y -= _spriteRect.y;
	textCoord.x /= _spriteRect.z - _spriteRect.x;
	textCoord.y /= _spriteRect.w - _spriteRect.y;

	half lev = tex2D(_LevelTex, textCoord).r;

	int2 checkPos = int2(round(i.suv));

	uint d = min(depthOfPixel(lev), round(terrainColor(checkPos) * 30));

		//check creature mask if applicable
	if (d > 5) {
		half4 c = _PreLevelColorGrab.Load(int3(checkPos, 0));
		if (c.r > 1.0f / 255.0f || c.g > 0 || c.b > 0) {
			d = 5;
		}
	}

	_LZC_LevelTex[checkPos] = d / 255.0f;

}
ENDCG
				
			}
		
			Pass 
			{
				
CGPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag

#pragma multi_compile _ THELAZYCOWBOY1_DEPTHCURVE
#pragma multi_compile _ THELAZYCOWBOY1_INVDEPTHCURVE
#pragma multi_compile _ THELAZYCOWBOY1_CLOSESTPIXELONLY
#pragma multi_compile _ THELAZYCOWBOY1_DYNAMICOPTIMIZATION
#pragma multi_compile _ THELAZYCOWBOY1_PROCESSLAYER2
#pragma multi_compile _ THELAZYCOWBOY1_BACKGROUNDNOISE

#include "UnityCG.cginc"

sampler2D _MainTex;
uniform float4 _MainTex_TexelSize;

#if defined(SHADER_API_PSSL)
Texture2D<float4> _ParallaxGrabTex;
#else
Texture2D<float4> _ParallaxGrabTex : register(t0);
#endif
uniform float2 _ParallaxGrabTex_TexelSize;

sampler2D _NoiseTex;
uniform float4 _NoiseTex_TexelSize;

RWTexture2D<float> _LZC_LevelTex : register(u1);
//uniform float2 _LZC_LevelTex_TexelSize; //DOES NOT WORK for RWTexture2D
uniform float2 _screenSize;

uniform float4 _spriteRect;

#if THELAZYCOWBOY1_PROCESSLAYER2
sampler2D _TheLazyCowboy1_Layer2Tex;
#endif

uniform float TheLazyCowboy1_Warp;
uniform float TheLazyCowboy1_MaxWarp;
uniform float2 TheLazyCowboy1_CamPos;
uniform uint TheLazyCowboy1_TestNum;
uniform float TheLazyCowboy1_StepSize;
uniform float TheLazyCowboy1_NegativeWarp;
uniform float TheLazyCowboy1_Layer30Depth;
uniform float TheLazyCowboy1_BackgroundDepth;
uniform float TheLazyCowboy1_BackgroundScale;
uniform float TheLazyCowboy1_AntiAliasingFac;
uniform float TheLazyCowboy1_BackgroundNoise;
#if THELAZYCOWBOY1_CLOSESTPIXELONLY
uniform float TheLazyCowboy1_MaxXDistance;
#endif

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
	float2  nuv : TEXCOORD1;
	float2  suv : TEXCOORD2;
	float2  camPos : TEXCOORD3;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
	o.nuv = o.uv * float2(10.667f, 6);
	o.suv = o.uv * _screenSize;
	o.camPos = lerp(float2(0.5f,0.5f), o.uv, TheLazyCowboy1_BackgroundScale);
    return o;
}

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

inline float highFreqNoise(float2 uv, float2 scale) {
	float2 nuv = frac(uv * scale);
	float2 rawLerpFac = 2 * (nuv - float2(0.5f, 0.5f));
	rawLerpFac = rawLerpFac * rawLerpFac; //^2 (also abs)
	float lerpFac = max(rawLerpFac.x, rawLerpFac.y);
	lerpFac = lerpFac * lerpFac; //^4
	lerpFac = lerpFac * lerpFac; //^6
	lerpFac = lerpFac * lerpFac; //^8
	lerpFac = lerpFac * lerpFac; //^10
	float n1 = tex2Dlod(_NoiseTex, float4(nuv, 0, 0)).x;
	float n2 = tex2Dlod(_NoiseTex, float4(nuv + _NoiseTex_TexelSize.xy * 2 * (scale + float2(1,1)), 0, 0)).x; //10 pixel buffer when scale=5
	return lerp(n1, n2, lerpFac * 0.5f);
}


half4 frag (v2f i) : SV_Target
{

	//float2 posCamDiff = i.uv*TheLazyCowboy1_BackgroundScale + float2(0.5f,0.5f)*(1-TheLazyCowboy1_BackgroundScale) - TheLazyCowboy1_CamPos //pixel position, adjusted via BackgroundScale
	float2 posCamDiff = i.camPos - TheLazyCowboy1_CamPos;

	//OPTIMIZATION

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	float2 moveStep = posCamDiff;
	float invWarpFac = min(
			TheLazyCowboy1_MaxWarp / max(abs(moveStep.x), abs(moveStep.y)),
			0.5f * TheLazyCowboy1_TestNum); //can't be less than 2 totalTests
#else
	float2 moveStep = float2(
		clamp(posCamDiff.x, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp), //clamp it to maxWarp
		clamp(posCamDiff.y, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp)
		);
#endif

		//scale moveStep back up to its proper size
	moveStep = moveStep * TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize
		* _screenSize / _screenSize.x; //adjust moveStep to respect the fact that the screen ratio is 16:9, not 1:1
	float2 unoptimizedMoveStep = moveStep;

	uint totalTests = TheLazyCowboy1_TestNum;
	float stepSize = TheLazyCowboy1_StepSize;

		//Do this before scaling values! If it's done afterward, then there will be noticeable lines where totalTests change
	half noiseVal = tex2D(_NoiseTex, i.nuv).x;
	half noiseOffset = TheLazyCowboy1_AntiAliasingFac * clamp(noiseVal - 0.3f, 0, 0.4f); //up to 0.4 step offset

	float2 initGrabPos = i.suv - moveStep * (totalTests + noiseOffset); //start at the END and then move BACKWARDS

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	if (invWarpFac > 1) {
		stepSize = stepSize * invWarpFac;
		moveStep = moveStep * invWarpFac;
		totalTests = ceil(totalTests / invWarpFac);
	}
#endif

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	half maxXDist = max(TheLazyCowboy1_MaxXDistance, stepSize);
#endif
#if THELAZYCOWBOY1_BACKGROUNDNOISE
	half bestXDist = 1;
	half bestDep = 1;
#endif

	int2 bestGrabPos = int2(round(i.suv));
	//uint bestLayer = 0;

	float percentage = 0.0002f; //very tiny margin of error, just in case there's some weird imprecision error
	float2 grabPos = initGrabPos + float2(0.5f, 0.5f); //adjust coords slightly so that int2(round(grabPos)) becomes int2(grabPos)

	uint c = 0;
	[loop]
	while(c <= totalTests) {

		int2 checkPos = int2(grabPos);

		half currDepth = _LZC_LevelTex[checkPos] * 255.0f / 30.0f;
		half newDepth = currDepth >= 1
			? TheLazyCowboy1_BackgroundDepth
			: depthCurve(currDepth) * TheLazyCowboy1_Layer30Depth;
		half xDistance = percentage - newDepth;

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
		//OBVIOUSLY HAS SOME EXTRA LOGIC
		if (xDistance >= 0) {
			if (xDistance < maxXDist) {
				bestGrabPos = checkPos;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
				bestXDist = xDistance;
				bestDep = newDepth;
	#endif
				break; //we found it! don't run any more code, ideally
			}
		}
		else {
			bestGrabPos = checkPos;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
			bestXDist = min(bestXDist, -xDistance);
			bestDep = newDepth;
	#endif
		}
#else
		//OBVIOUSLY WAY SIMPLER
		if (xDistance >= 0) {
			bestGrabPos = checkPos;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
			bestXDist = xDistance;
			bestDep = newDepth;
	#endif
			break;
		}
#endif

		grabPos = grabPos + moveStep;
		percentage = percentage + stepSize;
		c = c + 1;
	}


//APPLY FINAL NOISE

	half4 finalCol = _ParallaxGrabTex.Load(int3(bestGrabPos, 0));

#if !THELAZYCOWBOY1_BACKGROUNDNOISE
	return finalCol;
#else

	if (bestXDist <= stepSize || bestDep >= 1) { //it's close enough; don't add noise. Also don't add noise to the sky
		return finalCol;
	}

	float2 noisePoint;
#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	if (c > totalTests) { //loop did NOT break
		noisePoint = (initGrabPos //start at starting pos
			+ unoptimizedMoveStep * TheLazyCowboy1_TestNum * bestDep) //go "bestDep" of the way towards the ending pos
			/ _screenSize; //convert from texel coordinates to uv
	}
	else
#endif
		//logic if the loop DID break. This is always used if we're not using CLOSESTPIXELONLY
	noisePoint = (bestGrabPos //start at grabPos
		+ float2(bestXDist, bestXDist) * TheLazyCowboy1_Warp*0.5f) //fixed offset based on bestXDist and Warp; *0.5f because I think it'll look better
		/ _screenSize; //convert from texel coordinates to uv

	noiseVal = highFreqNoise(noisePoint - _spriteRect.xy, float2(5.333f, 3)); //subtract spriteRect.xy so that noise doesn't appear to move when the screen is moving

	half curBrightness = finalCol.r * 0.299f + finalCol.g * 0.587f + finalCol.b * 0.114f;
	half add = bestXDist * TheLazyCowboy1_BackgroundNoise * (noiseVal - 0.5h) * (curBrightness + 0.3h); //more noise if pixel is already brighter
	finalCol.x += add;
	finalCol.y += add;
	finalCol.z += add;
	return finalCol;
#endif

}
ENDCG
				
			}
		} 
	}
}
