// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Managers;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Models.Medical
{
    public class AnatomyManager : BaseManager, ICrud
    {
        public AnatomyManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "AnatomyManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "delete", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Anatomy)n;
            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<Anatomy>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the Anatomy from the table with all the data so when its updated it still contains the same data.
            res = this.Get(s.UUID);
            if (res.Code != 200)
                return res;

            s = (Anatomy)res.Result;
            if (s == null)
                return ServiceResponse.Error("Anatomy not found");

            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Anatomy>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        public int Delete(AnatomyTag s, bool purge = false)
        {
            if (s == null)
                return 0;

            if (!this.DataAccessAuthorized(s, "DELETE", false)) return 0;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.Delete<AnatomyTag>(s);
                }
            }

            //get the AnatomyTag from the table with all the data so when its updated it still contains the same data.
            s = this.GetAnatomyTagBy(s.UUID);
            if (s == null)
                return 0;
            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.Update<AnatomyTag>(s);
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Anatomy>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Anaomy not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public AnatomyTag GetAllAnatomyTag(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<AnatomyTag>()?.FirstOrDefault(sw => sw.Name.EqualsIgnoreCase(name));
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<Anatomy> GetAnatomies(string accountUUID, ref DataFilter filter)
        {
            var deleted = filter.IncludeDeleted;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Anatomy>(ref filter)?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public AnatomyTag GetAnatomyTagBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<AnatomyTag>()?.FirstOrDefault(sw => sw.UUID == uuid);
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<AnatomyTag> GetAnatomyTags(string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<AnatomyTag>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Anatomy)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Anatomy dbU = context.GetAll<Anatomy>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(s.Name) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Anatomy already exists.");

                if (context.Insert<Anatomy>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Anatomy " + s.Name);
        }

        public ServiceResult Insert(AnatomyTag s)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                AnatomyTag dbU = context.GetAll<AnatomyTag>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(s.Name) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("AnatomyTag already exists.");

                s.UUID = Guid.NewGuid().ToString("N");
                s.UUIDType = "AnatomyTag";

                if (!this.DataAccessAuthorized(s, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

                if (context.Insert<AnatomyTag>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting AnatomyTag " + s.Name);
        }

        public List<Anatomy> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Anatomy>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Anatomy>()?.Where(sw => sw.Name.EqualsIgnoreCase(name) && sw.AccountUUID == this._requestingUser.AccountUUID).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Anatomy data.");

            if (!this.DataAccessAuthorized(n, "patch", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Anatomy)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Anatomy>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Anatomy was not updated.");
        }

        public ServiceResult Update(AnatomyTag s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid AnatomyTag data.");

            if (!this.DataAccessAuthorized(s, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<AnatomyTag>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, AnatomyTag was not updated.");
        }
    }
}