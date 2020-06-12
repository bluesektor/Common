// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Helpers;

namespace GreenWerx.Models.Finance
{
    [Table("Currency")]
    public class Currency : Node, INode
    {
        public Currency()
        {
            this.UUIDType = "Currency";
        }

        public string AssetClass { get; set; }

        public string Code { get; set; }
        public string Country { get; set; }
        public string Symbol { get; set; }

        [JsonConverter(typeof(BoolConverter))]
        public bool Test { get; set; }
    }
}