using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ThrowEverything.Models;
using UnityEngine;

namespace Physics_Items.ModCompatFixes
{
    internal static class ThrowEverythingCompatibility
    {
        private static bool? _enabled;
        private static string modGUID = ThrowEverything.PluginInfo.PLUGIN_GUID;
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
            Plugin.Instance.Harmony.PatchAll(typeof(ThrowEverythingCompatibility));
        }

        static Collider colliderCache;
       [HarmonyPatch(typeof(ChargingThrow), nameof(ChargingThrow.DrawLandingCircle)), HarmonyPostfix]
        static void DrawLandingCircle(ChargingThrow __instance)
        {
            if (colliderCache == null)
                colliderCache = __instance.preview.GetComponent<Collider>();
            colliderCache.isTrigger = true; // This is running in PlayerControllerB.Update(). GetComponent bad1!1!1
        }

        [HarmonyPatch(typeof(ChargingThrow), nameof(ChargingThrow.Stop)), HarmonyPostfix]
        static void Stop()
       {
            colliderCache = null;
        }
    }
}