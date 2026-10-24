namespace PlatformTools
{
    public interface IPlayerService
    {
        string Id { get; }
        string DisplayName { get; }
        string PhotoUrl { get; }
        /// Platform locale like "en_US" or "ru_RU". Empty if unknown.
        string Locale { get; }
    }
}