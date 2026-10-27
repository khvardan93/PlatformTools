namespace PlatformTools
{
    public static class PlatformFactory
    {
        public static IPlatformService Create(PlatformSettings settings)
        {
#if PLATFORM_FACEBOOK && !UNITY_EDITOR
            return new PlatformTools.Facebook.FacebookPlatform(settings);
#elif PLATFORM_OK && !UNITY_EDITOR
            return new PlatformTools.OK.OkPlatform(settings);
#elif PLATFORM_UNITY_PLAY && !UNITY_EDITOR
            return new PlatformTools.UnityPlay.UnityPlayPlatform(settings);
#else
            return new PlatformTools.Mock.MockPlatform(settings ? settings.mockConfig : null);
#endif
        }
    }
}