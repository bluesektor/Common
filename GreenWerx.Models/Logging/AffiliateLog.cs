using System;

namespace GreenWerx.Models.Logging
{
    public class AffiliateLog : Node, INode
    {
        // Link (clicked), Profile View
        public string AccessType { get; set; }

        public decimal AmmountReceived { get; set; }

        public decimal AmountSent { get; set; }

        public string ClientIp { get; set; }

        public string ClientUserUUID { get; set; }

        public decimal CommissionAmount { get; set; }

        //percent,  see priceRule
        public string CommissionOperator { get; set; }

        public string CommissionType { get; set; }

        public string Link { get; set; }

        // Name =target userName, link name etc. being accesed
        public string NameType { get; set; }

        public DateTime? PaymentReceived { get; set; }
        public DateTime? PaymentSent { get; set; }
        public string PromoCode { get; set; }
        public string Referrer { get; set; }
        public string ReferringUUID { get; set; }
        public string ReferringUUIDType { get; set; }
        public string TemplateId { get; set; }

        // inbound, outbound
        public string Direction { get; set; }
    }
}