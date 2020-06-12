// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.General;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.General
{
    public class CategoryManager : BaseManager, ICrud
    {
        public CategoryManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "CategoryManager CONTEXT IS NULL!");

            
                 this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Category)n;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<Category>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                }
            }

            //get the Category from the table with all the data so when its updated it still contains the same data.
             res = this.Get(s.UUID);
            if (res.Code != 200)
                return res;

            s = (Category)res.Result;
            if (s == null)
                return ServiceResponse.Error("Symptom not found");
            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Category>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        public List<Category> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Category>();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Category>()?.Where(w => (w.Name?.EqualsIgnoreCase(name) ?? false)  && w.AccountUUID == this._requestingUser.AccountUUID).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }


        public INode GetCategory(string name, string categoryType, string AccountUUID)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Category>()?.FirstOrDefault(w =>( w.Name?.EqualsIgnoreCase(name)??false) && (w.CategoryType?.EqualsIgnoreCase(categoryType)?? false) && w.AccountUUID == AccountUUID);
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<Category> GetCategories(string accountUUID, bool deleted = false, bool includeDefaults = false)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if(includeDefaults)
                    return context.GetAll<Category>()?.Where(sw => (sw.AccountUUID == accountUUID || sw.AccountUUID == SystemFlag.Default.Account) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();

                return context.GetAll<Category>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
        }

       

        public List<Category> GetCategories(string categoryType, string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Category>()?.Where(sw => (sw.CategoryType?.EqualsIgnoreCase(categoryType)??false) && (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Get(string uuid )
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Category>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Category not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Category)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
           
                    Category dbU = context.GetAll<Category>()
                        .FirstOrDefault( wu => (wu.Name?.EqualsIgnoreCase(s.Name)?? false) && 
                                wu.AccountUUID == s.AccountUUID && 
                                wu.CategoryType.EqualsIgnoreCase(s.CategoryType) );


                    if (dbU != null)
                    {
                        if(dbU.Deleted == false)
                            return ServiceResponse.Error("Category already exists.");

                        //It was deleted, so just reinstate it.
                        dbU.Deleted = false;

                        return this.Update(dbU);
                    }

              
   
                if (context.Insert<Category>(s))
                    return ServiceResponse.OK("",s);
            }
            return ServiceResponse.Error("An error occurred inserting Category " + s.Name);
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Category data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Category)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Category>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Category was not updated.");
        }

    }
}
