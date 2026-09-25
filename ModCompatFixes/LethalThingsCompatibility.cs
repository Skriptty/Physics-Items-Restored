using Physics_Items.ItemPhysics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Physics_Items.ModCompatFixes
{
    internal class LethalThingsCompatibility
    {
        private static bool? _enabled;
        private static string modGUID = "evaisa.lethalthings";

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
        public static void ApplyFixes(PhysicsComponent comp)
        {
            GrabbableObject grabbableObjectRef = comp.grabbableObjectRef;
            switch (grabbableObjectRef)
            {
                case LethalThings.Dingus dingus:
                    comp.rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                    comp.SetRotation();
                    break;
            }
            On.GameNetcodeStuff.PlayerControllerB.SetObjectAsNoLongerHeld += PlayerControllerB_SetObjectAsNoLongerHeld;
        }

        private static void PlayerControllerB_SetObjectAsNoLongerHeld(On.GameNetcodeStuff.PlayerControllerB.orig_SetObjectAsNoLongerHeld orig, GameNetcodeStuff.PlayerControllerB self, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, GrabbableObject dropObject, int floorYRot)
        {
            orig(self, droppedInElevator, droppedInShipRoom, targetFloorPosition, dropObject, floorYRot);
            switch (dropObject)
            {
                case LethalThings.Dingus dingus:
                    Utils.Physics.GetPhysicsComponent(dingus.gameObject, out PhysicsComponent physics);
                    if (physics == null) return;
                    if (!physics.alreadyPickedUp) return;
                    physics.SetRotation();
                    break;
            }
        }
    }
}
