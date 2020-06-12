// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Events
{
    public class NotificationManager : BaseManager, ICrud
    {
        public NotificationManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "NotificationManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "delete", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Notification)n;
            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<Notification>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the Notification from the table with all the data so when its updated it still contains the same data.
            var res2 = this.Get(s.UUID);
            if (res2.Code != 200)
                return res2;
            s = (Notification)res2.Result;

            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Notification>(s) == 0)
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
                var res = context.GetAll<Notification>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Notification not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<Notification> GetNotifications(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Notification>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Notification)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Notification dbU = context.GetAll<Notification>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(s.Name) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Notification already exists.");

                if (context.Insert<Notification>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Notification " + s.Name);
        }

        public List<Notification> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Notification>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Notification>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Notification data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Notification)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Notification>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Notification was not updated.");
        }
    }
}