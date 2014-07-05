(function () {
    'use strict';

    var serviceId = 'kcoRepo';

    angular.module('klarnaCheckout').factory(serviceId,
        ['$http', '$q', kcoRepo]);

    function kcoRepo($http, $q) {
        return {
            get: function () {
                var deferred = $q.defer();

                $http({ method: 'GET', url: '/Plugins/PaymentsKlarnaCheckout/CheckoutSnippet' }).
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