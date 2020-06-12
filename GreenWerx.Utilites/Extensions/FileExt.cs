using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GreenWerx.Models.App;

namespace GreenWerx.Utilites.Extensions
{
    public static class FileExt
    {
        public static ServiceResult TryMove( string from, string to)
        {
            if (string.IsNullOrEmpty(from) || File.Exists(from) == false)
                return ServiceResponse.Error("From file path is empty or file does not exist.");

            if (string.IsNullOrEmpty(to))
                return ServiceResponse.Error("From file path is empty or file does not exist.");

            try
            {
                File.Move(from, to);
                return ServiceResponse.OK();
            }
            catch (Exception ex)
            {
                //Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
        }

        public static string GetFileExtensionFromUrl(this string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            return url.Contains('.') ? url.Substring(url.LastIndexOf('.')) : "";
        }

        public static IEnumerable<string> ReadAsLines(this string pathToFile)
        {
            using (var reader = new StreamReader(pathToFile))
                while (!reader.EndOfStream)
                    yield return reader.ReadLine();
        }
    }
}