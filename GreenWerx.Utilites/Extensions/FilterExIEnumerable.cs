// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Models.Datasets;

namespace GreenWerx.Utilites.Extensions
{
    /*
    // https://github.com/dotnet/BenchmarkDotNet
    public static class FilterIEnumerable
    {
        public static IEnumerable Filter(this IEnumerable input,ref DataFilter filter, out int filter.TotalRecordCount)
        {

            filter.TotalRecordCount = 0;
            IEnumerable<T> results = new List<T>();

            if (input == null)
            {
                filter.TotalRecordCount = 0;
                return results;
            }

            if (filter == null)
            {
                filter.TotalRecordCount = input.Count();
                return input;
            }

            filter.Screens = filter.Screens.OrderBy(o => o.Order).ToList();

            if (filter.Screens.Any() == false)
            {
                results = input;
            }
            else
            {
                // string junction = "";

                foreach (DataScreen f in filter.Screens)
                {
                    if (input.Any() == false)
                        break;

                    if (string.IsNullOrWhiteSpace(f.Command) || string.IsNullOrWhiteSpace(f.Field))
                        continue;

                    //switch for IEnumerable<T> 
                    switch (f.Command?.ToUpper())
                    {
                        case "BETWEEN":
                            results.Concat(input.GetBetween(f));
                            break;
                        case "SEARCHBY":
                        case "SEARCH!BY":
                            results.Concat(input.SearchBy(f));
                            break;
                        case "ORDERBY":     //order by ascending is default
                        case "ORDERBYDESC":
                            results.Concat(input.OrderBy(f));
                            break;
                        case "DISTINCTBY":
                            throw new NotImplementedException();
                          //  results.Concat(input.DistinctBy(f));
                            break;
                        case "DISTINCT":
                            results.Concat(input.Distinct());
                            break;
                    }
                    //switch (junction)
                    //{
                    //    case "AND":
                    //        break;
                    //    case "OR":
                    //        break;
                    //}
                    ////junction = f.Junction;//junction is a trailing operator so it'll be applied on the next loop
                }
            }

            // if (!commandExecuted) { filter.TotalRecordCount = input.Count();return input  }

            // remove duplicates
            results = results.GroupBy(x => x.UUID).Select(group => group.First()).ToList();

            filter.ResultCount = results.Count();
            if (filter.PageResults)
            {
                var t = results.Paginate(filter.StartIndex, filter.PageSize);
                return t;
            }
            else
                return results;

            //todo make sure there's no data leaks for users under roles
            //this will need to be changed for store items and stuff(we wan't enought data to get out to show
            //product info.
            //todo the roleweight need to be defined, but for now if a basic user is logged in
            //they should be in RoleWeight 1 or 2. Roleweight 4 and above should start getting into admin range.
            //todo we can check the data's role weight and operator here also.
            ////if (filter.UserRoleWeight < 4)
            ////{
            ////    res = res.Select(s => new
            ////    {
            ////        Name = s.Name,
            ////        UUID = s.UUID
            ////    }).Cast<dynamic>().ToList();
            ////}

        }

        public static IEnumerable<dynamic> DistinctBy<T>(this IEnumerable<dynamic> input, DataScreen screen)
        {

            if (input == null || input.Any() == false)
            {
                return new List<dynamic>();
            }
            switch (screen.Field?.ToUpper())
            {
                //Node Properties
                case "UUID":
                    input = input.DistinctBy(d => d.UUID).ToList();
                    break;
                case "UUIDTYPE":
                    input = input.DistinctBy(d => d.UUIDType).ToList();
                    break;
                case "UUPARENTID":
                    input = input.DistinctBy(d => d.UUParentID).ToList();
                    break;
                case "UUPARENTIDTYPE":
                    input = input.DistinctBy(d => d.UUParentIDType).ToList();
                    break;
                case "NAME":
                    input = input.DistinctBy(d => d.Name).ToList();
                    break;
                case "STATUS":
                    input = input.DistinctBy(d => d.Status).ToList();
                    break;
                case "ACCOUNTUUID":
                    input = input.DistinctBy(d => d.AccountUUID).ToList();
                    break;
                case "ACTIVE":
                    input = input.DistinctBy(d => d.Active).ToList();
                    break;
                case "DELETED":
                    input = input.DistinctBy(d => d.Deleted).ToList();
                    break;
                case "PRIVATE":
                    input = input.DistinctBy(d => d.Private).ToList();
                    break;
                case "SORTORDER":
                    input = input.DistinctBy(d => d.SortOrder).ToList();
                    break;
                case "CREATEDBY":
                    input = input.DistinctBy(d => d.CreatedBy).ToList();
                    break;
                case "DATECREATED":
                    input = input.Where(w => Convert.ToDateTime(w?.DateCreated) == Convert.ToDateTime(screen.Value)).ToList();
                    break;
                //Strain Specific
                case "HARVESTTIME":
                    input = input.DistinctBy(d => d.HarvestTime).ToList();
                    break;
                //Category Specific
                case "CATEGORYTYPE":
                    input = input.DistinctBy(d => d.CategoryType).ToList();
                    break;
                //Product Specific
                case "CATEGORY":
                    input = input.DistinctBy(d => d.CategoryUUID).ToList();
                    break;
                case "MANUFACTURER":
                    input = input.DistinctBy(d => d.ManufacturerUUID).ToList();
                    break;
                case "DEPARTMENT":
                    input = input.DistinctBy(d => d.DepartmentUUID).ToList();
                    break;
                //Vendor Specific
                case "BREEDER":
                    input = input.DistinctBy(d => d.Breeder).ToList();
                    break;
                case "GROWER":
                    input = input.Where(w => w?.Grower).ToList();
                    break;
                case "DISPENSARY":
                    input = input.DistinctBy(d => d.Dispensary).ToList();
                    break;
                case "BREEDERTYPE":
                    input = input.DistinctBy(d => d.BreederType).ToList();
                    break;
                //Units Of Measure
                case "CATEGORYUUID":
                    #region original code not ported. may not be needed
                    ////CategoryManager categoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    ////Category category = categoryManager.Get(f.SearchTerm);
                    ////if (category == null)
                    ////    continue;
                    ////input = input.Where(w => w.Category?.EqualsIgnoreCase(category.Name.ToUpper()).ToList();
                    #endregion
                    Debug.Assert(false, "Verify the search term is category and not uuid");
                    input = input.DistinctBy(d => d.BreederType).ToList();
                    break;
            }
            return input;
        }

        public static IEnumerable<dynamic> OrderBy(this IEnumerable<dynamic> input, DataScreen screen)
        {
            if (input == null || input.Any() == false)
            {
                return new List<dynamic>();
            }

            bool orderbyDesc = false;
            if (screen.Command.ToUpper().Contains("DESC"))
                orderbyDesc = true;

            switch (screen.Field.ToUpper())
            {
                //Node Properties
                case "UUID":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.UUID).ToList();
                    else
                        input = input.OrderBy(d => d.UUID).ToList();
                    break;
                case "UUIDTYPE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.UUIDType).ToList();
                    else
                        input = input.OrderBy(d => d.UUIDType).ToList();
                    break;
                case "UUPARENTID":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.UUParentID).ToList();
                    else
                        input = input.OrderBy(d => d.UUParentID).ToList();
                    break;
                case "UUPARENTIDTYPE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.UUParentIDType).ToList();
                    else
                        input = input.OrderBy(d => d.UUParentIDType).ToList();
                    break;
                case "NAME":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Name).ToList();
                    else
                        input = input.OrderBy(d => d.Name).ToList();
                    break;
                case "STATUS":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Status).ToList();
                    else
                        input = input.OrderBy(d => d.Status).ToList();
                    break;
                case "ACCOUNTUUID":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.AccountUUID).ToList();
                    else
                        input = input.OrderBy(d => d.AccountUUID).ToList();
                    break;
                case "ACTIVE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Active).ToList();
                    else
                        input = input.OrderBy(d => d.Active).ToList();
                    break;
                case "DELETED":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Deleted).ToList();
                    else
                        input = input.OrderBy(d => d.Deleted).ToList();
                    break;
                case "PRIVATE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Private).ToList();
                    else
                        input = input.OrderBy(d => d.Private).ToList();
                    break;
                case "SORTORDER":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.SortOrder).ToList();
                    else
                        input = input.OrderBy(d => d.SortOrder).ToList();
                    break;
                case "CREATEDBY":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.CreatedBy).ToList();
                    else
                        input = input.OrderBy(d => d.CreatedBy).ToList();
                    break;
                case "DATECREATED":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.DateCreated).ToList();
                    else
                        input = input.Where(w => Convert.ToDateTime(w?.DateCreated) == Convert.ToDateTime(screen.Value)).ToList();
                    break;
                //Strain Specific
                case "HARVESTTIME":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.HarvestTime).ToList();
                    else
                        input = input.OrderBy(d => d.HarvestTime).ToList();
                    break;
                //Category Specific
                case "CATEGORYTYPE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.CategoryType).ToList();
                    else
                        input = input.OrderBy(d => d.CategoryType).ToList();
                    break;
                //Product Specific
                case "CATEGORY":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.CategoryUUID).ToList();
                    else
                        input = input.OrderBy(d => d.CategoryUUID).ToList();
                    break;
                case "MANUFACTURER":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.ManufacturerUUID).ToList();
                    else
                        input = input.OrderBy(d => d.ManufacturerUUID).ToList();
                    break;
                case "DEPARTMENT":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.DepartmentUUID).ToList();
                    else
                        input = input.OrderBy(d => d.DepartmentUUID).ToList();
                    break;
                //Vendor Specific
                case "BREEDER":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Breeder).ToList();
                    else
                        input = input.OrderBy(d => d.Breeder).ToList();
                    break;
                case "GROWER":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Grower).ToList();
                    else
                        input = input.Where(w => w?.Grower).ToList();
                    break;
                case "DISPENSARY":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.Dispensary).ToList();
                    else
                        input = input.OrderBy(d => d.Dispensary).ToList();
                    break;
                case "BREEDERTYPE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.BreederType).ToList();
                    else
                        input = input.OrderBy(d => d.BreederType).ToList();
                    break;
                //Units Of Measure
                case "CATEGORYUUID":
                    #region original code not ported. may not be needed
                    ////CategoryManager categoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    ////Category category = categoryManager.Get(f.SearchTerm);
                    ////if (category == null)
                    ////    continue;
                    ////input = input.Where(w => w.Category?.EqualsIgnoreCase(category.Name.ToUpper()).ToList();
                    #endregion
                    Debug.Assert(false, "todo Verify the search term is category and not uuid");
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.CategoryUUID).ToList();
                    else
                        input = input.OrderBy(d => d.CategoryUUID).ToList();
                    break;
                //Finance account transaction
                case "TRANSACTIONDATE":
                    if (orderbyDesc)
                        input = input.OrderByDescending(d => d.TransactionDate).ToList();
                    else
                        input = input.OrderBy(d => d.TransactionDate).ToList();
                    break;
            }

            return input;
        }

        public static IEnumerable<dynamic> SearchBy(this IEnumerable<dynamic> input, DataScreen screen)
        {
            if (input == null || input.Any() == false || string.IsNullOrWhiteSpace(screen.Value) || string.IsNullOrWhiteSpace(screen.Field))
            {
                return new List<dynamic>();
            }
            try
            {
                screen.Operator = string.IsNullOrWhiteSpace(screen.Operator) ? string.Empty : screen.Operator;

                switch (screen.Field?.ToUpper())
                {
                    //API Key filter
                    case "VALUE"://settings
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.Value?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Value?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.Value?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    //case "KEY": //settings
                    //    if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                    //        input = input.Where(w => w.Key?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                    //    else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                    //        input = input.Where(w => w.Key?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                    //    else
                    //        input = input.Where(w => w.Key?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                    //    break;
                    //Node Properties
                    case "UUID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.UUID?.ToUpper().Contains(screen.Value));
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.UUID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.UUID == screen.Value).ToList();
                        break;
                    case "HOSTACCOUNTUUID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.HostAccountUUID?.ToUpper().Contains(screen.Value));
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.HostAccountUUID != screen.Value);
                        else
                            input = input.Where(w => w.HostAccountUUID == screen.Value);
                        break;

                    //case "BODY":
                    //    if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                    //    {
                    //        string value = screen.Value?.ToUpper();
                    //        input = input.Where(w => string.IsNullOrWhiteSpace(w?.Body) == false && w?.Body?.ToUpper()?.Contains(value))?.ToList();
                    //    }
                    //    else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                    //        input = input.Where(w => w?.Body != screen.Value).ToList();
                    //    else
                    //        input = input.Where(w => w?.Body?.ToUpper()?.Trim() == screen.Value?.ToUpper()?.Trim()).ToList();
                    //    break;
                    case "VARIETYUUID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.VarietyUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.VarietyUUID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.VarietyUUID?.ToUpper()?.Trim() == screen.Value?.ToUpper()?.Trim()).ToList();
                        break;
                    case "UUIDTYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.UUIDType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.UUIDType != screen.Value).ToList();
                        else
                            input = input.Where(w => w.UUIDType == screen.Value).ToList();
                        break;
                    case "UUPARENTID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.UUParentID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.UUParentID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.UUParentID == screen.Value).ToList();
                        break;
                    case "UUPARENTIDTYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.UUParentIDType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.UUParentIDType != screen.Value).ToList();
                        else
                            input = input.Where(w => w.UUParentIDType == screen.Value).ToList();
                        break;
                    case "NAME":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.Name?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Name?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.Name?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    case "DESCRIPTION":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                        {
                            input = input.Where(w => string.IsNullOrEmpty(w.Description) == false &&
                                                     w.Description?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                        }
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Description?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.Description?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    case "STATUS":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.Status?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Status?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.Status?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    case "ACCOUNTUUID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.AccountUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.AccountUUID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.AccountUUID == screen.Value).ToList();
                        break;
                    case "ACTIVE":

                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.Active) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w.Active) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    case "ISDEFAULT":
                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.IsDefault) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w.IsDefault) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    case "FAVORITE":
                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.Favorite) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w.Favorite) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    case "BREEDER":
                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.Breeder) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w.Breeder) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    case "BANNED":

                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.Banned) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w?.Banned) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    case "LOCKEDOUT":
                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.LockedOut) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w?.LockedOut) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    case "DELETED":

                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToBoolean(w.Deleted) != Convert.ToBoolean(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToBoolean(w?.Deleted) == Convert.ToBoolean(screen.Value)).ToList();
                        break;
                    //case "PRIVATE": //commented out since it's handled in roles  manager for now.
                    //   if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                    //        input = input.Where(w => Convert.ToBoolean(w.Private) != Convert.ToBoolean(screen.Value)).ToList();
                    //    else
                    //        input = input.Where(w => Convert.ToBoolean(w.Private) == Convert.ToBoolean(screen.Value)).ToList();
                    //    break;
                    case "SORTORDER":

                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.SortOrder != StringEx.ConvertTo<int>(screen.Value)).ToList();
                        else
                            input = input.Where(w => w.SortOrder == StringEx.ConvertTo<int>(screen.Value)).ToList();
                        break;
                    case "CREATEDBY":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.CreatedBy?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.CreatedBy != screen.Value).ToList();
                        else
                            input = input.Where(w => w.CreatedBy == screen.Value).ToList();
                        break;
                    case "DATECREATED":
                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => Convert.ToDateTime(w.DateCreated) != Convert.ToDateTime(screen.Value)).ToList();
                        else
                            input = input.Where(w => Convert.ToDateTime(w.DateCreated) == Convert.ToDateTime(screen.Value)).ToList();
                        break;
                    //Strain Specific
                    case "HARVESTTIME":
                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.HarvestTime != screen.Value).ToList();
                        else
                            input = input.Where(w => w.HarvestTime == StringEx.ConvertTo<int>(screen.Value)).ToList();
                        break;
                    //Category Specific
                    case "CATEGORYTYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.CategoryType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.CategoryType?.ToUpper() != screen.Value).ToList();
                        else
                        {
                            input = input.Where(w => StringEx.EqualsEx(w.CategoryType, screen.Value))?.ToList();
                        }
                        break;
                    //Location and Finance Account
                    case "LOCATIONTYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.LocationType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.LocationType?.ToString()?.ToUpper() != screen.Value).ToList();
                        else
                        {
                            input = input.Where(w => StringEx.EqualsEx(w.LocationType, screen.Value)).ToList();
                        }
                        break;
                    //Event/Product Specific
                    case "CATEGORY":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.Category?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Category?.ToUpper() != screen.Value.ToUpper()).ToList();
                        else
                            input = input.Where(w => w.Category?.ToUpper() == screen.Value.ToUpper()).ToList();

                        //TODO CategoryUUID is from products, we'll have to pass in a type to switch between fields
                        //if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                        //    input = input.Where(w => w.CategoryUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        //else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                        //    input = input.Where(w => w.CategoryUUID != screen.Value).ToList();
                        //else
                        //    input = input.Where(w => w.CategoryUUID == screen.Value).ToList();
                        break;
                    case "MANUFACTURER":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.ManufacturerUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.ManufacturerUUID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.ManufacturerUUID == screen.Value).ToList();
                        break;
                    case "DEPARTMENT":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.DepartmentUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.DepartmentUUID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.DepartmentUUID == screen.Value).ToList();
                        break;
                    case "GROWER":

                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Grower != screen.Value).ToList();
                        else
                            input = input.Where(w => w.Grower == true).ToList();
                        break;
                    case "DISPENSARY":

                        if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Dispensary != screen.Value).ToList();
                        else
                            input = input.Where(w => w.Dispensary == true).ToList();
                        break;
                    case "BREEDERTYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.BreederType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.BreederType?.ToUpper() != screen.Value).ToList();
                        else
                            input = input.Where(w => StringEx.EqualsEx(w.BreederType, screen.Value)).ToList();
                        break;
                    //Units Of Measure
                    case "CATEGORYUUID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.CategoryUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.CategoryUUID != screen.Value).ToList();
                        else
                            input = input.Where(w => w.CategoryUUID == screen.Value).ToList();
                        break;
                    case "GUUIDTYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.GuuidType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.GuuidType?.ToUpper() != screen.Value).ToList();
                        else
                            input = input.Where(w => StringEx.EqualsEx(w.GuuidType, screen.Value)).ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return input;
        }

        public static IEnumerable<dynamic> GetBetween(this IEnumerable<dynamic> input, DataScreen screen)
        {
            if (input == null || input.Any() == false || string.IsNullOrWhiteSpace(screen.Value) ||
                string.IsNullOrWhiteSpace(screen.Field) || string.IsNullOrWhiteSpace(screen.Operator))
            {
                return new List<dynamic>();
            }
            try
            {
                DateTime startDate = DateTime.Now;
                DateTime endDate = DateTime.Now;
                if (screen.Value.Contains("|"))
                {
                    string[] dates = screen.Value.Split('|');

                    if (!DateTime.TryParse(dates[0], out startDate))
                    {
                        Debug.Assert(false, "COULD NOT PARSE START DATE!");
                        return input;
                    }

                    if (!DateTime.TryParse(dates[1], out endDate))
                    {
                        Debug.Assert(false, "COULD NOT PARSE END DATE!");
                        return input;
                    }
                }
                else
                {
                    if (!DateTime.TryParse(screen.Value, out startDate))
                    {
                        Debug.Assert(false, "COULD NOT PARSE START DATE!");
                        return input;
                    }
                }

                switch (screen.Field?.ToUpper())
                {
                    case "DATECREATED":
                        if (screen.Operator == "INCLUSIVE")
                        {
                            input = input.Where(w => w.DateCreated != null && w.DateCreated >= startDate && endDate <= w.DateCreated).ToList();
                        }
                        else if (screen.Operator == "INCLUSIVE.END")
                        {
                            input = input.Where(w => w.DateCreated > startDate && endDate <= w.DateCreated).ToList();
                        }
                        else if (screen.Operator == "INCLUSIVE.START")
                        {
                            input = input.Where(w => w.DateCreated >= startDate && endDate < w.DateCreated).ToList();
                        }
                        else
                            input = input.Where(w => w.DateCreated > startDate && endDate < w.DateCreated).ToList();
                        break;
                    case "STARTDATE":
                        if (screen.Operator == "INCLUSIVE")
                        {
                            input = input.Where(w => w.StartDate != null && w.StartDate >= startDate && endDate <= w.StartDate).ToList();
                        }
                        else if (screen.Operator == "INCLUSIVE.END")
                        {
                            input = input.Where(w => w.StartDate > startDate && endDate <= w.StartDate).ToList();
                        }
                        else if (screen.Operator == "INCLUSIVE.START")
                        {
                            input = input.Where(w => w.StartDate >= startDate && endDate < w.StartDate).ToList();
                        }
                        else
                            input = input.Where(w => w.StartDate > startDate && endDate < w.StartDate).ToList();
                        break;
                    case "ENDDATE":
                        if (screen.Operator == "INCLUSIVE")
                        {
                            input = input.Where(w => w.EndDate != null && w.EndDate >= startDate && endDate <= w.EndDate).ToList();
                        }
                        else if (screen.Operator == "INCLUSIVE.END")
                        {
                            input = input.Where(w => w.EndDate > startDate && endDate <= w.EndDate).ToList();
                        }
                        else if (screen.Operator == "INCLUSIVE.START")
                        {
                            input = input.Where(w => w.EndDate >= startDate && endDate < w.EndDate).ToList();
                        }
                        else
                            input = input.Where(w => w.EndDate > startDate && endDate < w.EndDate).ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return input;
        }

        public static IEnumerable<dynamic> Paginate(this IEnumerable<dynamic> input, int? startIndex = 0, int? pageSize = 1)
        {
            if (startIndex == null || pageSize == null)
                return new List<dynamic>();

            if (input == null || input.Any() == false)
            {
                return new List<dynamic>();
            }

            return input.Skip(startIndex ?? 0).Take(pageSize ?? 25);
        }

        public static IEnumerable<dynamic> Paginate(this IEnumerable<dynamic> input, int startIndex = 0, int pageSize = 1)
        {
            if (input == null || input.Any() == false)
            {
                return new List<dynamic>();
            }
            return input.Skip(startIndex).Take(pageSize);
        }

        public static IEnumerable<dynamic> PaginateByPage(this IEnumerable<dynamic> input, int page = 1, int pageSize = 1)
        {
            IEnumerable<dynamic> value = null;// default(IEnumerable<T>);

            if (input == null || input.Any() == false)
            {
                return new List<dynamic>();                // return (List<T>)value;
            }
            if (input.Count() < pageSize)
                pageSize = input.Count();


            return input.Skip(page * pageSize).Take(pageSize);
        }

        /// <summary>
        /// This function is an aggregate search. It doesn't get applied
        /// from the filter function.
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="filter"></param>
        /// <param name="filter.ResultCount"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> Search(this IEnumerable<dynamic> input,ref DataFilter filter, out int filter.ResultCount)
        {

            filter.TotalRecordCount = 0;
            IEnumerable<dynamic> results = null;

            if (filter == null)
            {
                if (input != null)
                    filter.ResultCount = input.Count();

                return input;
            }

            filter.Screens = filter.Screens.OrderBy(o => o.Order).ToList();

            foreach (DataScreen f in filter.Screens)
            {
                if (!input.Any())
                    break;

                if (string.IsNullOrWhiteSpace(f.Command) || string.IsNullOrWhiteSpace(f.Field))
                    continue;

                switch (f.Command?.ToUpper())
                {
                    case "SEARCHBY":
                    case "SEARCH!BY":
                        results.Append(input.SearchBy(f));//results are added to gether since we should be searching across multiple fields.
                        break;
                }
            }

            input = results.DistinctBy(d => d.UUID);//now remove duplicate items.
            filter.ResultCount = input.Count();
            if (filter.PageResults)
            {
                var t = input.Paginate(filter.StartIndex, filter.PageSize);
                return t;
            }
            else
                return input;

            //todo make sure there's no data leaks for users under roles
            //this will need to be changed for store items and stuff(we wan't enought data to get out to show
            //product info.
            //todo the roleweight need to be defined, but for now if a basic user is logged in
            //they should be in RoleWeight 1 or 2. Roleweight 4 and above should start getting into admin range.
            //todo we can check the data's role weight and operator here also.
            ////if (filter.UserRoleWeight < 4)
            ////{
            ////    res = res.Select(s => new
            ////    {
            ////        Name = s.Name,
            ////        UUID = s.UUID
            ////    }).Cast<dynamic>().ToList();
            ////}

        }


    }
    */

