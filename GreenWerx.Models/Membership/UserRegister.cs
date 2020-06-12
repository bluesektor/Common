// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;

namespace GreenWerx.Models.Membership
{
    public class UserRegister
    {
        public string AccountUUID { get; set; }

        public DateTime? AgreedToTOS { get; set; }

        //website, mobile.app
        public string ClientType { get; set; }

        public string ConfirmPassword { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DOB { get; set; }

        public string Email { get; set; }

        public string Gender { get; set; }

        // [Required]
        [Display(Name = "User name")]
        public string Name { get; set; }

        // [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        public string ReferringMember { get; set; }
        public string RelationshipStatus { get; set; }
        public string SecurityAnswer { get; set; }
        public string SecurityQuestion { get; set; }

        // these are honeypot fields, not mapped to any table fields.
        public DateTime SubmitDate { get; set; }

        public string SubmitValue { get; set; }
        public bool UserIsPrivate { get; set; }
    }
}