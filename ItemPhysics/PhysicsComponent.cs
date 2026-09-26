using GameNetcodeStuff;
using MoreShipUpgrades.Patches;
using Physics_Items.ModCompatFixes;
using Physics_Items.NamedMessages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.ParticleSystem.PlaybackState;
using Collision = UnityEngine.Collision;

namespace Physics_Items.ItemPhysics
{
    [RequireComponent(typeof(GrabbableObject))]
    public class PhysicsComponent : MonoBehaviour, IHittable
    {
        public GrabbableObject grabbableObjectRef;
        public Collider collider;
        public Rigidbody rigidbody;
        public NetworkTransform networkTransform;
        public NetworkObject networkObject;
        public NetworkRigidbody networkRigidbody;
        public PhysicsHelper physicsHelperRef;
        public bool isPlaced = false;
        public Rigidbody scanNodeRigid;
        public float terminalVelocity;
        public float gravity = 9.8f;
        public float throwForce;
        public float oldVolume;
        public bool alreadyPickedUp = false;
        public Vector3 up;
        public float defaultPitch;

        public AudioSource audioSource;
        public LungProp apparatusRef; 
        
        public int storedHitForce = 0; 

        private float lastSoundTime = 0f;
        private const float SOUND_COOLDOWN = 0.15f;
        private const float MIN_SOUND_VELOCITY = 1.2f;

        public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            return true;
        }
        
        void Awake()
        {
            grabbableObjectRef = gameObject.GetComponent<GrabbableObject>();
            if (grabbableObjectRef == null) return;

            if (grabbableObjectRef.propBody != null)
            {
                rigidbody = grabbableObjectRef.propBody;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            else if (!TryGetComponent(out rigidbody))
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rigidbody.isKinematic = true; 
            }

            networkObject = GetComponent<NetworkObject>();
            ScanNodeProperties scanNodeProperties = GetComponentInChildren<ScanNodeProperties>();
            if (scanNodeProperties != null && !scanNodeProperties.gameObject.TryGetComponent(out scanNodeRigid))
            {
                scanNodeRigid = scanNodeProperties.gameObject.AddComponent<Rigidbody>();
            }
            if (!TryGetComponent(out networkTransform) && networkObject != null)
            {
                networkTransform = gameObject.AddComponent<NetworkTransform>();
            }
            if (!TryGetComponent(out networkRigidbody) && networkObject != null)
            {
                networkRigidbody = gameObject.AddComponent<NetworkRigidbody>();
            }
            
            if (grabbableObjectRef is LungProp lungProp && lungProp != null)
            {
                apparatusRef = lungProp;
            }
            audioSource = Utils.Physics.CopyComponent(gameObject.GetComponent<AudioSource>(), gameObject);
            physicsHelperRef = gameObject.AddComponent<PhysicsHelper>();
            collider = gameObject.GetComponent<Collider>();
            oldVolume = audioSource != null ? audioSource.volume : 1f;
            defaultPitch = audioSource != null ? audioSource.pitch : 1f;
            up = grabbableObjectRef.itemProperties.verticalOffset * Vector3.up;
            grabbableObjectRef.itemProperties.syncDiscardFunction = true;
            if (LethalThingsCompatibility.enabled)
            {
                LethalThingsCompatibility.ApplyFixes(this);
            }
        }

        public int gridSize = 5;
        public float cellSize = 2f;
        public Collider[] results = new Collider[3];

