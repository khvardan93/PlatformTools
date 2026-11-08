using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Cysharp.Threading.Tasks;
using PlatformTools.JsBridge;
using UnityEngine;

namespace PlatformTools.VK
{
    /// VK Mini Apps (VK, OK, Mail.ru). The VK WebGL template loads vk-bridge and calls VKWebAppInit,
    /// this class adds player info, launch language, native ads and pause on hide.
    /// Saves stay browser-local for now (VK storage values are small, add later if needed).
    public sealed class VkPlatform :
        IPlatformService, IAdsService, IPlayerService,
        ICloudSaveService, ILeaderboardService, IPaymentsService
    {
        // Don't change these after release, or players lose their saves.
        const string SavePrefix = "pt_vk_";
        const string PlayerIdKey = "pt_vk_player_id";
        const float UserInfoTimeout = 5f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int PTVK_IsAvailable();
        [DllImport("__Internal")] static extern string PTVK_LaunchParam(string name);
        [DllImport("__Internal")] static extern void PTVK_GetUserInfo(int id, JsRequests.Callback cb);
        [DllImport("__Internal")] static extern void PTVK_ShowAd(int id, string format, JsRequests.Callback cb);
        [DllImport("__Internal")] static extern void PTVK_SubscribePause(PauseCallback cb);
#endif

        delegate void PauseCallback(int paused);
        static event Action<bool> PauseFromJs;

        [Serializable]
        sealed class VkUser
        {
            public long id;
            public string first_name;
            public string last_name;
            public string photo_200;
        }

        readonly float _adTimeout;
        VkUser _user;
        bool _adShowing;

        public VkPlatform(PlatformSettings settings)
        {
            _adTimeout = settings ? settings.adTimeoutSeconds : 30f;
        }

        static bool Available
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return PTVK_IsAvailable() == 1;
#else
                return false;
#endif
            }
        }

        static string LaunchParam(string name)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PTVK_LaunchParam(name) ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        // IPlatformService
        public string PlatformId => "vk";
        public bool IsInitialized { get; private set; }
        public PlatformCapabilities Capabilities { get; private set; } = new PlatformCapabilities(cloudSave: true);
        public event Action<bool> PauseRequested;

        public async UniTask InitAsync()
        {
            if (IsInitialized) return;

            PauseFromJs += OnPauseFromJs;
#if UNITY_WEBGL && !UNITY_EDITOR
            PTVK_SubscribePause(OnPause);
#endif

            if (Available)
            {
                Capabilities = new PlatformCapabilities(interstitial: true, rewarded: true, cloudSave: true);
#if UNITY_WEBGL && !UNITY_EDITOR
                var (id, task) = JsRequests.Create();
                PTVK_GetUserInfo(id, JsRequests.OnResult);
                var result = await JsRequests.WithTimeout(id, task, UserInfoTimeout);
                if (result.Ok && !string.IsNullOrEmpty(result.Data))
                {
                    try { _user = JsonUtility.FromJson<VkUser>(result.Data); }
                    catch (Exception e) { Debug.LogWarning($"[VK] user info parse failed: {e.Message}"); }
                }
#endif
            }
            else
            {
                Debug.LogWarning("[VK] vkBridge not found, running without VK features");
            }

            IsInitialized = true;
        }

        // The template already called VKWebAppInit, nothing else VK needs here.
        public void SetLoadingProgress(float progress01) { }
        public void GameReady() { }
        public void GameplayStarted() { }
        public void GameplayStopped() { }

        [MonoPInvokeCallback(typeof(PauseCallback))]
        static void OnPause(int paused) => PauseFromJs?.Invoke(paused == 1);

        void OnPauseFromJs(bool paused)
        {
            if (_adShowing) return; // ad flow owns pause while it runs
            PauseRequested?.Invoke(paused);
        }

        // IAdsService
        public event Action<bool> AdShowing;
        public UniTask<bool> ShowInterstitialAsync(string placement) => ShowAdAsync("interstitial");
        public UniTask<bool> ShowRewardedAsync(string placement) => ShowAdAsync("reward");

        async UniTask<bool> ShowAdAsync(string format)
        {
            if (!Available || _adShowing) return false;
#if UNITY_WEBGL && !UNITY_EDITOR
            _adShowing = true;
            AdShowing?.Invoke(true);
            try
            {
                var (id, task) = JsRequests.Create();
                PTVK_ShowAd(id, format, JsRequests.OnResult);
                var result = await JsRequests.WithTimeout(id, task, _adTimeout);
                return result.Ok;
            }
            finally
            {
                _adShowing = false;
                AdShowing?.Invoke(false);
            }
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }

        // IPlayerService
        public string Id
        {
            get
            {
                if (_user != null && _user.id != 0) return "vk_" + _user.id;
                var launchId = LaunchParam("vk_user_id");
                if (!string.IsNullOrEmpty(launchId)) return "vk_" + launchId;

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

        public string DisplayName => _user == null ? string.Empty : (_user.first_name ?? string.Empty).Trim();
        public string PhotoUrl => _user?.photo_200 ?? string.Empty;

        /// From the vk_language launch param: ru, uk, be, en, ...
        public string Locale
        {
            get
            {
                var lang = LaunchParam("vk_language");
                switch (lang)
                {
                    case "": return string.Empty;
                    case "ru": return "ru_RU";
                    case "en": return "en_US";
                    case "uk": return "uk_UA";
                    case "be": return "be_BY";
                    case "kz": return "kk_KZ";
                    default: return lang;
                }
            }
        }

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

        // ILeaderboardService: none yet
        public UniTask SubmitScoreAsync(string board, int score) => UniTask.CompletedTask;

        public UniTask<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string board, int count) =>
            UniTask.FromResult<IReadOnlyList<LeaderboardEntry>>(Array.Empty<LeaderboardEntry>());

        // IPaymentsService: none yet
        public UniTask<IReadOnlyList<ProductInfo>> GetCatalogAsync() =>
            UniTask.FromResult<IReadOnlyList<ProductInfo>>(Array.Empty<ProductInfo>());

        public UniTask<PurchaseResult> PurchaseAsync(string productId) =>
            UniTask.FromResult(PurchaseResult.NotSupported(productId));

        public UniTask ConsumeAsync(string purchaseToken) => UniTask.CompletedTask;
    }
}
