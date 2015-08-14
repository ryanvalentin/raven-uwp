using Newtonsoft.Json;
using System;

namespace RavenClient.Helpers
{
    public class RavenLogLevelJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            RavenLogLevel logLevel = (RavenLogLevel)value;
            switch (logLevel)
            {
                case RavenLogLevel.Debug:
                    writer.WriteValue("debug");
                    break;
                case RavenLogLevel.Info:
                    writer.WriteValue("info");
                    break;
                case RavenLogLevel.Warning:
                    writer.WriteValue("warning");
                    break;
                case RavenLogLevel.Error:
                    writer.WriteValue("error");
                    break;
                case RavenLogLevel.Fatal:
                    writer.WriteValue("fatal");
                    break;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var logLevelString = (string)reader.Value;
            RavenLogLevel? logLevel = null;
            switch (logLevelString)
            {
                case "debug":
                    logLevel = RavenLogLevel.Debug;
                    break;
                case "info":
                    logLevel = RavenLogLevel.Info;
                    break;
                case "warning":
                    logLevel = RavenLogLevel.Warning;
                    break;
                case "error":
                    logLevel = RavenLogLevel.Error;
                    break;
                case "fatal":
                    logLevel = RavenLogLevel.Fatal;
                    break;
            }

            return logLevel;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
