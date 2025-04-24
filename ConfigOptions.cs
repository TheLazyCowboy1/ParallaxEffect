using BepInEx.Logging;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using MoreSlugcats;
using System.Collections.Generic;
using UnityEngine;
using Watcher;

namespace ParallaxEffect;

public class ConfigOptions : OptionInterface
{
    private static string[] smoothingValues = new string[] { "SINESMOOTHING", "INVSINESMOOTHING", "FLAT" };
    private static string[] depthCurveValues = new string[] { "EXTREME", "PARABOLIC", "LINEAR", "INVERSE" };
    public ConfigOptions()
    {
        //AllSlugcats = this.config.Bind<bool>("AllSlugcats", false);
        Warp = this.config.Bind<float>("Warp", 20f, new ConfigAcceptableRange<float>(0, 100f));
        MaxWarp = this.config.Bind<float>("MaxWarp", 15f, new ConfigAcceptableRange<float>(0, 100f));
        Optimization = this.config.Bind<float>("Optimization", 1.5f, new ConfigAcceptableRange<float>(0, 5f));
        StartOffset = this.config.Bind<float>("StartOffset", -0.1f, new ConfigAcceptableRange<float>(-1f, 1f));
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
    public readonly Configurable<float> MaxWarp;
    public readonly Configurable<float> Optimization;
    public readonly Configurable<float> StartOffset;
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

        float t = 150f, y = 550f, h = -40f, x = 50f, w = 80f;

        //General Options
        optionsTab.AddItems(
            new OpLabel(t, y, "Pixel Warp Factor"),
            new OpUpdown(Warp, new(x, y), w, 1) { description = "The amount in pixels (roughly) to warp the texture by." },
            new OpLabel(t, y+=h, "Max Warp"),
            new OpUpdown(MaxWarp, new(x, y), w, 1) { description = "The maximum amount in pixels (roughly) by which the texture can be warped. This should be <= the Warp Factor.\nUseful for performance reasons: lower values = better performance." },
            new OpLabel(t, y+=h, "Optimization"),
            new OpUpdown(Optimization, new(x, y), w, 2) { description = "Used to reduce the number of calculations, at the risk of visual artifacts.\n1.00 = unoptimized, 2.00 = reasonably stable." },
            new OpLabel(t, y += h, "[ADVANCED] Minimum Warp"),
            new OpUpdown(StartOffset, new(x, y), w, 2) { description = "ADVANCED: The lowest amount by which pixels can be warped. If set < 0, allows objects to \"project into the screen,\" which looks like more realistic rotation but might be annoying.\nDon't set < -0.20 or > 0. Both of these simply make it look bad." },
            new OpLabel(t, y+=h, "Invert Pos"),
            new OpCheckBox(InvertPos, x, y) { description = "Makes the warp stronger closer to the player, and weaker further away.\nThis is more visually realistic, but not ideal for gameplay." },
            new OpLabel(t, y += h, "Don't Warp when Centered"),
            new OpCheckBox(NoCenterWarp, x, y) { description = "Makes the warp factor scale according to how far away the player is from the center of the screen.\nThis means that the room will look totally normal when the player is in the center of the room." },
            new OpLabel(t, y += h, "Camera Move Speed"),
            new OpUpdown(CameraMoveSpeed, new(x, y), w, 2) { description = "How quickly the camera adjusts to position changes.\n1 = instantaneously; 0 = not at all; 0.1 = slowly." },
            new OpLabel(t, y += h, "Warp Decals"),
            new OpCheckBox(WarpDecals, x, y) { description = "Also warps decals added through dev tools. These are common occurrences.\nNOTE: Runs on the CPU, not on the GPU!! If there are a lot of decals, this will increase lag." },
            new OpLabel(t, y += h, "Mouse Sensitivity"),
            new OpUpdown(MouseSensitivity, new(x, y), w, 2) { description = "The sensitivity with which mouse movements alter the camera position.\nSet to 0 to disable this feature entirely." },
            new OpLabel(t+w, y+=h, "Smoothing Setting"), //keep this as the last option!
            new OpListBox(SmoothingType, new(x, y - 70f), w+w, smoothingValues) { description = "Makes the parallax effect more realistic. SINESMOOTHING produces the most realistic result.\nFLAT is the most optimized. INVSINE reduces the warping around the player." },
            new OpLabel(t + w, y+=h-60f, "Depth Curve"),
            new OpListBox(DepthCurve, new(x, y - 90f), w + w, depthCurveValues) { description = "EXTREME and PARABOLIC make closer objects appear farther. INVERSE makes the effect negligible on closer objects.\nIf LINEAR, the furthest background is warped 5x more than the back wall. I personally found this difference excessive, so I curbed it." }
            );
    }
}