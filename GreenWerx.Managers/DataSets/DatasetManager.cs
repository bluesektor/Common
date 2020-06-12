// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GreenWerx.Data;
using GreenWerx.Data.Helpers;
using GreenWerx.Data.Logging;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Flags;
using GreenWerx.Utilites.Extensions;

/// <summary>
/// For implementation see
/// See
///     ReportsController
///     web page..
/// </summary>
namespace GreenWerx.Managers.DataSets
{
    public class DatasetManager : BaseManager
    {
        private SystemLogger _logger = null;

        public DatasetManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "DatasetManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
            _logger = new SystemLogger(_connectionKey);
        }

        public List<dynamic> GetData(string type)
        {
            List<dynamic> dataset = new List<dynamic>();

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    dataset = context.GetAllOf(type).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                _logger.InsertError(ex.Message, "DatasetManager", "GetData:" + type);
            }

            return dataset;
        }

        public List<DataPoint> GetDataSet(string type, ref DataFilter filter)
        {
            var screens = filter.Screens.OrderBy(o => o.Order).ToList();

            string accountUUID = this._requestingUser.AccountUUID;

            List<DataPoint> dataset = new List<DataPoint>();

            if (string.IsNullOrWhiteSpace(type))
                return dataset;

            try
            {
                using (var context = new GreenWerxDbContext(this._connectionKey))
                {
                    var list = context.GetAllOf(type, ref filter)?.Where(w => w.AccountUUID == accountUUID).ToList();

                    foreach (dynamic item in list)
                    {
                        dataset.Add(new DataPoint()
                        {
                            Value = JsonConvert.SerializeObject(item),
                            ValueType = type
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.Assert(false, ex.Message);
                //dataset.Add(new DataPoint()
                //{
                //    Value = "Error getting report data.",
                //    ValueType = "ERROR"
                //});
                //_logger.InsertError(ex.Message + Environment.NewLine + dq.SQL, "DatasetManager", "GetData:" + type + ":" + field);
            }
            finally
            {
                //if (reader != null) { reader.Close(); }
                ////if (conn != null) conn.Close();
            }
            return dataset;
        }

        /// <summary>
        /// returns a query based on the screens
        /// Not using parameters. kept throwing “Invalid type owner for DynamicMethod” error
        /// </summary>
        /// <param name="screens"></param>
        /// <returns></returns>
        protected DataQuery BuildQuery<T>(List<DataScreen> screens) where T : class
        {
            DataQuery dq = new DataQuery();
            string table = DatabaseEx.GetTableName<T>();
            if (string.IsNullOrEmpty(table))
            {
                dq.SQL = "ERROR: Table name could not be parsed.";
                return dq;
            }

            dq.SQL = "SELECT * FROM " + table;

            if (screens.Count() == 0)
                return dq;

            dq.SQL += GetWhereClause(screens);

            dq.Parameters = null;

            return dq;
        }

        protected DataQuery BuildQuery(string typeName, List<DataScreen> screens)
        {
            DataQuery dq = new DataQuery();

            string table = DatabaseEx.GetTableName(typeName);

            if (string.IsNullOrEmpty(table))
            {
                dq.SQL = "ERROR: Table name could not be found.";
                return dq;
            }

            //Set default sql query
            dq.SQL = "SELECT * FROM " + table;
            dq.Parameters = null;

            if (screens == null || screens.Count == 0)
                return dq;

            //Check if we do a distinct query.
            //
            DataScreen distinctDataScreen = screens.FirstOrDefault(w => w.Command?.ToLower() == "distinct");

            if (distinctDataScreen == null)
                dq.SQL += GetWhereClause(screens);
            else
                dq.SQL = "SELECT DISTINCT( " + distinctDataScreen.Field + " ) FROM " + table + GetWhereClause(screens);

            return dq;
        }

        private string GetWhereClause(List<DataScreen> screens)
        {
            if (screens == null || screens.Count == 0)
                return string.Empty;

            screens = screens.Where(fw => fw.Type == "sql").ToList();
            if (screens == null)
                return string.Empty;

            StringBuilder SQL = new StringBuilder(" WHERE ");

            int screenIndex = 1;

            foreach (DataScreen screen in screens)
            {
                if (screen.Type != "sql" || string.IsNullOrWhiteSpace(screen.Command) || screen.Command?.ToLower() == "distinct")
                    continue;

                #region range query

                if (screen.Command.EqualsIgnoreCase("BETWEEN"))
                {
                    if (screen.Order == 0)
                        SQL.Append(screen.Field + " BETWEEN '" + screen.Value + "' " + screen.Junction + " ");

                    if (screen.Order == 1)
                    {
                        SQL.Append(" '" + screen.Value + "'");
                        if (screenIndex != screens.Count)
                            SQL.Append(" " + screen.Junction + " ");
                    }
                    screenIndex++;
                    continue;
                }

                #endregion range query

                if (screenIndex == screens.Count)
                    SQL.Append(screen.Field + " " + screen.Command + " '" + screen.Value + "'");
                else
                    SQL.Append(screen.Field + " " + screen.Command + " '" + screen.Value + "' " + screen.Junction + " ");

                SQL = SQL.Replace("'(", "(").Replace(")'", ")");
                //dapper requires this format
                //SELECT * FROM " + this.TableName + " WHERE name = @accountName AND password = @accountPassword
                //var parameters2 = new { accountName = screen.Value, accountPassword = screen.Value };

                screenIndex++;
            }

            if (SQL.Equals(" WHERE "))
                return "";//no where statement compiled

            return SQL.ToString();
        }

        /// <summary>
        /// This is for queries in events, profiles etc. 
        /// </summary>
        /// <param name="screens"></param>
        /// <returns></returns>
        public static string BuildWhereClause(List<DataScreen> screens)
        {
            if (screens == null || screens.Count == 0)
                return string.Empty;

            screens = screens.Where(fw => fw.ParserType == "sql").ToList();
            if (screens == null || screens.Count == 0 )
                return string.Empty;

            StringBuilder SQL = new StringBuilder();

            int screenIndex = 1;

            foreach (DataScreen screen in screens)
            {
                // was screen.Type
                if (screen.ParserType != "sql" || string.IsNullOrWhiteSpace(screen.Command) || screen.Command?.ToLower() == "distinct")
                    continue;

                #region range query

                if (screen.Command.EqualsIgnoreCase("BETWEEN"))
                {
                    if (screen.Order == 0)
                        SQL.Append(screen.Field + " BETWEEN '" + screen.Value + "' " + screen.Junction + " ");

                    if (screen.Order == 1)
                    {
                        SQL.Append(" '" + screen.Value + "'");
                        if (screenIndex != screens.Count)
                            SQL.Append(" " + screen.Junction + " ");
                    }
                    screenIndex++;
                    continue;
                }

                #endregion range query

                if (screenIndex == screens.Count)
                    SQL.Append(screen.Field + " " + GetOperator(screen.Field, screen.Command, screen.Operator, screenIndex));// screen.Value));
                else// add the junction
                    SQL.Append(screen.Field + " " + GetOperator(screen.Field, screen.Command,screen.Operator, screenIndex) + " OR ");
                

                SQL = SQL.Replace("'(", "(").Replace(")'", ")");
                //dapper requires this format
                //SELECT * FROM " + this.TableName + " WHERE name = @accountName AND password = @accountPassword
                //var parameters2 = new { accountName = screen.Value, accountPassword = screen.Value };

                screenIndex++;
            }

            if (string.IsNullOrWhiteSpace(SQL.ToString()))
                return "";//no where statement compiled

            return SQL.ToString();
        }

        public static DynamicParameters GetParameters(List<DataScreen> screens)
        {
            if (screens == null || screens.Count == 0)
                return null;

            screens = screens.Where(fw => fw.ParserType == "sql").ToList();
            if (screens == null || screens.Count == 0)
                return null;

            DynamicParameters parameters = new DynamicParameters();

            int screenIndex = 1;

            foreach (DataScreen screen in screens)
            {
                // was screen.Type
                if (screen.ParserType != "sql" || string.IsNullOrWhiteSpace(screen.Command) || screen.Command?.ToLower() == "distinct")
                    continue;
                string value = screen.Value;
                //switch (screen.Operator?.ToUpper())
                //{
                //    case "CONTAINS":
                //        value = " \"%\"" + value + "\"%\" ";
                //        break;
                //   // case "SEARCH!BY":
                //    //    value = " <> @" + screen.Value + " ";
                //     //   break;
                  
                //}

                parameters.Add("@" + screen.Field + screenIndex.ToString(), value );
                screenIndex++;
            }
            return parameters;
            
        }

        /// <summary>
        /// Pass in the screen.Operator to get the SQL operator
        /// </summary>
        /// <param name="screenOperator"></param>
        /// <returns></returns>
        private static string GetOperator(string field,string screenCommand, string screenOperator, int index)//, string screenValue )
        {
            string res = "";
            switch (screenOperator)
            {
                case "CONTAINS":
                    // "SELECT * from country WHERE Name LIKE '%' + @name + '%';"
                    // res = "LIKE '%'@" + field + index.ToString() + "'|% ";
                    res = "LIKE CONCAT('%',@" + field + index.ToString() + ", '%') ";
                    break;
                case "SEARCH!BY":
                    res = " <> @" + field + index.ToString() + " ";
                    break;
                default:
                    res = " = @" + field + index.ToString() + " ";
                    break;
            }
            //switch (screenOperator) {
            //    case "CONTAINS":
            //        res = " '%" + screenValue + "%' ";
            //        break;
            //    case "SEARCH!BY":
            //        res = " <> '" + screenValue + "' ";
            //        break;
            //    default:
            //        res = " = '" + screenValue + "' ";
            //        break;
            //}
            return res;
        }

        public static string GetProfileQueryFilter(List<DataScreen> screens)
        {
            if (screens == null || screens.Count == 0)
                return string.Empty;

            var relationShipScreens = screens.Where(w => w.Field.EqualsIgnoreCase("RelationshipStatus") &&
                w.Value.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Group) ||
                  w.Value.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Couple) ||
                    w.Value.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Poly) ||
                      w.Value.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.SingleFemale) ||
                        w.Value.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.SingleMale)
             ).ToList();

            if (relationShipScreens == null || relationShipScreens.Count() == 0)
                return string.Empty;

            string query = "";
            for (int i = 0; i <  relationShipScreens.Count(); i++)
            {
                if (i == relationShipScreens.Count() - 1)
                    query += relationShipScreens[i].Field + " = '" + relationShipScreens[i].Value +"'";
                else
                    query += relationShipScreens[i].Field + " = '" + relationShipScreens[i].Value + "' OR ";


            }
            return query;
        }
    }
}