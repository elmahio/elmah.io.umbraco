using Elmah.Io.Client;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.HealthChecks.NotificationMethods;

namespace Elmah.Io.Umbraco
{
    /// <summary>
    /// Health check notification method for Umbraco that log results as elmah.io heartbeats.
    /// </summary>
    [HealthCheckNotificationMethod("elmah.io")]
    public class ElmahIoNotificationMethod : NotificationMethodBase
    {
        private static string _assemblyVersion = typeof(ElmahIoNotificationMethod).Assembly.GetName().Version.ToString();
        private static string _umbracoAssemblyVersion = typeof(NotificationMethodBase).Assembly.GetName().Version.ToString();
        internal IHeartbeatsClient heartbeats;

        private readonly string apiKey;
        private readonly string logId;
        private readonly string heartbeatId;

        /// <summary>
        /// Create a new instance of the notification method. This constructor should not be called manually but invoked
        /// automatically by Umbraco when the notification method is correctly configured in appsettings.json.
        /// </summary>
        public ElmahIoNotificationMethod(IOptionsMonitor<HealthChecksSettings> healthChecksSettings) : base(healthChecksSettings)
        {
            if (Settings == null)
            {
                Enabled = false;
                return;
            }

            apiKey = Settings?["apiKey"];
            logId = Settings?["logId"];
            heartbeatId = Settings?["heartbeatId"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(logId) || string.IsNullOrWhiteSpace(heartbeatId))
            {
                Enabled = false;
            }
        }

        /// <summary>
        /// Send a health check result to elmah.io as a heartbeat. This method should not be called manually but invoked
        /// automatically by Umbraco when the notification method is correctly configured in appsettings.json.
        /// </summary>
        public override async Task SendAsync(HealthCheckResults results)
        {
            if (!ShouldSend(results))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(logId) || string.IsNullOrWhiteSpace(heartbeatId))
            {
                return;
            }

            if (heartbeats == null)
            {
                var api = (ElmahioAPI)ElmahioAPI.Create(apiKey, new ElmahIoOptions
                {
                    Timeout = new TimeSpan(0, 0, 30),
                    UserAgent = UserAgent(),
                });
                heartbeats = api.Heartbeats;
            }

            var reason = $"Results of the scheduled Umbraco Health Checks run on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()} are as follows:\n\n{results.ResultsAsMarkDown(Verbosity)}";

            var all = new Dictionary<string, List<HealthCheckStatus>>();
            AddOrAppend(all, results.GetResultsForStatus(StatusResultType.Error));
            AddOrAppend(all, results.GetResultsForStatus(StatusResultType.Warning));
            AddOrAppend(all, results.GetResultsForStatus(StatusResultType.Info));
            AddOrAppend(all, results.GetResultsForStatus(StatusResultType.Success));

            var checks = GenerateChecks(all);

            string result = "Healthy";
            if (checks.Exists(check => check.Result == "Unhealthy")) result = "Unhealthy";
            else if (checks.Exists(check => check.Result == "Degraded")) result = "Degraded";

            await heartbeats.CreateAsync(heartbeatId, logId, new CreateHeartbeat
            {
                Result = result,
                Reason = reason,
                Checks = checks,
            });
        }

        private static List<Check> GenerateChecks(Dictionary<string, List<HealthCheckStatus>> all)
        {
            var checks = new List<Check>();
            foreach (var checkName in all.Keys)
            {
                var checkResults = all[checkName];

                var sb = new StringBuilder();
                foreach (var rrr in checkResults)
                {
                    sb.Append("- Result: '").Append(rrr.ResultType).Append("', Message: '").Append(rrr.Message).Append('\'').AppendLine();
                }

                var checkResult = "Healthy";
                if (checkResults.Exists(rrr => rrr.ResultType == StatusResultType.Error)) checkResult = "Unhealthy";
                else if (checkResults.Exists(rrr => rrr.ResultType == StatusResultType.Warning)) checkResult = "Degraded";

                checks.Add(new Check
                {
                    Name = checkName,
                    Result = checkResult,
                    Reason = sb.ToString(),
                });
            }

            return checks;
        }

        private static void AddOrAppend(Dictionary<string, List<HealthCheckStatus>> all, Dictionary<string, IEnumerable<HealthCheckStatus>> dictionary)
        {
            foreach (var check in dictionary)
            {
                if (!all.ContainsKey(check.Key))
                {
                    all.Add(check.Key, check.Value.ToList());
                }
                else
                {
                    all[check.Key].AddRange(check.Value);
                }
            }
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Umbraco", _assemblyVersion)).ToString())
                .Append(' ')
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Umbraco.Cms", _umbracoAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
