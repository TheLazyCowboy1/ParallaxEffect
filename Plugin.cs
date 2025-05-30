﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using Unity.Mathematics;
using RWCustom;
using Watcher;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ParallaxEffect;

[BepInDependency("SBCameraScroll", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.ParallaxEffect",
        MOD_NAME = "Parallax Effect",
        MOD_VERSION = "0.0.5";


    public static ConfigOptions Options;

    #region Setup
    public Plugin()
    {
        try
        {
            Options = new ConfigOptions();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }
    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorldOnOnModsInit;
        if (IsInit)
        {
            On.RoomCamera.ctor -= RoomCamera_ctor;
            On.RoomCamera.ApplyPositionChange -= RoomCamera_ApplyPositionChange;
            On.RoomCamera.GetCameraBestIndex -= RoomCamera_GetCameraBestIndex;

            On.RoomCamera.UpdateSnowLight -= RoomCamera_UpdateSnowLight;

            On.CustomDecal.DrawSprites -= CustomDecal_DrawSprites;
            On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
            On.BackgroundScene.DrawPos -= BackgroundScene_DrawPos;
            On.BackgroundScene.Update -= BackgroundScene_Update;
            On.Watcher.OuterRimView.DrawPos -= OuterRimView_DrawPos;
            On.Watcher.OuterRimView.Update -= OuterRimView_Update;
            //On.BackgroundScene.BackgroundSceneElement.DrawSprites -= BackgroundSceneElement_DrawSprites;
            On.BackgroundScene.Simple2DBackgroundIllustration.DrawSprites -= Simple2DBackgroundIllustration_DrawSprites;
            On.AboveCloudsView.CloseCloud.DrawSprites -= CloseCloud_DrawSprites;
            On.AboveCloudsView.DistantCloud.DrawSprites -= DistantCloud_DrawSprites;
            On.AboveCloudsView.FlyingCloud.DrawSprites -= FlyingCloud_DrawSprites;

            //On.Watcher.LevelTexCombiner.CreateBuffer -= LevelTexCombiner_CreateBuffer;

            IsInit = false;
        }
    }

    //public FShader parallaxFShader;
    public Shader ParallaxShader;
    public Material ParallaxMaterial;

    public int ShadCamPosX = -1, ShadCamPosY = -1;

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            On.RoomCamera.ctor += RoomCamera_ctor;
            On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
            On.RoomCamera.GetCameraBestIndex += RoomCamera_GetCameraBestIndex;

            On.RoomCamera.UpdateSnowLight += RoomCamera_UpdateSnowLight;

            On.CustomDecal.DrawSprites += CustomDecal_DrawSprites;
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.BackgroundScene.DrawPos += BackgroundScene_DrawPos;
            On.BackgroundScene.Update += BackgroundScene_Update;
            On.Watcher.OuterRimView.DrawPos += OuterRimView_DrawPos;
            On.Watcher.OuterRimView.Update += OuterRimView_Update;
            //On.BackgroundScene.BackgroundSceneElement.DrawSprites += BackgroundSceneElement_DrawSprites;
            On.BackgroundScene.Simple2DBackgroundIllustration.DrawSprites += Simple2DBackgroundIllustration_DrawSprites;
            On.AboveCloudsView.CloseCloud.DrawSprites += CloseCloud_DrawSprites;
            On.AboveCloudsView.DistantCloud.DrawSprites += DistantCloud_DrawSprites;
            On.AboveCloudsView.FlyingCloud.DrawSprites += FlyingCloud_DrawSprites;

            //On.Watcher.LevelTexCombiner.CreateBuffer += LevelTexCombiner_CreateBuffer;

            //load shader
            try
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles\\LazyCowboy\\ParallaxEffect.assets"));

                ParallaxShader = assetBundle.LoadAsset<Shader>("ParallaxEffect.shader");
                if (ParallaxShader == null)
                    Logger.LogError("Could not find shader ParallaxEffect.shader");
                ParallaxMaterial = new(ParallaxShader);

                ShadCamPosX = Shader.PropertyToID("TheLazyCowboy1_CamPosX");
                ShadCamPosY = Shader.PropertyToID("TheLazyCowboy1_CamPosY");
            }
            catch (Exception ex) { Logger.LogError(ex); }
            
            MachineConnector.SetRegisteredOI(MOD_ID, Options);
            IsInit = true;

            Logger.LogDebug("Applied hooks");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    #endregion

    #region CameraHooks

    public RenderTexture OrigSnowTexture;

    //Sets/calculates the shader constants
    private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
    {
        OrigSnowTexture?.Release();
        OrigSnowTexture = new(1400, 800, 0, DefaultFormat.LDR);

        //setup constants
        Shader.SetGlobalFloat("TheLazyCowboy1_Warp", Options.Warp.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarp", Options.MaxWarpFactor.Value);

        float startOffset = Mathf.Max(Options.StartOffset.Value, depthCurve(-0.2f)); //prevent unnecessary processing
        int testNum = Mathf.Max(2, (int)Mathf.Ceil(Mathf.Abs(Options.Warp.Value) * Options.MaxWarpFactor.Value * (Options.EndOffset.Value - startOffset) / Options.Optimization.Value));
        Shader.SetGlobalInt("TheLazyCowboy1_TestNum", testNum);
        Shader.SetGlobalFloat("TheLazyCowboy1_StepSize", (Options.EndOffset.Value - startOffset) / testNum);
        //Shader.SetGlobalFloat("TheLazyCowboy1_StepSize", Options.Optimization.Value);

        Shader.SetGlobalFloat("TheLazyCowboy1_StartOffset", startOffset);
        Shader.SetGlobalFloat("TheLazyCowboy1_RedModScale", Options.RedModScale.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_BackgroundScale", Options.BackgroundScale.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_AntiAliasingFac", Options.AntiAliasing.Value * 2.5f);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxXDistance", Options.MaxXDistance.Value);
        Shader.SetGlobalFloat(ShadCamPosX, 0.5f);
        Shader.SetGlobalFloat(ShadCamPosY, 0.5f);

        //keywords

        switch (Options.SmoothingType.Value)
        {
            case "EXTREME": //enables both options, just to save on file size
                Shader.EnableKeyword("THELAZYCOWBOY1_SINESMOOTHING");
                Shader.EnableKeyword("THELAZYCOWBOY1_INVSINESMOOTHING");
                break;
            case "SINUSOIDAL":
                Shader.EnableKeyword("THELAZYCOWBOY1_SINESMOOTHING");
                Shader.DisableKeyword("THELAZYCOWBOY1_INVSINESMOOTHING");
                break;
            case "INVERSE":
                Shader.DisableKeyword("THELAZYCOWBOY1_SINESMOOTHING");
                Shader.EnableKeyword("THELAZYCOWBOY1_INVSINESMOOTHING");
                break;
            default://case "FLAT":
                Shader.DisableKeyword("THELAZYCOWBOY1_SINESMOOTHING");
                Shader.DisableKeyword("THELAZYCOWBOY1_INVSINESMOOTHING");
                break;
        }

        switch (Options.DepthCurve.Value)
        {
            case "EXTREME":
                Shader.EnableKeyword("THELAZYCOWBOY1_DEPTHCURVE");
                Shader.DisableKeyword("THELAZYCOWBOY1_INVDEPTHCURVE");
                break;
            case "PARABOLIC": //this case enables BOTH options... to indicate a "compromise" or something...?
                Shader.EnableKeyword("THELAZYCOWBOY1_DEPTHCURVE");
                Shader.EnableKeyword("THELAZYCOWBOY1_INVDEPTHCURVE");
                break;
            case "INVERSE":
                Shader.DisableKeyword("THELAZYCOWBOY1_DEPTHCURVE");
                Shader.EnableKeyword("THELAZYCOWBOY1_INVDEPTHCURVE");
                break;
            default: //case "LINEAR":
                Shader.DisableKeyword("THELAZYCOWBOY1_DEPTHCURVE");
                Shader.DisableKeyword("THELAZYCOWBOY1_INVDEPTHCURVE");
                break;
        }

        if (Options.DynamicOptimization.Value && !Shader.IsKeywordEnabled("THELAZYCOWBOY1_DYNAMICOPTIMIZATION"))
            Shader.EnableKeyword("THELAZYCOWBOY1_DYNAMICOPTIMIZATION");
        else if (!Options.DynamicOptimization.Value && Shader.IsKeywordEnabled("THELAZYCOWBOY1_DYNAMICOPTIMIZATION"))
            Shader.DisableKeyword("THELAZYCOWBOY1_DYNAMICOPTIMIZATION");

        if (Options.NoCenterWarp.Value && !Shader.IsKeywordEnabled("THELAZYCOWBOY1_NOCENTERWARP"))
            Shader.EnableKeyword("THELAZYCOWBOY1_NOCENTERWARP");
        else if (!Options.NoCenterWarp.Value && Shader.IsKeywordEnabled("THELAZYCOWBOY1_NOCENTERWARP"))
            Shader.DisableKeyword("THELAZYCOWBOY1_NOCENTERWARP");

        if (Options.ClosestPixelOnly.Value && !Shader.IsKeywordEnabled("THELAZYCOWBOY1_CLOSESTPIXELONLY"))
            Shader.EnableKeyword("THELAZYCOWBOY1_CLOSESTPIXELONLY");
        else if (!Options.ClosestPixelOnly.Value && Shader.IsKeywordEnabled("THELAZYCOWBOY1_CLOSESTPIXELONLY"))
            Shader.DisableKeyword("THELAZYCOWBOY1_CLOSESTPIXELONLY");

        Logger.LogDebug("Setup shader constants");

        orig(self, game, cameraNumber);

        //FSprite shaderSprite = new("Futile_White");
        //shaderSprite.scaleX = 1400f;//self.sSize.x;
        //shaderSprite.scaleY = 800f;// self.sSize.y;
        //shaderSprite.shader = parallaxFShader;
        //self.ReturnFContainer("Foreground").AddChildAtIndex(shaderSprite, 1);
    }

    //Actually adds the shader to the LevelTexCombiner whenever the LevelTexCombiner gets cleared
    //ALSO attempts to resolution scale...
    private void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
    {
        Vector2 origSize = Vector2.zero; //say origSize was 0 if it didn't exist before
        if (self.levelTexCombiner.combinedLevelTex == null) //SBCameraScroll refuses to resize unless the texture already exists; a big shortcoming
            self.levelTexCombiner.Initialize();
        else
            origSize = new(self.levelTexCombiner.combinedLevelTex.width, self.levelTexCombiner.combinedLevelTex.height);

        orig(self);

        //Shader.SetGlobalTexture("TheLazyCowboy1_ScreenTexture", self.levelTexture);
        try
        {
            //Resolution Scale in a SBCameraScroll-friendly way
            var t = self.levelTexCombiner;
            if (Options.ResolutionScaleEnabled.Value && (t.combinedLevelTex.width != origSize.x || t.combinedLevelTex.height != origSize.y))
            {
                if (!t.isActive)
                    t.Initialize();
                t.combinedLevelTex.Release();
                t.combinedLevelTex = new RenderTexture(Mathf.RoundToInt(t.combinedLevelTex.width * Options.ResolutionScale.Value), Mathf.RoundToInt(t.combinedLevelTex.height * Options.ResolutionScale.Value), 0, DefaultFormat.LDR);
                t.combinedLevelTex.filterMode = 0;
                t.intermediateTex.Release();
                t.intermediateTex = new RenderTexture(Mathf.RoundToInt(t.intermediateTex.width * Options.ResolutionScale.Value), Mathf.RoundToInt(t.intermediateTex.height * Options.ResolutionScale.Value), 0, DefaultFormat.LDR);
                t.intermediateTex.filterMode = 0;

                Logger.LogDebug($"Upscaled level texture to {t.combinedLevelTex.width}x{t.combinedLevelTex.height}");
            }

            //t.AddPass(RenderTexture.GetTemporary(1400, 800), parallaxMaterial, parallaxShader.name, LevelTexCombiner.last);
            t.AddPass(ParallaxShader, ParallaxShader.name, LevelTexCombiner.last);
            //Logger.LogDebug($"Added {parallaxShader.name} shader pass!"); //happens every screen change; annoying log spam
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }


    //public static float2 CamPos;
    public static Dictionary<int, float2> CamPos = new(4);
    public static Dictionary<int, Vector2> MidpointWarp = new(4);

    //Sets the CamPos
    private void RoomCamera_GetCameraBestIndex(On.RoomCamera.orig_GetCameraBestIndex orig, RoomCamera self)
    {
        orig(self);

        Vector2 pos = new(0.5f, 0.5f);

        if (!CamPos.ContainsKey(self.cameraNumber))
            CamPos.Add(self.cameraNumber, new (0.5f, 0.5f));

        //Follow creatures
        var crit = self.followAbstractCreature?.realizedCreature;
        if (!Options.AlwaysCentered.Value && crit != null)
        {
            Vector2? critPos = (crit.inShortcut ? self.game.shortcuts.OnScreenPositionOfInShortCutCreature(self.room, crit) : crit.mainBodyChunk.pos);
            if (critPos != null)
            {
                //Vector2 localPos = (critPos.Value - self.CamPos(self.currentCameraPosition)
                //Vector2 localPos = (critPos.Value - self.levelGraphic.GetPosition()
                pos = (critPos.Value - self.pos
                    + (self.followCreatureInputForward + self.leanPos) * 2f)
                    / self.sSize;
            }
        }

        //Mouse movement
        if (Options.MouseSensitivity.Value > 0)
        {
            try
            {
                float mouseX = Options.MouseSensitivity.Value * Input.GetAxis("Mouse X") * 0.25f;
                if (mouseX != 0f)
                {
                    float strength = Mathf.Clamp01(Mathf.Abs(mouseX));
                    //mouseX = 0.5f + 0.5f * Mathf.Clamp(mouseX, -1f, 1f);
                    pos.x += strength * ((mouseX > 0 ? 1f : 0f) - pos.x);
                }

                float mouseY = Options.MouseSensitivity.Value * Input.GetAxis("Mouse Y") * 0.25f;// * 0.5625f; //0.5625 = 9/16 
                if (mouseY != 0f)
                {
                    float strength = Mathf.Clamp01(Mathf.Abs(mouseY));
                    //mouseY = 0.5f + 0.5f * Mathf.Clamp(mouseY, -1f, 1f);
                    pos.y += strength * ((mouseY > 0 ? 1f : 0f) - pos.y);
                }
            }
            catch { }
        }

        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);

        //Actually change camera position
        CamPos[self.cameraNumber] += Options.CameraMoveSpeed.Value * (new float2(pos.x, pos.y) - CamPos[self.cameraNumber]);
        if (Options.InvertPos.Value)
        {
            Shader.SetGlobalFloat(ShadCamPosX, 1f - CamPos[self.cameraNumber].x);
            Shader.SetGlobalFloat(ShadCamPosY, 1f - CamPos[self.cameraNumber].y);
        }
        else
        {
            Shader.SetGlobalFloat(ShadCamPosX, CamPos[self.cameraNumber].x);
            Shader.SetGlobalFloat(ShadCamPosY, CamPos[self.cameraNumber].y);
        }


        MidpointWarp.Remove(self.cameraNumber); //cam pos changed, so midpoint needs to be recalculated if it's there
    }
    private Vector2 GetMidpointWarp(int cameraNumber)
    {
        if (!MidpointWarp.ContainsKey(cameraNumber)) MidpointWarp.Add(cameraNumber, CalculateWarp(new(0.5f, 0.5f), CamPos[cameraNumber]));
        return MidpointWarp[cameraNumber];
    }


    //Resolution Scaling
    [Obsolete]
    private void LevelTexCombiner_CreateBuffer(On.Watcher.LevelTexCombiner.orig_CreateBuffer orig, LevelTexCombiner self, string id, RenderTargetIdentifier texture, Material material, CameraEvent evt)
    {
        try
        {
            if (Options.ResolutionScaleEnabled.Value && (int)evt == 10)
            {
                var source = Custom.rainWorld.persistentData.cameraTextures[0, 0];

                self.combinedLevelTex = new RenderTexture(Mathf.RoundToInt(source.width * Options.ResolutionScale.Value), Mathf.RoundToInt(source.height * Options.ResolutionScale.Value), 0, DefaultFormat.LDR);
                self.combinedLevelTex.filterMode = 0;
                self.intermediateTex = new RenderTexture(Mathf.RoundToInt(source.width * Options.ResolutionScale.Value), Mathf.RoundToInt(source.height * Options.ResolutionScale.Value), 0, DefaultFormat.LDR);
                self.intermediateTex.filterMode = 0;
                Shader.SetGlobalTexture("_LevelTex", self.combinedLevelTex);
            }

            orig(self, id, texture, material, evt);
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }


    //Forces the snow to use the combined level texture
    private void RoomCamera_UpdateSnowLight(On.RoomCamera.orig_UpdateSnowLight orig, RoomCamera self)
    {
        /*if (self.levelTexCombiner.isActive)
        {
            Shader.DisableKeyword("COMBINEDLEVEL");
            orig(self);
            Shader.EnableKeyword("COMBINEDLEVEL");
        }
        else orig(self);*/
        orig(self);

        //Save the new snow texture, so I don't accidentally overwrite it...
        //OrigSnowTexture?.Release();
        //OrigSnowTexture = new(self.SnowTexture);
        //OrigSnowTexture.Create();
        //Logger.LogDebug($"OrigSnowTexture: {OrigSnowTexture.width}x{OrigSnowTexture.height}");
        if (Options.WarpSnow.Value)
            Graphics.Blit(self.SnowTexture, OrigSnowTexture);
    }

    #endregion

    #region WarpCalc
    //Warps decals placed through dev tools
    private void CustomDecal_DrawSprites(On.CustomDecal.orig_DrawSprites orig, CustomDecal self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        try
        {
            if (Options.WarpDecals.Value && CamPos.TryGetValue(rCam.cameraNumber, out float2 pos))
            {
                for (int i = 0; i < self.verts.Length; i++)
                {
                    Vector2 localVert = self.verts[i] - camPos;
                    if (localVert.x >= 0 && localVert.x < rCam.sSize.x
                        && localVert.y >= 0 && localVert.y < rCam.sSize.y) //simple bounds check
                    {
                        var data = self.placedObject.data as PlacedObject.CustomDecalData; //use flat data instead of image depth?
                        //Vector2 warp = Options.Warp.Value * depthCurve(rCam.DepthAtCoordinate(self.verts[i]) * 1.2f - 0.2f)
                        //use the decal's depth (weighted towards the deeper part) to determine a warp factor
                        Vector2 warp = depthCurve((0.3f * data.fromDepth + 0.7f * data.toDepth - 5f) * 0.04f) * CalculateWarp(localVert / rCam.sSize, pos);

                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i, localVert + warp);
                    }
                }
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    //Attempts to warp background image, which I'm not sure is ever even used...
    //ALSO warps snow texture
    private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    {
        //self.snowChange = true;

        orig(self, timeStacker, timeSpeed);

        //warp background
        if (Options.BackgroundWarp.Value > 0 && self.backgroundGraphic.isVisible && CamPos.ContainsKey(self.cameraNumber))
        {
            
            Vector2 warp = Options.BackgroundWarp.Value * GetMidpointWarp(self.cameraNumber);

            self.backgroundGraphic.x = self.backgroundGraphic.x - warp.x;
            self.backgroundGraphic.y = self.backgroundGraphic.y - warp.y;
        }

        //warp snow
        if (Options.WarpSnow.Value && Shader.IsKeywordEnabled("SNOW_ON"))
        {
            Shader.EnableKeyword("THELAZYCOWBOY1_WARPMAINTEX");
            Graphics.Blit(OrigSnowTexture, self.SnowTexture, ParallaxMaterial);
            Shader.DisableKeyword("THELAZYCOWBOY1_WARPMAINTEX");
        }
    }

    //Offsets the camera position used for backgrounds, helping to provide basic visual continuity
    private Vector2 BackgroundScene_DrawPos(On.BackgroundScene.orig_DrawPos orig, BackgroundScene self, Vector2 pos, float depth, Vector2 camPos, float hDisplace)
    {
        if (Options.BackgroundWarp.Value > 0)
        {
            var cameras = self.room.game.cameras;
            float lowestCamDist = float.PositiveInfinity;
            RoomCamera lowestCam = null;
            foreach (var cam in cameras)
            {
                float dist = (cam.pos - camPos).sqrMagnitude;
                if (cam.room == self.room && dist < lowestCamDist) { lowestCamDist = dist; lowestCam = cam; }
            }

            if (lowestCam != null && CamPos.ContainsKey(lowestCam.cameraNumber))
            {
                camPos += Options.BackgroundWarp.Value * GetMidpointWarp(lowestCam.cameraNumber);
            }
        }
        return orig(self, pos, depth, camPos, hDisplace);
    }

    //Warps the convergence point for backgrounds, giving a severe parallax effect
    private void BackgroundScene_Update(On.BackgroundScene.orig_Update orig, BackgroundScene self, bool eu)
    {
        orig(self, eu);

        if (Options.BackgroundRotation.Value > 0)
        {
            var cam = self.room.game.cameras.FirstOrDefault(c => c.room == self.room);
            if (cam != null && CamPos.ContainsKey(cam.cameraNumber))
            {
                //reset convergence point in case I messed it up previously. Adapted from decompiled code
                self.convergencePoint = new Vector2(self.room.game.rainWorld.screenSize.x * 0.5f,
                    self.room.game.rainWorld.screenSize.y * (ModManager.DLCShared && self.room.waterInverted ? 1f : 2f) / 3f);

                self.convergencePoint += Options.BackgroundRotation.Value * GetMidpointWarp(cam.cameraNumber);
            }
        }
    }

    //Same as above, but apparently Outer Rim overwrites its inherited function for no good reason
    private Vector2 OuterRimView_DrawPos(On.Watcher.OuterRimView.orig_DrawPos orig, OuterRimView self, BackgroundScene.BackgroundSceneElement element, Vector2 camPos, RoomCamera camera)
    {
        if (Options.BackgroundWarp.Value > 0 && CamPos.ContainsKey(camera.cameraNumber))
            camPos += Options.BackgroundWarp.Value * GetMidpointWarp(camera.cameraNumber);

        return orig(self, element, camPos, camera);
    }

    //Warps the convergence point for Outer Rim
    private void OuterRimView_Update(On.Watcher.OuterRimView.orig_Update orig, OuterRimView self, bool eu)
    {
        orig(self, eu);

        if (Options.BackgroundRotation.Value > 0)
        {
            var cam = self.room.game.cameras.FirstOrDefault(c => c.room == self.room);
            if (cam != null && CamPos.ContainsKey(cam.cameraNumber))
            {
                //reset convergence point in case I messed it up previously. Adapted from decompiled code
                self.perspectiveCenter = new Vector2(self.room.game.rainWorld.screenSize.x,
                    self.room.game.rainWorld.screenSize.y) * OuterRimView.ConvergenceMult;

                self.perspectiveCenter += Options.BackgroundRotation.Value * GetMidpointWarp(cam.cameraNumber);
            }
        }
    }


    //Attempts to force all background elements to have some basic warp
    [Obsolete]
    private void BackgroundSceneElement_DrawSprites(On.BackgroundScene.BackgroundSceneElement.orig_DrawSprites orig, BackgroundScene.BackgroundSceneElement self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        //Applies some shift to elements that aren't normally shifted, like background images and (surprisingly) clouds
        if ((Options.BackgroundWarp.Value > 0 || Options.BackgroundRotation.Value > 0)
            && CamPos.TryGetValue(rCam.cameraNumber, out float2 pos)
            && self is AboveCloudsView.Cloud or BackgroundScene.Simple2DBackgroundIllustration)
        {
            bool cloud = self is AboveCloudsView.Cloud;
            //Vector2 realCamPos = (self.scene is AboveCloudsView acv) ? new(camPos.x, camPos.y + acv.yShift) : camPos;

            bool firstSprite = true;
            foreach (var sprite in sLeaser.sprites)
            {
                //Vector2 center = self.pos + rCam.sSize * 0.5f;//new Vector2(sprite.scaleX * sprite.width, sprite.scaleY * sprite.height);
                //Vector2 warp = CalculateWarp(center, pos) * rCam.sSize / self.depth;
                //Vector2 newPos = self.scene.DrawPos(center - self.scene.sceneOrigo, self.depth, Vector2.zero, rCam.hDisplace); //remember to offset it to corner not center
                //sprite.x = newPos.x;
                //sprite.y = newPos.y;

                Vector2 center = self.pos + (cloud ? rCam.sSize * 0.5f : Vector2.zero);
                Vector2 newCenter = (center - CalculateWarp(center, pos) - self.scene.convergencePoint) / self.depth + self.scene.convergencePoint;
                Vector2 newPos = newCenter;// - rCam.sSize * 0.5f;
                sprite.x = newPos.x + (firstSprite && cloud ? 683 : 0);
                sprite.y = newPos.y;

                firstSprite = false;
            }
        }
    }

    //Easily offsets any sprite
    private void OffsetSprite(BackgroundScene.BackgroundSceneElement self, RoomCamera rCam, FSprite sprite, Vector2 pos, bool offsetX = true, bool offsetY = true)
    {
        if (Options.BackgroundWarp.Value > 0 && CamPos.ContainsKey(rCam.cameraNumber))
        {
            Vector2 newPos = pos + Options.BackgroundWarp.Value * GetMidpointWarp(rCam.cameraNumber);
            if (offsetX) sprite.x = newPos.x;
            if (offsetY) sprite.y = newPos.y;
        }
    }

    //Warps background image
    private void Simple2DBackgroundIllustration_DrawSprites(On.BackgroundScene.Simple2DBackgroundIllustration.orig_DrawSprites orig, BackgroundScene.Simple2DBackgroundIllustration self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        OffsetSprite(self, rCam, sLeaser.sprites[0], self.pos);
    }

    //Warps clouds
    private void CloseCloud_DrawSprites(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, AboveCloudsView.CloseCloud self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        OffsetSprite(self, rCam, sLeaser.sprites[1], new(sLeaser.sprites[1].x, 0), true, false);
    }
    private void DistantCloud_DrawSprites(On.AboveCloudsView.DistantCloud.orig_DrawSprites orig, AboveCloudsView.DistantCloud self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        OffsetSprite(self, rCam, sLeaser.sprites[1], new(sLeaser.sprites[1].x, 0), true, false);
    }
    private void FlyingCloud_DrawSprites(On.AboveCloudsView.FlyingCloud.orig_DrawSprites orig, AboveCloudsView.FlyingCloud self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        OffsetSprite(self, rCam, sLeaser.sprites[0], new(sLeaser.sprites[0].x, 0), true, false);
    }


    private float depthCurve(float d)
    {
        switch (Options.DepthCurve.Value)
        {
            case "EXTREME":
                return d * (d * (d - 3) + 3); //much more severe, cubic curve
            case "PARABOLIC": //this case enables BOTH options... to indicate a "compromise" or something...?
                return d * (2 - d); //simple parabola
            case "INVERSE":
                return 0.5f * d * (d * d + 1); //averages d^3 with d
        }
        return d; //linear
    }
    private static float approxSine(float x)
    {
        return x * (1.5f - 0.5f * x * x); //this is a really cheap but more than adaquate approximation!
    }
    private float sinSmoothCurve(float x)
    {
        switch (Options.SmoothingType.Value)
        {
            case "EXTREME":
                return 0.125f * x * (15 + x * x * (-10 + x * x * 3));
            case "SINUSOIDAL":
                return approxSine(x);
            case "INVERSE":
                return x + x - approxSine(x);
        }
        return x;
    }

    public Vector2 CalculateWarp(Vector2 objPos, float2 playerPos)
    {
        float absBackScale = Mathf.Abs(Options.BackgroundScale.Value);
        float camDiffMod = 1 / (absBackScale + 0.5f * (1 - absBackScale));
        Vector2 warp = new Vector2(
            sinSmoothCurve(camDiffMod * (objPos.x*Options.BackgroundScale.Value + 0.5f*(1-Options.BackgroundScale.Value) - playerPos.x)),
            sinSmoothCurve(camDiffMod * (objPos.y*Options.BackgroundScale.Value + 0.5f*(1-Options.BackgroundScale.Value) - playerPos.y)));
        if (Options.NoCenterWarp.Value)
        {
            float camXDiff2 = 4 * (playerPos.x - 0.5f) * (playerPos.x - 0.5f);
            warp.x *= camXDiff2 * (2 - camXDiff2); //posCamXDiff *= 1.5c^2 - 0.5c^4; c = 2 * (camPos - 0.5)
            float camYDiff2 = 4 * (playerPos.y - 0.5f) * (playerPos.y - 0.5f);
            warp.y *= camYDiff2 * (2 - camYDiff2);
        }
        return Options.Warp.Value * new Vector2(
            Mathf.Clamp(warp.x, -Options.MaxWarpFactor.Value, Options.MaxWarpFactor.Value),
            Mathf.Clamp(warp.y, -Options.MaxWarpFactor.Value, Options.MaxWarpFactor.Value)
            );
    }
    #endregion
}
