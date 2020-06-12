// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
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
    public class ReminderManager : BaseManager, ICrud
    {
        public ReminderManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "ReminderManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Reminder)n;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<Reminder>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                    else
                        return ServiceResponse.OK();
                }
            }

            //get the Reminder from the table with all the data so when its updated it still contains the same data.
            res = this.Get(s.UUID);
            s = (Reminder)res.Result;
            if (s == null)
                return ServiceResponse.Error("Reminder not found");

            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Reminder>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        /// <summary>
        /// set the title if you want it updated on a soft delete, like 'this item as been deleted...'
        /// </summary>
        /// <param name="eventUUID"></param>
        /// <param name="title"></param>
        /// <param name="purge"></param>
        /// <returns></returns>
        public ServiceResult DeleteForEvent(string eventUUID, string title = "", bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var reminders = context.GetAll<Reminder>().Where(w => w.EventUUID == eventUUID && w.Deleted == false).ToList();

                foreach (var reminder in reminders)
                {
                    if (purge)
                    {
                        if (context.Delete<Reminder>(reminder) == 0)
                            return ServiceResponse.Error("Failed to delete reminder.");
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(title))
                            reminder.Name = title;

                        reminder.Deleted = true;
                        if (context.Update<Reminder>(reminder) == 0)
                            return ServiceResponse.Error("Failed to delete reminder.");
                    }
                }
            }
            return ServiceResponse.OK();
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Reminder>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Reminder not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public INode GetByEvent(string eventUUID)
        {
            if (string.IsNullOrWhiteSpace(eventUUID))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Reminder>()?.FirstOrDefault(sw => sw.EventUUID == eventUUID);
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<Reminder> GetReminders(string userUUID, string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Reminder>()?.Where(sw => (sw?.CreatedBy == userUUID && sw?.AccountUUID == accountUUID) && sw?.Deleted == deleted)?.OrderBy(ob => ob.Name)?.ToList();
            }
            //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Reminder)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.GetAll<Reminder>()?.FirstOrDefault() == null)
                {
                    if (context.Insert<Reminder>(s))
                        return ServiceResponse.OK("", s);
                    else
                        return ServiceResponse.Error("Failed to save reminder.");
                }
                Reminder dbU = context.GetAll<Reminder>()?.FirstOrDefault(wu =>
                (wu.EventUUID == s.EventUUID) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Reminder already exists.");

                if (context.Insert<Reminder>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Reminder " + s.Name);
        }

        //todo refactor into own class
        public ServiceResult Insert(ReminderRule rr)
        {
            rr.UUID = Guid.NewGuid().ToString("N");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ////if (!this.DataAccessAuthorized(rr, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

                if (context.Insert<ReminderRule>(rr))
                    return ServiceResponse.OK("", rr);
            }
            return ServiceResponse.Error("An error occurred inserting reminder rule " + rr.RangeStart.ToString() + " " + rr.RangeEnd.ToString());
        }

        public List<Reminder> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Reminder>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Reminder>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
            //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Reminder data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Reminder)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Reminder>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Reminder was not updated.");
        }
    }
}