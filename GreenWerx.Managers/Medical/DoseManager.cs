// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Medical;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers
{
    public class DoseManager : BaseManager, ICrud
    {
        public DoseManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "DoseManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (DoseLog)n;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<DoseLog>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the Dose from the table with all the data so when its updated it still contains the same data.
            res = this.Get(s.UUID);
            if (res.Code != 200)
                return res;
            s = (DoseLog)res.Result;
            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<DoseLog>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<DoseLog>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Dose log not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<DoseLog> GetDoses(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<DoseLog>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name)?.ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (DoseLog)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                DoseLog dbU = context.GetAll<DoseLog>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Dose already exists.");

                if (context.Insert<DoseLog>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Dose " + s.Name);
        }

        public List<DoseLog> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<DoseLog>();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<DoseLog>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Dose data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (DoseLog)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<DoseLog>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Dose was not updated.");
        }
    }
}