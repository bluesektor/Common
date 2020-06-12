// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Logging
{
    [Table("AuthenticationLog")]
    public class AuthenticationLog : Node, INode
    {
        public AuthenticationLog()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "AuthenticationLog";
        }

        /// <summary>
        /// Passed or failed the authentication scheme
        /// </summary>
        public bool Authenticated { get; set; }

        public DateTime? AuthenticationDate { get; set; }

        public string FailType { get; set; }
        public string IPAddress { get; set; }

        public string UserName { get; set; }

        /// <summary>
        /// Where is the attempt coming from..
        /// </summary>
        public string Vector { get; set; }
    }
}