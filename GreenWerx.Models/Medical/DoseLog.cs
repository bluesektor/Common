// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Medical
{
    [Table("DoseLogs")]
    public class DoseLog : Node, INode
    {
        public DoseLog()
        {
            UUIDType = "Dose";
            Symptoms = new List<SymptomLog>();
        }

        public DateTime DoseDateTime { get; set; }

        public string Notes { get; set; }

        [StringLength(32)]
        public string ProductUUID { get; set; }

        public float Quantity { get; set; }

        [NotMapped]
        public List<SymptomLog> Symptoms { get; set; }

        public string UnitOfMeasure { get; set; }

        //aka patient id
        [StringLength(32)]
        public string UserUUID { get; set; }
    }
}