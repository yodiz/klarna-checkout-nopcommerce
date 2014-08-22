(function () {
    'use strict';

    var controllerId = 'checkoutCtrl';

    angular.module('klarnaCheckout').controller(controllerId,
        ['$sce', 'kcoRepo', 'shippingMethodRepo', checkoutCtrl]);

    function checkoutCtrl($sce, kcoRepo, shippingMethodRepo) {
        var vm = this;
        var isCheckoutLoaded = false;

        vm.changeShippingMethod = changeShippingMethod;
        vm.checkoutSnippet = '';
        vm.selectedShippingOption = null;
        vm.shippingMethods = [];

        activate();

        function activate() {
            getShippingMethods();
        }

        function changeShippingMethod(shippingMethod) {
            if (isCheckoutLoaded) { klarnaSuspend(); }
            return shippingMethodRepo.save(shippingMethod).then(function () {
                vm.selectedShippingOption = shippingMethod.name;
                getCheckoutSnippet();
            }), function (error) {
                console.log(error);
                klarnaResume();
            };
        }

        function getCheckoutSnippet() {
            return kcoRepo.get().then(function (data) {
                var snippet = data.gui.snippet;
                snippet = snippet.replace(/\\/g, '');
                if (isCheckoutLoaded) { klarnaResume(); }
                isCheckoutLoaded = true;
                updateCartTotals(data);
                return vm.checkoutSnippet = $sce.trustAsHtml(snippet);
            }, function () {
                var msg = "<div class=\"alert alert-error\"><strong>Oops!</strong> Kunde inte ladda kassan.</div>";
                return vm.checkoutSnippet = $sce.trustAsHtml(msg);
            });
        }

        function getShippingMethods() {
            return shippingMethodRepo.get().then(function (data) {
                vm.shippingMethods = data;
                setSelectedShippingOption();
                return vm.shippingMethods;
            }, function (error) {
                console.log(error);
            });

            function setSelectedShippingOption() {
                for (var i = 0; i < vm.shippingMethods.length; i++) {
                    var shippingMethod = vm.shippingMethods[i];
                    vm.shippingMethods[i].description = $sce.trustAsHtml(shippingMethod.description);
                    if (shippingMethod.selected) {
                        if (!isCheckoutLoaded) {
                            changeShippingMethod(shippingMethod);
                        }
                        vm.selectedShippingOption = shippingMethod.name;
                        return;
                    }
                }
            }
        }

        function klarnaSuspend() {
            window._klarnaCheckout(function (api) { api.suspend(); });
        }

        function klarnaResume() {
            window._klarnaCheckout(function (api) { api.resume(); });
        }

        function updateCartTotals(klarnaOrder) {
            // Cart total
            var cartTotal = klarnaOrder.cart.total_price_including_tax;
            cartTotal = currencyFormat(cartTotal, klarnaOrder.purchase_currency);
            $('table.cart-total .order-total').html(cartTotal);
            $('table.cart-total .order-total strong').html(cartTotal);

            // Shipping total
            var shippingTotal = 0;
            for (var i = 0; i < klarnaOrder.cart.items.length; i++) {
                var item = klarnaOrder.cart.items[i];
                if (item.type === "shipping_fee") {
                    shippingTotal = item.total_price_including_tax;
                }
            }
            shippingTotal = currencyFormat(shippingTotal, klarnaOrder.purchase_currency);
            $('table.cart-total .shipping-total').html(shippingTotal);

            // Discounts
            var discountTotal = 0;
            for (var i = 0; i < klarnaOrder.cart.items.length; i++) {
                var item = klarnaOrder.cart.items[i];
                if (item.reference === "order_discount") {
                    discountTotal = item.total_price_including_tax;
                }
            }
            discountTotal = currencyFormat(discountTotal, klarnaOrder.purchase_currency);
            $('table.cart-total .discount-total').html(discountTotal);

            // Gift cards
            var giftCardTotal = 0;
            for (var i = 0; i < klarnaOrder.cart.items.length; i++) {
                var item = klarnaOrder.cart.items[i];
                if (item.reference === "giftcard") {
                    giftCardTotal = item.total_price_including_tax;
                }
            }
            giftCardTotal = currencyFormat(giftCardTotal, klarnaOrder.purchase_currency);
            $('table.cart-total .gift-card-total').html(giftCardTotal);
        }

        function currencyFormat(value, currency) {
            // Not in cents
            value = (value / 100).toFixed(2);

            switch (currency) {
                case 'sek':
                    return value + ' kr';
            }

            return value;

        }
    }
})();