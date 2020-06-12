// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.

using Dapper;
//using MySql.Data.Entity;
//using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GreenWerx.Data.Helpers;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Document;
using GreenWerx.Models.Equipment;
using GreenWerx.Models.Events;
using GreenWerx.Models.Finance;
using GreenWerx.Models.Finance.PaymentGateways;
using GreenWerx.Models.General;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Inventory;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Medical;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Plant;
using GreenWerx.Models.Store;
using GreenWerx.Models.Tools;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;

namespace GreenWerx.Data
{
    // [DbConfigurationType(typeof(MultipleDbConfiguration))]
    [DbConfigurationType(typeof(MultipleDbConfiguration))]
    public partial class GreenWerxDbContext : DbContext, IDbContext
    {
        #region Properties
        public static Dictionary<string, object> TypeTables = new Dictionary<string, object>();
        private readonly SystemLogger _fileLogger = new SystemLogger(null, true);

        public GreenWerxDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Database.SetInitializer<GreenWerxDbContext>(null); // dev try context has changed since the database was created

            Initialize(nameOrConnectionString);
        }

        //Installer constructor
        public GreenWerxDbContext(bool install = false)
        {
            this.Install = install;
        }

        public string _providerType { get; set; }
        public string ConnectionKey { get; set; }
        public bool Install { get; set; }
        public string Message { get; set; }
        private ConnectionStringSettings ConnectionSettings { get; set; }

        /// <summary>
        /// Currently used for the file path of the sqlite database.
        /// </summary>
        private string PathToDatabase { get; set; }

        #endregion Properties
        #region Initialization

        public void Add<T>(T entity) where T : class
        {
            try
            {
                base.Set<T>().Add(entity);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Add:" + typeof(T).ToString());
            }
        }

        public void AddRange<T>(List<T> entities) where T : class
        {
            try
            {
                base.Set<T>().AddRange(entities);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Add:" + typeof(T).ToString());
            }
        }

        public ServiceResult CreateDatabase(AppInfo appSettings, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(appSettings.ActiveDbConnectionKey))
                return ServiceResponse.Error("Failed to create databse, ActiveDbConnectionKey is not set.");
            ServiceResult res = null;
            switch (appSettings.ActiveDbProvider.ToUpper())
            {
                case "SQLITE":
                    //This actually works, but the schema is so out of sync I removed it for now.
                    //// res = CopyDefaultSQLiteDatabase(appSettings);
                    res = ServiceResponse.Error(appSettings.ActiveDbProvider + " NOT IMPLEMENTED");

                    break;

                case "MSSQL":
                    #region sql database creation
                    try
                    {
                        this.Install = true;
                        this.Initialize(appSettings.ActiveDbConnectionKey);
                        ConnectionSettings = new ConnectionStringSettings(appSettings.ActiveDbConnectionKey, connectionString);
                        this.Database.Connection.ConnectionString = connectionString;
                        Database.SetInitializer<GreenWerxDbContext>(new CreateDatabaseIfNotExists<GreenWerxDbContext>());

                        Database.Initialize(true);
                        //// this.Insert<LogEntry>(new LogEntry() { LogDate = DateTime.UtcNow, Level = SystemFlag.Level.Info, Source = "GreenWerxDbContext.Initialize", Type = "LogEntry" });
                        SystemLogger sl = new SystemLogger(this.ConnectionKey, false);
                        sl.Insert(new LogEntry() { LogDate = DateTime.UtcNow, Level = SystemFlag.Level.Info, Source = "GreenWerxDbContext.Initialize", Type = "LogEntry" });
                    }
                    catch (Exception ex)
                    {
                        _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "InstallDatabase:");
                        return new ServiceResult() { Code = 500, Status = "ERROR", Message = ex.DeserializeException(true) };
                    }
                    #endregion sql database creation
                    res = ServiceResponse.OK();
                    break;

                case "MYSSQL":
                    res = ServiceResponse.Error(appSettings.ActiveDbProvider + " NOT IMPLEMENTED");
                    break;

