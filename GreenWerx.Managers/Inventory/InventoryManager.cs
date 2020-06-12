// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Managers.Geo;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.General;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Inventory;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Inventory
{
    public class InventoryManager : BaseManager, ICrud
    {
        private readonly PostalCodeManager _locationManager;
        private readonly SystemLogger _logger;

        public InventoryManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "InventoryManager CONTEXT IS NULL!");

            this._locationManager = new PostalCodeManager(connectionKey, sessionKey);
            this._connectionKey = connectionKey;

            _logger = new SystemLogger(connectionKey);
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var p = (InventoryItem)n;
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@PRODUCTUUID", p.UUID);
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (purge)
                    {
                        if (context.Delete<InventoryItem>("WHERE UUID=@PRODUCTUUID", parameters) == 0)
                            return ServiceResponse.Error(p.Name + " failed to delete. ");
                    }
                    else
                    {
                        p.Deleted = true;
                        if (context.Update<InventoryItem>(p) == 0)
                            return ServiceResponse.Error(p.Name + " failed to delete. ");
                    }
                }
                ////SQLITE
                ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
                ////object[] paramters = new object[] { rp.PermissionUUID , rp.RoleUUID ,rp.AccountUUID };
                ////context.Delete<RolePermission>("WHERE PermissionUUID=? AND RoleUUID=? AND AccountUUID=?", paramters);
                ////  context.Delete<RolePermission>(rp);
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "ItemManager", "DeleteItem");
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }

            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("No uuid sent.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                var res = context.GetAll<InventoryItem>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("No item found.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<InventoryItem> GetAccountItems(string accountUUID)
        {
            ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (string.IsNullOrWhiteSpace(accountUUID))
                    return context.GetAll<InventoryItem>().ToList();

                return context.GetAll<InventoryItem>()?.Where(pw => pw.AccountUUID == accountUUID).ToList();
            }
        }

        public ServiceResult GetItemDetails(string uuid)
        {
            var res = this.Get(uuid);
            if (res.Code != 200)
                return res;
            var item = res.Result as InventoryItem;
            if (item == null)
                return ServiceResponse.Error("Inventory Item could not be located for the uuid " + uuid);

            if (item.RoleWeight > 0 && this._requestingUser == null)
                return ServiceResponse.Error("You are not authorized this content.");

            if (item.RoleWeight > this._requestingUser?.RoleWeight)
                return ServiceResponse.Error("You are not authorized this content.");

            string locationUUID = item.LocationUUID;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {   ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                //LocationName
                item.LocationUUID = context.GetAll<Location>()?.FirstOrDefault(w => w.UUID == locationUUID)?.Name;
                Location parentLocation = context.GetAll<Location>()?.FirstOrDefault(w => w.UUParentID == locationUUID);
                if (parentLocation != null)
                {
                    item.LocationUUID = parentLocation.Name + "->" + item.LocationUUID;
                    locationUUID = parentLocation.UUID;
                }
                parentLocation = context.GetAll<Location>()?.FirstOrDefault(w => w.UUParentID == locationUUID);
                if (parentLocation != null)
                {
                    item.LocationUUID = parentLocation.Name + "->" + item.LocationUUID;
                    locationUUID = parentLocation.UUID;
                }

                //CategoryName
                item.CategoryUUID = context.GetAll<Category>()?.FirstOrDefault(w => w.UUID == item.CategoryUUID)?.Name;

                //UserName
                item.CreatedBy = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == item.CreatedBy)?.Name;
            }
            return ServiceResponse.OK("", item);
        }

        public List<InventoryItem> GetItems(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

                List<InventoryItem> items = context.GetAll<InventoryItem>()
                                    .Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();

                items.ForEach(x =>
               {
                   x.WeightUOM = context.GetAll<UnitOfMeasure>()?.FirstOrDefault(w => w.UUID == x.UOMUUID)?.Name;
               });

                ////todo reimplement this after making sure the unit of measure id is set.
                ////return context.GetAll<InventoryItem>()
                ////                 .Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted)
                ////                 .Join(context.GetAll<UnitOfMeasure>(),
                ////                 ii => ii?.UOMUUID,
                ////                 uom => uom?.UUID,
                ////                 (ii, uom) =>{
                ////                     ii.WeightUOM = uom.Name;
                ////                     return ii;
                ////                 })
                ////                 .OrderBy(ob => ob.Name).ToList();

                return items;
            }
        }

        public List<InventoryItem> GetItems(string locationName, int distance, bool deleted = false, bool published = true)
        {
            List<InventoryItem> items;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {   ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                if (string.IsNullOrWhiteSpace(locationName))
                {
                    items = context.GetAll<InventoryItem>()?.Where(w => w.Deleted == deleted && w.Published == published)
                        .OrderBy(o => o.DateCreated)
                        .ToList();
                }
                else
                {
                    //get zips in area
                    GeoCoordinate zips = _locationManager.GetLocationsIn(locationName, distance);

                    items = context.GetAll<InventoryItem>()?.Where(w => w.Deleted == deleted && w.Published == published)
                            .Join(zips.Distances,
                                    item => item.LocationUUID,
                                    zip => zip.UUID,
                                    (item, zip) => new { item, zip }).Select(s => s.item)
                            .ToList();
                }

                return items;
            }
        }

        public List<InventoryItem> GetPublishedItems(bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

                List<InventoryItem> items = context.GetAll<InventoryItem>()
                                    .Where(sw => (sw.Published == true) && sw.Deleted == deleted).ToList();

                //items.ForEach(x =>
                //{
                //    x.WeightUOM = context.GetAll<UnitOfMeasure>()?.FirstOrDefault(w => w.UUID == x.UOMUUID)?.Name;
                //});

                ////todo reimplement this after making sure the unit of measure id is set.
                ////return context.GetAll<InventoryItem>()
                ////                 .Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted)
                ////                 .Join(context.GetAll<UnitOfMeasure>(),
                ////                 ii => ii?.UOMUUID,
                ////                 uom => uom?.UUID,
                ////                 (ii, uom) =>{
                ////                     ii.WeightUOM = uom.Name;
                ////                     return ii;
                ////                 })
                ////                 .OrderBy(ob => ob.Name).ToList();

                return items;
            }
        }

        public List<InventoryItem> GetUserItems(string accountUUID, string userUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                if (string.IsNullOrWhiteSpace(accountUUID))
                    return context.GetAll<InventoryItem>().ToList();

                return context.GetAll<InventoryItem>()?.Where(pw =>
                        pw.AccountUUID == accountUUID
                        && pw.CreatedBy == userUUID
                        && pw.Deleted == deleted).ToList();
            }
        }

        /// <summary>
        /// This was created for use in the bulk process..
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="checkName">This will check the products by name to see if they exist already. If it does an error message will be returned.</param>
        /// <returns></returns>
        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var p = (InventoryItem)n;

            if (string.IsNullOrWhiteSpace(p.CreatedBy))
                return ServiceResponse.Error("You must assign who the product was created by.");

            if (string.IsNullOrWhiteSpace(p.AccountUUID))
                return ServiceResponse.Error("The account id is empty.");

            if (!p.LocationType.EqualsIgnoreCase("coordinate"))
            {
                //prioritize coordinate location so we can get distance.
                var location = _locationManager.Search(p.LocationUUID, "coordinate")?.FirstOrDefault();
                if (location == null)
                {
                    location = _locationManager.Search(p.LocationUUID)?.FirstOrDefault();
                    //todo if not coordinate and not a user group get long lat from an api
                }

                if (location != null)
                {
                    p.LocationUUID = location.UUID;
                    p.LocationType = location.LocationType;
                }
            }

            p.ItemDate = DateTime.UtcNow;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<InventoryItem>(p))
                    return ServiceResponse.OK("", p);
            }
            return ServiceResponse.Error("An error occurred inserting product " + p.Name);
        }

        public List<InventoryItem> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<InventoryItem>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context.GetAll<InventoryItem>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var p = (InventoryItem)n;

            if (p.ItemDate == DateTime.MinValue)
                p.ItemDate = DateTime.UtcNow;

            if (p.DateCreated == DateTime.MinValue)
                p.DateCreated = DateTime.UtcNow;

            if (!p.LocationType.EqualsIgnoreCase("coordinate"))
            {
                //prioritize coordinate location so we can get distance.
                var location = _locationManager.Search(p.LocationUUID, "coordinate")?.FirstOrDefault();
                if (location == null)
                {
                    location = _locationManager.Search(p.LocationUUID)?.FirstOrDefault();
                    //todo if not coordinate and not a user group get long lat from an api
                }

                if (location != null)
                {
                    p.LocationUUID = location.UUID;
                    p.LocationType = location.LocationType;
                }
            }

            ServiceResult res = ServiceResponse.OK();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<InventoryItem>(p) == 0)
                    return ServiceResponse.Error(p.Name + " failed to update. ");
            }
            return res;
        }
    }
}