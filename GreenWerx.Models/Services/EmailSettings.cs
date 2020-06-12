// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
namespace GreenWerx.Models.Services
{
    public class EmailSettings
    {
        public string EmailDomain { get; set; }

        /// <summary>
        /// Should be the AppKey
        /// </summary>
        public string EncryptionKey { get; set; }

        public string FromUserUUID { get; set; }
        public string HostPassword { get; set; }
        public string HostUser { get; set; }
        public string MailHost { get; set; }
        public int MailPort { get; set; }
        public int RoleWeight { get; set; }
        public string SiteDomain { get; set; }
        public string SiteEmail { get; set; }
        public string ToUserUUID { get; set; }
        public bool UseSSL { get; set; }
    }
}