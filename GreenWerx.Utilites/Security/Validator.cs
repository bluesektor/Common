// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GreenWerx.Models.Flags;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Utilites.Security
{
    public class Validator
    {
        private static string[] badTags = "<script>,<applet>,<body>,<embed>,<frame>,<frameset><html>,<iframe>,<img>,<style>,<layer>,<link>,<ilayer>,<meta>,<object>".Split(',');

        private static string[] logins = "admin,guest,mod,m0d,owner,0wner,omniports,account,job, help,billing,press,spam,sales,support,supp0rt,service,manager,director,president,ceo,editor,email,test,secure,root,NULL, operator,webmaster, backup,demo, test, trial,member, private member,private,moderator,m0derator".Split(',');

        private static string[] sqlres = @"1=1,1=2,go?,!and*,../,exec,--,readfile,cmdshell,.xss,.exe,.bat,xp_cmdshell,xp_startmail,xp_sendmail,sp_makewebtask,null,
            OR 'SQLi' = 'SQL'+'i',OR 'SQLi' > 'S',or 20 > 1,OR 2 between 3 and 1,OR 'SQLi' = N'SQLi',1 and 1 = 1,1 || 1 = 1,1 && 1 = 1".Split(',');

        protected Validator()
        {
        }

        #region Fields

        public static string Message { get; set; }

        #endregion Fields

        #region Code injection

        //These are sites that host raw code that hackers use to inject and exploit backdoors.
        //For example this was added to a request on a wordpress site I had  go?http://pastebin.com/raw.php?i=pa3y1psn
        //So if their adding code to requests the fuck them.
        private static string[] _codeSites = "chopapp.com, hastebin.com,tny.cz,snipt.org,pastie.org,privatepaste.com,reviewboard.org,copypastecode.com,aspin.com,zubrag.com,snipt.net,darkcoding.net,codelifter.com,gonet.biz,dpaste.com,codepad.org,friendpaste.com,codepaste.net,slexy.org".Split(',');

        public static bool HasCodeInjection(string scanThis)
        {
            Message = "";
            foreach (string codeSite in _codeSites)//backlog see if we can optimize this (not use a loop).
            {
                if (scanThis.Contains(codeSite))
                {
                    Message = "Code injection: " + scanThis;
                    return true;
                }
            }
            return false;
        }

        #endregion Code injection

        //"#" ,"*" ;,..*,,

        public static string eregi(string str, string patrn, string rpl)
        {
            Regex re = new Regex(patrn);
            return re.Replace(str, patrn);
        }

        //This function will return true as soon as an attempt is found, it won't continue to
        //find other attack vectors. If we want to to a report on how many attack vectors were attempted in
        //one request then we can write another function to scan the logged attacks to another table with
        //the attacks in each row.
        //
        public static bool HasBadTags(string scanThis)
        {
            Message = "";
            foreach (string tag in badTags)//backlog see if we can optimize this (not use a loop).
            {
                if (scanThis.Contains(tag))
                {
                    Message = "HTML Tag bad:" + tag;
                    return true;
                }
            }

            return false;
        }

        public static bool HasElevatedSqlCommand(string scanThis)
        {
            foreach (string sqlWord in sqlres)//backlog see if we can optimize this (not use a loop).
            {
                if (scanThis.Contains(sqlWord))
                {
                    Message = "SQL reserved word:" + sqlWord;
                    return true;
                }
            }
            if (scanThis.Contains("union ")) { Message = "union"; return true; }
            if (scanThis.Contains("drop index")) { Message = "dropIndex"; return true; }
            if (scanThis.Contains("drop table")) { Message = "dropTable"; return true; }
            if (scanThis.Contains("drop database")) { Message = "dropDatabase"; return true; }
            if (scanThis.Contains("delete from")) { Message = "deleteFrom"; return true; }
            if (scanThis.Contains("create database")) { Message = "createDatabase"; return true; }
            if (scanThis.Contains("create table")) { Message = "createTable"; return true; }
            if (scanThis.Contains("create index")) { Message = "createIndex"; return true; }
            if (scanThis.Contains("create unique")) { Message = "createUnique"; return true; }

            return false;
        }

        public static bool HasReservedLoginName(string emailAddress)
        {
            Message = "";
            emailAddress = emailAddress.ToLower();
            string[] emailArr = emailAddress.Split('@');

            if (emailArr.Length > 1)
            {
                string name = emailArr[0];

                foreach (string login in logins)
                {
                    if (login == name)
                    {
                        Message = "ReservedName used:" + login;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HasSqlCommand(string scanThis)
        {
            if (HasElevatedSqlCommand(scanThis)) { return true; }
            if (scanThis.Contains("insert into")) { Message = "insertInto"; return true; }
            if (scanThis.Contains("select *")) { Message = "selectAll"; return true; }
            if (scanThis.Contains("select from")) { Message = "selectFrom"; return true; }

            return false;
        }

        public static bool IsEmailInjectionAttempt(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return false;

            Message = "";
            string[] ResWords = "to:,bcc:,cc:,),[,[:space:],],*,:,multipart, subject:".Split(',');
            bool bFound = true;
            emailAddress = emailAddress.ToLower();

            foreach (string Word in ResWords)
            {
                bFound = emailAddress.Contains(Word);

                if (true == bFound)
                {
                    Message = "IsEmailInjectionAttempt:" + Word;
                    return bFound;
                }
            }
            return bFound;
        }

        public static bool IsMinAge(DateTime dob, int minAgeYears)
        {
            if (dob == DateTime.MinValue)
                return false;

            int age = DateTime.Today.Year - dob.Year;

            if (age >= minAgeYears)
                return true;

            return false;
        }

        public static bool IsValidEmailFormat(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress) || emailAddress.Length > 255)
                return false;

            if (new EmailAddressAttribute().IsValid(emailAddress))
                return true;

            return false;
        }

        public static bool IsValidRelationshipStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            if (status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Couple) ||
                status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Group) ||
                status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Poly) ||
                 status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.Single) ||
                status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.SingleMale) ||
                status.EqualsIgnoreCase(ProfileFlags.RelationshipStatus.SingleFemale)
                )
                return true;

            return false;
        }
    }
}