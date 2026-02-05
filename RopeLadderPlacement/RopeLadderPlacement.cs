using HarmonyLib;
using System;
using ConfigLib;
using Vintagestory.API.Common;

namespace RopeLadderPlacement;

public class RopeLadderPlacement : ModSystem {
    public static ILogger Logger { get; private set; }
    public static ICoreAPI Api { get; private set; }
    public static Harmony harmony { get; private set; }

    public override void Start(ICoreAPI api) {

        harmony = new Harmony(Mod.Info.ModID);
        Api = api;
        Logger = Mod.Logger;

        try {
            ModConfig.Instance = api.LoadModConfig<ModConfig>(ModConfig.ConfigName) ?? new ModConfig();
            api.StoreModConfig(ModConfig.Instance, ModConfig.ConfigName);
        } catch (Exception) { ModConfig.Instance = new ModConfig(); }

        if (api.ModLoader.IsModEnabled("configlib")) {
            SubscribeToConfigChange(api);
        }

        harmony.PatchAll();
    }

    private void SubscribeToConfigChange(ICoreAPI api) {
        ConfigLibModSystem system = api.ModLoader.GetModSystem<ConfigLibModSystem>();

        system.SettingChanged += (domain, _, setting) => {
            if (domain != "ropeladderplacement")
                return;

            setting.AssignSettingValue(ModConfig.Instance);
        };
    }


    public override void Dispose() {
        Logger = null;
        Api = null;
        harmony?.UnpatchAll(Mod.Info.ModID);
        harmony = null;
        base.Dispose();
    }
}
