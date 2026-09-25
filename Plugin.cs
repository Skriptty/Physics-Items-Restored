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
using System.Text.RegularExpressions;

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
        internal HashSet<string> manualSkipNames = new HashSet<string> { "Hive", "Soccer ball" };
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
        
       
        internal ConfigEntry<float> explosionForceMultiplier;
        internal ConfigEntry<bool> enableCollisionAudio;
        internal ConfigEntry<bool> enableDropImpulse;

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
            myAssembly = Assembly.GetExecutingAssembly();
            manualSkipList.Add(typeof(ExtensionLadderItem));
            manualSkipList.Add(typeof(RadarBoosterItem));
            #endregion

            #region "Configs"[cite: 17]
            InitializeConfigs = Config.Bind("Technical", "Initialize Configs", true, "Re-Initializes all configs when set to true");
            customBlockList = new ConfigFile(Path.Combine(configDirectory, "physicsItems_CustomBlockList.cfg"), true);
            useSourceSounds = Config.Bind("Fun", "Use Source Engine Collision Sounds", false, "Use source rigidbody sounds.");
            overrideAllItemPhysics = Config.Bind("Fun", "Override all Item Physics", false, "ALL Items will have physics, regardless of blocklist.");
            physicsOnPickup = Config.Bind("Physics Behaviour", "Physics On Pickup", false, "Only enable item physisc when it has been picked up at least once.");
            disablePlayerCollision = Config.Bind("Physics Behaviour", "Disable Player Collision", false, "Set if Physical Items can collide with players.");
            maxCollisionVolume = Config.Bind("Physics Behaviour", "Max Collision Volume", 4f, "Sets the max volume each collision should have.");
            DebuggingStuff = Config.Bind("Technical", "Debug", false, "Debug mode");
            DiscardFollowAmplitude = Config.Bind("Physics Behaviour", "Discard Follow Aplitude", 36f, "Sets how strong items should go with the players velocity. In VR this sets how hard you'll be able to throw items.");
            
            
            explosionForceMultiplier = Config.Bind("Physics Behaviour", "Explosion Force Multiplier", 80f, "Multiplicador de fuerza para las minas terrestres en los objetos.");
            enableCollisionAudio = Config.Bind("Fun", "Enable Collision Audio", true, "Activa o desactiva los sonidos de impacto por colisión.");
            enableDropImpulse = Config.Bind("Physics Behaviour", "Enable Drop Impulse", true, "Si está activo, los objetos heredan el impulso/inercia al soltarlos.");

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

            #region "MonoMod Hooks"[cite: 17]
            ItemPhysics.Environment.Landmine.Init();
            GrabbablePatches.Init();
            On.GameNetcodeStuff.PlayerControllerB.PlaceGrabbableObject += PlayerControllerB_PlaceGrabbableObject;
            On.GameNetcodeStuff.PlayerControllerB.SetObjectAsNoLongerHeld += PlayerControllerB_SetObjectAsNoLongerHeld;
            On.GameNetcodeStuff.PlayerControllerB.DropAllHeldItems += PlayerControllerB_DropAllHeldItems;
            On.GameNetworkManager.Awake += GameNetworkManager_Awake;
            #endregion
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
            Logger.LogWarning($"Changed Blocklist: {e.ChangedSetting.Definition.Section} to {e.ChangedSetting.GetSerializedValue()}");
            var grabbable = allItemsDictionary[e.ChangedSetting.Definition.Section];
            List<GrabbableObject> grabbableList = FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
            if (e.ChangedSetting.GetSerializedValue() == "true")
            {
                foreach(var grab in grabbableList)
                {
                    if(grab.GetType() == grabbable.GetType())
                    {
                        skipObject.Add(grab);
                        blockList.Add(grab.GetType());
                    }
                }
            }
            else
            {
                foreach (var grab in grabbableList)
                {
                    if (grab.GetType() == grabbable.GetType())
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
            var value = false;
            var name = "";
            if(grabbableObject == null) return;
            if (grabbableObject.itemProperties == null) return;
            if (grabbableObject.itemProperties.itemName.IsNullOrWhiteSpace())
            {
                name = grabbableObject.itemProperties.name;
            }
            else
            {
                name = grabbableObject.itemProperties.itemName;
            }
            name = StringUtil.SanitizeString(ref name);
            if (name.IsNullOrWhiteSpace()) return;
            if (manualSkipList.Contains(grabbableObject.GetType()) || manualSkipNames.Contains(name)) value = true;
            allItemsDictionary[name] = grabbableObject;
            ConfigDefinition configDef = new ConfigDefinition(name, "Add to blocklist");
            customBlockList.Bind(configDef, value, new ConfigDescription("If check/true, adds to blocklist. [REQUIRES RESTART]"));
            if (InitializeConfigs.Value)
            {
                customBlockList[configDef].BoxedValue = value;
            }
            if (customBlockList[configDef].GetSerializedValue() == "true")
            {
                skipObject.Add(grabbableObject);
                blockList.Add(grabbableObject.GetType());
            }
        }

        #region "MonoMod Patches"[cite: 17]
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
            if (skipObject.Contains(dropObject)) return;
            Utils.Physics.GetPhysicsComponent(dropObject.gameObject, out PhysicsComponent comp);
            if (comp == null) return;
            
            if (!enableDropImpulse.Value)
            {
                comp.rigidbody.velocity = Vector3.zero;
                comp.rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            Vector3 startPosition = new Vector3(dropObject.startFallingPosition.x, 0, dropObject.startFallingPosition.z);
            Vector3 targetPosition = new Vector3(dropObject.targetFloorPosition.x, 0, dropObject.targetFloorPosition.z);

            float distance = CalculateDistance(startPosition, targetPosition);
            Vector3 direction = CalculateDirection(targetPosition, startPosition);
            bool isThrown = IsThrown(distance);
            float throwForce = CalculateThrowForce(isThrown, comp.throwForce);
            float forceMultiplier = CalculateForceMultiplier(isThrown, distance, throwForce, comp.rigidbody.mass);
            Vector3 force = CalculateForce(forceMultiplier, direction, comp, isThrown);

            ApplyForce(comp, force);
        }

        private const float MinThrowForce = 36f;
        private const float MaxForceMultiplier = 10f;

        private float CalculateDistance(Vector3 startPosition, Vector3 targetPosition)
        {
            return (startPosition - targetPosition).magnitude;
        }

        private Vector3 CalculateDirection(Vector3 targetPosition, Vector3 startPosition)
        {
            return (targetPosition - startPosition).normalized;
        }

        private bool IsThrown(float distance)
        {
            return distance > 1f;
        }

        private float CalculateThrowForce(bool isThrown, float throwForce)
        {
            return isThrown ? Mathf.Min(throwForce, MinThrowForce) : 0f;
        }

        private float CalculateForceMultiplier(bool isThrown, float distance, float throwForce, float mass)
        {
            return isThrown ? Mathf.Min(distance * throwForce, mass * MaxForceMultiplier) : 0f;
        }

        private Vector3 CalculateForce(float forceMultiplier, Vector3 direction, PhysicsComponent comp, bool isThrown)
        {
            Vector3 baseForce = isThrown ? direction * forceMultiplier : Vector3.zero;
            Vector3 velocityForce = comp.heldVelocityNormalized * DiscardFollowAmplitude.Value * comp.rigidbody.mass * Utils.Physics.FastInverseSqrt(comp.heldVelocityMagnitudeSqr);
            return baseForce + velocityForce;
        }

        private void ApplyForce(PhysicsComponent comp, Vector3 force)
        {
            comp.rigidbody.AddForce(force, ForceMode.Impulse);
        }

        #endregion
    }
}