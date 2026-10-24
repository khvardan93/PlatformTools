namespace PlatformTools
{
    public sealed class PlatformCapabilities
    {
        public bool Interstitial { get; }
        public bool Rewarded { get; }
        public bool CloudSave { get; }
        public bool Leaderboard { get; }
        public bool Payments { get; }
        public bool Share { get; }
        public bool Invite { get; }

        public PlatformCapabilities(
            bool interstitial = false, bool rewarded = false, bool cloudSave = false,
            bool leaderboard = false, bool payments = false, bool share = false, bool invite = false)
        {
            Interstitial = interstitial;
            Rewarded = rewarded;
            CloudSave = cloudSave;
            Leaderboard = leaderboard;
            Payments = payments;
            Share = share;
            Invite = invite;
        }
    }
}