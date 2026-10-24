using System;
using Cysharp.Threading.Tasks;

namespace PlatformTools
{
    public interface IPlatformService
    {
        string PlatformId { get; }
        bool IsInitialized { get; }
        PlatformCapabilities Capabilities { get; }

        UniTask InitAsync();
        void SetLoadingProgress(float progress01);
        void GameReady();
        void GameplayStarted();
        void GameplayStopped();

        /// Platform asks the game to pause (true) or resume (false), e.g. tab hidden.
        event Action<bool> PauseRequested;
    }
}