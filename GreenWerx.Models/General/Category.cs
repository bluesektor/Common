// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.General
{
    [Table("Categories")]
    public class Category : Node, INode
    {
        public Category()
        {
            this.UUIDType = "Category";
            this.UsesStrains = false;
            this.CategoryType = string.Empty;
        }

        public string CategoryType { get; set; }

        public bool UsesStrains { get; set; }
    }
}