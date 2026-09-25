using System.Collections.Generic;
using UnityEngine;

namespace Physics_Items.Utils
{
    internal class ListUtil
    {
        public static T GetRandomElement<T>(List<T> objects, ref int index)
        {
            if (objects.Count == 0)
            {
                index = -1;
                return default;
            }
            index = UnityEngine.Random.Range(0, objects.Count);
            return objects[index];
        }
        public static T GetRandomElement<T>(List<T> objects)
        {
            int num = 0;
            return GetRandomElement<T>(objects, ref num);
        }

        public static string[] GetLayersFromMask(int layerMask)
        {
            var layers = new List<string>();
            for (int i = 0; i < 32; ++i)
            {
                int shifted = 1 << i;
                if ((layerMask & shifted) == shifted)
                    layers.Add(LayerMask.LayerToName(i));
            }
            return layers.ToArray();
        }
    }
}
