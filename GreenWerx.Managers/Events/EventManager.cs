// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Managers.DataSets;
using GreenWerx.Managers.Membership;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Events
{
    public class EventManager : BaseManager, ICrud
    {
        public EventManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "EventManager CONTEXT IS NULL!");
            this._connectionKey = connectionKey;
        }

        public ServiceResult Delete(INode n, bool purge = false)
        {
            ServiceResult res = ServiceResponse.OK();

            if (n == null)
                return ServiceResponse.Error("No record sent.");

            if (!this.DataAccessAuthorized(n, "DELETE", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Event)n;

            //if (purge)
            //{
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                if (context.Delete<Event>(s) == 0)
                    return ServiceResponse.Error(s.Name + " failed to delete. ");
                else
                    return ServiceResponse.OK();
                }
            //}

            ////get the Event from the table with all the data so when its updated it still contains the same data.
            //res = this.Get(s.UUID);
            //if (res.Code != 200)
            //    return res;
            //s = (Event)res.Result;

            //s.Deleted = true;
            //using (var context = new GreenWerxDbContext(this._connectionKey))
            //{
            //    if (context.Update<Event>(s) == 0)
            //        return ServiceResponse.Error(s.Name + " failed to delete. ");
            //}
            //return res;
        }

        public void DeleteEventMember(EventMember member)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    context.Delete<EventMember>(member);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
        }

        public ServiceResult Get(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                var res = context.GetAll<Event>()?.FirstOrDefault(sw => sw?.UUID == uuid);
                if (res == null)
                    return ServiceResponse.Error("Event not found for uuid.");
                return ServiceResponse.OK("", res);
            }
            ////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public List<EventLocation> GetAccountEventLocations(string accountUUID, bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<EventLocation>()?.Where(sw => sw.AccountUUID == accountUUID && sw?.Deleted == deleted)?
                        .GroupBy(x => x.Name.ToUpper()).Select(group => group.First())?
                        .ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<EventLocation>();
        }

        //public List<Event> GetAllEventsByDistance()
        //{
        //    try
        //    {
        //        using (var context = new GreenWerxDbContext(this._connectionKey))
        //        {
        //            DynamicParameters parameters = new DynamicParameters();// see function GetTableData

        //            parameters.Add("@CLIENTLAT", 33.3776);
        //            parameters.Add("@CLIENTLON", -112.3874);
        //            parameters.Add("@MEASURE", 3956.55); // 3956.55 = miles
        //                                                 //
        //            string sql = @"SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , e.Latitude, e.Longitude, @MEASURE ))  as Distance
        //                                    ,[Name]	,[Category]	,[EventDateTime]	,[RepeatCount]
        //                              ,[RepeatForever]	,[Frequency]		,[StartDate]	,[EndDate]
        //                              ,[Url]				,[HostAccountUUID]	,[GUUID]		,[GuuidType]
        //                              ,[UUID]				,[UUIDType]			,[UUParentID]   ,[UUParentIDType]
        //                              ,[Status]			,[AccountUUID]      ,[Active]		,[Deleted]
        //                              ,[Private]			,[SortOrder]		,[CreatedBy]    ,[DateCreated]
        //                              ,[Image]			,[RoleWeight]		,[RoleOperation],[NSFW]
        //                              ,[Latitude]			,[Longitude] 		,[Description], [IsAffiliate]
        //                            FROM Events e";

        //            sql += " ORDER BY Distance ASC";

        //            var events = context.Select<Event>(sql, parameters);
        //            return events.ToList();
        //        }

        //        //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Assert(false, ex.Message);
        //    }
        //    return new List<Event>();
        //}

        public List<string> GetEventCategories(bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<Event>()?.Where(sw =>
                            sw.Private == false &&
                            sw?.Deleted == deleted)
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

        public List<EventGroup> GetEventGroups(bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<EventGroup>()?.Where(sw => sw?.Deleted == deleted).OrderBy(ob => ob?.StartDate)?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<EventGroup>();
        }

        public List<EventGroup> GetEventGroups(string eventUUID, bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<EventGroup>()?.Where(sw => sw.EventUUID == eventUUID && sw?.Deleted == deleted).OrderBy(ob => ob?.StartDate)?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<EventGroup>();
        }

        public List<EventItem> GetEventInventory(string eventUUID, bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<EventItem>()?.Where(sw => sw.EventUUID == eventUUID && sw?.Deleted == deleted).OrderBy(ob => ob?.Name)?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<EventItem>();
        }

        public ServiceResult GetEventLocation(string eventLocationUUID)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    var location = context.GetAll<EventLocation>()?.FirstOrDefault(sw => sw.UUID == eventLocationUUID);
                    if (location == null)
                        return ServiceResponse.Error("Location was not found.");

                    return ServiceResponse.OK("", location);
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Error loading location");
            }
        }

        /// <summary>
        /// We pass in the eventUUID because the client allows to pick from previous event locations
        /// If we try to save a new event based on a previouse location the eventlocation.evenuuid will
        /// be changed in the table by the update and the other event will lose its location. So we need
        /// a unique eventUUID and UUID combo
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="eventUUID"></param>
        /// <returns></returns>
        public ServiceResult GetEventLocation(string uuid, string eventUUID)
        {
            //Func<EventLocation, Location, EventLocation> UpdateLocation
            //                                                = ((a, b) => {
            //                                                    a.Latitude = b.Latitude;
            //                                                    a.Longitude = b.Longitude;
            //                                                    return a; });
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    var location = context.GetAll<EventLocation>()?.FirstOrDefault(sw => sw.UUID == uuid && sw.EventUUID == eventUUID);
                    //.Join(context.GetAll<Location>(),
                    // evtLocation => evtLocation.LocationUUID,
                    // tmpLoc => tmpLoc.UUID,
                    // (evtLocation, tmpLoc) => new { evtLocation, tmpLoc })
                    // .Select(s => UpdateLocation(s.evtLocation, s.tmpLoc))?.FirstOrDefault();

                    if (location == null)
                        return ServiceResponse.Error("Location was not found.");

                    return ServiceResponse.OK("", location);
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Error loading location");
            }
        }

        public ServiceResult GetEventLocationByEventUUID(string eventUUID)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    var location = context.GetAll<EventLocation>()?.FirstOrDefault(sw => sw.EventUUID == eventUUID);
                    if (location == null)
                        return ServiceResponse.Error("Location was not found.");

                    return ServiceResponse.OK("", location);
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Error loading location");
            }
        }

        public ServiceResult SearchEventLocation(string name, bool includePartialMatch )
        {
            try
            {
                name = name.ToLower();
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    List<EventLocation> locations = new List<EventLocation>();
                    if (includePartialMatch)
                    {
                        locations = context.GetAll<EventLocation>()
                            .Where( sw => sw.Name.ToLower() == name || 
                                    sw.Name.ToLower().Contains(name)).ToList();
                    }
                    else
                    {
                         locations = context.GetAll<EventLocation>().Where(sw => sw.Name.ToLower() == name.ToLower()).ToList();
                    }
                    if (locations == null)
                        return ServiceResponse.Error("Location was not found.");

                    return ServiceResponse.OK("", locations);
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Error loading location");
            }
        }


        public List<EventLocation> GetEventLocations(string eventUUID, bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<EventLocation>()?.Where(sw => sw.EventUUID == eventUUID && sw?.Deleted == deleted)?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<EventLocation>();
        }

        public List<EventMember> GetEventMember(string eventUUID, string accountUUID)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<EventMember>()?.Where(w => w.EventUUID == eventUUID && w.AccountUUID == accountUUID).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return null;
        }

        public List<User> GetEventMembers(string eventUUID, string accountUUID)
        {
            List<User> members = new List<User>();
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    members = context.GetAll<EventMember>()?.Where(w => w.EventUUID == eventUUID)
                                       .Join(
                                           context.GetAll<User>()
                                               .Where(w => w.Deleted == false &&
                                                      w.AccountUUID == accountUUID),
                                           acct => acct.UserUUID,
                                           users => users.UUID,
                                           (acct, users) => new { acct, users }
                                        )
                                        .Select(s => s.users)
                                        .ToList();
                }
                if (members == null)
                    return new List<User>();

                members = UserManager.ClearSensitiveData(members);
                ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return members;
        }

        public List<User> GetEventNonMembers(string eventUUID, string accountUUID)
        {
            List<User> nonMembers;
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    nonMembers = context.GetAll<User>()?.Where(w => w.Deleted == false && w.AccountUUID == accountUUID)?.ToList();
                    if (nonMembers == null || nonMembers.Count == 0)
                        return new List<User>();

                    nonMembers.AddRange(context.GetAll<EventMember>()?.Where(w => w.EventUUID != eventUUID)
                                         .Join(
                                             context.GetAll<User>()
                                                   .Where(w => w.Deleted == false &&
                                                          w.AccountUUID == accountUUID),
                                             acct => acct.UserUUID,
                                             users => users.UUID,
                                             (acct, users) => new { acct, users }
                                          )
                                          .Select(s => s.users)
                                          .ToList());
                }

                if (nonMembers == null)
                    return new List<User>();

                List<User> members = GetEventMembers(eventUUID, accountUUID);

                nonMembers = nonMembers.Except(members).ToList();

                nonMembers = UserManager.ClearSensitiveData(nonMembers);
                ///if (!this.DataAccessAuthorized(u, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
                return nonMembers; // , clearSensitiveData TODO clear this
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<User>();
        }

        public List<Event> GetEvents(bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    return context.GetAll<Event>()?.Where(sw => sw?.Deleted == deleted).OrderBy(ob => ob?.StartDate)?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<Event>();
        }

        public List<dynamic> GetFavoriteEvents(string userUUID, string accountUUID)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (context.GetAll<Favorite>()?.FirstOrDefault() == null)
                        return new List<dynamic>();

                    var events = context.GetAll<Favorite>()?.Where(w => w?.CreatedBy == userUUID && w?.AccountUUID == accountUUID
                        && w.UUIDType.EqualsIgnoreCase("event"))
                        ?.Join(context.GetAll<Event>(),
                         rem => rem.ItemUUID,
                         evt => evt.UUID,
                        (rem, evt) => new { rem, evt })
                        ?.Select(s => new Favorite()
                        {
                            UUID = s.rem.UUID,
                            UUIDType = s.rem.UUIDType,
                            CreatedBy = s.rem.CreatedBy,
                            AccountUUID = s.rem.AccountUUID
                        });

                    return events.Cast<dynamic>().ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<dynamic>();
        }

        public List<Event> GetHostEvents(string accountUUID, bool deleted = false, bool includePrivate = false)
        {
            try
            {
                if (includePrivate)
                { //if looking at private then check permission
                    if (!this.DataAccessAuthorized("EVENT", false, includePrivate))
                    {
                        includePrivate = false;
                        // if not allowed private then check if they can see anything..
                        if (!this.DataAccessAuthorized("EVENT", false, includePrivate))
                            return new List<Event>();
                    }
                }
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    var x = context.GetAll<Event>()?.Where(sw =>
                        sw.HostAccountUUID == accountUUID &&
                        (sw.Private == false || sw.Private == includePrivate) &&
                        sw?.Deleted == deleted).OrderBy(ob => ob?.StartDate)?.ToList();

                    if (x != null)
                    {
                        x.ForEach(y =>
                        {
                            if (!string.IsNullOrWhiteSpace(y.EventLocationUUID))
                            {
                                var lres = this.GetEventLocation(y.EventLocationUUID);
                                if (lres != null && lres.Code == 200)
                                {
                                    var location = (EventLocation)lres.Result;
                                    y.Latitude = location.Latitude;
                                    y.Longitude = location.Longitude;
                                }
                            }
                            y.HostName = context.GetAll<Account>().FirstOrDefault(w => w.UUID == y.HostAccountUUID)?.Name;
                        });
                    }
                    return x;
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<Event>();
        }

        public List<Event> GetPrivateSubEvents(string parentUUID, string userUUID, string accountUUID, bool includeParent = false, bool deleted = false)
        {
            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    if (includeParent)
                    {
                        return context.GetAll<Event>()?.Where(sw =>
                            (sw.UUID == parentUUID || sw.UUParentID == parentUUID) &&
                            (sw.Private == true && sw.CreatedBy == userUUID && sw.AccountUUID == accountUUID) &&
                            sw?.Deleted == deleted).OrderBy(ob => ob?.StartDate)?.ToList();
                    }

                    return context.GetAll<Event>()?.Where(sw =>
                        sw.UUParentID == parentUUID &&
                        (sw.Private == true && sw.CreatedBy == userUUID && sw.AccountUUID == accountUUID) &&
                        sw?.Deleted == deleted).OrderBy(ob => ob?.StartDate)?.ToList();
                }
                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<Event>();
        }

        /// <summary>
        /// optimization. if iterating over ienumerable multiple times call
        /// tolist first. Move the filter to within context to cut down array size
        /// </summary>
        /// <param name="parentUUID"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetSubEvents(string parentUUID, DateTime endDate, ref DataFilter filter)
        {
            
            try
            {
                if (filter.IncludePrivate)
                { //if looking at private then check permission
                    if (!this.DataAccessAuthorized("EVENT", false, filter.IncludePrivate))
                    {
                        filter.IncludePrivate = false;

                        // if not allowed private then check if they can see anything..
                        if (!this.DataAccessAuthorized("EVENT", false, filter.IncludePrivate))
                            return new List<dynamic>();
                    }
                }
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    DynamicParameters parameters = new DynamicParameters();// see function GetTableData
                    parameters.Add("@PARENTUUID", parentUUID);
                    parameters.Add("@PRIVATE", filter.IncludePrivate);
                    parameters.Add("@DELETED", filter.IncludeDeleted);
                    parameters.Add("@ENDDATE", endDate);

                    string sql = @"SELECT COUNT(*) FROM Events e
                                   WHERE
	                                (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
	                                (e.Deleted = 0 OR e.Deleted = @DELETED) AND
	                                (e.EndDate > @ENDDATE)";
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
                        return new List<dynamic>();

                    parameters.Add("@CLIENTLAT", filter.Latitude);
                    parameters.Add("@CLIENTLON", filter.Longitude);
                    parameters.Add("@MEASURE", 3956.55); // 3956.55 = miles
                    //
                    sql = @"SELECT CEILING(dbo.CalcDistance(@CLIENTLAT, @CLIENTLON , el.Latitude, el.Longitude, @MEASURE ) ) as Distance
                                            ,a.Name AS HostName
                                            ,el.Name AS Location
											,el.City
											,el.State
											,el.Country
                                            ,e.[Name]	            ,e.[Category]	        ,e.[EventDateTime],e.[RepeatCount]
		                                    ,e.[RepeatForever]	,e.[Frequency]		,e.[StartDate]	,e.[EndDate]
		                                    ,e.[Url]				,e.[HostAccountUUID]	,e.[GUUID]		,e.[GuuidType]
		                                    ,e.[UUID]				,e.[UUIDType]			,e.[UUParentID]   ,e.[UUParentIDType]
		                                    ,e.[Status]			,e.[AccountUUID]      ,e.[Active]		,e.[Deleted]
		                                    ,e.[Private]			,e.[SortOrder]		,e.[CreatedBy]    ,e.[DateCreated]
		                                    ,e.[Image]			,e.[RoleWeight]		,e.[RoleOperation],e.[NSFW]
		                                    ,e.[Latitude]			,e.[Longitude] 		,e.[Description], e.[IsAffiliate]
                                            ,el.Latitude, el.Longitude
                                    FROM Events e
                                    LEFT JOIN Accounts a ON e.HostAccountUUID = a.UUID
                                    LEFT JOIN (SELECT DISTINCT EventUUID,Latitude, Longitude, Name, City, State, Country FROM EventLocations) el   ON e.UUID = el.EventUUID
                                   WHERE
	                                (e.UUID = @PARENTUUID OR e.UUParentID =  @PARENTUUID ) AND
	                                (e.Private = 0 OR e.Private = @PRIVATE) AND
	                                (e.Deleted = 0 OR e.Deleted = @DELETED) AND
	                                (e.EndDate > @ENDDATE)";

                   // where = DatasetManager.BuildWhereClause(filter.Screens);
                    if (!string.IsNullOrWhiteSpace(where))
                    {
                        sql += " AND " + where;
                    }

                    if (string.IsNullOrWhiteSpace(filter.SortBy))
                    {   //for events we want to default sort by start date
                        sql += " ORDER BY StartDate ASC";
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

                        parameters.Add("@PAGESIZE" , filter.PageSize);
                        parameters.Add("@PAGEINDEX", filter.Page);
                        sql += @" OFFSET @PAGESIZE *(@PAGEINDEX - 1) ROWS FETCH NEXT @PAGESIZE ROWS ONLY";
                    }

                    var events = context.Select<dynamic>(sql, parameters);
                    if (events != null)
                    {

                        //events = events.Filter(ref filter);
                        events = events.GroupBy(x => x.UUID).Select(group => group.First()).ToList();

                     
                        return events; 
                       
                    }

                    #region old code

                    /*
                    //sw.UUID == parentUUID Includes parent item
                    var x = context.GetAll<Event>()?.Where(sw =>
                           (sw.UUID == parentUUID || sw.UUParentID == parentUUID) &&
                           (sw.Private == false || sw.Private == filter.IncludePrivate) &&
                           (sw?.Deleted == false || sw?.Deleted == filter.IncludeDeleted)
                           );

                    #region join filters out events that don't have eventlocation

                    //?.Join(context.GetAll<EventLocation>(),
                    //          evt => evt.UUID,
                    //          loc => loc.EventUUID,
                    //          (evt, loc) => new { evt, loc })
                    //          ?.Select(s =>
                    //          {
                    //              s.evt.HostName = context.GetAll<Account>().FirstOrDefault(w => w.UUID == s.evt.HostAccountUUID)?.Name;
                    //              s.evt.Latitude = s.loc.Latitude;
                    //              s.evt.Longitude = s.loc.Longitude;

                    //              if (filterParams != null && (filterParams.Latitude != 0 && filterParams.Longitude != 0))
                    //              {
                    //                  s.evt.Distance = MathHelper.Distance(filterParams.Latitude, filterParams.Longitude, s.evt.Latitude ?? 0, s.evt.Longitude ?? 0);
                    //              }
                    //              return s.evt;
                    //          });

                    #endregion join filters out events that don't have eventlocation

                    if (x == null || x.Any() == false)
                        return new List<Event>();

                    if ((filter.Latitude + filter.Longitude ) !=  0)
                    {
                        foreach (var z in x)
                        {
                            //z.HostName = context.GetAll<Account>().FirstOrDefault(w => w.UUID == z.HostAccountUUID)?.Name;
                            //var eventLocation = context.GetAll<EventLocation>().FirstOrDefault(w => w.EventUUID == z.UUID);
                            //if (eventLocation == null)
                            //    continue;
                            z.Distance = Math.Ceiling(MathHelper.Distance(filter.Latitude, filter.Longitude, z.Latitude ?? 0, z.Longitude ?? 0));
                        }
                    }

                    if (filter.IncludeParent == false)
                    {
                        x = x.Where(s => s.UUID != parentUUID);// see if this excludes parent, if not try below
                                                               //  x = x.Where(s => x.All(y => y.UUID != parentUUID));
                    }

                    if (!string.IsNullOrWhiteSpace(filter.SortBy) &&
                        filter.SortBy.EqualsIgnoreCase("distance"))
                    {
                        x = x.OrderBy(ob => ob?.Distance);
                    }
                    else
                    {
                        x = x.OrderBy(ob => ob?.StartDate);
                    }

                    return x.ToList();
                    */

                    #endregion old code
                }

                //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return new List<dynamic>();
        }

        public ServiceResult Insert(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (Event)n;

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    s.Name = s.Name.Trim();
                    if (context.Insert<Event>(s))
                        return ServiceResponse.OK("", s);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return ServiceResponse.Error("An error occurred inserting Event " + s.Name);
        }

        public ServiceResult InsertEventGroup(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (EventGroup)n;

            s.Name = s.Name.Trim();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                EventGroup dbU = context.GetAll<EventGroup>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("EventGroup already exists.");

                if (context.Insert<EventGroup>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting EventGroup " + s.Name);
        }

        public ServiceResult InsertEventLocation(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (EventLocation)n;

            s.Name = s.Name.Trim();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                // EventLocation dbU = context.GetAll<EventLocation>()?.FirstOrDefault(wu => (wu.Name?.EqualsIgnoreCase(s.Name) ?? false) && wu.AccountUUID == s.AccountUUID);

                // if (dbU != null)                    return ServiceResponse.Error("EventLocation already exists.");

                s.UUID = Guid.NewGuid().ToString("N");
                try
                {
                    if (string.IsNullOrWhiteSpace(s.TimeZone))
                        s.TimeZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(w => w.BaseUtcOffset.TotalHours < -9 && w.BaseUtcOffset.TotalHours > -12).StandardName;
                }
                catch
                {
                    s.TimeZone = "na";
                }
                if (context.Insert<EventLocation>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting EventLocation " + s.Name);
        }

        public ServiceResult InsertEventMember(INode n)
        {
            if (!this.DataAccessAuthorized(n, "post", false)) return ServiceResponse.Error("You are not authorized this action.");

            n.Initialize(this._requestingUser.UUID, this._requestingUser.AccountUUID, this._requestingUser.RoleWeight);

            var s = (EventMember)n;

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                EventMember dbU = context.GetAll<EventMember>()?.
                    FirstOrDefault(wu => wu.EventUUID == s.EventUUID && wu.UserUUID == s.UserUUID && wu.AccountUUID == s.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("EventMember already exists.");

                if (context.Insert<EventMember>(s))
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("An error occurred inserting EventMember " + s.Name);
        }

        public ServiceResult Save(EventLocation geo)
        {
            if (geo == null)
                return ServiceResponse.Error("No location information sent.");

            EventLocation dbLocation = null;
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (!string.IsNullOrWhiteSpace(geo.UUID))
                {
                    var tmp = this.GetEventLocation(geo.UUID);
                    if (tmp.Code != 200)
                        return tmp;

                    dbLocation = (EventLocation)tmp.Result;
                }
                else
                    geo.UUID = Guid.NewGuid().ToString("N");

                geo.Name = geo.Name.Trim();
                if (dbLocation == null)
                    return InsertEventLocation(geo);
            }

            // geo = ObjectHelper.Merge<EventLocation>(dbLocation, geo);
            dbLocation.Name = geo.Name.Trim();
            dbLocation.Country = geo.Country;
            dbLocation.Postal = geo.Postal;
            dbLocation.State = geo.State;
            dbLocation.City = geo.City;
            dbLocation.Longitude = geo.Longitude;
            dbLocation.Latitude = geo.Latitude;
            dbLocation.isDefault = geo.isDefault;
            dbLocation.Description = geo.Description;
            dbLocation.Category = geo.Category;
            dbLocation.Address1 = geo.Address1?.Trim();
            dbLocation.Address2 = geo.Address2?.Trim();
            dbLocation.TimeZone = geo.TimeZone;
            dbLocation.Email = geo.Email;
            return UpdateEventLocation(dbLocation);
        }

        public List<Event> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Event>();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                return context.GetAll<Event>()?.Where(sw => sw.Name.EqualsIgnoreCase(name)).ToList();
            }
            //////if (!this.DataAccessAuthorized(s, "GET", false)) return ServiceResponse.Error("You are not authorized this action.");
        }

        public ServiceResult Update(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Event data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (Event)n;

            s.Name = s.Name.Trim();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<Event>(s) > 0)
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("System error, Event was not updated.");
        }

        public ServiceResult UpdateEventLocation(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Event location data.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (EventLocation)n;

            s.Name = s.Name.Trim();
            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<EventLocation>(s) > 0)
                    return ServiceResponse.OK("", s);
            }
            return ServiceResponse.Error("System error, Event location was not updated.");
        }

        public ServiceResult UpdateGroup(INode n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Event group.");

            if (!this.DataAccessAuthorized(n, "PATCH", false)) return ServiceResponse.Error("You are not authorized this action.");

            var s = (EventGroup)n;

            s.Name = s.Name.Trim();

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                if (context.Update<EventGroup>(s) > 0)
                    return ServiceResponse.OK();
            }
            return ServiceResponse.Error("System error, Event was not updated.");
        }
    }
}