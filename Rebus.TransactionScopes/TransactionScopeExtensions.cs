using System;
using System.Transactions;
using Rebus.Transport;

namespace Rebus.TransactionScopes
{
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

            if (Transaction.Current == null)
            {
                throw new InvalidOperationException(
                    "Cannot enlist a new ambient Rebus transaction in the current transaction scope, but there's no current transaction" +
                    " on the thread!! Did you accidentally begin the transaction scope WITHOUT the TransactionScopeAsyncFlowOption.Enabled" +
                    " option? You must ALWAYS remember the TransactionScopeAsyncFlowOption.Enabled switch when you start an ambient .NET" +
                    " transaction and you intend to work with async/await, because otherwise the ambient .NET transaction will not flow" +
                    " properly to threads when executing continuations.");
            }

            var scope = new DefaultTransactionContextScope();
            var transactionContext = AmbientTransactionContext.Current;

            var ambientTransactionBridge = new AmbientTransactionBridge(transactionContext, scope);

            Transaction.Current.EnlistVolatile(ambientTransactionBridge, EnlistmentOptions.None);

            Transaction.Current.TransactionCompleted += (o, ea) =>
            {
                ambientTransactionBridge.Dispose();
            };

            return transactionScope;
        }

        class AmbientTransactionBridge : IEnlistmentNotification, IDisposable
        {
            readonly ITransactionContext _transactionContext;
            readonly DefaultTransactionContextScope _scope;

            public AmbientTransactionBridge(ITransactionContext transactionContext, DefaultTransactionContextScope scope)
            {
                _transactionContext = transactionContext;
                _scope = scope;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                _scope.Complete()
                    .ContinueWith(_ =>
                    {
                        using (_transactionContext)
                        {
                            enlistment.Done();
                        }
                    });
            }

            public void Rollback(Enlistment enlistment)
            {
                _transactionContext.Dispose();

                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }

            static void CleanUp()
            {
                AmbientTransactionContext.SetCurrent(null);
            }

            public void Dispose()
            {
                CleanUp();
            }
        }
    }
}