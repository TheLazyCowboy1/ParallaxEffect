using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ParallaxEffect;

public class ConfigOptions : OptionInterface
{
    private static readonly string[] smoothingValues = new string[] { "EXTREME", "SINUSOIDAL", "LINEAR", "INVERSE" };
    private static readonly string[] depthCurveValues = new string[] { "EXTREME", "PARABOLIC", "LINEAR", "INVERSE" };
    public ConfigOptions()
    {
        Warp = this.config.Bind<float>("Warp", 25, new ConfigAcceptableRange<float>(-500, 500));
        //AdvancedShader = this.config.Bind<bool>("AdvancedShader", true);
        //ThirdLayer = this.config.Bind<bool>("ThirdLayer", false);
        //MaxWarp = this.config.Bind<float>("MaxWarp", 20f, new ConfigAcceptableRange<float>(0, 100f));
        MaxWarpFactor = this.config.Bind<float>("MaxWarpFactor", 0.9f, new ConfigAcceptableRange<float>(0.1f, 1));
        Optimization = this.config.Bind<float>("Optimization", 1, new ConfigAcceptableRange<float>(0.2f, 10));
        DynamicOptimization = this.config.Bind<bool>("DynamicOptimization", true);
        StartOffset = this.config.Bind<float>("StartOffset", -0.1f, new ConfigAcceptableRange<float>(-1, 0));
        EndOffset = this.config.Bind<float>("EndOffset", 1f, new ConfigAcceptableRange<float>(0.1f, 2f));
        RedModScale = this.config.Bind<float>("RedModScale", 0.85f, new ConfigAcceptableRange<float>(0, 10));
        BackgroundScale = this.config.Bind<float>("BackgroundScale", 1, new ConfigAcceptableRange<float>(-10, 10));
        AntiAliasing = this.config.Bind<float>("AntiAliasing", 0.2f, new ConfigAcceptableRange<float>(0, 1));
        ClosestPixelOnly = this.config.Bind<bool>("ClosestPixelOnly", false);
        MaxXDistance = this.config.Bind<float>("MaxXDistance", 0.2f, new ConfigAcceptableRange<float>(0, 2));
        InvertPos = this.config.Bind<bool>("InvertPos", false);
        NoCenterWarp = this.config.Bind<bool>("NoCenterWarp", false);
        AlwaysCentered = this.config.Bind<bool>("AlwaysCentered", false);
        CameraMoveSpeed = this.config.Bind<float>("CameraMoveSpeed", 0.1f, new ConfigAcceptableRange<float>(0, 1));
        WarpDecals = this.config.Bind<bool>("WarpDecals", true);
        WarpSnow = this.config.Bind<bool>("WarpSnow", true);
        //WarpTerrainCurves = this.config.Bind<bool>("WarpTerrainCurves", true);
        TerrainCurveWarp = this.config.Bind<float>("TerrainCurveWarp", 1, new ConfigAcceptableRange<float>(-5, 5));
        BackgroundWarp = this.config.Bind<float>("BackgroundWarp", 1, new ConfigAcceptableRange<float>(-5, 5));
        BackgroundRotation = this.config.Bind<float>("BackgroundRotation", 1, new ConfigAcceptableRange<float>(-5, 5));
        MouseSensitivity = this.config.Bind<float>("MouseSensitivity", 1f, new ConfigAcceptableRange<float>(-10, 10));
        ResolutionScaleEnabled = this.config.Bind<bool>("ResolutionScaleEnabled", false);
        ResolutionScale = this.config.Bind<float>("ResolutionScale", 1, new ConfigAcceptableRange<float>(0.5f, 5));
        SmoothingType = this.config.Bind<string>("SmoothingType", "SINUSOIDAL", new ConfigAcceptableList<string>(smoothingValues));
        DepthCurve = this.config.Bind<string>("DepthCurve", "PARABOLIC", new ConfigAcceptableList<string>(depthCurveValues));

        ShaderLayers = this.config.Bind<int>("ShaderLayers", 1, new ConfigAcceptableRange<int>(1, 3));
        SimplerBackgrounds = this.config.Bind<bool>("SimplerBackgrounds", true);
        DepthScale = this.config.Bind<float>("DepthScale", 0.5f, new ConfigAcceptableRange<float>(0, 30));
        MinObjectDepth = this.config.Bind<float>("MinObjectDepth", 3, new ConfigAcceptableRange<float>(-30, 30));
        CachedRenderTextures = this.config.Bind<int>("CachedRenderTextures", 4, new ConfigAcceptableRange<int>(1, 20));
        PreLoadBackgroundTextures = this.config.Bind<bool>("PreLoadBackgroundTextures", true);
        SaveBackgroundTextures = this.config.Bind<bool>("SaveBackgroundTextures", true);
        OverwriteBackgroundTextures = this.config.Bind<bool>("OverwriteBackgroundTextures", false);
        PreLoadWorld = this.config.Bind<bool>("PreLoadWorld", false);
        PreLoadRoomCap = this.config.Bind<int>("PreLoadRoomCap", 50, new ConfigAcceptableRange<int>(1, 1000));
    }

