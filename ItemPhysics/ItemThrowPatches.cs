using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Physics_Items.ItemPhysics;
using Physics_Items.Utils;

namespace Physics_Items
{
    [HarmonyPatch]
    public static class ItemThrowPatches
    {
        public static Dictionary<GrabbableObject, Vector3> PendingThrowImpulses = new Dictionary<GrabbableObject, Vector3>();

        private static bool IsKeyPressed(KeyCode keyCode)
        {
            if (Mouse.current != null)
            {
                if (keyCode == KeyCode.Mouse0 && Mouse.current.leftButton.wasPressedThisFrame) return true;
                if (keyCode == KeyCode.Mouse1 && Mouse.current.rightButton.wasPressedThisFrame) return true;
                if (keyCode == KeyCode.Mouse2 && Mouse.current.middleButton.wasPressedThisFrame) return true;
                if (keyCode == KeyCode.Mouse3 && Mouse.current.forwardButton.wasPressedThisFrame) return true;
                if (keyCode == KeyCode.Mouse4 && Mouse.current.backButton.wasPressedThisFrame) return true;
            }

            if (Keyboard.current == null) return false;

            string keyName = keyCode.ToString();
            if (keyName.StartsWith("Alpha")) keyName = keyName.Replace("Alpha", "Digit");

            if (Enum.TryParse(keyName, true, out Key inputKey) && inputKey != Key.None)
            {
                return Keyboard.current[inputKey].wasPressedThisFrame;
            }

            return false;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void PlayerControllerB_Update(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner || !__instance.isPlayerControlled || __instance.isPlayerDead) return;

            if (IsKeyPressed(Plugin.Instance.throwKey.Value))
            {
                if (__instance.isTypingChat || __instance.inTerminalMenu || (__instance.quickMenuManager != null && __instance.quickMenuManager.isMenuOpen)) return;

                GrabbableObject item = __instance.currentlyHeldObjectServer;
                if (item == null) return;

                Vector3 forwardDir = __instance.gameplayCamera.transform.forward;
                Vector3 throwVector = (forwardDir * Plugin.Instance.throwForce.Value) + (Vector3.up * 0f );

                if (__instance.thisController != null)
                {
                    throwVector += __instance.thisController.velocity * 0.8f;
                }
                
                PendingThrowImpulses[item] = throwVector;
                
                __instance.DiscardHeldObject(placeObject: false, null, Vector3.zero);
                
                item.fallTime = 1f;
                item.hasHitGround = true;
                
                if (Utils.Physics.GetPhysicsComponent(item.gameObject, out PhysicsComponent comp))
                {
                    comp.isPlaced = false;
                    comp.enabled = true;
                    
                    if (comp.rigidbody != null)
                    {
                        comp.rigidbody.isKinematic = false;
                        comp.rigidbody.velocity = throwVector;
                        comp.rigidbody.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
                    }
                }
                
                PendingThrowImpulses.Remove(item);
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.EquipItem))]
        [HarmonyPostfix]
        public static void EquipItem_Postfix(GrabbableObject __instance)
        {
            if (__instance.IsOwner)
            {
                UpdateItemControlTip(__instance);
            }
        }

        public static void UpdateItemControlTip(GrabbableObject item)
        {
            if (item == null || HUDManager.Instance == null || item.itemProperties == null) return;

            string throwTip = $"Throw item: [{Plugin.Instance.throwKey.Value}]";
            
            List<string> tipsList = new List<string>();

            if (item.itemProperties.toolTips != null)
            {
                foreach (string tip in item.itemProperties.toolTips)
                {
                    if (!string.IsNullOrEmpty(tip) && !tip.StartsWith("Throw item:"))
                    {
                        tipsList.Add(tip);
                    }
                }
            }

            tipsList.Add(throwTip);

            HUDManager.Instance.ChangeControlTipMultiple(tipsList.ToArray(), holdingItem: true, item.itemProperties);
        }
    }
}