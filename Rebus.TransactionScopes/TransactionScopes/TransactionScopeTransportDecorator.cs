using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Messages;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.TransactionScopes;

/// <summary>
/// Decorator of <see cref="ITransport"/> that ensures that everything from the receive of a message until its completion is executed inside a transaction scope
/// </summary>
class TransactionScopeTransportDecorator : ITransport
{
    readonly ITransport _transport;
    readonly TransactionOptions _transactionOptions;

    public TransactionScopeTransportDecorator(ITransport transport, TransactionOptions transactionOptions)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _transactionOptions = transactionOptions;
    }

    public void CreateQueue(string address) => _transport.CreateQueue(address);

    public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context) => _transport.Send(destinationAddress, message, context);

    public string Address => _transport.Address;

    public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
    {
        var scope = new TransactionScope(TransactionScopeOption.Required, _transactionOptions,
            TransactionScopeAsyncFlowOption.Enabled);

        context.OnCompleted(async _ => scope.Complete());
        context.OnDisposed(_ => scope.Dispose());

        // stash current tx so we can re-attach it later
        context.Items[TransactionScopeIncomingStep.CurrentTransactionContextKey] = Transaction.Current;

        return await _transport.Receive(context, cancellationToken);
    }
}