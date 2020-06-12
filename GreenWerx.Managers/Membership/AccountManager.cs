// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.DataSets;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Plant;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Helpers;

namespace GreenWerx.Managers.Membership
{
    public class AccountManager : BaseManager, ICrud
    {
        private readonly SystemLogger _logger = null;
        private readonly RoleManager _roleManager = null;

        public AccountManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "AccountManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;
            _roleManager = new RoleManager(connectionKey, this.GetUser(SessionKey));
            _logger = new SystemLogger(this._connectionKey);
        }

        public Account AddAccountFromStrain(Strain s)
        {
            if (s == null)
                return null;
            ///if (!this.DataAccessAuthorized(v, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            var res = Get(s.BreederUUID);
            if (res.Code != 200)
                return null;

            Account v = (Account)res.Result;

            if (v != null)
                return v;
            //try getting the Account by name with the UUID because the ui allows adding
            //via text/combobox.
            res = Get(s.BreederUUID);
            if (res.Code != 200)
                return null;

            v = (Account)res.Result;

            if (v != null)
                return v;

            v = new Account()
            {
                AccountUUID = s.AccountUUID,
                Active = true,
                CreatedBy = s.CreatedBy,
                DateCreated = DateTime.UtcNow,
                Deleted = false,
                Name = s.BreederUUID,
                UUIDType = "Account"
            };

            ServiceResult resi = this.Insert(v);

            if (resi.Code == 200)
                return (Account)res.Result;

            return null;
        }

        public ServiceResult AddUsersToAccount(string accountUUID, List<AccountMember> users, User requestingUser)
        {
            ///if (!this.DataAccessAuthorized(a, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");
            foreach (AccountMember am in users)
            {
                ServiceResult res = AddUserToAccount(accountUUID, am.MemberUUID, requestingUser);
                if (res.Code == 500)
                    return res;
            }
            return ServiceResponse.OK(string.Format("{0} users added to account.", users.Count));
        }

        public ServiceResult AddUserToAccount(string accountUUID, string userUUID, User requestingUser)
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("Invalid account id");

            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("Invalid user id");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Account a = context.GetAll<Account>()?.FirstOrDefault(w => w.UUID == accountUUID);
                if (a == null)
                    return ServiceResponse.Error("Account not found.");

