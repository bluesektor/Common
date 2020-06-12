// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace GreenWerx.Models.Logging
{
    [Table("AccessLog")]
    public class AccessLog : Node, INode
    {
        public AccessLog()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "AccessLog";
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

    [Table("RequestLogs")]
    public class RequestLog
    {
        public RequestLog()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "RequestLog";
            ExecutionTime = -1;
        }

        public string AbsolutePath { get; set; }

        public string AccountUUID { get; set; }

        public DateTime DateCreated { get; set; }

        public long ExecutionTime { get; set; }

        public string IPAddress { get; set; }

        public string Method { get; set; }

        public string Referrer { get; set; }

        [NotMapped]
        public bool RequestComplete { get; set; }

        public string RequestLocalPath { get; set; }

        public string RequestURL { get; set; }

        public string Response { get; set; }

        //ctx.Request.Url.ToString();
        [NotMapped]
        public Stopwatch Timer { get; set; }

        public string UserUUID { get; set; }

        [Key]
        public string UUID { get; set; }

        public string UUIDType { get; set; }
    }
}