// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.

// https://stackoverflow.com/questions/14427596/convert-an-int-to-bool-with-json-net
//
using Newtonsoft.Json;
using System;

namespace GreenWerx.Models.Helpers
{
    public class BoolConverter : JsonConverter
    {
        public object StringEx { get; private set; }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (
                reader.Value?.ToString() == "1" ||
                reader.Value?.ToString().ToLower() == "true" ||
                reader.Value?.ToString().ToLower() == "on");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }

    public class IntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(int);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            int tmp = 0;

            if (int.TryParse(reader.Value?.ToString(), out tmp))
                return tmp;

            return 0;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}