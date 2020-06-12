// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Membership
{
    [Table("RolesBlocked")]
    public class BlockedRole : Node, INode
    {
        public BlockedRole()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "BlockedRole";
        }

        public string Action { get; set; }

        /// <summary>
        /// web, forms, mobile
        /// </summary>
        public string AppType { get; set; }

        public DateTime? EndDate { get; set; }

        //Set this to ignore EndDate
        public bool Persists { get; set; }

        public string ReferenceType { get; set; }

        [StringLength(32)]
        public string ReferenceUUID { get; set; }

        [StringLength(32)]
        public string RoleUUID { get; set; }

        public DateTime? StartDate { get; set; }

        public string TargetType { get; set; }

        //refers to the target of the action.. blocking for example.
        //ReferenceUUID  Action     TargetUUID
        // 1234          Block       5678
        public string TargetUUID { get; set; }

        /// <summary>
        /// class for the action
        /// </summary>
        public string Type { get; set; }
    }

    [Table("RolesBlockedUsers")]
    public class BlockedUser : Node, INode
    {
        public BlockedUser()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "BlockedUser";
        }

        public string Action { get; set; }

        /// <summary>
        /// web, forms, mobile
        /// </summary>
        public string AppType { get; set; }

        public DateTime? EndDate { get; set; }

        //Set this to ignore EndDate
        public bool Persists { get; set; }

        public string ReferenceType { get; set; }

        [StringLength(32)]
        public string ReferenceUUID { get; set; }

        [StringLength(32)]
        public string RoleUUID { get; set; }

        public DateTime? StartDate { get; set; }

        public string TargetType { get; set; }

        //refers to the target of the action.. blocking for example.
        //ReferenceUUID  Action     TargetUUID
        // 1234          Block       5678
        public string TargetUUID { get; set; }

        /// <summary>
        /// class for the action
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// this has been changed to be more flexibile so
    /// we can have other types in the role.
    /// </summary>
    [Table("UsersInRoles")]
    public class UserRole : Node, INode
    {
        public UserRole()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "UserRole";
        }

        public string Action { get; set; }

        /// <summary>
        /// web, forms, mobile
        /// </summary>
        public string AppType { get; set; }

        public DateTime? EndDate { get; set; }

        //Set this to ignore EndDate
        public bool Persists { get; set; }

        public string ReferenceType { get; set; }

        [StringLength(32)]
        public string ReferenceUUID { get; set; }

        [StringLength(32)]
        public string RoleUUID { get; set; }

        public DateTime? StartDate { get; set; }

        public string TargetType { get; set; }

        //refers to the target of the action.. blocking for example.
        //ReferenceUUID  Action     TargetUUID
        // 1234          Block       5678
        public string TargetUUID { get; set; }

        /// <summary>
        /// class for the action
        /// </summary>
        public string Type { get; set; }
    }
}