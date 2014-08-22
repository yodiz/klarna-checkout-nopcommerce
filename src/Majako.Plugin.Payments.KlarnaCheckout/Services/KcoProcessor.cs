using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Web;
using Klarna.Checkout;
using Klarna.Checkout.HTTP;
using Majako.Plugin.Payments.KlarnaCheckout.Domain;
using Majako.Plugin.Payments.KlarnaCheckout.Extensions;
using Majako.Plugin.Payments.KlarnaCheckout.Models;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Customer = Nop.Core.Domain.Customers.Customer;
using Order = Klarna.Checkout.Order;
using Nop.Services.Events;

namespace Majako.Plugin.Payments.KlarnaCheckout.Services
{
    public enum RenderForDevice
    {
        Desktop = 1, Mobile = 2
    }

    public class KcoProcessor : IKcoProcessor
    {
        #region Private Fields

        private readonly IWorkContext _workContext;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly KlarnaCheckoutSettings _kcoSettings;
        private readonly IRepository<KcoOrderRequest> _kcoOrderRequestRepository;
        private readonly ICustomerService _customerService;
        private readonly ILanguageService _languageService;
        private readonly TaxSettings _taxSettings;
        private readonly IWebHelper _webHelper;
        private readonly CurrencySettings _currencySettings;
        private readonly ICountryService _countryService;
        private readonly IAddressService _addressService;
        private readonly IOrderService _orderService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IShippingService _shippingService;
        private readonly IAffiliateService _affiliateService;
        private readonly OrderSettings _orderSettings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IPaymentService _paymentService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IGiftCardService _giftCardService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IVendorService _vendorService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        private const string CONTENT_TYPE = "application/vnd.klarna.checkout.aggregated-order-v2+json";

        #endregion

