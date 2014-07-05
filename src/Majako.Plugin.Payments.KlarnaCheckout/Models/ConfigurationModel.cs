using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Majako.Plugin.Payments.KlarnaCheckout.Models
{
    public class ConfigurationModel : BaseNopModel
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

        [NopResourceDisplayName("Majako.Plugin.Payments.KlarnaCheckout.Settings.PlaceOrderToRegisteredAccount")]
        public bool PlaceOrderToRegisteredAccount { get; set; }
    }
}