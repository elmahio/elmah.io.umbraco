using Elmah.Io.Client;
using Microsoft.Extensions.Options;
using System;
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

            string result = results.AllChecksSuccessful ? "Healthy" : "Unhealthy";
            var reason = $"Results of the scheduled Umbraco Health Checks run on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()} are as follows:\n\n{results.ResultsAsMarkDown(Verbosity)}";

            await heartbeats.CreateAsync(heartbeatId, logId, new CreateHeartbeat
            {
                Result = result,
                Reason = reason,
            });
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
