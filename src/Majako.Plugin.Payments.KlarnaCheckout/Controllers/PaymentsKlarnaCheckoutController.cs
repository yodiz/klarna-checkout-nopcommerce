using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using Klarna.Checkout.HTTP;
using Majako.Plugin.Payments.KlarnaCheckout.Attributes;
using Majako.Plugin.Payments.KlarnaCheckout.Domain;
using Majako.Plugin.Payments.KlarnaCheckout.Models;
using Majako.Plugin.Payments.KlarnaCheckout.Services;
using Majako.Plugin.Payments.KlarnaCheckout.ViewModels;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Web.Controllers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Media;
using System.Text.RegularExpressions;

namespace Majako.Plugin.Payments.KlarnaCheckout.Controllers
{
    [NoCache]
    public class PaymentsKlarnaCheckoutController : JsonController
    {
        private readonly ISettingService _settingService;
        private readonly KlarnaCheckoutSettings _klarnaCheckoutSettings;
        private readonly IKcoProcessor _kcoProcessor;
        private readonly IWorkContext _workContext;
        private readonly IShippingService _shippingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPictureService _pictureService;
        private readonly MediaSettings _mediaSettings;
        private readonly IKcoVmBuilder _kcoVmBuilder;
        private readonly IStoreContext _storeContext;
        private readonly OrderSettings _orderSettings;
        private readonly KlarnaCheckoutSettings _kcoSettings;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IRepository<KcoOrderRequest> _kcoOrderRequestRepository;
        private readonly ICountryService _countryService;


        #region Constructors

        public PaymentsKlarnaCheckoutController(
            ISettingService settingService,
            KlarnaCheckoutSettings klarnaCheckoutSettings,
            IKcoProcessor kcoProcessor,
            IWorkContext workContext,
            IShippingService shippingService,
            IGenericAttributeService genericAttributeService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IPictureService pictureService,
            MediaSettings mediaSettings,
            IKcoVmBuilder kcoVmBuilder,
            IStoreContext storeContext,
            OrderSettings orderSettings,
            KlarnaCheckoutSettings kcoSettings,
            IOrderProcessingService orderProcessingService,
            IRepository<KcoOrderRequest> kcoOrderRequestRepository,
            ICountryService countryService)
        {
            _settingService = settingService;
            _klarnaCheckoutSettings = klarnaCheckoutSettings;
            _kcoProcessor = kcoProcessor;
            _workContext = workContext;
            _shippingService = shippingService;
            _genericAttributeService = genericAttributeService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _taxService = taxService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _pictureService = pictureService;
            _mediaSettings = mediaSettings;
            _kcoVmBuilder = kcoVmBuilder;
            _storeContext = storeContext;
            _orderSettings = orderSettings;
            _kcoSettings = kcoSettings;
            _orderProcessingService = orderProcessingService;
            _kcoOrderRequestRepository = kcoOrderRequestRepository;
            _countryService = countryService;
        }

        #endregion

        #region Action Methods

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.EId = _klarnaCheckoutSettings.EId;
            model.SharedSecret = _klarnaCheckoutSettings.SharedSecret;
            model.TermsUrl = _klarnaCheckoutSettings.TermsUrl;
            model.CheckoutUrl = _klarnaCheckoutSettings.CheckoutUrl;
            model.TestMode = _klarnaCheckoutSettings.TestMode;
            model.PlaceOrderToRegisteredAccount = _klarnaCheckoutSettings.PlaceOrderToRegisteredAccount;

            return View("~/Plugins/Payments.KlarnaCheckout/Views/PaymentsKlarnaCheckout/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            _klarnaCheckoutSettings.EId = model.EId;
            _klarnaCheckoutSettings.SharedSecret = model.SharedSecret;
            _klarnaCheckoutSettings.TermsUrl = model.TermsUrl;
            _klarnaCheckoutSettings.CheckoutUrl = model.CheckoutUrl;
            _klarnaCheckoutSettings.TestMode = model.TestMode;
            _klarnaCheckoutSettings.PlaceOrderToRegisteredAccount = model.PlaceOrderToRegisteredAccount;
            _settingService.SaveSetting(_klarnaCheckoutSettings);

            return View("~/Plugins/Payments.KlarnaCheckout/Views/PaymentsKlarnaCheckout/Configure.cshtml", model);
        }

        [ChildActionOnly]
        public ActionResult PublicInfo(string widgetZone)
        {
            return View("~/Plugins/Payments.KlarnaCheckout/Views/PaymentsKlarnaCheckout/PublicInfo.cshtml");
        }

        [ChildActionOnly]
        public ActionResult ShippingMethod()
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
            {
                // cart empty
            }
            if (!cart.RequiresShipping())
            {
                // shipping not required
            }
            var model = PrepareShippingMethodModel(cart);
            return PartialView("Majako.Plugin.Payments.KlarnaCheckout.Views.PaymentsKlarnaCheckout.ShippingMethods", model);
        }

