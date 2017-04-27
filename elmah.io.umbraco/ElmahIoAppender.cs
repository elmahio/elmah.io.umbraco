using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace Elmah.Io.Umbraco
{
    public class ElmahIoAppender : AppenderSkeleton
    {
        public IElmahioAPI Client;
        private Guid _logId;
        private string _apiKey;

        public string LogId
        {
            set
            {
                if (!Guid.TryParse(value, out _logId))
                {
                    throw new ArgumentException("LogId is not a GUID");
                }
            }
        }

        public string ApiKey
        {
            set { _apiKey = value; }
        }

        public string Application { get; set; }

        public override void ActivateOptions()
        {
            EnsureClient();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            EnsureClient();

            var message = new CreateMessage
            {
                Title = loggingEvent.RenderedMessage,
                Severity = LevelToSeverity(loggingEvent.Level).ToString(),
                DateTime = loggingEvent.TimeStampUtc,
                Detail = loggingEvent.ExceptionObject?.ToString(),
                Data = PropertiesToData(loggingEvent.GetProperties()),
                Application = Application ?? loggingEvent.Domain,
                Source = loggingEvent.LoggerName,
                User = loggingEvent.UserName,
                Hostname = Hostname(loggingEvent),
                Type = Type(loggingEvent),
            };

            Client.Messages.CreateAndNotify(_logId, message);
        }

        private string Type(LoggingEvent loggingEvent)
        {
            return loggingEvent.ExceptionObject?.GetType().FullName;
        }

        private string Hostname(LoggingEvent loggingEvent)
        {
            var log4netHostname = "log4net:HostName";
            var properties = loggingEvent.GetProperties();
            if (properties == null || properties.Count == 0 || !properties.Contains(log4netHostname)) return null;
            return properties[log4netHostname].ToString();
        }

        private List<Item> PropertiesToData(PropertiesDictionary properties)
        {
            return properties.GetKeys().Select(key => new Item {Key = key, Value = properties[key].ToString()}).ToList();
        }

        private Severity? LevelToSeverity(Level level)
        {
            if (level == Level.Emergency) return Severity.Fatal;
            if (level == Level.Fatal) return Severity.Fatal;
            if (level == Level.Alert) return Severity.Fatal;
            if (level == Level.Critical) return Severity.Fatal;
            if (level == Level.Severe) return Severity.Fatal;
            if (level == Level.Error) return Severity.Error;
            if (level == Level.Warn) return Severity.Warning;
            if (level == Level.Notice) return Severity.Information;
            if (level == Level.Info) return Severity.Information;
            if (level == Level.Debug) return Severity.Debug;
            if (level == Level.Fine) return Severity.Verbose;
            if (level == Level.Trace) return Severity.Verbose;
            if (level == Level.Finer) return Severity.Verbose;
            if (level == Level.Verbose) return Severity.Verbose;
            if (level == Level.Finest) return Severity.Verbose;

            return Severity.Information;
        }

        private void EnsureClient()
        {
            if (Client == null)
            {
                Client = ElmahioAPI.Create(_apiKey);
            }
        }
    }
}