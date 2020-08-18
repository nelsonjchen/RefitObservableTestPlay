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

            new TestScheduler().With(
                scheduler =>
                {
                    _logger.Information("Setup variable to write into");
                    Thing thing = null;
                    _logger.Information("Setup observable");
                    var tenSeconds = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1), scheduler: RxApp.MainThreadScheduler)
                        .Do(
                            intervalNumber => { _logger.Information("interval #{number}", intervalNumber); }
                        );
                    var thingsObservable =
                        tenSeconds.Select(_ => someApiService.GetThing().Delay(TimeSpan.FromMilliseconds(1)));
                    var switchedObservable = thingsObservable.Switch();
                    switchedObservable.Subscribe(
                        onNext: remoteThing =>
                        {
                            _logger.Debug("Assigning Thing");
                            thing = remoteThing;
                        }
                    );
                    _logger.Information("Observable is setup");
                    _logger.Information("Advancing Time");
                    scheduler.AdvanceByMs(2000);
                    _logger.Information("Time is advanced");
                    scheduler.AdvanceByMs(1);

                    Assert.NotNull(thing);
                    Assert.Equal("R.J. MacReady", thing.Name);
                }
            );
        }
    }
}
