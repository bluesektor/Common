using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreenWerx.Data;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.DataSets;
using GreenWerx.Managers.Geo;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.Managers.Membership
{
    public class ProfileManager : BaseManager, ICrud
    {
        private AppManager _appManager = null;
        private string _appSecret = "";
        private ProfileMemberManager _profileMemberManager = null;
        private RoleManager _roleManager = null;

        public ProfileManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "ProfileManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
            _appManager = new AppManager(this._connectionKey, "web", this.SessionKey);
            _roleManager = new RoleManager(_connectionKey, _requestingUser);
        }

        public static Profile ClearSensitiveData(Profile profile)
        {
            if (profile == null)
                return null;

            profile.UserCache = "";
            profile.LocationDetailCache = "";
            profile.MembersCache = "";
            profile.User = UserManager.ClearSensitiveData(profile.User);

            return profile;
        }

        public static List<Profile> ClearSensitiveData(List<Profile> profiles)
        {
            profiles.ForEach(am =>
            {
                am = ClearSensitiveData(am);
            });

            return profiles;
        }

        public static List<dynamic> ClearSensitiveData(List<dynamic> profiles)
        {
            profiles.ForEach(am =>
            {
                am = ClearSensitiveData(am);
            });

            return profiles;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (purge && context.Delete<Profile>((Profile)n) == 0)
                {
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
                }

                //get the profile from the table with all the data so when its updated it still contains the same data.
                res = this.Get(n.UUID);
                if (res.Code != 200)
                    return res;

                n.Deleted = true;
                if (context.Update<Profile>((Profile)n) == 0)
                    return ServiceResponse.Error(n.Name + " failed to delete. ");
            }
            return res;
        }

        //public ServiceResult Get(string uuid)
        //{
        //    if (string.IsNullOrWhiteSpace(uuid))
        //        return  ServiceResponse.Error("UUID was not sent.");
        //    using (var context = new GreenWerxDbContext(this._connectionKey))
        //    {
        //        var profile = context.GetAll<Profile>()?.FirstOrDefault(pw => pw.UUID == uuid);
        //        if (profile == null)
        //            return ServiceResponse.Error("Profile not found.");
        //        string reason;
        //        if (!ProfileAccessAuthorized(profile, out reason))
        //            return ServiceResponse.Unauthorized(reason);

        //        profile.Members = string.IsNullOrWhiteSpace(profile.MembersCache) ? context.GetAll<ProfileMember>().Where(w => w.ProfileUUID == profile.UUID).OrderBy(o => o.SortOrder).ToList() :
        //                                                                            JsonConvert.DeserializeObject<List<ProfileMember>>(profile.MembersCache);
        //        profile.User = string.IsNullOrWhiteSpace(profile.UserCache) ? context.GetAll<User>().FirstOrDefault(w => w.UUID == profile.UserUUID) :
        //                                                                            JsonConvert.DeserializeObject<User>(profile.UserCache);
        //        profile.LocationDetail = string.IsNullOrWhiteSpace(profile.LocationDetailCache) ? context.GetAll<Location>().FirstOrDefault(w => w.UUID == profile.LocationUUID) :
        //                                                                                            JsonConvert.DeserializeObject<Location>(profile.LocationDetailCache);

        //        profile.Attributes = context.GetAll<TMG.Attribute>()?.Where(w => w.AccountUUID == profile.AccountUUID
        //                  && w.ReferenceUUID == profile.UUID && w.ReferenceType.EqualsIgnoreCase(profile.UUIDType)).ToList();
        //        profile = ClearSensitiveData(profile);
        //        return ServiceResponse.OK("", profile);
        //    }
        //}

        // I. Check Blocked Roles
        //      a. did requestor (sf, cpl) block targets role (sm)
        //II. Check blocked users
        //      a. did requestor blocke target userUUID
        //      b. did requestor block target profileUUID
        public bool DidRequestorBlockTarget(Profile requesterProfile, Profile targetProfile, out string reason)
        {
            reason = string.Empty;

            var targetRoles = _roleManager.GetAssignedRoles(targetProfile.UserUUID, targetProfile.AccountUUID);// returns UserRoles
            var requestorsBlockedRoles = _roleManager.GetBlockedRoles(_requestingUser.UUID, _requestingUser.AccountUUID);

            // I. Check Blokecd Roles
            //      a. did target block requestors role (single male)
            foreach (var requestorBlockedRole in requestorsBlockedRoles)
            {
                // this doesn't work because uuid's are different because accounts
                foreach (var targetRole in targetRoles)
                {
                    if (requestorBlockedRole.Name.Contains("Block " + targetRole.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        reason = "You have blocked " + targetRole.Name + " in your preferences.";
                        return true;
                    }

                    if (requestorBlockedRole.RoleUUID == targetRole.RoleUUID)
                    {
                        reason = targetRole.Name + " is blocked by this member.";
                        return true;
                    }
                }
            }

            //II. Check blocked users
            //      a. did requestor blocke target userUUID
            //      b. did requestor block target profileUUID
            //II. Check blocked users
            var requestorsBlockedUsers = _roleManager.GetBlockedUsers(_requestingUser.UUID, _requestingUser.AccountUUID);
            foreach (var requestorsBlockedUser in requestorsBlockedUsers)
            {
                //      a. did requestor block targets userUUID
                if (requestorsBlockedUser.TargetUUID == targetProfile.UserUUID)
                {
                    reason = "You have blocked this member.";
                    return true;
                }
                //      b. did requestor block targes profileUUID
                if (requestorsBlockedUser.TargetUUID == targetProfile.UUID)
                {
                    reason = "You have blocked this profile.";
                    return true;
                }
            }

            ////cpl block sm
            //reason = "";
            //string requestingUserUUID = string.Empty;
            //string requestingAccountUUID = string.Empty;
            //string requestingProfileUUID = string.Empty;
            //try
            //{
            //    var payload = JWT.JsonWebToken.Decode(this.SessionKey, _appSecret, false);
            //    JwtClaims requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload);

            //    string[] tokens = requestorClaims.aud.Replace(SystemFlag.Default.Account, "systemdefaultaccount").Split('.');
            //    if (tokens.Length == 0)
            //    {
            //        reason = "Invalid token";
            //        return true;
            //    }

            //    requestingUserUUID = tokens[0];
            //    requestingAccountUUID = tokens[1];

            //    if ("systemdefaultaccount" == requestingAccountUUID)
            //        requestingAccountUUID = SystemFlag.Default.Account;

            //     requestingProfileUUID = tokens[2];
            //}
            //catch
            //{
            //}
            //// User Blocked?   _requestingUser.UUID, _requestingUser.UUIDType
            //var blockedUsers = _roleManager.GetBlockedUsers(_requestingUser.UUID, _requestingUser.AccountUUID);
            //if (blockedUsers.Any(w => w.TargetUUID == targetProfile.UUID))
            //    return true;
            //#region origianl
            ////if (_roleManager.IsInRole(requestingUserUUID, "block", targetProfile.UUID, targetProfile.UUIDType))
            ////{
            ////    reason = "Targeted";
            ////    return true;
            ////}
            //#endregion

            //Profile requesterProfile = null;
            //using (var context = new GreenWerxDbContext(this._connectionKey))
            //{
            //    requesterProfile = context.GetAll<Profile>().FirstOrDefault(w => w.UUID == _requestingUser.UUID && w.AccountUUID == _requestingUser.AccountUUID && w.Deleted == false);
            //}
            //// did targetProfile block a sm requestors  Profile Blocked?
            //var blockedProfiles = _roleManager.GetBlockedUsers(requesterProfile.UUID, requesterProfile.AccountUUID);
            //if (blockedProfiles.Any(w => w.TargetUUID == targetProfile?.UUID && w.UUIDType.EqualsIgnoreCase(targetProfile.UUIDType)))
            //    return true;

            //// if (_roleManager.IsInRole(targetProfile.UUID, "block", requesterProfile?.UUID, requesterProfile?.UUIDType)) //pre  table split
            ////   return true;
            //#region original code
            ////if (_roleManager.IsInRole(requestingUserUUID, "block", targetProfile.UserUUID, "User"))
            ////{
            ////    reason = "Targeted";
            ////    return true;
            ////}
            //#endregion

            ////Profile blocked
            //var targetProfileMemberRoles = _roleManager.GetRolesForUser(targetProfile.UUID, targetProfile.AccountUUID).Where(w => w.Category.EqualsIgnoreCase("member"));
            //var requestorBlockingRoles = _roleManager.GetRolesForUser(requestingProfileUUID, requestingAccountUUID).Where(w => w.Category.EqualsIgnoreCase("block"));

            //// check if the requesting user blocked the member role
            //foreach (var targetRole in targetProfileMemberRoles)
            //{
            //    foreach (var requestorBlockRole in requestorBlockingRoles)
            //    {
            //        if (targetRole.CategoryRoleName == requestorBlockRole.CategoryRoleName)
            //        {
            //            reason = "Role";
            //            return true;
            //        }
            //    }
            //}
            return false;
        }

        // I. Check Blocked Roles
        //      a. did target block(sm) requestors role (sf, cpl)
        //II. Check blocked users
        //      a. did target block requestors userUUID
        //      b. did target block requestors profileUUID
        // i.e. <cpl blocked sm> requestor = sm, target = cpl;  result: cant see profile
        public bool DidTargetBlockRequestor(Profile requesterProfile, Profile targetProfile, out string reason)
        {
            reason = string.Empty;

            var requestorRoles = _roleManager.GetAssignedRoles(_requestingUser.UUID, _requestingUser.AccountUUID);// returns UserRoles
            var targetsBlockedRoles = _roleManager.GetBlockedRoles(targetProfile.UserUUID, targetProfile.AccountUUID);

            // I. Check Blocked Roles
            //      a. did target (sm) block requestors role.
            foreach (var targetBlockedRole in targetsBlockedRoles)
            {
                foreach (var requestorRole in requestorRoles)
                {
                    if (targetBlockedRole.Name.Contains("Block " + requestorRole.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        reason = requestorRole.Name + " is blocked by this member.";
                        return true;
                    }

                    if (targetBlockedRole.RoleUUID == requestorRole.RoleUUID)
                    {
                        reason = requestorRole.Name + " is blocked by this member.";
                        return true;
                    }
                }
            }

            //II. Check blocked users
            var targetsBlockedUsers = _roleManager.GetBlockedUsers(targetProfile.UserUUID, targetProfile.AccountUUID);
            foreach (var targetsBlockedUser in targetsBlockedUsers)
            {
                //      a. did target block requestors userUUID
                if (targetsBlockedUser.TargetUUID == _requestingUser.UUID)
                {
                    reason = targetsBlockedUser.Name + " is blocked by this member.";
                    return true;
                }
                //      b. did target block requestors profileUUID
                if (targetsBlockedUser.TargetUUID == requesterProfile.UUID)
                {
                    reason = "Profile is blocked by this member.";
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="uuid">Profile UUID</param>
        /// <returns></returns>
        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("UUID was not sent.");
            string error = "";

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    DynamicParameters parameters = new DynamicParameters();// see function GetTableData

                    parameters.Add("@UUID", uuid);

                    string sql = @"SELECT [LocationUUID] ,[LocationType] ,[Theme],[View],[UserUUID],[GUUID],[GuuidType] ,[UUID]
                                      ,[UUIDType] ,[UUParentID],[UUParentIDType],[Name],[Status],[AccountUUID],[Active]
                                      ,[Deleted],[Private],[SortOrder],[CreatedBy],[DateCreated],[Image],[RoleWeight]
                                      ,[RoleOperation],[Description],[LookingFor],[MembersCache],[UserCache]
                                      ,[LocationDetailCache],[RelationshipStatus],[NSFW],[ShowPublic],[Latitude],[Longitude]
                                      ,[VerificationsCache]
                                    FROM[dbo].[Profiles] p
                                    WHERE
                                        p.UUID = @UUID AND
	                                    (p.Private = 0 ) AND
	                                    (p.Deleted = 0 );
                                  SELECT [DOB],[DobType],[Gender],[UserUUID],[Height],[HeightUOM],[Weight],[WeightUOM],[BodyFat]
                                            ,[GUUID],[GuuidType],[UUID],[UUIDType],[UUParentID],[UUParentIDType],[Name]
                                            ,[Status],[AccountUUID],[Active],[Deleted],[Private],[SortOrder],[CreatedBy]
                                            ,[DateCreated],[Image],[RoleWeight],[RoleOperation],[Description],[ProfileUUID]
                                            ,[LookingFor],[Preference],[Orientation],[RelationshipStatus],[NSFW]
                                    FROM [dbo].[ProfileMembers] pm
                                    WHERE pm.ProfileUUID = @UUID;
                                  SELECT l.[RootId],l.[Abbr],l.[Code],l.[CurrencyUUID],l.[LocationType],l.[Latitude],l.[Longitude]
                                        ,l.[TimeZone],l.[FirstName],l.[LastName],l.[Address1],l.[Address2],l.[City],l.[State]
                                        ,l.[Country],l.[Postal],l.[Type],l.[Description],l.[IsBillingAddress],l.[Virtual]
                                        ,l.[isDefault],l.[GUUID],l.[GuuidType],l.[UUID],l.[UUIDType],l.[UUParentID],l.[UUParentIDType]
                                        ,l.[Name],l.[Status],l.[AccountUUID],l.[Active],l.[Deleted],l.[Private],l.[SortOrder],l.[CreatedBy]
                                        ,l.[DateCreated],l.[Image],l.[RoleWeight],l.[RoleOperation],l.[AccountReference],l.[Category],l.[County]
                                        ,l.[NSFW]
                                    FROM [dbo].Locations l
                                    LEFT JOIN [dbo].[Profiles] p on p.LocationUUID = l.UUID
                                    WHERE p.UUID = @UUID;
                                  SELECT [Value],[ValueType],[ReferenceUUID],[ReferenceType],[GUUID],[GuuidType],[UUID]
                                        ,[UUIDType],[UUParentID],[UUParentIDType],[Name],[Status],[AccountUUID]
                                        ,[Active],[Deleted],[Private],[SortOrder],[CreatedBy],[DateCreated]
                                        ,[Image],[RoleWeight],[RoleOperation],[Description],[UserUUID],[NSFW]
                                   FROM [dbo].[Attributes] a
                                    WHERE   a.ReferenceUUID = @UUID AND
                                            a.ReferenceType = 'Profile';
                                  SELECT [UUID]      ,[UUIDType]      ,[VerificationDate]      ,[RecipientUUID]      ,[RecipientProfileUUID]
                                          ,[RecipientAccountUUID]           ,[RecipientLocationUUID]      ,[VerifierUUID]
                                          ,[Points]      ,[Deleted]     ,[VerificationType]
                                      FROM UserVerificationLog
                                      WHERE RecipientProfileUUID = @UUID;
                             SELECT u.[GUUID],u.[GuuidType],u.[UUID],u.[UUIDType],u.[UUParentID],u.[UUParentIDType]
                                        ,u.[Name],u.[Status],u.[AccountUUID],u.[Active],u.[Deleted],u.[Private],u.[SortOrder],u.[CreatedBy]
                                        ,u.[DateCreated],u.[Image],u.[RoleWeight],u.[RoleOperation],u.[NSFW]
                                    FROM [dbo].Users u
                                    LEFT JOIN [dbo].[Profiles] p on p.UserUUID = u.UUID
                                    WHERE p.UUID = @UUID;";

                    //                -- ,[RecipientIP] ,[VerifierIP]      ,[VerifierProfileUUID]      ,[VerifierAccountUUID]      ,[VerifierRoleUUID]      ,[VerifierLocationUUID]
                    //   --,[VerifierLatitude]      ,[VerifierLongitude]      ,[RecipientLatitude]      ,[RecipientLongitude]
                    //-- ,[Weight]      ,[Multiplier]   ,[VerificationTypeMultiplier]      ,[DateDeleted]

                    var multi = context.Database.Connection.QueryMultiple(sql, parameters);
                    var profile = multi.Read<Profile>()?.FirstOrDefault();
                    if (profile == null)
                        return ServiceResponse.Error("No profile found:" + uuid);

                    //
                    profile.Members = multi.Read<ProfileMember>()?.ToList();
                    profile.LocationDetail = multi.Read<Location>()?.FirstOrDefault();
                    profile.Attributes = multi.Read<TMG.Attribute>()?.ToList();
                    profile.Verifications = multi.Read<VerificationEntry>()?.ToList();
                    profile.User = multi.Read<User>()?.FirstOrDefault();

                    //profiles = profiles.Select(s => new
                    //{
                    //    Name = s.Name,
                    //    UUID = s.UUID,
                    //    UUIDType = s.UUIDType,
                    //    Image = s.Image,
                    //    // Email = s.Email,
                    //    Active = s.Active,
                    //    Description = s.Description,
                    //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ?
                    //   _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID, tmpFilter) :
                    //   JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache),
                    //    User = string.IsNullOrWhiteSpace(s.UserCache) ?
                    //   ConvertResult(userManager.Get(s.UserUUID)) :
                    //   JsonConvert.DeserializeObject<User>(s.UserCache),
                    //    LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ?
                    //   ConvertLocationResult(locationManager.Get(s.LocationUUID)) :
                    //   JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),
                    //}

                    profile = ClearSensitiveData(profile);
                    return ServiceResponse.OK("", profile);
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                error = ex.DeserializeException();
            }
            return ServiceResponse.Error("No profile found.", error);
        }

        public List<Profile> GetAllProfiles(ref DataFilter filter)
        {
            List<Profile> profiles;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                DynamicParameters parameters = new DynamicParameters();// see function GetTableData
                parameters.Add("@DELETED", filter.IncludeDeleted);

                string sql = @"SELECT COUNT(*) FROM Profiles p
                                   WHERE
	                                (p.Private = 0) AND
	                                (p.ShowPublic = 1 OR p.Deleted = @DELETED)";

                var profileWhereQuery = DatasetManager.GetProfileQueryFilter(filter.Screens);

                if (!string.IsNullOrWhiteSpace(profileWhereQuery))
                    sql = sql + " AND (" + profileWhereQuery + ")";

                string where = DatasetManager.BuildWhereClause(filter.Screens);
                if (!string.IsNullOrWhiteSpace(where))
                {
                    sql += " AND " + where;
                    var screenParams = DatasetManager.GetParameters(filter.Screens);
                    if (screenParams != null)
                        parameters.AddDynamicParams(screenParams);
                }

                var res = context.ExecuteScalar(sql, parameters);
                if (res != null)
                    filter.TotalRecordCount = (int)res;

                if (filter.TotalRecordCount == 0)
                    return new List<Profile>();
                parameters.Add("@PRIVATE", filter.IncludePrivate);
                parameters.Add("@DELETED", filter.IncludeDeleted);
                parameters.Add("@CLIENTLAT", filter.Latitude);
                parameters.Add("@CLIENTLON", filter.Longitude);
                parameters.Add("@MEASURE", 3956.55); // 3956.55 = miles
                sql = @"SELECT CEILING(dbo.CalcDistance(@clientLat, @clientLon , p.Latitude, p.Longitude, @MEASURE )) as Distance
		                , p.LocationUUID      ,p.LocationType      ,p.Theme           ,p.UserUUID
                      ,p.GUUID      ,p.GuuidType      ,p.UUID      ,p.UUIDType      ,p.UUParentID
                      ,p.UUParentIDType      ,p.Name      ,p.Status      ,p.AccountUUID      ,p.Active
                      ,p.Deleted      ,p.Private      ,p.SortOrder      ,p.CreatedBy      ,p.DateCreated
                      ,p.Image      ,p.RoleWeight      ,p.RoleOperation      ,p.Description      ,p.LookingFor
                      ,p.MembersCache      ,p.UserCache      ,p.LocationDetailCache      ,p.RelationshipStatus
                      ,p.NSFW      ,p.ShowPublic      ,p.Latitude      ,p.Longitude      ,p.VerificationsCache
                  FROM [dbo].[Profiles] p
                  WHERE
	                (p.Private = 0 OR p.Private = @private) AND
	                (p.Deleted = 0 OR p.Deleted = @deleted)";

                if (!string.IsNullOrWhiteSpace(profileWhereQuery))
                    sql = sql + " AND (" + profileWhereQuery + ")";

                where = DatasetManager.BuildWhereClause(filter.Screens);
                if (!string.IsNullOrWhiteSpace(where))
                {
                    sql += " AND " + where;
                }

                if (filter.SortBy.EqualsIgnoreCase("random") || string.IsNullOrWhiteSpace(filter.SortBy))
                {
                    sql += " ORDER BY NEWID()";// return random rows for profiles. mix it up a litle
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

                    parameters.Add("@PAGESIZE", filter.PageSize);
                    parameters.Add("@PAGEINDEX", filter.Page);
                    sql += @" OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY";
                }

                profiles = context.Select<Profile>(sql, parameters).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            profiles = ProfileManager.ClearSensitiveData(profiles);
            return profiles;
        }

        public ServiceResult GetProfile(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return ServiceResponse.Error("User name was not sent.");

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    var userUUID = context.GetAll<User>().FirstOrDefault(w => w.Name.EqualsIgnoreCase(userName))?.UUID;
                    if (string.IsNullOrWhiteSpace(userUUID))
                        return ServiceResponse.Error("User was not found for name." + userName);

                    var uuid = context.GetAll<Profile>().FirstOrDefault(w => w.UserUUID == userUUID && w.Active == true)?.UUID;
                    // get profileUUID from profiles
                    if (string.IsNullOrWhiteSpace(uuid))
                        return ServiceResponse.Error("Profile was not found for user uuid. " + userUUID);

                    DynamicParameters parameters = new DynamicParameters();// see function GetTableData

                    parameters.Add("@UUID", uuid);

                    string sql = @"SELECT [LocationUUID] ,[LocationType] ,[Theme],[View],[UserUUID],[GUUID],[GuuidType] ,[UUID]
                                      ,[UUIDType] ,[UUParentID],[UUParentIDType],[Name],[Status],[AccountUUID],[Active]
                                      ,[Deleted],[Private],[SortOrder],[CreatedBy],[DateCreated],[Image],[RoleWeight]
                                      ,[RoleOperation],[Description],[LookingFor],[MembersCache],[UserCache]
                                      ,[LocationDetailCache],[RelationshipStatus],[NSFW],[ShowPublic],[Latitude],[Longitude]
                                      ,[VerificationsCache]
                                  FROM[dbo].[Profiles] p
                                  WHERE
                                     p.UUID = @UUID AND
	                                (p.Private = 0 ) AND
	                                (p.Deleted = 0 );
                                  SELECT [DOB],[DobType],[Gender],[UserUUID],[Height],[HeightUOM],[Weight],[WeightUOM],[BodyFat]
                                            ,[GUUID],[GuuidType],[UUID],[UUIDType],[UUParentID],[UUParentIDType],[Name]
                                            ,[Status],[AccountUUID],[Active],[Deleted],[Private],[SortOrder],[CreatedBy]
                                            ,[DateCreated],[Image],[RoleWeight],[RoleOperation],[Description],[ProfileUUID]
                                            ,[LookingFor],[Preference],[Orientation],[RelationshipStatus],[NSFW]
                                         FROM [dbo].[ProfileMembers] pm WHERE pm.ProfileUUID = @UUID;
                                  SELECT l.[RootId],l.[Abbr],l.[Code],l.[CurrencyUUID],l.[LocationType],l.[Latitude],l.[Longitude]
                                        ,l.[TimeZone],l.[FirstName],l.[LastName],l.[Address1],l.[Address2],l.[City],l.[State]
                                        ,l.[Country],l.[Postal],l.[Type],l.[Description],l.[IsBillingAddress],l.[Virtual]
                                        ,l.[isDefault],l.[GUUID],l.[GuuidType],l.[UUID],l.[UUIDType],l.[UUParentID],l.[UUParentIDType]
                                        ,l.[Name],l.[Status],l.[AccountUUID],l.[Active],l.[Deleted],l.[Private],l.[SortOrder],l.[CreatedBy]
                                        ,l.[DateCreated],l.[Image],l.[RoleWeight],l.[RoleOperation],l.[AccountReference],l.[Category],l.[County]
                                        ,l.[NSFW]
                                    FROM [dbo].Locations l
                                    LEFT JOIN [dbo].[Profiles] p on p.LocationUUID = l.UUID
                                    WHERE p.UUID = @UUID;
                                  SELECT [Value],[ValueType],[ReferenceUUID],[ReferenceType],[GUUID],[GuuidType],[UUID]
                                        ,[UUIDType],[UUParentID],[UUParentIDType],[Name],[Status],[AccountUUID]
                                        ,[Active],[Deleted],[Private],[SortOrder],[CreatedBy],[DateCreated]
                                        ,[Image],[RoleWeight],[RoleOperation],[Description],[UserUUID],[NSFW]
                                   FROM [dbo].[Attributes] a WHERE a.ReferenceUUID = @UUID AND a.ReferenceType = 'Profile';
                                    SELECT [UUID]      ,[UUIDType]      ,[VerificationDate]      ,[RecipientUUID]      ,[RecipientProfileUUID]
                                          ,[RecipientAccountUUID]           ,[RecipientLocationUUID]      ,[VerifierUUID]
                                          ,[Points]      ,[Deleted]     ,[VerificationType]
                                      FROM UserVerificationLog
                                      WHERE RecipientProfileUUID = @UUID;
                                    SELECT u.[GUUID],u.[GuuidType],u.[UUID],u.[UUIDType],u.[UUParentID],u.[UUParentIDType]
                                        ,u.[Name],u.[Status],u.[AccountUUID],u.[Active],u.[Deleted],u.[Private],u.[SortOrder],u.[CreatedBy]
                                        ,u.[DateCreated],u.[Image],u.[RoleWeight],u.[RoleOperation],u.[NSFW]
                                    FROM [dbo].Users u
                                    LEFT JOIN [dbo].[Profiles] p on p.UserUUID = u.UUID
                                    WHERE p.UUID = @UUID;";
                    //                -- ,[RecipientIP] ,[VerifierIP]      ,[VerifierProfileUUID]      ,[VerifierAccountUUID]      ,[VerifierRoleUUID]      ,[VerifierLocationUUID]
                    //   --,[VerifierLatitude]      ,[VerifierLongitude]      ,[RecipientLatitude]      ,[RecipientLongitude]
                    //-- ,[Weight]      ,[Multiplier]   ,[VerificationTypeMultiplier]      ,[DateDeleted]

                    var multi = context.Database.Connection.QueryMultiple(sql, parameters);
                    var profile = multi.Read<Profile>().FirstOrDefault();
                    if (profile == null)
                        return ServiceResponse.Error("No profile found.");

                    profile.Members = multi.Read<ProfileMember>().ToList();
                    profile.LocationDetail = multi.Read<Location>().FirstOrDefault();
                    profile.Attributes = multi.Read<TMG.Attribute>().ToList();
                    profile.Verifications = multi.Read<VerificationEntry>().ToList();
                    profile.User = multi.Read<User>().FirstOrDefault();
                    profile = ClearSensitiveData(profile);
                    return ServiceResponse.OK("", profile);
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return ServiceResponse.Error("No profile found.");
        }

        public ServiceResult GetProfile(string userUUID, string accountUUID, bool includeAttributes)
        {
            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("No user id sent.");
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var profile = context.GetAll<Profile>()?//.OrderByDescending(po => po.DateCreated)?.
                    .FirstOrDefault(pw => pw.Active == true &&
                                   pw.UserUUID == userUUID &&
                                   pw.AccountUUID == accountUUID);
                if (profile != null)
                {
                    string reason;
                    if (!ProfileAccessAuthorized(profile, out reason))
                        return ServiceResponse.Unauthorized(reason);

                    _profileMemberManager = new ProfileMemberManager(this._connectionKey, this.SessionKey);
                    try
                    {
                        var filter = new DataFilter();
                        profile.Members = string.IsNullOrWhiteSpace(profile.MembersCache) ? _profileMemberManager.GetProfileMembers(profile.UUID, profile.AccountUUID, ref filter) :
                                                                                            JsonConvert.DeserializeObject<List<ProfileMember>>(profile.MembersCache);

                        profile.User = string.IsNullOrWhiteSpace(profile.UserCache) ? context.GetAll<User>().FirstOrDefault(w => w.UUID == profile.UserUUID) :
                                                                                        JsonConvert.DeserializeObject<User>(profile.UserCache);

                        profile.LocationDetail = (string.IsNullOrWhiteSpace(profile.LocationDetailCache) || profile.LocationDetailCache.EqualsIgnoreCase("null"))
                            ? context.GetAll<Location>().FirstOrDefault(w => w.UUID == profile.LocationUUID) :
                              JsonConvert.DeserializeObject<Location>(profile.LocationDetailCache);
                    }
                    catch (Exception ex)
                    {
                        profile = ResetCache(profile);
                    }
                    if (includeAttributes)
                    {
                        profile.Attributes = context.GetAll<TMG.Attribute>()?.Where(w => w.AccountUUID == accountUUID
                            && w.ReferenceUUID == profile.UUID && w.ReferenceType.EqualsIgnoreCase(profile.UUIDType)).ToList();
                    }
                }
                profile = ProfileManager.ClearSensitiveData(profile);
                return ServiceResponse.OK("", profile);
            }
        }

        //todo filter out blocked profiles
        public List<Profile> GetProfiles(string userUUID, string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(userUUID))
                return new List<Profile>();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                // todo cache
                //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
                //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
                //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

                var profiles = context.GetAll<Profile>()?.Where(pw => pw.UserUUID == userUUID && pw.AccountUUID == accountUUID)?.OrderByDescending(po => po.DateCreated).ToList();
                profiles = ProfileManager.ClearSensitiveData(profiles);
                return profiles;
            }
        }

        public List<Profile> GetProfiles(string accountUUID, bool deleted = false, bool includeSystemAccount = false)
        {
            List<Profile> profiles = new List<Profile>();
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            // todo cache
            //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
            //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
            //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (includeSystemAccount)
                {
                    profiles = context.GetAll<Profile>()?.Where(sw => (sw.AccountUUID == accountUUID || sw.AccountUUID == SystemFlag.Default.Account) && sw.Deleted == deleted).GroupBy(x => x.Name).Select(group => group.First()).OrderBy(ob => ob.Name).ToList();
                }
                else
                {
                    profiles = context.GetAll<Profile>()?.Where(sw => (sw.AccountUUID == accountUUID) && sw.Deleted == deleted).OrderBy(ob => ob.Name).ToList();
                }
                profiles = ProfileManager.ClearSensitiveData(profiles);
                return profiles;
            }
        }

        public List<Profile> GetPublicProfiles(ref DataFilter filter)
        {
            List<Profile> profiles;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                DynamicParameters parameters = new DynamicParameters();// see function GetTableData
                parameters.Add("@DELETED", filter.IncludeDeleted);

                string sql = @"SELECT COUNT(*) FROM Profiles e
                                   WHERE
	                                (e.Private = 0) AND
	                                (e.ShowPublic = 1 OR e.Deleted = @DELETED)";

                var profileWhereQuery = DatasetManager.GetProfileQueryFilter(filter.Screens);

                if (!string.IsNullOrWhiteSpace(profileWhereQuery))
                    sql = sql + " AND (" + profileWhereQuery + ")";

                string where = DatasetManager.BuildWhereClause(filter.Screens);
                if (!string.IsNullOrWhiteSpace(where))
                {
                    sql += " AND " + where;
                    var screenParams = DatasetManager.GetParameters(filter.Screens);
                    if (screenParams != null)
                        parameters.AddDynamicParams(screenParams);
                }

                filter.TotalRecordCount = (int)context.ExecuteScalar(sql, parameters);

                //status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Couple) ||
                //             status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Group) ||
                //             status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Poly) ||
                //             status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Single)

                if (filter.TotalRecordCount == 0)
                    return new List<Profile>();

                parameters.Add("@CLIENTLAT", filter.Latitude);
                parameters.Add("@CLIENTLON", filter.Longitude);
                parameters.Add("@MEASURE", 3956.55); // 3956.55 = miles
                sql = @"SELECT CEILING(dbo.CalcDistance(@clientLat, @clientLon , p.Latitude, p.Longitude, @MEASURE )) as Distance
		                            , [LocationUUID]      ,[LocationType]      ,[Theme]      ,[View]      ,[UserUUID]
                                  ,[GUUID]      ,[GuuidType]      ,[UUID]      ,[UUIDType]      ,[UUParentID]
                                  ,[UUParentIDType]      ,[Name]      ,[Status]      ,[AccountUUID]      ,[Active]
                                  ,[Deleted]      ,[Private]      ,[SortOrder]      ,[CreatedBy]      ,[DateCreated]
                                  ,[Image]      ,[RoleWeight]      ,[RoleOperation]      ,[Description]      ,[LookingFor]
                                  ,[MembersCache]      ,[UserCache]      ,[LocationDetailCache]      ,[RelationshipStatus]
                                  ,[NSFW]      ,[ShowPublic]      ,[Latitude]      ,[Longitude]      ,[VerificationsCache]
                              FROM Profiles p
                              WHERE
	                            (p.Private = 0  ) AND
	                             P.ShowPublic = 1 AND
	                            (p.Deleted = 0 OR p.Deleted = @deleted)";

                where = DatasetManager.BuildWhereClause(filter.Screens);
                if (!string.IsNullOrWhiteSpace(where))
                {
                    sql += " AND " + where;
                }

                if (filter.SortBy.EqualsIgnoreCase("random") || string.IsNullOrWhiteSpace(filter.SortBy))
                {
                    //  parameters.Add("@SORTBY", "Name");
                    // parameters.Add("@SORTDIR", "ASC");
                    //  sql += " ORDER BY @SORTBY @SORTDIR";
                    sql += " ORDER BY NEWID()";// If no sort order is set, return random rows for profiles. mix it up a litle
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

                    parameters.Add("@PAGESIZE", filter.PageSize);
                    parameters.Add("@PAGEINDEX", filter.Page);
                    sql += @" OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY";
                }

                profiles = context.Select<Profile>(sql, parameters).ToList();
            }
            ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            profiles = ProfileManager.ClearSensitiveData(profiles);
            return profiles;
        }

        public IEnumerable<dynamic> GetUserProfiles(string accountUUID, ref DataFilter filter)
        {
            Stopwatch sw = new Stopwatch();
            //IEnumerable<dynamic> userProfilesEnum = null;
            //using (var context = new GreenWerxDbContext(this._connectionKey))
            //{
            //    sw.Start();
            //    userProfilesEnum =
            //      (from am in context.GetAll<Profile>()
            //       join users in context.GetAll<User>()
            //                        ?.Where(uw => uw.Deleted == false)
            //                         .Cast<dynamic>()
            //                         .Filter(ref filter) //NOTE the filter here
            //       on am.UserUUID equals users.UUID
            //       select am).Cast<dynamic>();
            //    sw.Stop();
            //    var el = sw.ElapsedMilliseconds;
            //    sw.Reset();
            //    ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            //}
            //return userProfilesEnum;
            List<dynamic> userProfiles = new List<dynamic>();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                sw.Start();
                userProfiles =
                  (from am in context.GetAll<Profile>()
                   join users in context.GetAll<User>()
                                    ?.Where(uw => uw.Deleted == false)
                                     .Cast<dynamic>().ToList()
                                     .Filter(ref filter) //NOTE the filter here
                   on am.UserUUID equals users.UUID
                   select am).Cast<dynamic>().ToList();

                sw.Stop();
                var el = sw.ElapsedMilliseconds;
                ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                userProfiles = ProfileManager.ClearSensitiveData(userProfiles);

                return userProfiles;
            }
        }

        public ServiceResult Insert(INode n)
        {
            // todo cache
            //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
            //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
            //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Profile)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Profile dbU = context.GetAll<Profile>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Profile already exists.");

                if (context.Insert<Profile>((Profile)s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting profile " + s.Name);
        }

        public bool ProfileAccessAuthorized(Profile p, out string reason)
        {
            reason = string.Empty;
            _appSecret = _appManager.GetSetting("AppKey")?.Value;

            var tmp = Cipher.Crypt(_appSecret, "@tH3Pl4ayR0om", true);

            if (!IsRequesterAuthorized(p, out reason))
                return false;

            //todo was blocked by admin?

            #region did request own profile? if so return true

            JwtClaims requestorClaims = null;

            try
            {
                var payload2 = JWT.JsonWebToken.Decode(this.SessionKey, _appSecret, false);
                requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload2);

                TimeSpan ts = requestorClaims.expires - DateTime.UtcNow;

                if (ts.TotalSeconds <= 0)
                {
                    reason = "Session expired.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                //todo us.IsPersistent
                return false;
            }

            string[] tokens = requestorClaims.aud.Replace(SystemFlag.Default.Account, "systemdefaultaccount").Split('.');
            if (tokens.Length == 0)
                return true;

            string requestingUserUUID = tokens[0];
            string requestingAccountUUID = tokens[1];
            if ("systemdefaultaccount" == requestingAccountUUID)
                requestingAccountUUID = SystemFlag.Default.Account;

            string requestingProfileUUID = tokens[2];

            if (p.UserUUID == requestingUserUUID && p.AccountUUID == requestingAccountUUID && p.UUID == requestingProfileUUID)
                return true;

            #endregion did request own profile? if so return true

            #region Check if session is still valid

            string payload = "";
            try
            {
                payload = JWT.JsonWebToken.Decode(this.SessionKey, _appSecret, false);
                requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload);
                TimeSpan ts = requestorClaims.expires - DateTime.UtcNow;

                if (ts.TotalSeconds <= 0)
                    return false;
            }
            catch (Exception ex)
            {
                //todo if  us.IsPersistent  {
                //payload = JWT.JsonWebToken.Decode(this.SessionKey, _appSecret, false);
                // requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload);
                //}

                return true;
            }

            #endregion Check if session is still valid

            Profile requesterProfile = new Profile();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                requesterProfile = context.GetAll<Profile>().FirstOrDefault(w => w.UUID == _requestingUser.UUID && w.AccountUUID == _requestingUser.AccountUUID && w.Deleted == false);
            }

            if (DidTargetBlockRequestor(requesterProfile, p, out reason))
                return false;

            if (DidRequestorBlockTarget(requesterProfile, p, out reason))
                return false;

            return true;
        }

        public List<Profile> Search(string name)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            if (string.IsNullOrWhiteSpace(name))
                return new List<Profile>();

            // todo cache
            //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
            //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
            //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var profiles = context.GetAll<Profile>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
                profiles = ProfileManager.ClearSensitiveData(profiles);
                return profiles;
            }
        }

        //}
        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Profile data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Profile>((Profile)n) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Profile was not updated.");
        }

        public async Task<ServiceResult> UpdateCache(string userUUID, string accountUUID)
        {
            var res = this.GetProfile(userUUID, accountUUID, true);
            if (res.Code != 200)
                return res;
            var profile = (Profile)res.Result;
            profile = this.ResetCache(profile);
            var filter = new DataFilter();
            var profileMemberManager = new ProfileMemberManager(this._connectionKey, this.SessionKey);
            var members = profileMemberManager.GetProfileMembers(profile.UUID, accountUUID, ref filter);
            if (members != null && members.Count > 0)
                profile.MembersCache = JsonConvert.SerializeObject(members);

            var userManager = new UserManager(this._connectionKey, this.SessionKey);
            res = userManager.GetUser(userUUID);
            if (res.Code != 200)
                return res;

            profile.UserCache = JsonConvert.SerializeObject(UserManager.ClearSensitiveData((User)res.Result));

            var locationManager = new LocationManager(this._connectionKey, this.SessionKey);
            res = locationManager.Get(profile.LocationUUID);
            if (res.Code != 200)
                return res;

            profile.LocationDetailCache = JsonConvert.SerializeObject((Location)res.Result);

            var verMan = new VerificationManager(this._connectionKey, this.SessionKey);
            var vers = verMan.GetVerificationEntries(profile.UUID);
            if (vers != null && vers.Count > 0)
                profile.VerificationsCache = JsonConvert.SerializeObject(vers);

            return this.Update(profile);
        }

        private Profile ResetCache(Profile p)
        {
            p.MembersCache = string.Empty;
            p.UserCache = string.Empty;
            p.LocationDetailCache = string.Empty;
            p.VerificationsCache = string.Empty;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                context.Update<Profile>(p);
            }
            return p;
        }

        //public INode GetByGetByGUUID(string GUUID)
        //{
        //    if (string.IsNullOrWhiteSpace(GUUID))
        //        return null;
        //    using (var context = new GreenWerxDbContext(this._connectionKey))
        //    {
        //        // todo cache
        //        //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
        //        //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
        //        //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

        //        return context.GetAll<Profile>()?.FirstOrDefault(sw => sw.GUUID == GUUID);
        //    }
        //}

        //public ProfileMember GetCurrentProfile(string userUUID)
        //{
        //    if (string.IsNullOrWhiteSpace(userUUID))
        //        return null;
        //    using (var context = new GreenWerxDbContext(this._connectionKey))
        //    {
        //        return context.GetAll<Profile>().OrderByDescending(po => po.DateCreated)?.FirstOrDefault(pw => pw.UserUUID == userUUID);

        //        // var profile =  context.GetAll<Profile>().OrderByDescending(po => po.DateCreated)?.FirstOrDefault(pw => pw.UserUUID == userUUID);

        //        //profile.Members = string.IsNullOrWhiteSpace(profile.MembersCache) ? context.GetAll<ProfileMember>().Where(w => w.ProfileUUID == profile.UUID).OrderBy(o => o.SortOrder).ToList() :
        //        //                                                               JsonConvert.DeserializeObject<List<ProfileMember>>(profile.MembersCache);
        //        //profile.User = string.IsNullOrWhiteSpace(profile.UserCache) ? context.GetAll<User>().FirstOrDefault(w => w.UUID == profile.UserUUID) :
        //        //                                                                    JsonConvert.DeserializeObject<User>(profile.UserCache);
        //        //profile.LocationDetail = string.IsNullOrWhiteSpace(profile.LocationDetailCache) ? context.GetAll<Location>().FirstOrDefault(w => w.UUID == profile.LocationUUID) :
        //        //                                                                                    JsonConvert.DeserializeObject<Location>(profile.LocationDetailCache),;

        //        //profile.Attributes = context.GetAll<TMG.Attribute>()?.Where(w => w.AccountUUID == profile.AccountUUID
        //        //          && w.ReferenceUUID == profile.UUID && w.ReferenceType.EqualsIgnoreCase(profile.UUIDType)).ToList();
        //        // return profile;
        //    }
        //}

        #region from user manager

        public void DeleteProfile(string profileUUID)
        {
            if (string.IsNullOrWhiteSpace(profileUUID))
                return;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Profile p = context.GetAll<Profile>()?.FirstOrDefault(w => w.UUID == profileUUID);
                context.Delete<Profile>(p);
            }
        }

        public ServiceResult InsertProfile(Profile p)
        {
            // todo cache
            //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
            //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
            //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

            if (p == null)
                return ServiceResponse.Error("Invalid profile.");

            p.UUID = Guid.NewGuid().ToString("N");

            p.DateCreated = DateTime.UtcNow;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<Profile>(p))
                    return ServiceResponse.OK("", p);
            }
            return ServiceResponse.Error("Failed to add profile.");
        }

        public Profile SetActiveProfile(string profileUUID, string userUUID, string accountUUID)
        {
            Profile active = new Profile();
            //todo set all other for user account to not active
            //then set this to active
            if (string.IsNullOrWhiteSpace(profileUUID) || string.IsNullOrWhiteSpace(userUUID) || string.IsNullOrWhiteSpace(accountUUID))
                return null;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var profiles = context.GetAll<Profile>().Where(w => w.UserUUID == userUUID && w.AccountUUID == accountUUID).ToList();
                if (profiles.Count() == 0)
                    return null;

                foreach (Profile p in profiles)
                {
                    if (p.UUID == profileUUID)
                    {
                        p.Active = true;
                        context.Update(p);
                        active = p;
                        continue;
                    }

                    if (p.Active)
                    {
                        p.Active = false;
                        context.Update(p);
                    }
                }
            }
            return active;
        }

        public ServiceResult UpdateProfile(Profile p)
        {
            if (p == null)
                return ServiceResponse.Error("Invalid profile.");

            p.DateCreated = DateTime.UtcNow;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                try
                {
                    p.MembersCache = JsonConvert.SerializeObject(context.GetAll<ProfileMember>().Where(w => w.ProfileUUID == p.UUID).OrderBy(o => o.SortOrder).ToList());
                }
                catch { }
                try
                {
                    p.UserCache = JsonConvert.SerializeObject(UserManager.ClearSensitiveData(context.GetAll<User>().FirstOrDefault(w => w.UUID == p.UserUUID)));
                }
                catch { }
                try
                {
                    p.LocationDetailCache = JsonConvert.SerializeObject(context.GetAll<Location>().FirstOrDefault(w => w.UUID == p.LocationUUID));
                }
                catch { }

                if (context.Update<Profile>(p) > 0)
                    return ServiceResponse.OK("", p);
            }
            return ServiceResponse.Error("Failed to save profile.");
        }

        #endregion from user manager

        //private List<Profile> FindProfile(string uuid, string name, string AccountUUID, bool includeDefaultAccount = true)
        //{
        //    // todo cache
        //    //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
        //    //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
        //    //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

        //    List<Profile> res = null;

        //    if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(name) == false)
        //        res = this.Search(name);
        //    else
        //    {
        //        using (var context = new GreenWerxDbContext(this._connectionKey))
        //        {
        //            res = context.GetAll<Profile>()?.Where(w => w.UUID == uuid || (w.Name?.EqualsIgnoreCase(name) ?? false)).ToList();
        //        }
        //    }

        //    if (res == null)
        //        return res;

        //    if (includeDefaultAccount && (res.FirstOrDefault().AccountUUID == AccountUUID))
        //        return res;

        //    if (res.FirstOrDefault().AccountUUID == AccountUUID)
        //        return res;

        //    return new List<Profile>();
        ///// <summary>
        ///// We log profiles instead of having a single record.
        ///// This way the user can track changes.
        ///// </summary>
        ///// <param name="p"></param>
        ///// <returns></returns>
        //public ServiceResult LogUserProfile(ProfileMember p)
        //{
        //    if (p == null)
        //        return ServiceResponse.Error("Invalid profile.");

        //    p.DateCreated = DateTime.UtcNow;
        //    using (var context = new GreenWerxDbContext(this._connectionKey))
        //    {
        //        if (context.Insert<ProfileMember>(p))
        //            return ServiceResponse.OK();
        //    }
        //    return ServiceResponse.Error("Failed to save profile.");
        //}
    }
}