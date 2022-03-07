using System;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.TransactionScopes;

[StepDocumentation("Incoming pipeline step that ensures that the current ambient System.Transactions.Transaction is mounted when handling the message")]
class TransactionScopeIncomingStep : IIncomingStep
{
    public const string CurrentTransactionContextKey = "current-system-transactions-transaction"; 

    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var items = context.Load<ITransactionContext>().Items;

        if (!items.TryGetValue(CurrentTransactionContextKey, out var temp))
        {
            await next();
            return;
        }

        if (!(temp is Transaction transaction))
        {
            await next();
            return;
        }

        using (var scope = new TransactionScope(transaction, TransactionScopeAsyncFlowOption.Enabled))
        {
            await next();
            scope.Complete();
        }
    }
}