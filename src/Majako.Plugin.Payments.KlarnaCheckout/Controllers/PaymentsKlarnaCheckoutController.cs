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

        #region Fields

        #endregion

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

            return View("Majako.Plugin.Payments.KlarnaCheckout.Views.PaymentsKlarnaCheckout.Configure", model);
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

            return View("Majako.Plugin.Payments.KlarnaCheckout.Views.PaymentsKlarnaCheckout.Configure", model);
        }

        [ChildActionOnly]
        public ActionResult PublicInfo(string widgetZone)
        {
            return View("PublicInfo");
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

        public ActionResult CheckoutSnippet()
        {
            string resourceUri;
            var customer = _workContext.CurrentCustomer;
            var kcoOrderRequest = _kcoOrderRequestRepository.Table
                .OrderByDescending(x => x.CreatedOnUtc)
                .FirstOrDefault(x => x.CustomerId == customer.Id && !x.IsCompleted);

            if (kcoOrderRequest == null)
            {
                try
                {
                    resourceUri = _kcoProcessor.Create();
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
                    _kcoProcessor.Update(resourceUri);
                }
                catch
                {
                    resourceUri = _kcoProcessor.Create();
                }
            }

            var response = _kcoProcessor.Fetch(resourceUri);

            if (response.StatusCode != HttpStatusCode.OK)
                return new HttpStatusCodeResult(response.StatusCode);

            var klarnaOrder = JsonConvert.DeserializeObject<KlarnaOrder>(response.Data);
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

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var klarnaOrder = JsonConvert.DeserializeObject<KlarnaOrder>(response.Data);
                int orderId;
                _kcoProcessor.PlaceOrder(klarnaOrder, resourceUri, out orderId);
                if (orderId > 0)
                {
                    _kcoProcessor.Acknowledge(resourceUri, orderId); 
                }
            }
            else
            {
                throw new NopException("Klarna Error. Error deserializing response data.");
            }
        }

        public ActionResult ThankYou(string eId, string resourceUri)
        {
            if (string.IsNullOrEmpty(eId) || eId != _kcoSettings.EId.ToString())
                throw new NopException("Klarna Error. Invalid or no EId provided");

            if (string.IsNullOrEmpty(resourceUri))
                throw new NopException("Klarna Error. No Resource URI provided");

            var response = _kcoProcessor.Fetch(resourceUri);

            if (response.StatusCode != HttpStatusCode.OK)
                return new HttpStatusCodeResult(response.StatusCode);

            var klarnaOrder = JsonConvert.DeserializeObject<KlarnaOrder>(response.Data);

            if (klarnaOrder.status == "checkout_complete")
            {
                var model = new KlarnaCheckoutModel { Snippet = klarnaOrder.gui.snippet };
                return View("ThankYou", model);
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
