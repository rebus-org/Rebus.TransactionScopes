# Rebus.TransactionScopes

[![install from nuget](https://img.shields.io/nuget/v/Rebus.TransactionScope.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.TransactionScope)

Provides a `System.Transactions.TransactionScope` helper for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

Use it like this:

	using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
	{
		scope.EnlistRebus();

		// this one is automatically enlisted in the ambient .NET transaction
		await _bus.Send("ostemad");

		scope.Complete();
	}

