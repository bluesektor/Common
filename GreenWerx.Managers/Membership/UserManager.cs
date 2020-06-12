// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using Omni.Managers.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Documents;
using GreenWerx.Managers.General;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Flags;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Services;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;

namespace GreenWerx.Managers.Membership
{
    public class UserManager : BaseManager, ICrud
    {
        private readonly AttributeManager _attributeManager = null;
        private readonly SystemLogger _logger = null;
        private readonly RoleManager _roleManager = null;

        public UserManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "UserManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;

            _logger = new SystemLogger(connectionKey);
            _attributeManager = new AttributeManager(connectionKey, SessionKey);

            var res = this.GetUser(SessionKey, true);
            if (res.Code == 200)
                _roleManager = new RoleManager(connectionKey, (User)res.Result);
        }

        public static User ClearSensitiveData(User u)
        {
            if (u == null)
                return u;

            u.Password = "";
            u.PasswordAnswer = "";
            u.MobilPin = "";
            u.PasswordHashIterations = 0;
            u.PasswordSalt = "";
            //u.ProviderUserKey = "";
            //u.ProviderName = "";
            u.UserKey = "";
            return u;
        }

        public static List<User> ClearSensitiveData(List<User> users)
        {
            users.ForEach(am =>
            {
                am.Password = "";
                am.PasswordAnswer = "";
                am.MobilPin = "";
                am.PasswordHashIterations = 0;
                am.PasswordSalt = "";
                am.ProviderUserKey = "";
                am.ProviderName = "";
                am.UserKey = "";
            });

            return users;
        }

