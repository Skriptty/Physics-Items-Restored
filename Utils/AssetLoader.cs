using System.Collections.Generic;
using UnityEngine;

namespace Physics_Items.Utils
{
    internal class AssetLoader
    {
        internal static List<AudioClip> allAudioList { get; private set; } = new List<AudioClip>();
        internal static AssetBundle bundle { get; private set; }
        internal static void LoadAssetBundles()
        {
            bundle = AssetBundle.LoadFromMemory(Properties.Resources.physicsitems);
            if (bundle == null)
            {
                Plugin.Logger.LogWarning($"Assetbundle could not be loaded!");
                return;
            }
            foreach(var assetName in bundle.GetAllAssetNames())
            {
                Object asset = bundle.LoadAsset<Object>(assetName);
                if (asset == null)
                {
                    Plugin.Logger.LogWarning($"Asset {assetName} could not be loaded because it is not an Object!");
                    continue;
                }
                Plugin.Logger.LogWarning($"Loaded: {assetName}");
                if(asset is AudioClip clip && !allAudioList.Contains(clip))
                {
                    allAudioList.Add(clip);
                    Plugin.Logger.LogInfo($"Added custom audio clip: {clip.name}");
                }
            }
        }
    }
}
