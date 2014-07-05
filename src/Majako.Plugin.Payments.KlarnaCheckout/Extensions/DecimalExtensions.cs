using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majako.Plugin.Payments.KlarnaCheckout.Extensions
{
    public static class DecimalExtensions
    {
        public static int ToCents(this decimal value)
        {
           return  (int)Math.Round(value*100, 0);
        }

        public static decimal GetTaxRate(this decimal inclTax, decimal exclTax, bool usePercentage = true)
        {
            var factor = usePercentage ? 100 : 1;

            if (inclTax > exclTax && exclTax > 0)
                return ((inclTax / exclTax) - 1) * factor;

            return 0;
        }

        public static decimal GetDiscountRate(this decimal regularPrice, decimal discountedPrice)
        {
            if (regularPrice > discountedPrice && regularPrice > 0)
                return (1 - (discountedPrice / regularPrice)) * 100;

            return 0;
        }
    }
}