                default:
                    res = ServiceResponse.Error("UNSUPORTED PROVIDER:" + appSettings.ActiveDbProvider);
                    break;
            }

            return res;
        }

        public int Delete<T>(T entity) where T : class
        {
            int res = 0;
            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();
                res = Database.Connection.Delete<T>(entity);
                SaveChanges();
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Delete:" + typeof(T).ToString());
            }

            return res;
        }

        /// <summary>
        ///object[] paramters = new object[] { userUUID, roleUUID, domain.ToUpper() };
        /// da.Delete("WHERE UserUUID=? RoleUUID=? AND ApplicationName=?", paramters);/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereStatement"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public int Delete<T>(string whereStatement, object parameters) where T : class
        {
            if (Validator.HasSqlCommand(whereStatement))
            {
                this._fileLogger.InsertSecurity("illegal statement:" + whereStatement, "GreenWerxDbContext", "Delete");
                Debug.Assert(false, "Illegal statement.");
                return 0;
            }

            int res = -1;
            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();
                string sql = "DELETE FROM " + DatabaseEx.GetTableName<T>() + " " + whereStatement;
                res = Database.Connection.Execute(sql, parameters);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Delete:" + typeof(T).ToString() + " " + "DELETE FROM " + DatabaseEx.GetTableName<T>() + " " + whereStatement);
                Debug.Assert(false, ex.DeserializeException(true));
            }

            return res;
        }

        public async Task<int> DeleteAsync<T>(T entity) where T : class
        {
            int res = 0;
            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();

                res = await Database.Connection.DeleteAsync<T>(entity);
                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "DeleteAsync:" + typeof(T).ToString());
            }

            return res;
        }

        public IDataReader Execute(string sql, object parameters)
        {
            //if (Validator.HasSqlCommand(sql))
            //{
            //    this._fileLogger.InsertSecurity("illegal statement:" + sql, "GreenWerxDbContext", "Execute");
            //    Debug.Assert(false, "Illegal statement.");
            //    return null;
            //}
            IDataReader res = null;

            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();

                res = Database.Connection.ExecuteReader(sql, parameters);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Select:" + sql);
            }
            return res;
        }

        public int ExecuteNonQuery(string sql, object parameters)
        {
            if (Validator.HasSqlCommand(sql))
            {
                this._fileLogger.InsertSecurity("illegal statement:" + sql, "GreenWerxDbContext", "ExecuteNonQuery");

                Debug.Assert(false, "Illegal statement.");
                return 0;
            }
            int res = 0;

            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();
                res = Database.Connection.Execute(sql, parameters);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "ExecuteNonQuery:" + sql);
            }

            return res;
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, object parameters)
        {
            if (Validator.HasSqlCommand(sql))
            {
                this._fileLogger.InsertSecurity("illegal statement:" + sql, "GreenWerxDbContext", "ExecuteNonQueryAsync");

                Debug.Assert(false, "Illegal statement.");
                return 0;
            }
            int res = 0;

            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();
                res = await Database.Connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                res = -1;
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "ExecuteNonQuery:" + sql);
            }

            return res;
        }

        public object ExecuteScalar(string sql, object parameters)
        {
            if (Validator.HasSqlCommand(sql))
            {
                Debug.Assert(false, "Illegal statement.");
                return 0;
            }
            object res = null;

            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();

                res = Database.Connection.ExecuteScalar(sql, parameters);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "ExecuteScalar:" + sql);
            }
            return res;
        }

        public async Task<object> ExecuteScalarAsync(string sql, object parameters)
        {
            if (Validator.HasSqlCommand(sql))
            {
                this._fileLogger.InsertSecurity("illegal statement:" + sql, "GreenWerxDbContext", "ExecuteScalarAsync");

                Debug.Assert(false, "Illegal statement.");
                return 0;
            }
            object res = null;

            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();

                res = await Database.Connection.ExecuteScalarAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "ExecuteNonQuery:" + sql);
            }
            return res;
        }

        public T Get<T>(int id) where T : class
        {
            T res = null;
            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();

                // this.Configuration.LazyLoadingEnabled = false;

                res = Database.Connection.Get<T>(id);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Get:" + typeof(T).ToString());
            }

            return res;
        }

        public IEnumerable<T> GetAll<T>() where T : class
        {
            string constring = Database.Connection.ConnectionString;
            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();
                return Database.Connection?.GetList<T>();
            }
            catch (Exception ex)
            {
                Message = ex.DeserializeException(true);
                var test = constring;
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "GetAll:" + typeof(T).ToString());
            }
            return new List<T>();
        }

        //        // filter.Latitude
        //        // filter.SortBy
        //        /// Database.Connection?.GetListPaged
        //
        //        return Database.Connection?.GetList<T>()?.Filter(filter,).Cast<T>();
        //    }
        //    catch (Exception ex)
        //    {
        //        Message = ex.DeserializeException(true);
        //        _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "GetAll:" + typeof(T).ToString());
        //    }
        //    return new List<T>();
        //}
        public IEnumerable<T> GetAll<T>(ref DataFilter filter) where T : class
        {
            string constring = Database.Connection.ConnectionString;
            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();
                return Database.Connection?.GetList<T>().Filter(ref filter).Cast<T>();
            }
            catch (Exception ex)
            {
                Message = ex.DeserializeException(true);
                var test = constring;
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "GetAll:" + typeof(T).ToString());
            }
            return new List<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>() where T : class
        {
            IEnumerable<T> res = null;
            try
            {
                using (var connection = base.Database.Connection)
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    res = await connection.GetListAsync<T>();
                }
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "GetAllAsync:" + typeof(T).ToString());
                Debug.Assert(false, ex.DeserializeException(true));
            }
            return res;
        }

        public IEnumerable<dynamic> GetAllOf(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return new List<object>();

            IEnumerable<object> res = null;
            switch (type.ToUpper())
            {
                #region General
                case "TAG": return GetAll<Tag>();
                case "POST": return GetAll<Post>();
                case "ATTRIBUTE": return GetAll<GreenWerx.Models.General.Attribute>();
                case "CATEGORY": return GetAll<Category>();
                case "UNITOFMEASURE": return GetAll<UnitOfMeasure>();
                case "STATUSMESSAGE": return GetAll<StatusMessage>();
                case "FAVORITE": return GetAll<Favorite>();

                #endregion General

                #region App

                case "APPINFO": return GetAll<AppInfo>();
                case "SETTING": return GetAll<Setting>();
                case "USERSESSION": return GetAll<UserSession>();
                #endregion App

                #region Medical
                case "ANATOMYTAG": return GetAll<AnatomyTag>();
                case "ANATOMY": return GetAll<Anatomy>();
                case "SIDEAFFECT": return GetAll<SideAffect>();
                case "SYMPTOM": return GetAll<Symptom>();
                case "SYMPTOMLOG": return GetAll<SymptomLog>();
                case "DOSELOG": return GetAll<DoseLog>();

                #endregion Medical

                case "STAGEDATA": return GetAll<StageData>();

                #region Equipment

                case "BALLAST": return GetAll<Ballast>();
                case "BULB": return GetAll<Bulb>();
                case "FAN": return GetAll<Fan>();
                case "FILTER": return GetAll<Filter>();
                case "PUMP": return GetAll<Pump>();
                case "VEHICLE": return GetAll<Vehicle>();
                case "INVENTORYITEM": return GetAll<InventoryItem>();

                #endregion Equipment

                #region Events

                case "NOTIFICATION": return GetAll<Notification>();
                case "REMINDER": return GetAll<Reminder>();
                case "REMINDERRULE": return GetAll<ReminderRule>();
                case "EVENT": return GetAll<Event>();
                case "EVENTMEMBER": return GetAll<EventMember>();
                case "EVENTITEM": return GetAll<EventItem>();
                case "EVENTGROUP": return GetAll<EventGroup>();
                case "EVENTLOCATION": return GetAll<EventLocation>();

                #endregion Events

                #region Finance

                case "CURRENCY": return GetAll<Currency>();
                case "FEE": return GetAll<Fee>();
                case "FINANCEACCOUNT": return GetAll<FinanceAccount>();
                case "FINANCEACCOUNTTRANSACTION": return GetAll<FinanceAccountTransaction>();
                case "PAYMENTGATEWAYLOG": return GetAll<PaymentGatewayLog>();

                #endregion Finance

                #region Geo

                case "LOCATION": return GetAll<Location>();

                #endregion Geo

                #region Logging

                case "ACCESSLOG": return GetAll<AccessLog>();
                case "AFFILIATELOG": return GetAll<AffiliateLog>();
                case "REQUESTLOG": return GetAll<RequestLog>();
                case "LINEITEMLOG": return GetAll<LineItemLog>();
                case "MEASUREMENTLOG": return GetAll<MeasurementLog>();
                case "EMAILMESSAGE": return GetAll<EmailMessage>();
                case "LOGENTRY": return GetAll<LogEntry>();

                #endregion Logging

                #region Membership

                case "ACCOUNT": return GetAll<Account>();
                case "ACCOUNTMEMBER": return GetAll<AccountMember>();
                case "APIKEY": return GetAll<ApiKey>();
                case "CREDENTIAL": return GetAll<Credential>();
                case "ROLEPERMISSION": return GetAll<RolePermission>();
                case "ProfileMember": return GetAll<ProfileMember>();
                case "PROFILE": return GetAll<Profile>();
                case "ROLE": return GetAll<Role>();
                case "USER": return GetAll<User>();
                case "USERROLE": return GetAll<UserRole>();
                case "PERMISSION": return GetAll<Permission>();
                case "VERIFICATIONENTRY": return GetAll<VerificationEntry>();
                case "BLOCKEDROLE": return GetAll<BlockedRole>();
                case "BLOCKEDUSER": return GetAll<BlockedUser>();

                #endregion Membership

                #region Plant

                case "PLANT": return GetAll<Plant>();
                case "STRAIN": return GetAll<Strain>();

                #endregion Plant

                #region Store

                case "PRODUCT": return GetAll<Product>();
                case "VENDOR": return GetAll<Vendor>();
                case "SHOPPINGCART": return GetAll<ShoppingCart>();
                case "SHOPPINGCARTITEM": return GetAll<ShoppingCartItem>();
                case "PRICERULE": return GetAll<PriceRule>();
                case "PRICERULELOG": return GetAll<PriceRuleLog>();
                case "ORDER": return GetAll<Order>();
                case "ORDERITEM": return GetAll<OrderItem>();
                    #endregion Store
            }

            return res;
        }

        public IEnumerable<dynamic> GetAllOf(string type, ref DataFilter filter)
        {
            if (string.IsNullOrWhiteSpace(type))
                return new List<object>();

            IEnumerable<object> res = null;
            switch (type.ToUpper())
            {
                #region General
                case "TAG": return GetAll<Tag>(ref filter);
                case "POST": return GetAll<Post>(ref filter);
                case "ATTRIBUTE": return GetAll<GreenWerx.Models.General.Attribute>(ref filter);
                case "CATEGORY": return GetAll<Category>(ref filter);
                case "UNITOFMEASURE": return GetAll<UnitOfMeasure>(ref filter);
                case "STATUSMESSAGE": return GetAll<StatusMessage>(ref filter);
                case "FAVORITE": return GetAll<Favorite>(ref filter);
                #endregion General

                #region App
                case "APPINFO": return GetAll<AppInfo>(ref filter);
                case "SETTING": return GetAll<Setting>(ref filter);
                case "USERSESSION": return GetAll<UserSession>(ref filter);
                #endregion App

                #region Medical
                case "ANATOMYTAG": return GetAll<AnatomyTag>(ref filter);
                case "ANATOMY": return GetAll<Anatomy>(ref filter);
                case "SIDEAFFECT": return GetAll<SideAffect>(ref filter);
                case "SYMPTOM": return GetAll<Symptom>(ref filter);
                case "SYMPTOMLOG": return GetAll<SymptomLog>(ref filter);
                case "DOSELOG": return GetAll<DoseLog>(ref filter);
                #endregion Medical

                case "STAGEDATA": return GetAll<StageData>(ref filter);

                #region Equipment
                case "BALLAST": return GetAll<Ballast>(ref filter);
                case "BULB": return GetAll<Bulb>(ref filter);
                case "FAN": return GetAll<Fan>(ref filter);
                case "FILTER": return GetAll<Filter>(ref filter);
                case "PUMP": return GetAll<Pump>(ref filter);
                case "VEHICLE": return GetAll<Vehicle>(ref filter);
                case "INVENTORYITEM": return GetAll<InventoryItem>(ref filter);
                #endregion Equipment

                #region Events
                case "NOTIFICATION": return GetAll<Notification>(ref filter);
                case "REMINDER": return GetAll<Reminder>(ref filter);
                case "REMINDERRULE": return GetAll<ReminderRule>(ref filter);
                case "EVENT": return GetAll<Event>(ref filter);
                case "EVENTMEMBER": return GetAll<EventMember>(ref filter);
                case "EVENTITEM": return GetAll<EventItem>(ref filter);
                case "EVENTGROUP": return GetAll<EventGroup>(ref filter);
                case "EVENTLOCATION": return GetAll<EventLocation>(ref filter);

                #endregion Events

                #region Finance
                case "CURRENCY": return GetAll<Currency>(ref filter);
                case "FEE": return GetAll<Fee>(ref filter);
                case "FINANCEACCOUNT": return GetAll<FinanceAccount>(ref filter);
                case "FINANCEACCOUNTTRANSACTION": return GetAll<FinanceAccountTransaction>(ref filter);
                case "PAYMENTGATEWAYLOG": return GetAll<PaymentGatewayLog>(ref filter);
                #endregion Finance

                #region Geo
                case "LOCATION": return GetAll<Location>(ref filter);
                #endregion Geo

                #region Logging
                case "ACCESSLOG": return GetAll<AccessLog>(ref filter);
                case "REQUESTLOG": return GetAll<RequestLog>(ref filter);
                case "LINEITEMLOG": return GetAll<LineItemLog>(ref filter);
                case "MEASUREMENTLOG": return GetAll<MeasurementLog>(ref filter);
                case "EMAILMESSAGE": return GetAll<EmailMessage>(ref filter);
                case "LOGENTRY": return GetAll<LogEntry>(ref filter);
                #endregion Logging

                #region Membership
                case "ACCOUNT": return GetAll<Account>(ref filter);
                case "ACCOUNTMEMBER": return GetAll<AccountMember>(ref filter);
                case "APIKEY": return GetAll<ApiKey>(ref filter);
                case "CREDENTIAL": return GetAll<Credential>(ref filter);
                case "ROLEPERMISSION": return GetAll<RolePermission>(ref filter);
                case "ProfileMember": return GetAll<ProfileMember>(ref filter);
                case "PROFILE": return GetAll<Profile>(ref filter);
                case "ROLE": return GetAll<Role>(ref filter);
                case "USER": return GetAll<User>(ref filter);
                case "USERROLE": return GetAll<UserRole>(ref filter);
                case "PERMISSION": return GetAll<Permission>(ref filter);
                case "VERIFICATIONENTRY": return GetAll<VerificationEntry>(ref filter);
                case "BLOCKEDROLE": return GetAll<BlockedRole>(ref filter);
                case "BLOCKEDUSER": return GetAll<BlockedUser>(ref filter);
                #endregion Membership

                #region Plant
                case "PLANT": return GetAll<Plant>(ref filter);
                case "STRAIN": return GetAll<Strain>(ref filter);
                #endregion Plant

                #region Store
                case "PRODUCT": return GetAll<Product>(ref filter);
                case "VENDOR": return GetAll<Vendor>(ref filter);
                case "SHOPPINGCART": return GetAll<ShoppingCart>(ref filter);
                case "SHOPPINGCARTITEM": return GetAll<ShoppingCartItem>(ref filter);
                case "PRICERULE": return GetAll<PriceRule>(ref filter);
                case "PRICERULELOG": return GetAll<PriceRuleLog>(ref filter);
                case "ORDER": return GetAll<Order>(ref filter);
                case "ORDERITEM": return GetAll<OrderItem>(ref filter);
                    #endregion Store
            }

            return res;
        }

        public async Task<T> GetAsync<T>(int id) where T : class
        {
            T res = null;
            try
            {
                using (var connection = base.Database.Connection)
                {
                    connection.Open();
                    res = await connection.GetAsync<T>(id);
                }
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "GetAsync:" + typeof(T).ToString());
                Debug.Assert(false, ex.DeserializeException(true));
            }
            return res;
        }

        public bool Insert<T>(T entity) where T : class
        {
            this.Message = "";
            try
            {
                base.Set<T>().Add(entity);

                int res = SaveChanges();
                return res > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.DeserializeException(true));
                this.Message += ex.DeserializeException(true);
                _fileLogger.InsertError(this.Message, "GreenWerxDbContext", "Insert:" + typeof(T).ToString());
                return false;
            }
        }

        public async Task<int> InsertAsync<T>(T entity) where T : class
        {
            int res = 0;
            try
            {
                base.Set<T>().Add(entity);
                res = await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "InsertAsync:" + typeof(T).ToString());
                return res;
            }
            return res;
        }

        public async Task<int> SaveAsync()
        {
            try
            {
                return await base.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "SaveAsync");
            }
            return 0;
        }

        public new int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    this.Message += "Entity of type \"" + eve.Entry.Entity.GetType().Name +
                        "\" in state \"" + eve.Entry.State + "\" has the following validation errors:";
                    foreach (var ve in eve.ValidationErrors)
                    {
                        this.Message += "- Property: \"" + ve.PropertyName + "\", Error: \"" + ve.ErrorMessage + "\"";
                    }
                }
            }
            catch (DbUpdateConcurrencyException cex)
            {
                // Client wins update
                var entry = cex.Entries.Single();
                entry.OriginalValues.SetValues(entry.GetDatabaseValues());

                _fileLogger.InsertError(cex.DeserializeException(true) + Environment.NewLine + this.Message, "GreenWerxDbContext", "SaveChanges:");
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "SaveChanges:");
            }
            return 0;
        }

        //        if (Database.Connection.State != ConnectionState.Open)
        //            Database.Connection.Open();
        public IEnumerable<T> Select<T>(string sql, object parameters) where T : class
        {
            IEnumerable<T> res = new List<T>();

            try
            {
                if (Database.Connection.State != ConnectionState.Open)
                    Database.Connection.Open();

                res = Database.Connection.Query<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Select:" + typeof(T).ToString() + " " + sql);
            }

            return res;
        }

        public async Task<IEnumerable<T>> SelectAsync<T>(string sql, object parameters) where T : class
        {
            IEnumerable<T> res = null;

            if (Validator.HasSqlCommand(sql))
            {
                this._fileLogger.InsertSecurity("illegal statement:" + sql, "GreenWerxDbContext", "SelectAsync");

                Debug.Assert(false, "Illegal statement.");
                return res;
            }

            try
            {
                using (var connection = base.Database.Connection)
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    res = await connection.QueryAsync<T>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "SelectAsync:" + typeof(T).ToString());
                Debug.Assert(false, ex.DeserializeException(true));
            }

            return res;
        }

        //public IEnumerable<T> GetAll<T>(DataFilter filter) where T : class
        //{
        //    try
        //    {
        /// <summary>
        /// If this is not updating, check the Id property, it may need to be
        /// set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Update<T>(T entity) where T : class
        {
            int res = 0;

            bool saveFailed = false;
            do
            {
                try
                {
                    if (Database.Connection.State != ConnectionState.Open)
                        Database.Connection.Open();

                    res = Database.Connection.Update<T>(entity);
                    //        res = SaveChanges();
                    return res;
                }
                catch (DbUpdateConcurrencyException cex)
                {
                    saveFailed = true;
                    res = 0;

                    // Database wins update
                    cex.Entries.Single().Reload();

                    //    Client wins update
                    var entry = cex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                    _fileLogger.InsertError(cex.DeserializeException(true), "GreenWerxDbContext", "Update.1:" + typeof(T).ToString());
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.DeserializeException(true));
                    _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Update.2:" + typeof(T).ToString());
                    res = 0;
                }
            } while (saveFailed);

            return res;
        }

        public async Task<int> UpdateAsync<T>(T entity) where T : class
        {
            int res = 0;
            try
            {
                Entry<T>(entity).State = EntityState.Modified;
                res = await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "UpdateAsync:" + typeof(T).ToString());
                return res;
            }

            return res;
        }

        /// <summary>
        /// Copies a sqlite boilerplate database to the App_Data folder for initial use.
        /// </summary>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        protected ServiceResult CopyDefaultSQLiteDatabase(AppInfo appSettings)
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", "");

            string pathToDefaults = Path.Combine(directory, "App_Data\\Install\\GreenWerx.sqlite");

            if (!File.Exists(pathToDefaults))
                return new ServiceResult() { Code = 500, Status = "ERROR", Message = "Default database file missing in " + pathToDefaults };

            try
            {    //Connection.DataSource is null so hack away at it :/
                 //BACKLOG: Warn user and let them choose to overwrite
                if (File.Exists(PathToDatabase))
                {
                    File.Move(PathToDatabase, PathToDatabase + "_BACKUP_" + string.Format("text-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.UtcNow));
                }
                File.Copy(pathToDefaults, PathToDatabase);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "CopyDefaultSQLiteDatabase:" + PathToDatabase);
                return new ServiceResult() { Code = 500, Status = "ERROR", Message = "Error creating database: " + ex.DeserializeException(true) };
            }
            return new ServiceResult() { Code = 200, Status = "OK" };
        }

        protected void Initialize(string nameOrConnectionString)
        {
            if (string.IsNullOrWhiteSpace(nameOrConnectionString))
                return;

            ConnectionKey = nameOrConnectionString;
            try
            {
                ConnectionStringSettingsCollection connStrings = ConfigurationManager.ConnectionStrings;

                foreach (ConnectionStringSettings settings in connStrings)
                {
                    if (settings.Name.EqualsIgnoreCase(nameOrConnectionString) || settings.ConnectionString == nameOrConnectionString)
                    {
                        ConnectionSettings = settings;
                        break;
                    }
                }

                if (ConnectionSettings != null)
                {
                    if (ConnectionSettings.ProviderName.ToUpper().Contains("SQLITE"))
                    {
                        _providerType = "SQLITE";
                        string directory = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", "") + "App_Data";

                        AppDomain.CurrentDomain.SetData("DataDirectory", directory);
                        string dataSource = this.ConnectionSettings.ConnectionString.Replace("data source=|DataDirectory|", "").Replace(";", "");
                        PathToDatabase = directory + dataSource;

                        //This gets rid of the provider doesn't support database creation errror.
                        Database.SetInitializer<GreenWerxDbContext>(null);
                    }
                    else if (ConnectionSettings.ProviderName.ToUpper().Contains("MYSQL"))
                    {
                        _providerType = "MYSQL";
                    }
                    else if (ConnectionSettings.ProviderName.ToUpper().Contains("SQLCLIENT"))
                    {
                        _providerType = "MSSQL";
                    }
                }
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "Initialize");
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<StageData>())) { TypeTables.Add(DatabaseEx.GetTableName<StageData>(), new StageData()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Location>())) { TypeTables.Add(DatabaseEx.GetTableName<Location>(), new Location()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Post>())) { TypeTables.Add(DatabaseEx.GetTableName<Post>(), new Post()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Tag>())) { TypeTables.Add(DatabaseEx.GetTableName<Tag>(), new Tag()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<GreenWerx.Models.General.Attribute>())) { TypeTables.Add(DatabaseEx.GetTableName<GreenWerx.Models.General.Attribute>(), new GreenWerx.Models.General.Attribute()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Category>())) { TypeTables.Add(DatabaseEx.GetTableName<Category>(), new Category()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<UnitOfMeasure>())) { TypeTables.Add(DatabaseEx.GetTableName<UnitOfMeasure>(), new UnitOfMeasure()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<StatusMessage>())) { TypeTables.Add(DatabaseEx.GetTableName<StatusMessage>(), new StatusMessage()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<GreenWerx.Models.General.Favorite>())) { TypeTables.Add(DatabaseEx.GetTableName<GreenWerx.Models.General.Favorite>(), new GreenWerx.Models.General.Favorite()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<AppInfo>())) { TypeTables.Add(DatabaseEx.GetTableName<AppInfo>(), new AppInfo()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Setting>())) { TypeTables.Add(DatabaseEx.GetTableName<Setting>(), new Setting()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<UserSession>())) { TypeTables.Add(DatabaseEx.GetTableName<UserSession>(), new UserSession()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<AnatomyTag>())) { TypeTables.Add(DatabaseEx.GetTableName<AnatomyTag>(), new AnatomyTag()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Anatomy>())) { TypeTables.Add(DatabaseEx.GetTableName<Anatomy>(), new Anatomy()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<SideAffect>())) { TypeTables.Add(DatabaseEx.GetTableName<SideAffect>(), new SideAffect()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Symptom>())) { TypeTables.Add(DatabaseEx.GetTableName<Symptom>(), new Symptom()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<SymptomLog>())) { TypeTables.Add(DatabaseEx.GetTableName<SymptomLog>(), new SymptomLog()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<DoseLog>())) { TypeTables.Add(DatabaseEx.GetTableName<DoseLog>(), new DoseLog()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Ballast>())) { TypeTables.Add(DatabaseEx.GetTableName<Ballast>(), new Ballast()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Bulb>())) { TypeTables.Add(DatabaseEx.GetTableName<Bulb>(), new Bulb()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Fan>())) { TypeTables.Add(DatabaseEx.GetTableName<Fan>(), new Fan()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Filter>())) { TypeTables.Add(DatabaseEx.GetTableName<Filter>(), new Filter()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Pump>())) { TypeTables.Add(DatabaseEx.GetTableName<Pump>(), new Pump()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Vehicle>())) { TypeTables.Add(DatabaseEx.GetTableName<Vehicle>(), new Vehicle()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Currency>())) { TypeTables.Add(DatabaseEx.GetTableName<Currency>(), new Currency()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Fee>())) { TypeTables.Add(DatabaseEx.GetTableName<Fee>(), new Fee()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<FinanceAccount>())) { TypeTables.Add(DatabaseEx.GetTableName<FinanceAccount>(), new FinanceAccount()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<FinanceAccountTransaction>())) { TypeTables.Add(DatabaseEx.GetTableName<FinanceAccountTransaction>(), new FinanceAccountTransaction()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<PriceRule>())) { TypeTables.Add(DatabaseEx.GetTableName<PriceRule>(), new PriceRule()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<PaymentGatewayLog>())) { TypeTables.Add(DatabaseEx.GetTableName<PaymentGatewayLog>(), new PaymentGatewayLog()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Event>())) { TypeTables.Add(DatabaseEx.GetTableName<Event>(), new Event()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<EventMember>())) { TypeTables.Add(DatabaseEx.GetTableName<EventMember>(), new EventMember()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<EventGroup>())) { TypeTables.Add(DatabaseEx.GetTableName<EventGroup>(), new EventGroup()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<EventItem>())) { TypeTables.Add(DatabaseEx.GetTableName<EventItem>(), new EventItem()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<EventLocation>())) { TypeTables.Add(DatabaseEx.GetTableName<EventLocation>(), new EventLocation()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Notification>())) { TypeTables.Add(DatabaseEx.GetTableName<Notification>(), new Notification()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Reminder>())) { TypeTables.Add(DatabaseEx.GetTableName<Reminder>(), new Reminder()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<ReminderRule>())) { TypeTables.Add(DatabaseEx.GetTableName<ReminderRule>(), new ReminderRule()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<AccessLog>())) { TypeTables.Add(DatabaseEx.GetTableName<AccessLog>(), new AccessLog()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<AffiliateLog>())) { TypeTables.Add(DatabaseEx.GetTableName<AffiliateLog>(), new AffiliateLog()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<RequestLog>())) { TypeTables.Add(DatabaseEx.GetTableName<RequestLog>(), new RequestLog()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<LineItemLog>())) { TypeTables.Add(DatabaseEx.GetTableName<LineItemLog>(), new LineItemLog()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<MeasurementLog>())) { TypeTables.Add(DatabaseEx.GetTableName<MeasurementLog>(), new MeasurementLog()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<EmailMessage>())) { TypeTables.Add(DatabaseEx.GetTableName<EmailMessage>(), new EmailMessage()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<LogEntry>())) { TypeTables.Add(DatabaseEx.GetTableName<LogEntry>(), new LogEntry()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Account>())) { TypeTables.Add(DatabaseEx.GetTableName<Account>(), new Account()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<AccountMember>())) { TypeTables.Add(DatabaseEx.GetTableName<AccountMember>(), new AccountMember()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<ApiKey>())) { TypeTables.Add(DatabaseEx.GetTableName<ApiKey>(), new ApiKey()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Credential>())) { TypeTables.Add(DatabaseEx.GetTableName<Credential>(), new Credential()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<RolePermission>())) { TypeTables.Add(DatabaseEx.GetTableName<RolePermission>(), new RolePermission()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<ProfileMember>())) { TypeTables.Add(DatabaseEx.GetTableName<ProfileMember>(), new ProfileMember()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Profile>())) { TypeTables.Add(DatabaseEx.GetTableName<Profile>(), new Profile()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Role>())) { TypeTables.Add(DatabaseEx.GetTableName<Role>(), new Role()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<User>())) { TypeTables.Add(DatabaseEx.GetTableName<User>(), new User()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<UserRole>())) { TypeTables.Add(DatabaseEx.GetTableName<UserRole>(), new UserRole()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<VerificationEntry>()))
            {
                TypeTables.Add(DatabaseEx.GetTableName<VerificationEntry>(), new BlockedRole());
            }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<BlockedRole>()))
            {
                TypeTables.Add(DatabaseEx.GetTableName<BlockedRole>(), new BlockedRole());
            }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<BlockedUser>()))
            {
                TypeTables.Add(DatabaseEx.GetTableName<BlockedUser>(), new BlockedUser());
            }

            //string tblName = DatabaseEx.GetTableName<BlockedRole>();
            //modelBuilder.Entity<BlockedRole>().ToTable(tblName);

            //var dtblName = DatabaseEx.GetTableName<BlockedUser>();
            //modelBuilder.Entity<BlockedUser>().ToTable(dtblName);

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Permission>())) { TypeTables.Add(DatabaseEx.GetTableName<Permission>(), new Permission()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Product>())) { TypeTables.Add(DatabaseEx.GetTableName<Product>(), new Product()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Vendor>())) { TypeTables.Add(DatabaseEx.GetTableName<Vendor>(), new Vendor()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<ShoppingCart>())) { TypeTables.Add(DatabaseEx.GetTableName<ShoppingCart>(), new ShoppingCart()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<ShoppingCartItem>())) { TypeTables.Add(DatabaseEx.GetTableName<ShoppingCartItem>(), new ShoppingCartItem()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<InventoryItem>())) { TypeTables.Add(DatabaseEx.GetTableName<InventoryItem>(), new InventoryItem()); }

            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<PriceRule>())) { TypeTables.Add(DatabaseEx.GetTableName<PriceRule>(), new PriceRule()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<PriceRuleLog>())) { TypeTables.Add(DatabaseEx.GetTableName<PriceRuleLog>(), new PriceRuleLog()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Order>())) { TypeTables.Add(DatabaseEx.GetTableName<Order>(), new Order()); }
            if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<OrderItem>())) { TypeTables.Add(DatabaseEx.GetTableName<OrderItem>(), new OrderItem()); }

            try
            {
                //Note: if you get error 'The entity type XXXXX is not part of the model for the current context.' you need to add it below.
                // make sure the table name is correct in DatabaseEx.GetTableName

                modelBuilder.Entity<StageData>().ToTable(DatabaseEx.GetTableName<StageData>()).HasKey(o => o.UUID).
                    Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                         {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                #region General

                modelBuilder.Entity<Tag>()
                     .ToTable(DatabaseEx.GetTableName<Tag>())
                     .HasKey(o => o.UUID)
                     .Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                     {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                     }));

                modelBuilder.Entity<GreenWerx.Models.General.Attribute>()
                          .ToTable(DatabaseEx.GetTableName<GreenWerx.Models.General.Attribute>())
                          .HasKey(o => o.UUID)
                          .Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                          {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                          }));

                modelBuilder.Entity<Category>()
                        .ToTable(DatabaseEx.GetTableName<Category>())
                        .HasKey(o => o.UUID)
                        .Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Post>()
                            .ToTable(DatabaseEx.GetTableName<Post>())
                            .HasKey(o => o.UUID)
                            .Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                            }));

                modelBuilder.Entity<UnitOfMeasure>().ToTable(DatabaseEx.GetTableName<UnitOfMeasure>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<StatusMessage>().ToTable(DatabaseEx.GetTableName<StatusMessage>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<GreenWerx.Models.General.Favorite>()
                       .ToTable(DatabaseEx.GetTableName<GreenWerx.Models.General.Favorite>())
                       .HasKey(o => o.UUID)
                       .Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                       }));
                #endregion General

                #region App

                modelBuilder.Entity<AppInfo>().ToTable(DatabaseEx.GetTableName<AppInfo>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Setting>().ToTable(DatabaseEx.GetTableName<Setting>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<UserSession>().ToTable(DatabaseEx.GetTableName<UserSession>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                #endregion App

                #region Medical
                modelBuilder.Entity<AnatomyTag>().ToTable(DatabaseEx.GetTableName<AnatomyTag>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Anatomy>().ToTable(DatabaseEx.GetTableName<Anatomy>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<SideAffect>().ToTable(DatabaseEx.GetTableName<SideAffect>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Symptom>().ToTable(DatabaseEx.GetTableName<Symptom>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<SymptomLog>().ToTable(DatabaseEx.GetTableName<SymptomLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<DoseLog>().ToTable(DatabaseEx.GetTableName<DoseLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                #endregion Medical

                #region Equipment
                modelBuilder.Entity<Ballast>().ToTable(DatabaseEx.GetTableName<Ballast>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Bulb>().ToTable(DatabaseEx.GetTableName<Bulb>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Fan>().ToTable(DatabaseEx.GetTableName<Fan>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Filter>().ToTable(DatabaseEx.GetTableName<Filter>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Pump>().ToTable(DatabaseEx.GetTableName<Pump>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Vehicle>().ToTable(DatabaseEx.GetTableName<Vehicle>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<InventoryItem>().ToTable(DatabaseEx.GetTableName<InventoryItem>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                      {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                #endregion Equipment

                #region Events
                modelBuilder.Entity<Event>().ToTable(DatabaseEx.GetTableName<Event>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                             {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<EventMember>() // //    Table("Cat")       Map(s => s.CatId).Key(KeyType.Assigned);            AutoMap();
                    .ToTable(DatabaseEx.GetTableName<EventMember>())
                    .HasKey(o => o.UUID).Property(p => p.UUID)
                    .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<EventGroup>().ToTable(DatabaseEx.GetTableName<EventGroup>())
                    .HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                          {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<EventItem>().ToTable(DatabaseEx.GetTableName<EventItem>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<EventLocation>().ToTable(DatabaseEx.GetTableName<EventLocation>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Notification>().ToTable(DatabaseEx.GetTableName<Notification>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Reminder>().ToTable(DatabaseEx.GetTableName<Reminder>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<ReminderRule>().ToTable(DatabaseEx.GetTableName<ReminderRule>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                #endregion Events

                #region Finance
                modelBuilder.Entity<Currency>().ToTable(DatabaseEx.GetTableName<Currency>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                      {
                                new IndexAttribute("IX_UUID") { IsUnique = true }
                            }));

                modelBuilder.Entity<Fee>().ToTable(DatabaseEx.GetTableName<Fee>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                     {
                                new IndexAttribute("IX_UUID") { IsUnique = true }
                            }));
                modelBuilder.Entity<FinanceAccount>().ToTable(DatabaseEx.GetTableName<FinanceAccount>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                     {
                                new IndexAttribute("IX_UUID") { IsUnique = true }
                            }));
                modelBuilder.Entity<FinanceAccountTransaction>().ToTable(DatabaseEx.GetTableName<FinanceAccountTransaction>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                     { new IndexAttribute("IX_UUID") { IsUnique = true } }));

                modelBuilder.Entity<PriceRule>().ToTable(DatabaseEx.GetTableName<PriceRule>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                          { new IndexAttribute("IX_UUID") { IsUnique = true } }));

                modelBuilder.Entity<PaymentGatewayLog>().ToTable(DatabaseEx.GetTableName<PaymentGatewayLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            { new IndexAttribute("IX_UUID") { IsUnique = true }}));
                #endregion Finance

                #region Geo
                modelBuilder.Entity<Location>().ToTable(DatabaseEx.GetTableName<Location>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                          {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                #endregion Geo

                #region Logging

                modelBuilder.Entity<AccessLog>().ToTable(DatabaseEx.GetTableName<AccessLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<AffiliateLog>().ToTable(DatabaseEx.GetTableName<AffiliateLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                      {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<RequestLog>().ToTable(DatabaseEx.GetTableName<RequestLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                          {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<LineItemLog>().ToTable(DatabaseEx.GetTableName<LineItemLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                           {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<MeasurementLog>().ToTable(DatabaseEx.GetTableName<MeasurementLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<EmailMessage>().ToTable(DatabaseEx.GetTableName<EmailMessage>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                     {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                //Data project...
                modelBuilder.Entity<LogEntry>().ToTable(DatabaseEx.GetTableName<LogEntry>()).HasKey(o => o.UUID);

                #endregion Logging

                #region Membership
                modelBuilder.Entity<Account>().ToTable(DatabaseEx.GetTableName<Account>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<AccountMember>().ToTable(DatabaseEx.GetTableName<AccountMember>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<ApiKey>().ToTable(DatabaseEx.GetTableName<ApiKey>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Credential>().ToTable(DatabaseEx.GetTableName<Credential>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<RolePermission>().ToTable(DatabaseEx.GetTableName<RolePermission>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<ProfileMember>().ToTable(DatabaseEx.GetTableName<ProfileMember>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Role>().ToTable(DatabaseEx.GetTableName<Role>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<User>().ToTable(DatabaseEx.GetTableName<User>()).HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<UserRole>().ToTable(DatabaseEx.GetTableName<UserRole>()).HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<VerificationEntry>().ToTable(DatabaseEx.GetTableName<VerificationEntry>())
                .HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                     {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                    }));

                modelBuilder.Entity<BlockedRole>().ToTable(DatabaseEx.GetTableName<BlockedRole>()).HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                         {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<BlockedUser>().ToTable(DatabaseEx.GetTableName<BlockedUser>()).HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                         {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Permission>().ToTable(DatabaseEx.GetTableName<Permission>()).HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Profile>().ToTable(DatabaseEx.GetTableName<Profile>()).HasKey(usr => usr.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                #endregion Membership

                #region Plant
                modelBuilder.Entity<Plant>().ToTable(DatabaseEx.GetTableName<Plant>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                            {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Plant>())) { TypeTables.Add(DatabaseEx.GetTableName<Plant>(), new Plant()); }

                modelBuilder.Entity<Strain>().ToTable(DatabaseEx.GetTableName<Strain>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                if (!TypeTables.ContainsKey(DatabaseEx.GetTableName<Strain>())) { TypeTables.Add(DatabaseEx.GetTableName<Strain>(), new Strain()); }

                #endregion Plant

                #region Store

                modelBuilder.Entity<Product>().ToTable(DatabaseEx.GetTableName<Product>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<Vendor>().ToTable(DatabaseEx.GetTableName<Vendor>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<ShoppingCart>().ToTable(DatabaseEx.GetTableName<ShoppingCart>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<ShoppingCartItem>().ToTable(DatabaseEx.GetTableName<ShoppingCartItem>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                        {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                modelBuilder.Entity<PriceRule>().ToTable(DatabaseEx.GetTableName<PriceRule>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<PriceRuleLog>().ToTable(DatabaseEx.GetTableName<PriceRuleLog>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<Order>().ToTable(DatabaseEx.GetTableName<Order>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));
                modelBuilder.Entity<OrderItem>().ToTable(DatabaseEx.GetTableName<OrderItem>()).HasKey(o => o.UUID).Property(p => p.UUID).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new[]
                       {
                            new IndexAttribute("IX_UUID") { IsUnique = true }
                        }));

                #endregion Store
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "GreenWerxDbContext", "OnModelCreating");
                Debug.Assert(false, ex.DeserializeException(true));
            }
        }

        #endregion Initialization

        #region ---===  CRUD Async Functions  ===---
        #endregion ---===  CRUD Async Functions  ===---

        #region ---=== CRUD Functions ===---
        #endregion ---=== CRUD Functions ===---


        #region     ---=== CRUD Functions ===---
        #region     ---=== Installer Code ===---
        #endregion
        #endregion
    }


    public class MultipleDbConfiguration : DbConfiguration
    {

        //public MultipleDbConfiguration()
        //{
        //    SetProviderServices(MySqlProviderInvariantName.ProviderName, new MySqlProviderServices());
        //}


        //public static DbConnection GetMySqlConnection(string connectionString)
        //{
        //    var connectionFactory = new MySqlConnectionFactory();

        //    return connectionFactory.CreateConnection(connectionString);
        //}

    }

}