﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.TransactionScopes.Tests;

[TestFixture]
public class TestTransactionScopeSupport : FixtureBase
{
    [TestCase(true)]
    [TestCase(false)]
    public void CanHandleMessagesInsideTransactionScope(bool useTransactionScope)
    {
        var done = Using(new ManualResetEvent(false));
        var activator = Using(new BuiltinHandlerActivator());

        var detectedAmbientTransaction = false;

        activator.Handle<string>(async _ =>
        {
            await Task.Delay(10);
            detectedAmbientTransaction = Transaction.Current != null;

            done.Set();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "txtest"))
            .Options(o =>
            {
                if (useTransactionScope)
                {
                    o.HandleMessagesInsideTransactionScope();
                }

                o.LogPipeline();
            })
            .Start();

        activator.Bus.SendLocal("hej").Wait();

        done.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(detectedAmbientTransaction, Is.EqualTo(useTransactionScope),
            $"Detected: {detectedAmbientTransaction}, expected: {useTransactionScope}");
    }
}