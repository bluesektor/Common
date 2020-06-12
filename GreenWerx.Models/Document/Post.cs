using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.Models.Document
{
    [Table("Posts")]
    public class Post : Node, INode
    {
        public Post()
        {
            this.UUIDType = "Post";
        }
            
        
        [NotMapped]
        public List<TMG.Attribute> Attributes { get; set; }

        public bool AllowComments { get; set; }

        public string Author { get; set; }

        // status: draft, published, pending review, kickback (to author)
        public string Body { get; set; }

        public string Category { get; set; }
        public string KeyWords { get; set; }
        public DateTime? PublishDate { get; set; }
        public bool Sticky { get; set; }
    }
}