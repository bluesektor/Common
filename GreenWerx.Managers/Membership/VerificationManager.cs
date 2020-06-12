using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Membership
{
    public class VerificationManager : BaseManager
    {
        public VerificationManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "VerificationManager CONTEXT IS NULL!");

            this.SessionKey = sessionKey;
            this._connectionKey = connectionKey;
        }

        //todo remove from role if logcount for type == 0
        public ServiceResult Delete(VerificationEntry n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            //if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (purge && context.Delete<VerificationEntry>((VerificationEntry)n) == 0)
                {
                    return ServiceResponse.Error("Failed to delete verification");
                }

                //get the VerificationEntry from the table with all the data so when its updated it still contains the same data.
                res = this.Get(n.UUID);
                if (res.Code != 200)
                    return res;

                n.Deleted = true;
                if (context.Update<VerificationEntry>((VerificationEntry)n) == 0)
                    return ServiceResponse.Error("Failed to delete verification. ");
            }
            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<VerificationEntry>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("VerificationEntry not found for uuid.");
                return ServiceResponse.OK("", res);
            }
        }

        public List<VerificationEntry> GetVerificationEntries(string recipientProfileUUID, bool deleted = false)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var tmp = context.GetAll<VerificationEntry>()?.Where(sw => sw.RecipientProfileUUID == recipientProfileUUID && sw.Deleted == deleted)
                    .OrderBy(ob => ob.VerificationDate)
                    .ToList();
                return tmp;
            }
        }

        //todo add to role if not already in it
        public ServiceResult Insert(VerificationEntry s)
        {
            var CurrentUser = this.GetUser(SessionKey);

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            s.VerifierAccountUUID = CurrentUser.AccountUUID;
            s.VerifierUUID = CurrentUser.UUID;
            s.VerificationDate = DateTime.UtcNow;
            s.VerifierProfileUUID = this.GetProfileUUID(this.SessionKey);
            GreenWerx.Models.Membership.Profile verifierProfile = null;
            //todo check if set, if not use profile -> locationUUID
            if (string.IsNullOrWhiteSpace(s.VerifierLocationUUID))
            {
                ProfileManager profileManager = new ProfileManager(this._connectionKey, this.SessionKey);
                var res = profileManager.Get(s.VerifierProfileUUID);
                try
                {
                    if (res.Code == 200)
                    {
                        verifierProfile = (GreenWerx.Models.Membership.Profile)res.Result;
                        s.VerifierLocationUUID = verifierProfile.LocationUUID;
                    }
                }
                catch
                {//not that important.
                }
            }

            var vcts = GetVerificationEntries(s.RecipientProfileUUID);
            //one user can do multiple adds. so make it that they can only verify every 90 days.
            var tmp = GetVerificationEntries(s.RecipientProfileUUID)
                .FirstOrDefault(w => w.VerifierUUID == CurrentUser.UUID &&
                     w.VerificationType.EqualsIgnoreCase(s.VerificationType)
                      && w.VerificationDate.AddDays(-90) < DateTime.UtcNow
                    );//
            if (tmp != null)
                return ServiceResponse.Error("You may only verify every ninety days.");

            RoleManager rm = new RoleManager(this._connectionKey, CurrentUser);
            var userRole = rm.GetRolesForUser(CurrentUser.UUID, CurrentUser.AccountUUID)
                                .Where(w => w.Category.EqualsIgnoreCase("member"))
                                .OrderByDescending(o => o.RoleWeight).FirstOrDefault();

            if (userRole == null)
                return ServiceResponse.Error("You must be assigned a role to verify.");

            s.VerifierRoleUUID = userRole.UUID;

            //verificationType
            s.Weight = userRole.Weight; //<== role.Category of verifying user
            var relationshipRole = rm.GetRoles(CurrentUser.AccountUUID)
                                    .FirstOrDefault(w => w.CategoryRoleName.EqualsIgnoreCase(verifierProfile.RelationshipStatus) &&
                                                    w.Category.EqualsIgnoreCase("member"));
            s.Multiplier = relationshipRole.Weight;// <== of verifying user verifierProfile.RelationshipStatus
            var verTypeRole = rm.GetRoles(CurrentUser.AccountUUID).FirstOrDefault(w => w.Category.EqualsIgnoreCase("verified")
                                                                && w.CategoryRoleName.EqualsIgnoreCase(s.VerificationType));
            //Category CategoryRoleName
            //verified critical user
            //verified    ambassador
            //verified    geolocation
            //verified    photo submission
            //verified other member
            s.VerificationTypeMultiplier = verTypeRole.Weight;
            s.Points = ((s.VerificationTypeMultiplier) + s.Weight) * s.Multiplier;

            string destinationRoleUUID = verTypeRole.UUID;// "verification role"; //TODO get the uuid for the verification role
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                //todo add RecipientMemberRoleUUID //this is the member role (UsersInRoles) the user ended kup in due to the verification.
                //  create default roles fore verification

                //todo make this a transacion and add to verification role
                var ur = context.GetAll<UserRole>().Any(rpw => rpw.ReferenceUUID == s.RecipientUUID &&
                                                                rpw.AccountUUID == s.RecipientAccountUUID &&
                                                                rpw.RoleUUID == destinationRoleUUID);

                if (ur == false)// if not in role already
                { // add to role..
                    //var role = context.GetAll<Role>()
                    //                    .FirstOrDefault(w => w.AccountUUID == s.RecipientAccountUUID &&
                    //                    w.Category.EqualsIgnoreCase("verified") &&
                    //                    w.CategoryRoleName.EqualsIgnoreCase(s.VerificationType)
                    //                    );

                    var guid = Guid.NewGuid().ToString("N");
                    var userVerifiedRole = new UserRole()
                    {
                        GUUID = guid,
                        UUID = guid,
                        AccountUUID = s.RecipientAccountUUID,
                        Action = "get",
                        Active = true,
                        AppType = "web",
                        CreatedBy = s.VerifierUUID,
                        DateCreated = DateTime.UtcNow,
                        Deleted = false,
                        EndDate = DateTime.UtcNow.AddDays(90),
                        Name = verTypeRole.Name,
                        ReferenceUUID = s.RecipientUUID,
                        ReferenceType = "User",
                        UUIDType = "UserRole",
                        RoleOperation = verTypeRole.RoleOperation,
                        RoleWeight = verTypeRole.RoleWeight,
                        RoleUUID = verTypeRole.UUID,
                        Image = verTypeRole.Image,
                        StartDate = DateTime.UtcNow,
                    };
                    context.Insert<UserRole>(userVerifiedRole);

                    guid = Guid.NewGuid().ToString("N");
                    var profileRole = new UserRole()
                    {
                        GUUID = guid,
                        UUID = guid,
                        AccountUUID = s.RecipientAccountUUID,
                        Action = "get",
                        Active = true,
                        AppType = "web",
                        CreatedBy = s.VerifierUUID,
                        DateCreated = DateTime.UtcNow,
                        Deleted = false,
                        EndDate = DateTime.UtcNow.AddDays(90),
                        Name = verTypeRole.Name,
                        ReferenceUUID = s.RecipientProfileUUID,
                        ReferenceType = "Profile",
                        UUIDType = "UserRole",
                        RoleOperation = verTypeRole.RoleOperation,
                        RoleWeight = verTypeRole.RoleWeight,
                        RoleUUID = verTypeRole.UUID,
                        Image = verTypeRole.Image,
                        StartDate = DateTime.UtcNow,
                    };
                    context.Insert<UserRole>(profileRole);
                }

                if (context.Insert<VerificationEntry>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting verification.");
        }

        public List<VerificationEntry> Search(string recipientProfileUUID)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            if (string.IsNullOrWhiteSpace(recipientProfileUUID))
                return new List<VerificationEntry>();

            // todo cache
            //    Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache) ,
            //User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
            //LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var a = context.GetAll<VerificationEntry>()?.Where(sw => sw.RecipientProfileUUID == recipientProfileUUID).ToList();

                return a;
            }
        }

        public ServiceResult Update(VerificationEntry n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid verification data.");

            //   if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<VerificationEntry>((VerificationEntry)n) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, VerificationEntry was not updated.");
        }
    }
}