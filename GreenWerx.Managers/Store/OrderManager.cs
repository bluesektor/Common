﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Store;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Store
{
    public class OrderManager : BaseManager, ICrud
    {
        private readonly SystemLogger _logger = null;

        public OrderManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "OrderManager CONTEXT IS NULL!");

            SessionKey = sessionKey;
            this._connectionKey = connectionKey;
            _logger = new SystemLogger(connectionKey);
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Order data.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Order)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (purge)
                {
                    if (context.Delete<Order>(s) > 0)
                        return ServiceResponse.OK();
                    else
                        return ServiceResponse.Error("Failed to delete the order.");
                }

                //get the Order from the table with all the data so when its updated it still contains the same data.
                var res = this.Get(s.UUID);
                if (res.Code != 200)
                    return res;
                s = (Order)res.Result;

                s.Deleted = true;
                if (context.Update<Order>(s) > 0)
                    return ServiceResponse.OK();
                else
                    return ServiceResponse.Error("Failed to delete the order.");
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;

            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Order>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Order not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<Order> GetOrders(string accountUUID, bool deleted = false)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Order>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
        }

        public ServiceResult Insert(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            if (n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = this._requestingUser?.AccountUUID;

            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Order)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Order dbU = context.GetAll<Order>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(s.Name));

                if (dbU != null)
                    return ServiceResponse.Error("Order already exists.");

                if (context.Insert<Order>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Order " + s.Name);
        }

        public List<Order> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Order>();

            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Order>()?.Where(w => w.Name.EqualsIgnoreCase(name) && w.AccountUUID == _requestingUser?.AccountUUID).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Order data.");

            var s = (Order)n;

            if (!this.DataAccessAuthorized(s, "UPDATE", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Order>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Order was not updated.");
        }
    }
}