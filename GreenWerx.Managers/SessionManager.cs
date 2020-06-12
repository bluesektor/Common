// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Linq;
using System.Reflection;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers
{
    public class SessionManager
    {
        private readonly AppManager _app;
        private readonly string _connectionKey;
        private readonly SystemLogger _logger;

        public SessionManager(string connectionKey)
        {
            _connectionKey = connectionKey;
            _logger = new SystemLogger(connectionKey);
           
           
            _app = new AppManager(_connectionKey, "web", "");
          // _app.Installing
            //  if (Globals.Application.Status == "REQUIRES_INSTALL" || Globals.Application.Status == "INSTALLING")

            SessionLength = GetSessionLength();
        }

        public int SessionLength { get; set; }

        public int ClearExpiredSessions(int minutes)
        {
            if (minutes > 0)
            {
                minutes = minutes * -1;
            }

            int deletedSessions = 0;
            DateTime expireDate = DateTime.UtcNow.AddMinutes(minutes);
            try
            {
                List<UserSession> sessions;

                using (var context = new GreenWerxDbContext(_connectionKey))
                {
                    sessions = context.GetAll<UserSession>().
                             Where(w => w.Issued < expireDate &&
                                   w.IsPersistent == false)?.ToList();
                }
                if (sessions == null)
                    return 0;

                foreach (UserSession s in sessions)
                {
                    if (DeleteSession(s.AuthToken))
                        deletedSessions++;
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "SessionManager", MethodInfo.GetCurrentMethod().Name);
            }
            return deletedSessions;
        }

        public string CreateJwt(string secretKey, User user, string issuer, DateTime expires)
        {
            if (user == null)
                return string.Empty;

            string profileUUID = string.Empty;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var profile = context.GetAll<Profile>()?.FirstOrDefault(sw => sw?.AccountUUID == user.AccountUUID && sw?.UserUUID == user.UUID);
                profileUUID = profile?.UUID;
            }

            var payload = new JwtClaims();
            if (string.IsNullOrWhiteSpace(profileUUID))
                payload.aud = user.UUID + "." + user.AccountUUID;
            else
                payload.aud = user.UUID + "." + user.AccountUUID + "." + profileUUID;

            payload.iss = issuer;
            payload.jti = Guid.NewGuid().ToString();
            payload.exp = expires.ConvertToUnixTimestamp().ToString();
            payload.expires = expires;
            RoleManager roleManager = new RoleManager(this._connectionKey);
            List<Role> userRoles = roleManager.GetRolesForUser(user.UUID, user.AccountUUID);
            List<Role> profileRoles = roleManager.GetRolesForUser(profileUUID, user.AccountUUID);

            userRoles.AddRange(profileRoles);
            if (userRoles.Count > 0)
            {
                payload.roleWeights = userRoles.Select(s => s.RoleWeight.ToString()).Aggregate((current, next) => current + "," + next);
                payload.roleNames = userRoles.Select(s => s.Name?.ToUpper()).Aggregate((current, next) => current + "," + next);
            }

            //NOTE: This uses bouncy castle portable, specifically version 1.8.1. Runtime error occures with other versions.
            string token = JWT.JsonWebToken.Encode(payload, secretKey, JWT.JwtHashAlgorithm.HS256);

            return token;

            #region Unsecured jwt example

            ////var hmac = new HMACSHA256();
            ////var header = "{ \"alg\": \"HS256\",  \"typ\": \"JWT\"  }";
            //////Some PayLoad that contain information about the  customer
            ////var payload = "{ \"userUUID\": \"" + userUUID + "\",  \"scope\": \"http://test.com\"  }";
            ////var secToken = hmac.ComputeHash(Encoding.UTF8.GetBytes(header + payload));
            ////// Token to String so you can use it in your client

            ////Debug.Assert(false, "TODO NEED TO REMOVE THE END PADDING = OR == ");
            ////var tokenString = Convert.ToBase64String(Encoding.UTF8.GetBytes(header)) + "." +
            ////                    Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)) + "." +
            ////                    Convert.ToBase64String(secToken);

            ////return tokenString;

            #endregion Unsecured jwt example
        }

        /// <summary>
        /// Delete tokens for the specific deleted user
        /// </summary>
        /// <param name="userUUID"></param>
        /// <returns>true for successful delete</returns>
        public bool DeleteByUserId(string userUUID, string accountUUID, bool deleteIfPersisted)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@USERID", userUUID);
            parameters.Add("@ACCOUNTID", accountUUID);

            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                if (deleteIfPersisted)
                    return context.Delete<UserSession>("WHERE UserUUID=@USERID AND AccountUUID=@ACCOUNTID AND IsPersistent=0", parameters) > 0 ? true : false;
                else
                    return context.Delete<UserSession>("WHERE UserUUID=@USERID AND AccountUUID=@ACCOUNTID AND IsPersistent=0", parameters) > 0 ? true : false;
            }
        }

        /// <summary>
        /// Method to kill the provided token id.
        /// </summary>
        /// <param name="tokenId">true for successful delete</param>
        public bool DeleteSession(string tokenId)
        {
            DynamicParameters p = new DynamicParameters();
            p.Add("@TOKEN", tokenId);
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                return context.Delete<UserSession>("WHERE AuthToken=@TOKEN", p) > 0 ? true : false;
            }
        }

        public string GetAccountUUID(string sessionKey)
        {
            UserSession us = GetSession(sessionKey);
            User u = JsonConvert.DeserializeObject<User>(us.UserData);
            return u?.AccountUUID;
        }

        public UserSession GetSession(string authToken)//, bool validate = true)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(authToken))
                    return null;
                authToken = authToken.Replace("\"", "");
                using (var context = new GreenWerxDbContext(_connectionKey))
                {
                    var session = context.GetAll<UserSession>().OrderByDescending(ob => ob.Issued)?.FirstOrDefault(w => w.AuthToken == authToken);
                    return session;
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "SessionManager", "GetSession");
                Debug.Assert(false, ex.Message);
            }
            return null;
        }

        public UserSession GetSessionByEndToken(string endToken)//, bool validate = true)
        {
            try
            {
                using (var context = new GreenWerxDbContext(_connectionKey))
                {
                    return context.GetAll<UserSession>().OrderByDescending(ob => ob.Issued)?.FirstOrDefault(w => w.AuthToken.Contains(endToken));
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "SessionManager", "GetSession");
                Debug.Assert(false, ex.Message);
            }
            return null;
        }

        public UserSession GetSessionByUser(string userUUID, string accountUUID)
        {
            if (string.IsNullOrEmpty(userUUID))
                return new UserSession();
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                return context.GetAll<UserSession>()
                    .FirstOrDefault(w => w.UserUUID == userUUID
                    && w.AccountUUID == accountUUID);
            }
        }

        public UserSession GetSessionByUserName(string userName, string accountUUID)
        {
            if (string.IsNullOrEmpty(userName))
                return new UserSession();
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                return context.GetAll<UserSession>().
                    FirstOrDefault(w => (w.UserName?.EqualsIgnoreCase(userName) ?? false)
                                        && w.AccountUUID == accountUUID);
            }
        }

        /// <summary>
        /// TODO unit test this.
        /// </summary>
        /// <param name="jwt"></param>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public bool IsValidJwt(string jwt, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return false;

            try
            {
                string jsonPayload = "";
                jsonPayload = JWT.JsonWebToken.Decode(jwt, secretKey, false); // JWT.SignatureVerificationException
                JwtClaims claims = JsonConvert.DeserializeObject<JwtClaims>(jsonPayload);

                TimeSpan ts = claims.expires - DateTime.UtcNow;

                if (ts.TotalSeconds <= 0)
                    return false;

                if (claims.iss != _app.GetSetting("SiteDomain")?.Value)
                    return false;
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Method to validate token against expiry and existence in database.
        /// </summary>
        /// <param name="tokenId"></param>
        /// <returns></returns>
        public bool IsValidSession(string authToken)
        {
            if (string.IsNullOrEmpty(authToken))
                return false;

            UserSession us = null;
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                try
                {
                    us = context.GetAll<UserSession>().OrderByDescending(ob => ob.Issued)?.FirstOrDefault(w => w.AuthToken == authToken);
                }
                catch (Exception ex)
                {
                    _logger.InsertError(ex.Message, "SessionManager", "IsValidSession");
                    Debug.Assert(false, ex.Message);
                    return false;
                }

                if (us == null)
                    return false;

                double secondsLeft = us.Expires.Subtract(DateTime.UtcNow).TotalSeconds;

                if (us.IsPersistent == false && secondsLeft <= 0)
                {
                    context.Delete<UserSession>(us);
                    return false;
                }

                if (us.SessionLength != SessionLength)
                    us.SessionLength = this.SessionLength;
                //This adds time to the session so it won't cut them off while they're working (non idle user).
                us.Expires = DateTime.UtcNow.AddMinutes(us.SessionLength);
                context.Update<UserSession>(us);
            }
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="userUUID"></param>
        /// <param name="userData">This is the User objec serialized in json</param>
        /// <returns></returns>
        public UserSession SaveSession(string ipAddress, string userUUID, string accountUUID, string userData, bool persistSession = false)
        {
            //todo for persisted sessions get device info and make sure it matches incase someone steals the token
            UserSession us = new UserSession();
            us.Issued = DateTime.UtcNow;
            us.IsPersistent = persistSession;
            if (!persistSession)
            {
                us.SessionLength = GetSessionLength();
                us.Expires = DateTime.UtcNow.AddMinutes(us.SessionLength);
            }
            else
            {
                us.SessionLength = 315569520;
                us.Expires = DateTime.UtcNow.AddMinutes(us.SessionLength);
            }
            User u = string.IsNullOrWhiteSpace(userData) ? null : JsonConvert.DeserializeObject<User>(userData);
            string secret = _app.GetSetting("AppKey")?.Value;
            string issuer = _app.GetSetting("SiteDomain")?.Value;

            us.AuthToken = CreateJwt(secret, u, issuer, us.Expires);
            us.UserData = userData;
            us.UserUUID = userUUID;
            us.AccountUUID = accountUUID;
            return SaveSession(us);
        }

        /// <summary>
        /// Saves user session, expects the authtoken to
        /// not be null or empty. If authtoken is null it uses
        /// the user id instead of ip to generate it.
        /// </summary>
        /// <param name="us"></param>
        /// <returns></returns>
        public UserSession SaveSession(UserSession us)
        {
            if (us == null)
                return us;

            if (us.SessionLength != SessionLength)
                us.SessionLength = this.SessionLength;

            us.Expires = DateTime.UtcNow.AddMinutes(us.SessionLength);

            if (string.IsNullOrWhiteSpace(us.AuthToken))
            {
                string secret = _app.GetSetting("AppKey")?.Value;
                string issuer = _app.GetSetting("SiteDomain")?.Value;
                User u = string.IsNullOrWhiteSpace(us.UserData) ? null : JsonConvert.DeserializeObject<User>(us.UserData);
                us.AuthToken = CreateJwt(secret, u, issuer, us.Expires);
            }

            //make sure other sessions for this user in this account are cleared so there's no confusion.
            this.DeleteByUserId(us.UserUUID, us.AccountUUID, false);

            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                if (context.Insert<UserSession>(us))
                    return us;
            }
            _logger.InsertError("Failed to save session." + us.UserName, "SessionManager", "SaveSession");
            return null;
        }

        public bool Update(UserSession us)
        {
            if (us == null)
                return false;
            using (var context = new GreenWerxDbContext(_connectionKey))
            {
                return context.Update<UserSession>(us) > 0 ? true : false;
            }
        }

        private int GetSessionLength()
        {
            int length = 30;

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    Setting s = context.GetAll<Setting>()?.FirstOrDefault(sw => (sw.Name?.EqualsIgnoreCase("SESSIONLENGTH") ?? false));
                    return StringEx.ConvertTo<int>(s?.Value ?? "30");
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "SessionManager", "GetSessionLength");
                return length;
            }
        }
    }
}