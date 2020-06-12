// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
namespace GreenWerx.Models.Flags
{
    public class UserFlags
    {
        public struct ProviderName
        {
            public const string ForgotPassword = "USER.ACCOUNT.FORGOTPASSWORD.KEY";
            public const string SendAccountInfo = "USER.SEND.ACCOUNT.INFO.KEY";
            public const string ValidateEmail = "USER.REGISTRATION.EMAIL.VALIDATION.KEY";
        }
    }
}