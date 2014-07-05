namespace Majako.Plugin.Payments.KlarnaCheckout.ViewModels
{
    public class ShippingMethodVm
    {
        public string ShippingRateComputationMethodSystemName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Fee { get; set; }
        public string ImageUrl { get; set; }
        public bool Selected { get; set; }
    }
}