    //General
    //public readonly Configurable<float> GhostFright;
    public readonly Configurable<float> Warp;
    //public readonly Configurable<float> MaxWarp;
    public readonly Configurable<float> MaxWarpFactor;
    public readonly Configurable<float> Optimization;
    public readonly Configurable<bool> DynamicOptimization;
    public readonly Configurable<float> StartOffset;
    public readonly Configurable<float> EndOffset;
    public readonly Configurable<float> RedModScale;
    public readonly Configurable<float> BackgroundScale;
    public readonly Configurable<float> AntiAliasing;
    public readonly Configurable<bool> ClosestPixelOnly;
    public readonly Configurable<float> MaxXDistance;
    public readonly Configurable<bool> InvertPos;
    public readonly Configurable<bool> NoCenterWarp;
    public readonly Configurable<bool> AlwaysCentered;
    public readonly Configurable<float> CameraMoveSpeed;
    public readonly Configurable<bool> WarpDecals;
    public readonly Configurable<bool> WarpSnow;
    //public readonly Configurable<bool> WarpTerrainCurves;
    public readonly Configurable<float> TerrainCurveWarp;
    public readonly Configurable<bool> WarpTerrainCurveMask = new(true);
    public readonly Configurable<float> BackgroundWarp;
    public readonly Configurable<float> BackgroundRotation;
    public readonly Configurable<float> MouseSensitivity;
    public readonly Configurable<bool> ResolutionScaleEnabled;
    public readonly Configurable<float> ResolutionScale;
    public readonly Configurable<string> SmoothingType;
    public readonly Configurable<string> DepthCurve;

    public readonly Configurable<int> ShaderLayers;
    //public readonly Configurable<bool> AdvancedShader;// = new(true);
    public readonly Configurable<bool> SimplerBackgrounds;
    public readonly Configurable<float> DepthScale;
    public readonly Configurable<float> MinObjectDepth;
    //public readonly Configurable<bool> ThirdLayer;// = new(false);
    public readonly Configurable<int> CachedRenderTextures;
    public readonly Configurable<bool> PreLoadBackgroundTextures;// = new(true);
    public readonly Configurable<bool> SaveBackgroundTextures;// = new(true);
    public readonly Configurable<bool> OverwriteBackgroundTextures;// = new(true);
    public readonly Configurable<bool> PreLoadWorld;// = new(true);
    public readonly Configurable<int> PreLoadRoomCap;

    private OpCheckBox limitExtendCheckbox;
    private OpUpdown limitExtendUpdown;

    private OpCheckBox resolutionScaleCheckbox;
    private OpUpdown resolutionScaleUpdown;

