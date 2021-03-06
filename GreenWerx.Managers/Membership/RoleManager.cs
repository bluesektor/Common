﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using AutoMapper;
using Dapper;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;
using GreenWerx.Data;
using GreenWerx.Data.Helpers;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Flags;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Membership
{
    public class RoleManager : ICrud //Can't derive from BaseManager
    {
        private readonly string _dbConnectionKey = null;
        private readonly SystemLogger _logger = null;
        private readonly List<string> _siteAdmins = null;
        private User _requestingUser = null;
        private bool _runningInstall = false; //for bypassing the authorization when adding roles/permissions

        public RoleManager(string connectionKey)
        {
            _dbConnectionKey = connectionKey;
            _logger = new SystemLogger(_dbConnectionKey);
        }

        public RoleManager(string connectionKey, User requestingUser)
        {
            _dbConnectionKey = connectionKey;
            _logger = new SystemLogger(_dbConnectionKey);
            _requestingUser = requestingUser;
        }

        public RoleManager(string connectionKey, List<string> siteAdmins, User requestingUser)
        {
            _dbConnectionKey = connectionKey;
            _logger = new SystemLogger(_dbConnectionKey);
            _requestingUser = requestingUser;
            _siteAdmins = siteAdmins;
        }

        private RoleManager()
        {
        }

        public string CreateKey(string name, string action, string appType, string accountUUID)
        {
            return name.ToSafeString(true)?.ToLower() + "." + action.ToSafeString(true)?.ToLower() + "." + appType.ToSafeString(true)?.ToLower() + "." + accountUUID.ToSafeString(true);
        }

        public string NameFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            path = path.StripGuids('-');

            path = StringEx.ReplaceIncluding("%", "", path, "");//replace from % to end
            path = StringEx.ReplaceIncluding("?", "", path, "");

            path = path.Replace("/", ".");
            path = path.Replace("..", ".");

            if (path[0] == '.')
                path = path.Remove(0, 1);

            if (path[path.Length - 1] == '.')
                path = path.Remove(path.Length - 1, 1);

            return path;
        }

        #region Permission TODO refactor into icrud class   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// note: Don't add DataAccessAuthorized to this. It's called from the Api to create permissions on the fly.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <param name="request"></param>
        /// <param name="appType"></param>
        /// <param name="AccountUUID"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public ServiceResult CreatePermission(string name, string action, string request, string appType, string AccountUUID = SystemFlag.Default.Account, int weight = 0)
        {
            //// if (!_runningInstall &&  !this.DataAccessAuthorized(p, _requestingUser,"POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            name = StringEx.ReplaceIncluding("?", "", name, "");
            request = StringEx.ReplaceIncluding("?", "", request, "");
            name = StringEx.ReplaceIncluding("%", "", name, "");
            request = StringEx.ReplaceIncluding("%", "", request, "");

            name = name.StripGuids('.');

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name[0] == '.')
                    name = name.Remove(0, 1);

                if (name[name.Length - 1] == '.')
                    name = name.Remove(name.Length - 1, 1);
            }
            action = action.StripGuids('/');

            if (!string.IsNullOrWhiteSpace(action))
            {
                if (action[0] == '/')
                    action = action.Remove(0, 1);

                if (action[action.Length - 1] == '/')
                    action = action.Remove(action.Length - 1, 1);
            }
            string key = CreateKey(name, action, appType, AccountUUID);

            if (PermissionExists(key, true))
                return ServiceResponse.Error("Permission " + key + " already exists.");

            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    context.Insert<Permission>(new Permission
                    {
                        AccountUUID = AccountUUID,
                        Action = action,
                        Active = true,
                        AppType = appType,
                        Key = key,
                        Name = name,
                        Weight = weight,
                        Persists = true,
                        Request = request.StripGuids('/'),
                        EndDate = null,// new DateTime(2222, 2, 22), //sql isn't liking the min date :/
                        StartDate = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "CreatePermission");
                return ServiceResponse.Error("An error occured inserting the permission.");
            }

            return ServiceResponse.OK();
        }

        public List<Permission> GetAccountPermissions(string accountUUID, ref DataFilter filter)
        {
            ////if (!_runningInstall && !this.DataAccessAuthorized(permissions,_requestingUser, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            List<Permission> permissions;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                permissions = context.GetAll<Permission>(ref filter)?.Where(pw => pw.AccountUUID == accountUUID && pw.Deleted == false).DistinctBy(d => d.Name).ToList();
            }

            return permissions;
        }

        public List<Permission> GetAvailablePermissions(string roleUUID, string accountUUID)
        {
            List<Permission> allAccountPermissions;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                allAccountPermissions = context.GetAll<Permission>()?.Where(w => w.AccountUUID == accountUUID && w.Deleted == false).DistinctBy(d => d.Name).ToList();
            }

            //Selected permissions for role.
            List<Permission> rolePermissions = GetPermissionsForRole(roleUUID, accountUUID);

            if (rolePermissions == null || !rolePermissions.Any())
                return allAccountPermissions;//none are selected so return all.

            List<Permission> availablePermissions = new List<Permission>();

            foreach (Permission accountPermission in allAccountPermissions)
            {
                if (!rolePermissions.Any(w => w.UUID == accountPermission.UUID))
                    availablePermissions.Add(accountPermission);
            }

            return availablePermissions;
        }

        public List<Permission> GetPermissionsForRole(string roleUUID, string accountUUID)
        {
            //// if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser,"GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            List<Permission> members;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                members = context.GetAll<RolePermission>()
                        .Where(rrw => rrw.RoleUUID == roleUUID &&
                               rrw.AccountUUID == accountUUID)
                        .Join(
                            context.GetAll<Permission>()?.Where(uw => uw.Deleted == false),
                            role => role.PermissionUUID,
                            perms => perms.UUID,
                            (role, perms) => new { role, perms }
                        )
                        .Select(s => s.perms)
                        .ToList();
            }
            return members;
        }

        public bool PermissionExists(string key, bool creatingPermission = false)
        {
            try
            {
                IEnumerable<Permission> permissions;
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    permissions = context.GetAll<Permission>()?.Where(pw => pw?.Key == key).ToList();
                }
                if (permissions == null || !permissions.Any())
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                //this is cause sqlite is a pain in the ass.
                //Keeps giving stupid errors when accessed too fast.
                //so if we're creating permissions we don't want to accidentally duplicate, and hope it
                //won't error next call to recreate the permission. f***** wonky as hell. >:|
                if (creatingPermission)
                    return true;

                return false;
            }
        }

        #endregion Permission TODO refactor into icrud class   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #region Roles  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public ServiceResult CloneRole(string roleUUID)
        {
            var res = this.Get(roleUUID);
            if (res.Code != 200)
                return res;

            Role originalRole = (Role)res.Result;

            if (!_runningInstall && !this.DataAccessAuthorized(originalRole, _requestingUser, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Role, Role>();
            });

            IMapper mapper = config.CreateMapper();
            Role clonedRole = mapper.Map<Role, Role>(originalRole);
            clonedRole.UUID = Guid.NewGuid().ToString("N");
            clonedRole.Name += " - Copy";

            using (var transactionScope = new TransactionScope())
            using (var dbContext = new GreenWerxDbContext(_dbConnectionKey))
            {
                //backlog revisit this transaction. Using the member context to make it work seems off.
                try
                {
                    if (!dbContext.Insert<Role>(clonedRole))
                    {
                        _logger.InsertError("Failed cloning role:" + originalRole.UUID, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                        return ServiceResponse.Error("Failed to insert cloned role.");
                    }

                    //Copy permissions into cloned role
                    List<RolePermission> rolePermissions;
                    using (var context = new GreenWerxDbContext(_dbConnectionKey))
                    {
                        rolePermissions = context.GetAll<RolePermission>()?.Where(w => w.AccountUUID == originalRole.AccountUUID && w.RoleUUID == originalRole.UUID).ToList();
                    }
                    //assing the new roleUUID to the permissisons
                    rolePermissions.ForEach(x => x.RoleUUID = clonedRole.UUID);

                    foreach (RolePermission rp in rolePermissions)
                    {
                        if (!dbContext.Insert<RolePermission>(rp))
                        {
                            _logger.InsertError("Failed cloning role permissions:" + originalRole.UUID, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                            return ServiceResponse.Error("Failed cloning role permissions.");
                        }
                    }

                    //Copy Users in to cloned role..
                    List<UserRole> userRoles;
                    using (var context = new GreenWerxDbContext(_dbConnectionKey))
                    {
                        userRoles = context.GetAll<UserRole>()
                                                .Where(rrw => rrw.RoleUUID == originalRole.UUID &&
                                                        rrw.AccountUUID == originalRole.AccountUUID).ToList();
                    }
                    userRoles.ForEach(x => x.RoleUUID = clonedRole.UUID);

                    foreach (UserRole ur in userRoles)
                    {
                        if (!dbContext.Insert<UserRole>(ur))
                        {
                            _logger.InsertError("Failed cloning role users:" + originalRole.UUID, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                            return ServiceResponse.Error("Failed cloning role users.");
                        }
                    }
                    transactionScope.Complete();
                }
                catch (Exception ex)
                {
                    _logger.InsertError(ex.Message, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                }
            }

            return ServiceResponse.OK();
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, _requestingUser, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var r = (Role)n;

            List<UserRole> usersInRole;
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                usersInRole = context.GetAll<UserRole>()?.Where(w => w.AccountUUID == r.AccountUUID && w.RoleUUID == r.UUID).ToList();
            }
            List<RolePermission> rolePermissions = GetRolePermissions(r.UUID, r.AccountUUID);

            if (purge)
            {
                using (var transactionScope = new TransactionScope())
                using (var context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    try
                    {
                        if (context.Delete<Role>(r) == 0)
                        {
                            msg.AppendLine("Failed to delete role " + r.Name);
                        }

                        foreach (RolePermission rp in rolePermissions)
                        {
                            if (context.Delete<RolePermission>(rp) == 0)
                                msg.AppendLine("Failed to delete role permission" + rp.UUID);
                        }

                        foreach (UserRole ur in usersInRole)
                        {
                            if (context.Delete<UserRole>(ur) == 0)
                                msg.AppendLine("Failed to delete role user role" + ur.Name);
                        }

                        transactionScope.Complete();
                    }
                    catch (Exception ex)
                    {
                        _logger.InsertError(ex.Message, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                        return ServiceResponse.Error("Exception occured while deleting this record.");
                    }
                }
            }
            else //mark as deleted
            {
                using (var transactionScope = new TransactionScope())
                using (var context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    try
                    {
                        r.Deleted = true;

                        if (context.Update<Role>(r) == 0)
                            return ServiceResponse.Error(r.Name + " failed to delete. ");

                        foreach (UserRole ur in usersInRole)
                        {
                            ur.Deleted = true;
                            if (context.Update<UserRole>(ur) == 0)
                                msg.AppendLine("Failed to delete role user role" + ur.Name);
                        }
                        transactionScope.Complete();
                    }
                    catch (Exception ex)
                    {
                        _logger.InsertError(ex.Message, "RoleManager", MethodInfo.GetCurrentMethod().Name);
                        return ServiceResponse.Error("Exception occured while deleting this record.");
                    }
                }
            }
            if (msg.Length > 0)
            {
                res.Message = msg.ToString();
                res.Code = 500;
                res.Status = "ERROR";
            }
            return res;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="accountUUID"></param>
        /// <param name="purge">If this is true they system will do a hard delete, otherwise it set the delete flag to false.</param>
        /// <returns></returns>
        public ServiceResult DeleteRole(string roleUUID, bool purge = false)
        {
            Role r;
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                r = context.GetAll<Role>()?.FirstOrDefault(w => w.UUID == roleUUID);
            }
            if (r == null)
                return ServiceResponse.Error("Role not found.");

            return Delete(r, purge);
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("No uuid was sent.");

            //// if (!_runningInstall && !this.DataAccessAuthorized(r,_requestingUser, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                var res = context.GetAll<Role>()?.FirstOrDefault(rw => rw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Role not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public INode GetRole(string name, string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            //// if (!_runningInstall && !this.DataAccessAuthorized(r, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                return context.GetAll<Role>()?.FirstOrDefault(rw => (rw.Name?.EqualsIgnoreCase(name) ?? false) && rw.AccountUUID == accountUUID);
            }
        }

        public List<Role> GetRoles()
        {
            //// if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser,"GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                return context.GetAll<Role>().ToList();
            }
        }

        public List<Role> GetRoles(string accountUUID)
        {
            List<Role> roles;

            //// if (!_runningInstall && !this.DataAccessAuthorized(r,_requestingUser, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                roles = context.GetAll<Role>()?.Where(rw => rw.AccountUUID == accountUUID && rw.Deleted == false).OrderBy(ob => ob.Name).ToList();
            }

            return roles;
        }

        /// <summary>
        /// NOTE: Make sure accountUUID is set.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public ServiceResult Insert(INode n)
        {
            if (!_runningInstall && !this.DataAccessAuthorized(n, _requestingUser, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var r = (Role)n;

            Role dbU;
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                dbU = context.GetAll<Role>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(r.Name) && wu.AccountUUID == r.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Role already exists.");

                context.Insert<Role>(r);
            }

            return ServiceResponse.OK("", r);
        }

        public List<Role> Search(string name, string category)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Role>();

            if (_requestingUser == null)
                return new List<Role>();
            //// if (!_runningInstall && !this.DataAccessAuthorized(r, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                return context.GetAll<Role>()?.Where(rw =>
                            (rw.CategoryRoleName.EqualsIgnoreCase(name)) &&
                            (rw.Category.EqualsIgnoreCase(category)) &&
                            rw.AccountUUID == _requestingUser.AccountUUID).ToList();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid role data.");

            if (!_runningInstall && !this.DataAccessAuthorized(n, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var r = (Role)n;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                if (context.Update<Role>(r) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, role was not updated.");
        }

        #endregion Roles  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #region RolePermission  TODO refactor into icrud class  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public ServiceResult AddPermisssionsToRole(string roleUUID, List<Permission> rps, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();
            res = this.Get(roleUUID);
            if (res.Code != 200)
                return res;

            Role r = (Role)res.Result;

            if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            foreach (Permission p in rps)
            {
                RolePermission rp = new RolePermission()
                {
                    PermissionUUID = p.UUID,
                    RoleUUID = roleUUID,
                    AccountUUID = r.AccountUUID,
                    RoleWeight = r.RoleWeight
                };
                ServiceResult addRes = AddRolePermission(rp);
                if (addRes.Code != 200)
                {
                    msg.AppendLine(addRes.Message);
                    res.Code = 500;
                    res.Status = "ERROR";
                }
            }
            res.Message = msg.ToString();
            return res;
        }

        public ServiceResult AddRolePermission(RolePermission rp)
        {
            if (!_runningInstall && !this.DataAccessAuthorized(rp, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            ServiceResult res = ServiceResponse.OK();
            if (RolePermissionExists(rp.RoleUUID, rp.AccountUUID, rp.PermissionUUID))
                return res;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                rp.DateCreated = DateTime.UtcNow;
                rp.CreatedBy = _requestingUser.UUID;
                if (!context.Insert<RolePermission>(rp))
                    return ServiceResponse.Error("Failed to add. ");
            }
            return res;
        }

        public ServiceResult DeletePermissionsFromRole(string roleUUID, List<Permission> rps, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();

            foreach (Permission p in rps)
            {
                if (!_runningInstall && !this.DataAccessAuthorized(p, _requestingUser, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

                ServiceResult delRes = DeleteRolePermission(new RolePermission() { PermissionUUID = p.UUID, RoleUUID = roleUUID, AccountUUID = p.AccountUUID });
                if (delRes.Code != 200)
                {
                    msg.AppendLine(delRes.Message);
                    res.Code = 500;
                    res.Status = "ERROR";
                }
            }
            res.Message = msg.ToString();
            return res;
        }

        public ServiceResult DeleteRolePermission(RolePermission rp)
        {
            ServiceResult res = ServiceResponse.OK();

            if (rp == null)
                return ServiceResponse.Error("No record sent.");

            if (!RolePermissionExists(rp.RoleUUID, rp.AccountUUID, rp.PermissionUUID))
                return ServiceResponse.Error("Record not found.");

            if (!_runningInstall && !this.DataAccessAuthorized(rp, _requestingUser, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@PERMISSIONUUID", rp.PermissionUUID);
                parameters.Add("@ROLEUUID", rp.RoleUUID);
                parameters.Add("@ACCOUNTUUID", rp.AccountUUID);
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    if (context.Delete<RolePermission>("WHERE PermissionUUID=@PERMISSIONUUID AND RoleUUID=@ROLEUUID AND AccountUUID=@ACCOUNTUUID", parameters) == 0)
                        return ServiceResponse.Error("Failed to delete. ");
                }
                ////SQLITE
                ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
                ////object[] paramters = new object[] { rp.PermissionUUID , rp.RoleUUID ,rp.AccountUUID };
                ////context.Delete<RolePermission>("WHERE PermissionUUID=? AND RoleUUID=? AND AccountUUID=?", paramters);
                ////  context.Delete<RolePermission>(rp);
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "DeleteRolePermission");
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }
            return res;
        }

        public RolePermission GetRolePermission(string roleUUID, string accountUUID, string permissionUUID)
        {
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                //// if (!_runningInstall &&  !this.DataAccessAuthorized(r,_requestingUser, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return context.GetAll<RolePermission>()?.FirstOrDefault(w => w.AccountUUID == accountUUID && w.RoleUUID == roleUUID && w.PermissionUUID == permissionUUID);
            }
        }

        public List<RolePermission> GetRolePermissions(string roleUUID, string accountUUID)
        {
            //// if (!_runningInstall &&  !this.DataAccessAuthorized(r,_requestingUser, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                return context.GetAll<RolePermission>()?.Where(w => w.AccountUUID == accountUUID && w.RoleUUID == roleUUID).ToList();
            }
        }

        public bool RolePermissionExists(string roleUUID, string accountUUID, string permissionUUID)
        {
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                if (context.GetAll<RolePermission>().Any(rpw => rpw.AccountUUID == accountUUID && rpw.RoleUUID == roleUUID && rpw.PermissionUUID == permissionUUID))
                    return true;
            }
            return false;
        }

        #endregion RolePermission  TODO refactor into icrud class  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #region UserRole  TODO refactor into icrud class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public ServiceResult AddUsersToRole(string roleUUID, List<User> urs, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();
            foreach (User u in urs)
            {
                ServiceResult addRes = AddUserToRole(roleUUID, u, requestingUser);
                if (addRes.Code != 200)
                {
                    msg.AppendLine(addRes.Message);
                    res.Code = 500;
                    res.Status = "ERROR";
                }
            }
            res.Message = msg.ToString();
            return res;
        }

        /// <summary>
        /// AccountUUID should be the users account id, not the account for the role.
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
        public ServiceResult AddUserToRole(string roleUUID, INode u, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();

            if (IsInRole(u.UUID, u.AccountUUID, roleUUID, false))
                return res;

            res = Get(roleUUID);
            if (res.Code != 200)
                return res;
            Role r = (Role)res.Result;
            //if the role doesn't match the account then the role
            //hasn't been created for the account so the user can not be added to it.
            //
            if (r.AccountUUID != requestingUser.AccountUUID)
                return ServiceResponse.Error("Invalid parameter");

            if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                UserRole ur = new UserRole();
                //{
                //    Name = r.Name,
                //    AccountUUID = r.AccountUUID,
                //    Active = true,
                //    CreatedBy = requestingUser.UUID, DateCreated = DateTime.UtcNow,
                //    RoleUUID = r.UUID, RoleOperation = r.RoleOperation, RoleWeight = r.RoleWeight,
                //    ReferenceUUID = requestingUser.UUID,
                //    ReferenceType = requestingUser.UUIDType,
                //    TargetUUID = u.UUID,
                //    TargetType = u.UUIDType

                //    //ReferenceUUID = u.UUID,
                //    // ReferenceType = u.UUIDType,
                //    //TargetUUID = requestingUser.UUID,
                //    //TargetType = requestingUser.UUIDType
                //};
                //if (!context.Insert<UserRole>(ur))
                //    return ServiceResponse.Error(u.Name + " failed to add. ");

                var profile = context.GetAll<Models.Membership.Profile>().FirstOrDefault(w => w.UserUUID == u.UUID && w.AccountUUID == u.AccountUUID);
                if (profile == null)
                    return res;

                ur = new UserRole()
                {
                    Name = r.Name,
                    AccountUUID = r.AccountUUID,
                    Active = true,
                    CreatedBy = requestingUser.UUID,
                    DateCreated = DateTime.UtcNow,
                    RoleUUID = r.UUID,
                    RoleOperation = r.RoleOperation,
                    RoleWeight = r.RoleWeight,
                    ReferenceUUID = requestingUser.UUID,
                    ReferenceType = requestingUser.UUIDType,
                    TargetUUID = profile.UUID,
                    TargetType = profile.UUIDType
                    //ReferenceUUID = profile.UUID,
                    //ReferenceType = profile.UUIDType,
                    //TargetUUID = requestingUser.UUID,
                    //TargetType = requestingUser.UUIDType
                };
                if (!context.Insert<UserRole>(ur))
                    return ServiceResponse.Error(u.Name + " failed to add. ");
            }
            return res;
        }

        public ServiceResult BlockRole(string roleUUID, INode u, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();

            if (IsInRole(u.UUID, u.AccountUUID, roleUUID, false))
                return res;

            res = Get(roleUUID);
            if (res.Code != 200)
                return res;
            Role r = (Role)res.Result;
            //if the role doesn't match the account then the role
            //hasn't been created for the account so the user can not be added to it.
            //
            if (r.AccountUUID != requestingUser.AccountUUID)
                return ServiceResponse.Error("Invalid parameter");

            if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                var profile = context.GetAll<Models.Membership.Profile>().FirstOrDefault(w => w.UserUUID == u.UUID && w.AccountUUID == u.AccountUUID);
                if (profile == null)
                    return res;

                UserRole ur = new UserRole()
                {
                    Name = r.Name,
                    AccountUUID = r.AccountUUID,
                    Active = true,
                    CreatedBy = requestingUser.UUID,
                    DateCreated = DateTime.UtcNow,
                    RoleUUID = r.UUID,
                    RoleOperation = r.RoleOperation,
                    RoleWeight = r.RoleWeight,
                    ReferenceUUID = requestingUser.UUID,
                    ReferenceType = requestingUser.UUIDType,
                    //TargetUUID = profile.UUID,
                    //TargetType = profile.UUIDType
                };
                if (!context.Insert<UserRole>(ur))
                    return ServiceResponse.Error(u.Name + " failed to add. ");
            }
            return res;
        }

        public ServiceResult DeleteUserFromRole(string roleUUID, User u, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();

            if (u == null)
                return ServiceResponse.Error("User is null");

            res = Get(roleUUID);
            if (res.Code != 200)
                return res;
            Role r = (Role)res.Result;
            //if the role doesn't match the account then the role
            //hasn't been created for the account so the user can not be added to it.
            //
            if (r == null || r.AccountUUID != requestingUser.AccountUUID)
                return ServiceResponse.Error("Invalid roleUUID parameter");

            if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@USERUUID", u.UUID);
                parameters.Add("@ROLEUUID", roleUUID);
                parameters.Add("@ACCOUNTUUID", r.AccountUUID);
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    if (context.Delete<UserRole>("WHERE ReferenceUUID=@USERUUID AND RoleUUID=@ROLEUUID AND AccountUUID=@ACCOUNTUUID", parameters) == 0)
                        return ServiceResponse.Error(u.Name + " failed to remove from role. ");

                    var profile = context.GetAll<Models.Membership.Profile>().FirstOrDefault(w => w.UserUUID == u.UUID && w.AccountUUID == u.AccountUUID);
                    if (profile == null)
                        return res;

                    DynamicParameters profileParameters = new DynamicParameters();
                    profileParameters.Add("@PROFILEUUID", profile.UUID);
                    profileParameters.Add("@ROLEUUID", roleUUID);
                    profileParameters.Add("@ACCOUNTUUID", r.AccountUUID);

                    var t = context.Delete<UserRole>("WHERE ReferenceUUID=@PROFILEUUID AND RoleUUID=@ROLEUUID AND AccountUUID=@ACCOUNTUUID", profileParameters);
                }

                ////SQLITE
                ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
                //// object[] paramters = new object[] { rp.UserUUID, rp.RoleUUID, rp.AccountUUID };
                ////context.Delete<UserRole>("WHERE UserUUID=? AND RoleUUID=? AND AccountUUID=?", paramters);
                ////  context.Delete<UserRole>(rp);
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "DeleteUserFromRole");
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }
            return res;
        }

        public ServiceResult DeleteUsersFromRole(string roleUUID, List<User> users, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();
            foreach (User user in users)
            {
                ServiceResult delRes = DeleteUserFromRole(roleUUID, user, requestingUser);
                if (delRes.Code != 200)
                {
                    msg.AppendLine(delRes.Message);
                    res.Code = 500;
                    res.Status = "ERROR";
                }
            }

            res.Message = msg.ToString();
            return res;
        }

        public List<UserRole> GetAssignedRoles(string userUUID, string accountUUID)
        {
            IEnumerable<UserRole> assignedRoles = new List<UserRole>();
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                assignedRoles = context.GetAll<UserRole>()
                                    ?.Where(urw => urw.ReferenceUUID == userUUID &&
                                            urw.AccountUUID == accountUUID && urw.Deleted == false);
            }
            return assignedRoles.ToList();
        }

        public List<Role> GetRolesForUser(string userUUID, string accountUUID)
        {
            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    List<Role> userRoles = context.GetAll<Role>()?.Where(w => w.AccountUUID == accountUUID && w.Deleted == false)
                                                    .Join(context.GetAll<UserRole>()?
                                                    .Where(w => w.ReferenceUUID == userUUID &&
                                                           w.AccountUUID == accountUUID && w.Deleted == false),
                                                        role => role.UUID,
                                                        userRole => userRole.RoleUUID,
                                                        (role, userRole) => new { role, userRole }
                                                    ).Select(s => s.role).ToList();
                    return userRoles;
                }
            }
            catch (Exception ex)
            {
                string msg = ex.DeserializeException();
                _logger.InsertError(msg, "RoleManager", "GetRolesForUser");
                Debug.Assert(false, ex.Message);
            }
            return new List<Role>();
        }

        /// <summary>
        /// Pulls from UsersInRoles
        /// </summary>
        /// <param name="userUUID"></param>
        /// <param name="accountUUID"></param>
        /// <returns></returns>
        public List<UserRole> GetUserRoles(string userUUID, string accountUUID)
        {
            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    List<UserRole> userRoles = context.GetAll<UserRole>()?.Where(w => w.AccountUUID == accountUUID &&
                        w.ReferenceUUID == userUUID).ToList();

                    return userRoles;
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "GetUserRoles");
                Debug.Assert(false, ex.Message);
            }
            return new List<UserRole>();
        }

        #endregion UserRole  TODO refactor into icrud class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #region User and User authorization

        /// <summary>
        /// Validate by individual object
        ///
        /// </summary>
        /// <param name="dataItem"></param>
        /// <param name="requestingUser"></param>
        /// <param name="verb"></param>
        /// <param name="isSensitiveData"></param>
        /// <returns></returns>
        public bool DataAccessAuthorized(INode dataItem, User requestingUser, string verb, bool isSensitiveData)
        {
            if (dataItem == null || requestingUser == null || requestingUser.Banned || requestingUser.LockedOut)
                return false;

            if (dataItem.CreatedBy == requestingUser.UUID)
                return true;

            if (requestingUser.SiteAdmin)
                return true;

            if (isSensitiveData)
                return false;

            if (dataItem.AccountUUID == requestingUser.AccountUUID)
            {
                if (UserInAuthorizedRole(requestingUser, dataItem.RoleWeight, dataItem.RoleOperation))
                    return true;
            }

            if (dataItem.AccountUUID == SystemFlag.Default.Account)
            {
                switch (verb?.ToLower())
                {
                    case "get":
                        if (dataItem.Private)
                        {  //todo check if user is in group.
                            return false;
                        }
                        return true;

                    case "delete":
                        return false;

                    case "post":

                        return false;

                    case "put":
                        return false;

                    case "patch":
                        return false;
                }
            }

            //if (  dataItem.AccountUUID == requestingUser.AccountUUID ) {
            //     return UserInAuthorizedRole( requestingUser, dataItem.RoleWeight, dataItem.RoleOperation);
            //}

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="requestingUser"></param>
        /// <param name="allowSensitiveData"></param>
        /// <param name="allowPrivateData"></param>
        /// <returns></returns>
        public bool DataAccessAuthorized(string type, User requestingUser, bool allowSensitiveData, bool allowPrivateData)
        {
            if (string.IsNullOrWhiteSpace(type) == true || requestingUser == null || requestingUser.Banned || requestingUser.LockedOut)
                return false;

            if (requestingUser.SiteAdmin)
                return true;

            if (allowSensitiveData)
                return false;

            if (allowPrivateData)
                return false;

            return true;
        }

        /// <summary>
        /// return users assigned to a role
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public List<User> GetUsersInRole(string roleUUID, string accountUUID)
        {
            List<User> members;
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                members = context.GetAll<UserRole>()
                        .Where(rrw => rrw.RoleUUID == roleUUID &&
                               rrw.AccountUUID == accountUUID)
                        .Join(
                            context.GetAll<User>()?.Where(uw => uw.Deleted == false),
                            role => role.ReferenceUUID,
                            users => users.UUID,
                            (role, users) => new { role, users }
                        )
                        .Select(s => s.users)
                        .ToList();
            }
            return members;
        }

        public List<User> GetUsersNotInRole(string roleUUID, string accountUUID)
        {
            List<User> usersInAccount;
            //GetAccountMembers
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                usersInAccount = context.GetAll<AccountMember>()?.Where(w => w.AccountUUID == accountUUID)
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

            List<User> usersInRole = GetUsersInRole(roleUUID, accountUUID);

            if (usersInAccount == null)
                return new List<User>();

            return usersInAccount.Except(usersInRole).ToList();
        }

        public bool IsInRole(string referenceUUID, string category, string categoryRoleName)
        {
            bool isInRole = false;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                List<Role> roles = context.GetAll<Role>().Where(w => w.Category.EqualsIgnoreCase(category) &&
                                                                     w.CategoryRoleName.EqualsIgnoreCase(categoryRoleName) &&
                                                                     w.AccountUUID == _requestingUser.AccountUUID &&
                                                                     w.Deleted == false).ToList();
                if (roles == null || roles.Count == 0)
                    return false;

                foreach (var role in roles)
                {
                    isInRole = context.GetAll<UserRole>().Any(w => w.Deleted == false &&
                                    w.RoleUUID == role.UUID &&
                                   w.ReferenceUUID == referenceUUID &&
                                   roles.Any(a => a.UUID == w.RoleUUID)
                                   );

                    if (isInRole == true)
                        return isInRole;
                }
            }
            return isInRole;
        }

        //    return false;
        //}
        public bool IsInRole(string referenceUUID, string roleName, string targetUUID, string targetType)
        {
            List<Role> roles = (List<Role>)Search(roleName, "member");
            if (roles == null || roles.Count == 0)
                return false;
            bool isInRole = false;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                foreach (var role in roles)
                {
                    isInRole = context.GetAll<UserRole>().Any(w => w.RoleUUID == role.UUID &&
                                   w.ReferenceUUID == referenceUUID &&
                                   w.TargetType.EqualsIgnoreCase(targetType) &&
                                   w.TargetUUID == targetUUID &&
                                   w.Name.EqualsIgnoreCase(roleName));
                    if (isInRole == true)
                        return isInRole;
                }
            }
            return isInRole;
        }

        //    if (GetUsersInRole(r.FirstOrDefault().UUID, accountUUID).Any(w => w.UUID == userUUID))
        //        return true;
        public bool IsInRole(string referenceUUID, string accountUUID, string roleUUID, bool ignoreAccountFilter)
        {
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                if (ignoreAccountFilter)
                {
                    return context.GetAll<UserRole>().Any(rpw => rpw.ReferenceUUID == referenceUUID &&
                                rpw.RoleUUID == roleUUID);
                }

                return context.GetAll<UserRole>().Any(rpw => rpw.ReferenceUUID == referenceUUID &&
                                                   rpw.AccountUUID == accountUUID &&
                                                   rpw.RoleUUID == roleUUID);
            }
        }

        public bool IsSiteAdmin(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;
            userName = userName.ToLower();

            if (_siteAdmins == null)
                return false;

            return _siteAdmins.Contains(userName);
        }

        /// <summary>
        /// This verifies the permission is still assigned to the role, and that the user is in the role.
        /// </summary>
        /// <param name="userUUID"></param>
        /// <param name="accountUUID"></param>
        /// <param name="requestPath"></param>
        /// <returns></returns>
        public bool IsUserRequestAuthorized(string userUUID, string accountUUID, string requestPath)
        {
            IEnumerable<UserRole> userRoles = new List<UserRole>();
            IEnumerable<RolePermission> rolePermissions = new List<RolePermission>();
            List<Permission> permissions;

            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                //1. Get the roles the user is assigned
                userRoles = context.GetAll<UserRole>()?.Where(urw => urw.ReferenceUUID == userUUID && urw.AccountUUID == accountUUID && urw.Deleted == false);

                if (userRoles == null || !userRoles.Any())
                    return false;

                //2. Get the permissions for the role
                rolePermissions = context.GetAll<RolePermission>()?.Where(
                    rpw => rpw.AccountUUID == accountUUID
                    && userRoles.Any(ura => ura.RoleUUID == rpw.RoleUUID) //2.A. Filter the role permissions based on the users roles (return only RolePermissions where the user is in it).
                    ).DistinctBy(db => db.PermissionUUID);

                if (rolePermissions == null || !rolePermissions.Any())
                    return false;

                //Get permissions  for the account and path and distinct Request
                permissions = context.GetAll<Permission>()?.Where(
                    pw => (pw.AccountUUID == accountUUID &&//Whether the permission was created by the account or if it was a system created permission doesn't matter, we'll match the true permission below.
                    pw.Request == requestPath && //get the permission for the request being made
                    pw.Deleted == false
                     && rolePermissions.Any(ry => ry.PermissionUUID == pw.UUID) //now match the permission that was assigned to the account (above) in the resulting permissions.
                    )).DistinctBy(db => db.Request).ToList();
            }

            if (permissions == null || permissions.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Used in ApiAuthorization.
        /// </summary>
        /// <param name="requestingUser"></param>
        /// <param name="roleWeight"></param>
        /// <param name="weightOperator"></param>
        /// <returns></returns>
        public bool UserInAuthorizedRole(User requestingUser, int roleWeight, string weightOperator)
        {
            if (roleWeight == 0)
                return true;

            if (requestingUser == null || roleWeight < 0 || string.IsNullOrWhiteSpace(weightOperator))
                return false;

            _requestingUser = requestingUser;
            List<Role> allowedRoles = new List<Role>();
            switch (weightOperator)
            {
                case ">="://role weight greater or equal to
                    allowedRoles = this.GetRoles(requestingUser.AccountUUID).Where(w => w.RoleWeight >= roleWeight)?.ToList();
                    break;

                case "=":
                    allowedRoles = this.GetRoles(requestingUser.AccountUUID)
                        .Where(w => w.RoleWeight == roleWeight)?.ToList();
                    break;

                case ">":
                    allowedRoles = this.GetRoles(requestingUser.AccountUUID)
                        .Where(w => w.RoleWeight > roleWeight)?.ToList();
                    break;

                case "<=":
                    allowedRoles = this.GetRoles(requestingUser.AccountUUID)
                        .Where(w => w.RoleWeight <= roleWeight)?.ToList();
                    break;

                case "<":
                    allowedRoles = this.GetRoles(requestingUser.AccountUUID)
                        .Where(w => w.RoleWeight < roleWeight).ToList();
                    break;
            }
            if (allowedRoles.Count == 0)
                return false;
            ProfileMemberManager _profileMemberManager = new ProfileMemberManager(this._dbConnectionKey, "");
            ProfileMember memberProfile = new ProfileMember();
            var tmp = _profileMemberManager.GetMemberProfile(requestingUser.UUID, requestingUser.AccountUUID);
            if (tmp != null)
                memberProfile = (ProfileMember)tmp;
            //Check if user is in role
            foreach (Role r in allowedRoles)
            {
                //in this case we're ignoring the account because if a users signs up through a partner
                // they will have a different account, but we want' them to be able to block users across accounts.
                if (string.IsNullOrWhiteSpace(memberProfile.ProfileUUID) == false &&
                     IsInRole(memberProfile.ProfileUUID, requestingUser.AccountUUID, r.UUID, true))
                    return true;

                if (IsInRole(requestingUser.UUID, requestingUser.AccountUUID, r.UUID, true))
                    return true;
            }
            return false;
        }

        //public bool IsUserInRole(string userUUID, string roleName, string accountUUID)
        //{
        //    List<Role> r = (List<Role>)Search(roleName, "member");
        //    if (r == null || r.Count == 0)
        //        return false;

        #endregion User and User authorization

        #region Install Methods  TODO refactor bulk inserts (see importjson function in appmanager).

        private readonly string[] _roles = new string[] { "owner", "admin", "manager", "employee", "patient", "customer" };

        //Generating permissions
        private readonly string[] _verbs = new string[] { "insert", "update", "delete", "purge", "get" };

        public string[] DefaultRoles
        {
            get { return _roles; }
        }

        public ServiceResult InsertDefaults(string AccountUUID, string appType)
        {
            this._runningInstall = true;

            ServiceResult res = InsertDefaultRoles(AccountUUID, appType);

            if (res.Status != "OK")
                return res;

            res = InsertDefaultPermissions(AccountUUID, appType);

            if (res.Status != "OK")
                return res;

            Role ownerRole = (Role)GetRole("owner", AccountUUID);

            res = AssignDefaultRolePermissions(AccountUUID, appType, ownerRole.UUID);

            if (res.Status != "OK")
                return res;

            //this should be 52 for Owner role
            List<Permission> availablePermissions = GetAvailablePermissions(ownerRole.UUID, AccountUUID);
            foreach (Permission p in availablePermissions)
            {
                RolePermission rp = new RolePermission()
                {
                    AccountUUID = AccountUUID,
                    RoleUUID = ownerRole.UUID,
                    RoleWeight = RoleFlags.MemberRoleWeights.Member,
                    RoleOperation = ">=",
                    PermissionUUID = p.UUID,
                    DateCreated = DateTime.UtcNow,
                    Name = ownerRole.Name + p.Name,
                    Deleted = false,
                    Active = true
                };

                res = AddRolePermission(rp);
                if (res.Code != 200)
                    return res;
            }
            this._runningInstall = false;
            return res;
        }

        protected ServiceResult AssignDefaultRolePermissions(string AccountUUID, string appType, string ownerRoleUUID)
        {
            if (string.IsNullOrWhiteSpace(AccountUUID))
            {
                Debug.Assert(false, "Account id is empty.");
                return ServiceResponse.Error("Account id is empty.");
            }
            try
            {
                List<Role> tmpRoles = this.GetRoles(AccountUUID)?.Where(w => w.AppType == appType).ToList();
                var filter = new DataFilter();
                filter.PageResults = false;
                List<Permission> permissions = this.GetAccountPermissions(AccountUUID, ref filter).Where(w => w.AppType == appType).ToList();
                List<string> matrix = GetPermissionsMatrix();

                foreach (string permissionsSet in matrix)
                {
                    string[] sets = permissionsSet.Split('|');
                    if (sets.Length == 0) { continue; }

                    string table = sets[0].ToLower().Trim();

                    for (int roleIndex = 1; roleIndex < sets.Length; roleIndex++)
                    {
                        string[] roleTokens = sets[roleIndex].Split('{');
                        string role = roleTokens[0].ToLower().Trim();
                        Role r = tmpRoles.FirstOrDefault(w => w.Name.ToLower() == role && w.AccountUUID == AccountUUID && w.AppType?.ToLower() == appType);
                        if (r == null)
                        {
                            var tmp = this.GetRole(role, SystemFlag.Default.Account);
                            if (tmp == null)
                                continue;
                            r = (Role)tmp;
                            r = new Role()
                            {
                                AccountUUID = AccountUUID,
                                Name = role,
                                DateCreated = DateTime.Now,
                                AppType = appType,
                                CreatedBy = _requestingUser?.UUID,
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now,
                            };
                            using (var context = new GreenWerxDbContext(_dbConnectionKey))
                            {
                                context.Insert<Role>(r);
                            }
                        }
                        string[] tmpVerbs = roleTokens[1].Split(',');
                        foreach (string verb in tmpVerbs)
                        {
                            string name = table.ToLower() + "." + verb.ToLower();
                            string key = CreateKey(name, verb, appType, AccountUUID);

                            Permission p = permissions.FirstOrDefault(w => w.Key == key && w.AccountUUID == AccountUUID && w.AppType?.ToLower() == appType?.ToLower());

                            if (p == null) { continue; }

                            RolePermission dbRp;
                            using (var context = new GreenWerxDbContext(_dbConnectionKey))
                            {
                                dbRp = context.GetAll<RolePermission>()?.FirstOrDefault(w => w.RoleUUID == r.UUID && w.PermissionUUID == p.UUID && w.AccountUUID == AccountUUID);

                                if (dbRp != null) { continue; }

                                RolePermission rp = new RolePermission()
                                {
                                    Name = r.Name + p.Name,
                                    AccountUUID = AccountUUID,
                                    PermissionUUID = p.UUID,
                                    RoleUUID = r.UUID,
                                    UUID = Guid.NewGuid().ToString("N"),
                                    DateCreated = DateTime.UtcNow
                                };
                                if (string.IsNullOrWhiteSpace(rp.AccountUUID) || string.IsNullOrWhiteSpace(rp.PermissionUUID) || string.IsNullOrWhiteSpace(rp.RoleUUID))
                                    continue;

                                if (!context.Insert<RolePermission>(rp))
                                {
                                    _logger.InsertError("Failed to insert:" + rp.Name, "RoleManager", "AssingDefaultRolePermissions");
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(ownerRoleUUID) && !RolePermissionExists(ownerRoleUUID, AccountUUID, p.UUID))
                                {
                                    RolePermission ownerRP = (RolePermission)rp.Clone();
                                    ownerRP.RoleUUID = ownerRoleUUID;
                                    ownerRP.DateCreated = DateTime.UtcNow;
                                    ownerRP.AccountUUID = AccountUUID;
                                    context.Insert<RolePermission>(ownerRP);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "AssingDefaultRolePermissions");
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK();
        }

        protected ServiceResult InsertDefaultPermissions(string AccountUUID, string appType)
        {
            List<string> tables;
            using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
            {
                tables = DatabaseEx.GetTableNames();

                foreach (string verb in _verbs)
                {
                    foreach (string type in tables)
                    {
                        string name = type.ToLower() + "." + verb.ToLower();
                        string key = CreateKey(name, verb, appType, AccountUUID);

                        if (this.PermissionExists(key))
                            continue;

                        Permission p = new Permission()
                        {
                            Action = verb,
                            Active = true,
                            Type = type,
                            AccountUUID = AccountUUID,
                            AppType = appType,
                            Persists = true,
                            StartDate = DateTime.UtcNow,
                            Weight = 0,
                            Key = key,
                            Name = name,
                            Deleted = false,
                            DateCreated = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow
                        };

                        if (!context.Insert<Permission>(p))
                            _logger.InsertError("Failed to insert:" + p.Name, "RoleManager", "InsertDefaultPermissions:" + AccountUUID);
                    }
                }
                context.SaveChanges();
            }
            return new ServiceResult() { Code = 200, Status = "OK" };
        }

        /// <summary>
        /// This add barebone roles to the system. Use the
        /// admin panel to create more.
        /// BACKLOG: add ability to create template for export/import..
        /// </summary>
        /// <param name="account"></param>
        /// <param name="appType"></param>
        /// <returns></returns>
        protected ServiceResult InsertDefaultRoles(string AccountUUID, string appType)
        {
            try
            {
                string lastGuid = "";
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    foreach (string role in _roles)
                    {
                        if (context.GetAll<Role>().Any(w => w.Name == role && w.AppType == appType && w.AccountUUID == AccountUUID))
                            continue;

                        Role r = new Role()
                        {
                            Name = role,
                            AccountUUID = AccountUUID,
                            AppType = appType,
                            Persists = true
                        };
                        r.UUID = Guid.NewGuid().ToString("N");
                        r.UUParentID = lastGuid;
                        r.UUParentIDType = "Role";
                        lastGuid = r.UUID;
                        r.UUIDType = "Role";
                        r.DateCreated = DateTime.UtcNow;
                        r.StartDate = DateTime.UtcNow;
                        r.EndDate = DateTime.UtcNow;

                        if (!context.Insert<Role>(r))
                            _logger.InsertError("Failed to insert:" + role, "RoleManager", "InsertDefaultRoels:" + AccountUUID);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "InsertDefaultRoels:" + AccountUUID);
                return ServiceResponse.Error(ex.Message);
            }
            return new ServiceResult() { Code = 200, Status = "OK" };
        }

        private List<string> GetPermissionsMatrix()
        {
            //Permissions Matrix.. work in progress...
            List<string> matrix = new List<string>();
            matrix.Add("Notifications |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("SideAffects   |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Reminders     |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Anatomy       |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Symptoms      |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("AnatomyTags   |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("ReminderRules |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Ballasts      |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Bulbs         |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Fans          |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Filters       |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Vehicles      |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("SymptomsLog   |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Measurements  |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Plants        |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Pumps         |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Strains       |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("DoseLogs      |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("UnitsOfMeasure|Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("UsersInAccount|Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get  } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Addresses     |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get  } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("StatusMessages|Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get  } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("ProfileLogs   |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("Accounts      |Customer{insert,update,delete,get }|Patient{insert,update,delete,get }|Employee{insert,update,delete,get }|Manager{insert,update,delete,get }|Admin{insert,update,delete,purge,get  } |Owner{insert,update,delete,purge,get }                           ");
            matrix.Add("Credentials   |Customer{insert,update,delete,get }|Patient{insert,update,delete,get }|Employee{insert,update,delete,get }|Manager{insert,update,delete,get }|Admin{insert,update,delete,purge,get  } |Owner{insert,update,delete,purge,get }                           ");
            matrix.Add("Vendor        |Customer{insert,update,delete,get }|Patient{insert,update,delete,get }|Employee{insert,update,delete,get  }|Manager{insert,update,delete,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }                           ");
            matrix.Add("Users         |Customer{insert,update,delete,get }|Patient{insert,update,delete,get }|Employee{insert,update,delete,get }|Manager{insert,update,delete,get }|Admin{insert,update,delete,purge,get  } |Owner{insert,update,delete,purge,get }                           ");
            matrix.Add("LineItems       |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("Roles           |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("UserPermissions |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("UsersInRoles    |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("RolePermissions |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("UserSessions    |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("Permissions     |Manager{insert,update,delete,get } |Admin{insert,update,delete,purge,get }|Owner{insert,update,delete,purge,get }                                                                                                                                     ");
            matrix.Add("Products      |Customer{insert,update,delete,purge,get }|Patient{insert,update,delete,purge,get }|Employee{insert,update,delete,purge,get  }|Manager{insert,update,delete,purge,get }|Admin{insert,update,delete,purge,get } |Owner{insert,update,delete,purge,get }   ");
            matrix.Add("AppInfo           |Admin{insert,update,delete,get } |Owner{insert,update,delete,purge,get }                                                                                                                                                                            ");
            matrix.Add("AccessLog |Admin{get} |Owner{insert,update,delete,purge,get }                                                                                                                                                                                                  ");
            matrix.Add("SystemLog         |Owner{insert,update,delete,purge,get }                                                                                                                                                                                                              ");
            matrix.Add("Settings          |Owner{insert,update,delete,purge,get }                                                                                                                                                                                                              ");

            return matrix;
        }

        #endregion Install Methods  TODO refactor bulk inserts (see importjson function in appmanager).

        #region Blocked Roles

        public ServiceResult AddBlockedRole(BlockedRole br, User requestingUser)
        {
            ServiceResult res = Get(br.RoleUUID);

            if (res.Code != 200)
                return res;
            Role r = (Role)res.Result;
            //if the role doesn't match the account then the role
            //hasn't been created for the account so the user can not be added to it.
            //
            if (r.AccountUUID != requestingUser.AccountUUID)
                return ServiceResponse.Error("Invalid parameter");

            if (!_runningInstall && !this.DataAccessAuthorized(r, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                var currentBR = context.GetAll<BlockedRole>().FirstOrDefault(w => w.TargetUUID == r.UUID && w.TargetType == r.UUIDType &&
                                                    w.AccountUUID == requestingUser.AccountUUID);

                if (currentBR != null)
                    return ServiceResponse.OK("", currentBR);

                BlockedRole ur = new BlockedRole()
                {
                    Name = r.Name,
                    AccountUUID = r.AccountUUID,
                    Active = true,
                    CreatedBy = requestingUser.UUID,
                    DateCreated = DateTime.UtcNow,
                    RoleUUID = r.UUID,
                    RoleOperation = r.RoleOperation,
                    RoleWeight = r.RoleWeight,
                    ReferenceUUID = requestingUser.UUID,
                    ReferenceType = requestingUser.UUIDType,
                    TargetUUID = r.UUID,
                    TargetType = r.UUIDType
                };
                if (!context.Insert<BlockedRole>(ur))
                    return ServiceResponse.Error(r.Name + " failed to add. ");

                return ServiceResponse.OK("", ur);
            }
        }

        public ServiceResult DeleteBlockedRole(BlockedRole br, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();

            if (br == null)
                return ServiceResponse.Error("User is not in the role.");

            if (!_runningInstall && !this.DataAccessAuthorized(br, _requestingUser, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    var count = context.Delete(br);
                }

                ////SQLITE
                ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
                //// object[] paramters = new object[] { rp.UserUUID, rp.RoleUUID, rp.AccountUUID };
                ////context.Delete<UserRole>("WHERE UserUUID=? AND RoleUUID=? AND AccountUUID=?", paramters);
                ////  context.Delete<UserRole>(rp);
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "DeleteBlockedRole");
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }
            return res;
        }

        public List<BlockedRole> GetBlockedRoles(string userUUID, string accountUUID)
        {
            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    List<BlockedRole> userRoles = context.GetAll<BlockedRole>()?.Where(w => w.AccountUUID == accountUUID &&
                        w.ReferenceUUID == userUUID).ToList();

                    return userRoles;
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "GetUserRoles");
                Debug.Assert(false, ex.Message);
            }
            return new List<BlockedRole>();
        }

        #endregion Blocked Roles

        #region Blocked Users

        public ServiceResult AddBlockedUser(string targetUserUUID, User requestingUser)
        {
            var list = new List<BlockedUser>();
            ServiceResult res = ServiceResponse.OK();
            using (var context = new GreenWerxDbContext(_dbConnectionKey))
            {
                var targetUser = context.GetAll<User>().FirstOrDefault(w => w.UUID == targetUserUUID);

                if (targetUser == null)
                    return ServiceResponse.Error("User not found.");

                var blockRole = context.GetAll<Role>()
                                        .FirstOrDefault(w =>
                                                w.Category.EqualsIgnoreCase("block") &&
                                                w.CategoryRoleName == "" &&
                                                w.AccountUUID == requestingUser.AccountUUID &&
                                                w.Deleted == false);

                if (blockRole == null)
                    return ServiceResponse.Error("Blocking role doesn't exist.");

                ////if the role doesn't match the account then the role
                ////hasn't been created for the account so the user can not be added to it.
                ////
                if (blockRole.AccountUUID != requestingUser.AccountUUID)
                    return ServiceResponse.Error("Role doesn't exist for your account.");

                if (GetBlockedUsers(requestingUser.UUID, requestingUser.AccountUUID)
                                        .Any(w => w.RoleUUID == blockRole.UUID &&
                                             w.TargetUUID == targetUser.UUID &&
                                             w.TargetType == targetUser.UUIDType))
                {
                    return ServiceResponse.Error("User is already blocked.");
                }

                ////block the user
                BlockedUser ur = new BlockedUser()
                {
                    Name = blockRole.Name,
                    AccountUUID = blockRole.AccountUUID,
                    Active = true,
                    CreatedBy = requestingUser.UUID,
                    DateCreated = DateTime.UtcNow,
                    RoleUUID = blockRole.UUID,
                    RoleOperation = blockRole.RoleOperation,
                    RoleWeight = blockRole.RoleWeight,
                    ReferenceUUID = requestingUser.UUID,
                    ReferenceType = requestingUser.UUIDType,
                    TargetUUID = targetUser.UUID,
                    TargetType = targetUser.UUIDType
                };
                if (!context.Insert<BlockedUser>(ur))
                    return ServiceResponse.Error("Block " + targetUser.Name + " failed. ");

                list.Add(ur);
                var profile = context.GetAll<Models.Membership.Profile>().FirstOrDefault(w => w.UserUUID == targetUser.UUID && w.AccountUUID == targetUser.AccountUUID);
                if (profile == null)
                    return res;
                //Block the users profile
                ur = new BlockedUser()
                {
                    Name = blockRole.Name,
                    AccountUUID = blockRole.AccountUUID,
                    Active = true,
                    CreatedBy = requestingUser.UUID,
                    DateCreated = DateTime.UtcNow,
                    RoleUUID = blockRole.UUID,
                    RoleOperation = blockRole.RoleOperation,
                    RoleWeight = blockRole.RoleWeight,
                    ReferenceUUID = requestingUser.UUID,
                    ReferenceType = requestingUser.UUIDType,
                    TargetUUID = profile.UUID,
                    TargetType = profile.UUIDType
                };
                if (!context.Insert<BlockedUser>(ur))
                    return ServiceResponse.Error("Block " + targetUser.Name + " failed to add profile. ");

                list.Add(ur);
                return ServiceResponse.OK("", list);
            }
        }

        public ServiceResult DeleteBlockedUser(string targetUserUUID, User requestingUser)
        {
            ServiceResult res = ServiceResponse.OK();

            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    var targetUser = context.GetAll<User>().FirstOrDefault(w => w.UUID == targetUserUUID);

                    if (targetUser == null)
                        return ServiceResponse.Error("User not found.");

                    var blockRole = context.GetAll<Role>()
                                    .FirstOrDefault(w =>
                                            w.Category.EqualsIgnoreCase("block") &&
                                            w.CategoryRoleName == "" &&
                                            w.AccountUUID == requestingUser.AccountUUID &&
                                            w.Deleted == false);

                    if (blockRole == null)
                        return ServiceResponse.Error("Blocking role doesn't exist.");

                    var profile = context.GetAll<Models.Membership.Profile>().FirstOrDefault(w => w.UserUUID == targetUser.UUID && w.AccountUUID == targetUser.AccountUUID);

                    var br = GetBlockedUsers(requestingUser.UUID, requestingUser.AccountUUID)
                            ?.Where(w => w.RoleUUID == blockRole.UUID &&
                                (w.TargetUUID == targetUser.UUID ||  //get blocked user
                                    w.TargetUUID == profile.UUID)       // get blocked profile
                                                                        //&& w.TargetType == targetUser.UUIDType
                            );

                    if (br == null || br.Any() == false)
                        return ServiceResponse.Error("User is not in the role.");

                    foreach (var blockedUser in br)
                    {
                        if (!_runningInstall && !this.DataAccessAuthorized(blockedUser, _requestingUser, "PATCH", false))
                            return ServiceResponse.Error("You are not authorized this action.");

                        //delete the blocked user
                        var count = context.Delete(blockedUser);
                    }

                    ////delete the blocked profile
                    //var profile = context.GetAll<Models.Membership.Profile>().FirstOrDefault(w => w.UserUUID == targetUser.UUID && w.AccountUUID == targetUser.AccountUUID);
                    //if (profile == null)
                    //    return res;

                    //var bpr = GetBlockedUsers(requestingUser.UUID, requestingUser.AccountUUID)
                    //        ?.FirstOrDefault(w => w.RoleUUID == blockRole.UUID &&
                    //            w.TargetType == profile.UUIDType &&
                    //            w.TargetUUID == profile.UUID
                    //        );
                    // count = context.Delete(bpr);
                }

                ////SQLITE
                ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
                //// object[] paramters = new object[] { rp.UserUUID, rp.RoleUUID, rp.AccountUUID };
                ////context.Delete<UserRole>("WHERE UserUUID=? AND RoleUUID=? AND AccountUUID=?", paramters);
                ////  context.Delete<UserRole>(rp);
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "DeleteBlockedUser");
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Exception occured while deleting this record.");
            }
            return res;
        }

        /// <summary>
        /// Returns users blocked by the parameters (returns targets).
        /// </summary>
        /// <param name="userUUID"></param>
        /// <param name="accountUUID"></param>
        /// <returns></returns>
        public List<BlockedUser> GetBlockedUsers(string userUUID, string accountUUID)
        {
            try
            {
                using (GreenWerxDbContext context = new GreenWerxDbContext(_dbConnectionKey))
                {
                    List<BlockedUser> userRoles = context.GetAll<BlockedUser>()?.Where(w => w.AccountUUID == accountUUID &&
                        w.ReferenceUUID == userUUID
                        ).ToList();

                    userRoles.ForEach(x =>
                    {
                        // if (x.TargetType.EqualsIgnoreCase("profile"))
                        // {
                        x.Name = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == x.TargetUUID)?.Name;
                        // }
                    });
                    return userRoles;
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "RoleManager", "GetBlockedUsers");
                Debug.Assert(false, ex.Message);
            }
            return new List<BlockedUser>();
        }

        #endregion Blocked Users
    }
}