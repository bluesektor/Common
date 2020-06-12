// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Data.Logging.Models
{
    [Table("SystemLog")]
    public class LogEntry
    {
        public LogEntry()
        {
            this.UUID = Guid.NewGuid().ToString("N");
            this.UUIDType = "LogEntry";
        }

        #region Properties

        public string InnerException
        {
            get; set;
        }

        public string Level
        {
            get; set;
        }

        public DateTime LogDate
        {
            get; set;
        }

        public string Source
        {
            get; set;
        }

        public string StackTrace
        {
            get; set;
        }

        public string Type
        {
            get; set;
        }

        public string User
        {
            get; set;
        }

        [Key]
        public string UUID { get; set; }

        [NotMapped]
        public string UUIDType { get; set; }

        #endregion Properties
    }
}