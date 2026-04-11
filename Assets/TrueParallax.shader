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

#pragma multi_compile _ THELAZYCOWBOY1_PROCESSLAYER2
#pragma multi_compile _ THELAZYCOWBOY1_CLOSESTPIXELONLY

#define CreatureSteps 6

#include "UnityCG.cginc"

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

sampler2D _LevelTex;
Texture2D<float4> _PreLevelColorGrab;
Texture2D<float4> _SlopedTerrainMask;

#if THELAZYCOWBOY1_PROCESSLAYER2
sampler2D _TheLazyCowboy1_Layer2Tex;
#endif

#if THELAZYCOWBOY1_PROCESSLAYER2
RWTexture2D<float4> _LZC_LevelTex : register(u1);
#else
RWTexture2D<float> _LZC_LevelTex : register(u1);
#endif

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
inline uint depthOfPixel(float r) {
	return (r < 0.997) ? ((uint)round(r * 255) - 1) % 30 : 30;
}
inline uint terrainDep(int2 pos) {
	return round(30 * (2 - 3 * _SlopedTerrainMask.Load(int3(pos, 0)).r));
}

void frag (v2f i)
{
		//map screen pos to level tex coord
	float2 textCoord = (i.uv - _spriteRect.xy) / (_spriteRect.zw - _spriteRect.xy);
	float lev = tex2D(_LevelTex, textCoord).r;

	int2 checkPos = int2(round(i.suv));

	uint ld = depthOfPixel(lev);
	uint d = ld;

	bool terrainMask = false;
	uint td = terrainDep(checkPos);
	if (td < ld) {
		d = td;
		terrainMask = true;
	}

		//check creature mask if applicable
	bool creatureMask = false;
	if (d > 5) {
		float4 c = _PreLevelColorGrab.Load(int3(checkPos, 0));
		if (c.r > 1.0f / 255.0f || c.g > 0 || c.b > 0) {
			d = 5;
			creatureMask = true;
		}
	}

#if THELAZYCOWBOY1_PROCESSLAYER2
	float g, b, a;
	if (creatureMask) {

			//TODO: ADD CODE TO TEST HOW BIG THE CREATURE IS AND MAKE THICKNESS ACCORDINGLY

		g = 1 / 255.0f; //thickness = 1
		b = 0; //layer2 = idk so just don't use it ig
		a = 0; //distance = 0
	}
	else if (terrainMask) {
		g = 31 / 255.0f; //thickness = 31
		b = 0; //layer2 = not applicable
		a = 0; //distance = 0
	}
	else {
			//read it from background tex, but shift the bits around to pack 'em all in
		float4 backCol = tex2D(_TheLazyCowboy1_Layer2Tex, textCoord);
		uint4 backColInts = uint4(round(backCol * 255));
		g = (backColInts.y | ((backColInts.x & 7) << 5)) / 255.0f;
		b = (backColInts.z | ((backColInts.x & 24) << 3)) / 255.0f;
		a = backCol.w;
	}
	//layer1dep = 5, layer1thick = 5, layer2dep = 5, lDist = 5, rDist = 5, dir = 3;   total = 28
	//r = layer1dep
	//g = layer1thick, rDist(3)
	//b = layer2dep, rDist(2)
	//a = lDist, dir

	//layer2Tex: r = rDist, g = layer1thick, b = layer2dep, a = lDist, dir

	_LZC_LevelTex[checkPos] = float4(d / 255.0f, g, b, a);

#else
	_LZC_LevelTex[checkPos] = d / 255.0f;
#endif

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

sampler2D _NoiseTex;
uniform float4 _NoiseTex_TexelSize;

#if THELAZYCOWBOY1_PROCESSLAYER2
RWTexture2D<float4> _LZC_LevelTex : register(u1);
#else
RWTexture2D<float> _LZC_LevelTex : register(u1);
#endif
//uniform float2 _LZC_LevelTex_TexelSize; //DOES NOT WORK for RWTexture2D
uniform float2 _screenSize;
uniform float4 _spriteRect;

#if THELAZYCOWBOY1_PROCESSLAYER2
Texture2D<float4> _PreLevelColorGrab;
#endif

uniform float2 TheLazyCowboy1_CamPos;
uniform float TheLazyCowboy1_BackgroundScale;
uniform float TheLazyCowboy1_Warp;
uniform float TheLazyCowboy1_MaxWarp;
uniform uint TheLazyCowboy1_TestNum;
uniform float TheLazyCowboy1_StepSize;
uniform float TheLazyCowboy1_PivotDepth;
uniform float TheLazyCowboy1_Layer30Depth;
uniform float TheLazyCowboy1_AntiAliasingFac;
#if THELAZYCOWBOY1_BACKGROUNDNOISE
uniform float TheLazyCowboy1_BackgroundNoise;
#endif
#if THELAZYCOWBOY1_CLOSESTPIXELONLY
uniform float TheLazyCowboy1_MaxXDistance;
#endif

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
	float2  nuv : TEXCOORD1;
	float2  suv : TEXCOORD2;
	float2  posCamDiff : TEXCOORD3;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
	o.nuv = o.uv * float2(10.667f, 6);
	o.suv = o.uv * _screenSize;
	o.posCamDiff = lerp(float2(0.5f,0.5f), o.uv, TheLazyCowboy1_BackgroundScale) - TheLazyCowboy1_CamPos;
    return o;
}

