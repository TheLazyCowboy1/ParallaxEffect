using BepInEx.Logging;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace ParallaxEffect;

public class ConfigOptions : OptionInterface
{
    private static readonly string[] smoothingValues = new string[] { "EXTREME", "SINUSOIDAL", "LINEAR", "INVERSE" };
    private static readonly string[] depthCurveValues = new string[] { "EXTREME", "PARABOLIC", "LINEAR", "INVERSE" };
    public ConfigOptions()
    {
        //AllSlugcats = this.config.Bind<bool>("AllSlugcats", false);
        Warp = this.config.Bind<float>("Warp", 25, new ConfigAcceptableRange<float>(2, 200));
        //MaxWarp = this.config.Bind<float>("MaxWarp", 20f, new ConfigAcceptableRange<float>(0, 100f));
        MaxWarpFactor = this.config.Bind<float>("MaxWarpFactor", 0.9f, new ConfigAcceptableRange<float>(0.1f, 1));
        Optimization = this.config.Bind<float>("Optimization", 1, new ConfigAcceptableRange<float>(0.2f, 10));
        DynamicOptimization = this.config.Bind<bool>("DynamicOptimization", true);
        StartOffset = this.config.Bind<float>("StartOffset", -0.1f, new ConfigAcceptableRange<float>(-1, 0));
        //EndOffset = this.config.Bind<float>("EndOffset", 1f, new ConfigAcceptableRange<float>(0.1f, 2f));
        RedModScale = this.config.Bind<float>("RedModScale", 0.85f, new ConfigAcceptableRange<float>(0f, 10));
        ClosestPixelOnly = this.config.Bind<bool>("ClosestPixelOnly", false);
        MaxXDistance = this.config.Bind<float>("MaxXDistance", 0.2f, new ConfigAcceptableRange<float>(0f, 2));
        InvertPos = this.config.Bind<bool>("InvertPos", false);
        NoCenterWarp = this.config.Bind<bool>("NoCenterWarp", false);
        AlwaysCentered = this.config.Bind<bool>("AlwaysCentered", false);
        CameraMoveSpeed = this.config.Bind<float>("CameraMoveSpeed", 0.1f, new ConfigAcceptableRange<float>(0, 1));
        WarpDecals = this.config.Bind<bool>("WarpDecals", true);
        BackgroundWarp = this.config.Bind<float>("BackgroundWarp", 1, new ConfigAcceptableRange<float>(0, 5));
        BackgroundRotation = this.config.Bind<float>("BackgroundRotation", 0.5f, new ConfigAcceptableRange<float>(0, 5));
        MouseSensitivity = this.config.Bind<float>("MouseSensitivity", 1f, new ConfigAcceptableRange<float>(0, 10));
        ResolutionScaleEnabled = this.config.Bind<bool>("ResolutionScaleEnabled", false);
        ResolutionScale = this.config.Bind<float>("ResolutionScale", 1, new ConfigAcceptableRange<float>(0.5f, 5));
        SmoothingType = this.config.Bind<string>("SmoothingType", "SINUSOIDAL", new ConfigAcceptableList<string>(smoothingValues));
        DepthCurve = this.config.Bind<string>("DepthCurve", "PARABOLIC", new ConfigAcceptableList<string>(depthCurveValues));
    }

    //General
    //public readonly Configurable<float> GhostFright;
    public readonly Configurable<float> Warp;
    //public readonly Configurable<float> MaxWarp;
    public readonly Configurable<float> MaxWarpFactor;
    public readonly Configurable<float> Optimization;
    public readonly Configurable<bool> DynamicOptimization;
    public readonly Configurable<float> StartOffset;
    public readonly Configurable<float> EndOffset = new(1.0f); //temporarily disabled, because it isn't useful
    public readonly Configurable<float> RedModScale;
    public readonly Configurable<bool> ClosestPixelOnly;
    public readonly Configurable<float> MaxXDistance;
    public readonly Configurable<bool> InvertPos;
    public readonly Configurable<bool> NoCenterWarp;
    public readonly Configurable<bool> AlwaysCentered;
    public readonly Configurable<float> CameraMoveSpeed;
    public readonly Configurable<bool> WarpDecals; //what about making this a config? we probably don't have to re-warp EVERY frame
    public readonly Configurable<float> BackgroundWarp;
    public readonly Configurable<float> BackgroundRotation;
    public readonly Configurable<float> MouseSensitivity;
    public readonly Configurable<bool> ResolutionScaleEnabled;
    public readonly Configurable<float> ResolutionScale;
    public readonly Configurable<string> SmoothingType;
    public readonly Configurable<string> DepthCurve;

    private OpCheckBox limitExtendCheckbox;
    private OpUpdown limitExtendUpdown;

