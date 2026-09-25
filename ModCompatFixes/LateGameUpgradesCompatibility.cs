using System.Runtime.CompilerServices;

namespace Physics_Items.ModCompatFixes
{
    internal static class LateGameUpgradesCompatibility
    {
        private static bool? _enabled;
        private static string modGUID = "com.malco.lethalcompany.moreshipupgrades";

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGUID);
                }
                return (bool)_enabled;
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ApplyFixes()
        {
            Plugin.Logger.LogInfo($"Applying compatibility fixes to: {modGUID}");
          // Plugin.Instance.manualSkipList.Add(typeof(MoreShipUpgrades.UpgradeComponents.Items.Wheelbarrow.StoreWheelbarrow));
           // Plugin.Instance.manualSkipList.Add(typeof(MoreShipUpgrades.UpgradeComponents.Items.Wheelbarrow.ScrapWheelbarrow));
        }
    }
}
