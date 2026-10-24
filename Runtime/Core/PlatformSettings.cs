using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformTools
{
    [CreateAssetMenu(menuName = "PlatformTools/Platform Settings", fileName = "PlatformSettings")]
    public sealed class PlatformSettings : ScriptableObject
    {
        [Tooltip("Used for build and zip names, e.g. 'domino'.")]
        public string gameId = "game";

        [Tooltip("Safety net: an ad call that never returns resolves as failed after this.")]
        public float adTimeoutSeconds = 30f;

        public MockPlatformConfig mockConfig;
        public FacebookSettings facebook = new FacebookSettings();
        public OkSettings ok = new OkSettings();
    }

    [Serializable]
    public sealed class FacebookSettings
    {
        public List<PlacementId> interstitialPlacements = new List<PlacementId>();
        public List<PlacementId> rewardedPlacements = new List<PlacementId>();
        [Tooltip("fbapp-config.json, copied into the zip by the packager.")]
        public TextAsset appConfigJson;
    }

    [Serializable]
    public sealed class OkSettings
    {
        [Tooltip("OK asks for 10-15 s between loadAd and showLoadedAd calls.")]
        public float rewardedCooldownSeconds = 15f;
    }

    /// Maps a logical placement ("round_end") to the platform's real id.
    [Serializable]
    public sealed class PlacementId
    {
        public string placement;
        public string id;
    }
}