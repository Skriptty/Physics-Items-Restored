using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Physics_Items.ModCompatFixes;
using Physics_Items.NamedMessages;
using Physics_Items.ItemPhysics;
using Physics_Items.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;
using System.IO;
using System.Reflection;
using GameNetcodeStuff;

namespace Physics_Items
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("lethal company.exe")]
    [BepInDependency("Spantle.ThrowEverything", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.potatoepet.AdvancedCompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Jordo.NeedyCats", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.malco.lethalcompany.moreshipupgrades", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Plugin Instance;
        internal bool Initialized = false;
        internal bool ServerHasMod = false;
        internal HashSet<string> manualSkipNames = new HashSet<string> { "Hive", "Beehive", "Bee hive", "RedLocustHive", "RedLocustHive(Clone)", "SoccerBall", "SoccerBall(Clone)" };
        internal HashSet<Type> manualSkipList = new HashSet<Type>();
        internal HashSet<Type> blockList = new HashSet<Type>();
        string configDirectory = Paths.ConfigPath;
        internal ConfigEntry<bool> useSourceSounds;
        internal ConfigEntry<bool> physicsOnPickup;
        internal ConfigEntry<bool> disablePlayerCollision;
        internal ConfigEntry<float> maxCollisionVolume;
        internal ConfigEntry<bool> overrideAllItemPhysics;
        internal ConfigEntry<bool> InitializeConfigs;
        internal ConfigEntry<bool> DebuggingStuff;
        internal ConfigEntry<float> DiscardFollowAmplitude;
        internal ConfigEntry<bool> freezeInShip;
        
        internal ConfigEntry<float> explosionForceMultiplier;
        internal ConfigEntry<bool> enableCollisionAudio;
        internal ConfigEntry<bool> enableDropImpulse;
        
        internal ConfigEntry<KeyCode> throwKey;
        internal ConfigEntry<float> throwForce;

        internal ConfigFile customBlockList;
        internal Dictionary<string, GrabbableObject> allItemsDictionary = new Dictionary<string, GrabbableObject>();
        internal Assembly myAssembly;
        internal readonly Harmony Harmony = new(PluginInfo.PLUGIN_GUID);

        internal SkipObjectSet skipObject = new SkipObjectSet();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            Logger = base.Logger; 
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            
            AssetLoader.LoadAssetBundles();

            #region "Compatibility"
            if (AdvancedCompanyCompatibility.enabled)
            {
                AdvancedCompanyCompatibility.ApplyFixes();
            }
            if (NeedyCatsCompatibility.enabled)
            {
                NeedyCatsCompatibility.ApplyFixes();
            }
            if (LateGameUpgradesCompatibility.enabled)
            {
                LateGameUpgradesCompatibility.ApplyFixes();
            }
            #endregion

            #region "Harmony Patches"
            Harmony.PatchAll(typeof(ModCheck));
            Harmony.PatchAll(typeof(OnCollision));
            Harmony.PatchAll(typeof(ItemThrowPatches));
            myAssembly = Assembly.GetExecutingAssembly();
            manualSkipList.Add(typeof(ExtensionLadderItem));
            manualSkipList.Add(typeof(RadarBoosterItem));
            #endregion

            #region "Configs"
            InitializeConfigs = Config.Bind("Technical", "Initialize Configs", false, "Re-Initializes all configs when set to true");
            customBlockList = new ConfigFile(Path.Combine(configDirectory, "physicsItems_CustomBlockList.cfg"), true);
            useSourceSounds = Config.Bind("Fun", "Use Source Engine Collision Sounds", false, "Use source rigidbody sounds.");
            overrideAllItemPhysics = Config.Bind("Fun", "Override all Item Physics", false, "ALL Items will have physics, regardless of blocklist.");
            physicsOnPickup = Config.Bind("Physics Behaviour", "Physics On Pickup", true, "Only enable item physics when it has been picked up at least once.");
            disablePlayerCollision = Config.Bind("Physics Behaviour", "Disable Player Collision", true, "Set if Physical Items can collide with players.");
            maxCollisionVolume = Config.Bind("Physics Behaviour", "Max Collision Volume", 4f, "Sets the max volume each collision should have.");
            DebuggingStuff = Config.Bind("Technical", "Debug", false, "Debug mode");
            freezeInShip = Config.Bind("Physics Behaviour", "Freeze In Ship", true, "If enabled, physical items will freeze in place and behave like Vanilla items while inside the ship or elevator.");
            DiscardFollowAmplitude = Config.Bind("Physics Behaviour", "Discard Follow Amplitude", 1f, "Sets how strong items should go with the players velocity.");
            
            explosionForceMultiplier = Config.Bind("Physics Behaviour", "Explosion Force Multiplier", 80f, "Multiplies how much blast knockback should the mines give the items.");
            enableCollisionAudio = Config.Bind("Fun", "Enable Collision Audio", true, "Enables or disables collision sound effects.");
            enableDropImpulse = Config.Bind("Physics Behaviour", "Enable Drop Impulse", true, "If enabled, items will obtain the momentum of the player.");
            
            throwKey = Config.Bind("Controls", "Throw Item Key", KeyCode.Mouse1, "Tecla para lanzar cualquier objeto que tengas en la mano.");
            throwForce = Config.Bind("Controls", "Throw Force", 16f, "Fuerza con la que se lanzan los objetos.");
            throwKey.SettingChanged += OnThrowKeyChanged;

            if (disablePlayerCollision.Value)
            {
                UnityEngine.Physics.IgnoreLayerCollision(3, 6, true);
            }

            customBlockList.SettingChanged += CustomBlockList_SettingChanged;
            Config.SettingChanged += Config_SettingChanged;
            if (InitializeConfigs.Value)
            {
                if (File.Exists(Config.ConfigFilePath))
                {
                    File.Delete(Config.ConfigFilePath);
                }
                Config.Save();
                Logger.Log(LogLevel.All, "Initializing Configs..");
            }
            #endregion

            #region "MonoMod Hooks"
            ItemPhysics.Environment.Landmine.Init();
            GrabbablePatches.Init();
            On.GameNetcodeStuff.PlayerControllerB.PlaceGrabbableObject += PlayerControllerB_PlaceGrabbableObject;
            On.GameNetcodeStuff.PlayerControllerB.SetObjectAsNoLongerHeld += PlayerControllerB_SetObjectAsNoLongerHeld;
            On.GameNetcodeStuff.PlayerControllerB.DropAllHeldItems += PlayerControllerB_DropAllHeldItems;
            On.GameNetworkManager.Awake += GameNetworkManager_Awake;
            #endregion
        }

        private void OnThrowKeyChanged(object sender, EventArgs e)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;
            if (localPlayer != null && localPlayer.currentlyHeldObjectServer != null)
            {
                ItemThrowPatches.UpdateItemControlTip(localPlayer.currentlyHeldObjectServer);
            }
        }

        private void GameNetworkManager_Awake(On.GameNetworkManager.orig_Awake orig, GameNetworkManager self)
        {
            orig(self);
            InitializeNetworkTransformAndBlocklistConfig();
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            Logger.LogWarning($"Changed: {e.ChangedSetting.Definition.Key} to {e.ChangedSetting.GetSerializedValue()}");
        }

        private void CustomBlockList_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (overrideAllItemPhysics.Value) return;
            Logger.LogWarning($"Changed Blocklist: {e.ChangedSetting.Definition.Key} to {e.ChangedSetting.GetSerializedValue()}");
            
            string itemTypeName = e.ChangedSetting.Definition.Key;
            List<GrabbableObject> grabbableList = FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
            
            bool isBlocked = e.ChangedSetting.GetSerializedValue() == "true";
            
            foreach (var grab in grabbableList)
            {
                if (grab.GetType().Name == itemTypeName)
                {
                    if (isBlocked)
                    {
                        skipObject.Add(grab);
                        blockList.Add(grab.GetType());
                    }
                    else
                    {
                        skipObject.Remove(grab);
                        blockList.Remove(grab.GetType());
                    }
                }
            }
        }

        private void InitializeNetworkTransformAndBlocklistConfig()
        {
            if (Initialized) return;
            foreach (GrabbableObject grabbableObject in Resources.FindObjectsOfTypeAll<GrabbableObject>())
            {
                InitializeBlocklistConfig(grabbableObject);
                if (manualSkipList.Contains(grabbableObject.GetType())) continue;
                if (grabbableObject.gameObject.GetComponent<NetworkTransform>() == null) 
                {
                    NetworkTransform netTransform = grabbableObject.gameObject.AddComponent<NetworkTransform>();
                    netTransform.enabled = false;
                }
            }
            Initialized = true;
            InitializeConfigs.Value = false;
        }

        private void InitializeBlocklistConfig(GrabbableObject grabbableObject)
        {
            if (grabbableObject == null || grabbableObject.itemProperties == null) return;

            Type itemType = grabbableObject.GetType();
            bool isDefaultBlocked = manualSkipList.Contains(itemType);

            string configKey = itemType.Name;
            ConfigDefinition configDef = new ConfigDefinition("BlockList", configKey);

            var configEntry = customBlockList.Bind(configDef, isDefaultBlocked, new ConfigDescription($"Si está en true, desactiva las físicas para {configKey}."));

            if (configEntry.Value)
            {
                skipObject.Add(grabbableObject);
                blockList.Add(itemType);
            }
        }

        #region "MonoMod Patches"
        private void PlayerControllerB_DropAllHeldItems(
            On.GameNetcodeStuff.PlayerControllerB.orig_DropAllHeldItems orig,
            GameNetcodeStuff.PlayerControllerB self,
            bool itemsFall,
            bool disconnecting,
            bool setInShip,
            bool setInElevator,
            Vector3 syncedPlayerPosition,
            Vector3 syncedHeldObjectPosition,
            Vector3 syncedHeldObjectRotation,
            Vector3 syncedPlayerCamPosition,
            Vector3 syncedPlayerCamRotation)
        {
            var oldItems = new List<GrabbableObject>(self.ItemSlots);
            orig(self, itemsFall, disconnecting, setInShip, setInElevator, syncedPlayerPosition, syncedHeldObjectPosition, syncedHeldObjectRotation, syncedPlayerCamPosition, syncedPlayerCamRotation);
            if (Utils.Physics.GetPhysicsComponent(self.gameObject) == null) return;
            for (int i = 0; i < oldItems.Count; i++)
            {
                GrabbableObject item = oldItems[i];
                if (item is null) continue;
                if (Utils.Physics.GetPhysicsComponent(item.gameObject, out PhysicsComponent comp))
                {
                    skipObject.Add(item);
                    comp.physicsHelperRef.why = true;
                    comp.alreadyPickedUp = false;
                    comp.enabled = false;
                }
            }
        }

        private void PlayerControllerB_PlaceGrabbableObject(On.GameNetcodeStuff.PlayerControllerB.orig_PlaceGrabbableObject orig, GameNetcodeStuff.PlayerControllerB self, Transform parentObject, Vector3 positionOffset, bool matchRotationOfParent, GrabbableObject placeObject)
        {
            orig(self, parentObject, positionOffset, matchRotationOfParent, placeObject);
            if (skipObject.Contains(placeObject)) return;
            Utils.Physics.GetPhysicsComponent(placeObject.gameObject, out PhysicsComponent physics);
            if (physics == null) return;
            physics.isPlaced = true;
            physics.rigidbody.isKinematic = true;
            placeObject.gameObject.transform.rotation = Quaternion.Euler(placeObject.itemProperties.restingRotation.x, placeObject.floorYRot + placeObject.itemProperties.floorYOffset + 90f, placeObject.itemProperties.restingRotation.z);
            placeObject.gameObject.transform.localPosition = positionOffset;
        }

        private void PlayerControllerB_SetObjectAsNoLongerHeld(On.GameNetcodeStuff.PlayerControllerB.orig_SetObjectAsNoLongerHeld orig, GameNetcodeStuff.PlayerControllerB self, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, GrabbableObject dropObject, int floorYRot)
        {
            orig(self, droppedInElevator, droppedInShipRoom, targetFloorPosition, dropObject, floorYRot);

            if (dropObject == null || skipObject.Contains(dropObject)) return;

            if (!Utils.Physics.GetPhysicsComponent(dropObject.gameObject, out PhysicsComponent comp)) return;

            comp.heldVelocityNormalized = Vector3.zero;
            comp.heldVelocityMagnitudeSqr = 0f;

            if (comp.rigidbody == null) return;

            comp.rigidbody.isKinematic = false;
            comp.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            
            if (ItemThrowPatches.PendingThrowImpulses.ContainsKey(dropObject))
            {
                return;
            }

            if (!enableDropImpulse.Value)
            {
                comp.rigidbody.velocity = Vector3.zero;
                comp.rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            Vector3 playerVelocity = self.thisController != null ? self.thisController.velocity : Vector3.zero;

            comp.rigidbody.velocity = playerVelocity * DiscardFollowAmplitude.Value;
            comp.rigidbody.angularVelocity = Vector3.zero;
        }
        #endregion
    }
}