// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
namespace GreenWerx.Models.Flags
{
    public class SettingFlags
    {
        public struct Types
        {
            public const string Boolean = "BOOL";
            public const string DateTime = "DATETIME";
            public const string Decimal = "DECIMAL";
            public const string EncryptedString = "STRING.ENCRYPTED";
            public const string Numeric = "INT";
            public const string String = "STRING";
        }
    }
}