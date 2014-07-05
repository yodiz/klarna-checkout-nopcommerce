using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Web.Framework.Web;

namespace Majako.Plugin.Payments.KlarnaCheckout
{
    public class PaymentsKlarnaCheckoutPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly KcoObjectContext _context;

        public PaymentsKlarnaCheckoutPlugin(KcoObjectContext context)
        {
            _context = context;
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string>() { "shopping_cart_bottom" };
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentsKlarnaCheckout";
            routeValues = new RouteValueDictionary { { "Namespaces", "Majako.Plugin.Misc.PaymentsKlarnaCheckout.Controllers" }, { "area", null } };
        }

        public void GetDisplayWidgetRoute(string widgetZone, out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PublicInfo";
            controllerName = "PaymentsKlarnaCheckout";
            routeValues = new RouteValueDictionary()
            {
                {"Namespaces", "Majako.Plugin.Payments.KlarnaCheckout.Controllers"},
                {"area", null},
                {"widgetZone", widgetZone}
            };
        }

        public override void Install()
        {
            _context.Install();

            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.EId", "Butiks-ID(EID)");
            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.SharedSecret", "Lösneord");
            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.TermsUrl", "Köpvillkor URL");
            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.CheckoutUrl", "Kassa URL");
            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.TestMode", "Testläge");
            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.PlaceOrderToRegisteredAccount", "Lägg order på registrerat kontot");
            this.AddOrUpdatePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.PlaceOrderToRegisteredAccount.Hint", "Om kund lägger beställning som gäst men har ett konto, lägg order på detta konto.");
            base.Install();
        }

        public override void Uninstall()
        {
            _context.Unistall();

            this.DeletePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.EId");
            this.DeletePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.SharedSecret");
            this.DeletePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.TermsUrl");
            this.DeletePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.TestMode");
            this.DeletePluginLocaleResource("Majako.Plugin.Payments.KlarnaCheckout.Settings.PlaceOrderToRegisteredAccount");
            base.Uninstall();
        }
    }
}