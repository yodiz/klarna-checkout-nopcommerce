using Nop.Web.Framework.Themes;

namespace Majako.Plugin.Payments.KlarnaCheckout.ViewEngines
{
    public class KcoViewEngine : ThemeableRazorViewEngine
    {
        public KcoViewEngine()
        {
            PartialViewLocationFormats =
                new[]
                {
                    "~/Plugins/Payments.KlarnaCheckout/Views/PaymentsKlarnaCheckout/{0}.cshtml"
                };

            ViewLocationFormats =
                new[]
                {
                    "~/Plugins/Payments.KlarnaCheckout/Views/PaymentsKlarnaCheckout/{0}.cshtml"
                };

        }
    }
}
