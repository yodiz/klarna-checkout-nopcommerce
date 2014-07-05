using System;
using System.Collections.Generic;
using System.Linq;
using Majako.Plugin.Payments.KlarnaCheckout.ViewModels;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Media;

namespace Majako.Plugin.Payments.KlarnaCheckout.Services
{
    public class KcoVmBuilder : IKcoVmBuilder
    {
        public List<ShippingMethodVm> GetShippingMethods(CheckoutShippingMethodModel model)
        {
            return model.ShippingMethods
                .Select(shipmentMethod => new ShippingMethodVm
            {
                ShippingRateComputationMethodSystemName = shipmentMethod.ShippingRateComputationMethodSystemName,
                Name = shipmentMethod.Name,
                Description = shipmentMethod.Description,
                Fee = shipmentMethod.Fee,
                Selected = shipmentMethod.Selected
            }).ToList();
        }
    }
}