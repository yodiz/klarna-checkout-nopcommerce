(function () {
    'use strict';

    var serviceId = 'shippingMethodRepo';

    angular.module('klarnaCheckout').factory(serviceId,
        ['$http', '$q', shippingMethodRepo]);

    function shippingMethodRepo($http, $q) {
        return {
            get: function() {
                var deferred = $q.defer();
                
                $http({ method: 'GET', url: '/Plugins/PaymentsKlarnaCheckout/ShippingMethods' }).
					success(function (data, status, headers, config) {
					    deferred.resolve(data);
					}).
					error(function (data, status, headers, config) {
					    deferred.reject(status);
					});

                return deferred.promise;
            },
            save: function(shippingMethod) {
                var deferred = $q.defer();
                
                $http({ method: 'POST', url: '/Plugins/PaymentsKlarnaCheckout/ChangeShippingMethod', data: { 'shippingMethod': shippingMethod } }).
					success(function (data, status, headers, config) {
					    deferred.resolve(data);
					}).
					error(function (data, status, headers, config) {
					    deferred.reject(status);
					});

                return deferred.promise;
                
            }
        };
    }
})();