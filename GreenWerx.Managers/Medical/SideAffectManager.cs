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

namespace GreenWerx.Managers.Medical
{
    public class SideAffectManager : BaseManager, ICrud
    {
        public SideAffectManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "SideAffectManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (SideAffect)n;
            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<SideAffect>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the SideAffect from the table with all the data so when its updated it still contains the same data.
            var res2 = this.Get(s.UUID);

            if (res2.Code != 200)
                return res2;

            s = (SideAffect)res2.Result;
            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<SideAffect>(s) == 0)
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
                var res = context.GetAll<SideAffect>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Side affect not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<SideAffect> GetSideAffects(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<SideAffect>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<SideAffect> GetSideAffects(string parentUUID, string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (string.IsNullOrWhiteSpace(parentUUID) || parentUUID == "0")
                    return context.GetAll<SideAffect>()?.Where(sw => (sw.UUParentID == "" || sw.UUParentID == null) && sw.AccountUUID == accountUUID && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();

                return context.GetAll<SideAffect>()?.Where(sw => sw.UUParentID == parentUUID && sw.AccountUUID == accountUUID && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<SideAffect> GetSideAffectsByDose(string doseUUID, string parentUUID, string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (string.IsNullOrWhiteSpace(parentUUID) || parentUUID == "0")
                    return context.GetAll<SideAffect>()?.Where(sw => sw.DoseUUID == doseUUID && (sw.UUParentID == "" || sw.UUParentID == null) && sw.AccountUUID == accountUUID && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();

                return context.GetAll<SideAffect>()?.Where(sw => sw.DoseUUID == doseUUID && sw.UUParentID == parentUUID && sw.AccountUUID == accountUUID && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (SideAffect)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                SideAffect dbU = context.GetAll<SideAffect>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(s.Name) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("SideAffect already exists.");

                if (context.Insert<SideAffect>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting SideAffect " + s.Name);
        }

        public List<SideAffect> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<SideAffect>();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<SideAffect>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid SideAffect data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (SideAffect)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<SideAffect>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, SideAffect was not updated.");
        }
    }
}