// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Geo;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.Models.Membership
{
    public class Employee : Node
    {
        public Employee()
        {
            UUIDType = "Employee";
        }

        public DateTime? BirthDate { get; set; }
        public string FirstName { get; set; }

        public DateTime? HireDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public DateTime? StartDate { get; set; }
        public string SurName { get; set; }

        //references the user table.
        [StringLength(32)]
        public long UserUUID { get; set; }
    }

    [Table("Profiles")]
    public class Profile : Node
    {
        public Profile()
        {
            UUIDType = "Profile";
            Attributes = new List<TMG.Attribute>();
        }

        [NotMapped]
        public List<TMG.Attribute> Attributes { get; set; }

        [NotMapped]
        public string BlockDescription { get; set; }

        [NotMapped]
        public bool Blocked { get; set; }

        public string Description { get; set; }

        [NotMapped]
        public double Distance { get; set; }

        public double? Latitude { get; set; }

        [NotMapped]
        public Location LocationDetail { get; set; }

        public string LocationDetailCache { get; set; }

        public string LocationType { get; set; }

        //todo pull from account? if empty?
        public string LocationUUID { get; set; }

        public double? Longitude { get; set; }

        public string LookingFor { get; set; }

        [NotMapped]
        public List<ProfileMember> Members { get; set; }

        public string MembersCache { get; set; }

        public string RelationshipStatus { get; set; }

        //public is for showing the profile when the user is not
        //logged in (anyone can see it).
        public bool ShowPublic { get; set; }

        public string Theme { get; set; }

        [NotMapped]
        public User User { get; set; }

        public string UserCache { get; set; }
        public string UserUUID { get; set; }

        [NotMapped]
        public List<VerificationEntry> Verifications { get; set; }

        public string VerificationsCache { get; set; }
        public string View { get; set; }
    }

    [Table("ProfileMembers")]
    public class ProfileMember : Node
    {
        public ProfileMember()
        {
            UUIDType = "ProfileMember";
        }

        public float BodyFat { get; set; }
        public string Description { get; set; }
        public DateTime? DOB { get; set; }

        //estimated,actual
        public string DobType { get; set; }

        public string Gender { get; set; }

        public int? Height { get; set; }

        public string HeightUOM { get; set; }

        public string LookingFor { get; set; }

        public string Orientation { get; set; }

        public string Preference { get; set; }

        public string ProfileUUID { get; set; }

        public string RelationshipStatus { get; set; }

        [StringLength(32)]
        public string UserUUID { get; set; }

        public float Weight { get; set; }

        public string WeightUOM { get; set; }
    }

    [Table("Celebrities")]
    public class Celebrity : Node
    {
        public Celebrity()
        {
            UUIDType = "Celebrity";
        }
        //dbname => Node

        public string Aliases { get; set; } //dbaka

        public string Description { get; set; }

        public string Occupation { get; set; }

        public string PoliticalParty { get; set; }

        public string Nationality { get; set; }

        public string Sex { get; set; }
        //uses locations table
        public string BirthPLaceUUID  { get; set; }

        public string Race { get; set; }

        public DateTime? DOB { get; set; } //dbborn

        //date of death
        public DateTime? DOD { get; set; }

        public string CauseOfDeath { get; set; }

        public decimal NetWorth { get; set; }

        public int PopularityRank { get; set; }

        public string Gender { get; set; }

        public int? Height { get; set; }

        public string HeightUOM { get; set; }

        public string Orientation { get; set; }

        public string RelationshipStatus { get; set; }

        public float Weight { get; set; }

        public string WeightUOM { get; set; }
    }
}