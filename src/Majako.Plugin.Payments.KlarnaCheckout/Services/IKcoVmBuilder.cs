using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Majako.Plugin.Payments.KlarnaCheckout.ViewModels;
using Nop.Web.Models.Checkout;

namespace Majako.Plugin.Payments.KlarnaCheckout.Services
{
    public interface IKcoVmBuilder
    {
        List<ShippingMethodVm> GetShippingMethods(CheckoutShippingMethodModel model);
    }
}
