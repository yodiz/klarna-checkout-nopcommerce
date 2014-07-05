using System.Web.Mvc;
using System.Web.Routing;
using Majako.Plugin.Payments.KlarnaCheckout.ViewEngines;
using Nop.Web.Framework.Mvc.Routes;

namespace Majako.Plugin.Payments.KlarnaCheckout
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            System.Web.Mvc.ViewEngines.Engines.Add(new KcoViewEngine());
            
            routes.MapRoute("Plugin.Payments.KlarnaCheckout.Configure",
                 "Plugins/PaymentsKlarnaCheckout/Configure",
                 new { controller = "PaymentsKlarnaCheckout", action = "Configure" },
                 new[] { "Majako.Plugin.Payments.KlarnaCheckout.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.KlarnaCheckout.Fetch",
                 "Plugins/PaymentsKlarnaCheckout/Fetch",
                 new { controller = "PaymentsKlarnaCheckout", action = "Fetch" },
                 new[] { "Majako.Plugin.Payments.KlarnaCheckout.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.KlarnaCheckout.ShippingMethods",
                 "Plugins/PaymentsKlarnaCheckout/ShippingMethods",
                 new { controller = "PaymentsKlarnaCheckout", action = "ShippingMethods" },
                 new[] { "Majako.Plugin.Payments.KlarnaCheckout.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.KlarnaCheckout.ChangeShippingMethod",
                 "Plugins/PaymentsKlarnaCheckout/ChangeShippingMethod",
                 new { controller = "PaymentsKlarnaCheckout", action = "ChangeShippingMethod" },
                 new[] { "Majako.Plugin.Payments.KlarnaCheckout.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.KlarnaCheckout.CheckoutSnippet",
                 "Plugins/PaymentsKlarnaCheckout/CheckoutSnippet",
                 new { controller = "PaymentsKlarnaCheckout", action = "CheckoutSnippet" },
                 new[] { "Majako.Plugin.Payments.KlarnaCheckout.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.KlarnaCheckout.KcoPush",
                 "Plugins/PaymentsKlarnaCheckout/KcoPush",
                 new { controller = "PaymentsKlarnaCheckout", action = "KcoPush" },
                 new[] { "Majako.Plugin.Payments.KlarnaCheckout.Controllers" }
            );
            
            routes.MapRoute("Plugin.Payments.KlarnaCore.ThankYou",
                 "Plugins/PaymentsKlarnaCheckout/ThankYou",
                 new { controller = "PaymentsKlarnaCheckout", action = "ThankYou" },
                 new[] { "Majako.Plugin.Payments.KlarnaCore.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
