// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Utilites.Extensions;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.Managers.General
{
    public class AttributeManager : BaseManager, ICrud
    {
        public AttributeManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "AttributeManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "delete", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (TMG.Attribute)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Delete<TMG.Attribute>(s) == 0)
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
                var res = context.GetAll<TMG.Attribute>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Attribute not found for uuid. " + uuid);
                return ServiceResponse.OK("", res);
            }
        }

        public List<TMG.Attribute> GetAttributes(string referenceUUID, string referenceType, string accountUUID)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<TMG.Attribute>()?.Where(sw => (sw.AccountUUID == accountUUID &&
                                                                        sw.ReferenceUUID == referenceUUID &&
                                                                        (sw.ReferenceType?.EqualsIgnoreCase(referenceType) ?? false)))
                                                                        .OrderBy(ob => ob.Name).ToList();
            }
        }

        public List<TMG.Attribute> GetAttributes(string accountUUID, ref DataFilter filter)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                //if(filter == null || filter.FilterByAccount == true)
                //    return context.GetAll<TMG.Attribute>()?.Where(sw => (sw.AccountUUID == accountUUID)).OrderBy(ob => ob.SortOrder).ToList();

                //if(filter.FilterByAccount == false && _requestingUser.RoleWeight < 90)// not authorized to get all data
                //    return context.GetAll<TMG.Attribute>()?.Where(sw => (sw.AccountUUID == accountUUID)).OrderBy(ob => ob.SortOrder).ToList();

                //if (filter.FilterByAccount == false)
                return context.GetAll<TMG.Attribute>()?.OrderBy(ob => ob.SortOrder).ToList();

                //return context.GetAll<TMG.Attribute>()?.Where(sw => (sw.AccountUUID == accountUUID)).OrderBy(ob => ob.SortOrder).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (TMG.Attribute)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.GetAll<TMG.Attribute>().Any(a => a.UUID == s.UUID))
                {
                    return this.Update(s);
                }

                if (context.Insert<TMG.Attribute>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Attribute " + s.Status);
        }

        public List<TMG.Attribute> Search(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return new List<Models.General.Attribute>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<TMG.Attribute>()?.Where(sw => sw.Status.EqualsIgnoreCase(status)).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Attribute data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(_requestingUser.UUID, _requestingUser.AccountUUID, _requestingUser.RoleWeight);

            var s = (TMG.Attribute)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<TMG.Attribute>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Attribute was not updated.");
        }
    }
}