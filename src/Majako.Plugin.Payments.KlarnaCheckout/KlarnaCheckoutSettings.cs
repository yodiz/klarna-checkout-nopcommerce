using Nop.Core.Configuration;
using Nop.Web.Framework;

namespace Majako.Plugin.Payments.KlarnaCheckout
{
    public class KlarnaCheckoutSettings : ISettings
    {
        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.EId")]
        public int EId { get; set; }

        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.SharedSecret")]
        public string SharedSecret { get; set; }

        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.TermsUrl")]
        public string TermsUrl { get; set; }

        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.CheckoutUrl")]
        public string CheckoutUrl { get; set; }

        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.TestMode")]
        public bool TestMode { get; set; }

        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.AutoRegister")]
        public bool PlaceOrderToRegisteredAccount { get; set; }
    }
}
