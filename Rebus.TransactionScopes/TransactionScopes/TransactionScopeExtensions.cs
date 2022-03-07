using System;
using System.Transactions;
using Rebus.Transport;

namespace Rebus.TransactionScopes;

/// <summary>
/// Extension for <see cref="TransactionScope"/> that allows for enlisting an ambient Rebus transaction in an ambient .NET transaction.
/// </summary>
public static class TransactionScopeExtensions
{
    /// <summary>
    /// Starts a new Rebus transcation, enlisting it to be committed in the COMMIT phase of the ambient .NET transaction. Can only be
    /// called once
    /// </summary>
    public static TransactionScope EnlistRebus(this TransactionScope transactionScope)
    {
        if (transactionScope == null) throw new ArgumentNullException(nameof(transactionScope));

        if (AmbientTransactionContext.Current != null)
        {
            throw new InvalidOperationException("Cannot start a new ambient Rebus transaction because there is already one associated with the current execution context!");
        }

        var transaction = Transaction.Current;

        if (transaction == null)
        {
            throw new InvalidOperationException(
                "Cannot enlist a new ambient Rebus transaction in the current transaction scope becausethere's no current transaction" +
                " on the thread!! Did you accidentally begin the transaction scope WITHOUT the TransactionScopeAsyncFlowOption.Enabled" +
                " option? You must ALWAYS remember the TransactionScopeAsyncFlowOption.Enabled switch when you start an ambient .NET" +
                " transaction and you intend to work with async/await, because otherwise the ambient .NET transaction will not flow" +
                " properly to threads when executing continuations.");
        }

        var ambientTransactionBridge = new AmbientTransactionBridge(new RebusTransactionScope());

        transaction.EnlistVolatile(ambientTransactionBridge, EnlistmentOptions.None);

        transaction.TransactionCompleted += (_, _) => ambientTransactionBridge.Dispose();

        return transactionScope;
    }

    class AmbientTransactionBridge : IEnlistmentNotification, IDisposable
    {
        readonly RebusTransactionScope _scope;

        public AmbientTransactionBridge(RebusTransactionScope scope) => _scope = scope ?? throw new ArgumentNullException(nameof(scope));

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            _scope.Complete();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}