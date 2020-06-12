// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using GreenWerx.Models.Finance;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Membership;

namespace GreenWerx.Models.Store
{
    public class CartView : ShoppingCart
    {
        public CartView()
        {
            this.CartItems = new List<dynamic>();
            this.BillingAddress = new Location();
            this.ShippingAddress = new GreenWerx.Models.Geo.Location();
            this.Customer = new GreenWerx.Models.Membership.User();
            this.Customer.UUID = "";
            // this.PriceRule = new PriceRule();
            this.PriceRules = new List<PriceRuleLog>();
        }

        public Location BillingAddress { get; set; }
        public List<dynamic> CartItems { get; set; }
        public User Customer { get; set; }
        public bool IsPaymentSandbox { get; set; }
        public string PaidTo { get; set; }
        public string PaidType { get; set; }
        public string PaymentGateway { get; set; }
        public List<PriceRuleLog> PriceRules { get; set; }
        public Location ShippingAddress { get; set; }
    }
}