inline float depthCurve(float d) {
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

	//OPTIMIZATION

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	float2 moveStep = i.posCamDiff;
	float invWarpFac = min(
			TheLazyCowboy1_MaxWarp / max(abs(moveStep.x), abs(moveStep.y)),
			0.5f * TheLazyCowboy1_TestNum); //can't be less than 2 totalTests
#else
	float2 moveStep = float2(
		clamp(i.posCamDiff.x, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp), //clamp it to maxWarp
		clamp(i.posCamDiff.y, -TheLazyCowboy1_MaxWarp, TheLazyCowboy1_MaxWarp)
		);
#endif

		//scale moveStep up to its proper size
	moveStep = moveStep * TheLazyCowboy1_Warp * TheLazyCowboy1_StepSize
		* _screenSize / _screenSize.x; //adjust moveStep to respect the fact that the screen ratio is 16:9, not 1:1
	float2 unoptimizedMoveStep = moveStep;

	uint totalTests = TheLazyCowboy1_TestNum;
	float stepSize = TheLazyCowboy1_StepSize;

		//Do this before scaling values! If it's done afterward, then there will be noticeable lines where totalTests change
	float noiseVal = tex2D(_NoiseTex, i.nuv).x;
	float noiseOffset = TheLazyCowboy1_AntiAliasingFac * (noiseVal - 0.2f);// * saturate((noiseVal - 0.3f) * 2);

	float2 initGrabPos = i.suv - moveStep * (totalTests + noiseOffset) * TheLazyCowboy1_PivotDepth; //start at the END and then move BACKWARDS

#if THELAZYCOWBOY1_DYNAMICOPTIMIZATION
	if (invWarpFac > 1) {
		stepSize = stepSize * invWarpFac;
		moveStep = moveStep * invWarpFac;
		totalTests = ceil(totalTests / invWarpFac);
	}
#endif

#if THELAZYCOWBOY1_CLOSESTPIXELONLY
	float maxXDist = max(TheLazyCowboy1_MaxXDistance, stepSize);
#endif
#if THELAZYCOWBOY1_BACKGROUNDNOISE
	float bestXDist = 1;
	float bestDep = 1;
#endif
#if THELAZYCOWBOY1_PROCESSLAYER2
	uint bestLayer = 1;
#endif

	int2 bestGrabPos = int2(round(i.suv));

	float percentage = 0.0002f; //very tiny margin of error, just in case there's some weird imprecision error
	float2 grabPos = initGrabPos + float2(0.5f, 0.5f); //adjust coords slightly so that int2(round(grabPos)) becomes int2(grabPos)

	uint c = 0;
	[loop]
	while(c <= totalTests) {

			//CALCULATE XDISTANCE

		int2 checkPos = int2(grabPos);
#if THELAZYCOWBOY1_PROCESSLAYER2
		float4 lev = _LZC_LevelTex[checkPos];
		float currDepth = lev.r * 255.0f / 30.0f;
#else
		float currDepth = _LZC_LevelTex[checkPos].x * 255.0f / 30.0f;
#endif
		float newDepth = currDepth >= 1
			? 1
			: depthCurve(currDepth) * TheLazyCowboy1_Layer30Depth;
		float xDistance = percentage - newDepth;


			//CHECK XDISTANCE

#if THELAZYCOWBOY1_PROCESSLAYER2 && THELAZYCOWBOY1_CLOSESTPIXELONLY
		if (xDistance >= 0) {
			float thickness = (uint(lev.y * 255) & 31) / 30.0f;
			if (xDistance < thickness) {
				bestGrabPos = checkPos;
				bestLayer = 1;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
				bestXDist = xDistance;
				bestDep = newDepth;
	#endif
				break;
			}
			uint l2DepInt = uint(lev.z * 255) & 31;
			if (l2DepInt > 0) { //0 means that there is no layer2 for this texel
				float l2Dep = l2DepInt >= 30
					? 1
					: depthCurve(l2DepInt / 30.0f) * TheLazyCowboy1_Layer30Depth;
				xDistance = percentage - l2Dep;
				if (xDistance >= 0) {
					if (xDistance < maxXDist) {
						bestGrabPos = checkPos;
						bestLayer = 2;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
						bestXDist = xDistance;
						bestDep = l2dep;
	#endif
						break;
					}
				}
				else {
					bestGrabPos = checkPos;
					bestLayer = 2;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
					bestXDist = min(bestXDist, -xDistance);
					bestDep = l2dep;
	#endif
				}
			}
		}
		else {
			bestGrabPos = checkPos;
			bestLayer = 1;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
			bestXDist = min(bestXDist, -xDistance);
			bestDep = newDepth;
	#endif
		}

#elif THELAZYCOWBOY1_CLOSESTPIXELONLY
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
#elif THELAZYCOWBOY1_PROCESSLAYER2
		if (xDistance >= 0) {
			float thickness = (uint(lev.y * 255) & 31) / 30.0f;
			if (xDistance < thickness) {
				bestGrabPos = checkPos;
				//bestLayer = 1; //in this implementation, bestLayer should always be 1 here anyway
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
				bestXDist = xDistance;
				bestDep = newDepth;
	#endif
				break;
			}
			float l2Dep = (uint(lev.z * 255) & 31) / 30.0f;
			l2Dep = l2Dep >= 1
				? 1
				: depthCurve(l2Dep) * TheLazyCowboy1_Layer30Depth;
			xDistance = percentage - l2Dep;
			if (l2Dep > 0 && xDistance >= 0) {
				bestGrabPos = checkPos;
				bestLayer = 2;
	#if THELAZYCOWBOY1_BACKGROUNDNOISE
				bestXDist = xDistance;
				bestDep = l2dep;
	#endif
				break;
			}
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

#if THELAZYCOWBOY1_PROCESSLAYER2
	half4 finalCol;
	if (bestLayer > 1) {
		int4 lev = int4(_LZC_LevelTex.Load(int3(bestGrabPos, 0)) * 255);
		int d = lev.w >> 5;
		int lDist = lev.w & 31;//0b11111;
		int rDist = (lev.y >> 5) | ((lev.z & 224) << 2); //224 = 0b11100000

		int2 dir[8];
		dir[0] = int2(2, 0);
		dir[1] = int2(0, 2);
		dir[2] = int2(2, 2);
		dir[3] = int2(2, -2); //(equivalent to -1,1)
		dir[4] = int2(2, 1); //1, 0.5
		dir[5] = int2(1, 2); //0.5, 1
		dir[6] = int2(-1, 2); //-0.5, 1
		dir[7] = int2(2, -1); //1, -0.5

		int2 lPos = bestGrabPos + (dir[d] * lDist)/2;
		int2 rPos = bestGrabPos - (dir[d] * rDist)/2;
		float4 lCritCol = _PreLevelColorGrab.Load(int3(lPos, 0));
		float4 rCritCol = _PreLevelColorGrab.Load(int3(rPos, 0));
		bool lCrit = lCritCol.r > 1.0f / 255.0f || lCritCol.g > 0 || lCritCol.b > 0;
		bool rCrit = rCritCol.r > 1.0f / 255.0f || rCritCol.g > 0 || rCritCol.b > 0;
		if (lCrit == rCrit) { //either there's no creatures, or there's creatures on both sides; either way, interpolate the two sides
			float4 lCol = _ParallaxGrabTex.Load(int3(lPos, 0));
			float4 rCol = _ParallaxGrabTex.Load(int3(rPos, 0));
			finalCol = lerp(lCol, rCol, lDist / float(lDist + rDist));
		}
		else {
			int dist3 = 0; //for the noise stuff below
			if (lCrit) { //left is creature
				finalCol = _ParallaxGrabTex.Load(int3(rPos, 0)); //so just use right side
				dist3 = rDist;
			}
			else { //right is creature
				finalCol = _ParallaxGrabTex.Load(int3(lPos, 0)); //so just use left side
				dist3 = lDist;
			}
		#if THELAZYCOWBOY1_BACKGROUNDNOISE && THELAZYCOWBOY1_CLOSESTPIXELONLY
			if (bestXDist <= stepSize) { //change how the noise is applied
				bestXDist = dist3 / TheLazyCowboy1_Warp; //noise severity = how far away from pixel
				c = 0; //act like it's CLOSESTPIXELONLY
			}
		#endif
		}

	}
	else {
		finalCol = _ParallaxGrabTex.Load(int3(bestGrabPos, 0));
	}

#else
	half4 finalCol = _ParallaxGrabTex.Load(int3(bestGrabPos, 0));
#endif

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

	float noiseVal2 = highFreqNoise(noisePoint - _spriteRect.xy, float2(5.333f, 3)); //subtract spriteRect.xy so that noise doesn't appear to move when the screen is moving

	float curBrightness = finalCol.r * 0.299f + finalCol.g * 0.587f + finalCol.b * 0.114f;
	half add = bestXDist * TheLazyCowboy1_BackgroundNoise * (noiseVal2 - 0.5f) * (curBrightness + 0.3f); //more noise if pixel is already brighter
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