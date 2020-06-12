// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Plant
{
    [Table("Plants")]
    public class Plant : Item
    {
        public Plant()
        {
            UUIDType = "Plant";
        }

        /// <summary>
        /// Male, female, hermaphrodite( may want to flag these, if I recall correctly they
        /// produce all female seeds?).
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        ///  Clone,  Seed.. pollen? root ball (canada has to track the root balls)
        /// </summary>
        [StringLength(32)]
        public string SourceType { get; set; }

        [StringLength(32)]
        public string SourceUUID { get; set; }

        /// <summary>
        ///  seed/cutting, veg, flower,pollen?
        /// </summary>
        public string Stage { get; set; }

        [StringLength(32)]
        public string StrainUUID { get; set; }

        /// <summary>
        /// aka vendor
        /// Where did this come from?
        /// </summary>
        [StringLength(32)]
        public string VendorUUID { get; set; }
    }
}