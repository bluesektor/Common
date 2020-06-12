// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.General
{
    [Table("Attributes")]
    public class Attribute : Node, INode
    {
        public Attribute()
        {
            this.UUIDType = "Attribute";
        }

        /// <summary>
        /// The type being referenced ( Plant, Fan, Vendor, Product, InventoryItem etc..);
        /// </summary>
        public string ReferenceType { get; set; }

        //UUID of the item being referenced
        //
        public string ReferenceUUID { get; set; }

        public string Value { get; set; }

        public string ValueType { get; set; }
    }
}