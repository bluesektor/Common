// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Equipment
{
    [Table("Ballasts")]
    public class Ballast : Item, INode
    {
        public Ballast()
        {
            UUIDType = "Ballast";
            CategoryUUID = UUIDType;
        }

        public float HoursUsed { get; set; }

        public int Watts { get; set; }

        #region Fluorescent lamp ballasts

        //Instant start
        //Rapid start
        //Dimmable ballast
        //Programmed start
        //Hybrid

        #endregion Fluorescent lamp ballasts

        //ANSI Ballast factor
    }
}