using System.Data.Entity.ModelConfiguration;
using Majako.Plugin.Payments.KlarnaCheckout.Domain;

namespace Majako.Plugin.Payments.KlarnaCheckout.Data
{
    public class KcoOrderRequestMap : EntityTypeConfiguration<KcoOrderRequest>
    {
        public KcoOrderRequestMap()
        {
            ToTable("KcoOrderRequest");

            HasKey(m => m.Id);
            Property(m => m.OrderGuid).IsRequired();
            Property(m => m.StoreId).IsRequired();
            Property(m => m.CustomerId).IsRequired();
            Property(m => m.OrderGuid).IsRequired();
            Property(m => m.KlarnaResourceUri).IsRequired();
            Property(m => m.IsCompleted).IsRequired();
            Property(m => m.IpAddress);
            Property(m => m.ShippingRateComputationMethodSystemName);
            Property(m => m.CreatedOnUtc).IsRequired();
        }
    }
}
