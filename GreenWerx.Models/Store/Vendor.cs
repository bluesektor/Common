// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Store
{
    [Table("Vendors")]
    public class Vendor : Node, INode
    {
        public Vendor()
        {
            this.UUIDType = "Vendor";
        }

        public bool Breeder { get; set; }
        public string BreederType { get; set; }
        public bool Dispensary { get; set; }
        public bool Farmer { get; set; }
    }
}