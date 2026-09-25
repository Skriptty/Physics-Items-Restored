using GameNetcodeStuff;
using Physics_Items.ItemPhysics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Physics_Items.Utils
{
    internal class Physics
    {
        internal static Dictionary<GameObject, PhysicsComponent> physicsComponents = new Dictionary<GameObject, PhysicsComponent>();
        internal static List<PlayerControllerB> playerControllerBs = new List<PlayerControllerB>();

        internal static PhysicsComponent GetPhysicsComponent(GameObject gameObj)
        {
            if (physicsComponents.TryGetValue(gameObj, out var cached) && cached != null)
            {
                return cached;
            }
            var comp = gameObj.GetComponent<PhysicsComponent>();
            physicsComponents[gameObj] = comp;
            return comp;
        }

        public static float FastInverseSqrt(float number)
        {
            int i;
            float x2, y;
            const float threehalfs = 1.5F;

            x2 = number * 0.5F;
            y = number;
            i = BitConverter.ToInt32(BitConverter.GetBytes(y), 0);
            i = 0x5f3759df - (i >> 1);
            y = BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
            y = y * (threehalfs - (x2 * y * y));

            return 1 / y;
        }

        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            Type type = original.GetType();
            var dst = destination.AddComponent(type) as T;
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name" || prop.IsDefined(typeof(ObsoleteAttribute), true)) continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }
            return dst as T;
        }

        public static float mapValue(float mainValue, float inValueMin, float inValueMax, float outValueMin, float outValueMax)
        {
            return (mainValue - inValueMin) * (outValueMax - outValueMin) / (inValueMax - inValueMin) + outValueMin;
        }

        internal static bool GetPhysicsComponent(GameObject gameObj, out PhysicsComponent physicsComponent)
        {
            if (physicsComponents.TryGetValue(gameObj, out physicsComponent) && physicsComponent != null)
            {
                return true;
            }
            physicsComponent = gameObj.GetComponent<PhysicsComponent>();
            physicsComponents[gameObj] = physicsComponent;
            return physicsComponent != null;
        }

        internal static bool RemovePhysicsComponent(GameObject gameObj)
        {
            if (!physicsComponents.ContainsKey(gameObj)) return false;
            physicsComponents.Remove(gameObj);
            return true;
        }
    }
}