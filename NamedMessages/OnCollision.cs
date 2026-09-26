using HarmonyLib;
using Physics_Items.ItemPhysics;
using Physics_Items.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Physics_Items.NamedMessages
{
    internal class OnCollision
    {
        internal static string CollisionCheck = "PhysicsItemsCollisionCheck";

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake)), HarmonyPrefix]
        public static void StartOfRound_Awake()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(CollisionCheck, OnReceive);
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect)), HarmonyPrefix]
        public static void OnDestroy()
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(CollisionCheck);
            Utils.Physics.physicsComponents.Clear();
        }

        private static void OnReceive(ulong senderClientId, FastBufferReader messagePayload)
        {
            NetworkObjectReference value;
            messagePayload.ReadValueSafe(out value);
            if (value.TryGet(out NetworkObject netobj))
            {
                PhysicsComponent physComp = Utils.Physics.GetPhysicsComponent(netobj.transform.gameObject);
                if (physComp != null)
                {
                    if (Plugin.Instance.enableCollisionAudio.Value) 
                    {
                        physComp.PlayDropSFX();
                    }
                }
            }
        }
    }
}