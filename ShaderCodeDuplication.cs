using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace DEPRECATED_STUFF;

internal class DEPRECATED_STUFF
{
    /*private Color tex2D(Texture2D _LevelTex, float2 textCoord) => _LevelTex.GetPixel((int)(textCoord.x * 1400f), (int)(textCoord.y * 800f));

    private Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera self, Vector2 coord)
    {
        float3 warp = LazyCowboyParallax(self.levelTexture, new(coord.x / 1400f, coord.y / 800f));

        Color ret = orig(self, new(warp.x * 1400f, warp.y * 800f));
        ret.r += warp.z / 255f;
        return ret;
    }

    private bool? RoomCamera_LitAtCoordinate(On.RoomCamera.orig_LitAtCoordinate orig, RoomCamera self, Vector2 coord)
    {
        float3 warp = LazyCowboyParallax(self.levelTexture, new(coord.x / 1400f, coord.y / 800f));

        return orig(self, new(warp.x * 1400f, warp.y * 800f));
    }

    private float RoomCamera_DepthAtCoordinate(On.RoomCamera.orig_DepthAtCoordinate orig, RoomCamera self, Vector2 coord)
    {
        float3 warp = LazyCowboyParallax(self.levelTexture, new(coord.x / 1400f, coord.y / 800f));

        return orig(self, new(warp.x * 1400f, warp.y * 800f)) + warp.z / 30f;
    }

    float depthOfPixel(Color col)
    {
        if (col.r != 1.0f || col.g != 1.0f || col.b != 1.0f)
        {
            return ((float)(((int)((((uint)(col.r * 255.0f)) - 1) % 30)) - 5)) * 0.04f;
        }
        return 1.0f;
    }
    float sinSmoothCurve(float diff)
    {
        switch (Options.SmoothingType.Value)
        {
            case "SINESMOOTHING":
                return Mathf.Sin(diff * 1.570796f);
            case "INVSINESMOOTHING":
                return diff + diff - Mathf.Sin(diff * 1.570796f);
        }
        return diff;
    }

    float3 LazyCowboyParallax(Texture2D _LevelTex, float2 textCoord)
    {
        Color origCol = tex2D(_LevelTex, textCoord);
        float posCamXDiff = sinSmoothCurve(textCoord.x - CamPos.x);
        float posCamYDiff = sinSmoothCurve(textCoord.y - CamPos.y);

        int testNum = (int)Mathf.Ceil(Options.MaxWarp.Value / Options.Optimization.Value);

        float stepMod = 1.0f / (float)testNum;
        float clampedXWarp = Mathf.Clamp(Options.Warp.Value * posCamXDiff, -Options.MaxWarp.Value, Options.MaxWarp.Value) / 1400f;
        float clampedYWarp = Mathf.Clamp(Options.Warp.Value * posCamYDiff, -Options.MaxWarp.Value, Options.MaxWarp.Value) / 800f;
        float xStep = stepMod * clampedXWarp;
        float yStep = stepMod * clampedYWarp;

        float oldDepth = depthOfPixel(origCol);

        float warpX = oldDepth * clampedXWarp;
        float warpY = oldDepth * clampedYWarp;

        //fixed4 bestCol = col;
        //fixed2 bestGrabOff = fixed2(0, 0);
        //calculate values for initial warp
        float2 bestGrabOff = new(warpX, warpY);
        float currentDepth = depthOfPixel(tex2D(_LevelTex, textCoord + bestGrabOff));
        float bestScore = currentDepth - oldDepth - 1.0f;
        float redColorMod = oldDepth - currentDepth;

        float2 grabOff = new(0, 0);
        uint counter = 0;

        while (counter < testNum)
        {
            counter = counter + 1;

            grabOff.x = grabOff.x + xStep;
            //if (textCoord.x > 1.0f || textCoord.x < 0.0f) return bestCol; //too much processing; too little benefit
            grabOff.y = grabOff.y + yStep;
            //if (textCoord.y > 1.0f || textCoord.y < 0.0f) return bestCol;
            Color newCol = tex2D(_LevelTex, textCoord + grabOff);
            float newDepth = depthOfPixel(newCol);
            if (newDepth < oldDepth)
            {
                //float score = newDepth + (float)counter * stepMod;
                float score = (newDepth - oldDepth) + (float)counter * stepMod;
                if (score < bestScore)
                {
                    bestScore = score;
                    //bestCol = newCol;
                    bestGrabOff = grabOff;

                    redColorMod = (oldDepth - newDepth) * (float)counter * stepMod;
                }
            }
        }
        //return bestCol;
        //return tex2D(_MainTex, bestGrabPos);
        textCoord = textCoord + bestGrabOff;

        //half4 levelCol = tex2D(_LevelTex, textCoord);
        //levelCol.r = levelCol.r + (half)((float)redColorMod / 255.0f);

        return new float3(textCoord.x, textCoord.y, Mathf.Floor(redColorMod * 25f));
    }*/
}
