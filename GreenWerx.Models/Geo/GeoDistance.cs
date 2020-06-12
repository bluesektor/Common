// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;

namespace GreenWerx.Models.Geo
{
    public class GeoCoordinate
    {
        public GeoCoordinate()
        {
            MaxDistance = 100.0;
            Distances = new List<GeoCoordinate>();
        }

        public double Distance { get; set; }
        public List<GeoCoordinate> Distances { get; set; }
        public double Latitude { get; set; }
        public string LocationType { get; set; }
        public double Longitude { get; set; }
        public double MaxDistance { get; }
        public string Name { get; set; }
        public double SearchDistance { get; set; }

        public List<string> Tags { get; set; }
        public string UUID { get; set; }
    }
}