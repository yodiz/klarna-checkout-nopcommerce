using Klarna.Checkout.HTTP;
using Majako.Plugin.Payments.KlarnaCheckout.Models;
using Nop.Services.Orders;
using System.Collections.Generic;

namespace Majako.Plugin.Payments.KlarnaCheckout.Services
{
    public interface IKcoProcessor
    {
        string Create(RenderForDevice renderFor);
        void Update(RenderForDevice renderFor, string resourceUri);
        KlarnaOrder Fetch(string resourceUri);
        void Acknowledge(string resourceUri, int orderId);

        PlaceOrderResult PlaceOrder(KlarnaOrder klarnaOrder, string resourceUri, out int orderId);
    }
}