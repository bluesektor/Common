using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.General
{
    [Table("Favorites")]
    public class Favorite : Node, INode //Event
    {
        public Favorite()
        {
            UUIDType = "Favorite";
        }

        [NotMapped]
        public object Item { get; set; }

        public string ItemType { get; set; }
        public string ItemUUID { get; set; }
        public string UserUUID { get; set; }
    }
}