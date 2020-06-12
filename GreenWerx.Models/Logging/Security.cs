﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Logging
{
    [Table("SecurityLogs")]
    public class SecurityLog : Node, INode
    {
        public SecurityLog()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "SecurityLog";
        }

        /// <summary>
        /// did the action succeed? were they authorized to make the
        /// change and it went through.
        /// </summary>
        public bool Authorized { get; set; }

        public DateTime? ChangeDate { get; set; }

        public string Ip { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Reporting agent, controller etc
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// What was executed UpdatePassword, etc
        /// </summary>
        public string ServiceAction { get; set; }

        [StringLength(32)]
        public string UserUUID { get; set; }
    }
}