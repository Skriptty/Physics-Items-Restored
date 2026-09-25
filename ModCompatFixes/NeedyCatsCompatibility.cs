using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Physics_Items.ModCompatFixes
{
    internal class NeedyCatsCompatibility
    {
        private static bool? _enabled;

        private static string modGUID = NeedyCats.NeedyCatsBase.modGUID;
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
            Plugin.Instance.manualSkipList.Add(typeof(NeedyCats.NeedyCatProp));
        }
    }
}
