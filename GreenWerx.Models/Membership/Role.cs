// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Flags;
using GreenWerx.Models.Helpers;

namespace GreenWerx.Models.Membership
{
    [Table("Roles")]
    public class Role : INode
    {
        public Role()
        {
            this.UUParentID = string.Empty;
            this.UUParentIDType = string.Empty;
            this.Name = string.Empty;
            this.AccountUUID = string.Empty;
            this.Active = true;
            this.Deleted = false;
            this.Private = true;
            this.SortOrder = 0;
            this.CreatedBy = string.Empty;
            this.DateCreated = DateTime.MinValue;

            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "Role";
            RoleOperation = ">=";
            RoleWeight = RoleFlags.MemberRoleWeights.Member;
        }

        [StringLength(32)]
        public string AccountUUID { get; set; }

        public bool Active { get; set; }

        /// <summary>
        ///  Values: web, forms, or mobile
        /// </summary>
        public string AppType { get; set; }

        public string Category { get; set; }
        public string CategoryRoleName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public bool Deleted { get; set; }
        public DateTime? EndDate { get; set; }
        public string GUUID { get; set; }
        public string GuuidType { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }

        [JsonConverter(typeof(IntConverter))]
        public int NSFW { get; set; }

        //Set this to ignore EndDate
        public bool Persists { get; set; }

        public bool Private { get; set; }
        public string RoleOperation { get; set; }
        public int RoleWeight { get; set; }
        public int SortOrder { get; set; }
        public DateTime? StartDate { get; set; }
        public string Status { get; set; }

        // these are honeypot fields, not mapped to any table fields.
        [NotMapped]
        public DateTime SubmitDate { get; set; }

        [NotMapped]
        public string SubmitValue { get; set; }

        [Key]
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

        /// <summary>
        /// Values: 0-5 where 5 has the highest authority.
        /// </summary>
        public int Weight { get; set; }
    }
}