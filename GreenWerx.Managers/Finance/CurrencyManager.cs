// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
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
using GreenWerx.Models.Finance;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Finance
{
    public class CurrencyManager : BaseManager, ICrud
    {
        private readonly SystemLogger _logger;

        public CurrencyManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "InventoryManager CONTEXT IS NULL!");


            this._connectionKey = connectionKey;

            _logger = new SystemLogger(connectionKey);

  
        }

        public ServiceResult Delete(INode n, bool purge = false) //TODO check if finance account references this currency. if so then return error.
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var p = (Currency)n;

            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UUID", p.UUID);
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<Currency>("WHERE UUID=@UUID", parameters) == 0)
                        return ServiceResponse.Error(p.Name + " failed to delete. ");
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

        public List<Currency> GetAccountCurrency(string accountUUID)
        {
            ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (string.IsNullOrWhiteSpace(accountUUID))
                    return context. GetAll<Currency>(  ).ToList();

                return context. GetAll<Currency>(  )?.Where(pw => pw.AccountUUID == accountUUID).ToList();
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                var res = context. GetAll<Currency>( )?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Currency not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<Currency> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Currency>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context. GetAll<Currency>( )?.Where(sw => (sw.Name?.Trim()?.EqualsIgnoreCase(name?.Trim()) ?? false)).ToList();
            }
        }

        public List<Currency> GetCurrencies(string accountUUID, bool deleted = false, bool inlcudeSystemDefault = false )
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                //tod check if asset class is returned if so delete the line below.

                if(inlcudeSystemDefault)
                    return context. GetAll<Currency>(  )?.Where(sw => (sw.AccountUUID == accountUUID || sw.AccountUUID == SystemFlag.Default.Account) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();

                return context. GetAll<Currency>(  )?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
        }

        public List<Currency> GetAll()
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(dbP, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context. GetAll<Currency>(  ).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var p = (Currency)n;

            ServiceResult res = ServiceResponse.OK();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {

                if (context.Update<Currency>(p) == 0)
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

            var p = (Currency)n;

            if (string.IsNullOrWhiteSpace(p.CreatedBy))
                return ServiceResponse.Error("You must assign who the product was created by.");

            if (string.IsNullOrWhiteSpace(p.AccountUUID))
                return ServiceResponse.Error("The account id is empty.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<Currency>(p))
                    return ServiceResponse.OK("", p);
            }
            return ServiceResponse.Error("An error occurred inserting product " + p.Name);
        }
    }
}
