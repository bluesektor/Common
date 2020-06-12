// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Equipment
{
    [Table("Bulbs")]
    public class Bulb : Item, INode
    {
        public Bulb()
        {
            UUIDType = "Bulb";
            CategoryUUID = UUIDType;
        }

        //flourescent
        //     Mercury-vapor lamps
        // Metal-halide(MH) lamps
        //Ceramic MH lamps
        // Sodium-vapor lamps
        //Low-pressure sodium vapor lamps are extremely efficie
        // Xenon short-arc lamps

        public float HoursUsed { get; set; }
        public int Lumens { get; set; }

        //red blue green?
        public int Spectrum { get; set; }

        public int Watts { get; set; }
    }
}