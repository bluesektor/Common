//using MySql.Provider.Database;
//using MySql.Provider.Providers.WordPress.V5.Models.Core;
//using MySql.Provider.Providers.WordPress.V5.Models.WBAffiliateMaster;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;

namespace GreenWerx.Managers.Logging
{
    public class AffiliateManager : BaseManager // , ICrud
    {
       
        //private MySqlDbContext mysqlContext;

        private readonly SystemLogger _fileLogger = new SystemLogger(null, true);

        public AffiliateManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "AffiliateManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;
            _logger = new SystemLogger(connectionKey);
        }

        public ServiceResult DeleteLog(INode n, bool purge = false)
        {
            throw new NotImplementedException();
            //    if (n == null)
            //        return ServiceResponse.Error("No record sent.");

            //    if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            //    var p = (AffiliateLog)n;

            //    try
            //    {
            //        using (var context = new GreenWerxDbContext(this._connectionKey))
            //        {
            //            if (purge)
            //            {
            //                DynamicParameters parameters = new DynamicParameters();
            //                parameters.Add("@AffiliateLogUUID", p.UUID);
            //                if (context.Delete<AffiliateLog>("WHERE UUID=@AffiliateLogUUID", parameters) > 0)
            //                {
            //                    DynamicParameters credParams = new DynamicParameters();
            //                    credParams.Add("@UUID", p.UUID);
            //                    credParams.Add("@TYPE", "AffiliateLog");
            //                    context.Delete<Credential>("WHERE RecipientUUID=@UUID AND RecipientType=@TYPE", credParams);

            //                    return ServiceResponse.OK();
            //                }
            //            }
            //            else
            //            {
            //                p.Deleted = true;
            //                if (context.Update<AffiliateLog>(p) > 0)
            //                    return ServiceResponse.OK();
            //            }
            //        }
            //        return ServiceResponse.Error("No records deleted.");
            //        ////SQLITE
            //        ////this was the only way I could get it to delete a RolePermission without some stupid EF error.
            //        ////object[] paramters = new object[] { rp.PermissionUUID , rp.RoleUUID ,rp.UUID };
            //        ////context.Delete<RolePermission>("WHERE PermissionUUID=? AND RoleUUID=? AND UUID=?", paramters);
            //        ////  context.Delete<RolePermission>(rp);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.InsertError(ex.Message, "AffiliateManager", "DeleteAffiliateLog:" + p.UUID);
            //        Debug.Assert(false, ex.Message);
            //        return ServiceResponse.Error("Exception occured while deleting this record.");
            //    }
        }

        public ServiceResult Get(string uuid)
        {
            throw new NotImplementedException();
            //    if (string.IsNullOrWhiteSpace(uuid))
            //        return null;
            //    using (var context = new GreenWerxDbContext(this._connectionKey))
            //    {
            //        ///if (!this.DataAccessAuthorized(p, "GET", false))return ServiceResponse.Error("You are not authorized this action.");
            //        var res = context.GetAll<AffiliateLog>()?.FirstOrDefault(sw => sw.UUID == uuid);
            //        if (res == null)
            //            return ServiceResponse.Error("AffiliateLog not found for uuid.");
            //        return ServiceResponse.OK("", res);
            //    }
        }

