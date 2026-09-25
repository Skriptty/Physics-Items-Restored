using GameNetcodeStuff;
using HarmonyLib;
using System;
using Unity.Netcode;
using UnityEngine.Diagnostics;

namespace Physics_Items.NamedMessages
{
    internal class ModCheck
    {
        public static bool hasSameModVersion = false;

        internal static string RequestModCheck = "PhysicsItemsRequestModCheck";
        internal static string ReceiveModCheck = "PhysicsItemsReceiveModCheck";
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake)), HarmonyPrefix]
        public static void STARTOFROUNDAWAKE()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                hasSameModVersion = true;
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestModCheck, OnRequest);
                Plugin.Logger.LogInfo("Setting up Server CustomMessagingManager");
                SyncOnLocalClient();
            }
            else
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ReceiveModCheck, OnReceive);
                Plugin.Logger.LogInfo("Setting up Client CustomMessagingManager");
                SendRequestToServer();
            }
        }

        private static void SendRequestToServer()
        {
            if (NetworkManager.Singleton.IsClient)
            {
                Plugin.Logger.LogInfo("Sending request to server.");
                string modVersion = PluginInfo.PLUGIN_VERSION;
                FastBufferWriter writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(modVersion), Unity.Collections.Allocator.Temp);
                writer.WriteValueSafe(modVersion);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestModCheck, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            }
            else
            {
                Plugin.Logger.LogError("Server cannot use method SendRequestToServer because you're already the server.");
            }
        }

        private static void SyncOnLocalClient()
        {
            Plugin.Instance.ServerHasMod = hasSameModVersion;
        }

        private static void OnReceive(ulong senderClientId, FastBufferReader messagePayload)
        {
            bool value;
            messagePayload.ReadValueSafe(out value);
            hasSameModVersion = value;
            Plugin.Logger.LogInfo($"Received mod check: {hasSameModVersion} from server!");
            SyncOnLocalClient();
        }

        private static void OnRequest(ulong senderClientId, FastBufferReader messagePayload)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            Plugin.Logger.LogInfo($"Player_ID: {senderClientId} Requested for Mod Check.");
            messagePayload.ReadValueSafe(out string modVersionString);
            bool isCompatible = modVersionString.Equals(PluginInfo.PLUGIN_VERSION);
            FastBufferWriter writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(isCompatible), Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(isCompatible);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ReceiveModCheck, senderClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect)), HarmonyPrefix]
        public static void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(RequestModCheck);
                    Plugin.Logger.LogInfo("Destroying Server CustomMessagingManager");
                }
                else
                {
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(ReceiveModCheck);
                    Plugin.Logger.LogInfo("Destroying Client CustomMessagingManager");
                }
            }
            hasSameModVersion = false;
        }


    }
}