    public static class FilterDynamicToIEnumerable
     {
      public static IEnumerable<dynamic> Filter<T>(this IEnumerable<T> source,  ref DataFilter filter)
      {
            if (filter == null)
                filter = new DataFilter();

           filter.TotalRecordCount = 0;
        
           //  IEnumerable<T> results = null;// new List<dynamic>();
            IEnumerable<dynamic> results = new List<dynamic>();

            if (source == null)
          {
              filter.TotalRecordCount = 0;
                return results.Cast<dynamic>();
          }

          if (filter == null)
          {
              filter.TotalRecordCount = source.Count();
              return source.Cast<dynamic>();
          }

          filter.Screens = filter.Screens.OrderBy(o => o.Order).ToList();

        if (filter.Screens.Any() == false)
        {
            results = source.Cast<dynamic>();
        }
        else
        {
                var input = source.Cast<dynamic>();
              // string junction = "";

              foreach (DataScreen f in filter.Screens)
              {
                  if (input.Any() == false)
                      break;

                  if (string.IsNullOrWhiteSpace(f.Command) || string.IsNullOrWhiteSpace(f.Field))
                      continue;


                    switch (f.Command?.ToUpper())
                    {
                        case "BETWEEN":
                            results = results.Concat(input.GetBetween(f));
                            break;
                        case "SEARCHBY":
                        case "SEARCH!BY":
                               results = results.Concat(input.SearchBy(f));
                            break;
                        case "ORDERBY":     //order by ascending is default
                        case "ORDERBYDESC":
                            results = results.Concat(input.OrderBy<T>(f));
                            break;
                        case "DISTINCTBY":
                            results = results.Concat(input.DistinctBy(f));
                            break;
                        case "DISTINCT":
                            results = results.Concat(input.Distinct());
                            break;
                    }


                    //switch (junction)
                    //{
                    //    case "AND":
                    //        break;
                    //    case "OR":
                    //        break;
                    //}
                    ////junction = f.Junction;//junction is a trailing operator so it'll be applied on the next loop
                }
            }

            // if (!commandExecuted) { filter.TotalRecordCount = input.Count();return input  }

            // remove duplicates
            results = results.GroupBy(x => x.UUID).Select(group => group.First()).ToList();

          filter.TotalRecordCount = results.Count();
          if (filter.PageResults)
          {
              var  t = results.Paginate(filter.StartIndex, filter.PageSize);
              return t;
          }
          else
              return results;

          //todo make sure there's no data leaks for users under roles
          //this will need to be changed for store items and stuff(we wan't enought data to get out to show
          //product info.
          //todo the roleweight need to be defined, but for now if a basic user is logged in
          //they should be in RoleWeight 1 or 2. Roleweight 4 and above should start getting into admin range.
          //todo we can check the data's role weight and operator here also.
          ////if (filter.UserRoleWeight < 4)
          ////{
          ////    res = res.Select(s => new
          ////    {
          ////        Name = s.Name,
          ////        UUID = s.UUID
          ////    }).Cast<dynamic>().ToList();
          ////}

      }

