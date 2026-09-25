using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Physics_Items.ItemPhysics.Environment
{
    internal class Landmine
    {
        static Vector3 previousExplosion = Vector3.zero;
        public static void Init()
        {
            On.Landmine.SpawnExplosion += Landmine_SpawnExplosion;
        }

        private static void Landmine_SpawnExplosion(
            On.Landmine.orig_SpawnExplosion orig,
            UnityEngine.Vector3 explosionPosition,
            bool spawnExplosionEffect,
            float killRange,
            float damageRange,
            int nonLethalDamage,
            float physicsForce,
            GameObject overridePrefab,
            bool goThroughCar)
        {

            orig(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, overridePrefab, goThroughCar);
            previousExplosion = explosionPosition;
            List<Collider> list = Physics.OverlapSphere(explosionPosition, 6f, 64, QueryTriggerInteraction.Collide).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 local = ((explosionPosition + Vector3.up) - list[i].transform.position);
                float magnitude = Utils.Physics.FastInverseSqrt(local.sqrMagnitude);
                Vector3 normal = (local).normalized;
                if (Utils.Physics.GetPhysicsComponent(list[i].gameObject, out PhysicsComponent physics))
                {
                    physics.alreadyPickedUp = true;
                    physics.grabbableObjectRef.EnablePhysics(true);
                    
                    float baseForce = Plugin.Instance.explosionForceMultiplier.Value; 
                    float massResistance = Mathf.Max(physics.rigidbody.mass, 1f);
                    
                    physics.rigidbody.velocity = (local * baseForce / magnitude) / massResistance;
                }
            }
        }
    }
}