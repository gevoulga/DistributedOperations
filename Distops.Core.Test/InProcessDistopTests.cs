using Distops.Core.Extensions;
using Distops.Core.Services;
using Distops.Core.Test.Samples;
using Distops.InProcess.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Distops.Core.Test
{
    public class InProcessDistopTests
    {
        private IServiceProvider sp;

        public InProcessDistopTests()
        {
            // var loggerFactory = new NLogLoggerFactory();
            // interceptorLogger = loggerFactory.CreateLogger<DistopInterceptor>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddFilter("Microsoft.Skype.ChatServiceTestFramework", LogLevel.Trace)
                .AddFilter("Microsoft.Teams.NotificationService", LogLevel.Trace)
                .AddProvider(new TestContextLoggerProvider(TestContext.CurrentContext)));
            // serviceCollection.AddSingleton<NLogLoggerFactory>();
            // serviceCollection.AddLogging();


            // The distops initialization
            serviceCollection
                .AddInProcessDistops() // Add the processing of distops (client + server is same for InProcessor)
                .AddSingleton<IAsyncDistop, AsyncDistop>()
                .AddSingleton<ISyncDistop, SyncDistop>()
                .AddSingleton<IThrowsDistop, ThrowsDistop>()
                .AddSingleton<IFireAndForgetDistop, FireAndForgetDistop>();

            sp = serviceCollection.BuildServiceProvider();
        }

        [Test]
        public void InProcessSyncDistop()
        {
            var proxy = sp.GetDistop<ISyncDistop>();
            TestContext.Progress.WriteLine("Starting calls to proxy");

            proxy.SyncFireAndForget();
            TestContext.Progress.WriteLine($"After fire and forget");

            var tick = proxy.SyncCallReturns();
            TestContext.Progress.WriteLine($"got tick {tick}");
        }

        [Test]
        public async Task InProcessAsyncDistop()
        {
            var proxy = sp.GetDistop<IAsyncDistop>();

            TestContext.Progress.WriteLine("Starting calls to proxy");
            // await proxy.DoSomething<bool>(new DistopDto(), true, CancellationToken.None);
            // TestContext.Progress.WriteLine("Done Something");

            await proxy.FireAndForget();
            TestContext.Progress.WriteLine($"After fire and forget");

            var justALong = await proxy.JustALong();
            TestContext.Progress.WriteLine($"just a long{justALong}");

            var tick1 = await proxy.CurrentTick(new DistopDto(), CancellationToken.None);
            TestContext.Progress.WriteLine($"got tick {tick1}");

            await Task.Delay(TimeSpan.FromSeconds(2));
            var tick2 = await proxy.CurrentTick(new DistopDto(), CancellationToken.None);
            TestContext.Progress.WriteLine($"got tick {tick2}");
        }

        [Test]
        public async Task InProcessDistopFireAndForget()
        {
            var proxy = sp.GetFireAndForgetDistop<IFireAndForgetDistop>();

            TestContext.Progress.WriteLine("Starting calls to proxy");
            // await proxy.DoSomething<bool>(new DistopDto(), true, CancellationToken.None);
            // TestContext.Progress.WriteLine("Done Something");

            // await proxy.Throws();
            // TestContext.Progress.WriteLine($"Throws");

            proxy.SyncFireAndForget();
            TestContext.Progress.WriteLine($"Should return immediately after sync fire and forget");

            await proxy.FireAndForget();
            TestContext.Progress.WriteLine($"Should return immediately after fire and forget");

            await Task.Delay(TimeSpan.FromSeconds(22));
        }

        [Test]
        public async Task Throws()
        {
            var proxy = sp.GetDistop<IThrowsDistop>();
            FluentActions.Invoking(() => proxy.ThrowsSync())
                .Should().Throw<InvalidOperationException>();
            await FluentActions.Invoking(async () => await proxy.ThrowsAsync())
                .Should().ThrowAsync<NotImplementedException>();
            await FluentActions.Invoking(async () => await proxy.ThrowsAsyncLong())
                .Should().ThrowAsync<NotImplementedException>();

        }


        [Test]
        public async Task TestMethod1()
        {

            var proxy = sp.GetDistop<IAsyncDistop>();
            // var proxy = new ProxyGenerator()
            //     .CreateInterfaceProxyWithoutTarget<IDistop>(interceptor);
            // var proxy = new ProxyGenerator()
            //     .CreateInterfaceProxyWithTarget(typeof(IDistop), distopImpl, interceptor) as IDistop;

            // var proxy = new ProxyGenerator().CreateClassProxyWithTarget(distopImpl.GetType(),
            //     distopImpl, new IInterceptor[] { interceptor }) as Distop;
            // var proxy = new ProxyGenerator()
            //     .CreateClassProxy<DistopImpl>(
            //         interceptor);

            TestContext.Progress.WriteLine("Starting calls to proxy");
            // await proxy.DoSomething<bool>(new DistopDto(), true, CancellationToken.None);
            // TestContext.Progress.WriteLine("Done Something");

            var tick1 = await proxy.CurrentTick(new DistopDto(), CancellationToken.None);
            TestContext.Progress.WriteLine($"got tick {tick1}");

            await Task.Delay(TimeSpan.FromSeconds(2));
            var tick2 = await proxy.CurrentTick(new DistopDto(), CancellationToken.None);
            TestContext.Progress.WriteLine($"got tick {tick2}");
        }
    }
}