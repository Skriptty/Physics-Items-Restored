using System.Runtime.CompilerServices;

namespace Physics_Items.ModCompatFixes
{
    internal static class AdvancedCompanyCompatibility
    {
        private static bool? _enabled;
        private static string modGUID = "com.potatoepet.AdvancedCompany";

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
           // Plugin.Instance.manualSkipList.Add(typeof(AdvancedCompany.Objects.LightningRod));
        }
    }
}
