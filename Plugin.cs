using System;
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
using Graphics = UnityEngine.Graphics;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Logging;
using System.Security.Cryptography;

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
        MOD_VERSION = "0.1.3";


    public static ConfigOptions Options;
    public static ManualLogSource PublicLogger;

    #region Setup
    public Plugin()
    {
        try
        {
            PublicLogger = Logger;
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
            //On.RoomCamera.GetCameraBestIndex -= RoomCamera_GetCameraBestIndex;
            On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
            On.RoomCamera.Update -= RoomCamera_Update;

            On.RoomCamera.UpdateSnowLight -= RoomCamera_UpdateSnowLight;
            On.RoomCamera.PreLoadTexture -= RoomCamera_PreLoadTexture;
            On.RoomCamera.MoveCamera2 -= RoomCamera_MoveCamera2;
            //On.WorldLoader.CreatingAbstractRoomsThread -= WorldLoader_CreatingAbstractRoomsThread;
            On.WorldLoader.CreatingWorld -= WorldLoader_CreatingWorld;

            On.CustomDecal.DrawSprites -= CustomDecal_DrawSprites;
            On.BackgroundScene.DrawPos -= BackgroundScene_DrawPos;
            On.BackgroundScene.Update -= BackgroundScene_Update;
            On.Watcher.OuterRimView.DrawPos -= OuterRimView_DrawPos;
            On.Watcher.OuterRimView.Update -= OuterRimView_Update;
            //On.BackgroundScene.BackgroundSceneElement.DrawSprites -= BackgroundSceneElement_DrawSprites;
            On.BackgroundScene.Simple2DBackgroundIllustration.DrawSprites -= Simple2DBackgroundIllustration_DrawSprites;
            On.AboveCloudsView.CloseCloud.DrawSprites -= CloseCloud_DrawSprites;
            On.AboveCloudsView.DistantCloud.DrawSprites -= DistantCloud_DrawSprites;
            On.AboveCloudsView.FlyingCloud.DrawSprites -= FlyingCloud_DrawSprites;
            IL.TerrainCurve.DrawSprites -= TerrainCurve_DrawSprites;
            //On.TerrainCurveMaskSource.DrawSprites -= TerrainCurveMaskSource_DrawSprites;

            //On.Watcher.LevelTexCombiner.CreateBuffer -= LevelTexCombiner_CreateBuffer;

            IsInit = false;
        }
    }

    //public FShader parallaxFShader;
    public Shader ParallaxShader;
    public Material ParallaxMaterial;

    //public Shader ParallaxAdvancedShader;
    //public Material ParallaxAdvancedMaterial;

    public Shader BackgroundBuilderShader;
    public Material Layer2BuilderMaterial, Layer3BuilderMaterial;

    public static string ModFolderPath = "";
    public static bool SBCameraScrollEnabled = false;
    public static string SBCameraScrollPath = "";

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
            //On.RoomCamera.GetCameraBestIndex += RoomCamera_GetCameraBestIndex;
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.Update += RoomCamera_Update;

            On.RoomCamera.UpdateSnowLight += RoomCamera_UpdateSnowLight;
            On.RoomCamera.PreLoadTexture += RoomCamera_PreLoadTexture;
            On.RoomCamera.MoveCamera2 += RoomCamera_MoveCamera2;
            //On.WorldLoader.CreatingAbstractRoomsThread += WorldLoader_CreatingAbstractRoomsThread;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;

            On.CustomDecal.DrawSprites += CustomDecal_DrawSprites;
            On.BackgroundScene.DrawPos += BackgroundScene_DrawPos;
            On.BackgroundScene.Update += BackgroundScene_Update;
            On.Watcher.OuterRimView.DrawPos += OuterRimView_DrawPos;
            On.Watcher.OuterRimView.Update += OuterRimView_Update;
            //On.BackgroundScene.BackgroundSceneElement.DrawSprites += BackgroundSceneElement_DrawSprites;
            On.BackgroundScene.Simple2DBackgroundIllustration.DrawSprites += Simple2DBackgroundIllustration_DrawSprites;
            On.AboveCloudsView.CloseCloud.DrawSprites += CloseCloud_DrawSprites;
            On.AboveCloudsView.DistantCloud.DrawSprites += DistantCloud_DrawSprites;
            On.AboveCloudsView.FlyingCloud.DrawSprites += FlyingCloud_DrawSprites;
            IL.TerrainCurve.DrawSprites += TerrainCurve_DrawSprites;
            //On.TerrainCurveMaskSource.DrawSprites += TerrainCurveMaskSource_DrawSprites;

            //On.Watcher.LevelTexCombiner.CreateBuffer += LevelTexCombiner_CreateBuffer;

            //load shader
            try
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles\\LazyCowboy\\ParallaxEffect.assets"));

                ParallaxShader = assetBundle.LoadAsset<Shader>("ParallaxEffect.shader");
                if (ParallaxShader == null)
                    Logger.LogError("Could not find shader ParallaxEffect.shader");
                ParallaxMaterial = new(ParallaxShader);

                //ParallaxAdvancedShader = assetBundle.LoadAsset<Shader>("ParallaxAdvanced.shader");
                //if (ParallaxAdvancedShader == null)
                    //Logger.LogError("Could not find shader ParallaxAdvanced.shader");
                //ParallaxAdvancedMaterial = new(ParallaxAdvancedShader);

                BackgroundBuilderShader = assetBundle.LoadAsset<Shader>("ParallaxBackgroundBuilder.shader");
                if (BackgroundBuilderShader == null)
                    Logger.LogError("Could not find shader ParallaxBackgroundBuilder.shader");
                Layer2BuilderMaterial = new(BackgroundBuilderShader) { name = "TheLazyCowboy1_BackgroundBuilder_l2" };
                Layer3BuilderMaterial = new(BackgroundBuilderShader) { name = "TheLazyCowboy1_BackgroundBuilder_l3" };
                Layer3BuilderMaterial.EnableKeyword("THELAZYCOWBOY1_INCLUDELAYER2");

                ShadCamPosX = Shader.PropertyToID("TheLazyCowboy1_CamPosX");
                ShadCamPosY = Shader.PropertyToID("TheLazyCowboy1_CamPosY");
            }
            catch (Exception ex) { Logger.LogError(ex); }

            ModFolderPath = ModManager.ActiveMods.Find(mod => mod.id == MOD_ID).path;

            var SBCameraScroll = ModManager.ActiveMods.Find(mod => mod.id == "SBCameraScroll");
            if (SBCameraScroll != null)
            {
                SBCameraScrollEnabled = true;
                SBCameraScrollPath = SBCameraScroll.path;
                Logger.LogDebug($"SBCameraScroll enabled = {SBCameraScrollEnabled}; path = " + SBCameraScrollPath);
            }
            else SBCameraScrollEnabled = false;

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

    public int CachedTextureCount = 1;
    public RenderTexture[] Layer2TexArray = new RenderTexture[1];
    public RenderTexture[] Layer3TexArray = new RenderTexture[1];
    public string[] LayerRooms = new string[1];
    public int CurLayerIdx = 0;
    public RenderTexture Layer2Tex { get => Layer2TexArray[CurLayerIdx]; set => Layer2TexArray[CurLayerIdx] = value; }
    public RenderTexture Layer3Tex { get => Layer3TexArray[CurLayerIdx]; set => Layer3TexArray[CurLayerIdx] = value; }
    public string CurRoom { get => LayerRooms[CurLayerIdx]; set => LayerRooms[CurLayerIdx] = value; }
    public void IncrementLayerIdx()
    {
        CurLayerIdx = (CurLayerIdx > 0) ? CurLayerIdx - 1 : CachedTextureCount - 1;
    }
    public void SwitchToLayerIdx(int newIdx)
    {
        CurLayerIdx = newIdx;
    }

    public RenderTexture OrigSnowTexture;

    //public CommandBuffer BackgroundBuilderBuffer;
    public bool CanRenderBackground = true;
    //public string LastGeneratedRoom = "";
    public List<string> GeneratedBackgrounds = new();

    public byte[] PreLoadedLayer2, PreLoadedLayer3;


    //Sets/calculates the shader constants
    private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
    {

        //setup constants
        Shader.SetGlobalFloat("TheLazyCowboy1_Warp", Options.Warp.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxWarp", Options.MaxWarpFactor.Value);

        float startOffset = clampedDepthCurve(-0.2f); //prevent unnecessary processing
        int testNum = Mathf.Max(2, (int)Mathf.Ceil(Mathf.Abs(Options.Warp.Value) * Options.MaxWarpFactor.Value * (Options.EndOffset.Value - startOffset) / Options.Optimization.Value));
        Shader.SetGlobalInt("TheLazyCowboy1_TestNum", testNum);
        Shader.SetGlobalFloat("TheLazyCowboy1_StepSize", (Options.EndOffset.Value - startOffset) / testNum);
        //Shader.SetGlobalFloat("TheLazyCowboy1_StepSize", Options.Optimization.Value);

        Shader.SetGlobalFloat("TheLazyCowboy1_StartOffset", startOffset);
        Shader.SetGlobalFloat("TheLazyCowboy1_RedModScale", Options.RedModScale.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_BackgroundScale", Options.BackgroundScale.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_AntiAliasingFac", Options.AntiAliasing.Value * 2.5f);
        Shader.SetGlobalFloat("TheLazyCowboy1_MaxXDistance",Options.MaxXDistance.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_ProjectionMod", Options.DepthScale.Value);
        Shader.SetGlobalFloat("TheLazyCowboy1_MinObjectDepth", Options.MinObjectDepth.Value);
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

        if (Options.DynamicOptimization.Value)
            Shader.EnableKeyword("THELAZYCOWBOY1_DYNAMICOPTIMIZATION");
        else
            Shader.DisableKeyword("THELAZYCOWBOY1_DYNAMICOPTIMIZATION");

        if (Options.NoCenterWarp.Value)
            Shader.EnableKeyword("THELAZYCOWBOY1_NOCENTERWARP");
        else
            Shader.DisableKeyword("THELAZYCOWBOY1_NOCENTERWARP");

        if (Options.ClosestPixelOnly.Value)
            Shader.EnableKeyword("THELAZYCOWBOY1_CLOSESTPIXELONLY");
        else
            Shader.DisableKeyword("THELAZYCOWBOY1_CLOSESTPIXELONLY");

        if (Options.ShaderLayers.Value >= 2)
            Shader.EnableKeyword("THELAZYCOWBOY1_PROCESSLAYER2");
        else
            Shader.DisableKeyword("THELAZYCOWBOY1_PROCESSLAYER2");

        if (Options.ShaderLayers.Value >= 3)
            Shader.EnableKeyword("THELAZYCOWBOY1_PROCESSLAYER3");
        else
            Shader.DisableKeyword("THELAZYCOWBOY1_PROCESSLAYER3");

        if (Options.SimplerBackgrounds.Value)
            Shader.EnableKeyword("THELAZYCOWBOY1_SIMPLERLAYERS");
        else
            Shader.DisableKeyword("THELAZYCOWBOY1_SIMPLERLAYERS");

        Logger.LogDebug("Setup shader constants");


        orig(self, game, cameraNumber);


        //Setup rendertextures and buffers

        //clear out old arrays
        for (int i = 0; i < CachedTextureCount; i++)
        {
            LayerRooms[i] = null;
            Layer2TexArray[i]?.Release();
            Layer2TexArray[i] = null;
            Layer3TexArray[i]?.Release();
            Layer3TexArray[i] = null;
        }

        CurLayerIdx = 0;
        CachedTextureCount = Options.CachedRenderTextures.Value;
        LayerRooms = new string[CachedTextureCount];
        Layer2TexArray = new RenderTexture[CachedTextureCount];
        Layer3TexArray = new RenderTexture[CachedTextureCount];

        //fill new arrays with textures
        if (Options.ShaderLayers.Value >= 2)
        {
            for (int i = 0; i < CachedTextureCount; i++)
            {
                LayerRooms[i] = "";
                Layer2TexArray[i] = MakeRenderTex(1400, 800);
                if (Options.ShaderLayers.Value >= 3)
                {
                    Layer3TexArray[i] = MakeRenderTex(1400, 800);
                }
            }
        }

        PreLoadedLayer2 = null;
        PreLoadedLayer3 = null;
        //LastGeneratedRoom = "";
        GeneratedBackgrounds.Clear();

        OrigSnowTexture?.Release();
        //OrigSnowTexture = new(1400, 800, 0, DefaultFormat.LDR) { filterMode = 0 };

    }

    //Actually adds the shader to the LevelTexCombiner whenever the LevelTexCombiner gets cleared
    //ALSO attempts to resolution scale...
    //And builds the 2nd and 3rd layers...
    private void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
    {
        IntVector2 origSize = new(0, 0); //say origSize was 0 if it didn't exist before
        if (self.levelTexCombiner.combinedLevelTex == null) //SBCameraScroll refuses to resize unless the texture already exists; a big shortcoming
            self.levelTexCombiner.Initialize();
        else
            origSize = new(LevTex(self).width, LevTex(self).height);

        orig(self);

        if (self.room == null) return;

        //Shader.SetGlobalTexture("TheLazyCowboy1_ScreenTexture", LevTex(self));
        try
        {
            var t = self.levelTexCombiner;

            //Add the parallax shader pass
            t.AddPass(ParallaxShader, ParallaxShader.name, LevelTexCombiner.last);

            IntVector2 size = new(LevTex(self).width, LevTex(self).height);

            //create background textures
            if (Options.ShaderLayers.Value > 1)
            {
                string room = self.room.abstractRoom.name + "_" + ((size.x == 1400 && size.y == 800) ? (self.currentCameraPosition + 1).ToString() : "sb");
                //bool generateBackground = true;
                //Load from cache
                if (LayerRooms.Contains(room))
                {
                    //generateBackground = false;
                    SwitchToLayerIdx(Array.IndexOf(LayerRooms, room));
                    Shader.SetGlobalTexture("_TheLazyCowboy1_Layer2Tex", Layer2Tex);
                    Shader.SetGlobalTexture("_TheLazyCowboy1_Layer3Tex", Layer3Tex);

                    Logger.LogDebug($"Switching to background image {CurLayerIdx} for " + room);
                }
                //Generate/load the background textures
                else if (CanRenderBackground)
                {
                    IncrementLayerIdx();

                    //scale background textures for SBCameraScroll
                    if (size.x != Layer2Tex.width || size.y != Layer2Tex.height)
                    {
                        Layer2Tex?.Release();
                        Layer2Tex = MakeRenderTex(size.x, size.y);
                        Layer3Tex?.Release();
                        if (Options.ShaderLayers.Value >= 3)
                            Layer3Tex = MakeRenderTex(size.x, size.y);
                        Logger.LogDebug($"Resized layer textures: {Layer2Tex.width}x{Layer2Tex.height}");
                    }

                    CanRenderBackground = false;
                    CurRoom = room;

                    CommandBuffer buffer = new() { name = "TheLazyCowboy1_BackgroundBuilder" };
                    if (PreLoadedLayer2 != null)
                    { //pre-load layer 2
                        Texture2D l2Tex = MakeTex2D(size.x, size.y);
                        l2Tex.LoadImage(PreLoadedLayer2, false);
                        l2Tex.Apply();
                        buffer.Blit(l2Tex, Layer2Tex);
                        if (Options.ShaderLayers.Value < 3) CanRenderBackground = true;
                    }
                    else //generate layer 2
                    {
                        buffer.Blit(LevTex(self), Layer2Tex, Layer2BuilderMaterial);
                        buffer.RequestAsyncReadback(Layer2Tex, result =>
                        {
                            if (result.done)
                            {
                                if (result.hasError)
                                    Logger.LogError("Error generating layer 2 texture for " + room);
                                else if (Options.SaveBackgroundTextures.Value)
                                    SaveBackgroundTexture(result, room + "_l2");

                                if (Options.ShaderLayers.Value < 3)
                                {
                                    CanRenderBackground = true;
                                    buffer.Release();
                                }
                            }
                        });
                    }
                    buffer.SetGlobalTexture("_TheLazyCowboy1_Layer2Tex", Layer2Tex);

                    if (Options.ShaderLayers.Value >= 3)
                    {
                        if (PreLoadedLayer3 != null)
                        { //pre-load layer 3
                            Texture2D l3Tex = MakeTex2D(size.x, size.y);
                            l3Tex.LoadImage(PreLoadedLayer3, false);
                            l3Tex.Apply();
                            buffer.Blit(l3Tex, Layer3Tex);
                            CanRenderBackground = true;
                        }
                        else
                        {
                            Layer3BuilderMaterial.SetTexture("_Layer2Tex", Layer2Tex);
                            //buffer.EnableShaderKeyword("THELAZYCOWBOY1_INCLUDELAYER2");
                            buffer.Blit(LevTex(self), Layer3Tex, Layer3BuilderMaterial);
                            //buffer.DisableShaderKeyword("THELAZYCOWBOY1_INCLUDELAYER2");
                            buffer.RequestAsyncReadback(Layer3Tex, result =>
                            {
                                if (result.done)
                                {
                                    if (result.hasError)
                                        Logger.LogError("Error generating layer 3 texture for " + room);
                                    else if (Options.SaveBackgroundTextures.Value)
                                        SaveBackgroundTexture(result, room + "_l3");

                                    CanRenderBackground = true;
                                    buffer.Release();
                                }
                            });
                        }
                        buffer.SetGlobalTexture("_TheLazyCowboy1_Layer3Tex", Layer3Tex);
                    }

                    Graphics.ExecuteCommandBufferAsync(buffer, ComputeQueueType.Background);
                    
                    Logger.LogDebug($"Generating or loading background image {CurLayerIdx} for " + room);
                }
                PreLoadedLayer2 = null;
                PreLoadedLayer3 = null;
            }

            //Resolution Scale in a SBCameraScroll-friendly way
            if (Options.ResolutionScaleEnabled.Value && (size.x != origSize.x || size.y != origSize.y))
            {
                t.combinedLevelTex.Release();
                t.combinedLevelTex = MakeRenderTex(Mathf.RoundToInt(size.x * Options.ResolutionScale.Value),
                    Mathf.RoundToInt(size.y * Options.ResolutionScale.Value));

                t.intermediateTex.Release();
                t.intermediateTex = MakeRenderTex(t.combinedLevelTex.width, t.combinedLevelTex.height);
                
                Logger.LogDebug($"Upscaled level texture to {t.combinedLevelTex.width}x{t.combinedLevelTex.height}");
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    //Weird SBCameraScroll practices...
    private static Texture LevTex(RoomCamera self) => SBCameraScrollEnabled ? self.levelGraphic?._atlas?.texture : self.levelTexture;


    //public static float2 CamPos;
    public static Dictionary<int, float2> CamPos = new(4);
    public static Dictionary<int, Vector2> MidpointWarp = new(4);

    //Sets the CamPos
    public void SetCamPos(RoomCamera self, float moveMod = 1)
    {
        Vector2 pos = new(0.5f, 0.5f);

        if (!CamPos.ContainsKey(self.cameraNumber))
            CamPos.Add(self.cameraNumber, new(0.5f, 0.5f));

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
        CamPos[self.cameraNumber] += moveMod * Options.CameraMoveSpeed.Value
            * (new float2(pos.x, pos.y) - CamPos[self.cameraNumber]);
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
    //private void RoomCamera_GetCameraBestIndex(On.RoomCamera.orig_GetCameraBestIndex orig, RoomCamera self)
    private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    {
        orig(self, timeStacker, timeSpeed);

        SetCamPos(self, 0.5f * timeSpeed);
    }

    private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
    {
        orig(self);

        SetCamPos(self, 0.5f);

        WarpBackgroundAndSnow(self);
    }

    private Vector2 GetMidpointWarp(int cameraNumber)
    {
        if (!MidpointWarp.ContainsKey(cameraNumber)) MidpointWarp.Add(cameraNumber, CalculateWarp(new(0.5f, 0.5f), CamPos[cameraNumber]));
        return MidpointWarp[cameraNumber];
    }

    //Saves an unwarped version of the snow texture whenever a new snow texture is created
    private void RoomCamera_UpdateSnowLight(On.RoomCamera.orig_UpdateSnowLight orig, RoomCamera self)
    {
        orig(self);

        //Save the new snow texture, so I don't accidentally overwrite it...
        if (Options.WarpSnow.Value)
        {
            //scale snow texture for SBCameraScroll
            if (OrigSnowTexture == null || self.SnowTexture.width != OrigSnowTexture.width || self.SnowTexture.height != OrigSnowTexture.height)
            {
                OrigSnowTexture?.Release();
                OrigSnowTexture = MakeRenderTex(self.SnowTexture.width, self.SnowTexture.height);
                //IntermediateSnowTex?.Release();
                //IntermediateSnowTex = new(self.SnowTexture.width, self.SnowTexture.height, 0, DefaultFormat.LDR) { filterMode = 0 };
            }
            Graphics.Blit(self.SnowTexture, OrigSnowTexture);
        }
    }

    //Background texture preloading
    private void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera self, Room room, int camPos)
    {
        string qt = self.quenedTexture;

        orig(self, room, camPos);

        //There must have been a change
        if (self.quenedTexture != qt && Options.PreLoadBackgroundTextures.Value && Options.ShaderLayers.Value >= 2)
        {
            string name = room.abstractRoom.name + "_" + (camPos + 1);
            if (!LayerRooms.Contains(name)) //don't preload if it's already cached
            {
                PreLoadedLayer2 = PreLoadBackgroundTexture(name + "_l2");
                if (Options.ShaderLayers.Value >= 3)
                    PreLoadedLayer3 = PreLoadBackgroundTexture(name + "_l3");
            }
        }
    }

    private void RoomCamera_MoveCamera2(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera self, string roomName, int camPos)
    {
        bool applyPos = self.applyPosChangeWhenTextureIsLoaded;

        orig(self, roomName, camPos);

        //There must have been a change
        if (self.applyPosChangeWhenTextureIsLoaded && self.applyPosChangeWhenTextureIsLoaded != applyPos && Options.PreLoadBackgroundTextures.Value && Options.ShaderLayers.Value >= 2)
        {
            string name = roomName + "_" + (camPos + 1);
            if (!LayerRooms.Contains(name)) //don't preload if it's already cached
            {
                PreLoadedLayer2 = PreLoadBackgroundTexture(name + "_l2");
                if (Options.ShaderLayers.Value >= 3)
                    PreLoadedLayer3 = PreLoadBackgroundTexture(name + "_l3");
            }
        }
    }

    //Preload all textures in the world
    private void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
    {
        if (Options.PreLoadWorld.Value && Options.ShaderLayers.Value >= 2 && self.abstractRooms.Count > 2)
        {
            try
            {
                //This overly complex logic is done to arrange rooms in an order from closest to farthest from the player
                //so that if, say, only half of rooms are generated, at least it's the closest half.

                List<string> screens = new();
                List<int> roomsFound = new(), tempRooms = new();
                List<bool> roomSearched = new();
                //foreach (string[] r in self.roomAdder)
                bool anyFound = true;
                AbstractRoom firstRoom;
                if (!self.singleRoomWorld && self.game.IsStorySession)
                {
                    //var saveState = (self.game.session as StoryGameSession).saveState;
                    firstRoom = (self.abstractRooms.Find(room => room.name == self.game.overWorld.reportBackToGate?.room?.abstractRoom?.name) //gates
                        ?? (self.abstractRooms.Find(room => room.name == (self.game.session as StoryGameSession).saveState.GetSaveStateDenToUse()) //waking up in a shelter
                        ?? self.abstractRooms.Find(room => room.name == self.game.manager.menuSetup.regionSelectRoom))) //for fast travel
                        ?? self.abstractRooms[0];
                }
                else firstRoom = self.abstractRooms[0];

                Logger.LogDebug("Starting room: " + firstRoom.name);
                roomsFound.Add(firstRoom.index - self.world.firstRoomIndex);
                roomSearched.Add(false);

                while (anyFound)
                {
                    anyFound = false;
                    //foreach (int i in roomsFound)
                    for (int i = 0; i < roomsFound.Count; i++)
                    {
                        if (!roomSearched[i]) //don't search rooms that we've already searched
                        {
                            AbstractRoom r = self.abstractRooms[roomsFound[i]];
                            foreach (int c in r.connections)
                            {
                                if (c >= 0)
                                {
                                    int idx = c - self.world.firstRoomIndex;
                                    if (!roomsFound.Contains(idx) && !tempRooms.Contains(idx))
                                    {
                                        tempRooms.Add(idx);
                                        roomSearched.Add(false);
                                        anyFound = true;
                                    }
                                }
                            }
                            roomSearched[i] = true;
                        }
                    }
                    roomsFound.AddRange(tempRooms);
                    tempRooms.Clear();
                }
                //convert rooms to screens
                //foreach (var room in roomsFound)
                foreach (int idx in roomsFound)
                {
                    //string r = room.name;
                    string r = self.abstractRooms[idx].name;
                    if (SBCameraScrollEnabled && WorldLoader.FindRoomFile(r, false, $"_2.png") != null)
                        continue; //only pre-generate 1-screen rooms for SBCameraScroll
                    for (int i = 1; WorldLoader.FindRoomFile(r, false, $"_{i}.png") != null; i++)
                        screens.Add(r + "_" + i);
                }
                roomsFound.Clear();
                roomSearched.Clear();

                Logger.LogDebug($"Creating batch background textures for {self.worldName}: {screens.Count} rooms");
                StartCoroutine(GenerateForAllRooms(screens));
            }
            catch (Exception ex) { Logger.LogError(ex); }
        }

        //Okay, now continue with your loading business!
        orig(self);
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
                        Vector2 warp = clampedDepthCurve((0.3f * data.fromDepth + 0.7f * data.toDepth - 5f) * 0.04f) * CalculateWarp(localVert / rCam.sSize, pos);

                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i, localVert + warp);
                    }
                }
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }


    private bool shouldWarpSnow = true;
    //Attempts to warp background image, which I'm not sure is ever even used...
    //ALSO warps snow texture
    //private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    private void WarpBackgroundAndSnow(RoomCamera self)
    {
        //orig(self, timeStacker, timeSpeed);

        //warp background
        if (Options.BackgroundWarp.Value != 0 && self.backgroundGraphic.isVisible && CamPos.ContainsKey(self.cameraNumber))
        {
            
            Vector2 warp = Options.BackgroundWarp.Value * GetMidpointWarp(self.cameraNumber);

            self.backgroundGraphic.x = self.backgroundGraphic.x - warp.x;
            self.backgroundGraphic.y = self.backgroundGraphic.y - warp.y;
        }

        //warp snow
        if (Options.WarpSnow.Value && OrigSnowTexture != null && Shader.IsKeywordEnabled("SNOW_ON"))
        {
            if (shouldWarpSnow) //every other frame
            {
                Shader.EnableKeyword("THELAZYCOWBOY1_WARPMAINTEX");
                Graphics.Blit(OrigSnowTexture, self.SnowTexture, ParallaxMaterial);
                Shader.DisableKeyword("THELAZYCOWBOY1_WARPMAINTEX");
            }
            shouldWarpSnow = !shouldWarpSnow;

            //if (!SnowShaderActive || SnowWarpFence.passed)
                //Graphics.ExecuteCommandBufferAsync(SnowWarpBuffer, ComputeQueueType.Background);
            //SnowShaderActive = true;
        }
        //else
            //SnowShaderActive = false;
    }

    //Offsets the camera position used for backgrounds, helping to provide basic visual continuity
    private Vector2 BackgroundScene_DrawPos(On.BackgroundScene.orig_DrawPos orig, BackgroundScene self, Vector2 pos, float depth, Vector2 camPos, float hDisplace)
    {
        if (Options.BackgroundWarp.Value != 0)
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
                camPos -= Options.BackgroundWarp.Value
                    * (self is RoofTopView ? CalculateWarp(new(0.5f, 0.5f), CamPos[lowestCam.cameraNumber] + new float2(0, 0.4f)) //raise up pos for RoofTopView
                    : GetMidpointWarp(lowestCam.cameraNumber));
            }
        }

        if (self is RoofTopView)
            pos.y -= 0.4f * Options.Warp.Value * (Options.InvertPos.Value ? -1f : 1f); //fixes the roof not lining up right... hopefully

        return orig(self, pos, depth, camPos, hDisplace);
    }

    //Warps the convergence point for backgrounds, giving a severe parallax effect
    private void BackgroundScene_Update(On.BackgroundScene.orig_Update orig, BackgroundScene self, bool eu)
    {
        orig(self, eu);

        if (Options.BackgroundRotation.Value != 0)
        {
            var cam = self.room.game.cameras.FirstOrDefault(c => c.room == self.room);
            if (cam != null && CamPos.ContainsKey(cam.cameraNumber))
            {
                //reset convergence point in case I messed it up previously. Adapted from decompiled code
                Vector2 properMidpoint = new Vector2(0.5f, (ModManager.DLCShared && self.room.waterInverted ? 1f : 2f) / 3f);
                self.convergencePoint = self.room.game.rainWorld.screenSize * properMidpoint;

                self.convergencePoint -= Options.BackgroundRotation.Value * GetMidpointWarp(cam.cameraNumber);//GetMidpointWarp(cam.cameraNumber);
            }
        }
    }


    //Same as above, but apparently Outer Rim overwrites its inherited function for no good reason
    private Vector2 OuterRimView_DrawPos(On.Watcher.OuterRimView.orig_DrawPos orig, OuterRimView self, BackgroundScene.BackgroundSceneElement element, Vector2 camPos, RoomCamera camera)
    {
        if (Options.BackgroundWarp.Value != 0 && CamPos.ContainsKey(camera.cameraNumber))
            camPos -= Options.BackgroundWarp.Value * GetMidpointWarp(camera.cameraNumber);

        return orig(self, element, camPos, camera);
    }

    //Warps the convergence point for Outer Rim
    private void OuterRimView_Update(On.Watcher.OuterRimView.orig_Update orig, OuterRimView self, bool eu)
    {
        orig(self, eu);

        if (Options.BackgroundRotation.Value != 0)
        {
            var cam = self.room.game.cameras.FirstOrDefault(c => c.room == self.room);
            if (cam != null && CamPos.ContainsKey(cam.cameraNumber))
            {
                //reset convergence point in case I messed it up previously. Adapted from decompiled code
                self.convergencePoint = self.room.game.rainWorld.screenSize * OuterRimView.ConvergenceMult;

                self.convergencePoint -= Options.BackgroundRotation.Value * GetMidpointWarp(cam.cameraNumber);
            }
        }
    }


    //Attempts to force all background elements to have some basic warp
    [Obsolete]
    private void BackgroundSceneElement_DrawSprites(On.BackgroundScene.BackgroundSceneElement.orig_DrawSprites orig, BackgroundScene.BackgroundSceneElement self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        //Applies some shift to elements that aren't normally shifted, like background images and (surprisingly) clouds
        if ((Options.BackgroundWarp.Value != 0 || Options.BackgroundRotation.Value != 0)
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
        if (Options.BackgroundWarp.Value != 0 && CamPos.ContainsKey(rCam.cameraNumber))
        {
            Vector2 newPos = pos - Options.BackgroundWarp.Value * GetMidpointWarp(rCam.cameraNumber);
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


    //Terrain Curves... yay...
    private void TerrainCurve_DrawSprites(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.After,
                //x => x.MatchLdarg(1),
                //x => x.MatchLdfld(typeof(RoomCamera.SpriteLeaser), nameof(RoomCamera.SpriteLeaser.sprites)),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchCastclass<TriangleMesh>(),
                x => x.MatchStloc(5)
                ))
            {
                c.Emit(OpCodes.Ldarg_0); //reference this
                c.Emit(OpCodes.Ldarg_2); //reference rCam
                c.EmitDelegate<Action<TerrainCurve, RoomCamera>>((curve, rCam) => {
                    if (Options.TerrainCurveWarp.Value > 0 && CamPos.TryGetValue(rCam.cameraNumber, out var camPos))
                    {
                        //Check which points are actually on-screen; don't warp off-screen points
                        //THIS PROBABLY COSTS AS MUCH AS SIMPLY WARPING EVERYTHING!!
                        /*int onScreenStart = Int32.MaxValue, onScreenStop = curve.drawSegments - 1;
                        for (int i = 0; i < curve.drawSegments; i++)
                        {
                            //Check if on screen
                            Vector2 front = curve.drawFrontPoints[i], back = curve.drawBackPoints[i];
                            if ((back.x >= 0 && back.x <= rCam.sSize.x && back.y >= 0 && back.y <= rCam.sSize.y)
                            || (front.x >= 0 && front.x <= rCam.sSize.x && front.y >= 0 && front.y <= rCam.sSize.y))
                            {
                                onScreenStart = Math.Min(onScreenStart, i);
                                onScreenStop = i + 1;
                            }
                        }

                        //Warp the on-screen points (or any other relevant ones)
                        for (int i = Math.Max(0, onScreenStart-1); i <= onScreenStop; i++)*/
                        float frontDepth = clampedDepthCurve((curve.minDepth - 5) * 0.04f);
                        for (int i = 0; i < curve.drawSegments; i++)
                        {
                            Vector2 frontWarp = Options.TerrainCurveWarp.Value * frontDepth * CalculateWarp(curve.drawFrontPoints[i] / rCam.sSize, camPos);
                            curve.drawBackPoints[i] -= Vector2.LerpUnclamped(frontWarp, Options.TerrainCurveWarp.Value * CalculateWarp(curve.drawBackPoints[i] / rCam.sSize, camPos), (30 * 0.04f - frontDepth) / (1 - frontDepth));
                            curve.drawFrontPoints[i] -= frontWarp;
                        }
                    }
                });

                Logger.LogDebug("TerrainCurve IL hook succeeded");
            }
            else
            {
                Logger.LogError("TerrainCurve IL hook failed");
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    private void TerrainCurveMaskSource_DrawSprites(On.TerrainCurveMaskSource.orig_DrawSprites orig, TerrainCurveMaskSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (Options.TerrainCurveWarp.Value > 0 && CamPos.TryGetValue(rCam.cameraNumber, out var playerPos)) {
            var m = sLeaser.maskSources[0]._mesh;

            float frontDepth = clampedDepthCurve((self.MinDepth - 5) * 0.04f);
            int start = Int32.MaxValue;
            for (int i = 0; i < self.vertices.Length; i += 3)
            {
                if (self.vertices[i].x - camPos.x >= -10)
                {
                    start = i; break;
                }
            }
            for (int i = start; i < self.vertices.Length; i += 3)
            {
                Vector2 frontWarp = Options.TerrainCurveWarp.Value * frontDepth * CalculateWarp((V2FromV3(self.vertices[i + 1]) - camPos) / rCam.sSize, playerPos);
                m.vertices[i] = TerrainCurveMaskSource.To3D(V2FromV3(self.vertices[i]) + frontWarp, 0);
                m.vertices[i + 1] = TerrainCurveMaskSource.To3D(V2FromV3(self.vertices[i + 1]) + frontWarp, 0);

                Vector2 backWarp = Vector2.LerpUnclamped(frontWarp, Options.TerrainCurveWarp.Value * CalculateWarp((V2FromV3(self.vertices[i + 2]) - camPos) / rCam.sSize, camPos), (30 * 0.04f - frontDepth) / (1 - frontDepth));
                m.vertices[i + 2] = TerrainCurveMaskSource.To3D(V2FromV3(self.vertices[i + 2]) + backWarp, 1);

                if (self.vertices[i].x - camPos.x > rCam.sSize.x + 10) break;
            }
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);
    }
    private static Vector2 V2FromV3(Vector3 v3) => new(v3.x, v3.y);


    //Actual calculations

    private float clampedDepthCurve(float d)
    {
        return (d <= Options.StartOffset.Value) ? d : depthCurve(d);//Mathf.Clamp(depthCurve(d), Options.StartOffset.Value, 1);
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
        float clamp = Options.DynamicOptimization.Value ? 1 : Options.MaxWarpFactor.Value;
        return Options.Warp.Value * new Vector2(
            Mathf.Clamp(warp.x, -clamp, clamp),
            Mathf.Clamp(warp.y, -clamp, clamp)
            );
    }
    #endregion

}
