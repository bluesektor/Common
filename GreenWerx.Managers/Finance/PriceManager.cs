﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Finance;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Finance
{
    public class PriceManager : BaseManager, ICrud
    {
       
        private readonly SystemLogger _logger;

        public PriceManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "PriceRuleManager CONTEXT IS NULL!");

            SessionKey = sessionKey;
            this._connectionKey = connectionKey;

            _logger = new SystemLogger(connectionKey);


        }
        public ServiceResult Delete(INode n, bool purge = false) //TODO check if finance account references this currency. if so then return error.
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var p = (PriceRule)n;
            try
            {
                
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (purge)
                    {
                        DynamicParameters parameters = new DynamicParameters();
                        parameters.Add("@UUID", p.UUID);
                        if (context.Delete<PriceRule>("WHERE UUID=@UUID", parameters) == 0)
                            return ServiceResponse.Error(p.Name + " failed to delete. ");
                    }
                    else {
                        p.Deleted = true;
                        if (context.Update<PriceRule>(p) == 0)
                            return ServiceResponse.Error(p.Name + " failed to delete. ");
                    }
                }
                ////SQLITE
                ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
                ////object[] paramters = new object[] { rp.PermissionUUID , rp.RoleUUID ,rp.AccountUUID };
                ////context.Delete<RolePermission>("WHERE PermissionUUID=? AND RoleUUID=? AND AccountUUID=?", paramters);
                ////  context.Delete<RolePermission>(rp);
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "ItemManager", "DeleteItem");
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }

            return res;
        }

        public List<PriceRule> GetAccountPriceRule(string accountUUID)
        {
            ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (string.IsNullOrWhiteSpace(accountUUID))
                    return context.GetAll<PriceRule>().ToList();

                return context.GetAll<PriceRule>().Where(pw => pw.AccountUUID == accountUUID).ToList();
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                var res = context.GetAll<PriceRule>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Price rule not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<PriceRule> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<PriceRule>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context.GetAll<PriceRule>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }

        public PriceRule GetPriceRuleByCode(string PriceRuleCode)
        {
            if (string.IsNullOrWhiteSpace(PriceRuleCode))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context.GetAll<PriceRule>()?.FirstOrDefault(sw => sw.Code.EqualsIgnoreCase(PriceRuleCode));
            }
        }
        public List<PriceRule> GetPriceRules(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                //todo check if asset class is returned if so delete the line below.
                return context.GetAll<PriceRule>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
        }

        public List<PriceRuleLog> GetPriceRules(string trackingId, string trackingType, bool isDeleted = false)
        {
          
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {

                    return context.GetAll<PriceRuleLog>()?.Where(sw => (sw.TrackingId == trackingId && (sw.TrackingType?.EqualsIgnoreCase(trackingType) ?? false)) && sw.Deleted == isDeleted).OrderBy(ob => ob.Name).ToList();
                }
          
        }

        public List<PriceRule> GetAll()
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context.GetAll<PriceRule>().ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");
            var p = (PriceRule)n;
            ServiceResult res = ServiceResponse.OK();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {

                if (context.Update<PriceRule>(p) == 0)
                    return ServiceResponse.Error(p.Name + " failed to update. ");
            }
            return res;

        }


        /// <summary>
        /// This was created for use in the bulk process..
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="checkName">This will check the products by name to see if they exist already. If it does an error message will be returned.</param>
        /// <returns></returns>
        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var p = (PriceRule)n;

            
                if (string.IsNullOrWhiteSpace(p.CreatedBy))
                    return ServiceResponse.Error("You must assign who the product was created by.");

                if (string.IsNullOrWhiteSpace(p.AccountUUID))
                    return ServiceResponse.Error("The account id is empty.");
            

            if ( p.Expires == DateTime.MinValue)
                p.Expires = DateTime.UtcNow.AddYears(200);

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<PriceRule>(p))
                    return ServiceResponse.OK("", p);
            }
            return ServiceResponse.Error("An error occurred inserting product " + p.Name);
        }


      
    }
}
