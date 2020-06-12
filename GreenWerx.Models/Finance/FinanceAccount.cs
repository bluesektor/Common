// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Finance
{
    [Table("FinanceAccounts")]
    public class FinanceAccount : Node, INode
    {
        public FinanceAccount()
        {
            this.UUIDType = "FinanceAccount";
        }

        public string AccountNumber { get; set; }

        public decimal Balance { get; set; }

        public string ClientCode { get; set; }

        [NotMapped]
        public string CurrencyName { get; set; }

        public string CurrencyUUID { get; set; }
        public string Email { get; set; }

        public string LocationType { get; set; }
        public string Password { get; set; }

        public string ServiceAddress { get; set; }

        public string SourceClass { get; set; }

        public int SourceUUID { get; set; }
    }
}