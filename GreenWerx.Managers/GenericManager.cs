using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Models;
using GreenWerx.Models.App;

namespace GreenWerx.Managers
{
    public class GenericManager : BaseManager
    {
        private SystemLogger _logger = null;

        public GenericManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "GenericManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
            _logger = new SystemLogger(_connectionKey);
        }

        public ServiceResult Delete(string type, string uuid, string accountUUID, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            INode n = this.GetItem(type, uuid, accountUUID);

            if (n == null)
                return ServiceResponse.Error("Item not found.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (purge && context.Delete(n) == 0)
                {
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
                }

                n.Deleted = true;
                if (context.Update(n) == 0)
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
            }
            return res;
        }

        public List<dynamic> GetData(string type)
        {
            List<dynamic> dataset = new List<dynamic>();

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    dataset = context.GetAllOf(type).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                _logger.InsertError(ex.Message, "GenericManager", "GetData:" + type);
            }

            return dataset;
        }

        public INode GetItem(string type, string uuid, string accountUUID)
        {
            INode item = null;
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    item = context.GetAllOf(type).FirstOrDefault(w => w.UUID == uuid && w.AccountUUID == accountUUID);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                _logger.InsertError(ex.Message, "GenericManager", "GetItem:" + type);
            }

            return item;
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid  data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<INode>(n) > 0)
                    return ServiceResponse.OK("", n);
            }
            return ServiceResponse.Error("System error, Item was not updated.");
        }
    }
}