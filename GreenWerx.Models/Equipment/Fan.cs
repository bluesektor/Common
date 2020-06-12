// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Equipment
{
    [Table("Fans")]
    public class Fan : Item, INode
    {
        public Fan()
        {
            UUIDType = "Fan";
            CategoryUUID = UUIDType;
        }

        public float BladeAngle { get; set; }

        /// <summary>
        /// Metal, plastic,
        /// </summary>
        public string BladeMaterial { get; set; }

        /// Centrifugal: forward, backward, radial
        public string BladeType { get; set; }

        public int CFM { get; set; }

        /// <summary>
        /// vent, exhaust
        /// </summary>
        public string FanRole { get; set; }

        /// <summary>
        /// Axial,Centrifugal   ,
        /// </summary>
        ///
        public string FlowDirection { get; set; }

        public int RPM { get; set; }
        public bool Variable { get; set; }
        public int Watts { get; set; }
        // public string Motor { get; set; }
    }
}