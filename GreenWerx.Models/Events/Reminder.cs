// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Helpers;
using GreenWerx.Models.Membership;

namespace GreenWerx.Models.Events
{
    [Table("Events")]
    public class Event : Node, INode
    {
        public Event()
        {
            UUIDType = "Event";
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public string Category { get; set; }
        public string Description { get; set; }

        [NotMapped]
        public double Distance { get; set; }

        public DateTime? EndDate { get; set; }
        public DateTime? EventDateTime { get; set; }

        /// <summary>
        /// For reference when editing the event.
        /// Technically an event can have more than one location,
        /// for simplicity the ui is restricted to one.
        /// </summary>
        [NotMapped]
        public string EventLocationUUID { get; set; }

        //daily, bi-weekly, monthly..
        public string Frequency { get; set; }

        /// <summary>
        /// uuid of the account that is hosting the event.
        /// </summary>
        public string HostAccountUUID { get; set; }

        [NotMapped]
        public string HostName { get; set; }

        public bool IsAffiliate { get; set; }

        public double? Latitude { get; set; }

        // for distance calc in clients
        public double? Longitude { get; set; }

        //how many times to remind
        public int RepeatCount { get; set; }

        //if this is set ignore RepeatCount
        public bool RepeatForever { get; set; }

        public DateTime StartDate { get; set; }
        public string Url { get; set; }

       // reference for import file is path to file(html)
        public string ReferenceType { get; set; }

        public string Reference { get; set; }
    }

    /// <summary>
    /// time slots
    /// </summary>
    [Table("EventGroups")]
    public class EventGroup : Node, INode
    {
        public EventGroup()
        {
            UUIDType = "EventGroup";
        }

        public string Body { get; set; }

        public string Category { get; set; }

        public DateTime EndDate { get; set; }
        public string EventUUID { get; set; }
        public string SessionUUID { get; set; }
        public DateTime StartDate { get; set; }
    }

    [Table("EventInventory")]
    public class EventItem : Node, INode
    {
        public EventItem()
        {
            UUIDType = "EventItem";
        }

        public float Count { get; set; }
        public string EventUUID { get; set; }
        // need, want, have  Count = how many are 'there'for the status. i.e. status = 'need' count = 10.
        //clone item, set status to need and count to how many are needed. set the clones parent id to the clone from uuid
    }

    [Table("EventLocations")]
    public class EventLocation : Node, INode
    {
        public EventLocation()
        {
            UUIDType = "EventLocation";
        }

        public EventLocation(Location l)
        {
            this.UUID = l.UUID;
            this.UUIDType = "EventLocation";// l.UUIDType;
            this.UUParentID = l.UUParentID;
            this.UUParentIDType = l.UUParentIDType;
            this.Name = l.Name;
            this.Status = l.Status;
            this.AccountUUID = l.AccountUUID;
            this.Active = l.Active;
            this.Deleted = l.Deleted;
            this.Private = l.Private;
            this.SortOrder = l.SortOrder;
            this.CreatedBy = l.CreatedBy;
            this.DateCreated = l.DateCreated;
            this.RoleWeight = l.RoleWeight;
            this.RoleOperation = l.RoleOperation;
            this.Image = l.Image;
            this.TimeZone = l.TimeZone;

            //EventUUID = l.EventUUID;
            //Email = l.Email;
            //TimeZone = l.TimeZone;
            RootId = l.RootId;
            Abbr = l.Abbr;
            Code = l.Code;
            CurrencyUUID = l.CurrencyUUID;
            LocationType = l.LocationType;
            Latitude = l.Latitude;
            Longitude = l.Longitude;
            FirstName = l.FirstName;
            LastName = l.LastName;
            Address1 = l.Address1;
            Address2 = l.Address2;
            City = l.City;
            State = l.State;
            Country = l.Country;
            Postal = l.Postal;
            Category = l.Category;
            Type = l.Type;
            Description = l.Description;
            IsBillingAddress = l.IsBillingAddress;
            Virtual = l.Virtual;
            isDefault = l.isDefault;
            OnlineStore = l.OnlineStore;
            IpNumStart = l.IpNumStart;
            IpNumEnd = l.IpNumEnd;
            IpVersion = l.IpVersion;
        }

        public string Abbr { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Category { get; set; }
        public string City { get; set; }
        public string Code { get; set; }
        public string Country { get; set; }
        public int CurrencyUUID { get; set; }
        public string Description { get; set; }

        [NotMapped]
        public double Distance { get; set; }

        public string Email { get; set; }
        public string EventUUID { get; set; }
        public string FirstName { get; set; }

        public string IpNumEnd { get; set; }

        public string IpNumStart { get; set; }

        public float IpVersion { get; set; }

        [JsonConverter(typeof(BoolConverter))]
        public bool IsBillingAddress { get; set; }

        [JsonConverter(typeof(BoolConverter))]
        public bool isDefault { get; set; }

        public string LastName { get; set; }

        public double? Latitude { get; set; }

        public string LocationType { get; set; }

        public double? Longitude { get; set; }

        public bool OnlineStore { get; set; }

        public string Postal { get; set; }

        //this replaces the Id field on the insert. the ParentId will reference this.
        public int RootId { get; set; }

        public string State { get; set; }
        public string TimeZone { get; set; }
        public int Type { get; set; }

        [JsonConverter(typeof(BoolConverter))]
        public bool Virtual { get; set; }
    }

    [Table("EventMembers")]
    public class EventMember : Node, INode
    {
        public EventMember()
        {
            UUIDType = "EventMember";
        }

        public string EventUUID { get; set; }

        public string UserUUID { get; set; }

        //List<Role> roles
    }

    [Table("Reminders")]
    public class Reminder : Node, INode //Event
    {
        public Reminder()
        {
            UUIDType = "Reminder";
            ReminderRules = new List<ReminderRule>();
            Event = new Event();
            Account = new Account();
        }

        [NotMapped]
        public Account Account { get; set; }

        public string Body { get; set; }

        [NotMapped]
        public Event Event { get; set; }

        public DateTime? EventDateTime { get; set; }

        public string EventUUID { get; set; }

        public bool Favorite { get; set; }

        //daily, bi-weekly, monthly..
        public string Frequency { get; set; }

        //forgot what this was for. I think it was how many times the reminder has notified..
        public int ReminderCount { get; set; }

        //   public DateTime EndDate { get; set; }
        [NotMapped]
        public List<ReminderRule> ReminderRules { get; set; }

        //  public string Category { get; set; }
        //how many times to remind
        public int RepeatCount { get; set; }

        //if this is set ignore RepeatCount
        public bool RepeatForever { get; set; }

        //   public DateTime StartDate { get; set; }
    }

    [Table("ReminderRules")]
    public class ReminderRule
    {
        public ReminderRule()
        {
            this.UUIDType = "ReminderRule";
        }

        [StringLength(32)]
        public string CreatedBy { get; set; }

        public DateTime? DateCreated { get; set; }

        public string RangeEnd { get; set; }

        public string RangeStart { get; set; }

        /// Date, time
        public string RangeType { get; set; }

        [StringLength(32)]
        public string ReminderUUID { get; set; }

        /// SkipRange
        public string RuleType { get; set; }

        //]]
        [Key]
        [StringLength(32)]
        public string UUID { get; set; }

        [NotMapped]
        public string UUIDType { get; set; }
    }
}