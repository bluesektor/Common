// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Store
{
    [Table("ProductBarCodes")]
    public class ProductBarCode
    {
        public ProductBarCode()
        {
            UUID = Guid.NewGuid().ToString("N");

            UUIDType = "ProductBarCode";
        }

        public string BarCode { get; set; }

        public string DateCreated { get; set; }

        public string ImageUrl { get; set; }

        [StringLength(32)]
        public string ProductType { get; set; }

        [StringLength(32)]
        public string ProductUUID { get; set; }

        /// <summary>
        /// Keep track of how many ppl query the product.
        /// This way we can trim dead barcodes and their images.
        /// </summary>
        public uint QueryCount { get; set; }

        public int RoleWeight { get; set; }

        //public int Id { get; set; }
        [Key]
        [StringLength(32)]
        public string UUID { get; set; }

        [NotMapped]
        public string UUIDType { get; set; }
    }
}