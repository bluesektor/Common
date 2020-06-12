// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.General
{
    [Table("StatusMessages")]
    public class StatusMessage : Node, INode
    {
        public StatusMessage()
        {
            this.UUID = Guid.NewGuid().ToString("N");
            this.Status = string.Empty;
            this.AccountUUID = string.Empty;
            this.CreatedBy = string.Empty;
            this.DateCreated = DateTime.MinValue;
            this.UUIDType = "StatusMessage";
            this.Total = 0;
            this.CurrentIndex = 0;
        }

        public int CurrentIndex { get; set; }

        ///i.e. class for which the status represents.. SymptomLog.Status
        [StringLength(32)]
        public string StatusType { get; set; }

        public int Total { get; set; }
    }
}