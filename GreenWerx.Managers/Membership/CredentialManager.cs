// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Diagnostics;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Models.App;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;

namespace GreenWerx.Managers.Membership
{
    public class CredentialManager : BaseManager
    {
        public CredentialManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "CredentialManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public ServiceResult Insert(Credential c)
        {
            if (!this.DataAccessAuthorized(c, "POST", false)) return ServiceResponse.Error("You are not authorized this action.");

            using (var context = new GreenWerxDbContext(this._connectionKey))
            {
                Credential dbU = context.GetAll<Credential>()?.FirstOrDefault(wu => wu.Name.EqualsIgnoreCase(c.Name) && wu.AccountUUID == c.AccountUUID);

                if (dbU != null)
                    return ServiceResponse.Error("Credential already exists.");

                c.UUID = Guid.NewGuid().ToString("N");

                c.UUIDType = "Credential";
                if (context.Insert<Credential>(c))
                    return ServiceResponse.OK("", c);
            }
            return ServiceResponse.Error("An error occurred inserting credential " + c.Name);
        }
    }
}