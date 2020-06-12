// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Finance
{
    [Table("PriceRuleLogs")]
    public class PriceRuleLog : Node, INode
    {
        //for logging what steps the calculator took..
        public string CalcDetail { get; set; }

        public string PriceRuleUUID { get; set; }
        public string TrackingId { get; set; }

        //ShoppingCart, Order..
        public string TrackingType { get; set; }

        #region PriceRule

        public string Code { get; set; }

        public string DateUsed { get; set; }

        public DateTime Expires { get; set; }

        //use this to restrict the rules to specific location
        public string LocationUUID { get; set; }

        public bool Mandatory { get; set; }

        //for maximum discount
        public decimal Maximum { get; set; }

        public int MaxUseCount { get; set; }

        //set this for type like minimum delivery/shipping charge
        public decimal Minimum { get; set; }

        //This is the "discount" multiplier
        //
        public decimal Operand { get; set; }

        public string Operator { get; set; }

        //student, military, coupon, promo..
        //shipping, delivery
        public string ReferenceType { get; set; }

        //Use this for the record. Shipping method, coupon..
        //
        public string ReferenceUUID { get; set; }

        public decimal Result { get; set; }

        #endregion PriceRule
    }
}