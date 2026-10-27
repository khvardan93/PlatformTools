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
#else
            return new PlatformTools.Mock.MockPlatform(settings != null ? settings.mockConfig : null);
#endif
        }
    }
}