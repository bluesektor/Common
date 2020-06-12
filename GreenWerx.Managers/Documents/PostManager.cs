using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.General;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Document;
using GreenWerx.Models.Flags;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.Managers.Documents
{
    public class PostManager : BaseManager
    {
        public PostManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "PostManager CONTEXT IS NULL!");

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
                var post = (Post)n;
                if (purge )
                {
                    if(context.Delete<Post>(post) == 0)
                        return ServiceResponse.Error(n.Name + " failed to delete. ");

                    foreach (var att in post.Attributes)
                        context.Delete<TMG.Attribute>(att);
                }

                //get the post from the table with all the data so when its updated it still contains the same data.
                res = this.Get(n.UUID);
                if (res.Code != 200)
                    return res;

                n.Deleted = true;
                if (context.Update<Post>((Post)n) == 0)
                    return ServiceResponse.Error(n.Name + " failed to delete. ");

                foreach (var att in post.Attributes){
                    att.Deleted = true;
                    context.Update<TMG.Attribute>(att);
                }
            }
            return res;
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                 var res = context.GetAll<Post>()?.FirstOrDefault(sw => sw.UUID == uuid);
              

                if (res == null)
                    return ServiceResponse.Error("Post not found for uuid.");

                res.Attributes = context.GetAll<TMG.Attribute>()?.Where(w => w.ReferenceUUID == uuid && w.Deleted == false).ToList();
                return ServiceResponse.OK("", res);
            }
        }

        public INode GetByGetByGUUID(string GUUID)
        {
            if (string.IsNullOrWhiteSpace(GUUID))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var post = context.GetAll<Post>()?.FirstOrDefault(sw => sw.GUUID == GUUID);

                if (post == null)
                    return post;

                post.Attributes = context.GetAll<TMG.Attribute>()?.Where(w => w.ReferenceUUID == post.UUID && w.Deleted == false).ToList();
                return post;
            }
        }

      
        public List<Post> GetPosts(string accountUUID, ref DataFilter filter)
        {
            var deleted = filter.IncludeDeleted;
            var includePrivate = filter.IncludePrivate;

            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            List < Post > posts = new List<Post>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (this._requestingUser != null && this._requestingUser.SiteAdmin)
                {
                    posts = context.GetAll<Post>(ref filter)?.
                                    Where(sw => (
                                                  (sw.Private == false || sw.Private == includePrivate) &&
                                                  sw.PublishDate <= DateTime.UtcNow) &&
                                          sw.Deleted == deleted).
                                    OrderByDescending(ob => ob.PublishDate).ToList();
                }
                else
                {
                     posts = context.GetAll<Post>(ref filter)?.
                                       Where(sw => (sw.Status.EqualsIgnoreCase("publish") &&
                                                     (sw.Private == false || sw.Private == includePrivate) &&
                                                     sw.PublishDate <= DateTime.UtcNow) &&
                                             sw.Deleted == deleted).
                                       OrderByDescending(ob => ob.PublishDate).ToList();
                }
                if (posts == null)
                    return new List<Post>();

                int count = 0;
                posts.ForEach(x =>
               {
                   x.Attributes = context.GetAll<TMG.Attribute>()
                                            .Where(w => w.ReferenceUUID == x.UUID && w.Deleted == false)
                                            .OrderBy( o => o.SortOrder)
                                            .ToList();

                   if (x.Attributes.Count > 0)
                       count = x.Attributes.Count;
               });

                return posts;

            }
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            if (!IsPostAuthorized(n))
                return ServiceResponse.Error("Post failed to save. You may only post twice in 24 hour period.");

            var s = (Post)n;

            if (s.Status.EqualsIgnoreCase(PostFlags.Status.Publish))
            {
                if (s.PublishDate == DateTime.MinValue)
                    s.PublishDate = DateTime.UtcNow;

                if (_requestingUser.RoleWeight < RoleFlags.MemberRoleWeights.Manager)
                    s.Status += PostFlags.Status.Moderate;
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Insert<Post>((Post)s))
                {
                    AttributeManager am = new AttributeManager(this._connectionKey, this.SessionKey);
                    foreach (var att in s.Attributes)
                    {
                        att.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);
                        if (string.IsNullOrWhiteSpace(att.ReferenceUUID))
                        {
                            att.ReferenceUUID = s.UUID;
                            att.ReferenceType = s.UUIDType;
                        }
                        
                        am.Insert(att);
                    }

                    return ServiceResponse.OK("", s);
                }
            }
            return ServiceResponse.Error("An error occurred inserting post " + s.Name);
        }

        /// <summary>
        /// This is specifically for posts, to keep spammers
        /// from clogging up the board.
        /// Not to be confused with DataAccessAuthorized(...)
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool IsPostAuthorized(INode n)
        {
            if (_requestingUser.SiteAdmin)
                return true;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var assignedRoles = context.GetAll<UserRole>()
                                         ?.Where(urw => urw.ReferenceUUID == _requestingUser.UUID &&
                                                 urw.AccountUUID == _requestingUser.AccountUUID && urw.Deleted == false);

                if (assignedRoles.Any(a => a.RoleWeight >= RoleFlags.MemberRoleWeights.Manager))
                    return true;

                if (assignedRoles.Any(a => a.RoleWeight == RoleFlags.MemberRoleWeights.Guest))
                    return false;

                if (context.GetAll<Post>().Count(w => w.CreatedBy == _requestingUser.UUID && w.PublishDate >= DateTime.UtcNow.AddHours(-24)) > 2)
                    return false;

                return true;
            }
        }

        public List<Post> Search(string name)
        {
            ///if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            if (string.IsNullOrWhiteSpace(name))
                return new List<Post>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var posts = context.GetAll<Post>()?.Where(sw => sw.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)).ToList();

                if (posts == null)
                    return new List<Post>();

                posts.ForEach(x =>
                {
                    x.Attributes = context.GetAll<TMG.Attribute>().Where(w => w.ReferenceUUID == x.UUID && w.Deleted == false).ToList();
                });

                return posts;
            }
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Post data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            Post s = (Post)n;

            if (s.Status.EqualsIgnoreCase(PostFlags.Status.Publish))
            {
                if (s.PublishDate == DateTime.MinValue)
                    s.PublishDate = DateTime.UtcNow;

                if (_requestingUser.RoleWeight < RoleFlags.MemberRoleWeights.Manager && _requestingUser.SiteAdmin == false)
                    s.Status += PostFlags.Status.Moderate;
            }

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Post>(s) > 0)
                {
                    AttributeManager am = new AttributeManager(this._connectionKey, this.SessionKey);
                    foreach (var att in s.Attributes)// todo make all these transactions
                        am.Update(att);

                    return ServiceResponse.OK();
                }
            }
            return ServiceResponse.Error("System error, Post was not updated.");
        }

        private List<Post> FindPost(string uuid, string name, string AccountUUID, ref DataFilter filter)
        {
            List<Post> res = null;

            if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(name) == false)
                res = this.Search(name);
            else
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    res = context.GetAll<Post>()?.Where(w => w.UUID == uuid || (w.Name?.EqualsIgnoreCase(name) ?? false)).ToList();
                }
            }

            if (res == null)
                return res;

            if (filter.IncludeSystemAccount && (res.FirstOrDefault().AccountUUID == AccountUUID))
                return res;

            if (res.FirstOrDefault().AccountUUID == AccountUUID)
                return res;

            return new List<Post>();
        }
    }
}