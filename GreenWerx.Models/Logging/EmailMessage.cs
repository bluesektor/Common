// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Logging
{
    [Table("EmailLog")]
    public class EmailMessage : Node, INode
    {
        public EmailMessage()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "EmailMessage";
        }

        public string Body { get; set; }
        public DateTime? DateOpened { get; set; }

        //This is the date when the email successfully sent to recipient.
        //Not the date it was submitted.
        //
        public DateTime? DateSent { get; set; }

        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string IpAddress { get; set; }
        public string NameFrom { get; set; }
        public string NameTo { get; set; }
        public string SendFrom { get; set; }
        public string SendTo { get; set; }
        public string Subject { get; set; }
        public string Type { get; set; }
    }
}