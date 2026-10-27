using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformTools.Mock
{
    public sealed class MockPlatform :
        IPlatformService, IAdsService, IPlayerService,
        ICloudSaveService, ILeaderboardService, IPaymentsService
    {
        const string SavePrefix = "pt_mock_";
        const string MockPlatformId = "mock";

        readonly MockPlatformConfig _config;
        readonly Dictionary<string, List<LeaderboardEntry>> _boards = new Dictionary<string, List<LeaderboardEntry>>();

        public MockPlatform(MockPlatformConfig config)
        {
            _config = config != null ? config : ScriptableObject.CreateInstance<MockPlatformConfig>();
            Capabilities = new PlatformCapabilities(
                interstitial: _config.interstitial,
                rewarded: _config.rewarded,
                cloudSave: _config.cloudSave,
                leaderboard: _config.leaderboard,
                payments: _config.payments,
                share: _config.share,
                invite: _config.invite);
        }

        // IPlatformService
        public string PlatformId => MockPlatformId;
        public bool IsInitialized { get; private set; }
        public PlatformCapabilities Capabilities { get; }
        public event Action<bool> PauseRequested;

        public async UniTask InitAsync()
        {
            await UniTask.Delay(200, DelayType.Realtime);
            IsInitialized = true;
            Debug.Log("[MockPlatform] Initialized");
        }

        public void SetLoadingProgress(float progress01) { }
        public void GameReady() => Debug.Log("[MockPlatform] GameReady");
        public void GameplayStarted() => Debug.Log("[MockPlatform] GameplayStarted");
        public void GameplayStopped() => Debug.Log("[MockPlatform] GameplayStopped");

        /// Call from a debug button to test the platform pause path.
        public void SimulatePause(bool pause) => PauseRequested?.Invoke(pause);

        // IAdsService
        public event Action<bool> AdShowing;

        public UniTask<bool> ShowInterstitialAsync(string placement) =>
            ShowAdAsync($"INTERSTITIAL\n{placement}", Capabilities.Interstitial, rewarded: false);

        public UniTask<bool> ShowRewardedAsync(string placement) =>
            ShowAdAsync($"REWARDED\n{placement}", Capabilities.Rewarded, rewarded: true);

        async UniTask<bool> ShowAdAsync(string label, bool supported, bool rewarded)
        {
            if (!supported) return false;

            if (UnityEngine.Random.value < _config.adFailChance)
            {
                Debug.Log($"[MockPlatform] No fill: {label}");
                return false;
            }

            AdShowing?.Invoke(true);
            MockAdOverlay.Show(label);
            await UniTask.Delay(TimeSpan.FromSeconds(_config.adDelaySeconds), DelayType.Realtime);
            MockAdOverlay.Hide();
            AdShowing?.Invoke(false);

            if (rewarded && UnityEngine.Random.value < _config.rewardedSkipChance)
            {
                Debug.Log("[MockPlatform] Rewarded skipped, no reward");
                return false;
            }
            return true;
        }

        // IPlayerService
        public string Id => "mock-player";
        public string DisplayName => _config.playerName;
        public string PhotoUrl => string.Empty;
        public string Locale => _config.locale;

        // ICloudSaveService
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

        // ILeaderboardService
        public UniTask SubmitScoreAsync(string board, int score)
        {
            if (!Capabilities.Leaderboard) return UniTask.CompletedTask;

            var list = GetBoard(board);
            var me = list.FirstOrDefault(e => e.playerId == Id);
            if (me == null) list.Add(new LeaderboardEntry { playerId = Id, playerName = DisplayName, score = score });
            else if (score > me.score) me.score = score;
            return UniTask.CompletedTask;
        }

        public UniTask<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string board, int count)
        {
            if (!Capabilities.Leaderboard)
                return UniTask.FromResult<IReadOnlyList<LeaderboardEntry>>(Array.Empty<LeaderboardEntry>());

            var top = GetBoard(board).OrderByDescending(e => e.score).Take(count).ToList();
            for (var i = 0; i < top.Count; i++) top[i].rank = i + 1;
            return UniTask.FromResult<IReadOnlyList<LeaderboardEntry>>(top);
        }

        List<LeaderboardEntry> GetBoard(string board)
        {
            if (_boards.TryGetValue(board, out var list)) return list;
            list = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { playerId = "bot1", playerName = "Aram", score = 120 },
                new LeaderboardEntry { playerId = "bot2", playerName = "Ani", score = 90 },
                new LeaderboardEntry { playerId = "bot3", playerName = "Gor", score = 60 },
            };
            _boards[board] = list;
            return list;
        }

        // IPaymentsService
        public UniTask<IReadOnlyList<ProductInfo>> GetCatalogAsync()
        {
            IReadOnlyList<ProductInfo> catalog = Capabilities.Payments
                ? new[]
                {
                    new ProductInfo { productId = "remove_ads", title = "Remove Ads", price = "$1.99", priceCurrencyCode = "USD" },
                    new ProductInfo { productId = "coins_100", title = "100 Coins", price = "$0.99", priceCurrencyCode = "USD" },
                }
                : Array.Empty<ProductInfo>();
            return UniTask.FromResult(catalog);
        }

        public async UniTask<PurchaseResult> PurchaseAsync(string productId)
        {
            if (!Capabilities.Payments) return PurchaseResult.NotSupported(productId);
            await UniTask.Delay(500, DelayType.Realtime);
            return new PurchaseResult(PurchaseStatus.Success, productId, Guid.NewGuid().ToString("N"));
        }

        public UniTask ConsumeAsync(string purchaseToken) => UniTask.CompletedTask;
    }
}