        public void FixPosition()
        {
            if (rigidbody.isKinematic) return;
            Dictionary<Vector3, GameObject> primitives = new Dictionary<Vector3, GameObject>();
            Vector3 closestFreeSpot = Vector3.zero;
            float minDistance = Mathf.Infinity;
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 cellCenter = new Vector3((x - gridSize / 2) * cellSize / 2, 0, (y - gridSize / 2) * cellSize / 2) + new Vector3(cellSize, 0, cellSize) * 0.5f;
                    if (cellCenter == new Vector3(cellSize, 0, cellSize) * 0.5f) cellCenter = Vector3.zero;
                    Vector3 halfExtents = new Vector3(cellSize / 2, cellSize / 2, cellSize / 2);
                    cellCenter += transform.position;
                    
                    int overlap = Physics.OverlapBoxNonAlloc(cellCenter, halfExtents, results, Quaternion.identity, 2318, QueryTriggerInteraction.Ignore);
                    if (overlap <= 1 || overlap <= 0)
                    {
                        float distance = Vector3.Distance(cellCenter, transform.position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestFreeSpot = cellCenter;
                            if (closestFreeSpot == transform.position) break;
                        }
                    }
                }
            }
            if (minDistance != Mathf.Infinity)
            {
                transform.position = closestFreeSpot;
            }
        }

        public void SetPosition()
        {
            Transform parent = GetParent();
            if (parent != transform)
            {
                Vector3 relativePosition = parent.InverseTransformPoint(transform.position);
                transform.localPosition = relativePosition;
            }
        }

        public void SetRotation()
        {
            if (grabbableObjectRef.floorYRot == -1)
            {
                transform.rotation = Quaternion.Euler(grabbableObjectRef.itemProperties.restingRotation.x, grabbableObjectRef.transform.eulerAngles.y, grabbableObjectRef.itemProperties.restingRotation.z);
            }
            else
            {
                transform.rotation = Quaternion.Euler(grabbableObjectRef.itemProperties.restingRotation.x, grabbableObjectRef.floorYRot + grabbableObjectRef.itemProperties.floorYOffset + 90f, grabbableObjectRef.itemProperties.restingRotation.z);
            }
        }

        void InitializeVariables()
        {
            alreadyPickedUp = !Plugin.Instance.physicsOnPickup.Value;
            grabbableObjectRef.itemProperties.itemSpawnsOnGround = Plugin.Instance.physicsOnPickup.Value;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.drag = 0.1f;
            if (apparatusRef != null) rigidbody.isKinematic = apparatusRef.isLungDocked || apparatusRef.isLungDockedInElevator || isPlaced;
            else rigidbody.isKinematic = isPlaced;

            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;

            networkTransform.enabled = Plugin.Instance.ServerHasMod;

            grabbableObjectRef.fallTime = 1f;
            grabbableObjectRef.reachedFloorTarget = true;
            Plugin.Instance.skipObject.Remove(grabbableObjectRef);

            if (scanNodeRigid != null)
            {
                scanNodeRigid.isKinematic = true;
                scanNodeRigid.useGravity = false;
            }
            rigidbody.velocity = Vector3.zero;
            FixPosition();
            rigidbody.velocity = Vector3.zero;

            addedWeight = false;
            isPushed = false;
        }

        void UninitializeVariables()
        {
            grabbableObjectRef.EnablePhysics(false); 
            Plugin.Instance.skipObject.Add(grabbableObjectRef);
            EnableColliders(true);

            grabbableObjectRef.fallTime = 0f;
            grabbableObjectRef.reachedFloorTarget = false;
            networkTransform.enabled = false;
            firstHit = false;
            hitDir = Vector3.zero;
            oldValue = false;
            addedWeight = false;
            isPushed = false;
        }

        void OnEnable()
        {
            InitializeVariables();
        }

        void OnDisable()
        {
            UninitializeVariables();
        }

        bool HasRequiredComponents()
        {
            if (rigidbody == null) return false;
            if (networkTransform == null) return false;
            else if (GetComponent<NetworkObject>() == null) return false;
            if (grabbableObjectRef == null) return false;
            return true;
        }
        
        public float calculatedMass;
        public float clampedMass;

        void Start()
        {
            if (!HasRequiredComponents()) return;
            
            rigidbody.useGravity = false; 
            
            calculatedMass = ((grabbableObjectRef.itemProperties.weight - 1) * 105f);
            clampedMass = Mathf.Clamp(grabbableObjectRef.itemProperties.weight - 1f, 0f, 10f);
            rigidbody.mass = Mathf.Max(calculatedMass, 1) / 2.205f;
            throwForce = rigidbody.mass * 10f;
            terminalVelocity = MathF.Sqrt(2 * rigidbody.mass * gravity);
            
            if (StartOfRound.Instance.inShipPhase)
            {
                grabbableObjectRef.fallTime = 1f;
                grabbableObjectRef.hasHitGround = true;
                grabbableObjectRef.scrapPersistedThroughRounds = true;
                grabbableObjectRef.isInElevator = true;
                grabbableObjectRef.isInShipRoom = true;
            }
        }

        bool IsHostOrServer
        {
            get
            {
                return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
            }
        }
        
        protected virtual void FixedUpdate()
        {
            if (IsHostOrServer || !Plugin.Instance.ServerHasMod)
            {
                if (!rigidbody.isKinematic && !grabbableObjectRef.isHeld)
                {
                    rigidbody.useGravity = false;
                    
                    float weightGravityMultiplier = 1f + (clampedMass * 0.2f);
                    
                    rigidbody.AddForce(Vector3.down * gravity * weightGravityMultiplier, ForceMode.Acceleration);
                }
                else
                {
                    rigidbody.velocity = Vector3.zero;
                }
            }
        }

        public void EnableColliders(bool enable)
        {
            if (collider != null) collider.enabled = enable;
            for (int i = 0; i < grabbableObjectRef.propColliders.Length; i++)
            {
                if (!(grabbableObjectRef.propColliders[i] == null) && !grabbableObjectRef.propColliders[i].gameObject.CompareTag("InteractTrigger") && !grabbableObjectRef.propColliders[i].gameObject.CompareTag("DoNotSet"))
                {
                    grabbableObjectRef.propColliders[i].enabled = enable;
                    grabbableObjectRef.propColliders[i].excludeLayers = 0; 
                }
            }
        }

        bool oldValue = false;
        private Transform parent;

        public bool addedWeight = false;

        protected virtual void Update()
        {
            if (isPushed && !addedWeight)
            {
                addedWeight = true;
                GameNetworkManager.Instance.localPlayerController.carryWeight += clampedMass;
            }
            if (oldValue != Plugin.Instance.ServerHasMod)
            {
                oldValue = Plugin.Instance.ServerHasMod;
                networkTransform.enabled = Plugin.Instance.ServerHasMod;
            }
            
            if (apparatusRef != null)
            {
                bool isDocked = apparatusRef.isLungDocked || apparatusRef.isLungDockedInElevator || (!StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.inShipPhase || isPlaced);
                
                if (Plugin.Instance.freezeInShip.Value && (grabbableObjectRef.isInShipRoom || grabbableObjectRef.isInElevator))
                {
                    isDocked = true;
                }

                if (rigidbody.isKinematic != isDocked) rigidbody.isKinematic = isDocked;
            }
            else
            {
                bool shouldBeKinematic = ((grabbableObjectRef.isInShipRoom || grabbableObjectRef.isInElevator) && !StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.inShipPhase) || isPlaced; 
                
                if (Plugin.Instance.freezeInShip.Value && (grabbableObjectRef.isInShipRoom || grabbableObjectRef.isInElevator))
                {
                    shouldBeKinematic = true;
                }

                if (rigidbody.isKinematic != shouldBeKinematic) rigidbody.isKinematic = shouldBeKinematic;
            }
            
            if (grabbableObjectRef.isInShipRoom || grabbableObjectRef.isInElevator)
            {
                if (rigidbody.isKinematic && !isPlaced)
                {
                    if (addedWeight) GameNetworkManager.Instance.localPlayerController.carryWeight -= clampedMass;
                    alreadyPickedUp = false;
                    SetPosition();
                    enabled = false;
                }
            }
        }

        public Transform GetParent()
        {
            if (grabbableObjectRef.parentObject != null) parent = grabbableObjectRef.parentObject;
            else if (transform.parent != null) parent = transform.parent;
            else parent = transform;
            return parent;
        }

        public void PlayDropSFX()
        {
            if (Time.time - lastSoundTime < SOUND_COOLDOWN) return;

            var force = Vector3.zero;
            if (isHit)
            {
                isHit = false;
                
                float clampedHitForce = Mathf.Clamp(storedHitForce, 0f, 15f); 
                float massResistance = Mathf.Max(rigidbody.mass, 1f);
                float finalKnockback = (clampedHitForce * 4f) / massResistance;
                
                force = hitDir * finalKnockback;
                rigidbody.velocity = force;
            }

            if (velocityMag < MIN_SOUND_VELOCITY && force == Vector3.zero) return;

            if (grabbableObjectRef.itemProperties.dropSFX != null)
            {
                AudioClip clip = grabbableObjectRef.itemProperties.dropSFX;
                if (Plugin.Instance.useSourceSounds.Value) clip = Utils.ListUtil.GetRandomElement(Utils.AssetLoader.allAudioList);
                float? vol = null;
                if (audioSource != null)
                {
                    if (force != Vector3.zero) vol = Mathf.Min(force.magnitude, Plugin.Instance.maxCollisionVolume.Value);
                    else vol = Mathf.Clamp(velocityMag, 0.2f, Plugin.Instance.maxCollisionVolume.Value); 
                    
                    audioSource.volume = vol.Value; 
                    audioSource.pitch = Utils.Physics.mapValue(rigidbody.velocity.magnitude, .9f, 10f, .9f, defaultPitch + 0.5f);
                    audioSource.PlayOneShot(clip, audioSource.volume);
                    lastSoundTime = Time.time;
                }
                if (grabbableObjectRef.IsOwner)
                {
                    RoundManager.Instance.PlayAudibleNoise(gameObject.transform.position, vol.HasValue ? vol.Value * 8f : 8f, 0.5f, 0, grabbableObjectRef.isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
                }
            }
            grabbableObjectRef.hasHitGround = true;
        }

        static bool firstHit = false;
        static Vector3 velocity;
        static float velocityMag;

        protected virtual void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.layer == 26 && GetPlayer(collision.gameObject) == GameNetworkManager.Instance.localPlayerController && isPushed)
            {
                isPushed = false;
                if (addedWeight)
                {
                    addedWeight = false;
                    GameNetworkManager.Instance.localPlayerController.carryWeight -= clampedMass;
                }
            }
        }

        public bool isPushed = false;

        Dictionary<GameObject, PlayerControllerB> Players = new Dictionary<GameObject, PlayerControllerB>();

        private PlayerControllerB GetPlayer(GameObject obj)
        {
            if (Players.ContainsKey(obj)) return Players[obj];
            Players[obj] = obj.GetComponent<PlayerControllerB>();
            if (Players[obj] == null) Players[obj] = obj.GetComponentInParent<PlayerControllerB>();
            return Players[obj];
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == 26)
            {
                if (Plugin.Instance.disablePlayerCollision.Value)
                {
                    UnityEngine.Physics.IgnoreCollision(collider, collision.gameObject.GetComponent<Collider>(), true);
                    isPushed = false;
                    return;
                }

                if (GetPlayer(collision.gameObject) == GameNetworkManager.Instance.localPlayerController)
                {
                    isPushed = true;
                }
                return;
            }

            if (!firstHit) 
            {
                firstHit = true;
                return;
            }

            if (IsHostOrServer)
            {
                NetworkObjectReference networkRef = networkObject;
                FastBufferWriter writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkRef), Unity.Collections.Allocator.Temp);
                writer.WriteValueSafe(networkRef);
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(OnCollision.CollisionCheck, client.ClientId, writer, NetworkDelivery.ReliableSequenced);
                }
            }
            else if (!Plugin.Instance.ServerHasMod) 
            {
                PlayDropSFX();
            }
    
            velocity = rigidbody.velocity;
            velocityMag = velocity.magnitude;
        }

        void OnDestroy()
        {
            Utils.Physics.RemovePhysicsComponent(gameObject);
        }

        Vector3? oldPosition;

        protected virtual void LateUpdate()
        {
            if (grabbableObjectRef == null)
            {
                Destroy(this);
                return;
            }
            if (grabbableObjectRef.parentObject != null && grabbableObjectRef.isHeld)
            {
                transform.rotation = grabbableObjectRef.parentObject.rotation;
                Vector3 rotationOffset = grabbableObjectRef.itemProperties.rotationOffset;
                transform.Rotate(rotationOffset);
                transform.position = grabbableObjectRef.parentObject.position;
                Vector3 positionOffset = grabbableObjectRef.itemProperties.positionOffset;
                positionOffset = grabbableObjectRef.parentObject.rotation * positionOffset;
                transform.position += positionOffset;
            }
            if (grabbableObjectRef.radarIcon != null) grabbableObjectRef.radarIcon.position = transform.position;
            if (oldPosition.HasValue && grabbableObjectRef.isHeld)
            {
                heldVelocityMagnitudeSqr = (oldPosition.Value - transform.position).sqrMagnitude;
                heldVelocityNormalized = (transform.position - oldPosition.Value).normalized;
            }
            else
            {
                heldVelocityMagnitudeSqr = 0;
                heldVelocityNormalized = Vector3.zero;
            }
            oldPosition = transform.position;
        }
        
        public float heldVelocityMagnitudeSqr;
        public Vector3 heldVelocityNormalized;
        public bool isHit = false;
        public Vector3 hitDir = Vector3.zero;
        
        public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            hitDir = hitDirection;
            storedHitForce = force;
            isHit = true;
            
            if (IsHostOrServer)
            {
                NetworkObjectReference networkRef = networkObject;
                FastBufferWriter writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkRef), Unity.Collections.Allocator.Temp);
                writer.WriteValueSafe(networkRef);
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(OnCollision.CollisionCheck, client.ClientId, writer, NetworkDelivery.ReliableSequenced);
                }
            }
            else if (!Plugin.Instance.ServerHasMod) PlayDropSFX();
            
            return true;
        }
    }
}