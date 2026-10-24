using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace PlatformTools
{
    public interface ILeaderboardService
    {
        UniTask SubmitScoreAsync(string board, int score);
        UniTask<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string board, int count);
    }
}