    private OpCheckBox resolutionScaleCheckbox;
    private OpUpdown resolutionScaleUpdown;

    public override void Initialize()
    {
        var optionsTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            optionsTab
        };

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

        float t = 150f, y = 560f, h = -40f, x = 50f, w = 80f, c = 50f;
        float t2 = 400f, x2 = 300f;



        //General Options
        optionsTab.AddItems(
            new OpLabel(t, y, "Pixel Warp Factor"),
            new OpUpdown(Warp, new(x, y), w, 1) { description = "The amount in pixels (roughly) to warp the texture by." },
            new OpLabel(t, y += h, "Max Warp"),
            //new OpUpdown(MaxWarp, new(x, y), w, 1) { description = "The maximum amount in pixels (roughly) by which the texture can be warped. This should be <= the Warp Factor.\nUseful for performance reasons: lower values = better performance." },
            new OpUpdown(MaxWarpFactor, new(x, y), w, 2) { description = "The maximum amount by which the texture can be warped, expressed as a fraction of the Warp Factor.\nUseful for performance reasons: lower values = better performance." },
            new OpLabel(t, y += h, "Optimization"),
            new OpUpdown(Optimization, new(x, y), w, 2) { description = "Used to reduce the number of calculations, at the risk of visual artifacts (especially on small plants).\n1.00 = unoptimized, 2.00 = reasonably stable. Below 1 will severely increase lag with nominal visual difference." },
                new OpLabel(t2, y, "Dynamic Optimization"),
                new OpCheckBox(DynamicOptimization, x2 + c, y) { description = "Dynamically scales the optimization so that pixels closer to the player are cheaper.\nPotentially causes minor visual artifacts while moving, but usually saves A LOT of processing." },
            new OpLabel(t, y += h, "Minimum Warp"),
            new OpUpdown(StartOffset, new(x, y), w, 2) { description = "The lowest amount by which pixels can be warped. If set < 0, allows objects to \"project into the screen,\" which looks like more realistic rotation but might be annoying.\nI recommend setting it >= -0.20. Below -0.20 only has an effect for EXTREME or PARABOLIC depth curves. 0.00 disables \"projecting into the screen.\"" },
                //new OpLabel(t2, y, "[ADVANCED] Maximum Tested Warp"),
                //new OpUpdown(EndOffset, new(x2, y), w, 2) { description = "ADVANCED: Keep this at 1.00. In theory, raising it above 1.00 only increases lag, and lowering it from 1.00 creates visual artifacts.\nMight be useful to increase to ~1.05 if Don't Extend Objects is checked?" },
            new OpLabel(t, y += h, "Projection Steepness/Fade"),
            new OpUpdown(RedModScale, new(x, y), w, 2) { description = "Scales how quickly warped things \"fade into the background.\" It is recommended to set < 1 for PARABOLIC and EXTREME depth curves.\nLow values = objects appear thick; High values = objects aren't \"projected backwards\" as noticably (they fade faster)." },
            //new OpLabel(t, y += h, "[EXPERIMENTAL] Don't Extend Objects"),
            //new OpCheckBox(ClosestPixelOnly, x + c, y) { description = "EXPERIMENTAL: Disables the assumption that all objects extend indefinitely far back. Instead, the shader will find the \"closest pixel\" if an exact color cannot be found.\nThis inevitably has some obvious visual artifacts, and is MUCH more performance-intensive." },
            new OpLabel(t, y += h, "Invert Pos"),
            new OpCheckBox(InvertPos, x + c, y) { description = "Makes the warp stronger closer to the player, and weaker further away.\nThis is more visually realistic, but not ideal for gameplay." },
                new OpLabel(t2, y, "Don't Warp when Centered"),
                new OpCheckBox(NoCenterWarp, x2 + c, y) { description = "Makes the warp factor scale according to how far away the player is from the center of the screen.\nThis means that the room will look totally normal when the player is in the center of the room." },
            new OpLabel(t, y += h, "Don't Follow Player"),
            new OpCheckBox(AlwaysCentered, x + c, y) { description = "Always assumes the player is exactly in the center of the screen.\nRecommended for SBCameraScroll; otherwise this is pretty pointless. Might help motion-sensitive folks?" },
            new OpLabel(t, y += h, "Warp Decals"),
            new OpCheckBox(WarpDecals, x + c, y) { description = "Also warps decals added through dev tools. These are common occurrences.\nNOTE: Runs on the CPU, not on the GPU!! If there are a lot of decals, this will increase lag." },
            new OpLabel(t, y += h, "Background Offset"),
            new OpUpdown(BackgroundWarp, new(x, y), w, 2) { description = "How much to offset the background graphic, in order to make it more consistent with the parallax effect. Expressed as a fraction of the Warp Factor.\nSet to 0.00 to disable. 1.00 == same as deepest room object." },
                new OpLabel(t2, y, "Background Rotation"),
                new OpUpdown(BackgroundRotation, new(x2, y), w, 2) { description = "Warps the center-point of the in-game parallax effect for backgrounds, thus making them somewhat appear to rotate.\nSet to 0.00 to disable. 1.00 == same as the Warp Factor." },
            new OpLabel(t, y += h, "Camera Move Speed"),
            new OpUpdown(CameraMoveSpeed, new(x, y), w, 2) { description = "How quickly the camera adjusts to position changes.\n1.00 = instantaneously; 0.00 = not at all; 0.10 = slowly." },
                new OpLabel(t2, y, "Mouse Sensitivity"),
                new OpUpdown(MouseSensitivity, new(x2, y), w, 2) { description = "The sensitivity with which mouse movements alter the camera position.\nSet to 0.00 to disable this feature entirely." },
            new OpLabel(t + c, y += h, "[EXPERIMENTAL] Limit Projection")
            );
        limitExtendCheckbox = new OpCheckBox(ClosestPixelOnly, x, y) { description = "EXPERIMENTAL: Disables the assumption that all objects extend indefinitely far back. Instead, the shader will find the \"closest pixel\" if an exact color cannot be found.\nThis inevitably leads to visual artifacts, and is more performance-intensive." };
        limitExtendUpdown = new OpUpdown(MaxXDistance, new(x + c, y), w, 2) { description = "EXPERIMENTAL: How far objects are projected backward. If low, objects aren't stretched into the background. If high, they are.\n0.00 == objects are not projected backwards at all; >1 == objects are projected backwards indefinitely; same as if disabled" };
        resolutionScaleCheckbox = new OpCheckBox(ResolutionScaleEnabled, x, y += h) { description = "EXPERIMENTAL: Enables the experimental resolution scale option. This is compatible with SBCameraScroll (but can be very laggy!!!), but doesn't work as well with Sharpener." };
        resolutionScaleUpdown = new OpUpdown(ResolutionScale, new(x + c, y), w, 2) { description = "EXPERIMENTAL: Scales the resolution of the level texture to allow for finer details, but at a pretty big performance cost (especially with SBCameraScroll).\nYour recommended Resolution Scale ~= " + displayScaleString };
        optionsTab.AddItems(
            limitExtendCheckbox,
            limitExtendUpdown,
            new OpLabel(t + c, y, "[EXPERIMENTAL] Resolution Scale"),
            resolutionScaleCheckbox,
            resolutionScaleUpdown,
            new OpLabel(t+w-20f, y+=h, "Distance Curve"), //keep this as the last option!
            new OpListBox(SmoothingType, new(x-20f, y - 90f), w+w, smoothingValues) { description = "Makes the parallax effect more noticable by warping (horizontally) closer objects more. SINUSOIDAL produces the most pleasing and realistic result.\nLINEAR is the most optimized. INVERSE reduces the warping around the player, which might be nice for gameplay." },
                new OpLabel(t2+w+20f, y, "Depth Curve"), //maybe could shift this to the right if we're running out of room?
                new OpListBox(DepthCurve, new(x2+20f, y - 90f), w+w, depthCurveValues) { description = "EXTREME and PARABOLIC make shallower (closer) objects be warped more. INVERSE makes the effect negligible on shallow objects.\nIf LINEAR (fastest and most visually accurate setting), the deepest background is warped 5x more than the back wall, which I thought was too drastic." }
            );

        if (!ResolutionScaleEnabled.Value)
        {
            ResolutionScale.Value = displayScales[mainIdx];
            ResolutionScale.defaultValue = displayScales[mainIdx].ToString();
            //resolutionScaleUpdown.SetValueFloat(displayScales[mainIdx]);
            resolutionScaleRecommendedValue = displayScales[mainIdx];
        }
    }

    private float? resolutionScaleRecommendedValue = null;

    public override void Update()
    {
        base.Update();

        if (limitExtendCheckbox != null)
            limitExtendUpdown.greyedOut = !limitExtendCheckbox.GetValueBool();

        if (resolutionScaleCheckbox != null)
            resolutionScaleUpdown.greyedOut = !resolutionScaleCheckbox.GetValueBool();
        if (resolutionScaleRecommendedValue != null && resolutionScaleUpdown != null && resolutionScaleUpdown.GetValueFloat() != resolutionScaleRecommendedValue)
        {
            resolutionScaleUpdown.SetValueFloat(resolutionScaleRecommendedValue.Value);
            resolutionScaleRecommendedValue = null;
        }
    }
}