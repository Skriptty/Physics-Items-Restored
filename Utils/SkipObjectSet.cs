using System;
using System.Collections.Generic;

namespace Physics_Items
{
    internal class SkipObjectSet
    {
        private readonly HashSet<GrabbableObject> set = new HashSet<GrabbableObject>();

        public bool Add(GrabbableObject item)
        {
            Plugin.Logger.LogWarning($"[SKIP-ADD] {item?.name} desde:\n{Environment.StackTrace}");
            return set.Add(item);
        }

        public bool Remove(GrabbableObject item)
        {
            Plugin.Logger.LogWarning($"[SKIP-REMOVE] {item?.name}");
            return set.Remove(item);
        }

        public bool Contains(GrabbableObject item) => set.Contains(item);
    }
}