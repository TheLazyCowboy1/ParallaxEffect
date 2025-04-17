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
    public ConfigOptions()
    {
        //AllSlugcats = this.config.Bind<bool>("AllSlugcats", false);
        Warp = this.config.Bind<float>("Warp", 10f, new ConfigAcceptableRange<float>(0, 100f));
        MaxWarp = this.config.Bind<float>("MaxWarp", 10f, new ConfigAcceptableRange<float>(0, 100f));
        Optimization = this.config.Bind<float>("Optimization", 1f, new ConfigAcceptableRange<float>(0, 5f));
        InvertPos = this.config.Bind<bool>("InvertPos", false);
        SmoothingType = this.config.Bind<string>("SmoothingType", "SINESMOOTHING", new ConfigAcceptableList<string>(smoothingValues));
    }

    //General
    //public readonly Configurable<float> GhostFright;
    public readonly Configurable<float> Warp;
    public readonly Configurable<float> MaxWarp;
    public readonly Configurable<float> Optimization;
    public readonly Configurable<bool> InvertPos;
    public readonly Configurable<string> SmoothingType;

    public override void Initialize()
    {
        var optionsTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            optionsTab
        };

        float t = 150f, y = 450f, h = -50f, x = 50f, w = 80f;

        //General Options
        optionsTab.AddItems(
            new OpLabel(t, y, "Pixel Warp Factor"),
            new OpUpdown(Warp, new(x, y), w, 2) { description = "The amount in pixels (roughly) to warp the texture by." },
            new OpLabel(t, y+=h, "Max Warp"),
            new OpUpdown(MaxWarp, new(x, y), w, 2) { description = "The maximum amount in pixels (roughly) by which the texture can be warped. This should be <= the Warp Factor.\nUseful for performance reasons: lower values = better performance." },
            new OpLabel(t, y+=h, "Optimization"),
            new OpUpdown(Optimization, new(x, y), w, 2) { description = "Used to reduce the number of calculations, at the risk of visual artifacts.\n1.00 = unoptimized, 2.00 = reasonably stable." },
            new OpLabel(t, y+=h, "Invert Pos"),
            new OpCheckBox(InvertPos, x, y) { description = "Makes the warp stronger closer to the player, and weaker further away.\nThis is more visually realistic, but not ideal for gameplay." },
            new OpLabel(t+w, y+=h, "Smoothing Setting"), //keep this as the last option!
            new OpListBox(SmoothingType, new(x, y - 75f), w+w, smoothingValues) { description = "Makes the parallax effect more realistic. SINESMOOTHING produces the most realistic result.\nFLAT is the most optimized. INVSINE reduces the warping around the player." }
            );
    }
}