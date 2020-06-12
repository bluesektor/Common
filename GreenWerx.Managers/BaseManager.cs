// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Membership;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Membership;

namespace GreenWerx.Managers
{
    public class BaseManager
    {
        protected string _connectionKey;
        protected SystemLogger _logger = null;
        protected User _requestingUser = null;
        private RoleManager _roleManager;
        private SessionManager _sessionManager;

        public BaseManager(string connectionKey, string sessionKey)
        {
            _connectionKey = connectionKey;
            SessionKey = sessionKey;
            _sessionManager = new SessionManager(connectionKey);
            _requestingUser = this.GetUser(sessionKey);
            _roleManager = new RoleManager(connectionKey, _requestingUser);
            _logger = new SystemLogger(connectionKey);
        }

        protected BaseManager()
        {
            _logger = new SystemLogger(null, true);
        }

        public string SessionKey { get; set; }

        public bool DataAccessAuthorized(INode dataItem, string verb, bool isSensitiveData)
        {
            if (_requestingUser == null)
            {
                _requestingUser = this.GetUser(SessionKey);

                if (_requestingUser == null)
                    return false;
            }
            if (_roleManager == null)
                _roleManager = new RoleManager(_connectionKey, _requestingUser);
            return _roleManager.DataAccessAuthorized(dataItem, _requestingUser, verb, isSensitiveData);
        }

        public bool DataAccessAuthorized(string type, bool allowSensitiveData, bool allowPrivateData)
        {
            if (_requestingUser == null)
            {
                _requestingUser = this.GetUser(SessionKey);

                if (_requestingUser == null)
                    return false;
            }
            if (_roleManager == null)
                _roleManager = new RoleManager(_connectionKey, _requestingUser);
            return _roleManager.DataAccessAuthorized(type, _requestingUser, allowSensitiveData, allowPrivateData);
        }

        public string GetProfileUUID(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
                return authToken;

            var _appManager = new AppManager(this._connectionKey, "web", authToken);
            string appSecret = _appManager.GetSetting("AppKey")?.Value;

            JwtClaims requestorClaims = null;

            try
            {
                var payload = JWT.JsonWebToken.Decode(authToken, appSecret, false);
                requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload);

                TimeSpan ts = requestorClaims.expires - DateTime.UtcNow;

                if (ts.TotalSeconds <= 0)
                    return string.Empty;

                string[] tokens = requestorClaims.aud.Replace(SystemFlag.Default.Account, "systemdefaultaccount").Split('.');
                if (tokens.Length == 0)
                    return string.Empty;

                //string userUUID = tokens[0];
                //string accountUUID = tokens[1];
                //if ("systemdefaultaccount" == accountUUID) accountUUID = SystemFlag.Default.Account;

                string profileUUID = tokens[2];
                return profileUUID;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public User GetUser(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return null;

            if (_sessionManager == null)
                _sessionManager = new SessionManager(this._connectionKey);

            UserSession session = _sessionManager.GetSession(sessionToken);
            if (session == null || string.IsNullOrWhiteSpace(session.UserData))
                return null;

            return JsonConvert.DeserializeObject<User>(session.UserData);
        }

        /// <summary>
        /// This does basic checks for the current user and data item
        /// to validate whether access is granted.
        /// </summary>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        public bool IsRequesterAuthorized(INode dataItem, out string reason)
        {
            reason = string.Empty;
            if (dataItem == null)
                return true;

            if (_requestingUser == null)
            {
                _requestingUser = this.GetUser(SessionKey);
            }

            //if (_requestingUser == null)
            //{
            //    reason = "You must be logged in to view this profile.";
            //    return false;
            //}

                if (_requestingUser != null && _requestingUser.Banned == true)
            {
                reason = "Your account is banned.";
                return false;
            }

            if (_requestingUser != null && _requestingUser.LockedOut == true)
            {
                reason = "Your account is locked.";
                return false;
            }

            if (_requestingUser == null && dataItem.Private == true)
            {
                reason = "You cannot access this private record.";
                return false;
            }
            return true;
        }
    }
}