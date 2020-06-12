// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using GreenWerx.Models.Membership;

namespace GreenWerx.Models.Events
{
    public class EventsDashboard
    {
        public List<dynamic> Events { get; set; }

        public List<EventGroup> Groups { get; set; }

        public List<EventItem> Inventory { get; set; }
        public List<EventLocation> Locations { get; set; }

        public List<User> Members { get; set; }
    }
}