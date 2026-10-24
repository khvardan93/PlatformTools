using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace PlatformTools
{
    public interface IPaymentsService
    {
        UniTask<IReadOnlyList<ProductInfo>> GetCatalogAsync();
        UniTask<PurchaseResult> PurchaseAsync(string productId);
        UniTask ConsumeAsync(string purchaseToken);
    }
}