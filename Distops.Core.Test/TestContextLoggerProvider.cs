using Microsoft.Extensions.Logging;

namespace Distops.Core.Test
{
    [ProviderAlias("TestContext")]
    public class TestContextLoggerProvider : ILoggerProvider
    {
        private readonly TestContext _context;

        public TestContextLoggerProvider(TestContext context)
        {
            _context = context;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestContextLogger(_context, categoryName);
        }

        public void Dispose()
        {
        }

        private class TestContextLogger : ILogger
        {
            private readonly TestContext testContext;
            private readonly string categoryName;

            public TestContextLogger(TestContext testContext, string categoryName)
            {
                this.testContext = testContext;
                this.categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                TestContext.Progress.WriteLine($"[{timestamp}] [{logLevel}] [{categoryName}] - {formatter(state, exception)} {exception?.StackTrace}");
            }
        }

        internal class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            private NullScope()
            {
            }

            /// <inheritdoc />
            public void Dispose()
            {
            }
        }
    }
}