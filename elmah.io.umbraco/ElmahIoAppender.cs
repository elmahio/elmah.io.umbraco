using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace Elmah.Io.Umbraco
{
    public class ElmahIoAppender : AppenderSkeleton
    {
        private Logger _logger;
        private Guid _logId;

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

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_logger == null)
            {
                _logger = new Logger(_logId);
            }

            var message = new Message(loggingEvent.RenderedMessage)
            {
                Severity = LevelToSeverity(loggingEvent.Level),
                DateTime = loggingEvent.TimeStamp.ToUniversalTime(),
                Detail = loggingEvent.ExceptionObject != null ? loggingEvent.ExceptionObject.ToString() : null,
                Data = PropertiesToData(loggingEvent.Properties),
                Application = loggingEvent.Domain,
                Source = loggingEvent.LoggerName,
                User = loggingEvent.UserName,
                Hostname = Hostname(loggingEvent),
                Type = Type(loggingEvent),
            };

            _logger.Log(message);
        }

        private string Type(LoggingEvent loggingEvent)
        {
            if (loggingEvent.ExceptionObject == null) return null;
            return loggingEvent.ExceptionObject.GetType().FullName;
        }

        private string Hostname(LoggingEvent loggingEvent)
        {
            if (loggingEvent.Properties == null || loggingEvent.Properties.Count == 0) return null;
            return loggingEvent.Properties["log4net:HostName"].ToString();
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
    }
}