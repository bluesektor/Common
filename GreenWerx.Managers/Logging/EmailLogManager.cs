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
    public class EmailLogManager: ICrudi
    {
        private readonly string _dbConnectionKey;

        public EmailLogManager(string connectionKey)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "EmailLogManager CONTEXT IS NULL!");

        
             _dbConnectionKey = connectionKey;
    }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            var s = (EmailLog)n;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    if (context.Delete<EmailLog>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the EmailLog from the table with all the data so when its updated it still contains the same data.
             res = this.Get(s.UUID);
            if (res.Code != 200)
                return res;

            s = (EmailLog)res.Result;
       

            s.Deleted = true;
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                if (context.Update<EmailLog>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        public List<EmailLog> GetEmailLogs(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {

                return context.GetAll<EmailLog>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.DateSent).ToList();
            }
        }

        public List<EmailLog> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<EmailLog>();

            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                return context.GetAll<EmailLog>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }


        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                var res = context.GetAll<EmailLog>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Email log not found for uuid.");
                return ServiceResponse.OK("", res);
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

            var s = (EmailLog)n;
         
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                if (context.Insert<EmailLog>(s))
                    return ServiceResponse.OK("",s);
            }
            return ServiceResponse.Error("An error occurred inserting EmailLog " );
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid EmailLog data.");
            var s = (EmailLog)n;
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                if (context.Update<EmailLog>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, EmailLog was not updated.");
        }

    }
}