        #region Contructors
        public KcoProcessor(IWorkContext workContext,
            IPriceCalculationService priceCalculationService,
            ICurrencyService currencyService,
            ITaxService taxService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            KlarnaCheckoutSettings kcoSettings,
            IRepository<KcoOrderRequest> kcoOrderRequestRepository,
            ICustomerService customerService,
            ILanguageService languageService,
            TaxSettings taxSettings,
            IWebHelper webHelper,
            CurrencySettings currencySettings,
            ICountryService countryService,
            IAddressService addressService,
            IOrderService orderService,
            IProductAttributeFormatter productAttributeFormatter,
            IShippingService shippingService,
            IAffiliateService affiliateService,
            OrderSettings orderSettings,
            IShoppingCartService shoppingCartService,
            IPriceFormatter priceFormatter,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IPaymentService paymentService,
            IProductAttributeParser productAttributeParser,
            IGiftCardService giftCardService,
            IProductService productService,
            IDiscountService discountService,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings logLocalizationSettings,
            IVendorService vendorService,
            ICustomerActivityService customerActivityService,
            IEventPublisher eventPublisher,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            IQueuedEmailService queuedEmailService,
            CatalogSettings catalogSettings,
            ShoppingCartSettings shoppingCartSettings
            )
        {
            _workContext = workContext;
            _priceCalculationService = priceCalculationService;
            _currencyService = currencyService;
            _taxService = taxService;
            _storeContext = storeContext;
            _localizationService = localizationService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _kcoSettings = kcoSettings;
            _kcoOrderRequestRepository = kcoOrderRequestRepository;
            _customerService = customerService;
            _languageService = languageService;
            _taxSettings = taxSettings;
            _webHelper = webHelper;
            _currencySettings = currencySettings;
            _countryService = countryService;
            _addressService = addressService;
            _orderService = orderService;
            _productAttributeFormatter = productAttributeFormatter;
            _shippingService = shippingService;
            _affiliateService = affiliateService;
            _orderSettings = orderSettings;
            _shoppingCartService = shoppingCartService;
            _priceFormatter = priceFormatter;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _paymentService = paymentService;
            _productAttributeParser = productAttributeParser;
            _giftCardService = giftCardService;
            _productService = productService;
            _discountService = discountService;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = logLocalizationSettings;
            _vendorService = vendorService;
            _customerActivityService = customerActivityService;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _queuedEmailService = queuedEmailService;
            _catalogSettings = catalogSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Public Methods
        public string Create(RenderForDevice renderFor)
        {
            var connector = Connector.Create(SharedSecret);
            var klarnaCart = GetKlarnaCart();
            var merchant = GetMerchantItem(EId);
            var data = GetCartData(merchant, klarnaCart, renderFor);
            var baseUri = BaseUri;

            var order = new Order(connector)
            {
                BaseUri = new Uri(baseUri),
                ContentType = CONTENT_TYPE
            };

            order.Create(data);
            order.Fetch();
             
            var resourceUri = order.Location.ToString();
            var currentCustomer = _workContext.CurrentCustomer;

            var kcoOrderRequest = GetKcoOrderRequest(currentCustomer, resourceUri);
            _kcoOrderRequestRepository.Insert(kcoOrderRequest);

            return resourceUri;
        }

        public void Update(RenderForDevice renderFor, string resourceUri)
        {
            var connector = Connector.Create(SharedSecret);
            var klarnaCart = GetKlarnaCart();
            var merchant = GetMerchantItem(EId);
            var data = GetCartData(merchant, klarnaCart, renderFor);

            var order = new Order(connector, new Uri(resourceUri))
            {
                ContentType = CONTENT_TYPE
            };
            //var data = new Dictionary<string, object> { { "cart", klarnaCart } };

            

            order.Update(data);
        }

        public KlarnaOrder Fetch(string resourceUri)
        {
            var connector = Connector.Create(SharedSecret);
            var order = new Order(connector, new Uri(resourceUri))
            {
                ContentType = CONTENT_TYPE
            };
            order.Fetch();

            string str = Newtonsoft.Json.JsonConvert.SerializeObject(order.Marshal());

            return Newtonsoft.Json.JsonConvert.DeserializeObject<KlarnaOrder>(str);
        }

        public void Acknowledge(string resourceUri, int orderId)
        {
            try
            {
                var connector = Connector.Create(SharedSecret);
                var order = new Order(connector, new Uri(resourceUri))
                {
                    ContentType = CONTENT_TYPE
                };

                order.Fetch();

                if ((string)order.GetValue("status") == "checkout_complete")
                {
                    var reference =
                        new Dictionary<string, object>
                            {
                                { "orderid1", orderId.ToString() }
                            };
                    var data =
                        new Dictionary<string, object>
                            {
                                { "status", "created" },
                                { "merchant_reference", reference}
                            };

                    order.Update(data);
                }
            }
            catch (Exception ex)
            {
                
               throw new NopException(ex.ToString());
            }
        }
        public PlaceOrderResult PlaceOrder(KlarnaOrder klarnaOrder, string resourceUri, out int orderId)
        {
            if (klarnaOrder == null)
                throw new ArgumentNullException("klarnaOrder");

            var kcoOrderRequest = _kcoOrderRequestRepository.Table.FirstOrDefault(o => o.KlarnaResourceUri == resourceUri);

            if (kcoOrderRequest == null)
                throw new ArgumentNullException("kcoOrderRequest");

            //think about moving functionality of processing recurring orders (after the initial order was placed) to ProcessNextRecurringPayment() method
            if (kcoOrderRequest == null)
                throw new ArgumentNullException("processPaymentRequest");

            if (kcoOrderRequest.OrderGuid == Guid.Empty)
                kcoOrderRequest.OrderGuid = Guid.NewGuid();

            var result = new PlaceOrderResult();
            try
            {
                #region Order details (customer, addresses, totals)
                //customer
                var customer = _customerService.GetCustomerById(kcoOrderRequest.CustomerId);
                Customer registeredCustomer;

                if (_kcoSettings.PlaceOrderToRegisteredAccount && customer.IsGuest())
                {
                    registeredCustomer = _customerService.GetCustomerByEmail(klarnaOrder.billing_address.email) ??
                                         customer;
                    // TODO Implement auto register
                }
                else
                {
                    registeredCustomer = customer;
                }
                
                if (customer == null)
                    throw new ArgumentException("Customer is not set");

                //affilites
                int affiliateId = 0;
                var affiliate = _affiliateService.GetAffiliateById(customer.AffiliateId);
                if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                    affiliateId = affiliate.Id;

                //customer currency
                string customerCurrencyCode = "";
                decimal customerCurrencyRate = decimal.Zero;

                var currencyTmp = _currencyService.GetCurrencyById(customer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId, kcoOrderRequest.StoreId));
                var customerCurrency = (currencyTmp != null && currencyTmp.Published) ? currencyTmp : _workContext.WorkingCurrency;
                customerCurrencyCode = customerCurrency.CurrencyCode;
                var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
                customerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

                //customer language
                Language customerLanguage = null;
                customerLanguage = _languageService.GetLanguageById(customer.GetAttribute<int>(
                    SystemCustomerAttributeNames.LanguageId, kcoOrderRequest.StoreId));

                if (customerLanguage == null || !customerLanguage.Published)
                    customerLanguage = _workContext.WorkingLanguage;

                //check whether customer is guest
                if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new NopException("Anonymous checkout is not allowed");

                //billing address
                var billingAddress = AddressExists(registeredCustomer, klarnaOrder.billing_address) ??
                                     SaveKlarnaAddress(registeredCustomer, klarnaOrder);

                registeredCustomer.BillingAddress = billingAddress;

                if (registeredCustomer.BillingAddress == null)
                    throw new NopException("Billing address is not provided");

                if (!CommonHelper.IsValidEmail(registeredCustomer.BillingAddress.Email))
                    throw new NopException("Email is not valid");

                if (billingAddress.Country != null && !billingAddress.Country.AllowsBilling)
                    throw new NopException(string.Format("Country '{0}' is not allowed for billing", billingAddress.Country.Name));

                //load and validate customer shopping cart
                IList<ShoppingCartItem> cart = null;
                //load shopping cart
                cart = customer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .Where(sci => sci.StoreId == kcoOrderRequest.StoreId)
                    .ToList();

                if (cart.Count == 0)
                    throw new NopException("Cart is empty");

                //validate the entire shopping cart
                var warnings = _shoppingCartService.GetShoppingCartWarnings(cart,
                    customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes),
                    true);
                if (warnings.Count > 0)
                {
                    var warningsSb = new StringBuilder();
                    foreach (string warning in warnings)
                    {
                        warningsSb.Append(warning);
                        warningsSb.Append(";");
                    }
                    throw new NopException(warningsSb.ToString());
                }

                //validate individual cart items
                foreach (var sci in cart)
                {
                    var sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(customer, sci.ShoppingCartType,
                        sci.Product, kcoOrderRequest.StoreId, sci.AttributesXml,
                        sci.CustomerEnteredPrice, sci.Quantity, false);
                    if (sciWarnings.Count > 0)
                    {
                        var warningsSb = new StringBuilder();
                        foreach (string warning in sciWarnings)
                        {
                            warningsSb.Append(warning);
                            warningsSb.Append(";");
                        }
                        throw new NopException(warningsSb.ToString());
                    }
                }


                //min totals validation

                bool minOrderSubtotalAmountOk = _orderProcessingService.ValidateMinOrderSubtotalAmount(cart);
                if (!minOrderSubtotalAmountOk)
                {
                    decimal minOrderSubtotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                    throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount"), _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
                }
                bool minOrderTotalAmountOk = _orderProcessingService.ValidateMinOrderTotalAmount(cart);
                if (!minOrderTotalAmountOk)
                {
                    decimal minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                    throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"), _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
                }


                //tax display type
                var customerTaxDisplayType = TaxDisplayType.IncludingTax;
                if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                    customerTaxDisplayType = (TaxDisplayType)customer.GetAttribute<int>(SystemCustomerAttributeNames.TaxDisplayTypeId, kcoOrderRequest.StoreId);
                else
                    customerTaxDisplayType = _taxSettings.TaxDisplayType;

                //checkout attributes
                string checkoutAttributeDescription, checkoutAttributesXml;
                checkoutAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes);
                checkoutAttributeDescription = _checkoutAttributeFormatter.FormatAttributes(checkoutAttributesXml, customer);

