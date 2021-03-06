﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Diagnostics;
using GreenWerx.Utilites.Security;

namespace GreenWerx.Managers.Membership
{
    public class KeyManager : BaseManager
    {
        public KeyManager(string connectionKey, string sessionKey) : base(connectionKey, sessionKey)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionKey), "KeyManager CONTEXT IS NULL!");

            this._connectionKey = connectionKey;
        }

        public string GenerateKey(string type, int keyLength)
        {
            if (keyLength <= 0)
                return string.Empty;

            string res = Cipher.RandomString(keyLength);

            switch (type.ToUpper())
            {
                case "APIKEY":
                    // Debug.Assert(false, "NOT IMPLEMENTED");
                    // if(ApiKeyExists(res)){return GenerateKey(type, keyLength);}backlog implement
                    break;
            }

            return res;
        }
    }
}