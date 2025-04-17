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

            //On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;

            //On.RoomCamera.DepthAtCoordinate -= RoomCamera_DepthAtCoordinate;
            //On.RoomCamera.LitAtCoordinate -= RoomCamera_LitAtCoordinate;
            //On.RoomCamera.PixelColorAtCoordinate -= RoomCamera_PixelColorAtCoordinate;

            IsInit = false;
        }
    }

    //public FShader parallaxFShader;

    public static int ShadPostParallaxGrab = -1;

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

                LoadShader("ParallaxEffect.shader", "LevelColor");

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

                ShadPostParallaxGrab = Shader.PropertyToID("_PostParallaxGrab");
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

    private Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera self, Vector2 coord)
    {
        if (!WarpedLevelTextures.TryGetValue(self, out var tex))
            return orig(self, coord);

        Vector2 vector = coord - self.CamPos(self.currentCameraPosition);
        Color pixel = tex.GetPixel(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
        if (pixel.r == 1f && pixel.g == 1f && pixel.b == 1f)
        {
            return self.paletteTexture.GetPixel(0, 7);
        }
        int num = Mathf.FloorToInt(pixel.r * 255f);
        float num2 = 0f;
        if (num > 90)
        {
            num -= 90;
        }
        else
        {
            num2 = 1f;
        }
        int num3 = Mathf.FloorToInt((float)num / 30f);
        int num4 = (num - 1) % 30;
        return Color.Lerp(Color.Lerp(self.paletteTexture.GetPixel(num4, num3 + 3), self.paletteTexture.GetPixel(num4, num3), num2), self.paletteTexture.GetPixel(1, 7), (float)num4 * (1f - self.paletteTexture.GetPixel(9, 7).r) / 30f);
    }

    private bool? RoomCamera_LitAtCoordinate(On.RoomCamera.orig_LitAtCoordinate orig, RoomCamera self, Vector2 coord)
    {
        if (!WarpedLevelTextures.TryGetValue(self, out var tex))
            return orig(self, coord);

        Vector2 vector = coord - self.CamPos(self.currentCameraPosition);
        Color pixel = tex.GetPixel(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
        if (pixel.r == 1f && pixel.g == 1f && pixel.b == 1f)
        {
            return null;
        }
        return new bool?(Mathf.FloorToInt(pixel.r * 255f) > 90);
    }

    private float RoomCamera_DepthAtCoordinate(On.RoomCamera.orig_DepthAtCoordinate orig, RoomCamera self, Vector2 coord)
    {
        if (!WarpedLevelTextures.TryGetValue(self, out var tex))
            return orig(self, coord);

        Vector2 vector = coord - self.CamPos(self.currentCameraPosition);
        Color pixel = tex.GetPixel(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
        if (pixel.r == 1f && pixel.g == 1f && pixel.b == 1f)
        {
            return 1f;
        }
        int num = Mathf.FloorToInt(pixel.r * 255f);
        if (num > 90)
        {
            num -= 90;
        }
        return (float)((num - 1) % 30) / 30f;
    }

    //public static ConditionalWeakTable<RoomCamera, Texture2D> WarpedLevelTextures = new();
    public static Dictionary<RoomCamera, Texture2D> WarpedLevelTextures = new();

    private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    {
        orig(self, timeStacker, timeSpeed);

        try
        {
            var postParallaxGrabRaw = Shader.GetGlobalTexture(ShadPostParallaxGrab);
            if (postParallaxGrabRaw != null)
            {
                //RenderTexture texConverted = postParallaxGrabRaw as RenderTexture;
                Texture2D tex2D = self.levelTexture.Clone();
                //Texture2D grab2D = new(postParallaxGrabRaw.width, postParallaxGrabRaw.height);
                //Graphics.CopyTexture(grab2D, postParallaxGrabRaw);
                Graphics.CopyTexture(postParallaxGrabRaw, 0, 0, 0, 0, postParallaxGrabRaw.width, postParallaxGrabRaw.height,
                    tex2D, 0, 0, Mathf.Max(0, -Mathf.RoundToInt(self.levelGraphic.x)), Mathf.Max(0, -Mathf.RoundToInt(self.levelGraphic.y)));

                //Vector2 start = -self.levelGraphic.GetPosition(), size = new(postParallaxGrabRaw.width, postParallaxGrabRaw.height);
                //tex2D.SetPixels(postParallaxGrabRaw.)

                Shader.SetGlobalTexture(RainWorld.ShadPropLevelTex, tex2D);

                if (!WarpedLevelTextures.ContainsKey(self))
                    WarpedLevelTextures.Add(self, tex2D);
                else
                    WarpedLevelTextures[self] = tex2D;

                //Logger.LogDebug($"PostParallaxGrab Size = {postParallaxGrabRaw.width}x{postParallaxGrabRaw.height}");
                //var postParallaxGrab = postParallaxGrabRaw as Texture2D;
                //if (postParallaxGrab != null)
                //{
                //postParallaxGrab.Resize(1400, 800);
                //Shader.SetGlobalTexture(RainWorld.ShadPropLevelTex, postParallaxGrab);
                //}
                //else Logger.LogDebug("Texture is not a Texture2D!!!");
            }
            else Logger.LogDebug("Cannot find _PostParallaxGrab");
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    public static float2 CamPos;

    private void RoomCamera_GetCameraBestIndex(On.RoomCamera.orig_GetCameraBestIndex orig, RoomCamera self)
    {
        orig(self);

        var crit = self.followAbstractCreature?.realizedCreature;
        if (crit != null)
        {
            Vector2? critPos = (crit.inShortcut ? self.game.shortcuts.OnScreenPositionOfInShortCutCreature(self.room, crit) : crit.mainBodyChunk.pos);
            if (critPos != null)
            {
                Vector2 localPos = critPos.Value - self.CamPos(self.currentCameraPosition);
                if (Options.InvertPos.Value)
                {
                    CamPos = new(1f - localPos.x / self.sSize.x, 1f - localPos.y / self.sSize.y);
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosX", 1f - localPos.x / self.sSize.x);
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosY", 1f - localPos.y / self.sSize.y);
                }
                else
                {
                    CamPos = new(localPos.x / self.sSize.x, localPos.y / self.sSize.y);
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosX", localPos.x / self.sSize.x);
                    Shader.SetGlobalFloat("TheLazyCowboy1_CamPosY", localPos.y / self.sSize.y);
                }
            }
        }
    }

    private void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
    {
        orig(self);

        Shader.SetGlobalTexture("TheLazyCowboy1_ScreenTexture", self.levelTexture);
    }

    private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
    {
        WarpedLevelTextures.Clear(); //just in case

        //setup constants
        Shader.SetGlobalFloat("TheLazyCowboy1_WarpX", Options.Warp.Value / 1400f);
        Shader.SetGlobalFloat("TheLazyCowboy1_WarpY", Options.Warp.Value / 800f);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarpX", Options.MaxWarp.Value / 1400f);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarpY", Options.MaxWarp.Value / 800f);
        Shader.SetGlobalInt("TheLazyCowboy1_TestNum", (int)Mathf.Ceil(Options.MaxWarp.Value / Options.Optimization.Value));
        Shader.SetGlobalFloat("TheLazyCowboy1_CamPosX", 0.5f);
        Shader.SetGlobalFloat("TheLazyCowboy1_CamPosY", 0.5f);

        switch (Options.SmoothingType.Value)
        {
            case "SINESMOOTHING":
                if (!Shader.IsKeywordEnabled("SINESMOOTHING"))
                {
                    if (Shader.IsKeywordEnabled("INVSINESMOOTHING"))
                        Shader.DisableKeyword("INVSINESMOOTHING");
                    Shader.EnableKeyword("SINESMOOTHING");
                }
                break;
            case "INVSINESMOOTHING":
                if (!Shader.IsKeywordEnabled("INVSINESMOOTHING"))
                {
                    if (Shader.IsKeywordEnabled("SINESMOOTHING"))
                        Shader.DisableKeyword("SINESMOOTHING");
                    Shader.EnableKeyword("INVSINESMOOTHING");
                }
                break;
            case "FLAT":
                if (Shader.IsKeywordEnabled("SINESMOOTHING"))
                    Shader.DisableKeyword("SINESMOOTHING");
                if (Shader.IsKeywordEnabled("INVSINESMOOTHING"))
                    Shader.DisableKeyword("INVSINESMOOTHING");
                break;
        }

        orig(self, game, cameraNumber);

        //FSprite shaderSprite = new("Futile_White");
        //shaderSprite.scaleX = 1400f;//self.sSize.x;
        //shaderSprite.scaleY = 800f;// self.sSize.y;
        //shaderSprite.shader = parallaxFShader;
        //self.ReturnFContainer("Foreground").AddChildAtIndex(shaderSprite, 1);
    }
}
