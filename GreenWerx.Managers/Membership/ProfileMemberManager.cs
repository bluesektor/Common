using Dapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using GreenWerx.Data;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Membership
{
    public class ProfileMemberManager : BaseManager//, ICrud
    {
        public ProfileMemberManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "ProfileMemberManager CONTEXT IS NULL!");

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
                if (purge && context.Delete<ProfileMember>((ProfileMember)n) == 0)
                {
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
                }

                //get the profileMember from the table with all the data so when its updated it still contains the same data.
                res = this.Get(n.UUID);
                if (res.Code != 200)
                    return res;

                var p = res.Result as ProfileMember;

                p.Deleted = true;
                if (context.Update<ProfileMember>(p) == 0)
                    return ServiceResponse.Error(p.Name + " failed to delete. ");
            }
            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<ProfileMember>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Profile member not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public INode GetByGetByGUUID(string GUUID)
        {
            if (string.IsNullOrWhiteSpace(GUUID))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<ProfileMember>()?.FirstOrDefault(sw => sw.GUUID == GUUID);
            }
        }

        public INode GetMemberProfile(string userUUID, string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(userUUID) || string.IsNullOrWhiteSpace(accountUUID))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<ProfileMember>()?.FirstOrDefault(sw => sw.UserUUID == userUUID && sw.AccountUUID == accountUUID);
            }
        }

        public List<ProfileMember> GetProfileMembers(string profileUUID, string accountUUID, ref DataFilter filter)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            List<ProfileMember> members = new List<ProfileMember>();
            var includeDeleted = filter == null ? false : filter.IncludeDeleted;
            var includePrivate = filter == null ? false : filter.IncludePrivate;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                // return context.GetAll<ProfileMember>()?.Where(sw => sw.ProfileUUID == profileUUID &&  sw.AccountUUID == accountUUID)
                //     .OrderBy(ob => ob.SortOrder).ToList();
                DynamicParameters parameters = new DynamicParameters();// see function GetTableData
                parameters.Add("@DELETED", includeDeleted);
                parameters.Add("@PRIVATE", filter?.IncludePrivate);
                parameters.Add("@PROFILEUUID", profileUUID);
                parameters.Add("@ACCOUNTUUID", accountUUID);

                string sql = @"SELECT *
                  FROM [dbo].[ProfileMembers] p
                  WHERE
                    p.ProfileUUID = @PROFILEUUID AND
                    p.AccountUUID= @ACCOUNTUUID AND
	                (p.Private = 0 OR p.Private = @private) AND
	                (p.Deleted = 0 OR p.Deleted = @deleted)";
                members = context.Select<ProfileMember>(sql, parameters).ToList();
            }
            return members;
        }

        public ServiceResult Insert_(INode n)
        {
            throw new NotImplementedException();

            //if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");
            //n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);
            //var s = (ProfileMember)n;
            //using (var context = new GreenWerxDbContext(this._connectionKey))
            //{
            //    ProfileMember dbU = context.GetAll<ProfileMember>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);
            //    if (dbU != null)
            //        return ServiceResponse.Error("ProfileMember already exists.");

            //    if (context.Insert<ProfileMember>((ProfileMember)s))
            //        return ServiceResponse.OK(context.Message, s);
            //}
            //return ServiceResponse.Error("An error occurred inserting profileMember " + s.Name);
        }

        public ServiceResult Save(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            var profileMember = (ProfileMember)n;

            try
            {
                n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

                using (var scope = new TransactionScope())
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    var dbUser = context.GetAll<User>().FirstOrDefault(w => w.Name.EqualsIgnoreCase(profileMember.Name));
                    if (dbUser != null)
                        return ServiceResponse.Error("User name already exists." + profileMember.Name);

                    var newUser = this._requestingUser.Clone();
                    string userUUID = Guid.NewGuid().ToString("N");
                    newUser.Name = profileMember.Name;
                    newUser.UUID = userUUID;
                    profileMember.UserUUID = userUUID;
                    profileMember.AccountUUID = _requestingUser.AccountUUID;
                    if (!context.Insert<User>(newUser))
                        return ServiceResponse.Error("An error occurred creating user for profile member " + profileMember.Name);

                    if (!context.Insert<ProfileMember>(profileMember))
                        return ServiceResponse.Error("An error occurred creating profile member " + profileMember.Name);

                    context.SaveChanges();

                    scope.Complete();
                    context.Configuration.AutoDetectChangesEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured when creating profile member.");
            }

            return ServiceResponse.OK("", profileMember);
        }

        public List<ProfileMember> Search(string name)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            if (string.IsNullOrWhiteSpace(name))
                return new List<ProfileMember>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<ProfileMember>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid ProfileMember data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<ProfileMember>((ProfileMember)n) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, ProfileMember was not updated.");
        }

        private List<ProfileMember> FindProfileMember(string uuid, string name, string AccountUUID, bool includeDefaultAccount = true)
        {
            List<ProfileMember> res = null;

            if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(name) == false)
                res = this.Search(name);
            else
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    res = context.GetAll<ProfileMember>()?.Where(w => w.UUID == uuid || (w.Name?.EqualsIgnoreCase(name) ?? false)).ToList();
                }
            }

            if (res == null)
                return res;

            if (includeDefaultAccount && (res.FirstOrDefault().AccountUUID == AccountUUID))
                return res;

            if (res.FirstOrDefault().AccountUUID == AccountUUID)
                return res;

            return new List<ProfileMember>();
        }
    }
}