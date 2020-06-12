// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;

namespace GreenWerx.Models.Datasets
{
    public class Coordinate
    {
        public Coordinate()
        {
        }

        public Coordinate(string caption, float lat, float lon, DateTime pointDate, int pointIndex)
        {
            Caption = caption;
            Lat = lat;
            Lon = lon;
            Date = pointDate;
            Index = pointIndex;
        }

        public string Caption { get; set; }
        public DateTime? Date { get; set; }
        public int Index { get; set; }
        public float Lat { get; set; }
        public float Lon { get; set; }
        public int RoleWeight { get; set; }
    }

    public class DataPoint
    {
        public DataPoint()
        {
        }

        public DataPoint(string caption, string dataValue, string dataType, DateTime pointDate, int pointIndex, bool isSeries = true)
        {
            Caption = caption;
            Value = dataValue;
            ValueType = dataType;
            Date = pointDate;
            Index = pointIndex;
            IsSeries = isSeries;
        }

        public string Caption { get; set; }
        public DateTime? Date { get; set; }
        public int Index { get; set; }
        public bool IsSeries { get; set; }
        public int RoleWeight { get; set; }
        public string Value { get; set; }

        public string ValueType { get; set; }
    }
}