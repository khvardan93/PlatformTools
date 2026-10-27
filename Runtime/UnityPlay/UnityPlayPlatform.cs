using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformTools.UnityPlay
{
    /// Unity Play hosting: no SDK, no ads, browser-local saves.
    public sealed class UnityPlayPlatform :
        IPlatformService, IAdsService, IPlayerService,
        ICloudSaveService, ILeaderboardService, IPaymentsService
    {
        // Don't change these after release, or players lose their saves.
        const string SavePrefix = "pt_uplay_";
        const string PlayerIdKey = "pt_uplay_player_id";

        public UnityPlayPlatform(PlatformSettings settings) { }

        // IPlatformService
        public string PlatformId => "unity_play";
        public bool IsInitialized { get; private set; }
        public PlatformCapabilities Capabilities { get; } = new PlatformCapabilities(cloudSave: true);
        public event Action<bool> PauseRequested { add { } remove { } }

        public UniTask InitAsync()
        {
            IsInitialized = true;
            return UniTask.CompletedTask;
        }

        public void SetLoadingProgress(float progress01) { }
        public void GameReady() { }
        public void GameplayStarted() { }
        public void GameplayStopped() { }

        // IAdsService: no ads on Unity Play
        public event Action<bool> AdShowing { add { } remove { } }
        public UniTask<bool> ShowInterstitialAsync(string placement) => UniTask.FromResult(false);
        public UniTask<bool> ShowRewardedAsync(string placement) => UniTask.FromResult(false);

        // IPlayerService: anonymous, stable per browser
        public string Id
        {
            get
            {
                var id = PlayerPrefs.GetString(PlayerIdKey, null);
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(PlayerIdKey, id);
                    PlayerPrefs.Save();
                }
                return id;
            }
        }

        public string DisplayName => string.Empty;
        public string PhotoUrl => string.Empty;
        public string Locale => string.Empty; // game keeps its own language detection

        // ICloudSaveService: PlayerPrefs, which is IndexedDB in WebGL
        public UniTask<string> LoadAsync(string key)
        {
            var k = SavePrefix + key;
            return UniTask.FromResult(PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null);
        }

        public UniTask SaveAsync(string key, string json)
        {
            PlayerPrefs.SetString(SavePrefix + key, json);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        // ILeaderboardService: none
        public UniTask SubmitScoreAsync(string board, int score) => UniTask.CompletedTask;

        public UniTask<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string board, int count) =>
            UniTask.FromResult<IReadOnlyList<LeaderboardEntry>>(Array.Empty<LeaderboardEntry>());

        // IPaymentsService: none
        public UniTask<IReadOnlyList<ProductInfo>> GetCatalogAsync() =>
            UniTask.FromResult<IReadOnlyList<ProductInfo>>(Array.Empty<ProductInfo>());

        public UniTask<PurchaseResult> PurchaseAsync(string productId) =>
            UniTask.FromResult(PurchaseResult.NotSupported(productId));

        public UniTask ConsumeAsync(string purchaseToken) => UniTask.CompletedTask;
    }
}