      public static IEnumerable<dynamic> DistinctBy(this IEnumerable<dynamic> input, DataScreen screen)
      {

          if (input == null || input.Any() == false)
          {
              return input;
          }
          switch (screen.Field?.ToUpper())
          {
              //Node Properties
              case "UUID":
                  input = input.DistinctBy(d => d.UUID).ToList();
                  break;
              case "UUIDTYPE":
                  input = input.DistinctBy(d => d.UUIDType).ToList();
                  break;
              case "UUPARENTID":
                  input = input.DistinctBy(d => d.UUParentID).ToList();
                  break;
              case "UUPARENTIDTYPE":
                  input = input.DistinctBy(d => d.UUParentIDType).ToList();
                  break;
              case "NAME":
                  input = input.DistinctBy(d => d.Name).ToList();
                  break;
              case "STATUS":
                  input = input.DistinctBy(d => d.Status).ToList();
                  break;
              case "ACCOUNTUUID":
                  input = input.DistinctBy(d => d.AccountUUID).ToList();
                  break;
              case "ACTIVE":
                  input = input.DistinctBy(d => d.Active).ToList();
                  break;
              case "DELETED":
                  input = input.DistinctBy(d => d.Deleted).ToList();
                  break;
              case "PRIVATE":
                  input = input.DistinctBy(d => d.Private).ToList();
                  break;
              case "SORTORDER":
                  input = input.DistinctBy(d => d.SortOrder).ToList();
                  break;
              case "CREATEDBY":
                  input = input.DistinctBy(d => d.CreatedBy).ToList();
                  break;
              case "DATECREATED":
                  input = input.Where(w => Convert.ToDateTime(w?.DateCreated) == Convert.ToDateTime(screen.Value)).ToList();
                  break;
              //Strain Specific
              case "HARVESTTIME":
                  input = input.DistinctBy(d => d.HarvestTime).ToList();
                  break;
              //Category Specific
              case "CATEGORYTYPE":
                  input = input.DistinctBy(d => d.CategoryType).ToList();
                  break;
              //Product Specific
              case "CATEGORY":
                  input = input.DistinctBy(d => d.CategoryUUID).ToList();
                  break;
              case "MANUFACTURER":
                  input = input.DistinctBy(d => d.ManufacturerUUID).ToList();
                  break;
              case "DEPARTMENT":
                  input = input.DistinctBy(d => d.DepartmentUUID).ToList();
                  break;
              //Vendor Specific
              case "BREEDER":
                  input = input.DistinctBy(d => d.Breeder).ToList();
                  break;
              case "GROWER":
                  input = input.Where(w => w?.Grower).ToList();
                  break;
              case "DISPENSARY":
                  input = input.DistinctBy(d => d.Dispensary).ToList();
                  break;
              case "BREEDERTYPE":
                  input = input.DistinctBy(d => d.BreederType).ToList();
                  break;
              //Units Of Measure
              case "CATEGORYUUID":
                  #region original code not ported. may not be needed
                  ////CategoryManager categoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                  ////Category category = categoryManager.Get(f.SearchTerm);
                  ////if (category == null)
                  ////    continue;
                  ////input = input.Where(w => w.Category?.EqualsIgnoreCase(category.Name.ToUpper()).ToList();
                  #endregion
                  Debug.Assert(false, "Verify the search term is category and not uuid");
                  input = input.DistinctBy(d => d.BreederType).ToList();
                  break;
          }
          return input;
      }

