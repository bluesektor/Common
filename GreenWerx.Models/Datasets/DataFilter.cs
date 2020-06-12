// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;

namespace GreenWerx.Models.Datasets
{
    public class DataFilter
    {
        public DataFilter()
        {
            PageResults = true;
            StartIndex = 0;
            PageSize = 25;
            Screens = new List<DataScreen>();
            UserRoleWeight = 0;
            IncludeDeleted = false;
            IncludeSystemAccount = false;
            FilterByAccount = true;
            IncludeAttributes = false;
            IncludeParent = false;
            IncludePrivate = false;
        }

        public bool FilterByAccount { get; set; }
        public bool IncludeAttributes { get; set; }
        public bool IncludeDeleted { get; set; }
        public bool IncludeParent { get; set; }
        public bool IncludePrivate { get; set; }
        public bool IncludeSystemAccount { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        //this is for sql cause it pages different than linq.
        public int Page { get; set; }

        public bool PageResults { get; set; }
        public int PageSize { get; set; }

        //todo
        // This uses sort order to prepend the
        // top x to the list. Only used for
        // the locations comboboxes for now.
        public int PrependTop { get; set; }

        public int TotalRecordCount { get; set; }
        public List<DataScreen> Screens { get; set; }

        //these are initial sorts, additional sorting can be
        //added to the screens.
        public string SortBy { get; set; }

        public string SortDirection { get; set; }
        public int StartIndex { get; set; }
        public string TimeZone { get; set; }
        public string Type { get; set; }

        public string ViewType { get; set; }


        //NOTE:The requesting user RoleWeight should be set before calling FilterInput
        public int UserRoleWeight { get; set; }
    }

    public class FilterParameters
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}