using System;
using Nop.Core;

namespace Majako.Plugin.Payments.KlarnaCheckout.Domain
{
    public class KcoOrderRequest : BaseEntity
    {
        public int StoreId { get; set; }
        public string KlarnaResourceUri { get; set; }
        public int CustomerId { get; set; }
        public int AffiliateId { get; set; }
        public Guid OrderGuid { get; set; }
        public bool IsCompleted { get; set; }
        public string IpAddress { get; set; }
        public string ShippingRateComputationMethodSystemName { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
