using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Reactive.Testing;
using ReactiveUI;
using ReactiveUI.Testing;
using Refit;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Abstractions;
using Serilog;


namespace RefitObservableTestPlay
{
    public class TheUnitTest
    {
        private ILogger _logger;

        public TheUnitTest(ITestOutputHelper output)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output)
                .CreateLogger()
                .ForContext<TheUnitTest>();
        }

        [Fact]
        public void GrabSomethingObservableTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/v1/api/thing")
                .Respond(
                    "application/json",
                    @"{
""Name"" : ""R.J. MacReady""
}"
                );
            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");
            var someApiService = RestService.For<ISomeApiService>(
                client
            );

            _logger.Information("Checking if awaited refit observable works");
            var syncThing = someApiService.GetThing().ToTask().Result;
            Assert.Equal("R.J. MacReady", syncThing.Name);

            _logger.Information("Check complete");

            var apiCallsToMake = 20;
            var assignments = 0;
            Thing thing = null;

            new TestScheduler().With(
                scheduler =>
                {
                    _logger.Debug("Scheduler Clock: {clock}", scheduler.Clock);

                    _logger.Information("Setup observable");
                    var tenSeconds = Observable.Timer(
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(1),
                            scheduler: RxApp.MainThreadScheduler
                        )
                        .Do(
                            intervalNumber => { _logger.Information("interval #{number}", intervalNumber); }
                        );
                    var thingsObservable =
                        tenSeconds.Select(
                            _ => someApiService.GetThing()
                        );
                    var switchedObservable = thingsObservable.Switch();
                    switchedObservable.Subscribe(
                        onNext: remoteThing =>
                        {
                            _logger.Debug("Assigning Thing");
                            assignments += 1;
                            thing = remoteThing;
                        }
                    );

                    for (int i = 0; i < apiCallsToMake; i += 1)
                    {
                        _logger.Debug("Scheduler Clock: {clock}", scheduler.Clock);
                        scheduler.AdvanceByMs(1000);
                    }

                    Assert.NotNull(thing);
                    Assert.Equal("R.J. MacReady", thing.Name);
                    Assert.Equal(40, assignments);
                }
            );
        }
    }
}
