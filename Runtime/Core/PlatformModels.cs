using System;

namespace PlatformTools
{
    [Serializable]
    public sealed class LeaderboardEntry
    {
        public int rank;
        public string playerId;
        public string playerName;
        public int score;
    }

    [Serializable]
    public sealed class ProductInfo
    {
        public string productId;
        public string title;
        public string description;
        public string price;
        public string priceCurrencyCode;
    }

    public enum PurchaseStatus { Success, Cancelled, Failed, NotSupported }

    public readonly struct PurchaseResult
    {
        public readonly PurchaseStatus Status;
        public readonly string ProductId;
        public readonly string PurchaseToken;
        public readonly string Error;

        public PurchaseResult(PurchaseStatus status, string productId = null, string purchaseToken = null, string error = null)
        {
            Status = status;
            ProductId = productId;
            PurchaseToken = purchaseToken;
            Error = error;
        }

        public static PurchaseResult NotSupported(string productId) =>
            new PurchaseResult(PurchaseStatus.NotSupported, productId, error: "not_supported");
    }
}