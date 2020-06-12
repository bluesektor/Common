using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Tools;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Tools
{
    public class StageDataManager : BaseManager
    {
        public StageDataManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "StageDataManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (purge && context.Delete<StageData>((StageData)n) == 0)
                {
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
                }
                //get the stage data from the table with all the data so when its updated it still contains the same data.
                res = this.Get(n.UUID);
                if (res.Code != 200)
                    return res;

                n.Deleted = true;
                if (context.Update<StageData>((StageData)n) == 0)
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
            }
            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<StageData>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("StageData not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<dynamic> GetAll(ref DataFilter filter)
        {
            List<dynamic> acts;
            if (filter.IncludePrivate)
            { //if looking at private then check permission
                if (!this.DataAccessAuthorized("STAGEDATA", false, filter.IncludePrivate))
                {
                    filter.IncludePrivate = false;
                    // if not allowed private then check if they can see anything..
                    if (!this.DataAccessAuthorized("STAGEDATA", false, filter.IncludePrivate))
                        return new List<dynamic>();
                }
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<StageData>().OrderBy(o => o.SyncDate).Cast<dynamic>().Filter(ref filter).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }

        public List<string> GetDataTypes()
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var dataTypes = context.GetAll<StageData>().GroupBy(x => x.DataType).Select(group => group.First())?.ToList();
                return dataTypes.Select(x => x.DataType).ToList();
            }
       
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (StageData)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<StageData>((StageData)s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting stage data.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid StageData data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<StageData>((StageData)n) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, StageData was not updated.");
        }

    }
}