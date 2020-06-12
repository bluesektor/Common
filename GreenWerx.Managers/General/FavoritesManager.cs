using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Managers.Membership;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Document;
using GreenWerx.Models.Events;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.General
{
    public class FavoritesManager : BaseManager, ICrud
    {
        public FavoritesManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "FavoriteManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Favorite)n;

            if (purge)
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.Delete<Favorite>(s) == 0)
                        return ServiceResponse.Error(s.Name + " failed to delete. ");
                    else
                        return ServiceResponse.OK();
                }
            }

            //get the Favorite from the table with all the data so when its updated it still contains the same data.
            res = this.Get(s.UUID);
            if (res.Code != 200)
                return res;

            s = (Favorite)res.Result;
            if (s == null)
                return ServiceResponse.Error("Favorite not found");

            s.Deleted = true;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Favorite>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
            }
            return res;
        }

        /// <summary>
        /// set the title if you want it updated on a soft delete, like 'this item as been deleted...'
        /// </summary>
        /// <param name="ItemUUID"></param>
        /// <param name="title"></param>
        /// <param name="purge"></param>
        /// <returns></returns>
        public ServiceResult DeleteForEvent(string ItemUUID, string title = "", bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var reminders = context.GetAll<Favorite>()?.Where(w => w.ItemUUID == ItemUUID && w.Deleted == false).ToList();

                foreach (var reminder in reminders)
                {
                    if (purge)
                    {
                        if (context.Delete<Favorite>(reminder) == 0)
                            return ServiceResponse.Error("Failed to delete reminder.");
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(title))
                            reminder.Name = title;

                        reminder.Deleted = true;
                        if (context.Update<Favorite>(reminder) == 0)
                            return ServiceResponse.Error("Failed to delete reminder.");
                    }
                }
            }
            return ServiceResponse.OK();
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Favorite>()?.FirstOrDefault(sw => sw.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Favorite not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public INode GetByEvent(string ItemUUID)
        {
            if (string.IsNullOrWhiteSpace(ItemUUID))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Favorite>()?.FirstOrDefault(sw => sw.ItemUUID == ItemUUID);
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<Favorite> GetFavorites(string type, string userUUID, string accountUUID, bool deleted = false)
        {
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var favorites = context.GetAll<Favorite>()
                                        ?.Where(sw => (sw?.UserUUID == userUUID &&
                                                sw?.AccountUUID == accountUUID &&
                                                (sw?.ItemType.EqualsIgnoreCase(type) ?? false)) &&
                                                sw?.Deleted == deleted)?.OrderBy(ob => ob.Name)
                                                ?.ToList();

                //favorite properties
                //public string UserUUID { get; set; }
                //  public string ItemUUID { get; set; }
                //  public string ItemType { get; set; }
                ProfileManager profileManager = new ProfileManager(this._connectionKey, this.SessionKey);
                ServiceResult res = null;
                // todo forgot what i was going to load here
                favorites.ForEach(x =>
                {
                    switch (type.ToUpper())
                    {
                        case "POST":
                            x.Item = context.GetAll<Post>()?.FirstOrDefault(w => w.UUID == x.UUID);
                            break;

                        case "ITEM":
                            x.Item = context.GetAll<Models.General.Attribute>()?.FirstOrDefault(w => w.UUID == x.UUID);
                            break;

                        case "EVENT":
                            x.Item = context.GetAll<Event>()?.FirstOrDefault(w => w.UUID == x.UUID);
                            break;

                        case "PROFILEMEMBER":
                            x.Item = context.GetAll<ProfileMember>()?.FirstOrDefault(w => w.UUID == x.UUID);
                            break;

                        case "PROFILE":
                            res = profileManager.Get(x.UUID);
                            if (res.Code == 200)
                            {
                                x.Item = res.Result as Profile;
                            }
                            break;

                        case "ACCOUNT":
                            x.Item = context.GetAll<Account>()?.FirstOrDefault(w => w.UUID == x.UUID);
                            break;

                        case "USER":
                            x.Item = UserManager.ClearSensitiveData(context.GetAll<User>()?.FirstOrDefault(w => w.UUID == x.UUID));
                            break;
                    }
                });
                return favorites;
            }
            //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Favorite)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.GetAll<Favorite>()?.FirstOrDefault() == null)
                {
                    if (context.Insert<Favorite>(s))
                        return ServiceResponse.OK("", s);
                    else
                        return ServiceResponse.Error("Failed to save reminder.");
                }
                Favorite dbU = context.GetAll<Favorite>()?.FirstOrDefault(wu =>
                (wu.ItemUUID == s.ItemUUID) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Favorite already exists.");

                if (context.Insert<Favorite>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting Favorite " + s.Name);
        }

        public List<Favorite> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Favorite>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Favorite>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
            //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Favorite data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Favorite)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Favorite>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Favorite was not updated.");
        }
    }
}