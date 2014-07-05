using Klarna.Checkout.HTTP;
using Majako.Plugin.Payments.KlarnaCheckout.Models;
using Nop.Services.Orders;

namespace Majako.Plugin.Payments.KlarnaCheckout.Services
{
    public interface IKcoProcessor
    {
        string Create();
        IHttpResponse Update(string resourceUri);
        IHttpResponse Fetch(string resourceUri);
        void Acknowledge(string resourceUri, int orderId);

        PlaceOrderResult PlaceOrder(KlarnaOrder klarnaOrder, string resourceUri, out int orderId);
    }
}