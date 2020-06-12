// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.

namespace GreenWerx.Models.Geo
{
    public class GeoIp
    {
        public string LocationUUID { get; set; }

        #region blocks file

        public string EndIpNum { get; set; }
        public int LocationId { get; set; }

        public string StartIpNum { get; set; }

        #endregion blocks file

        #region Location File

        public string AreaCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string MetroCode { get; set; }
        public string PostalCode { get; set; }
        public string Region { get; set; }

        #endregion Location File
    }
}