        public ServiceResult AuthenticateUser(string userName, string password, string ip)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    context.Insert<AccessLog>(new AccessLog()
                    {
                        Authenticated = false,
                        AuthenticationDate = DateTime.UtcNow,
                        DateCreated = DateTime.UtcNow,
                        IPAddress = ip,
                        FailType = "Blank Username",
                        UserName = userName,
                        UUIDType = "AccessLog",
                        Vector = "UserManager.AuthenticateUser"
                    });
                }
                return ServiceResponse.Error("Invalid username or password.");
            }

            User user = null;
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    user = context.GetAll<User>()?.FirstOrDefault(uw => (uw.Name?.EqualsIgnoreCase(userName) ?? false));

                    if (user == null)
                        return ServiceResponse.Error("Invalid username or password.");

                    user.LastActivityDate = DateTime.UtcNow;

                    if (!PasswordHash.ValidatePassword(password, user.PasswordHashIterations + ":" + user.PasswordSalt + ":" + user.Password))
                    {
                        context.Insert<AccessLog>(new AccessLog()
                        {
                            Authenticated = false,
                            AuthenticationDate = DateTime.UtcNow,
                            IPAddress = ip,
                            FailType = "Invalid Password",
                            UserName = userName,
                            UUIDType = "AccessLog",
                            Vector = "UserManager.AuthenticateUser",
                            DateCreated = DateTime.UtcNow
                        });
                        user.FailedPasswordAttemptCount++;
                        context.Update<User>(user);
                        return ServiceResponse.Error("Invalid username or password.");
                    }

                    ServiceResult sr = IsUserAuthorized(user, ip);

                    if (sr.Status == "ERROR")
                        return sr;

                    user.Online = true;
                    user.LastLoginDate = DateTime.UtcNow;

                    context.Update<User>(user);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "UserManager", "AuthenticateUser:" + userName + " IP:" + ip);

                return ServiceResponse.Error(ex.Message);
            }
            user = ClearSensitiveData(user);

            return ServiceResponse.OK("", user);
        }

        /// <summary>
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public async Task<ServiceResult> AuthenticateUserAsync(string userName, string password, string ip)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    await context.InsertAsync<AccessLog>(new AccessLog() { Authenticated = false, AuthenticationDate = DateTime.UtcNow, IPAddress = ip, FailType = "Blank Username", UserName = userName, UUIDType = "AccessLog", Vector = "UserManager.AuthenticateUser" });
                }
                return ServiceResponse.Error("Invalid username or password.");
            }

            User user = null;
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    user = (await context.GetAllAsync<User>())?.FirstOrDefault(uw => (uw.Name?.EqualsIgnoreCase(userName) ?? false));

                    if (user == null)
                        return ServiceResponse.Error("Invalid username or password.");

                    user.LastActivityDate = DateTime.UtcNow;

                    if (!PasswordHash.ValidatePassword(password, user.PasswordHashIterations + ":" + user.PasswordSalt + ":" + user.Password))
                    {
                        context.Insert<AccessLog>(new AccessLog()
                        {
                            Authenticated = false,
                            AuthenticationDate = DateTime.UtcNow,
                            DateCreated = DateTime.UtcNow,
                            IPAddress = ip,
                            FailType = "Invalid Password",
                            UserName = userName,
                            UUIDType = "AccessLog",
                            Vector = "UserManager.AuthenticateUser"
                        });
                        user.FailedPasswordAttemptCount++;
                        context.Update<User>(user);
                        return ServiceResponse.Error("Invalid username or password.");
                    }

                    ServiceResult sr = IsUserAuthorized(user, ip);

                    if (sr.Status == "ERROR")
                        return sr;

                    user.Online = true;
                    user.LastLoginDate = DateTime.UtcNow;

                    await context.UpdateAsync<User>(user);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "UserManager", "AuthenticateUserAsync:" + userName + " IP:" + ip);

                return ServiceResponse.Error(ex.Message);
            }

            return ServiceResponse.OK();
        }

        public ServiceResult Delete(string userUUID, bool purge = false)
        {
            try
            {
                User u;
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    u = context.GetAll<User>()?.FirstOrDefault(w => w.UUID == userUUID);
                    if (u == null)
                        return ServiceResponse.Error("User not found.");

                    if (!this.DataAccessAuthorized(u, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

                    if (!DataAccessAuthorized(u, "delete", false))
                        return ServiceResponse.Error("You are not authorized access to ." + u.Name);

                    if (!purge)
                    {
                        u.Deleted = true;
                        return Update(u);
                    }
                    context.Delete<User>(u);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "UserManager", "DeleteUser:" + userUUID);

                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK();
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("You must pass in a valid user.");

            var user = (User)n;

            try
            {
                if (!this.DataAccessAuthorized(user, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

                User u = new User();
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (!purge)
                    {
                        u.Deleted = true;
                        return Update(u);
                    }
                    context.Delete<User>(u);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "UserManager", "DeleteUser:" + user.UUID);
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK();
        }

        public ServiceResult DeleteByEmail(string email, bool purge = false)
        {
            try
            {
                User u;
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    u = context.GetAll<User>()?.FirstOrDefault(w => w.Email == email);
                    if (u == null)
                        return ServiceResponse.Error("Email not found.");

                    if (!this.DataAccessAuthorized(u, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

                    if (!DataAccessAuthorized(u, "delete", false))
                        return ServiceResponse.Error("You are not authorized access to ." + u.Name);

                    if (!purge)
                    {
                        u.Deleted = true;
                        return Update(u);
                    }
                    context.Delete<User>(u);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "UserManager", "DeleteUser:" + email);

                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK();
        }

        public ServiceResult Get(string uuid)
        {
            return this.GetUser(uuid, true);
        }

        public List<User> GetAllUsers(ref DataFilter filter)
        {
            List<User> users;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                users = context.GetAll<User>(ref filter)?.Where(w => w.Deleted == false).OrderBy(o => o.Name).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            return users;
        }

        public ServiceResult GetUser(string UUID, bool clearSensitiveData = true)
        {
            //// if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                User res = null;
                if (clearSensitiveData)
                {
                    res = ClearSensitiveData(context.GetAll<User>()?.FirstOrDefault(uw => uw.UUID == UUID));
                }
                else
                {
                    res = context.GetAll<User>()?.FirstOrDefault(uw => uw.UUID == UUID);
                }
                if (res == null)
                    return ServiceResponse.Error("User not found");

                return ServiceResponse.OK("", res);
            }
        }

        public User GetUserByEmail(string email, bool clearSensitiveData = true)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new User();

            //// if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (clearSensitiveData)
                    return ClearSensitiveData(context.GetAll<User>()?.FirstOrDefault(uw => (uw.Email == email)));

                return context.GetAll<User>()?.FirstOrDefault(uw => (uw.Email == email));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="AccountUUID">This is technically accountUUID</param>
        /// <param name="clearSensitiveData"></param>
        /// <returns></returns>
        public List<User> GetUsers(string AccountUUID, bool clearSensitiveData = true)
        {
            //// if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (clearSensitiveData)
                    return ClearSensitiveData(context.GetAll<User>()?.Where(w => w.AccountUUID == AccountUUID && w.Deleted == false).ToList());

                return context.GetAll<User>()?.Where(w => w.AccountUUID == AccountUUID && w.Deleted == false).ToList();
            }
        }

        public List<User> GetUsers(bool clearSensitiveData = true)
        {
            //// if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            List<User> usrs;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (clearSensitiveData)
                {
                    usrs = ClearSensitiveData(context.GetAll<User>()?.Where(w => w.Deleted == false).ToList());
                    return usrs;
                }

                return context.GetAll<User>()?.Where(w => w.Deleted == false).ToList();
            }
        }

        public ServiceResult Insert(INode n)
        {
            return this.Insert(n, "");
        }

        public ServiceResult Insert(INode n, string ipAddress, bool ignoreAuthorization = false)
        {
            if (n == null)
                return ServiceResponse.Error("No record sent.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, 1);

            var u = (User)n;

            //we ignore authorization when a new user is registering.
            if (!ignoreAuthorization)
            {
                if (!this.DataAccessAuthorized(u, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            User dbU;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                dbU = context.GetAll<User>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(u.Name) ?? false));

                if (dbU != null)
                    return ServiceResponse.Error("Username already exists.");

                u.LastActivityDate = DateTime.UtcNow;

                if (context.Insert<User>(u))
                    return ServiceResponse.OK("", u);
            }
            return ServiceResponse.Error("Database failed to save user information.");
        }

        public async Task<ServiceResult> InsertUserAsync(User u, string ipAddress = "", bool ignoreAuthorization = true)
        {
            if (u == null)
                return ServiceResponse.Error("Invalid user object.");

            if (!ignoreAuthorization) //we ignore authorization when a new user is registering.
            {
                if (!this.DataAccessAuthorized(u, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");
            }

            User dbU;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                dbU = context.GetAll<User>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(u.Name) ?? false));

                if (dbU != null)
                    return ServiceResponse.Error("Username already exists.");

                u.UUID = Guid.NewGuid().ToString("N");
                u.UUIDType = "User";
                u.DateCreated = DateTime.UtcNow;
                u.LastActivityDate = DateTime.UtcNow;

                if (await context.InsertAsync<User>(u) > 0)
                    return ServiceResponse.OK("", u);
            }
            return ServiceResponse.Error("Database failed to save user information.");
        }

        /// <summary>
        /// This is just a basic check if they're banned, locked,
        /// approved etc.
        ///
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public ServiceResult IsUserAuthorized(User user, string ip)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (user.Deleted)
                {
                    context.Insert<AccessLog>(new AccessLog()
                    {
                        Authenticated = false,
                        DateCreated = DateTime.UtcNow,
                        CreatedBy = this.SessionKey,
                        AuthenticationDate = DateTime.UtcNow,
                        IPAddress = ip,
                        FailType = "Deleted User",
                        UserName = user.Name,
                        UUIDType = "AccessLog",
                        Vector = "UserManager.AuthenticateUser"
                    });
                    return ServiceResponse.Error("User was deleted.");
                }

                //this is set by clicking the validation email.
                if (!user.Approved)
                {
                    context.Insert<AccessLog>(new AccessLog()
                    {
                        Authenticated = false,
                        DateCreated = DateTime.UtcNow,
                        AuthenticationDate = DateTime.UtcNow,
                        IPAddress = ip,
                        FailType = "Unapproved User",
                        UserName = user.Name,
                        UUIDType = "AccessLog",
                        Vector = "UserManager.AuthenticateUser"
                    });
                    return ServiceResponse.Error("Account has not been approved. You must activate the account with the confirmation email before logging in. Check your spam/junk folder for the confirmation email if you have not received it. <a href=\"/Account/SendValidationEmail/?userUUID=" + user.UUID + "\">Resend Validation Email</a><br/>");
                }

                if (user.LockedOut)
                {
                    context.Insert<AccessLog>(new AccessLog()
                    {
                        Authenticated = false,
                        AuthenticationDate = DateTime.UtcNow,
                        DateCreated = DateTime.UtcNow,
                        IPAddress = ip,
                        FailType = "Locked User",
                        UserName = user.Name,
                        UUIDType = "AccessLog",
                        Vector = "UserManager.AuthenticateUser"
                    });
                    return ServiceResponse.Error("User is locked out.");
                }

                if (user.Banned)
                {
                    context.Insert<AccessLog>(new AccessLog() { Authenticated = false, AuthenticationDate = DateTime.UtcNow, IPAddress = ip, FailType = "Banned User", UserName = user.Name, UUIDType = "AccessLog", Vector = "UserManager.AuthenticateUser" });
                    return ServiceResponse.Error("User is banned.");
                }
            }
            return ServiceResponse.OK();
        }

        public List<User> Search(string name, bool contains)
        {
            return this.Search(name, true, contains);
        }

        public List<User> Search(string userName, bool clearSensitiveData, bool contains)
        {
            //   if (!this.DataAccessAuthorized(u, "GET", false)) return null; //todo fix this so log it or something

            List<User> u;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (contains)
                    u = context.GetAll<User>()?.Where(uw => (uw.Name.Contains(userName, StringComparison.InvariantCultureIgnoreCase))).ToList();
                else
                    u = context.GetAll<User>()?.Where(uw => (uw.Name?.EqualsIgnoreCase(userName) ?? false)).ToList();
            }
            if (clearSensitiveData)
            {
                u = ClearSensitiveData(u);
            }

            return u;
        }

        //string ip, string toEmailParam_Encrypted, string fromEmailParam_Encrypted, string subject, string body
        public async Task<ServiceResult> SendEmailAsync(EmailMessage message, EmailSettings settings)
        {
            //if (string.IsNullOrWhiteSpace(message.EmailTo))
            //    return ServiceResponse.Error("The recipients email is not set.");

            if (string.IsNullOrWhiteSpace(settings.SiteDomain))
            {
                _logger.InsertError("The  email setting site domain key is not set.", "UserManager", "SendEmail");
                return ServiceResponse.Error("An error occured when sending the message. <br/>ERROR: Site Domain configuration.");
            }

            if (string.IsNullOrWhiteSpace(settings.SiteEmail))
            {
                _logger.InsertError("The email setting SiteEmail is not set.", "UserManager", "SendEmail");
                return ServiceResponse.Error("An error occured when sending the message.");
            }

            #region EmailMessage

            var app = new AppManager(this._connectionKey, "web", "");
            string secret = app.GetSetting("AppKey")?.Value;
            UserManager um = new UserManager(this._connectionKey, this.SessionKey);
            var toUserRes = um.Get(message.SendTo);// um.GetUserByEmail(message.EmailTo);
            if (toUserRes.Code != 200)
                return ServiceResponse.Error("To user not found.");

            var fromUserRes = um.Get(message.SendFrom);// um.GetUserByEmail(message.EmailFrom);
            if (fromUserRes.Code != 200)
                return ServiceResponse.Error("From user not found.");

            var toUser = (User)toUserRes.Result;
            var fromUser = (User)fromUserRes.Result;
            message.Body = message.Body + "<br/><br/><br/>From:" + fromUser.Name + " < br/><br/><br/>Message Key:" + message.UUID;
            message.EmailTo = toUser.Email;
            message.SendTo = toUser.UUID;
            message.NameTo = toUser.Name;

            message.CreatedBy = _requestingUser.UUID;
            message.AccountUUID = _requestingUser.AccountUUID;
            message.EmailFrom = fromUser.Email;
            message.SendFrom = fromUser.UUID;
            message.NameFrom = fromUser.Name;
            message.DateCreated = DateTime.UtcNow;
            message.Status = "NotSent";

            EmailMessageManager emailMessageManager = new EmailMessageManager(this._connectionKey, string.Empty);

            if (emailMessageManager.Insert(message).Code == 500)
            {
                return ServiceResponse.Error("Failed to save the email. Try again later.");
            }

            #endregion EmailMessage

            string siteEmail = settings.SiteEmail;
            _logger.InsertInfo(siteEmail, "UserManager.cs", "SendMailAsync.siteEmail");
            _logger.InsertInfo(toUser.Email, "UserManager.cs", "SendMailAsync.toEmail");
            _logger.InsertInfo(fromUser.Email, "UserManager.cs", "SendMailAsync.fromEmail");

            MailMessage mail = new MailMessage();
            try
            {
                string toEmail = Cipher.Crypt(secret, toUser.Email, false);

                MailAddress ma = new MailAddress(siteEmail, settings.SiteDomain);
                mail.From = ma;
                mail.ReplyToList.Add(ma);
                mail.To.Add(toEmail);
                mail.Subject = message.Subject;
                mail.Body = message.Body;
                mail.IsBodyHtml = true;
                SMTP mailServer = new SMTP(this._connectionKey, settings);
                var mailRes = mailServer.SendMail(mail);//.SendMailAsync(mail);
                if (mailRes.Code == 200)
                {
                    message.Status = "sent";
                    message.DateSent = DateTime.UtcNow;
                    emailMessageManager.Update(message);
                }
                return mailRes;
            }
            catch (Exception ex)
            {
                _logger.InsertException(ex, "UserManager.cs", "SendMailAsync", "EmailMessage:" + message.UUID);
                return ServiceResponse.Error("Failed to send email. System will retry at a later time.", ex.DeserializeException());
            }
        }

        public ServiceResult SetUserFlag(string userUUID, string userFlag, string flagValue, bool authorize = true)
        {
            User u;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                u = context.GetAll<User>()?.FirstOrDefault(uw => uw.UUID == userUUID);
            }

            if (u == null)
                return ServiceResponse.Error("User not found.");

            if (authorize)
            {
                if (!this.DataAccessAuthorized(u, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");
            }

            if (string.IsNullOrWhiteSpace(userFlag) || string.IsNullOrWhiteSpace(flagValue))
                return ServiceResponse.Error("You are missing a flag or value to set.");

            switch (userFlag.ToUpper())
            {  // CTRL+SHIFT+U. to toggle case
                case "PROVIDERNAME":
                    u.ProviderName = flagValue;
                    break;

                case "PROVIDERUSERKEY":
                    u.ProviderUserKey = flagValue;
                    break;

                case "ACTIVE":
                    u.Active = StringEx.ConvertTo<bool>(flagValue);
                    break;

                case "DELETED":
                    u.Deleted = StringEx.ConvertTo<bool>(flagValue);
                    break;

                case "PRIVATE":
                    u.Private = StringEx.ConvertTo<bool>(flagValue);
                    break;

                case "ANONYMOUS":
                    u.Anonymous = StringEx.ConvertTo<bool>(flagValue);
                    break;

                case "APPROVED":
                    u.Approved = StringEx.ConvertTo<bool>(flagValue);
                    break;

                case "BANNED":
                    u.Banned = StringEx.ConvertTo<bool>(flagValue);
                    break;

                case "LOCKEDOUT":
                    u.LockedOut = StringEx.ConvertTo<bool>(flagValue);
                    if (u.LockedOut == true)
                    {
                        u.LastLockoutDate = DateTime.UtcNow;
                    }
                    break;

                case "ONLINE":
                    u.Online = StringEx.ConvertTo<bool>(flagValue);
                    break;
                //case "SITEADMIN": break;// backlog this requires security validation before implementing since its a very powerfull account
                default:
                    return ServiceResponse.Error("Invalid flag.");
            }
            u.LastUpdatedDate = DateTime.UtcNow;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<User>(u) < 0)
                    return ServiceResponse.Error("Error occurred saving user " + u.Name);
            }

            _logger.InsertInfo("userUUID:" + userUUID + ",userFlag:" + userFlag + ",flagValue:" + flagValue, "UserManager", "SetUserFlag");

            return ServiceResponse.OK("User " + u.Name + " updated.");
        }

        public ServiceResult Update(INode n)
        {
            return this.UpdateUser(n);
        }

        public ServiceResult UpdateUser(INode n, bool authorize = true)
        {
            if (n == null)
                return ServiceResponse.Error("No record was sent.");

            var u = (User)n;

            if (u.DateCreated == DateTime.MinValue)
                u.DateCreated = DateTime.UtcNow;

            if (authorize)
            {
                if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<User>(u) > 0)
                    return ServiceResponse.OK();
            }

            return ServiceResponse.Error("No records were updated.");
        }

        #region Registration Methods

        /// <summary>
        ///
        /// </summary>
        /// <param name="Approved"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public async Task<ServiceResult> RegisterUserAsync(UserRegister ur, bool Approved, string ipAddress)
        {
            if (ur.AgreedToTOS == null || ur.AgreedToTOS == DateTime.MinValue)
                return ServiceResponse.Error("You must agree to the Terms Of Service.");

            if (string.IsNullOrEmpty(ur.Name))
                return ServiceResponse.Error("Invalid username.");

            if (ur.Password != ur.ConfirmPassword)
                return ServiceResponse.Error("Passwords must match.");

            if (Validator.IsEmailInjectionAttempt(ur.Email))
            {
                _logger.InsertSecurity(ur.Email, "UserManager", "RegisterUser.IsEmailInjectionAttempt");
                return ServiceResponse.Error("Dangerous email format.");
            }

            if (!Validator.IsValidEmailFormat(ur.Email))
            {
                return ServiceResponse.Error("Invalid email format.");
            }

            if (Validator.HasReservedLoginName(ur.Email))
            {
                _logger.InsertSecurity(ur.Email, "UserManager", "RegisterUser.HasReservedLoginName");
                return ServiceResponse.Error("Invalid email name.");
            }

            if (!Validator.IsMinAge(ur.DOB, 18))
            {
                _logger.InsertSecurity(ur.Email, "UserManager", "RegisterUser.Underage");
                return ServiceResponse.Error("You must be at least 18 to enter this site.");
            }

            if (!Validator.IsValidRelationshipStatus(ur.RelationshipStatus))
            {
                return ServiceResponse.Error("You must select a valid relationship status.");
            }

            var app = new AppManager(this._connectionKey, "web", "");
            string secret = app.GetSetting("AppKey")?.Value;
            var encEmail = Cipher.Crypt(secret, ur.Email.ToLower(), true);

            User dbUser;
            Account defaultAccount = null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                dbUser = context.GetAll<User>()?.FirstOrDefault(uw => (uw.Email == encEmail) || (uw.Name?.EqualsIgnoreCase(ur.Name) ?? false));

                if (!string.IsNullOrWhiteSpace(ur.AccountUUID))
                {
                    defaultAccount = context.GetAll<Account>()?.FirstOrDefault(w => w.UUID == ur.AccountUUID);
                    if (defaultAccount == null)
                        return ServiceResponse.Error("The account uuid you passed in is not valid.");
                }
                else
                {  // the defalt account will be under plato. Only create accounts for business/affiates.
                    defaultAccount = context.GetAll<Account>()?.FirstOrDefault(w => w.Name.EqualsIgnoreCase("plato"));
                }
            }

            if (dbUser != null && dbUser.Approved == false)
                return ServiceResponse.Error("The email account you provided is already registered, but has not been validated.  Please check your email account and follow the instructions on the message sent. Thank you.");
            else if (dbUser != null)
                return ServiceResponse.Error("Username or email already exists.");

            var userUUID = Guid.NewGuid().ToString("N");

            //var accountGuid = Guid.NewGuid().ToString("N");
            //if (dbAccount != null)
            //    accountGuid = dbAccount.UUID;

            //Account newAccount = null;
            AccountMember accountMember = null;
            ServiceResult res = null;

            // using (var scope = new TransactionScope())
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                //if (dbAccount == null)
                //{
                //newAccount = new Account()
                //{
                //    Name = ur.Name,
                //    AccountUUID = accountGuid,
                //    UUID = accountGuid,
                //    AccountSource = ur.ClientType,
                //    DateCreated = DateTime.Now,
                //    CreatedBy = userUUID,
                //    Email = encEmail, //ur.Email,
                //    RoleOperation = "=",
                //    RoleWeight = RoleFlags.MemberRoleWeights.Member,
                //    GUUID = accountGuid,
                //    Private = true,
                //    Category = "User"
                //};

                //if (context.Insert<Account>(newAccount))
                //{
                accountMember = new AccountMember() { AccountUUID = defaultAccount.AccountUUID, MemberUUID = userUUID, MemberType = "User" };
                if (!context.Insert<AccountMember>(accountMember))
                {
                    //todo rollback make sure account is rolled back
                    return ServiceResponse.Error("Error adding user to account.");
                }
                //}
                //else
                //{
                //    return ServiceResponse.Error("Error creating account." + context.Message);
                //}

                ur.AccountUUID = defaultAccount.AccountUUID;
                //}

                string tmpHashPassword = PasswordHash.CreateHash(ur.Password);

                User referringUser = null;

                if (!string.IsNullOrWhiteSpace(ur.ReferringMember))
                    referringUser = context.GetAll<User>().FirstOrDefault(x => x.Name.EqualsIgnoreCase(ur.ReferringMember));

                User u = new User()
                {
                    ReferringMember = referringUser?.UUID,
                    ReferralDate = DateTime.UtcNow,
                    UUParentIDType = referringUser?.UUIDType,
                    AgreedToTOS = ur.AgreedToTOS,
                    UUID = userUUID,
                    AccountUUID = ur.AccountUUID,
                    Name = ur.Name,
                    Password = PasswordHash.ExtractHashPassword(tmpHashPassword),
                    PasswordAnswer = ur.SecurityAnswer,
                    PasswordQuestion = ur.SecurityQuestion,
                    Active = true,
                    DateCreated = DateTime.UtcNow,
                    Deleted = false,
                    PasswordSalt = PasswordHash.ExtractSalt(tmpHashPassword),
                    PasswordHashIterations = PasswordHash.ExtractIterations(tmpHashPassword),
                    Email = encEmail, // ur.Email,
                    SiteAdmin = false,
                    Approved = Approved,
                    Anonymous = false,
                    Banned = false,
                    LockedOut = false,
                    RoleWeight = RoleFlags.MemberRoleWeights.Member,
                    RoleOperation = "=",
                    Private = true, // Since its a site admin we'll make it private  appSettings.UserIsPrivate,
                    FailedPasswordAnswerAttemptWindowStart = 0,
                    FailedPasswordAttemptCount = 0,
                    FailedPasswordAnswerAttemptCount = 0,

                    ProviderName = UserFlags.ProviderName.ValidateEmail
                };
                if (string.IsNullOrWhiteSpace(u.ProviderUserKey))
                    u.ProviderUserKey = Cipher.RandomString(12);

                res = await InsertUserAsync(u, ipAddress);

                if (res.Code != 200)
                {
                    //todo rollback make sure account && AccountMember is rolled back
                    return res;
                }

                Profile p = new Profile()
                {
                    RelationshipStatus = ur.RelationshipStatus,
                    AccountUUID = u.AccountUUID,
                    CreatedBy = u.UUID,
                    UUIDType = "Profile",
                    UserCache = JsonConvert.SerializeObject(UserManager.ClearSensitiveData(u)),
                    DateCreated = DateTime.UtcNow,
                    Image = "/assets/img/blankprofile.png",
                    UserUUID = u.UUID,
                    SortOrder = 0,
                    Private = false,
                    ShowPublic = false,
                    Active = true,
                    RoleWeight = RoleFlags.MemberRoleWeights.Member,
                    RoleOperation = "="
                };

                if (context.Insert<Profile>(p))
                {
                    ProfileMember pm = new ProfileMember()
                    {
                        Private = false,
                        DOB = ur.DOB,
                        RelationshipStatus = ur.RelationshipStatus,
                        Gender = ur.Gender,
                        RoleWeight = RoleFlags.MemberRoleWeights.Member,
                        RoleOperation = "=",
                        SortOrder = 0,
                        UserUUID = u.UUID,
                        DobType = "DateTime",
                        Active = true,
                        AccountUUID = u.AccountUUID,
                        ProfileUUID = p.UUID,
                        DateCreated = DateTime.UtcNow,
                        CreatedBy = u.UUID
                    };
                    context.Insert<ProfileMember>(pm);
                }

                string catRoleName = ur.RelationshipStatus;

                if (ur.RelationshipStatus.EqualsIgnoreCase("single"))
                    catRoleName += " " + ur.Gender;

                var role = context.GetAll<Role>()?.FirstOrDefault(rw =>
                        (rw.CategoryRoleName?.EqualsIgnoreCase(catRoleName) ?? false) &&
                        (rw.Category.EqualsIgnoreCase("member")) &&
                        rw.AccountUUID == defaultAccount.AccountUUID);

                if (role == null)
                {
                    //create default roles for this account
                    var defaultRoles = context.GetAll<Role>()?.Where(rw =>
                        // rw.UUID != role.UUID &&
                        rw.AccountUUID == SystemFlag.Default.Account);

                    foreach (var defaultRole in defaultRoles)
                    {
                        defaultRole.AccountUUID = defaultAccount.AccountUUID;
                        defaultRole.UUID = Guid.NewGuid().ToString("N");
                        context.Insert<Role>(defaultRole);
                    }

                    role = context.GetAll<Role>()?.FirstOrDefault(rw =>
                       (rw.CategoryRoleName?.EqualsIgnoreCase(catRoleName) ?? false) &&
                       (rw.Category.EqualsIgnoreCase("member")) &&
                       rw.AccountUUID == defaultAccount.AccountUUID);

                    //if (role == null)
                    //{
                    //    role = context.GetAll<Role>()?.FirstOrDefault(rw =>
                    //        (rw.CategoryRoleName?.EqualsIgnoreCase(catRoleName) ?? false) &&
                    //        (rw.Category.EqualsIgnoreCase("member")) &&
                    //        rw.AccountUUID == SystemFlag.Default.Account);
                    //}

                    if (role == null)
                        return ServiceResponse.Error("Error loading role.");
                    //role.AccountUUID = accountGuid;
                    //role.UUID = Guid.NewGuid().ToString("N");
                    //context.Insert<Role>(role);
                }

                UserRole userRole = new UserRole()
                {
                    UUID = Guid.NewGuid().ToString("N"),
                    Name = role.Name,
                    AccountUUID = u.AccountUUID,
                    Active = true,
                    CreatedBy = u.UUID,
                    DateCreated = DateTime.UtcNow,
                    ReferenceUUID = u.UUID,
                    ReferenceType = "User",
                    RoleUUID = role.UUID,
                    RoleOperation = role.RoleOperation,
                    RoleWeight = role.RoleWeight
                };
                if (!context.Insert<UserRole>(userRole))
                    return ServiceResponse.Error(role.Name + " failed to add. ");

                UserRole profileRole = new UserRole()
                {
                    UUID = Guid.NewGuid().ToString("N"),
                    Name = role.Name,
                    AccountUUID = p.AccountUUID,
                    Active = true,
                    CreatedBy = u.UUID,
                    DateCreated = DateTime.UtcNow,
                    ReferenceUUID = p.UUID,
                    ReferenceType = "Profile",
                    RoleUUID = role.UUID,
                    RoleOperation = role.RoleOperation,
                    RoleWeight = role.RoleWeight
                };
                if (!context.Insert<UserRole>(profileRole))
                    return ServiceResponse.Error(role.Name + " failed to add. ");

                role = context.GetAll<Role>()?.FirstOrDefault(rw =>
                   (rw.CategoryRoleName?.EqualsIgnoreCase("member") ?? false) &&
                   (rw.Category.EqualsIgnoreCase("member")) &&
                   rw.AccountUUID == defaultAccount.AccountUUID);

                UserRole memberRole = new UserRole()
                {
                    UUID = Guid.NewGuid().ToString("N"),
                    Name = role.Name,
                    AccountUUID = p.AccountUUID,
                    Active = true,
                    CreatedBy = u.UUID,
                    DateCreated = DateTime.UtcNow,
                    ReferenceUUID = p.UUID,
                    ReferenceType = "User",
                    RoleUUID = role.UUID,
                    RoleOperation = role.RoleOperation,
                    RoleWeight = role.RoleWeight
                };
                if (!context.Insert<UserRole>(memberRole))
                    return ServiceResponse.Error(role.Name + " failed to add. ");

                context.SaveChanges();
                //  scope.Complete();
                context.Configuration.AutoDetectChangesEnabled = true;
                res.Result = u;
            }
            return res;
        }

        public async Task<ServiceResult> SendPasswordResetEmailAsync(User user, string ipAddress, EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SiteDomain))
                return ServiceResponse.Error("The applications site domain key is not set.");

            if (string.IsNullOrWhiteSpace(settings.SiteEmail))
                return ServiceResponse.Error("The applications site email key is not set.");

            #region build email from template backlog make function

            string validationUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=pwdr&code=" + user.ProviderUserKey;
            string oopsUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mdel&code=" + user.ProviderUserKey;

            DocumentManager dm = new DocumentManager(this._connectionKey, SessionKey);
            ServiceResult docRes = dm.GetTemplate("PasswordResetEmail");
            if (docRes.Status != "OK" || docRes.Result == null)
                return docRes;

            string template = docRes.Result.ToString();

            template = template.Replace("[DOMAIN]", settings.SiteDomain);
            template = template.Replace("[USERNAME]", user.Name);
            template = template.Replace("[UPDATEPASSWORD_LINK]", validationUrl);
            template = template.Replace("[UNREGISTRATION_LINK]", oopsUrl);

            #endregion build email from template backlog make function

            if (!Validator.IsValidEmailFormat(user.Email))
            {
                var app = new AppManager(this._connectionKey, "web", "");
                string secret = app.GetSetting("AppKey")?.Value;

                user.Email = Cipher.Crypt(secret, user.Email, false);
            }

            MailAddress ma = new MailAddress(settings.SiteEmail, settings.SiteDomain);
            MailMessage mail = new MailMessage();
            mail.From = ma;
            mail.ReplyToList.Add(ma);
            mail.To.Add(user.Email);
            mail.Subject = settings.SiteDomain + " Password Reset.";
            mail.Body = template;
            mail.IsBodyHtml = true;
            SMTP mailServer = new SMTP(this._connectionKey, settings);
            return mailServer.SendMail(mail);
        }

        public ServiceResult SendUserEmailValidation(User user, string validationCode, string ipAddress, EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SiteDomain))
                return ServiceResponse.Error("The applications site domain key is not set.");

            if (string.IsNullOrWhiteSpace(settings.SiteEmail))
                return ServiceResponse.Error("The applications site email key is not set.");

            #region build email from template

            //string validationUrl = "https://" + settings.SiteDomain + "/membership/validate/type/mbr/operation/mreg/code/" + validationCode;
            //string oopsUrl = "https://" + settings.SiteDomain + "/membership/validate/type/mbr/operation/mdel/code/" + validationCode;
            string validationUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mreg&code=" + validationCode;
            string oopsUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mdel&code=" + validationCode;

            DocumentManager dm = new DocumentManager(this._connectionKey, SessionKey);
            ServiceResult docRes = dm.GetTemplate("EmailNewMember");
            if (docRes.Status != "OK")
                return docRes;

            string template = docRes.Result?.ToString();

            if (string.IsNullOrWhiteSpace(template))
                return ServiceResponse.Error("Unable to locate email template.");

            template = template.Replace("[DOMAIN]", settings.SiteDomain);
            template = template.Replace("[USERNAME]", user.Name);
            template = template.Replace("[REGISTRATION_LINK]", validationUrl);
            template = template.Replace("[UNREGISTRATION_LINK]", oopsUrl);

            #endregion build email from template

            MailAddress ma = new MailAddress(settings.SiteEmail, settings.SiteDomain);
            MailMessage mail = new MailMessage();
            mail.From = ma;
            mail.ReplyToList.Add(ma);
            mail.To.Add(user.Email);
            mail.Subject = settings.SiteDomain + " account registration.";
            mail.Body = template;
            mail.IsBodyHtml = true;
            SMTP mailServer = new SMTP(this._connectionKey, settings);
            return mailServer.SendMail(mail);
        }

        //Used to resend validation email
        public async Task<ServiceResult> SendUserEmailValidationAsync(User user, string validationCode, string ipAddress, EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SiteDomain))
                return ServiceResponse.Error("The applications site domain key is not set.");

            if (string.IsNullOrWhiteSpace(settings.SiteEmail))
                return ServiceResponse.Error("The applications site email key is not set.");

            #region build email from template

            // string validationUrl = "https://" + settings.SiteDomain + "/membership/validate/type/mbr/operation/mreg/code/" + validationCode;
            // string oopsUrl = "https://" + settings.SiteDomain + "/membership/validate/type/mbr/operation/mdel/code/" + validationCode;
            string validationUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mreg&code=" + validationCode;
            string oopsUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mdel&code=" + validationCode;

            DocumentManager dm = new DocumentManager(this._connectionKey, SessionKey);
            ServiceResult docRes = dm.GetTemplate("EmailNewMember");
            if (docRes.Status != "OK")
                return docRes;

            string template = docRes.Result?.ToString();
            if (string.IsNullOrWhiteSpace(template))
            {
                _logger.InsertError("Email template is empty", "UserManager", "SendUserEmailValidationAsync");
                return ServiceResponse.Error("Template is empty.");
            }

            template = template.Replace("[DOMAIN]", settings.SiteDomain);
            template = template.Replace("[USERNAME]", user.Name);
            template = template.Replace("[REGISTRATION_LINK]", validationUrl);
            template = template.Replace("[UNREGISTRATION_LINK]", oopsUrl);

            #endregion build email from template

            //                From domain must match authenticated domain
            //HostUser = info@greenwerx.org
            // EmailHostUser= info@greenwerx.org

            // todo add email domain and make it what dev.greenwerx.org
            MailAddress ma = new MailAddress(settings.SiteEmail, settings.SiteDomain);
            MailMessage mail = new MailMessage();
            mail.From = ma;
            mail.ReplyToList.Add(ma);
            mail.Bcc.Add( "stephen.osterhoudt+" + settings.SiteDomain + "@gmail.com");
            mail.To.Add(user.Email);
            mail.Subject = settings.SiteDomain + " account registration.";
            mail.Body = template;
            mail.IsBodyHtml = true;

            SMTP mailServer = new SMTP(this._connectionKey, settings);

            _logger.InsertInfo("978", "usermanager.cs", "senduseremailvalidationasync");
            return mailServer.SendMail(mail);
        }

        public async Task<ServiceResult> SendUserInfoAsync(User user, string ipAddress, EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SiteDomain))
                return ServiceResponse.Error("The applications site domain key is not set.");

            if (string.IsNullOrWhiteSpace(settings.SiteEmail))
                return ServiceResponse.Error("The applications site email key is not set.");

            #region build email from template

            //users/validate/type/:type/operation/:operation/code/:code
            //string validationUrl = "https://" + settings.SiteDomain + "/membership/validate/type/mbr/operation/pwdr/code/" + user.ProviderUserKey;
            //string oopsUrl = "https://" + settings.SiteDomain + "/membership/validate/type/mbr/operation/mdel/code/" + user.ProviderUserKey;
            string validationUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mreg&code=" + user.ProviderUserKey;
            string oopsUrl = "https://" + settings.SiteDomain + "?validate=membership&type=mbr&operation=mdel&code=" + user.ProviderUserKey;

            DocumentManager dm = new DocumentManager(this._connectionKey, SessionKey);
            ServiceResult docRes = dm.GetTemplate("UserInfoEmail");
            if (docRes.Status != "OK" || docRes.Result == null)
                return docRes;

            string template = docRes.Result.ToString();

            template = template.Replace("[DOMAIN]", settings.SiteDomain);
            template = template.Replace("[USERNAME]", user.Name);
            template = template.Replace("[UPDATEPASSWORD_LINK]", validationUrl);
            template = template.Replace("[UNREGISTRATION_LINK]", oopsUrl);

            #endregion build email from template

            //  Your smtp login email MUST be same as your FROM address.
            if (!Validator.IsValidEmailFormat(user.Email))
            {
                var app = new AppManager(this._connectionKey, "web", "");
                string secret = app.GetSetting("AppKey")?.Value;

                user.Email = Cipher.Crypt(secret, user.Email, false);
            }

            MailAddress ma = new MailAddress(settings.SiteEmail, settings.EmailDomain);
            MailMessage mail = new MailMessage();
            mail.From = ma;
            mail.ReplyToList.Add(ma);
            mail.To.Add(user.Email);
            mail.Subject = settings.SiteDomain + "  Account Information";
            mail.Body = template;
            mail.IsBodyHtml = true;
            SMTP mailServer = new SMTP(this._connectionKey, settings);
            return mailServer.SendMail(mail);
        }

        public ServiceResult Validate(string type, string action, string validationCode, string ipAddress, User requestingUser)
        {
            if (!StringEx.IsSQLSafeString(type + " " + action + " " + validationCode))
            {
                _logger.InsertSecurity(type + " " + action + " " + validationCode, ipAddress, "Validate.IsSafeString:validationCode");
                return ServiceResponse.Error("Error processing request, invalid url.");
            }

            User user;
            try
            {
                user = this.GetUsers(false)?.FirstOrDefault(dd => dd.ProviderUserKey == validationCode &&
                                                            (dd.ProviderName == UserFlags.ProviderName.ValidateEmail ||
                                                             dd.ProviderName == UserFlags.ProviderName.SendAccountInfo ||
                                                             dd.ProviderName == UserFlags.ProviderName.ForgotPassword));
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "UserManager", "Validate.GetUsers");
                return ServiceResponse.Error("Error occured retrieving user.");
            }

            if (user == null)
                return ServiceResponse.Error("Invalid verification code.");

            if (user.Banned)
                return ServiceResponse.Error("Account is banned.");

            string typeAction = string.Format("{0}_{1}", type, action);

            if (user.Approved && typeAction == "mbr_mreg")// The email account you provided to subscribe is already on file and activated, but if mdel is in the action the user wants to delete account
                return ServiceResponse.OK("Account is approved.");//may have been set by an admin.

            user.LastActivityDate = DateTime.UtcNow;

            //Validate to subscribe.
            switch (typeAction)
            {
                case "mbr_mreg": //member subscribe.
                    user.Approved = true;
                    user.ProviderName = "";
                    user.ProviderUserKey = "";

                    RoleManager rm = new RoleManager(this._connectionKey, user);
                    var userRole = rm.GetRolesForUser(user.UUID, user.AccountUUID)
                                        .Where(w => w.Category.EqualsIgnoreCase("member"))
                                        .OrderByDescending(o => o.RoleWeight).FirstOrDefault();

                    if (userRole?.Weight < RoleFlags.MemberRoleWeights.Member)
                        user.RoleWeight = RoleFlags.MemberRoleWeights.Member; //increase the role weight so the can use the api
                    else
                        user.RoleWeight = userRole?.Weight ?? RoleFlags.MemberRoleWeights.Member;
                    //Add user to customer role by default.
                    //
                    //RoleManager rm = new RoleManager(this._connectionKey, user);
                    //Role customer = (Role)rm.GetRole("customer", user.AccountUUID);

                    //if (customer == null)
                    //{
                    //    _logger.InsertError("Failed to get role customer!", "UserManager", "ValidateAsync.mbr_mreg");
                    //    return ServiceResponse.Error("Error saving role information. Try again later.");
                    //}
                    //else {
                    //    rm.AddUserToRole(customer.UUID, user, requestingUser);
                    //}
                    return UpdateUser(user, false);

                case "mbr_mdel": //membership oops/remove
                    user.ProviderName = "";
                    user.ProviderUserKey = "";
                    user.Email = string.Empty;
                    UpdateUser(user, true);
                    //don't purge or it may break internal references.
                    return this.Delete(user, false);

                case "mbr_pwdr"://password reset
                    return UpdateUser(user, false);

                default:
                    return ServiceResponse.Error("Invalid code.");
            }

            #endregion Registration Methods
        }
    }
}