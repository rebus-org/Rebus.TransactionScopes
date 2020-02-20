# Rebus.TransactionScopes

[![install from nuget](https://img.shields.io/nuget/v/Rebus.TransactionScopes.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.TransactionScopes)

Provides a `System.Transactions.TransactionScope` helper for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

Use it like this when you send/publish things from anywhere besides inside Rebus handlers:

	using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
	{
		scope.EnlistRebus();

		// this one is automatically enlisted in the ambient .NET transaction
		await _bus.Send("ostemad");

		scope.Complete();
	}

Use it like this to have Rebus handlers invoked inside a `TransactionScope`:

    Configure.With(...)
        .(...)
        .Options(o =>
        {
            o.HandleMessagesInsideTransactionScope();
        })
        .Start();

By default, the transaction scope will use the `IsolationLevel.ReadCommitted` isolation level with a 1 minute timeout. These
values can be configured by passing an instance of `TransactionOptions` to `HandleMessagesInsideTransactionScope`.

That's about it.