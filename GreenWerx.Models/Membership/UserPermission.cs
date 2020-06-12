using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Membership
{
    [Table("UserPermissions")]
    public class UserPermission : Node, INode
    {
        public UserPermission()
        {
            UUIDType = "UserPermission";
        }

        [StringLength(32)]
        public string AccountUUID { get; set; }

        public string Action { get; set; }

        public bool Active { get; set; }

        /// <summary>
        /// web, forms, mobile
        /// </summary>
        public string AppType { get; set; }

        public bool Deleted { get; set; }

        public DateTime? EndDate { get; set; }

        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
        public int ParentId { get; set; }

        [StringLength(32)]
        public string PermissionUUID { get; set; }

        //Set this to ignore EndDate
        public bool Persists { get; set; }

        [StringLength(32)]
        public string RoleUUID { get; set; }

        public DateTime? StartDate { get; set; }

        /// <summary>
        /// class for the action
        /// </summary>
        public string Type { get; set; }

        [StringLength(32)]
        public string UserUUID { get; set; }

        /// <summary>
        /// Defines the type of SettingUUID used (guid, hash.<algo> )..
        /// </summary>
        [StringLength(32)]
        public string UUID { get; set; }

        /// <summary>
        /// Defines the type of SettingUUID used (guid, hash.<algo> )..
        /// </summary>
        [StringLength(32)]
        public string UUIDType { get; set; }

        [StringLength(32)]
        public string UUParentID { get; set; }

        [StringLength(32)]
        public string UUParentIDType { get; set; }
    }
}