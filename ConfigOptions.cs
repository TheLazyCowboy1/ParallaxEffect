using BepInEx.Logging;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;

namespace ParallaxEffect;

public class ConfigOptions : OptionInterface
{
    private static readonly string[] smoothingValues = new string[] { "SINESMOOTHING", "INVSINESMOOTHING", "FLAT" };
    private static readonly string[] depthCurveValues = new string[] { "EXTREME", "PARABOLIC", "LINEAR", "INVERSE" };
    public ConfigOptions()
    {
        //AllSlugcats = this.config.Bind<bool>("AllSlugcats", false);
        Warp = this.config.Bind<float>("Warp", 20f, new ConfigAcceptableRange<float>(0, 100f));
        //MaxWarp = this.config.Bind<float>("MaxWarp", 20f, new ConfigAcceptableRange<float>(0, 100f));
        MaxWarpFactor = this.config.Bind<float>("MaxWarpFactor", 0.9f, new ConfigAcceptableRange<float>(0, 1f));
        Optimization = this.config.Bind<float>("Optimization", 1.0f, new ConfigAcceptableRange<float>(0, 5f));
        StartOffset = this.config.Bind<float>("StartOffset", -0.1f, new ConfigAcceptableRange<float>(-1f, 0f));
        //EndOffset = this.config.Bind<float>("EndOffset", 1f, new ConfigAcceptableRange<float>(0f, 3f));
        RedModScale = this.config.Bind<float>("RedModScale", 0.7f, new ConfigAcceptableRange<float>(0f, 10f));
        InvertPos = this.config.Bind<bool>("InvertPos", false);
        NoCenterWarp = this.config.Bind<bool>("NoCenterWarp", false);
        CameraMoveSpeed = this.config.Bind<float>("CameraMoveSpeed", 0.1f, new ConfigAcceptableRange<float>(0, 1f));
        WarpDecals = this.config.Bind<bool>("WarpDecals", true);
        MouseSensitivity = this.config.Bind<float>("MouseSensitivity", 0.25f, new ConfigAcceptableRange<float>(0, 1f));
        SmoothingType = this.config.Bind<string>("SmoothingType", "SINESMOOTHING", new ConfigAcceptableList<string>(smoothingValues));
        DepthCurve = this.config.Bind<string>("DepthCurve", "PARABOLIC", new ConfigAcceptableList<string>(depthCurveValues));
    }

    //General
    //public readonly Configurable<float> GhostFright;
    public readonly Configurable<float> Warp;
    //public readonly Configurable<float> MaxWarp;
    public readonly Configurable<float> MaxWarpFactor;
    public readonly Configurable<float> Optimization;
    public readonly Configurable<float> StartOffset;
    public readonly Configurable<float> EndOffset = new(1.0f); //temporarily disabled, because it isn't useful
    public readonly Configurable<float> RedModScale;
    public readonly Configurable<bool> InvertPos;
    public readonly Configurable<bool> NoCenterWarp;
    public readonly Configurable<float> CameraMoveSpeed;
    public readonly Configurable<bool> WarpDecals; //what about making this a config? we probably don't have to re-warp EVERY frame
    public readonly Configurable<float> MouseSensitivity;
    public readonly Configurable<string> SmoothingType;
    public readonly Configurable<string> DepthCurve;

    public override void Initialize()
    {
        var optionsTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            optionsTab
        };

        float t = 150f, y = 550f, h = -35f, x = 50f, w = 80f;
        //float t2 = 400f, x2 = 300f;

