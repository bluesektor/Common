// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Equipment
{
    [Table("Vehicles")]
    public class Vehicle : Item, INode
    {
        public Vehicle()
        {
            UUIDType = "Vehicle";
            CategoryUUID = UUIDType;
        }

        public string Color { get; set; }

        public decimal FuelLevel { get; set; }
        public string FuelType { get; set; }

        //Gross Vehicle Weight
        public decimal GVW { get; set; }

        public string Make { get; set; }

        public decimal Mileage { get; set; }
        public string Model { get; set; }

        public string Plate { get; set; }

        public DateTime? ServiceDate { get; set; }
        public string Vin { get; set; }

        public int Year { get; set; }
    }
}