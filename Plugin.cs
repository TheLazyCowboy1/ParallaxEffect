using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using Unity.Mathematics;
using UnityEngine.Profiling;
using System.Runtime.CompilerServices;
using Menu;
using RWCustom;
using Rewired;
using Watcher;
using UnityEngine.Rendering;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ParallaxEffect;

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.ParallaxEffect",
        MOD_NAME = "Parallax Effect",
        MOD_VERSION = "0.0.1";


    public static ConfigOptions Options;

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

            On.CustomDecal.DrawSprites -= CustomDecal_DrawSprites;

            //On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;

            //On.RoomCamera.DepthAtCoordinate -= RoomCamera_DepthAtCoordinate;
            //On.RoomCamera.LitAtCoordinate -= RoomCamera_LitAtCoordinate;
            //On.RoomCamera.PixelColorAtCoordinate -= RoomCamera_PixelColorAtCoordinate;

            IsInit = false;
        }
    }

    //public FShader parallaxFShader;
    public Shader parallaxShader;
    public Material parallaxMaterial;

    //public static int ShadPostParallaxGrab = -1;

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

            On.CustomDecal.DrawSprites += CustomDecal_DrawSprites;

            //On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;

            //On.RoomCamera.DepthAtCoordinate += RoomCamera_DepthAtCoordinate;
            //On.RoomCamera.LitAtCoordinate += RoomCamera_LitAtCoordinate;
            //On.RoomCamera.PixelColorAtCoordinate += RoomCamera_PixelColorAtCoordinate;

            //load shader
            try
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles\\LazyCowboy\\ParallaxEffect.assets"));

                void LoadShader(string fileName, string internalName)
                {
                    Logger.LogDebug($"Shader: {fileName} -> {internalName}");
                    try
                    {
                        Shader shader = assetBundle.LoadAsset<Shader>(fileName);
                        if (shader == null)
                            Logger.LogDebug($"Shader {fileName} is null");
                        self.Shaders[internalName].shader = shader;
                    }
                    catch (Exception ex) { Logger.LogError(ex); }
                }

                //LoadShader("ParallaxEffect.shader", "LevelColor");
                //LoadShader("LevelColor.shader", "LevelColor");

                parallaxShader = assetBundle.LoadAsset<Shader>("ParallaxEffect.shader");
                if (parallaxShader == null)
                    Logger.LogError("Could not find shader ParallaxEffect.shader");
                parallaxMaterial = new(parallaxShader);

                /*LoadShader("WaterSurface.shader", "WaterSurface");
                LoadShader("WarpTear.shader", "WarpTear");
                LoadShader("Sandstorm.shader", "Sandstorm");
                LoadShader("RotWormFin.shader", "RotWormFin");
                LoadShader("RotWormBody.shader", "RotWormBody");
                LoadShader("PlaceholderBackgroundElement.shader", "PlaceholderBackgroundElement");
                LoadShader("OuterRimDustGradient.shader", "OuterRimDustGradient");
                LoadShader("OuterRimBackgroundBuilding.shader", "OuterRimBackgroundBuilding");
                LoadShader("MudPit.shader", "MudPit");
                LoadShader("LightSource.shader", "LightSource");
                LoadShader("LightBloom.shader", "LightBloom");
                LoadShader("LightAndSkyBloom.shader", "LightAndSkyBloom");
                LoadShader("GreebleGrid.shader", "GreebleGrid");
                LoadShader("GildedWind.shader", "GildedWind");
                LoadShader("FirmamentCloud.shader", "FirmamentCloud");
                LoadShader("FallingStar.shader", "FallingStar");
                LoadShader("DustGradient.shader", "DustGradient");
                LoadShader("DustDunes.shader", "DustDunes");
                LoadShader("DistantBkgObjectRepeatHorizontal.shader", "DistantBkgObjectRepeatHorizontal");
                LoadShader("DistantBkgObjectAlpha.shader", "DistantBkgObjectAlpha");
                LoadShader("DistantBkgObject.shader", "DistantBkgObject");
                LoadShader("DeepWater.shader", "DeepWater");
                LoadShader("Cloud.shader", "Cloud");

                LoadShader("BoxWorm.shader", "BoxWormBody");
                LoadShader("BoxWorm.shader", "BoxWormLarvaHolder");
                LoadShader("BoxWorm.shader", "BoxWormBox");
                LoadShader("BoxWorm.shader", "BoxWormOpenBox");
                LoadShader("BoxWorm.shader", "BoxWormFakeLarva");
                LoadShader("BoxWorm.shader", "BoxWormLarvaFood");
                LoadShader("BoxWorm.shader", "FireSpriteWing");
                LoadShader("BoxWorm.shader", "FireSpriteBody");

                LoadShader("BkgFloor.shader", "BkgFloor");
                LoadShader("BackgroundNoHoles.shader", "BackgroundNoHoles");
                LoadShader("BackgroundJaggedCircle.shader", "BackgroundJaggedCircle");
                LoadShader("BackgroundDune.shader", "BackgroundDune");
                LoadShader("BackgroundAdditive.shader", "BackgroundAdditive");
                LoadShader("Background.shader", "Background");
                LoadShader("AncientUrbanBuilding.shader", "AncientUrbanBuilding");*/

                //ShadPostParallaxGrab = Shader.PropertyToID("_PostParallaxGrab");
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

    public RenderTexture parallaxRenderTex;
    private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    {
        /*parallaxRenderTex ??= new(new RenderTextureDescriptor(self.levelTexture.width, self.levelTexture.height));
        Graphics.Blit(
            (self.levelTexCombiner != null && self.levelTexCombiner.isActive) ? self.levelTexCombiner.combinedLevelTex : self.levelTexture,
            parallaxRenderTex,
            parallaxMaterial
            );
        Shader.SetGlobalTexture(RainWorld.ShadPropLevelTex, parallaxRenderTex);

        Shader.EnableKeyword("COMBINEDLEVEL");*/

        try
        {
            if (!self.levelTexCombiner.bufferIDs.Contains("LazyCowboy_ParallaxShader"))
            {
                self.levelTexCombiner.AddPass(parallaxShader, "LazyCowboy_ParallaxShader");
                Logger.LogDebug($"Added {parallaxShader.name} shader pass!");
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }

        orig(self, timeStacker, timeSpeed);

        //Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4(0, 0, 1, 1));
        //Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4(self.levelGraphic.x / self.sSize.x, self.levelGraphic.y / self.sSize.y, 1f + self.levelGraphic.x / self.sSize.x, 1f + self.levelGraphic.y / self.sSize.y));
    }

    float depthCurve(float d)
    {
        switch (Options.DepthCurve.Value)
        {
            case "EXTREME":
                return d * (d * (d - 3) + 3); //much more severe, cubic curve
            case "PARABOLIC": //this case enables BOTH options... to indicate a "compromise" or something...?
                return d * (2 - d); //simple parabola
            case "INVERSE":
                return 0.5f*d * (d*d + 1); //averages d^3 with d
        }
        return d; //linear
    }
    float approxSine(float x)
    {
        return x * (1.5f - 0.5f * x * x); //this is a really cheap but more than adaquate approximation!
    }
    float sinSmoothCurve(float diff)
    {
        switch (Options.SmoothingType.Value)
        {
            case "SINESMOOTHING":
                return approxSine(diff);
            case "INVSINESMOOTHING":
                return diff + diff - approxSine(diff);
        }
        return diff;
    }
    private void CustomDecal_DrawSprites(On.CustomDecal.orig_DrawSprites orig, CustomDecal self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        try
        {
            if (Options.WarpDecals.Value && CamPos.TryGetValue(rCam, out float2 pos))
            {
                for (int i = 0; i < self.verts.Length; i++)
                {
                    Vector2 localVert = self.verts[i] - camPos;
                    if (localVert.x >= 0 && localVert.x < rCam.sSize.x
                        && localVert.y >= 0 && localVert.y < rCam.sSize.y) //simple bounds check
                    {
                        //Vector2 warp = Options.Warp.Value * depthCurve(rCam.DepthAtCoordinate(self.verts[i]) * 1.2f - 0.2f)
                        var data = self.placedObject.data as PlacedObject.CustomDecalData; //use flat data instead of image depth?
                        //use the decal's depth (weighted towards the deeper part) to determine a warp factor
                        Vector2 warp = Options.Warp.Value * depthCurve((0.3f*data.fromDepth + 0.7f*data.toDepth - 5f) * 0.04f)
                            * new Vector2(sinSmoothCurve(localVert.x / 1400f - pos.x), sinSmoothCurve(localVert.y / 800f - pos.y));
                            //* new Vector2(sinSmoothCurve(self.verts[i].x / 1400f - pos.x), sinSmoothCurve(self.verts[i].y / 800f - pos.y));
                        if (Options.NoCenterWarp.Value)
                        {
                            warp.x *= 2f * Mathf.Abs(pos.x - 0.5f);
                            warp.y *= 2f * Mathf.Abs(pos.y - 0.5f);
                        }
                        //(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i, self.verts[i] - camPos + warp);
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i, localVert + warp);
                    }
                }
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    //public static float2 CamPos;
    public static Dictionary<RoomCamera, float2> CamPos = new();

    private void RoomCamera_GetCameraBestIndex(On.RoomCamera.orig_GetCameraBestIndex orig, RoomCamera self)
    {
        orig(self);

        var crit = self.followAbstractCreature?.realizedCreature;
        if (crit != null)
        {
            Vector2? critPos = (crit.inShortcut ? self.game.shortcuts.OnScreenPositionOfInShortCutCreature(self.room, crit) : crit.mainBodyChunk.pos);
            if (critPos != null)
            {
                if (!CamPos.ContainsKey(self))
                    CamPos.Add(self, new(0.5f, 0.5f));

                //Vector2 localPos = (critPos.Value - self.CamPos(self.currentCameraPosition)
                //Vector2 localPos = (critPos.Value - self.levelGraphic.GetPosition()
                Vector2 localPos = (critPos.Value - self.pos
                    + (self.followCreatureInputForward + self.leanPos) * 4f)
                    / self.sSize;

                try
                {
                    float mouseX = Input.GetAxis("Mouse X") * 0.25f;
                    if (mouseX != 0f)
                    {
                        float strength = Mathf.Clamp01(Mathf.Abs(mouseX));
                        //mouseX = 0.5f + 0.5f * Mathf.Clamp(mouseX, -1f, 1f);
                        localPos.x += strength * ((mouseX > 0 ? 0.8f : -0.8f) - localPos.x);
                    }

                    float mouseY = Input.GetAxis("Mouse Y") * 0.25f * 0.5625f; //0.5625 = 9/16 
                    if (mouseY != 0f)
                    {
                        float strength = Mathf.Clamp01(Mathf.Abs(mouseY));
                        //mouseY = 0.5f + 0.5f * Mathf.Clamp(mouseY, -1f, 1f);
                        localPos.y += strength * ((mouseY > 0 ? 0.8f : -0.8f) - localPos.y);
                    }
                }
                catch { }

                localPos.x = Mathf.Clamp01(localPos.x);
                localPos.y = Mathf.Clamp01(localPos.y);

                CamPos[self] += Options.CameraMoveSpeed.Value * (new float2(localPos.x, localPos.y) - CamPos[self]);
                if (Options.InvertPos.Value)
                {
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosX", 1f - CamPos[self].x);
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosY", 1f - CamPos[self].y);
                }
                else
                {
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosX", CamPos[self].x);
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosY", CamPos[self].y);
                }
            }
        }
    }

    private void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
    {
        orig(self);

        //Shader.SetGlobalTexture("TheLazyCowboy1_ScreenTexture", self.levelTexture);
        try
        {
            //self.levelTexCombiner.AddPass(RenderTexture.GetTemporary(1400, 800), parallaxMaterial, parallaxShader.name, LevelTexCombiner.last);
            self.levelTexCombiner.AddPass(parallaxShader, parallaxShader.name, LevelTexCombiner.last);
            //Logger.LogDebug($"Added {parallaxShader.name} shader pass!"); //happens every screen change; annoying log spam
        } catch (Exception ex) { Logger.LogError(ex); }
    }

    private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
    {
        //WarpedLevelTextures.Clear(); //just in case

        //setup constants
        Shader.SetGlobalFloat("TheLazyCowboy1_WarpX", Options.Warp.Value / 1400f);
        Shader.SetGlobalFloat("TheLazyCowboy1_WarpY", Options.Warp.Value / 800f);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarpX", Options.Warp.Value * Options.MaxWarpFactor.Value / 1400f);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarpY", Options.Warp.Value * Options.MaxWarpFactor.Value / 800f);
        //Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarpY", Options.MaxWarp.Value / 800f);
        int testNum = (int)Mathf.Ceil(Options.Warp.Value * Options.MaxWarpFactor.Value * (Options.EndOffset.Value - Options.StartOffset.Value) / Options.Optimization.Value);
        Shader.SetGlobalInt("TheLazyCowboy1_TestNum", testNum + 1); //add 1 to make it range [0,1] instead of [0,1)
        Shader.SetGlobalFloat("TheLazyCowboy1_StepSize", (Options.EndOffset.Value - Options.StartOffset.Value) / testNum);
        Shader.SetGlobalFloat("TheLazyCowboy1_StartOffset", Options.StartOffset.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_RedModScale", Options.RedModScale.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_CamPosX", 0.5f);
        Shader.SetGlobalFloat("TheLazyCowboy1_CamPosY", 0.5f);

        switch (Options.SmoothingType.Value)
        {
            case "SINESMOOTHING":
                Shader.DisableKeyword("THELAZYCOWBOY1_INVSINESMOOTHING");
                Shader.EnableKeyword("THELAZYCOWBOY1_SINESMOOTHING");
                break;
            case "INVSINESMOOTHING":
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

        if (Options.NoCenterWarp.Value && !Shader.IsKeywordEnabled("THELAZYCOWBOY1_NOCENTERWARP"))
            Shader.EnableKeyword("THELAZYCOWBOY1_NOCENTERWARP");
        else if (!Options.NoCenterWarp.Value && Shader.IsKeywordEnabled("THELAZYCOWBOY1_NOCENTERWARP"))
            Shader.DisableKeyword("THELAZYCOWBOY1_NOCENTERWARP");

        Logger.LogDebug($"Setup shader constants");

        orig(self, game, cameraNumber);

        //FSprite shaderSprite = new("Futile_White");
        //shaderSprite.scaleX = 1400f;//self.sSize.x;
        //shaderSprite.scaleY = 800f;// self.sSize.y;
        //shaderSprite.shader = parallaxFShader;
        //self.ReturnFContainer("Foreground").AddChildAtIndex(shaderSprite, 1);
    }
}
