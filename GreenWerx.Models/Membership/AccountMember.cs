// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models.Membership
{
    [Table("UsersInAccount")]
    public class AccountMember
    {
        public AccountMember()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "AccountMember";
        }

        [StringLength(32)]
        public string AccountUUID { get; set; }

        [StringLength(32)]
        public string MemberType { get; set; }

        [StringLength(32)]
        public string MemberUUID { get; set; }

        public int RoleWeight { get; set; }

        public int SortOrder { get; set; }

        public string Status { get; set; }

        [Key]
        [StringLength(32)]
        public string UUID { get; set; }

        /// <summary>
        /// Defines the type of UUID used (guid, hash.<algo> )..
        /// </summary>
        [StringLength(32)]
        public string UUIDType { get; set; }

        [StringLength(32)]
        public string UUParentID { get; set; }

        [StringLength(32)]
        public string UUParentIDType { get; set; }
    }
}