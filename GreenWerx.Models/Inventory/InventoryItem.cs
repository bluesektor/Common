﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Inventory
{
    [Table("Inventory")]
    public class InventoryItem : Item, INode
    {
        public InventoryItem() : base()
        {
            this.UUIDType = "InventoryItem";
            this.DetailView = false;
        }

        public string DateType { get; set; }

        [NotMapped]
        public bool DetailView { get; set; }

        public DateTime ItemDate { get; set; }
        public string Link { get; set; }
        public string LinkProperties { get; set; }

        /// <summary>
        /// store,vehicle, room etc..
        /// </summary>
        public string LocationType { get; set; }

        public string LocationUUID { get; set; }

        //display in web store.
        public bool Published { get; set; }

        public float Quantity { get; set; }

        public string ReferenceType { get; set; }   //  product, item, user, ballast, plant

        public string ReferenceUUID { get; set; } //id of the item in inventory if we have to break it down to individual items.
                                                  //expires, end of cycle ....

        public string VendorUUID { get; set; }
        //TODO inventory log. show when items added, removed, etc.
        ////public DateTime DateAdded { get; set; }
        ////public string AddedByUUID { get; set; }
    }
}