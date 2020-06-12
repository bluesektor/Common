// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Equipment
{
    [Table("Filters")]
    public class Filter : Item, INode
    {
        public Filter()
        {
            UUIDType = "Filter";
            CategoryUUID = UUIDType;
        }
    }
}