                //applied discount (used to store discount usage history)
                var appliedDiscounts = new List<Discount>();

                //sub total
                decimal orderSubTotalInclTax, orderSubTotalExclTax;
                decimal orderSubTotalDiscountInclTax = 0, orderSubTotalDiscountExclTax = 0;
                //sub total (incl tax)
                decimal orderSubTotalDiscountAmount1 = decimal.Zero;
                Discount orderSubTotalAppliedDiscount1 = null;
                decimal subTotalWithoutDiscountBase1 = decimal.Zero;
                decimal subTotalWithDiscountBase1 = decimal.Zero;
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart,
                    true, out orderSubTotalDiscountAmount1, out orderSubTotalAppliedDiscount1,
                    out subTotalWithoutDiscountBase1, out subTotalWithDiscountBase1);
                orderSubTotalInclTax = subTotalWithoutDiscountBase1;
                orderSubTotalDiscountInclTax = orderSubTotalDiscountAmount1;

                //discount history
                if (orderSubTotalAppliedDiscount1 != null && !appliedDiscounts.ContainsDiscount(orderSubTotalAppliedDiscount1))
                    appliedDiscounts.Add(orderSubTotalAppliedDiscount1);

                //sub total (excl tax)
                decimal orderSubTotalDiscountAmount2 = decimal.Zero;
                Discount orderSubTotalAppliedDiscount2 = null;
                decimal subTotalWithoutDiscountBase2 = decimal.Zero;
                decimal subTotalWithDiscountBase2 = decimal.Zero;
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart,
                    false, out orderSubTotalDiscountAmount2, out orderSubTotalAppliedDiscount2,
                    out subTotalWithoutDiscountBase2, out subTotalWithDiscountBase2);
                orderSubTotalExclTax = subTotalWithoutDiscountBase2;
                orderSubTotalDiscountExclTax = orderSubTotalDiscountAmount2;

                //shipping info
                bool shoppingCartRequiresShipping = false;
                shoppingCartRequiresShipping = cart.RequiresShipping();

                var shippingAddress = AddressExists(registeredCustomer, klarnaOrder.shipping_address) ??
                                     SaveKlarnaAddress(registeredCustomer, klarnaOrder);

                registeredCustomer.ShippingAddress = shippingAddress;

                string shippingMethodName = "", shippingRateComputationMethodSystemName = "";
                if (shoppingCartRequiresShipping)
                {
                    if (registeredCustomer.ShippingAddress == null)
                        throw new NopException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(registeredCustomer.ShippingAddress.Email))
                        throw new NopException("Email is not valid");

                    //clone shipping address
                    if (shippingAddress.Country != null && !shippingAddress.Country.AllowsShipping)
                        throw new NopException(string.Format("Country '{0}' is not allowed for shipping", shippingAddress.Country.Name));

                    var shippingOption = customer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, kcoOrderRequest.StoreId);
                    if (shippingOption != null)
                    {
                        shippingMethodName = shippingOption.Name;
                        shippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                    }
                }


                //shipping total
                decimal? orderShippingTotalInclTax, orderShippingTotalExclTax = null;
                var taxRate = decimal.Zero;
                Discount shippingTotalDiscount = null;
                orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, true, out taxRate, out shippingTotalDiscount);
                orderShippingTotalExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, false);
                if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                    throw new NopException("Shipping total couldn't be calculated");

                if (shippingTotalDiscount != null && !appliedDiscounts.ContainsDiscount(shippingTotalDiscount))
                    appliedDiscounts.Add(shippingTotalDiscount);


                //payment total
                decimal paymentAdditionalFeeInclTax = 0;
                decimal paymentAdditionalFeeExclTax = 0;

                //tax total
                decimal orderTaxTotal = decimal.Zero;
                string vatNumber = "", taxRates = "";
                //tax amount
                SortedDictionary<decimal, decimal> taxRatesDictionary = null;
                orderTaxTotal = _orderTotalCalculationService.GetTaxTotal(cart, out taxRatesDictionary);

                //VAT number
                var customerVatStatus = (VatNumberStatus)customer.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId);
                if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
                    vatNumber = customer.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber);

                //tax rates
                foreach (var kvp in taxRatesDictionary)
                {
                    taxRate = kvp.Key;
                    var taxValue = kvp.Value;
                    taxRates += string.Format("{0}:{1};   ", taxRate.ToString(CultureInfo.InvariantCulture), taxValue.ToString(CultureInfo.InvariantCulture));
                }

                //order total (and applied discounts, gift cards, reward points)
                decimal? orderTotal = null;
                decimal orderDiscountAmount = decimal.Zero;
                List<AppliedGiftCard> appliedGiftCards = null;
                int redeemedRewardPoints = 0;
                decimal redeemedRewardPointsAmount = decimal.Zero;

                Discount orderAppliedDiscount = null;
                orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart,
                    out orderDiscountAmount, out orderAppliedDiscount, out appliedGiftCards,
                    out redeemedRewardPoints, out redeemedRewardPointsAmount);
                if (!orderTotal.HasValue)
                    throw new NopException("Order total couldn't be calculated");

                //discount history
                if (orderAppliedDiscount != null && !appliedDiscounts.ContainsDiscount(orderAppliedDiscount))
                    appliedDiscounts.Add(orderAppliedDiscount);

                //kcoOrderRequest.OrderTotal = orderTotal.Value; // TODO Is this needed?

                #endregion

                //save order in data storage
                //uncomment this line to support transactions
                //using (var scope = new System.Transactions.TransactionScope())
                {
                    #region Save order details

                    var shippingStatus = ShippingStatus.NotYetShipped;
                    if (!shoppingCartRequiresShipping)
                        shippingStatus = ShippingStatus.ShippingNotRequired;


                    var order = new Nop.Core.Domain.Orders.Order()
                    {
                        StoreId = kcoOrderRequest.StoreId,
                        OrderGuid = kcoOrderRequest.OrderGuid,
                        CustomerId = registeredCustomer.Id,
                        CustomerLanguageId = customerLanguage.Id,
                        CustomerTaxDisplayType = customerTaxDisplayType,
                        CustomerIp = kcoOrderRequest.IpAddress,
                        OrderSubtotalInclTax = orderSubTotalInclTax,
                        OrderSubtotalExclTax = orderSubTotalExclTax,
                        OrderSubTotalDiscountInclTax = orderSubTotalDiscountInclTax,
                        OrderSubTotalDiscountExclTax = orderSubTotalDiscountExclTax,
                        OrderShippingInclTax = orderShippingTotalInclTax.Value,
                        OrderShippingExclTax = orderShippingTotalExclTax.Value,
                        PaymentMethodAdditionalFeeInclTax = paymentAdditionalFeeInclTax,
                        PaymentMethodAdditionalFeeExclTax = paymentAdditionalFeeExclTax,
                        TaxRates = taxRates,
                        OrderTax = orderTaxTotal,
                        OrderTotal = orderTotal.Value,
                        RefundedAmount = decimal.Zero,
                        OrderDiscount = orderDiscountAmount,
                        CheckoutAttributeDescription = checkoutAttributeDescription,
                        CheckoutAttributesXml = checkoutAttributesXml,
                        CustomerCurrencyCode = customerCurrencyCode,
                        CurrencyRate = customerCurrencyRate,
                        AffiliateId = affiliateId,
                        OrderStatus = OrderStatus.Pending,
                        AllowStoringCreditCardNumber = false,
                        PaymentMethodSystemName = "Payments.KlarnaCheckout",
                        PaymentStatus = PaymentStatus.Paid,
                        PaidDateUtc = DateTime.UtcNow,
                        BillingAddress = billingAddress,
                        ShippingAddress = shippingAddress,
                        ShippingStatus = shippingStatus,
                        ShippingMethod = shippingMethodName,
                        ShippingRateComputationMethodSystemName = shippingRateComputationMethodSystemName,
                        VatNumber = vatNumber,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    _orderService.InsertOrder(order);

                    kcoOrderRequest.IsCompleted = true;
                    _kcoOrderRequestRepository.Update(kcoOrderRequest);

                    result.PlacedOrder = order;

                    //move shopping cart items to order product variants
                    foreach (var sc in cart)
                    {
                        //prices
                        taxRate = decimal.Zero;
                        decimal scUnitPrice = _priceCalculationService.GetUnitPrice(sc, true);
                        decimal scSubTotal = _priceCalculationService.GetSubTotal(sc, true);
                        decimal scUnitPriceInclTax = _taxService.GetProductPrice(sc.Product, scUnitPrice, true, customer, out taxRate);
                        decimal scUnitPriceExclTax = _taxService.GetProductPrice(sc.Product, scUnitPrice, false, customer, out taxRate);
                        decimal scSubTotalInclTax = _taxService.GetProductPrice(sc.Product, scSubTotal, true, customer, out taxRate);
                        decimal scSubTotalExclTax = _taxService.GetProductPrice(sc.Product, scSubTotal, false, customer, out taxRate);

                        //discounts
                        Discount scDiscount = null;
                        decimal discountAmount = _priceCalculationService.GetDiscountAmount(sc, out scDiscount);
                        decimal discountAmountInclTax = _taxService.GetProductPrice(sc.Product, discountAmount, true, customer, out taxRate);
                        decimal discountAmountExclTax = _taxService.GetProductPrice(sc.Product, discountAmount, false, customer, out taxRate);
                        if (scDiscount != null && !appliedDiscounts.ContainsDiscount(scDiscount))
                            appliedDiscounts.Add(scDiscount);

                        //attributes
                        string attributeDescription = _productAttributeFormatter.FormatAttributes(sc.Product, sc.AttributesXml, customer);

                        var itemWeight = _shippingService.GetShoppingCartItemWeight(sc);

                        //save order item
                        var orderItem = new OrderItem()
                        {
                            OrderItemGuid = Guid.NewGuid(),
                            Order = order,
                            ProductId = sc.ProductId,
                            UnitPriceInclTax = scUnitPriceInclTax,
                            UnitPriceExclTax = scUnitPriceExclTax,
                            PriceInclTax = scSubTotalInclTax,
                            PriceExclTax = scSubTotalExclTax,
                            AttributeDescription = attributeDescription,
                            AttributesXml = sc.AttributesXml,
                            Quantity = sc.Quantity,
                            DiscountAmountInclTax = discountAmountInclTax,
                            DiscountAmountExclTax = discountAmountExclTax,
                            DownloadCount = 0,
                            IsDownloadActivated = false,
                            LicenseDownloadId = 0,
                            ItemWeight = itemWeight,
                        };
                        order.OrderItems.Add(orderItem);
                        _orderService.UpdateOrder(order);

                        //gift cards
                        if (sc.Product.IsGiftCard)
                        {
                            string giftCardRecipientName, giftCardRecipientEmail,
                                giftCardSenderName, giftCardSenderEmail, giftCardMessage;
                            _productAttributeParser.GetGiftCardAttribute(sc.AttributesXml,
                                out giftCardRecipientName, out giftCardRecipientEmail,
                                out giftCardSenderName, out giftCardSenderEmail, out giftCardMessage);

                            for (int i = 0; i < sc.Quantity; i++)
                            {
                                var gc = new GiftCard()
                                {
                                    GiftCardType = sc.Product.GiftCardType,
                                    PurchasedWithOrderItem = orderItem,
                                    Amount = scUnitPriceExclTax,
                                    IsGiftCardActivated = false,
                                    GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                                    RecipientName = giftCardRecipientName,
                                    RecipientEmail = giftCardRecipientEmail,
                                    SenderName = giftCardSenderName,
                                    SenderEmail = giftCardSenderEmail,
                                    Message = giftCardMessage,
                                    IsRecipientNotified = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                };
                                _giftCardService.InsertGiftCard(gc);
                            }
                        }

                        //inventory
                        _productService.AdjustInventory(sc.Product, true, sc.Quantity, sc.AttributesXml);
                    }

                    //clear shopping cart
                    cart.ToList().ForEach(sci => _shoppingCartService.DeleteShoppingCartItem(sci, false));

                    //discount usage history
                    foreach (var discount in appliedDiscounts)
                    {
                        var duh = new DiscountUsageHistory()
                        {
                            Discount = discount,
                            Order = order,
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        _discountService.InsertDiscountUsageHistory(duh);
                    }

                    //gift card usage history
                    if (appliedGiftCards != null)
                        foreach (var agc in appliedGiftCards)
                        {
                            decimal amountUsed = agc.AmountCanBeUsed;
                            var gcuh = new GiftCardUsageHistory()
                            {
                                GiftCard = agc.GiftCard,
                                UsedWithOrder = order,
                                UsedValue = amountUsed,
                                CreatedOnUtc = DateTime.UtcNow
                            };
                            agc.GiftCard.GiftCardUsageHistory.Add(gcuh);
                            _giftCardService.UpdateGiftCard(agc.GiftCard);
                        }

                    //reward points history
                    if (redeemedRewardPointsAmount > decimal.Zero)
                    {
                        registeredCustomer.AddRewardPointsHistoryEntry(-redeemedRewardPoints,
                            string.Format(_localizationService.GetResource("RewardPoints.Message.RedeemedForOrder", order.CustomerLanguageId), order.Id),
                            order,
                            redeemedRewardPointsAmount);
                        _customerService.UpdateCustomer(registeredCustomer);
                    }

                    #endregion

                    #region Notifications & notes

                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = "Order placed",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);

                    //send email notifications
                    int orderPlacedStoreOwnerNotificationQueuedEmailId = _workflowMessageService.SendOrderPlacedStoreOwnerNotification(order, _localizationSettings.DefaultAdminLanguageId);
                    if (orderPlacedStoreOwnerNotificationQueuedEmailId > 0)
                    {
                        order.OrderNotes.Add(new OrderNote()
                        {
                            Note = string.Format("\"Order placed\" email (to store owner) has been queued. Queued email identifier: {0}.", orderPlacedStoreOwnerNotificationQueuedEmailId),
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                    }

                    int orderPlacedCustomerNotificationQueuedEmailId = _workflowMessageService.SendOrderPlacedCustomerNotification(order, order.CustomerLanguageId);
                    if (orderPlacedCustomerNotificationQueuedEmailId > 0)
                    {
                        order.OrderNotes.Add(new OrderNote()
                        {
                            Note = string.Format("\"Order placed\" email (to customer) has been queued. Queued email identifier: {0}.", orderPlacedCustomerNotificationQueuedEmailId),
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                    }

                    var vendors = new List<Vendor>();
                    foreach (var orderItem in order.OrderItems)
                    {
                        var vendorId = orderItem.Product.VendorId;
                        //find existing
                        var vendor = vendors.FirstOrDefault(v => v.Id == vendorId);
                        if (vendor == null)
                        {
                            //not found. load by Id
                            vendor = _vendorService.GetVendorById(vendorId);
                            if (vendor != null)
                            {
                                vendors.Add(vendor);
                            }
                        }
                    }
                    foreach (var vendor in vendors)
                    {
                        int orderPlacedVendorNotificationQueuedEmailId = _workflowMessageService.SendOrderPlacedVendorNotification(order, vendor, order.CustomerLanguageId);
                        if (orderPlacedVendorNotificationQueuedEmailId > 0)
                        {
                            order.OrderNotes.Add(new OrderNote()
                            {
                                Note = string.Format("\"Order placed\" email (to vendor) has been queued. Queued email identifier: {0}.", orderPlacedVendorNotificationQueuedEmailId),
                                DisplayToCustomer = false,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                            _orderService.UpdateOrder(order);
                        }
                    }

                    //check order status
                   _orderProcessingService.CheckOrderStatus(order);

                    //reset checkout data
                    _customerService.ResetCheckoutData(customer, kcoOrderRequest.StoreId, clearCouponCodes: true, clearCheckoutAttributes: true);

                    // compare order and klarna order
                    var klarnaOrderTotal = ((decimal)klarnaOrder.cart.total_price_including_tax) / 100;
                    if (order.OrderTotal != klarnaOrderTotal)
                    {
                        order.PaymentStatus = PaymentStatus.Pending;
                        order.OrderNotes.Add(new OrderNote()
                        {
                            Note = string.Format("Order and Klarna order totals miss match. Order total: '{0}'. Klarna Order Total {1}.",
                                order.OrderTotal, klarnaOrderTotal),
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                    }

                    if (order.OrderItems.Count != klarnaOrder.cart.items.Where(i => i.type == "physical").Count())
                    {
                        order.PaymentStatus = PaymentStatus.Pending;
                        order.OrderNotes.Add(new OrderNote()
                        {
                            Note = string.Format("Order and Klarna order number of items purchased miss match. Order: '{0}' items. Klarna Order {1} items.",
                                order.OrderItems.Count, klarnaOrder.cart.items.Count),
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                    }

                    // TODO: Do more comapresions with klarna order

                    _customerActivityService.InsertActivity(
                        "PublicStore.PlaceOrder",
                        _localizationService.GetResource("ActivityLog.PublicStore.PlaceOrder"),
                        order.Id);

                    //raise event       
                    _eventPublisher.PublishOrderPlaced(order);

                    //raise event         
                    if (order.PaymentStatus == PaymentStatus.Paid)
                    {
                        _eventPublisher.PublishOrderPaid(order);
                    }
                    #endregion

                    orderId = order.Id;
                }
            }
            catch (Exception exc)
            {
                _logger.Error(exc.Message, exc);
                result.AddError(exc.Message);
                SendOrderPlacedErrorMessage("Klarna Order Id: " + klarnaOrder.id + ". Message: " + exc.Message);
                orderId = -1;
            }

            #region Process errors

            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i + 1, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {
                //log it
                string logError = string.Format("Error while placing order. {0}", error);
                _logger.Error(logError);
            }

            #endregion
            
            return result;
        }

        #endregion

        #region Private Methods
        private void SendOrderPlacedErrorMessage(string errorMessage)
        {
            var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);

            var email = new QueuedEmail()
            {
                Priority = 1,
                From = emailAccount.Email,
                FromName = emailAccount.DisplayName,
                To = emailAccount.Email,
                ToName = emailAccount.DisplayName,
                CC = string.Empty,
                Bcc = string.Empty,
                Subject = "Klarna Checkout order not placed.",
                Body = errorMessage,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = emailAccount.Id
            };

            _queuedEmailService.InsertQueuedEmail(email);
        }

        private KcoOrderRequest GetKcoOrderRequest(Customer currentCustomer, string resourceUri)
        {
            return new KcoOrderRequest
            {
                CustomerId = currentCustomer.Id,
                OrderGuid = Guid.NewGuid(),
                KlarnaResourceUri = resourceUri,
                IsCompleted = false,
                IpAddress = _webHelper.GetCurrentIpAddress(),
                AffiliateId = currentCustomer.AffiliateId,
                StoreId = _storeContext.CurrentStore.Id,
                CreatedOnUtc = DateTime.UtcNow
            };
        }
        private Dictionary<string, object> GetCartData(Dictionary<string, object> merchant, Dictionary<string, object> klarnaCart, RenderForDevice renderFor)
        {
            var locale = _workContext.WorkingLanguage.LanguageCulture.ToLower();
            var purchaseCountry = "SE";

            switch (locale)
            {
                case "sv-se":
                    purchaseCountry = "SE";
                    break;
                case "nb-no":
                    purchaseCountry = "NO";
                    break;
                case "nn-no":
                    purchaseCountry = "No";
                    break;
                case "fi-fi":
                    purchaseCountry = "FI";
                    break;
            }
            
            var gui = new Dictionary<string, object>
            {
                { "layout", (renderFor == RenderForDevice.Mobile ? "mobile" : "desktop") },
                {"options", new List<string> {"disable_autofocus"}}
            };
            
            return new Dictionary<string, object>
            {
                { "purchase_country", purchaseCountry },
                { "purchase_currency", _workContext.WorkingCurrency.CurrencyCode },
                { "locale", locale },
                { "shipping_address", GetShippingAddress()},
                { "merchant", merchant},
                { "gui", gui},
                { "cart", klarnaCart }
            };
        }

        private Dictionary<string, object> GetMerchantItem(string eId)
        {
            var storeUrl = _storeContext.CurrentStore.Url;
            
            return new Dictionary<string, object>
            {
                {"id", eId},
                {"terms_uri", storeUrl + _kcoSettings.TermsUrl},
                {"checkout_uri", storeUrl + _kcoSettings.CheckoutUrl},
                {
                    "confirmation_uri", string.Format("{0}Plugins/PaymentsKlarnaCheckout/ThankYou?eId={1}&resourceUri={2}", storeUrl, _kcoSettings.EId, "{checkout.order.uri}")
                },
                {
                    "push_uri",
                    string.Format("{0}Plugins/PaymentsKlarnaCheckout/KcoPush?eId={1}&resourceUri={2}", storeUrl, _kcoSettings.EId, "{checkout.order.uri}")
                }
            };
        }

        private Dictionary<string, object> GetKlarnaCart()
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            var cartItems = GetCartItems(cart);
            cartItems.Add(GetShippingItem(cart));
            cartItems.AddRange(GetDiscountAndGiftCardItems(cart));
            return new Dictionary<string, object> { { "items", cartItems } };
        }

        private IEnumerable<Dictionary<string, object>> GetDiscountAndGiftCardItems(List<ShoppingCartItem> cart)
        {
            decimal orderDiscountAmount;
            List<AppliedGiftCard> appliedGiftCards;
            Discount orderAppliedDiscount;
            int redeemedRewardPoints;
            decimal redeemedRewardPointsAmount;
            _orderTotalCalculationService.GetShoppingCartTotal(cart,
                        out orderDiscountAmount, out orderAppliedDiscount, out appliedGiftCards,
                        out redeemedRewardPoints, out redeemedRewardPointsAmount);

            var items = new List<Dictionary<string, object>>();

            if (appliedGiftCards.Any())
            {
                items.Add(new Dictionary<string, object>
                {
                    {"type",  "discount"},
                    {"reference", "gift_card"},
                    {"name", _localizationService.GetResource("shoppingcart.giftcardcouponcode")},
                    {"quantity", 1},
                    {"unit_price", appliedGiftCards.Sum(x => x.AmountCanBeUsed).ToCents() * -1},
                    {"tax_rate", 0}
                });
            }

            if (orderDiscountAmount > 0)
            {
                items.Add(new Dictionary<string, object>
                {
                    {"type", "discount"},
                    {"reference", "order_discount"},
                    {"name", _localizationService.GetResource("order.totaldiscount")},
                    {"quantity", 1},
                    {"unit_price", orderDiscountAmount.ToCents()*-1},
                    {"tax_rate", 0}
                });
            }

            return items;
        }

        private Dictionary<string, object> GetShippingItem(List<ShoppingCartItem> cart)
        {
            return new Dictionary<string, object>
            {
                {"type", "shipping_fee"},
                {"reference", GetShippingName() },
                {"name", _localizationService.GetResource("order.shipping")},
                {"quantity", 1},
                {"unit_price", GetShippingTotal(cart).ToCents()},
                {"tax_rate", GetShippingTaxRate(cart).ToCents()}
            };
        }

        private List<Dictionary<string, object>> GetCartItems(List<ShoppingCartItem> cart)
        {
            return cart.Select(item =>
                new Dictionary<string, object>
                {
                    {"reference", item.Id.ToString()}, 
                    {"name", GetShoppingCartItemName(item)}, 
                    {"quantity", item.Quantity}, 
                    { "unit_price", GetUnitPrice(item).ToCents() },
                    { "discount_rate", GetDiscountRate(item).ToCents() },
                    { "tax_rate", GetTaxRate(item).ToCents() }
                }).ToList();
        }
        private Address SaveKlarnaAddress(Customer customer, KlarnaOrder klarnaOrder)
        {
            var a = klarnaOrder.billing_address;
            var address = new Address
            {
                FirstName = a.given_name,
                LastName = a.family_name,
                Email = a.email,
                Address1 = a.street_address,
                City = a.city,
                Company = a.care_of,
                ZipPostalCode = a.postal_code,
                PhoneNumber = a.phone,
                CreatedOnUtc = DateTime.UtcNow,
                Country = _countryService.GetCountryByTwoLetterIsoCode(a.country)
            };
            customer.Addresses.Add(address);
            _customerService.UpdateCustomer(customer);
            return address;
        }
        private Address AddressExists(Customer customer, ShippingAddress address)
        {
            var savedAddress = customer.Addresses.FirstOrDefault(a =>
                     String.Equals(a.FirstName, address.given_name, StringComparison.CurrentCultureIgnoreCase) &&
                     String.Equals(a.LastName, address.family_name, StringComparison.CurrentCultureIgnoreCase) &&
                     String.Equals(a.Address1, address.street_address, StringComparison.CurrentCultureIgnoreCase) &&
                     ArePostalEquals(address.postal_code, a.ZipPostalCode) &&
                     String.Equals(a.City, address.city, StringComparison.CurrentCultureIgnoreCase) &&
                     String.Equals(a.Email, address.email, StringComparison.CurrentCultureIgnoreCase) &&
                     ArePhoneEquals(a.PhoneNumber, address.phone)
                     );

            if (!string.IsNullOrEmpty(address.care_of) &&
                savedAddress != null &&
                !String.Equals(savedAddress.Company, address.care_of, StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return savedAddress;
        }

        private Address AddressExists(Customer customer, BillingAddress address)
        {
            var savedAddress = customer.Addresses.FirstOrDefault(a =>
                String.Equals(a.FirstName, address.given_name, StringComparison.CurrentCultureIgnoreCase) &&
                String.Equals(a.LastName, address.family_name, StringComparison.CurrentCultureIgnoreCase) &&
                String.Equals(a.Address1, address.street_address, StringComparison.CurrentCultureIgnoreCase) &&
                ArePostalEquals(address.postal_code, a.ZipPostalCode) &&
                String.Equals(a.City, address.city, StringComparison.CurrentCultureIgnoreCase) &&
                String.Equals(a.Email, address.email, StringComparison.CurrentCultureIgnoreCase) &&
                ArePhoneEquals(a.PhoneNumber, address.phone)
                );

            if (!string.IsNullOrEmpty(address.care_of) &&
                savedAddress != null &&
                !String.Equals(savedAddress.Company, address.care_of, StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return savedAddress;
        }

        private static bool ArePostalEquals(string postalCode, string zipPostalCode)
        {
            if (string.IsNullOrEmpty(postalCode) || string.IsNullOrEmpty(zipPostalCode))
                return false;

            return String.Equals(zipPostalCode.Replace(" ", ""), postalCode.Replace(" ", ""), StringComparison.CurrentCultureIgnoreCase);
        }

        private bool ArePhoneEquals(string phoneNumber, string phone)
        {
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(phone))
                return false;

            return String.Equals(phoneNumber.Replace(" ", "").Replace("-", ""), phone.Replace(" ", "").Replace("-", ""), StringComparison.CurrentCultureIgnoreCase);
        }
        private decimal GetOrderShipping(KlarnaOrder klarnaOrder, bool includeTax = true)
        {
            var shippingItem = klarnaOrder.cart.items.FirstOrDefault(i => i.type == "shipping_fee");

            if (shippingItem == null)
                return 0;

            var shippingTaxRate = (decimal)shippingItem.tax_rate / 100;
            var unitPrice = (decimal)shippingItem.unit_price / 100;

            return includeTax
                ? unitPrice
                : unitPrice / ((shippingTaxRate + 100) / 100);
        }
        private Dictionary<string, object> GetShippingAddress()
        {
            var address = _workContext.CurrentCustomer.Addresses.FirstOrDefault(a => a.Country.Name == "Sweden");
            var shippingAddress = new Dictionary<string, object>();

            if (address == null)
                return shippingAddress;

            if (!string.IsNullOrEmpty(address.FirstName))
                shippingAddress.Add("given_name", address.FirstName);

            if (!string.IsNullOrEmpty(address.LastName))
                shippingAddress.Add("family_name", address.LastName);

            if (!string.IsNullOrEmpty(address.Company))
                shippingAddress.Add("care_of", address.Company);

            if (!string.IsNullOrEmpty(address.Address1))
                shippingAddress.Add("street_address", address.Address1);

            if (!string.IsNullOrEmpty(address.ZipPostalCode))
                shippingAddress.Add("postal_code", address.ZipPostalCode);

            if (!string.IsNullOrEmpty(address.City))
                shippingAddress.Add("city", address.City);

            if (!string.IsNullOrEmpty(address.Country.Name))
                shippingAddress.Add("country", "SE");

            if (!string.IsNullOrEmpty(address.Email))
                shippingAddress.Add("email", address.Email);

            if (!string.IsNullOrEmpty(address.PhoneNumber))
                shippingAddress.Add("phone", address.PhoneNumber);

            return shippingAddress;
        }

        private decimal GetShippingTaxRate(IList<ShoppingCartItem> cart)
        {
            Decimal shippingTax;
            Discount shippingDiscount;
            _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, false, out shippingTax, out shippingDiscount);

            return shippingTax;
        }

        private string GetShippingName()
        {
            var shippingOption = _workContext.CurrentCustomer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, _storeContext.CurrentStore.Id);
            return shippingOption != null ? shippingOption.Name : string.Empty;
        }

        private decimal GetShippingTotal(IList<ShoppingCartItem> cart)
        {
            decimal shippingTax;
            Discount shippingDiscount;
            var shippingTotal = _orderTotalCalculationService.GetShoppingCartShippingTotal(cart, true, out shippingTax, out shippingDiscount);

            if (!shippingTotal.HasValue) return 0;

            var discount = GetShippingDiscount(shippingTotal.Value);
            shippingTotal = shippingTotal - discount;

            return shippingTotal.Value;
        }
        private decimal GetShippingDiscount(decimal shippingTotal)
        {
            Discount shippingDiscount;
            return GetShippingDiscount(_workContext.CurrentCustomer, shippingTotal, out shippingDiscount);
        }

        protected virtual decimal GetShippingDiscount(Customer customer, decimal shippingTotal, out Discount appliedDiscount)
        {
            appliedDiscount = null;
            decimal shippingDiscountAmount = decimal.Zero;
            if (_catalogSettings.IgnoreDiscounts)
                return shippingDiscountAmount;

            var allDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToShipping);
            var allowedDiscounts = new List<Discount>();
            if (allDiscounts != null)
                foreach (var discount in allDiscounts)
                    if (_discountService.IsDiscountValid(discount, customer) &&
                               discount.DiscountType == DiscountType.AssignedToShipping &&
                               !allowedDiscounts.ContainsDiscount(discount))
                        allowedDiscounts.Add(discount);

            appliedDiscount = allowedDiscounts.GetPreferredDiscount(shippingTotal);
            if (appliedDiscount != null)
            {
                shippingDiscountAmount = appliedDiscount.GetDiscountAmount(shippingTotal);
            }

            if (shippingDiscountAmount < decimal.Zero)
                shippingDiscountAmount = decimal.Zero;

            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                shippingDiscountAmount = Math.Round(shippingDiscountAmount, 2);

            return shippingDiscountAmount;
        }

        private decimal GetUnitPrice(ShoppingCartItem sci)
        {
            var value = _currencyService.ConvertFromPrimaryStoreCurrency(_priceCalculationService.GetUnitPrice(sci, !_taxSettings.PricesIncludeTax), _workContext.WorkingCurrency);
            return value;
        }

        private decimal GetDiscountRate(ShoppingCartItem sci)
        {
            var discountPrice = GetUnitPrice(sci);
            var price = GetUnitPrice(sci);

            return price.GetDiscountRate(discountPrice);
        }

        private decimal GetTaxRate(ShoppingCartItem sci)
        {
            decimal taxRate;
            _taxService.GetProductPrice(sci.Product, _priceCalculationService.GetUnitPrice(sci, true), out taxRate);

            return taxRate;
        }

        private static string GetShoppingCartItemName(ShoppingCartItem sci)
        {
            if (!String.IsNullOrEmpty(sci.Product.GetLocalized(x => x.Name)))
            {
                return string.Format("{0} ({1})", sci.Product.GetLocalized(x => x.Name),
                                     sci.Product.GetLocalized(x => x.Name));
            }

            return sci.Product.GetLocalized(x => x.Name);
        }

        //public Customer RegisterCustomer(Customer customer, KlarnaOrder klarnaOrder)
        //{
        //    //check whether registration is allowed
        //    if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
        //        throw new Exception("User registration disabled");

        //    var address = klarnaOrder.billing_address;
        //    var country = _countryService.GetCountryByTwoLetterIsoCode(address.country);

        //    bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
        //    var registrationRequest = new CustomerRegistrationRequest(customer, address.email,
        //        address.email, "changethis", _customerSettings.DefaultPasswordFormat, isApproved);
        //    var registrationResult = _customerRegistrationService.RegisterCustomer(registrationRequest);
        //    if (registrationResult.Success)
        //    {
        //        //form fields
        //        _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.FirstName, address.given_name);
        //        _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.LastName, address.family_name);
        //        if (_customerSettings.CompanyEnabled)
        //            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.Company, address.care_of);
        //        if (_customerSettings.StreetAddressEnabled)
        //            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.StreetAddress, address.street_address);
        //        if (_customerSettings.ZipPostalCodeEnabled)
        //            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.ZipPostalCode, address.postal_code);
        //        if (_customerSettings.CityEnabled)
        //            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.City, address.city);
        //        if (_customerSettings.CountryEnabled)
        //        {
        //            if (country != null)
        //                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.CountryId, country.Id);
        //        }
        //        if (_customerSettings.PhoneEnabled)
        //            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.Phone, address.phone);

        //        //insert default address (if possible)
        //        var defaultAddress = new Address()
        //        {
        //            FirstName = address.given_name,
        //            LastName = address.family_name,
        //            Email = address.email,
        //            Company = address.care_of,
        //            CountryId = country != null ? country.Id : 0,
        //            StateProvinceId = null,
        //            City = address.city,
        //            Address1 = address.street_address,
        //            ZipPostalCode = address.postal_code,
        //            PhoneNumber = address.phone,
        //            CreatedOnUtc = customer.CreatedOnUtc
        //        };
        //        if (this._addressService.IsAddressValid(defaultAddress))
        //        {
        //            //some validation
        //            if (defaultAddress.CountryId == 0)
        //                defaultAddress.CountryId = null;
        //            if (defaultAddress.StateProvinceId == 0)
        //                defaultAddress.StateProvinceId = null;
        //            //set default address
        //            customer.Addresses.Add(defaultAddress);
        //            customer.BillingAddress = defaultAddress;
        //            customer.ShippingAddress = defaultAddress;
        //            _customerService.UpdateCustomer(customer);
        //        }

        //        switch (_customerSettings.UserRegistrationType)
        //        {
        //            case UserRegistrationType.EmailValidation:
        //            {
        //                //email validation message
        //                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.AccountActivationToken, Guid.NewGuid().ToString());
        //                _workflowMessageService.SendCustomerEmailValidationMessage(customer, _workContext.WorkingLanguage.Id);
        //                break;
        //            }
        //            case UserRegistrationType.Standard:
        //            {
        //                //send customer welcome message
        //                _workflowMessageService.SendCustomerEmailValidationMessage(customer, _workContext.WorkingLanguage.Id);
        //                break;
        //            }
        //        }

        //        //notifications
        //        if (_customerSettings.NotifyNewCustomerRegistration)
        //            _workflowMessageService.SendCustomerRegisteredNotificationMessage(customer, _localizationSettings.DefaultAdminLanguageId);

        //        return customer;
        //    }

        //    throw new Exception(registrationResult.Errors.ToString());
        //}

        #endregion

        #region Private Properties
        private string EId
        {
            get { return _kcoSettings.EId.ToString(); }
        }

        private string SharedSecret
        {
            get { return _kcoSettings.SharedSecret; }
        }

        private string BaseUri
        {
            get
            {
                return _kcoSettings.TestMode
                    ? "https://checkout.testdrive.klarna.com/checkout/orders" // Test mode
                    : "https://checkout.klarna.com/checkout/orders";
            }
        }

        #endregion
    }
}