      public static IEnumerable<dynamic> OrderBy<T>(this IEnumerable<dynamic> input, DataScreen screen)
      {
          if (input == null || input.Any() == false)
          {
                 return new List<dynamic>();
             }

          bool orderbyDesc = false;
          if (screen.Command.ToUpper().Contains("DESC"))
              orderbyDesc = true;

          switch (screen.Field.ToUpper())
          {
              //Node Properties
              case "UUID":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.UUID).ToList();
                  else
                      input = input.OrderBy(d => d.UUID).ToList();
                  break;
              case "UUIDTYPE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.UUIDType).ToList();
                  else
                      input = input.OrderBy(d => d.UUIDType).ToList();
                  break;
              case "UUPARENTID":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.UUParentID).ToList();
                  else
                      input = input.OrderBy(d => d.UUParentID).ToList();
                  break;
              case "UUPARENTIDTYPE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.UUParentIDType).ToList();
                  else
                      input = input.OrderBy(d => d.UUParentIDType).ToList();
                  break;
              case "NAME":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Name).ToList();
                  else
                      input = input.OrderBy(d => d.Name).ToList();
                  break;
              case "STATUS":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Status).ToList();
                  else
                      input = input.OrderBy(d => d.Status).ToList();
                  break;
              case "ACCOUNTUUID":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.AccountUUID).ToList();
                  else
                      input = input.OrderBy(d => d.AccountUUID).ToList();
                  break;
              case "ACTIVE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Active).ToList();
                  else
                      input = input.OrderBy(d => d.Active).ToList();
                  break;
              case "DELETED":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Deleted).ToList();
                  else
                      input = input.OrderBy(d => d.Deleted).ToList();
                  break;
              case "PRIVATE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Private).ToList();
                  else
                      input = input.OrderBy(d => d.Private).ToList();
                  break;
              case "SORTORDER":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.SortOrder).ToList();
                  else
                      input = input.OrderBy(d => d.SortOrder).ToList();
                  break;
              case "CREATEDBY":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.CreatedBy).ToList();
                  else
                      input = input.OrderBy(d => d.CreatedBy).ToList();
                  break;
              case "DATECREATED":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.DateCreated).ToList();
                  else
                      input = input.Where(w => Convert.ToDateTime(w?.DateCreated) == Convert.ToDateTime(screen.Value)).ToList();
                  break;
              //Strain Specific
              case "HARVESTTIME":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.HarvestTime).ToList();
                  else
                      input = input.OrderBy(d => d.HarvestTime).ToList();
                  break;
              //Category Specific
              case "CATEGORYTYPE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.CategoryType).ToList();
                  else
                      input = input.OrderBy(d => d.CategoryType).ToList();
                  break;
              //Product Specific
              case "CATEGORY":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.CategoryUUID).ToList();
                  else
                      input = input.OrderBy(d => d.CategoryUUID).ToList();
                  break;
              case "MANUFACTURER":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.ManufacturerUUID).ToList();
                  else
                      input = input.OrderBy(d => d.ManufacturerUUID).ToList();
                  break;
              case "DEPARTMENT":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.DepartmentUUID).ToList();
                  else
                      input = input.OrderBy(d => d.DepartmentUUID).ToList();
                  break;
              //Vendor Specific
              case "BREEDER":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Breeder).ToList();
                  else
                      input = input.OrderBy(d => d.Breeder).ToList();
                  break;
              case "GROWER":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Grower).ToList();
                  else
                      input = input.Where(w => w?.Grower).ToList();
                  break;
              case "DISPENSARY":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.Dispensary).ToList();
                  else
                      input = input.OrderBy(d => d.Dispensary).ToList();
                  break;
              case "BREEDERTYPE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.BreederType).ToList();
                  else
                      input = input.OrderBy(d => d.BreederType).ToList();
                  break;
              //Units Of Measure
              case "CATEGORYUUID":
                  #region original code not ported. may not be needed
                  ////CategoryManager categoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                  ////Category category = categoryManager.Get(f.SearchTerm);
                  ////if (category == null)
                  ////    continue;
                  ////input = input.Where(w => w.Category?.EqualsIgnoreCase(category.Name.ToUpper()).ToList();
                  #endregion
                  Debug.Assert(false, "todo Verify the search term is category and not uuid");
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.CategoryUUID).ToList();
                  else
                      input = input.OrderBy(d => d.CategoryUUID).ToList();
                  break;
              //Finance account transaction
              case "TRANSACTIONDATE":
                  if (orderbyDesc)
                      input = input.OrderByDescending(d => d.TransactionDate).ToList();
                  else
                      input = input.OrderBy(d => d.TransactionDate).ToList();
                  break;
          }

          return input;
      }

      public static IEnumerable<dynamic> SearchBy(this IEnumerable<dynamic> input, DataScreen screen)
      {
          if (input == null || input.Any() == false || string.IsNullOrWhiteSpace(screen.Value) || string.IsNullOrWhiteSpace(screen.Field))
          {
                 return new List<dynamic>();
             }
          try
          {
              screen.Operator = string.IsNullOrWhiteSpace(screen.Operator) ? string.Empty : screen.Operator;

              switch (screen.Field?.ToUpper())
              {
                  //API Key filter
                  case "VALUE"://settings
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.Value?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Value?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                      else
                          input = input.Where(w => w.Value?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                      break;
                    case "REFERENCEUUID":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.ReferenceUUID?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.ReferenceUUID?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.ReferenceUUID?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;

                    case "REFERENCETYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.ReferenceType?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.ReferenceType?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.ReferenceType?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    case "WEBSITE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.Status?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.Status?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.Status?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    //case "KEY": //settings
                    //    if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                    //        input = input.Where(w => w.Key?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                    //    else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                    //        input = input.Where(w => w.Key?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                    //    else
                    //        input = input.Where(w => w.Key?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                    //    break;
                    //Node Properties
                    case "UUID":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.UUID?.ToUpper().Contains(screen.Value)).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.UUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.UUID == screen.Value).ToList();
                      break;
                  case "HOSTACCOUNTUUID":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.HostAccountUUID?.ToUpper().Contains(screen.Value)).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.HostAccountUUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.HostAccountUUID == screen.Value).ToList();
                      break;

                  //case "BODY":
                  //    if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                  //    {
                  //        string value = screen.Value?.ToUpper();
                  //        input = input.Where(w => string.IsNullOrWhiteSpace(w?.Body) == false && w?.Body?.ToUpper()?.Contains(value))?.ToList();
                  //    }
                  //    else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                  //        input = input.Where(w => w?.Body != screen.Value).ToList();
                  //    else
                  //        input = input.Where(w => w?.Body?.ToUpper()?.Trim() == screen.Value?.ToUpper()?.Trim()).ToList();
                  //    break;
                  case "VARIETYUUID":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.VarietyUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.VarietyUUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.VarietyUUID?.ToUpper()?.Trim() == screen.Value?.ToUpper()?.Trim()).ToList();
                      break;
                  case "UUIDTYPE":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.UUIDType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.UUIDType != screen.Value).ToList();
                      else
                          input = input.Where(w => w.UUIDType == screen.Value).ToList();
                      break;
                  case "UUPARENTID":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.UUParentID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.UUParentID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.UUParentID == screen.Value).ToList();
                      break;
                  case "UUPARENTIDTYPE":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.UUParentIDType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.UUParentIDType != screen.Value).ToList();
                      else
                          input = input.Where(w => w.UUParentIDType == screen.Value).ToList();
                      break;
                  case "NAME":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.Name?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Name?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                      else
                          input = input.Where(w => w.Name?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                      break;
                    case "DATATYPE":
                        if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                            input = input.Where(w => w.DataType?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                        else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                            input = input.Where(w => w.DataType?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                        else
                            input = input.Where(w => w.DataType?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                        break;
                    case "DESCRIPTION":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                      {
                          input = input.Where(w => string.IsNullOrEmpty(w.Description) == false &&
                                                   w.Description?.ToUpper()?.Contains(screen.Value.ToUpper().ToUpper())).ToList();
                      }
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Description?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                      else
                          input = input.Where(w => w.Description?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                      break;
                  case "STATUS":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.Status?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Status?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) == -1).ToList();
                      else
                          input = input.Where(w => w.Status?.IndexOf(screen.Value, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                      break;
                  case "ACCOUNTUUID":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.AccountUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.AccountUUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.AccountUUID == screen.Value).ToList();
                      break;
                  case "ACTIVE":

                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => Convert.ToBoolean(w.Active) != Convert.ToBoolean(screen.Value)).ToList();
                      else
                          input = input.Where(w => Convert.ToBoolean(w.Active) == Convert.ToBoolean(screen.Value)).ToList();
                      break;
                  case "ISDEFAULT":
                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => Convert.ToBoolean(w.IsDefault) != Convert.ToBoolean(screen.Value)).ToList();
                      else
                          input = input.Where(w => Convert.ToBoolean(w.IsDefault) == Convert.ToBoolean(screen.Value)).ToList();
                      break;
                  case "FAVORITE":
                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => Convert.ToBoolean(w.Favorite) != Convert.ToBoolean(screen.Value)).ToList();
                      else
                          input = input.Where(w => Convert.ToBoolean(w.Favorite) == Convert.ToBoolean(screen.Value)).ToList();
                      break;
                  case "BREEDER":
                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => Convert.ToBoolean(w.Breeder) != Convert.ToBoolean(screen.Value)).ToList();
                      else
                          input = input.Where(w => Convert.ToBoolean(w.Breeder) == Convert.ToBoolean(screen.Value)).ToList();
                      break;
                  case "DELETED":

                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => Convert.ToBoolean(w.Deleted) != Convert.ToBoolean(screen.Value)).ToList();
                      else
                          input = input.Where(w => Convert.ToBoolean(w?.Deleted) == Convert.ToBoolean(screen.Value)).ToList();
                      break;
                  //case "PRIVATE": //commented out since it's handled in roles  manager for now.
                  //   if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                  //        input = input.Where(w => Convert.ToBoolean(w.Private) != Convert.ToBoolean(screen.Value)).ToList();
                  //    else
                  //        input = input.Where(w => Convert.ToBoolean(w.Private) == Convert.ToBoolean(screen.Value)).ToList();
                  //    break;
                  case "SORTORDER":

                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.SortOrder != StringEx.ConvertTo<int>(screen.Value)).ToList();
                      else
                          input = input.Where(w => w.SortOrder == StringEx.ConvertTo<int>(screen.Value)).ToList();
                      break;
                  case "CREATEDBY":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.CreatedBy?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.CreatedBy != screen.Value).ToList();
                      else
                          input = input.Where(w => w.CreatedBy == screen.Value).ToList();
                      break;
                  case "DATECREATED":
                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => Convert.ToDateTime(w.DateCreated) != Convert.ToDateTime(screen.Value)).ToList();
                      else
                          input = input.Where(w => Convert.ToDateTime(w.DateCreated) == Convert.ToDateTime(screen.Value)).ToList();
                      break;
                  //Strain Specific
                  case "HARVESTTIME":
                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.HarvestTime != screen.Value).ToList();
                      else
                          input = input.Where(w => w.HarvestTime == StringEx.ConvertTo<int>(screen.Value)).ToList();
                      break;
                  //Category Specific
                  case "CATEGORYTYPE":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.CategoryType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.CategoryType?.ToUpper() != screen.Value).ToList();
                      else
                      {
                          input = input.Where(w => StringEx.EqualsEx(w.CategoryType, screen.Value))?.ToList();
                      }
                      break;
                    //Location and Finance Account
                    case "LOCATIONTYPE":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.LocationType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.LocationType?.ToString()?.ToUpper() != screen.Value).ToList();
                      else
                      {
                          input = input.Where(w => StringEx.EqualsEx(w.LocationType, screen.Value)).ToList();
                      }
                      break;
                  //Event/Product Specific
                  case "CATEGORY":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.Category?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Category != screen.Value).ToList();
                      else
                          input = input.Where(w => w.Category == screen.Value).ToList();

                      //TODO CategoryUUID is from products, we'll have to pass in a type to switch between fields
                      //if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                      //    input = input.Where(w => w.CategoryUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      //else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                      //    input = input.Where(w => w.CategoryUUID != screen.Value).ToList();
                      //else
                      //    input = input.Where(w => w.CategoryUUID == screen.Value).ToList();
                      break;
                  case "MANUFACTURER":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.ManufacturerUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.ManufacturerUUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.ManufacturerUUID == screen.Value).ToList();
                      break;
                  case "DEPARTMENT":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.DepartmentUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.DepartmentUUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.DepartmentUUID == screen.Value).ToList();
                      break;
                  case "GROWER":

                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Grower != screen.Value).ToList();
                      else
                          input = input.Where(w => w.Grower == true).ToList();
                      break;
                  case "DISPENSARY":

                      if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.Dispensary != screen.Value).ToList();
                      else
                          input = input.Where(w => w.Dispensary == true).ToList();
                      break;
                  case "BREEDERTYPE":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.BreederType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.BreederType?.ToUpper() != screen.Value).ToList();
                      else
                          input = input.Where(w => StringEx.EqualsEx(w.BreederType, screen.Value)).ToList();
                      break;
                  //Units Of Measure
                  case "CATEGORYUUID":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.CategoryUUID?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.CategoryUUID != screen.Value).ToList();
                      else
                          input = input.Where(w => w.CategoryUUID == screen.Value).ToList();
                      break;
                  case "GUUIDTYPE":
                      if (screen.Operator.EqualsIgnoreCase("CONTAINS"))
                          input = input.Where(w => w.GuuidType?.ToUpper().Contains(screen.Value.ToUpper())).ToList();
                      else if (screen.Operator.EqualsIgnoreCase("SEARCH!BY"))
                          input = input.Where(w => w.GuuidType?.ToUpper() != screen.Value).ToList();
                      else
                          input = input.Where(w => StringEx.EqualsEx(w.GuuidType, screen.Value)).ToList();
                      break;
              }
          }
          catch (Exception ex)
          {
              Debug.Assert(false, ex.Message);
          }
          return input;
      }

      public static IEnumerable<dynamic> GetBetween(this IEnumerable<dynamic> input, DataScreen screen)
      {
          if (input == null || input.Any() == false || string.IsNullOrWhiteSpace(screen.Value) || 
              string.IsNullOrWhiteSpace(screen.Field) || string.IsNullOrWhiteSpace(screen.Operator))
          {
                 return new List<dynamic>();
             }
          try
          {
              DateTime startDate = DateTime.Now;
              DateTime endDate = DateTime.Now;
              if (screen.Value.Contains("|"))
              {
                  string[] dates = screen.Value.Split('|');

                  if (!DateTime.TryParse(dates[0], out startDate))
                  {
                      Debug.Assert(false, "COULD NOT PARSE START DATE!");
                      return input.Cast<dynamic>();
                  }

                  if (!DateTime.TryParse(dates[1], out endDate))
                  {
                      Debug.Assert(false, "COULD NOT PARSE END DATE!");
                         return input.Cast<dynamic>();
                     }
              }
              else
              {
                  if (!DateTime.TryParse(screen.Value, out startDate))
                  {
                      Debug.Assert(false, "COULD NOT PARSE START DATE!");
                         return input.Cast<dynamic>();
                     }
              }

              switch (screen.Field?.ToUpper())
              {
                  case "DATECREATED":
                      if (screen.Operator == "INCLUSIVE")
                      {
                          input = input.Where(w => w.DateCreated != null && w.DateCreated >= startDate && endDate <= w.DateCreated).ToList();
                      }
                      else if (screen.Operator == "INCLUSIVE.END")
                      {
                          input = input.Where(w => w.DateCreated > startDate && endDate <= w.DateCreated).ToList();
                      }
                      else if (screen.Operator == "INCLUSIVE.START")
                      {
                          input = input.Where(w => w.DateCreated >= startDate && endDate < w.DateCreated).ToList();
                      }
                      else
                          input = input.Where(w => w.DateCreated > startDate && endDate < w.DateCreated).ToList();
                      break;
                  case "STARTDATE":
                      if (screen.Operator == "INCLUSIVE")
                      {
                          input = input.Where(w => w.StartDate != null && w.StartDate >= startDate && endDate <= w.StartDate).ToList();
                      }
                      else if (screen.Operator == "INCLUSIVE.END")
                      {
                          input = input.Where(w => w.StartDate > startDate && endDate <= w.StartDate).ToList();
                      }
                      else if (screen.Operator == "INCLUSIVE.START")
                      {
                          input = input.Where(w => w.StartDate >= startDate && endDate < w.StartDate).ToList();
                      }
                      else
                          input = input.Where(w => w.StartDate > startDate && endDate < w.StartDate).ToList();
                      break;
                  case "ENDDATE":
                      if (screen.Operator == "INCLUSIVE")
                      {
                          input = input.Where(w => w.EndDate != null && w.EndDate >= startDate && endDate <= w.EndDate).ToList();
                      }
                      else if (screen.Operator == "INCLUSIVE.END")
                      {
                          input = input.Where(w => w.EndDate > startDate && endDate <= w.EndDate).ToList();
                      }
                      else if (screen.Operator == "INCLUSIVE.START")
                      {
                          input = input.Where(w => w.EndDate >= startDate && endDate < w.EndDate).ToList();
                      }
                      else
                          input = input.Where(w => w.EndDate > startDate && endDate < w.EndDate).ToList();
                      break;
              }
          }
          catch (Exception ex)
          {
              Debug.Assert(false, ex.Message);
          }
          return input;
      }

      public static IEnumerable<dynamic> Paginate(this IEnumerable<dynamic> input, int? startIndex = 0, int? pageSize = 1)
      {
          if (startIndex == null || pageSize == null)
              return new List<dynamic>();

          List<dynamic> value = default(List<dynamic>);

          if (input == null || input.Any() == false)
          {
              return (List<dynamic>)value;
          }

          return input.Skip(startIndex ?? 0).Take(pageSize ?? 25);
      }

      public static IEnumerable<dynamic> Paginate(this IEnumerable<dynamic> input, int startIndex = 0, int pageSize = 1)
      {
          if (input == null || input.Any() == false)
          {
              return new List<dynamic>();
             }
          return input.Skip(startIndex).Take(pageSize);
      }

      public static IEnumerable<dynamic> PaginateByPage(this IEnumerable<dynamic> input, int page = 1, int pageSize = 1)
      {
             IEnumerable<dynamic> value = null;// default(IEnumerable<T>);

          if (input == null || input.Any() == false)
          {
                 return new List<dynamic>();                // return (List<T>)value;
             }
          if (input.Count() < pageSize)
              pageSize = input.Count();


          return input.Skip(page * pageSize).Take(pageSize);
      }

      /// <summary>
      /// This function is an aggregate search. It doesn't get applied
      /// from the filter function.
      /// 
      /// </summary>
      /// <param name="input"></param>
      /// <param name="filter"></param>
      /// <param name="filter.ResultCount"></param>
      /// <returns></returns>
      public static IEnumerable<dynamic> Search(this IEnumerable<dynamic> input,ref DataFilter filter)
      {

          filter.TotalRecordCount = 0;
          IEnumerable<dynamic> results =null;

          if (filter == null)
          {
              if (input != null)
                  filter.TotalRecordCount = input.Count();

              return input;
          }

          filter.Screens = filter.Screens.OrderBy(o => o.Order).ToList();

          foreach (DataScreen f in filter.Screens)
          {
              if (!input.Any() )
                  break;

              if (string.IsNullOrWhiteSpace(f.Command) || string.IsNullOrWhiteSpace(f.Field))
                  continue;

              switch (f.Command?.ToUpper())
              {
                  case "SEARCHBY":
                  case "SEARCH!BY":
                      results.Append(input.SearchBy(f) );//results are added to gether since we should be searching across multiple fields.
                      break;
              }
          }

          input = results.DistinctBy(d => d.UUID);//now remove duplicate items.
          filter.TotalRecordCount = input.Count();
          if (filter.PageResults)
          {
              var t = input.Paginate(filter.StartIndex, filter.PageSize);
              return t;
          }
          else
              return input;

          //todo make sure there's no data leaks for users under roles
          //this will need to be changed for store items and stuff(we wan't enought data to get out to show
          //product info.
          //todo the roleweight need to be defined, but for now if a basic user is logged in
          //they should be in RoleWeight 1 or 2. Roleweight 4 and above should start getting into admin range.
          //todo we can check the data's role weight and operator here also.
          ////if (filter.UserRoleWeight < 4)
          ////{
          ////    res = res.Select(s => new
          ////    {
          ////        Name = s.Name,
          ////        UUID = s.UUID
          ////    }).Cast<dynamic>().ToList();
          ////}

      }


  }

 


}
