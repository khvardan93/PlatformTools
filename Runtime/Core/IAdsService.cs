using System;
using Cysharp.Threading.Tasks;

namespace PlatformTools
{
    public interface IAdsService
    {
        /// placement is a logical name from the game ("round_end"); platforms map it to real ids.
        UniTask<bool> ShowInterstitialAsync(string placement);

        /// Returns true only if the reward was earned.
        UniTask<bool> ShowRewardedAsync(string placement);

        /// true when an ad starts, false when it ends. Game pauses and mutes while true.
        event Action<bool> AdShowing;
    }
}