// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenWerx.Models
{
    public interface INode
    {
        string AccountUUID { get; set; }
        bool Active { get; set; }
        string CreatedBy { get; set; }
        DateTime DateCreated { get; set; }
        bool Deleted { get; set; }
        string GUUID { get; set; }
        string GuuidType { get; set; }
        string Image { get; set; }
        string Name { get; set; }
        int NSFW { get; set; }
        bool Private { get; set; }
        string RoleOperation { get; set; }
        int RoleWeight { get; set; }
        int SortOrder { get; set; }
        string Status { get; set; }

        [NotMapped]
        DateTime SubmitDate { get; set; }

        [NotMapped]
        string SubmitValue { get; set; }

        string UUID { get; set; }

        /// <summary>
        /// Defines the type of SettingUUID used (guid, hash.<algo> )..
        /// </summary>
        string UUIDType { get; set; }

        string UUParentID { get; set; }
        string UUParentIDType { get; set; }
    }
}