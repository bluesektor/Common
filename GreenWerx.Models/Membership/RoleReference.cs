// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// This is so we can group the permissions into roles, making
/// magagement of permissions to users easier..
/// </summary>
namespace GreenWerx.Models.Membership
{
    [Table("RolePermissions")]
    public class RolePermission : Node, ICloneable, INode
    {
        public RolePermission()
        {
            UUID = Guid.NewGuid().ToString("N");
            UUIDType = "RolePermission";
        }

        [StringLength(32)]
        public string PermissionUUID { get; set; }

        [StringLength(32)]
        public string RoleUUID { get; set; }

        public object Clone()
        {
            return new RolePermission();
        }
    }
}