        //public static object GetObject(Type myType, object value)
        //{
        //    if (myType.IsClass && myType != typeof(string))
        //    {
        //        var x = (Dictionary<string,object>)value;
        //        var obj = Activator.CreateInstance(myType);
        //        var proeprties = myType.GetProperties();
        //        foreach (var p in proeprties)
        //        {
        //            p.SetValue(obj, GetObject(p.PropertyType, x[p.Name]));
        //        }
        //        return obj;
        //    }
        //    else
        //    {
        //        return System.Convert.ChangeType(value, myType);
        //    }
        //}

        //public static T GetObject<T>(object value)
        //{
        //    return (T)GetObject(typeof(T), value);
        //}

        public ActionResult CheckoutSnippet()
        {
            string u = this.Request.ServerVariables["HTTP_USER_AGENT"];

            RenderForDevice renderFor = RenderForDevice.Desktop;

            Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if ((b.IsMatch(u) || v.IsMatch(u.Substring(0, 4))))
            {
                renderFor = RenderForDevice.Mobile;
            }

            string resourceUri;
            var customer = _workContext.CurrentCustomer;
            var kcoOrderRequest = _kcoOrderRequestRepository.Table
                .OrderByDescending(x => x.CreatedOnUtc)
                .FirstOrDefault(x => x.CustomerId == customer.Id && !x.IsCompleted);

            if (kcoOrderRequest == null)
            {
                try
                {
                    resourceUri = _kcoProcessor.Create(renderFor);
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
                }
            }
            else
            {
                try
                {
                    resourceUri = kcoOrderRequest.KlarnaResourceUri;
                    _kcoProcessor.Update(renderFor, resourceUri);
                }
                catch
                {
                    resourceUri = _kcoProcessor.Create(renderFor);
                }
            }

            var response = _kcoProcessor.Fetch(resourceUri);

            var klarnaOrder = response;
            return Json(klarnaOrder, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ChangeShippingMethod(ShippingMethodVm shippingMethod)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .Where(sci => sci.StoreId == _storeContext.CurrentStore.Id)
                    .ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");

                if (!cart.RequiresShipping())
                    throw new Exception("Shipping is not required");

                //find it
                //performance optimization. try cache first
                var shippingOptions = _workContext.CurrentCustomer.GetAttribute<List<ShippingOption>>(SystemCustomerAttributeNames.OfferedShippingOptions, _storeContext.CurrentStore.Id);
                if (shippingOptions == null || shippingOptions.Count == 0)
                {
                    //not found? let's load them using shipping service
                    shippingOptions = _shippingService
                        .GetShippingOptions(cart, _workContext.CurrentCustomer.ShippingAddress, shippingMethod.ShippingRateComputationMethodSystemName)
                        .ShippingOptions
                        .ToList();
                }
                else
                {
                    //loaded cached results. let's filter result by a chosen shipping rate computation method
                    shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingMethod.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                var shippingOption = shippingOptions
                        .Find(so => !String.IsNullOrEmpty(so.Name) && so.Name.Equals(shippingMethod.Name, StringComparison.InvariantCultureIgnoreCase));
                if (shippingOption == null)
                    throw new Exception("Selected shipping method can't be loaded");

                // Save ShippingRateComputationMethodSystemName
                var kcoOrderRequest = _kcoOrderRequestRepository.Table
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .FirstOrDefault(x => x.CustomerId == _workContext.CurrentCustomer.Id);
                if (kcoOrderRequest != null)
                {
                    kcoOrderRequest.ShippingRateComputationMethodSystemName =
                        shippingMethod.ShippingRateComputationMethodSystemName;
                    _kcoOrderRequestRepository.Update(kcoOrderRequest);
                }

                //save
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, SystemCustomerAttributeNames.SelectedShippingOption, shippingOption, _storeContext.CurrentStore.Id);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        public ActionResult ShippingMethods()
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            var model = PrepareShippingMethodModel(cart);
            var kcoOrderRequest = _kcoOrderRequestRepository.Table
                .OrderByDescending(x => x.CreatedOnUtc)
                .FirstOrDefault(x => x.CustomerId == _workContext.CurrentCustomer.Id);

            if (kcoOrderRequest == null)
                return Json(_kcoVmBuilder.GetShippingMethods(model), JsonRequestBehavior.AllowGet);

            // Save ShippingRateComputationMethodSystemName
            var shippingMethodModel = model.ShippingMethods.FirstOrDefault(sm => sm.Selected);
            if (shippingMethodModel != null)
                kcoOrderRequest.ShippingRateComputationMethodSystemName =
                    shippingMethodModel.ShippingRateComputationMethodSystemName;
            _kcoOrderRequestRepository.Update(kcoOrderRequest);

            return Json(_kcoVmBuilder.GetShippingMethods(model), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public void KcoPush(string eId, string resourceUri)
        {
            if (string.IsNullOrEmpty(eId) || eId != _kcoSettings.EId.ToString())
                throw new NopException("Klarna Error. Invalid or no EId provided");

            if (string.IsNullOrEmpty(resourceUri))
                throw new NopException("Klarna Error. No Resource URI provided");

            var response = _kcoProcessor.Fetch(resourceUri);

            var klarnaOrder = response;
            int orderId;
            _kcoProcessor.PlaceOrder(klarnaOrder, resourceUri, out orderId);
            if (orderId > 0)
            {
                _kcoProcessor.Acknowledge(resourceUri, orderId); 
            }

            //if (response.StatusCode == HttpStatusCode.OK)
            //{
            //    var klarnaOrder = JsonConvert.DeserializeObject<KlarnaOrder>(response.Data);

            //}
            //else
            //{
            //    throw new NopException("Klarna Error. Error deserializing response data.");
            //}
        }

        public ActionResult ThankYou(string eId, string resourceUri)
        {
            if (string.IsNullOrEmpty(eId) || eId != _kcoSettings.EId.ToString())
                throw new NopException("Klarna Error. Invalid or no EId provided");

            if (string.IsNullOrEmpty(resourceUri))
                throw new NopException("Klarna Error. No Resource URI provided");

            var response = _kcoProcessor.Fetch(resourceUri);

            var klarnaOrder = response;

            if (klarnaOrder.status == "checkout_complete")
            {
                var model = new KlarnaCheckoutModel { Snippet = klarnaOrder.gui.snippet };
                return View("~/Plugins/Payments.KlarnaCheckout/Views/PaymentsKlarnaCheckout/ThankYou.cshtml", model);
            }

            // Checkout not completed - redirect to cart.
            return RedirectToAction("Cart", "ShoppingCart");
        }

        #endregion

        #region Utilities

        [NonAction]
        protected CheckoutShippingMethodModel PrepareShippingMethodModel(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutShippingMethodModel();

            if (_workContext.CurrentCustomer.ShippingAddress == null)
            {
                var country = _countryService.GetAllCountriesForShipping().OrderBy(c => c.DisplayOrder).FirstOrDefault();
                _workContext.CurrentCustomer.ShippingAddress = new Address
                {
                    Country = country,
                    CountryId = country != null ? country.Id : 0,
                    CreatedOnUtc = DateTime.UtcNow
                };
            }

            var response = _shippingService
                .GetShippingOptions(cart, _workContext.CurrentCustomer.ShippingAddress, "",
                _storeContext.CurrentStore.Id);

            if (response.Success)
            {
                CheckoutShippingMethodModel.ShippingMethodModel shippingMethodModel;
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, SystemCustomerAttributeNames.OfferedShippingOptions, response.ShippingOptions);

                foreach (var option in response.ShippingOptions)
                {
                    var item = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        Name = option.Name,
                        Description = option.Description,
                        ShippingRateComputationMethodSystemName = option.ShippingRateComputationMethodSystemName
                    };
                    Discount appliedDiscount;
                    var price = _orderTotalCalculationService.AdjustShippingRate(option.Rate, cart, out appliedDiscount);
                    var shippingPrice = _taxService.GetShippingPrice(price, _workContext.CurrentCustomer);
                    var currencyPrice = _currencyService.ConvertFromPrimaryStoreCurrency(shippingPrice, _workContext.WorkingCurrency);
                    item.Fee = _priceFormatter.FormatShippingPrice(currencyPrice, true);
                    model.ShippingMethods.Add(item);
                }

                var lastShippingOption = _workContext.CurrentCustomer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, _storeContext.CurrentStore.Id);
                if (lastShippingOption != null)
                {
                    Predicate<CheckoutShippingMethodModel.ShippingMethodModel> match = so => ((!string.IsNullOrEmpty(so.Name) && so.Name.Equals(lastShippingOption.Name, StringComparison.InvariantCultureIgnoreCase)) && !string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName)) && so.ShippingRateComputationMethodSystemName.Equals(lastShippingOption.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase);

                    shippingMethodModel = model.ShippingMethods.ToList().Find(match);
                    if (shippingMethodModel != null)
                    {
                        shippingMethodModel.Selected = true;
                    }
                }
                if ((from so in model.ShippingMethods
                     where so.Selected
                     select so).FirstOrDefault<CheckoutShippingMethodModel.ShippingMethodModel>() == null)
                {
                    shippingMethodModel = model.ShippingMethods.FirstOrDefault();
                    if (shippingMethodModel != null)
                    {
                        shippingMethodModel.Selected = true;
                    }
                }
                return model;
            }
            foreach (var error in response.Errors)
            {
                model.Warnings.Add(error);
            }
            return model;
        }

        #endregion
    }
}
