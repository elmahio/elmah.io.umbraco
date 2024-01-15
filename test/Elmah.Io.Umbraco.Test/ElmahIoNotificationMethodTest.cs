using Elmah.Io.Client;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.HealthChecks;

namespace Elmah.Io.Umbraco.Test
{
    public class ElmahIoNotificationMethodTest
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private ElmahIoNotificationMethod sut;
        private IHeartbeatsClient heartbeatsClient;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [SetUp]
        public void SetUp()
        {
            var options = Substitute.For<IOptionsMonitor<HealthChecksSettings>>();
            var settings = new HealthChecksSettings
            {
                Notification = new HealthChecksNotificationSettings
                {
                    Enabled = true,
                    NotificationMethods = new Dictionary<string, HealthChecksNotificationMethodSettings>
                    {
                        {
                            "elmah.io", new HealthChecksNotificationMethodSettings
                            {
                                Verbosity = HealthCheckNotificationVerbosity.Summary,
                                Enabled = true,
                                Settings = new Dictionary<string, string>
                                {
                                    { "apiKey", "API_KEY" },
                                    { "logId", "LOG_ID" },
                                    { "heartbeatId", "HEARTBEAT_ID" }
                                }
                            }
                        }
                    }
                }
            };
            options.CurrentValue.Returns(settings);
            heartbeatsClient = Substitute.For<IHeartbeatsClient>();
            sut = new ElmahIoNotificationMethod(options)
            {
                heartbeats = heartbeatsClient
            };
        }

        [Test]
        public async Task CanNotifyHealthy()
        {
            // Arrange
            var checks = await HealthCheckResults.Create(new List<HealthCheck>());

            // Act
            await sut.SendAsync(checks);

            // Assert
            await heartbeatsClient.Received().CreateAsync(Arg.Is("HEARTBEAT_ID"), Arg.Is("LOG_ID"), Arg.Is<CreateHeartbeat>(ch => ch.Result == "Healthy"));
        }

        [Test]
        public async Task CanNotifyUnhealthy()
        {
            // Arrange
            var checks = await HealthCheckResults.Create(new List<HealthCheck>
            {
                new TestHealthCheck()
            });
            var markdown = checks.ResultsAsMarkDown(HealthCheckNotificationVerbosity.Summary);

            // Act
            await sut.SendAsync(checks);

            // Assert
            await heartbeatsClient.Received().CreateAsync(Arg.Is("HEARTBEAT_ID"), Arg.Is("LOG_ID"), Arg.Is<CreateHeartbeat>(ch => ch.Result == "Unhealthy" && ch.Reason.Contains(markdown)));
        }
    }

    [HealthCheck("d7f57626-0473-496c-a956-a7fa8815909f", "test")]
    internal class TestHealthCheck : HealthCheck
    {
        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<HealthCheckStatus>> GetStatus()
        {
            return Task.FromResult<IEnumerable<HealthCheckStatus>>(new List<HealthCheckStatus>
            {
                new("Oh no")
                {
                    ResultType = StatusResultType.Error
                }
            });
        }
    }
}