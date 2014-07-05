using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majako.Plugin.Payments.KlarnaCheckout.Models
{
    public class Item
    {
        public string reference { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }
        public int unit_price { get; set; }
        public int tax_rate { get; set; }
        public int discount_rate { get; set; }
        public string type { get; set; }
        public int total_price_including_tax { get; set; }
        public int total_price_excluding_tax { get; set; }
        public int total_tax_amount { get; set; }
    }

    public class Cart
    {
        public int total_price_excluding_tax { get; set; }
        public int total_tax_amount { get; set; }
        public int total_price_including_tax { get; set; }
        public List<Item> items { get; set; }
    }

    public class Customer
    {
        public string type { get; set; }
    }

    public class ShippingAddress
    {
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string care_of { get; set; }
        public string street_address { get; set; }
        public string postal_code { get; set; }
        public string city { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string country { get; set; }
    }

    public class BillingAddress
    {
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string care_of { get; set; }
        public string street_address { get; set; }
        public string postal_code { get; set; }
        public string city { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string country { get; set; }
    }

    public class Gui
    {
        public string layout { get; set; }
        public string snippet { get; set; }
    }

    public class Options
    {
        public bool allow_separate_shipping_address { get; set; }
    }

    public class Merchant
    {
        public string id { get; set; }
        public string terms_uri { get; set; }
        public string checkout_uri { get; set; }
        public string confirmation_uri { get; set; }
        public string push_uri { get; set; }
    }

    public class KlarnaOrder
    {
        public string id { get; set; }
        public string purchase_country { get; set; }
        public string purchase_currency { get; set; }
        public string locale { get; set; }
        public string status { get; set; }
        public string started_at { get; set; }
        public string last_modified_at { get; set; }
        public Cart cart { get; set; }
        public Customer customer { get; set; }
        public ShippingAddress shipping_address { get; set; }
        public BillingAddress billing_address { get; set; }
        public Gui gui { get; set; }
        public Options options { get; set; }
        public Merchant merchant { get; set; }
    }
}
