// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Logging;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Logging
{
    public class AffliateManager : BaseManager, ICrud
    {
        private SystemLogger _logger = null;

        public AffliateManager(string connectionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "LoggingManager CONTEXT IS NULL!");

            _connectionKey = connectionKey;
            _logger = new SystemLogger(_connectionKey);
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            throw new NotImplementedException();
        }

        public ServiceResult Get(string uuid)
        {
            throw new NotImplementedException();
        }

        public List<dynamic> GetAllAccessLogs(ref DataFilter filter)
        {
            List<dynamic> acts;
            if (filter.IncludePrivate)
            { //if looking at private then check permission
                if (!this.DataAccessAuthorized("ACCESSLOG", false, filter.IncludePrivate))
                {
                    filter.IncludePrivate = false;
                    // if not allowed private then check if they can see anything..
                    if (!this.DataAccessAuthorized("ACCESSLOG", false, filter.IncludePrivate))
                        return new List<dynamic>();
                }
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<AccessLog>().OrderBy(o => o.DateCreated).Cast<dynamic>().Filter(ref filter).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }

        public List<dynamic> GetAllAffiliateLogs(DataFilter filter, out int count)
        {
            count = 0;
            List<dynamic> acts;
            if (filter.IncludePrivate)
            { //if looking at private then check permission
                if (!this.DataAccessAuthorized("ACCESSLOG", false, filter.IncludePrivate))
                {
                    filter.IncludePrivate = false;
                    // if not allowed private then check if they can see anything..
                    if (!this.DataAccessAuthorized("ACCESSLOG", false, filter.IncludePrivate))
                        return new List<dynamic>();
                }
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<AffiliateLog>().OrderBy(o => o.DateCreated).Cast<dynamic>().Filter(ref filter).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }
        public List<dynamic> GetAllRequestLogs(ref DataFilter filter)
        {
            List<dynamic> acts;
            if (filter.IncludePrivate)
            { //if looking at private then check permission
                if (!this.DataAccessAuthorized("REQUESTLOG", false, filter.IncludePrivate))
                {
                    filter.IncludePrivate = false;
                    // if not allowed private then check if they can see anything..
                    if (!this.DataAccessAuthorized("REQUESTLOG", false, filter.IncludePrivate))
                        return new List<dynamic>();
                }
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<RequestLog>().OrderBy(o => o.DateCreated).Cast<dynamic>().Filter(ref filter).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }

        public List<dynamic> GetAllSystemLogs(ref DataFilter filter)
        {
            List<dynamic> acts;
            if (filter.IncludePrivate)
            { //if looking at private then check permission
                if (!this.DataAccessAuthorized("LOGENTRY", false, filter.IncludePrivate))
                {
                    filter.IncludePrivate = false;
                    // if not allowed private then check if they can see anything..
                    if (!this.DataAccessAuthorized("LOGENTRY", false, filter.IncludePrivate))
                        return new List<dynamic>();
                }
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<LogEntry>().OrderBy(o => o.LogDate).Cast<dynamic>().Filter(ref filter).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }

        public ServiceResult Insert(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid log data.");

            // if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser?.UUID, this._requestingUser?.AccountUUID, this._requestingUser?.RoleWeight ?? 100);

            var a = (AffiliateLog)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<AffiliateLog>(a))
                    return ServiceResponse.OK("", a);
            }
            return ServiceResponse.Error("System error, log was not added.");
        }

        public ServiceResult Update(INode n)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var af = (AffiliateLog)n;
                if (context.Update<AffiliateLog>(af) <= 0)
                    return ServiceResponse.Error();

                return ServiceResponse.OK();
            }
        }
    }
}