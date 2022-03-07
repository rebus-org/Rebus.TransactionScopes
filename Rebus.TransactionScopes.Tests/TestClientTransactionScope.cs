using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable AccessToDisposedClosure

#pragma warning disable 1998

namespace Rebus.TransactionScopes.Tests;

[TestFixture]
public class TestClientTransactionScope : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "tx-text"))
            .Create();
    }

    static IEnumerable<Scenario> GetScenarios() => new Scenario[]
    {
        new(ThrowException: false, EnlistInScope: true, CompleteTheScope: false, ExpectToReceiveMessage: false),
        new(ThrowException: true, EnlistInScope: true, CompleteTheScope: false, ExpectToReceiveMessage: false),
        new(ThrowException: false, EnlistInScope: true, CompleteTheScope: false, ExpectToReceiveMessage: false),
        new(ThrowException: false, EnlistInScope: true, CompleteTheScope: true, ExpectToReceiveMessage: true),
        new(ThrowException: true, EnlistInScope: false, CompleteTheScope: true, ExpectToReceiveMessage: true)
    };

    [TestCaseSource(nameof(GetScenarios))]
    public async Task SendsMessageOnlyWhenTransactionScopeIsCompleted(Scenario scenario)
    {
        var (throwException, enlistInScope, completeTheScope, expectToReceiveMessage) = scenario;

        using var gotMessage = new ManualResetEvent(initialState: false);

        _activator.Handle<string>(async _ => gotMessage.Set());

        var bus = _starter.Start();

        try
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (enlistInScope)
            {
                scope.EnlistRebus();
            }

            await bus.SendLocal("hallå i stuen!1");

            if (throwException)
            {
                throw new ApplicationException("omg what is this?????");
            }

            if (completeTheScope)
            {
                scope.Complete();
            }
        }
        catch (ApplicationException exception) when (exception.Message == "omg what is this?????")
        {
            Console.WriteLine("An exception occurred... quite expected though");
        }

        Assert.That(gotMessage.WaitOne(TimeSpan.FromSeconds(1)), Is.EqualTo(expectToReceiveMessage),
            $@"Given the scenario 

    {scenario}

we expected that the message {(expectToReceiveMessage ? "WOULD be RECEIVED" : "would NOT BE RECEIVED")}");
    }

    public record Scenario(bool ThrowException, bool EnlistInScope, bool CompleteTheScope, bool ExpectToReceiveMessage);
}