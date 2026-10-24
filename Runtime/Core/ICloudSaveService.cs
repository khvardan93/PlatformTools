using Cysharp.Threading.Tasks;

namespace PlatformTools
{
    public interface ICloudSaveService
    {
        /// Returns stored JSON, or null if nothing is saved under key.
        UniTask<string> LoadAsync(string key);
        UniTask SaveAsync(string key, string json);
    }
}