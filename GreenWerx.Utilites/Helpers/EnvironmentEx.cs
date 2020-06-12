// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.IO;

namespace GreenWerx.Utilites.Helpers
{
    public class EnvironmentEx
    {
        protected EnvironmentEx()
        {
        }

        public static string AppDataFolder
        {
            get
            {
                string tmp = AppDomain.CurrentDomain.BaseDirectory.Replace("bin\\Debug\\", "").Replace("bin\\Release\\", "");
                return Path.Combine(tmp, "App_Data");
            }
        }
    }
}