using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.TransactionScopes.Tests;

[TestFixture]
public class ItWorksWithSecondLevelRetries : FixtureBase
{
    [Test]
    public async Task ItSureDoes()
    {
        using var activator = new BuiltinHandlerActivator();
        using var secondLevelDispatchSucceeded = new ManualResetEvent(initialState: false);

        var handlerInvocationCounter = 0L;
        var secondLevelHandlerInvocationCounter = 0L;

        activator.Handle<string>(async _ =>
        {
            Interlocked.Increment(ref handlerInvocationCounter);
            throw new ApplicationException("oh no!");
        });
        activator.Handle<IFailed<string>>(async _ =>
        {
            Interlocked.Increment(ref secondLevelHandlerInvocationCounter);
            secondLevelDispatchSucceeded.Set();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true))
            .Options(o => o.HandleMessagesInsideTransactionScope())
            .Start();

        var bus = activator.Bus;

        await bus.SendLocal("HEJ 🙂");

        secondLevelDispatchSucceeded.WaitOrDie(timeout: TimeSpan.FromSeconds(5));

        // be absolutely sure that nothing else happens
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.That(handlerInvocationCounter, Is.EqualTo(5));
        Assert.That(secondLevelHandlerInvocationCounter, Is.EqualTo(1));
    }
}