                if (!this.DataAccessAuthorized(a, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");
                //// if (!_roleManager.DataAccessAuthorized(a, requestingUser, "post", false))
                ////   return ServiceResponse.Error("Access denied for account " + a.Name );

                User u = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == userUUID);
                if (u == null)
                    return ServiceResponse.Error("User not found.");

                if (!_roleManager.DataAccessAuthorized(u, requestingUser, "post", false))
                    return ServiceResponse.Error("Access denied for user " + u.Name);

                if (IsUserInAccount(accountUUID, userUUID))
                    return ServiceResponse.OK("User is already a member of the account.");

                if (context.Insert<AccountMember>(new AccountMember() { AccountUUID = accountUUID, MemberUUID = userUUID, MemberType = "User" }))
                    return ServiceResponse.OK(string.Format("User {0} added account.", u.Name));
            }
            return ServiceResponse.Error("Server error, member was not added to the account.");
        }

        public ServiceResult CreateDefaultRolesForAccount(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid account data.");

            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var a = (Account)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                // get system default roles
                var defaultRoles = context.GetAll<Role>()?.Where(rw =>
                    rw.AccountUUID == SystemFlag.Default.Account);

                foreach (var defaultRole in defaultRoles)
                {
                    // does this acount have this role?
                    var role = context.GetAll<Role>()?.FirstOrDefault(rw =>
                               (rw.CategoryRoleName?.EqualsIgnoreCase(defaultRole.CategoryRoleName) ?? false) &&
                               (rw.Category.EqualsIgnoreCase(defaultRole.Category)) &&
                               rw.AccountUUID == a.UUID);

                    if (role != null)
                        continue;// already has it so move on
                    defaultRole.RoleWeight = defaultRole.Weight;
                    defaultRole.AccountUUID = a.UUID;
                    defaultRole.UUID = Guid.NewGuid().ToString("N");
                    defaultRole.DateCreated = DateTime.UtcNow;
                    defaultRole.CreatedBy = _requestingUser.UUID;
                    if (!context.Insert<Role>(defaultRole))
                        return ServiceResponse.Error("System error, default roles were not added.");
                }
            }
            return ServiceResponse.OK();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="accountUUID"></param>
        /// <param name="purge">If this is true they system will do a hard delete, otherwise it set the delete flag to false.</param>
        /// <returns></returns>
        public ServiceResult Delete(string accountUUID, bool purge = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Account a = context.GetAll<Account>()?.FirstOrDefault(w => w.UUID == accountUUID);
                if (a == null)
                    return ServiceResponse.Error("Account not found.");

                if (!this.DataAccessAuthorized(a, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

                ////if (!_roleManager.DataAccessAuthorized(a, requestingUser,"delete", false))
                ////  return ServiceResponse.Error("You are not authorized access to this account.");

                if (accountUUID == _requestingUser.AccountUUID)//todo check if any user has this as default account
                    return ServiceResponse.Error("Cannot delete a default account. You must select another account as default before deleting this one.");

                if (!purge)
                {
                    a.Deleted = true;
                    return Update(a);
                }

                try
                {
                    if (context.Delete<Account>(a) > 0)
                        return ServiceResponse.OK();

                    return ServiceResponse.Error("No records deleted.");
                }
                catch (Exception ex)
                {
                    _logger.InsertError(ex.Message, "AccountManager", "DeleteAccount:" + accountUUID);
                    Debug.Assert(false, ex.Message);
                    return ServiceResponse.Error(ex.Message);
                }
            }
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            if (n == null)
                return ServiceResponse.Error("No account record sent.");

            if (n.UUID == this._requestingUser.AccountUUID)//todo check if any user has this as default account
                return ServiceResponse.Error("Cannot delete a default account. You must select another account as default before deleting this one.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var account = (Account)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Account a = context.GetAll<Account>()?.FirstOrDefault(w => w.UUID == account.UUID);

                if (!this.DataAccessAuthorized(a, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

                if (!purge)
                {
                    Account dbAcct = context.GetAll<Account>()?.FirstOrDefault(w => w.UUID == a.UUID);

                    if (dbAcct == null)
                        return ServiceResponse.Error("Account not found.");

                    dbAcct.Deleted = true;

                    return Update(dbAcct);
                }

                try
                {
                    if (context.Delete<Account>(a) > 0)
                        return ServiceResponse.OK();

                    return ServiceResponse.Error("No records deleted.");
                }
                catch (Exception ex)
                {
                    _logger.InsertError(ex.Message, "AccountManager", "DeleteAccount:" + account.UUID);
                    Debug.Assert(false, ex.Message);
                    return ServiceResponse.Error(ex.Message);
                }
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("UUID was not sent.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Account>()?.FirstOrDefault(aw => aw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Account not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<string> GetAccountCategories(ref DataFilter filter)
        {
            try
            {
                bool includeDeleted = filter.IncludeDeleted;
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<Account>(ref filter)?.Where(sw =>
                            sw.Private == false &&
                            sw?.Deleted == includeDeleted)
                        ?.OrderBy(o => o.Category)
                        ?.Select(s => s.Category)
                        ?.Distinct()
                        ?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

            return new List<string>();
        }

        public List<User> GetAccountMembers(string accountUUID, ref DataFilter filter)
        {
            List<User> accountMembers;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                accountMembers = context.GetAll<AccountMember>(ref filter)?.Where(w => w.AccountUUID == accountUUID)
                                        .Join(
                                            context.GetAll<User>()
                                                .Where(w => w.Deleted == false),
                                            acct => acct.MemberUUID,
                                            users => users.UUID,
                                            (acct, users) => new { acct, users }
                                         )
                                         .Select(s => s.users)
                                         .ToList();
            }
            if (accountMembers == null)
                return new List<User>();

            accountMembers = UserManager.ClearSensitiveData(accountMembers);

            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return accountMembers;
        }

        /// <summary>
        /// Gets all users not in the account
        /// </summary>
        /// <param name="accountUUID"></param>
        /// <param name="clearSensitiveData"></param>
        /// <returns></returns>
        public List<User> GetAccountNonMembers(string accountUUID, ref DataFilter filter)
        {
            List<User> nonMembers;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                //this query is from a bug, just kludge it in for now. backlog fix this damn mess.
                nonMembers = context.GetAll<User>(ref filter)?.Where(w => w.Deleted == false && string.IsNullOrWhiteSpace(w.AccountUUID) == true).ToList();

                nonMembers.AddRange(context.GetAll<AccountMember>(ref filter)?.Where(w => w.AccountUUID != accountUUID)
                                           .Join(
                                               context.GetAll<User>(ref filter)
                                                     .Where(w => w.Deleted == false),
                                               acct => acct.MemberUUID,
                                               users => users.UUID,
                                               (acct, users) => new { acct, users }
                                            )
                                            .Select(s => s.users)
                                            .ToList());
            }
            List<User> members = GetAccountMembers(accountUUID, ref filter);

            if (members != null)
                nonMembers = nonMembers.Except(members).ToList();

            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return nonMembers;
        }

        public List<Account> GetAccounts(string userUUID, ref DataFilter filter)
        {
            List<Account> acts;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<AccountMember>()?.Where(w => w.MemberUUID == userUUID)
                         .Join(context.GetAll<Account>(ref filter)?.Where(w => w.Deleted == false),
                             am => am.AccountUUID,
                             act => act.UUID,
                             (am, acct) => new { am, acct }
                         )
                         .Select(s => s.acct).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }

        public Account GetAccountByDomain(string domainName)
        {
           
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Account>()?.FirstOrDefault(w => w.WebSite.Contains(domainName, StringComparison.CurrentCultureIgnoreCase));
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
           
        }

        public List<dynamic> GetAllAccounts(DataFilter filter)
        {
            List<dynamic> acts;
            if (filter.IncludePrivate)
            { //if looking at private then check permission
                if (!this.DataAccessAuthorized("ACCOUNT", false, filter.IncludePrivate))
                {
                    filter.IncludePrivate = false;
                    // if not allowed private then check if they can see anything..
                    if (!this.DataAccessAuthorized("ACCOUNT", false, filter.IncludePrivate))
                        return new List<dynamic>();
                }
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                acts = context.GetAll<Account>(ref filter)?.Where(w => w.Deleted == false && w.Private == false).OrderBy(o => o.Name).Cast<dynamic>().ToList();

                if (filter.Latitude != 0 && filter.Longitude != 0)
                {
                    foreach (var account in acts)
                    {
                        //   we have to put lat and lon in each type cause it's taking too long to get the data.
                        //var eventLocation = context.GetAll<Location>().FirstOrDefault(w => w.Deleted == false && w.UUID == account.LocationUUID);
                        //if (eventLocation == null || eventLocation.Latitude  == null || eventLocation.Longitude  == null)
                        //    continue;

                        account.Distance = Math.Ceiling(MathHelper.Distance(filter.Latitude, filter.Longitude, account.Latitude ?? 0, account.Longitude ?? 0));
                    }
                }
                //this should have been filtered
                //   acts = acts.Filter(ref filter).ToList();

                if (!string.IsNullOrWhiteSpace(filter.SortBy) &&
                    filter.SortBy.EqualsIgnoreCase("distance"))
                {
                    acts = acts.OrderBy(ob => ob?.Distance).ToList();
                }
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return acts;
        }

        public List<Favorite> GetFavoriteAccounts(string userUUID, string accountUUID, ref DataFilter filter)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.GetAll<Favorite>(ref filter)?.FirstOrDefault() == null)
                        return new List<Favorite>();

                    var accounts = context.GetAll<Favorite>(ref filter)?.Where(w => w?.CreatedBy == userUUID && w?.AccountUUID == accountUUID
                        && w.UUIDType.EqualsIgnoreCase("account"))
                        ?.Join(context.GetAll<Account>(), //shouldn't filter on this join because the subject is the filter.
                         rem => rem.ItemUUID,
                         act => act.UUID,
                        (rem, act) => new { rem, act })
                        ?.Select(s => new Favorite()
                        {
                            UUID = s.rem.UUID,
                            UUIDType = s.rem.UUIDType,
                            CreatedBy = s.rem.CreatedBy,
                            AccountUUID = s.rem.AccountUUID
                        });

                    return accounts.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

            return new List<Favorite>();
        }

        /// <summary>
        /// Returns accounts the users is not a member of
        /// </summary>
        /// <param name="userUUID"></param>
        /// <returns></returns>
        public List<Account> GetNonMemberAccounts(string userUUID)
        {
            string sql = @"SELECT
                            Accounts.Id,
                            Accounts.ParentId,
                            Accounts.UUID,
                            Accounts.UUIDType,
                            Accounts.UUParentID,
                            Accounts.UUParentIDType,
                            Accounts.Name,
                            Accounts.Status,
                            UsersInAccount.MemberUUID
                            FROM
                            Accounts ,
                            UsersInAccount
                            WHERE
                            UsersInAccount.MemberUUID <> @MEMBERID OR
                            Accounts.UUID <> '' and Accounts.Deleted = 0";
            DynamicParameters p = new DynamicParameters();
            p.Add("@MEMBERID", userUUID);

            List<Account> userAccounts;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                userAccounts = context.Select<Account>(sql, p).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return userAccounts;
        }

        /// <summary>
        /// Gets all accounts the user is a member of.
        ///     userUUID  xref to  UsersInAccount => Accounts
        /// </summary>
        /// <param name="userUUID"></param>
        /// <returns></returns>
        public List<Account> GetUsersAccounts(string userUUID, ref DataFilter filter)
        {
            //AccountMember = UsersInAccount table.
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                IEnumerable<Account> userAccounts =
              from am in context.GetAll<AccountMember>()?.Where(amw => amw.MemberUUID == userUUID)/// && rrw.UUParentIDType == "User")// && rrw.AccountUUID == accountUUID)
              join accounts in context.GetAll<Account>(ref filter)?.Where(uw => uw.Deleted == false) on am.AccountUUID equals accounts.UUID
              select accounts;

                ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return userAccounts.ToList();
            }
        }

        public ServiceResult Insert(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid account data.");

            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var a = (Account)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Account dbU = context.GetAll<Account>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(a.Name) ?? false) && wu.AccountUUID == a.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Account already exists.");

                if (context.Insert<Account>(a))
                    return ServiceResponse.OK("", a);
            }
            return ServiceResponse.Error("System error, account was not added.");
        }

        public bool IsUserInAccount(string accountUUID, string userUUID)
        {
            if (string.IsNullOrWhiteSpace(userUUID))
                return false;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.GetAll<AccountMember>().Any(w => w.AccountUUID == accountUUID && w.MemberUUID == userUUID))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 1. remove record in usersinaccounts AccountMember where AccountUUID and user id match
        /// 2. check user record and make sure the account id doesn't match. If it does erase the field.
        /// TBD may need other proccessing
        /// </summary>
        /// <param name="accountUUID"></param>
        /// <param name="userUUID"></param>
        /// <returns></returns>
        public ServiceResult RemoveUserFromAccount(string accountUUID, string userUUID)
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("Invalid account id");

            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("Invalid user id");

            if (!IsUserInAccount(accountUUID, userUUID))
                return ServiceResponse.OK();

            ///if (!this.DataAccessAuthorized(a, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            User u = null;
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    u = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == userUUID);
                    if (u == null)
                        return ServiceResponse.Error("Invalid user id");

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@AccountUUID", accountUUID);
                    parameters.Add("@MEMBERID", userUUID);

                    context.Delete<AccountMember>("WHERE AccountUUID=@AccountUUID AND  MemberUUID=@MEMBERID", parameters);
                    ////Remove the reference in the xref table
                    ////SQLITE SYNTAX
                    ////    object[] parameters = new object[] { accountUUID, userUUID };
                    //// int res =  context.Delete<AccountMember>("WHERE AccountUUID=? AND  MemberUUID=?", parameters);

                    if (u.AccountUUID == accountUUID)
                    {
                        u.AccountUUID = "";
                        if (context.Update<User>(u) == 0)
                            return ServiceResponse.Error("Failed to remove user " + u.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "AccountManager", "RemoveUserFromAccount:accountUUID:" + accountUUID + " userUUID" + userUUID);
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK(string.Format("User {0} removed from account.", u.Name));
        }

        ///This removes the user from all accounts
        public ServiceResult RemoveUserFromAllAccounts(string userUUID)
        {
            ServiceResult res = ServiceResponse.OK();

            User u = null;
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    //Make sure correct userid is passed in.
                    u = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == userUUID);

                    if (u == null)
                        return ServiceResponse.Error("Invalid user id.");

                    if (!this.DataAccessAuthorized(u, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@MEMBERID", userUUID);
                    context.Delete<AccountMember>("WHERE MemberUUID=@MEMBERID", parameters);

                    ////SQLITE
                    ////Remove the reference in the xref table
                    ////object[] parameters = new object[] { userUUID };
                    ////context.Delete<AccountMember>("WHERE MemberUUID=?", parameters);

                    //now make sure the primary account in the user table is emptied.
                    u.AccountUUID = "";
                    if (context.Update<User>(u) == 0)
                        return ServiceResponse.Error(u.Name + " failed to update. ");
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "AccountManager", "RemoveUserFromAllAccounts:userUUID:" + userUUID);
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }
            return res;
        }

        public ServiceResult RemoveUsersFromAccount(string accountUUID, List<AccountMember> users, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();
            foreach (AccountMember am in users)
            {
                ServiceResult removeRes = RemoveUserFromAccount(accountUUID, am.MemberUUID);
                if (res.Code != 200)
                {
                    msg.AppendLine(removeRes.Message);
                    res.Status = "ERROR";
                }
            }
            res.Message = msg.ToString();
            return res;
        }

        public List<Account> Search(string name, ref DataFilter filter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<Account>();
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Account>(ref filter)?.Where(aw => aw.Name.EqualsIgnoreCase(name)).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<Account> SearchEx(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<Account>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Account>()?.Where(
                                                aw => aw.Name.EqualsIgnoreCase(searchText) || 
                                                aw.Email.EqualsIgnoreCase(searchText) || 
                                                aw.WebSite.EqualsIgnoreCase(searchText)
                                                ).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        /// <summary>
        /// This sets the AccountUUID in the Users table. This is the default or active account
        /// the user is accociated with and all data will be loaded according to this field.
        /// </summary>
        /// <param name="accountUUID"></param>
        /// <param name="userUUID"></param>
        /// <returns></returns>
        public ServiceResult SetActiveAccount(string accountUUID, string userUUID, User requestingUser)
        {
            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("Invalid user id");

            if (!IsUserInAccount(accountUUID, userUUID))
                return ServiceResponse.Error("User must be added to the account before setting it as the active account.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                User u = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == userUUID);
                if (u == null)
                    return ServiceResponse.Error("Invalid user id.");

                if (u.AccountUUID == accountUUID)
                    return ServiceResponse.OK("This account is already the default for this user.");

                if (!this.DataAccessAuthorized(u, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

                u.AccountUUID = accountUUID;

                if (context.Update<User>(u) > 0)
                    return ServiceResponse.OK("This account is now the default for this user.");
            }
            return ServiceResponse.Error("Error occurred while updating the user.");
        }

        /// <summary>
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid account data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var a = (Account)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Account>(a) > 0)
                    return ServiceResponse.OK("", a);
            }
            return ServiceResponse.Error("System error, account was not updated.");
        }

        private List<Account> FindAccount(string uuid, string name, string AccountUUID, ref DataFilter filter)
        {
            List<Account> res = null;

            if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(name) == false)
                res = this.Search(name, ref filter);
            else
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    res = context.GetAll<Account>(ref filter)?.Where(w => w.UUID == uuid || (w.Name?.EqualsIgnoreCase(name) ?? false)).ToList();
                }
            }
            if (res == null)
                return res;

            ////    if (!this.DataAccessAuthorized(res, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            if (filter.IncludeSystemAccount && (res.FirstOrDefault().AccountUUID == AccountUUID))
                return res;

            if (res.FirstOrDefault().AccountUUID == AccountUUID)
                return res;

            return new List<Account>();
        }

        //public List<Account> ClearSensitiveData(List<Account> accounts)
        //{
        //    if (accounts == null)
        //        return accounts;

        //    accounts.ForEach(am =>
        //    {
        //        am.Email = "";
        //    });
        //    return accounts;
        //}
        //public Account ClearSensitiveData(Account account)
        //{
        //    if (account == null)
        //        return account;

        //    account.Email = "";
        //    return account;
        //}

        #region test

        public List<Account> GetAllAccountsEx(ref DataFilter filter)
        {
            try
            {
                if (filter.IncludePrivate)
                { //if looking at private then check permission
                    if (!this.DataAccessAuthorized("ACCOUNT", false, filter.IncludePrivate))
                    {
                        filter.IncludePrivate = false;
                        //// if not allowed private then check if they can see anything..
                        if (!this.DataAccessAuthorized("ACCOUNT", false, filter.IncludePrivate))
                            return new List<Account>();
                    }
                }

                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    DynamicParameters parameters = new DynamicParameters();// see function GetTableData

                    parameters.Add("@PRIVATE", filter.IncludePrivate);
                    parameters.Add("@DELETED", filter.IncludeDeleted);

                    string sql = @"SELECT COUNT(*) FROM Accounts e
                                   WHERE
	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
	                                (e.Deleted = 0 OR e.Deleted = @DELETED)";

                    string where = DatasetManager.BuildWhereClause(filter.Screens);
                    if (!string.IsNullOrWhiteSpace(where))
                    {
                        sql += " AND " + where;
                        var screenParams = DatasetManager.GetParameters(filter.Screens);
                        if (screenParams != null)
                            parameters.AddDynamicParams(screenParams);
                    }

                    filter.TotalRecordCount = (int)context.ExecuteScalar(sql, parameters);

                    if (filter.TotalRecordCount == 0)
                        return new List<Account>();

                    parameters.Add("@CLIENTLAT", filter.Latitude);
                    parameters.Add("@CLIENTLON", filter.Longitude);
                    parameters.Add("@MEASURE", 3956.55); // 3956.55 = miles
                    //
                    sql = @"SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , e.Latitude, e.Longitude, @MEASURE ))  as Distance
                                            ,[Name]	 ,[UUID]	,[UUIDType]		,[GUUID],
                                            [Image],	[Phone], [Status]
                                            ,[Email],[OwnerUUID],[CreatedBy],[WebSite],[Private]	,[Category],[LocationUUID]
                                            ,e.[Latitude], e.[Longitude], e.[IsAffiliate]
                                    FROM Accounts e
                                   WHERE
	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
	                                (e.Deleted = 0 OR e.Deleted = @DELETED)";

                    where = DatasetManager.BuildWhereClause(filter.Screens);
                    if (!string.IsNullOrWhiteSpace(where))
                    {
                        sql += " AND " + where;
                    }

                    if (string.IsNullOrWhiteSpace(filter.SortBy))
                    {   //for events we want to default sort by start date
                        sql += " ORDER BY Name ASC";
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(filter.SortDirection))
                            filter.SortDirection = "ASC";
                        sql += " ORDER BY " + filter.SortBy + " " + filter.SortDirection;
                    }

                    if (filter.PageResults)
                    {
                        if (filter.Page <= 0)
                            filter.Page = 1;

                       // filter.PageSize = 50;
                        parameters.Add("@PAGESIZE", filter.PageSize);
                        parameters.Add("@PAGEINDEX", filter.Page);
                        sql += @" OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY";
                    }

                    var accounts = context.Select<Account>(sql, parameters);
                    return accounts.ToList();
                }

                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<Account>();
        }

        #endregion test
    }
}