// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers
{
    public class UnitOfMeasureManager : BaseManager, ICrud
    {
        private readonly SystemLogger _logger;

        public UnitOfMeasureManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "UnitOfMeasureManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;

            _logger = new SystemLogger(this._connectionKey);
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            var s = (UnitOfMeasure)n;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (purge)
                {
                    if (context.Delete<UnitOfMeasure>(s) > 0)
                        return ServiceResponse.OK();

                    return ServiceResponse.Error("Failed to purge record.");
                }

                //get the UnitOfMeasure from the table with all the data so when its updated it still contains the same data.
                var res = this.Get(s.UUID);
                if (res.Code != 200)
                    return res;
                s = (UnitOfMeasure)res.Result;

                s.Deleted = true;
                if (context.Update<UnitOfMeasure>(s) > 0)
                    return ServiceResponse.OK();

                return ServiceResponse.Error("Failed to delete record.");
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("ID was not sent.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<UnitOfMeasure>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Measure not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<UnitOfMeasure> GetUnitsOfMeasure(string accountUUID, ref DataFilter filter)
        {
            bool includeDelete = filter.IncludeDeleted;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<UnitOfMeasure>(ref filter)
                    ?.Where(sw => (sw.AccountUUID == accountUUID)
                        && sw.Deleted == includeDelete).OrderBy(ob => ob.Name).ToList();
            }
        }

        public List<UnitOfMeasure> GetUnitsOfMeasure(string accountUUID, string category, ref DataFilter filter)
        {
            bool includeDelete = filter.IncludeDeleted;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<UnitOfMeasure>(ref filter)?.
                    Where(sw => (sw.AccountUUID == accountUUID)
                            && (sw.Category?.EqualsIgnoreCase(category) ?? false) && sw.Deleted == includeDelete).OrderBy(ob => ob.Name).ToList();
            }
        }

        public ServiceResult Insert(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Value is empty.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (UnitOfMeasure)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                UnitOfMeasure dbU = context.GetAll<UnitOfMeasure>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("UnitOfMeasure already exists.");

                if (context.Insert<UnitOfMeasure>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting UnitOfMeasure " + s.Name);
        }

        public List<UnitOfMeasure> Search(string name, ref DataFilter filter)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<UnitOfMeasure>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<UnitOfMeasure>(ref filter)?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid UnitOfMeasure data.");

            var s = (UnitOfMeasure)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<UnitOfMeasure>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, UnitOfMeasure was not updated.");
        }

        private UnitOfMeasure FindUnitOfMeasure(string uuid, string name, string AccountUUID, ref DataFilter filter)
        {
            UnitOfMeasure res = null;

            if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(name) == false)
                res = this.Search(name, ref filter)?.FirstOrDefault();
            else
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    res = context.GetAll<UnitOfMeasure>(ref filter)?.FirstOrDefault(w => w.UUID == uuid || (w.Name?.EqualsIgnoreCase(name) ?? false));
                }
            }
            if (res == null)
                return res;

            if (filter.IncludeSystemAccount && res.AccountUUID == AccountUUID)
                return res;

            if (res.AccountUUID == AccountUUID)
                return res;

            return null;
        }
    }
}