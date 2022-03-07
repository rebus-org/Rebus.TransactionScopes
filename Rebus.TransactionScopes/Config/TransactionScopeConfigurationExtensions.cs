using System;
using System.Transactions;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.TransactionScopes;
using Rebus.Transport;

namespace Rebus.Config;

/// <summary>
/// Configuration extensions for enabling automatic execution if handlers inside <see cref="System.Transactions.TransactionScope"/>
/// </summary>
public static class TransactionScopeConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to execute handlers inside a <see cref="System.Transactions.TransactionScope"/>. Uses Rebus' default transaction
    /// options which is <see cref="System.Data.IsolationLevel.ReadCommitted"/> isolation level and 1 minut timeout.
    /// Use the <see cref="HandleMessagesInsideTransactionScope(OptionsConfigurer, System.Transactions.TransactionOptions)"/> if you
    /// want to customize the transaction settings.
    /// </summary>
    public static void HandleMessagesInsideTransactionScope(this OptionsConfigurer configurer)
    {
        var defaultTransactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromMinutes(1)
        };

        configurer.HandleMessagesInsideTransactionScope(defaultTransactionOptions);
    }

    /// <summary>
    /// Configures Rebus to execute handlers inside a <see cref="TransactionScope"/>, using the transaction options
    /// given by <paramref name="transactionOptions"/> for the transaction scope.
    /// <para>
    /// The <see cref="TransactionScope"/> is managed with an <see cref="ITransport"/> decorator,
    /// which means that it gets created before receiving the incoming message.
    /// </para>
    /// <para>
    /// <code>
    /// Configure.With(..)<para/>
    ///     .(...)<para/>
    ///     .Options(o => o.LogPipeline(verbose: true)<para/>
    ///     .Start();<para/>
    /// </code>
    /// </para>
    /// </summary>
    public static void HandleMessagesInsideTransactionScope(this OptionsConfigurer configurer, TransactionOptions transactionOptions)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Decorate<ITransport>(c =>
        {
            var transport = c.Get<ITransport>();

            return new TransactionScopeTransportDecorator(transport, transactionOptions);
        });

        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();

            return new PipelineStepInjector(pipeline)
                .OnReceive(new TransactionScopeIncomingStep(), PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
        });
    }
}