    public override void Initialize()
    {
        //var optionsTab = new OpTab(this, "Options");
        OpTab basicTab = new(this, "Basics"),
            movementTab = new(this, "Movement"),
            advancedTab = new(this, "Advanced"),
            layersTab = new(this, "Layers"),
            experimentalTab = new(this, "Experiments");
        this.Tabs = new[]
        {
            basicTab, movementTab, advancedTab, layersTab, experimentalTab
        };

        //Calculate optimal Resolution Scales
        float[] displayScales = new float[Display.displays.Length];
        int mainIdx = 0;
        string displayScaleString = "";
        for (int i = 0; i < displayScales.Length; i++)
        {
            displayScales[i] = Mathf.Round(Mathf.Max(
                Display.displays[i].systemWidth / Custom.rainWorld.options.ScreenSize.x,
                Display.displays[i].systemHeight / Custom.rainWorld.options.ScreenSize.y
                ) * 100f) * 0.01f;
            if (Display.displays[i] == Display.main)
                mainIdx = i;

            if (i > 0) displayScaleString += ", or ";
            displayScaleString += $"{displayScales[i]} ({Display.displays[i].systemHeight}p)";
        }

        float t = 150f, y = 550f, h = -40f, H = -70f, x = 50f, w = 80f, c = 50f;
        float t2 = 400f, x2 = 300f;

        basicTab.AddItems(
            new OpLabel(x, y, "BASIC SETTINGS", true),

            new OpLabel(t, y += H, "Shader Layers"),
            new OpUpdown(ShaderLayers, new(x, y), w) { description = "How many layers the shader generates/uses.\nThis is done by a separate shader creating background layers that are fed to the parallax shader." }
            );
        const float scrollBoxHeight = 200;
        var layersDescription = new OpLabelLong(new(10, 10), new(500 - 20, 500),
            "Short Answer:\n" +
            "1 Layer == Fast, but poles and plants look stretched.\n" +
            "2 Layers == Medium, but intersecting poles/plants can have artifacts.\n" +
            "3 Layers == VERY slow, but everything usually looks great.\n" + 
            "2 layers is highly recommended if your GPU can handle it! However, 2 layers is roughly twice as expensive as 1.\n\n" +
            "1 Shader Layer: The fastest but most limited option. Here, the shader only uses the information given to it: The level texture. This is a flat 2D image, so it has no clue what is \"behind\" things like poles, so it assumes everything extends back indefinitely.\n\n" +
            "2 Shader Layers: Upon switching screens, a separate shader generates a second \"background texture\" that tries to guess what is behind poles and such. This can have excellent results, but it suffers from having both to complexly generate a new level image and THEN process twice as much data, so it's almost twice as slow.\n\n" +
            "3 Shader Layers: Mostly the same as 2 layers, except a third layer is generated (using the info from the first two layers). This reduces some visual oddities with 2 layers, but makes generating the background textures take roughly 5 times longer, and the parallax shader itself is slightly slower."
            )
        { verticalAlignment = OpLabel.LabelVAlignment.Bottom };

        var layersScrollBox = new OpScrollBox(new Vector2(x, y += 0.5f*h - scrollBoxHeight), new Vector2(500 + 20, scrollBoxHeight), layersDescription.GetDisplaySize().y + 20);
        basicTab.AddItems(layersScrollBox);
        layersScrollBox.AddItems(layersDescription);
        basicTab.AddItems(
            new OpLabel(t, y += H, "Warp Factor"),
            new OpUpdown(Warp, new(x, y), w, 1) { description = "The amount in pixels (roughly) to warp the texture by.\nIn short: The strength of the parallax effect." },
            new OpLabel(t, y += h, "Optimization"),
            new OpUpdown(Optimization, new(x, y), w, 2) { description = "Used to reduce the number of calculations, at the risk of visual artifacts (especially on small plants).\n1.00 = unoptimized, 2.00 = reasonably stable. Below 1 will severely increase lag with nominal visual difference." },
                new OpLabel(t2, y, "Dynamic Optimization"),
                new OpCheckBox(DynamicOptimization, x2 + c, y) { description = "Dynamically scales the optimization so that pixels closer to the player are cheaper.\nPotentially causes minor visual artifacts while moving, but usually saves A LOT of processing." },
            new OpLabel(t, y += h, "Max Warp"),
            new OpUpdown(MaxWarpFactor, new(x, y), w, 2) { description = "The maximum amount by which the texture can be warped, expressed as a fraction of the Warp Factor.\nUseful for performance reasons: lower values = better performance." }
            );

        y = 550;
        movementTab.AddItems(
            new OpLabel(x, y, "MOVEMENT SETTINGS", true),
                new OpLabel(x2, y, "How the screen (and a few specific objects) move."),

            new OpLabel(t, y += H, "Invert Position"),
            new OpCheckBox(InvertPos, x + c, y) { description = "Makes the warp stronger closer to the player, and weaker further away.\nThis is more visually realistic, but not ideal for gameplay." },
            new OpLabel(t, y += h, "Minimize Warp when Centered"),
            new OpCheckBox(NoCenterWarp, x + c, y) { description = "NOT RECOMMENDED: SET BACKGROUND SCALE TO 0.00 INSTEAD. Makes the warp factor scale according to how far away the player is from the center of the screen.\nThis means that the room will look mostly unwarped when the player is in the center of the room. However, it can look quite unnatural due to the non-linear camera movement." },
            new OpLabel(t, y += h, "Don't Follow Player"),
            new OpCheckBox(AlwaysCentered, x + c, y) { description = "Always assumes the player is exactly in the center of the screen.\nRecommended for SBCameraScroll; otherwise this is pretty pointless. Might help motion-sensitive folks?" },
            new OpLabel(t, y += h, "Camera Move Speed"),
            new OpUpdown(CameraMoveSpeed, new(x, y), w, 2) { description = "How quickly the camera adjusts to position changes.\n1.00 = instantaneously; 0.00 = not at all; 0.10 = slowly." },
            new OpLabel(t, y += h, "Mouse Sensitivity"),
            new OpUpdown(MouseSensitivity, new(x, y), w, 2) { description = "The sensitivity with which mouse movements alter the camera position.\nSet to 0.00 to disable this feature entirely." },
            
                new OpLabel(x, y += h, "Optionally Warped Objects:"),
            new OpLabel(t, y += h, "Warp Decals"),
            new OpCheckBox(WarpDecals, x + c, y) { description = "Also warps decals added through dev tools. These are common occurrences.\nNOTE: Runs on the CPU, not on the GPU!! If there are a lot of decals, this will increase lag." },
            new OpLabel(t, y += h, "Warp Snow"),
            new OpCheckBox(WarpSnow, x + c, y) { description = "Warps snow, but comes with a SEVERE performance cost in snowy rooms.\nHighly recommended unless your GPU can't handle it or Warp Factor is set very low." },
            new OpLabel(t, y += h, "Warp Terrain Curves"),
            //new OpCheckBox(WarpTerrainCurves, x + c, y) { description = "Warps terrain curves (the curved terrain from the Watcher's campaign) in a somewhat crude fashion.\nThe cost is higher than I'd like, but not too bad." },
            new OpUpdown(TerrainCurveWarp, new(x, y), w, 2) { description = "Warps terrain curves (the curved terrain from the Watcher's campaign) in a somewhat crude fashion.\nThe cost is higher than I'd like, but not too bad." },
            new OpLabel(t, y += h, "Background Offset"),
            new OpUpdown(BackgroundWarp, new(x, y), w, 2) { description = "How much to offset the background graphic, in order to make it more consistent with the parallax effect. Expressed as a fraction of the Warp Factor.\nSet to 0.00 to disable. 1.00 == same as deepest room object." },
            new OpLabel(t, y += h, "Background Rotation"),
            new OpUpdown(BackgroundRotation, new(x, y), w, 2) { description = "Warps the center-point of the in-game parallax effect for backgrounds, thus making them somewhat appear to rotate.\nSet to 0.00 to disable. 1.00 == same as the Warp Factor." }
            );

        y = 550;
        advancedTab.AddItems(
            new OpLabel(x, y, "ADVANCED SETTINGS", true),
                new OpLabel(x2, y, "These are pretty powerful! (But complicated)"),

            new OpLabel(t, y += H, "Minimum Warp"),
            new OpUpdown(StartOffset, new(x, y), w, 2) { description = "The lowest amount by which pixels can be warped. If set < 0, allows objects to \"project into the screen,\" which looks like more realistic rotation but might be annoying.\nI recommend setting it >= -0.20. Below -0.20 only has an effect for EXTREME or PARABOLIC depth curves. 0.00 disables \"projecting into the screen.\"" },
                new OpLabel(t2, y, "[ADVANCED] Maximum Tested Warp"),
                new OpUpdown(EndOffset, new(x2, y), w, 2) { description = "ADVANCED: Keep this at 1.00. In theory, raising it above 1.00 only increases lag, and lowering it from 1.00 creates visual artifacts.\nWhy is this even an option, then? Um, good question." },
            new OpLabel(t, y += h, "Projection Fade"),
            new OpUpdown(RedModScale, new(x, y), w, 2) { description = "Scales how quickly warped things \"fade into the background.\" It is recommended to set < 1 for PARABOLIC and EXTREME depth curves.\nLow values = objects appear thick; High values = objects aren't \"projected backwards\" as noticably (they fade faster)." },
            new OpLabel(t, y += h, "Convergence Scale"),
            new OpUpdown(BackgroundScale, new(x, y), w, 2) { description = "How much to converge (shrink) the back of the room towards the camera location. Basically, higher values = back wall looks farther away.\n0.00 = Keeps vanilla proportions (and doesn't warp near the center) (but can look jittery due to aliasing). 1.00 = Makes things look deeper (and doesn't warp near the player)." },
            new OpLabel(t, y += h, "Anti-Aliasing"),
            new OpUpdown(AntiAliasing, new(x, y), w, 2) { description = "Attempts some form of anti-aliasing by randomly offsetting some pixels to make moving edges less straight.\nRecommended to keep BELOW 0.50, unless Convergence Scale is near 0.00." },
            
            new OpLabel(t + w - 20f, y += H, "Distance Curve"),
                new OpLabel(t2 + w + 20f, y, "Depth Curve"), //re-arranged like this because the lists are actually lower down
            new OpListBox(SmoothingType, new(x - 20f, y -= 90f), w + w, smoothingValues) { description = "Makes the parallax effect more noticable by warping (horizontally) closer objects more. SINUSOIDAL produces the most pleasing and realistic result.\nLINEAR is the most optimized. INVERSE reduces the warping around the player, which might be nice for gameplay." },
                new OpListBox(DepthCurve, new(x2 + 20f, y), w + w, depthCurveValues) { description = "EXTREME and PARABOLIC make shallower (closer) objects be warped more. INVERSE makes the effect negligible on shallow objects.\nIf LINEAR (fastest and most visually accurate setting), the deepest background is warped 5x more than the back wall, which I thought was too drastic." }
            );

        y = 550;
        OpHoldButton clearButton;
        layersTab.AddItems(
            new OpLabel(x, y, "EXTRA LAYERS SETTINGS", true),
                new OpLabel(x2, y, "For 2+ Shader Layers"),

            new OpLabel(t, y += H, "Simpler Layers"),
            new OpCheckBox(SimplerBackgrounds, x + c, y) { description = "Generates layers almost twice as fast, but they might look slightly worse (it's not very noticeable)." },
            new OpLabel(t, y += h, "Minimum Object Depth"),
            new OpUpdown(MinObjectDepth, new(x, y), w, 2) { description = "How far the thinnest, 1-pixel-wide plant should extend backwards.\nShould probably be at least 1. If this value is too low, the shader might treat objects like large pipes as if they were several layers of poles. It can look weird." },
            new OpLabel(t, y += h, "Depth Scale"),
            new OpUpdown(DepthScale, new(x, y), w, 2) { description = "Multiplies how far back objects extend due to their thickness.\nE.g (Depth Scale = 1): If a pipe is 5x thicker than a pole, it should extend back 5x as far." },
            
            new OpLabel(t, y += H, "Cached Textures"),
            new OpUpdown(CachedRenderTextures, new(x, y), w) { description = "How many textures to keep readily accessible in memory, so that going back to previous rooms can be an instantaneous switch.\nKeeping this low is helpful to not clog up your graphics memory." },
            new OpLabel(t, y += h, "Load Saved Textures"),
            new OpCheckBox(PreLoadBackgroundTextures, x + c, y) { description = "Searches for layer textures that have already been saved as a file. Loading these is far faster than generating new ones.\nI don't know why you would disable this. Keep it on." },
            new OpLabel(t, y += h, "Save Generated Textures"),
            new OpCheckBox(SaveBackgroundTextures, x + c, y) { description = "Save textures to a file when generated, so that they can be loaded when the room is re-entered. This only takes a split second.\nDisabling this might save a few milliseconds when generating textures, but comes at the cost of having to re-generate the textures each time." },
            new OpLabel(t, y += h, "Overwrite Saved Textures"),
            new OpCheckBox(OverwriteBackgroundTextures, x + c, y) { description = "Ignores previously saved layer textures, regenerating them instead. However, it will not overwrite textures made in the same cycle.\nUseful only if you have changed Minimum Object Depth or Depth Scale and want those changes to apply to previous rooms." },
            
            new OpLabel(t, y += H, "Batch Generate Region Textures"),
            new OpCheckBox(PreLoadWorld, x + c, y) { description = "Generates all the layer textures for each room in the region upfront. This can take a minute (literally) for 2 shader layers, or several minutes for 3 shader layers.\nPros: Once it's done, your gameplay should be smooth. Cons: Jarring; can take a LONG time for large regions." },
            new OpLabel(t, y += h, "Max Batch Size"),
            new OpUpdown(PreLoadRoomCap, new(x, y), w) { description = "The maximum number of screens that can be rendered in a single batch render. It prioritizes closer rooms, so setting this to 50 means that it only generates textures for the 50 closest rooms (if they don't have textures already).\nThis is important because if the batch size gets too large, it can crash your game. It also splits the process over multiple cycles." },

            clearButton = new OpHoldButton(new(x, 15), new Vector2(200, 50), "Clear Saved Textures") { colorFill = new(0.7f, 0.1f, 0.1f), colorEdge = new(0.7f, 0.1f, 0.1f), description = "Deletes all layer textures saved by this mod.\nUseful to clearing up disk space or applying changes to Minimum Object Depth or Depth Scale." }
            );
        clearButton.OnPressDone += ClearButton_OnPressDone;

        y = 550;
        experimentalTab.AddItems(
            new OpLabel(x, y, "EXPERIMENTS", true),
                new OpLabel(x2, y, "Fun? Useful? Needs testing? Yes!"),
            new OpLabel(t + c, y += H, "Limit Projection")
            );
        limitExtendCheckbox = new OpCheckBox(ClosestPixelOnly, x, y) { description = "EXPERIMENTAL: Disables the assumption that all objects extend indefinitely far back. Instead, the shader will find the \"closest pixel\" if an exact color cannot be found.\nThis inevitably leads to visual artifacts, and is more performance-intensive." };
        limitExtendUpdown = new OpUpdown(MaxXDistance, new(x + c, y), w, 2) { description = "EXPERIMENTAL: How far objects are projected backward. Applies to the LAST layer in the shader. If low, objects aren't stretched far into the background. If high, they are.\n0.00 == objects in the last layer are not projected backwards at all; >1 == objects in the last layer are projected backwards indefinitely; same as if disabled" };
        limitExtendUpdown.greyedOut = !ClosestPixelOnly.Value;
        limitExtendCheckbox.OnChange += () => { limitExtendUpdown.greyedOut = !limitExtendCheckbox.GetValueBool(); };
        resolutionScaleCheckbox = new OpCheckBox(ResolutionScaleEnabled, x, y += h) { description = "EXPERIMENTAL: Enables the experimental resolution scale option. This is compatible with SBCameraScroll (but can be very laggy!!!), but doesn't work as well with Sharpener." };
        resolutionScaleUpdown = new OpUpdown(ResolutionScale, new(x + c, y), w, 2) { description = "EXPERIMENTAL: Scales the resolution of the level texture to allow for finer details, but at a pretty big performance cost (especially with SBCameraScroll).\nYour recommended Resolution Scale ~= " + displayScaleString };
        resolutionScaleUpdown.greyedOut = !ResolutionScaleEnabled.Value;
        resolutionScaleCheckbox.OnChange += () => { resolutionScaleUpdown.greyedOut = !resolutionScaleCheckbox.GetValueBool(); };
        experimentalTab.AddItems(
            limitExtendCheckbox,
            limitExtendUpdown,
            new OpLabel(t + c, y, "Resolution Scale"),
            resolutionScaleCheckbox,
            resolutionScaleUpdown
            );

        if (!ResolutionScaleEnabled.Value)
        {
            ResolutionScale.Value = displayScales[mainIdx];
            ResolutionScale.defaultValue = displayScales[mainIdx].ToString();
            //resolutionScaleUpdown.SetValueFloat(displayScales[mainIdx]);
            resolutionScaleRecommendedValue = displayScales[mainIdx];
        }
    }

    private void ClearButton_OnPressDone(UIfocusable trigger)
    {
        try
        {
            Directory.Delete(Path.Combine(Plugin.ModFolderPath, "world"), true);
            Plugin.PublicLogger.LogDebug("Cleared layer textures.");
            trigger?.Menu?.PlaySound(SoundID.MENU_Continue_Game);
        }
        catch (Exception ex) { Plugin.PublicLogger.LogError(ex); }
    }

    private float resolutionScaleRecommendedValue = -1;

    public override void Update()
    {
        base.Update();

        /*if (limitExtendCheckbox != null)
            limitExtendUpdown.greyedOut = !limitExtendCheckbox.GetValueBool();

        if (resolutionScaleCheckbox != null)
            resolutionScaleUpdown.greyedOut = !resolutionScaleCheckbox.GetValueBool();
        */
        if (resolutionScaleRecommendedValue >= 0 && resolutionScaleUpdown != null && resolutionScaleUpdown.GetValueFloat() != resolutionScaleRecommendedValue)
        {
            resolutionScaleUpdown.SetValueFloat(resolutionScaleRecommendedValue);
            resolutionScaleRecommendedValue = -1;
        }
    }
}