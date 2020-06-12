// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Store
{
    [Table("Products")]
    public class Product : Item
    {
        public Product()
        {
            UUIDType = "Product";
        }

        public string Link { get; set; }

        public string LinkProperties { get; set; }

        [StringLength(32)]
        public string StrainUUID { get; set; }
    }
}