        public List<AffiliateLog> GetAffiliateLogs(string UUID)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(p, "GET", false))return ServiceResponse.Error("You are not authorized this action.");

                if (string.IsNullOrWhiteSpace(UUID))
                    return context.GetAll<AffiliateLog>().ToList();

                return context.GetAll<AffiliateLog>()?.Where(pw => pw.UUID == UUID).ToList();
            }
        }

        public List<AffiliateLog> GetAffiliateLogs(string UUID, bool deleted = false, bool includeSystem = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                /////if (!this.DataAccessAuthorized(p, "GET", false))
                ////    return ServiceResponse.Error("You are not authorized this action.");
                if (includeSystem)
                {
                    return context.GetAll<AffiliateLog>()?.Where(sw => (sw.UUID == UUID) && sw.Deleted == deleted).GroupBy(x => x.Name).Select(group => group.First()).OrderBy(ob => ob.Name).ToList();
                }

                return context.GetAll<AffiliateLog>()?.Where(sw => (sw.UUID == UUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
            }
        }

        public List<AffiliateLog> GetUserAffiliateLogs(string UUID, string userUUID)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(p, "GET", false))return ServiceResponse.Error("You are not authorized this action.");
                if (string.IsNullOrWhiteSpace(UUID))
                    return context.GetAll<AffiliateLog>().ToList();

                return context.GetAll<AffiliateLog>()?.Where(pw => pw.UUID == UUID && pw.CreatedBy == userUUID).ToList();
            }
        }

        public ServiceResult InsertLog(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid log data.");

            // if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

             
            n.Initialize(this._requestingUser?.UUID, this._requestingUser?.AccountUUID, this._requestingUser?.RoleWeight ?? -1);

            var a = (AffiliateLog)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<AffiliateLog>(a))
                {
                    InsertLog_Wordpress(n);
                    return ServiceResponse.OK("", a);
                }
            }
            return ServiceResponse.Error("System error, log was not added.");
        }

        protected void InsertLog_Wordpress(INode log)
        {
            //if (log == null)
            //    return;
            //var track = new bswp_wpam_tracking_tokens();
            //AffiliateLog ppLog = new AffiliateLog();
            //try
            //{
            //    if (mysqlContext == null)
            //        mysqlContext = new MySqlDbContext("mysql");

            //    ppLog = log as AffiliateLog;

            //    var affiliate = mysqlContext.GetAll<bswp_wpam_affiliates>().FirstOrDefault(w => w.uniqueRefKey == ppLog.ReferringUUID);
            //    //clicks are logged here
            //    track = new bswp_wpam_tracking_tokens()
            //    {
            //        ipAddress = ppLog.ClientIp,
            //        sourceAffiliateId = affiliate?.affiliateId ?? -1,  // the affiliate id in the wordpress affiliates table
            //        affiliateSubCode = ppLog.Direction,
            //        browser = ppLog.Link,
            //        customId = ppLog.ClientUserUUID, // the person the clicked the link.
            //        dateCreated = DateTime.UtcNow,
            //        referer = ppLog.Referrer,
            //        sourceCreativeId = -1,
            //        trackingKey = ppLog.ReferringUUID, //this is the affliate userUUID in greenwerx
            //        trackingTokenId = -1
            //    };
            //    var id = mysqlContext.Insert<bswp_wpam_tracking_tokens>(track);
            //}
            //catch (Exception ex)
            //{
            //    _fileLogger.InsertException(ex, "AffiliateManager", "InsertLog_Wordpress",
            //      JsonConvert.SerializeObject(track) + "|" +
            //      JsonConvert.SerializeObject(ppLog) );
            //}
            ////bswp_wpam_impressions
            ////    bswp_wpam_events
            ////    bswp_wpam_tracking_tokens

        }

        public List<AffiliateLog> SearchLogs(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<AffiliateLog>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                ///if (!this.DataAccessAuthorized(p, "GET", false))return ServiceResponse.Error("You are not authorized this action.");

                return context.GetAll<AffiliateLog>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
        }

        public ServiceResult UpdateLog(INode n)
        {
            return ServiceResponse.OK();
            //throw new NotImplementedException();
            //    if (n == null)
            //        return ServiceResponse.Error("No record sent.");

            //    var p = (AffiliateLog)n;

            //    if (!this.DataAccessAuthorized(p, "PATCH", false))
            //        return ServiceResponse.Error("You are not authorized this action.");

            //    ServiceResult res = ServiceResponse.OK();
            //    using (var context = new GreenWerxDbContext(this._connectionKey))
            //    {
            //        if (context.Update<AffiliateLog>(p) == 0)
            //            return ServiceResponse.Error(p.Name + " failed to update. ");
            //    }
            //    return res;
        }


        //public ServiceResult InsertAffiliate(INode n)
        //{
        //    throw new NotImplementedException();
        //}
        
        public ServiceResult RegisterAffiliate_WordPress(User user)
        {
            return ServiceResponse.Error("NOT IMPLMENTED");
            //if (user == null)
            //    return ServiceResponse.Error("No user was sent.");

            //if (mysqlContext == null)
            //    mysqlContext = new MySqlDbContext("mysql");
            //bswp_users            wpUser           = new bswp_users();
            //bswp_usermeta          usermeta      = new bswp_usermeta();
            //bswp_wpam_affiliates affiliate        = new bswp_wpam_affiliates();
            //try
            //{
            //    // get a sample user for settings of new user
            //    var nonAdminuserMeta = mysqlContext.GetAll<bswp_usermeta>().FirstOrDefault(w => w.meta_key == "first_name" && w.meta_value == "template");

            //    if (nonAdminuserMeta == null)
            //        return ServiceResponse.Error("No settings for meta user.");

            //    var nonAdminSettings = mysqlContext.GetAll<bswp_usermeta>().Where(w => w.user_id == nonAdminuserMeta.user_id);

            //    // does user exist by email
            //    if (mysqlContext.GetAll<bswp_users>().Any<bswp_users>(w => w.user_login.EqualsIgnoreCase(user.Name) || w.user_email.EqualsIgnoreCase(user.Email)))
            //        return ServiceResponse.Error("Member is already an affiliate.");

            //    // encrypt the user password
            //    string wpPassword = PasswordHash.wpHashPasswordForWordpress(user.Password);

            //    // insert into bswp_users
            //    wpUser = new bswp_users()
            //    {
            //        display_name = user.Name,
            //        user_email = user.Email,
            //        user_login = user.Name,
            //        user_pass = wpPassword,
            //        user_nicename = user.Name,
            //        user_url = "",
            //        user_activation_key = "",
            //        user_registered = DateTime.UtcNow,
            //         user_status  = 0
            //    };
            //    int userId = mysqlContext.Insert<bswp_users>(wpUser);


            //    //insert into bswp_usermeta
            //     usermeta = new bswp_usermeta() { meta_key = "nickname", meta_value = user.Name, user_id = userId };
            //    mysqlContext.Insert<bswp_usermeta>( usermeta);

            //    foreach (var setting in nonAdminSettings)
            //    {

            //        if (setting.meta_key.EqualsIgnoreCase("nickname") ||
            //            setting.meta_key.EqualsIgnoreCase("first_name") ||
            //                setting.meta_key.EqualsIgnoreCase("last_name") ||
            //            setting.meta_key.EqualsIgnoreCase("description")
            //            )
            //            continue;
            //         usermeta = new bswp_usermeta() { meta_key = setting.meta_key, meta_value = setting.meta_value, user_id = userId };
            //         usermeta.umeta_id =  mysqlContext.Insert<bswp_usermeta>(usermeta);

            //    }

            //    decimal bounty = 50;
            //    if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["AffiliateDefaultBounty"]))
            //        decimal.TryParse(ConfigurationManager.AppSettings["AffiliateDefaultBounty"], out bounty);

            //    affiliate = new bswp_wpam_affiliates()
            //    {   // insert to affiliates 
            //        bountyAmount = bounty,
            //        bountyType = "percent",
            //        dateCreated = DateTime.UtcNow,
            //        email = user.Email,
            //        uniqueRefKey = user.UUID,
            //        userId = userId,
            //        status = "applied",
            //        addressLine1 = "",addressCity = "",companyName = "",
            //        firstName = "",lastName = "",nameOnCheck = "",
            //        paymentMethod = "manual",paypalEmail = "",phoneNumber = "",
            //        userData = "",addressCountry = "",addressLine2 = "",
            //        addressState = "",addressZipCode = "",websiteUrl = ""
            //    };


            //  affiliate.affiliateId =  mysqlContext.Insert<bswp_wpam_affiliates>(affiliate);
            //}
            //catch (Exception ex)
            //{
            //    _logger.InsertException(ex, "AffiliateManager", "RegisterAffiliate", 
            //        JsonConvert.SerializeObject(wpUser)     + "|"+  
            //        JsonConvert.SerializeObject(usermeta)   + "|" + 
            //        JsonConvert.SerializeObject(affiliate)  );
            //    return ServiceResponse.Error("Failed to create affliate.");
            //}

            //return ServiceResponse.OK("", affiliate);
        }

      
    }
}