        //General Options
        optionsTab.AddItems(
            new OpLabel(t, y, "Pixel Warp Factor"),
            new OpUpdown(Warp, new(x, y), w, 1) { description = "The amount in pixels (roughly) to warp the texture by." },
            new OpLabel(t, y+=h, "Max Warp"),
            //new OpUpdown(MaxWarp, new(x, y), w, 1) { description = "The maximum amount in pixels (roughly) by which the texture can be warped. This should be <= the Warp Factor.\nUseful for performance reasons: lower values = better performance." },
            new OpUpdown(MaxWarpFactor, new(x, y), w, 2) { description = "The maximum amount by which the texture can be warped, expressed as a fraction of the Warp Factor.\nUseful for performance reasons: lower values = better performance." },
            new OpLabel(t, y+=h, "Optimization"),
            new OpUpdown(Optimization, new(x, y), w, 2) { description = "Used to reduce the number of calculations, at the risk of visual artifacts (especially on small plants).\n1.00 = unoptimized, 2.00 = reasonably stable. Below 1 will severely increase lag with nominal visual difference." },
            new OpLabel(t, y += h, "Minimum Warp"),
            new OpUpdown(StartOffset, new(x, y), w, 2) { description = "The lowest amount by which pixels can be warped. If set < 0, allows objects to \"project into the screen,\" which looks like more realistic rotation but might be annoying.\nI recommend setting it >= -0.20. Below -0.20 has minimal effect (and can increase lag), unless EXTREME Depth Curve is chosen. 0.00 disables \"projecting into the screen.\"" },
                //new OpLabel(t2, y, "[ADVANCED] Maximum Tested Warp"),
                //new OpUpdown(EndOffset, new(x2, y), w, 2) { description = "ADVANCED: Keep this at 1.00. Raising it above 1.00 only increases lag, and lowering it from 1.00 creates visual artifacts." },
            new OpLabel(t, y += h, "Projection Steepness/Fade"),
            new OpUpdown(RedModScale, new(x, y), w, 2) { description = "Scales how quickly warped things \"fade into the background.\"\nLow values = objects appear thick; High values = objects aren't \"projected backwards\" as noticably (they fade faster)." },
            new OpLabel(t, y+=h, "Invert Pos"),
            new OpCheckBox(InvertPos, x, y) { description = "Makes the warp stronger closer to the player, and weaker further away.\nThis is more visually realistic, but not ideal for gameplay." },
            new OpLabel(t, y += h, "Don't Warp when Centered"),
            new OpCheckBox(NoCenterWarp, x, y) { description = "Makes the warp factor scale according to how far away the player is from the center of the screen.\nThis means that the room will look totally normal when the player is in the center of the room." },
            new OpLabel(t, y += h, "Camera Move Speed"),
            new OpUpdown(CameraMoveSpeed, new(x, y), w, 2) { description = "How quickly the camera adjusts to position changes.\n1.00 = instantaneously; 0.00 = not at all; 0.10 = slowly." },
            new OpLabel(t, y += h, "Warp Decals"),
            new OpCheckBox(WarpDecals, x, y) { description = "Also warps decals added through dev tools. These are common occurrences.\nNOTE: Runs on the CPU, not on the GPU!! If there are a lot of decals, this will increase lag." },
            new OpLabel(t, y += h, "Mouse Sensitivity"),
            new OpUpdown(MouseSensitivity, new(x, y), w, 2) { description = "The sensitivity with which mouse movements alter the camera position.\nSet to 0.00 to disable this feature entirely." },
            new OpLabel(t+w, y+=h, "Smoothing Setting"), //keep this as the last option!
            new OpListBox(SmoothingType, new(x, y - 70f), w+w, smoothingValues) { description = "Makes the parallax effect more realistic. SINESMOOTHING produces the most pleasing result.\nFLAT is the most optimized. INVSINE reduces the warping around the player, which might be nice for gameplay." },
            new OpLabel(t + w, y+=h-70f, "Depth Curve"),
            new OpListBox(DepthCurve, new(x, y - 90f), w + w, depthCurveValues) { description = "EXTREME and PARABOLIC make closer objects be warped more. INVERSE makes the effect negligible on closer objects.\nIf LINEAR (fastest setting), the furthest background is warped 5x more than the back wall. I personally found this difference excessive, so it's not the default option." }
            );
    }
}