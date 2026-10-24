using UnityEngine;

namespace PlatformTools
{
    [CreateAssetMenu(menuName = "PlatformTools/Mock Platform Config", fileName = "MockPlatformConfig")]
    public sealed class MockPlatformConfig : ScriptableObject
    {
        [Header("Ads")]
        public float adDelaySeconds = 1.5f;
        [Range(0f, 1f)] public float adFailChance = 0.1f;
        [Range(0f, 1f)] public float rewardedSkipChance = 0.1f;

        [Header("Capabilities")]
        public bool interstitial = true;
        public bool rewarded = true;
        public bool cloudSave = true;
        public bool leaderboard = true;
        public bool payments = false;
        public bool share = false;
        public bool invite = false;

        [Header("Player")]
        public string playerName = "Test Player";
        public string locale = "en_US";
    }
}