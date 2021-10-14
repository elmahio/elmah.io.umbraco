using Elmah.Io.Client;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Web.HealthCheck;
using Umbraco.Web.HealthCheck.NotificationMethods;

namespace Elmah.Io.Umbraco
{
    [HealthCheckNotificationMethod("elmah.io")]
    public class ElmahIoNotificationMethod : NotificationMethodBase
    {
        internal static string _assemblyVersion = typeof(ElmahIoNotificationMethod).Assembly.GetName().Version.ToString();
        internal static string _umbracoAssemblyVersion = typeof(NotificationMethodBase).Assembly.GetName().Version.ToString();

        private IHeartbeatsClient heartbeats;
        private readonly ILogger logger;

        public ElmahIoNotificationMethod(ILogger logger)
        {
            if (Settings == null)
            {
                Enabled = false;
                return;
            }

            var apiKey = Settings["apiKey"]?.Value;
            var logId = Settings["logId"]?.Value;
            var heartbeatId = Settings["heartbeatId"]?.Value;
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(logId) || string.IsNullOrWhiteSpace(heartbeatId))
            {
                Enabled = false;
                return;
            }

            ApiKey = apiKey;
            LogId = logId;
            HeartbeatId = heartbeatId;

            this.logger = logger;
        }

        public string ApiKey { get; private set; }

        public string LogId { get; set; }

        public string HeartbeatId { get; set; }

        public override async Task SendAsync(HealthCheckResults results, CancellationToken token)
        {
            if (ShouldSend(results) == false)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(LogId) || string.IsNullOrWhiteSpace(HeartbeatId))
            {
                return;
            }

            if (heartbeats == null)
            {
                var api = (ElmahioAPI)ElmahioAPI.Create(ApiKey, new ElmahIoOptions
                {
                    Timeout = new TimeSpan(0, 0, 30),
                    UserAgent = UserAgent(),
                });
                heartbeats = api.Heartbeats;
            }

            string result = results.AllChecksSuccessful ? "Healthy" : "Unhealthy";
            var reason = $"Results of the scheduled Umbraco Health Checks run on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()} are as follows:\n\n{results.ResultsAsMarkDown(Verbosity)}";

            try
            {
                await heartbeats.CreateAsync(HeartbeatId, LogId, new CreateHeartbeat
                {
                    Result = result,
                    Reason = reason,
                });
            }
            catch (Exception e)
            {
                logger.Error(GetType(), e);
                throw;
            }
        }

        private string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Umbraco", _assemblyVersion)).ToString())
                .Append(" ")
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("UmbracoCms.Web", _umbracoAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
