using Azure.Messaging;
using Distops.Core.Model;
using Distops.ServiceBus.Test.Samples;
using Distops.ServiceBus.Utils;
using FluentAssertions;

namespace Distops.ServiceBus.Test;

public class CloudEventTests
{
    [Test]
    public void TestRoundTrip()
    {
        long v = 5;
        var distopContext = new DistopContext
        {
            Arguments = new object[] { v },
            GenericArguments = new List<SerializableType> {typeof(long)},
            MethodDeclaringObject = typeof(IAsyncDistop),
            MethodName = nameof(Core.Test.Samples.IAsyncDistop.FireAndForget),
            MethodReturnType = typeof(void)
        };

        var data = distopContext.ToCloudEvent();
        var cloudEvent = CloudEvent.Parse(data);
        var res = cloudEvent?.Data?.ToObjectFromJson<DistopContext>();

        res.Should().BeEquivalentTo(distopContext);
    }
}