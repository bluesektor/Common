// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;

namespace GreenWerx.Models.Finance
{
    public class Fee : Node, INode
    {
        public Fee()
        {
            this.UUIDType = "Fee";
        }

        public decimal Ammount { get; set; }

        public string Category { get; set; }

        public DateTime DateAdded { get; set; }

        public string ItemUUID { get; set; }

        public string TransactionUUID { get; set; }
        public string UserUUID { get; set; }
    }
}