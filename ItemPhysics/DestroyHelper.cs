using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Physics_Items.ItemPhysics
{
    internal class DestroyHelper : MonoBehaviour
    {
        public GrabbableObject grabbableObjectRef;
        void Start()
        {
            grabbableObjectRef = GetComponent<GrabbableObject>();
        }

        void OnDestroy()
        {
            if (grabbableObjectRef == null)
            {
                Plugin.Logger.LogWarning("Grabbable object ref is null.");
                return;
            }
            if (Plugin.Instance.skipObject.Contains(grabbableObjectRef))
            {
                Plugin.Instance.skipObject.Remove(grabbableObjectRef);
            }
        }
    }
}
