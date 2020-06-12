// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

//TO create a coupon for 10% off..
//code = <xxxx>
//RedemptionType = "coupon";
//Operand = 10;
//Operator = "%;
//
//todo use this to create meta rules for totals of all rules i.e. max number of coupons, max total of discounts
namespace GreenWerx.Models.Finance
{
    [Table("PriceRules")]
    public class PriceRule : Node, INode
    {
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
    }
}