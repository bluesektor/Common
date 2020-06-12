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
using GreenWerx.Models.Logging;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers
{
    public class EmailMessageManager : BaseManager, ICrud
    {
        public EmailMessageManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "EmailMessageManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
            this.SessionKey = sessionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            var s = (EmailMessage)n;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(_connectionKey))
                {
                    if (context.Delete<EmailMessage>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the EmailMessage from the table with all the data so when its updated it still contains the same data.
            res = this.Get(s.UUID);
            if (res.Code != 200)
                return res;

            s = (EmailMessage)res.Result;

            s.Deleted = true;
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                if (context.Update<EmailMessage>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                var res = context.GetAll<EmailMessage>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Email log not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        // status = actually make this json data <Location>.<sent/notsent>.<read>
        // make name = from userName
        public List<dynamic> GetEmailMessages(string userUUID, DataFilter filter, out int count)
        {
            if (filter != null)
            {
                foreach (var screen in filter.Screens)
                {
                    switch (screen.Field)
                    {
                        case "EMAILTO":
                        case "EMAILFROM":
                            screen.Value = this._requestingUser.Email;
                            break;
                    }
                }
            }

            count = -1;
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                var msgs = context.GetAll<EmailMessage>()
                                    ?.Where(sw => (sw.SendTo == userUUID || sw.SendFrom == userUUID)
                                        && (sw.Deleted == false || sw.Deleted == filter.IncludeDeleted)
                                    ).OrderByDescending(ob => ob.DateSent);
                count = msgs.Count();
                // var m  = msgs.Filter(filter, out count).Cast<dynamic>().ToList();
                var m = msgs.Filter(ref filter).Cast<dynamic>().ToList();
                return m;
            }
        }

        /// <summary>
        /// Validate param not implemented
        /// </summary>
        /// <param name="n"></param>
        /// <param name="validateFirst"></param>
        /// <returns></returns>
        public ServiceResult Insert(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            n.Initialize("", "", 5);

            var s = (EmailMessage)n;

            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                if (context.Insert<EmailMessage>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting EmailMessage ");
        }

        public List<EmailMessage> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<EmailMessage>();

            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                return context.GetAll<EmailMessage>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            string error = "";
            try
            {
                if (n == null)
                    return ServiceResponse.Error("Invalid EmailMessage data.");
                var s = (EmailMessage)n;
                using (var context = new GreenWerxDbContext(_connectionKey))
                {
                    if (context.Update<EmailMessage>(s) > 0)
                        return ServiceResponse.OK();
                }
            }
            catch (Exception ex)
            {
                error = ex.DeserializeException();
            }
            return ServiceResponse.Error("System error, EmailMessage was not updated.", error);
        }
    }
}