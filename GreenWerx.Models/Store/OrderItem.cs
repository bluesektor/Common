// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Store
{
    [Table("OrderItems")]
    public class OrderItem : Node, INode
    {
        public OrderItem()
        {
            this.UUIDType = "OrderItem";
        }

        public DateTime AccessExpires { get; set; }

        public bool AccessGranted { get; set; }

        //who gets credit for the sale
        public string AffiliateUUID { get; set; }

        public bool IsVirtual { get; set; }
        public string ItemType { get; set; }
        public string ItemUUID { get; set; }
        public string OrderUUID { get; set; }

        public string PaymentStatusUUID { get; set; }

        public decimal Price { get; set; }
        public string ProductType { get; set; }
        public string ProductUUID { get; set; }
        public float Quantity { get; set; }

        public string ShippingMethodUUID { get; set; }

        public string SKU { get; set; }

        [NotMapped]
        public decimal TotalPrice { get { return Price * (decimal)Quantity; } }

        //how many units were in the product
        public float UnitsInProduct { get; set; }

        //e.g. how many downloads, trades, etc remaining.
        public float UnitsRemaining { get; set; }

        //Download,access,trade, jars etc..
        public string UnitType { get; set; }

        //User purchasing the item.
        //this is used to see scan the orders table and
        //make sure a payment actually settles
        //if after x time scan the orders table check if user id is
        //logged in. if not cancel the order and return the stock.
        //
        public string UserUUID { get; set; }
    }
}