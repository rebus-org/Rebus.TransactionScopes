using System;
using System.Transactions;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.TransactionScopes;

namespace Rebus.Config
{
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
        /// given by <paramref name="transactionOptions"/> for the transaction scope. If <paramref name="injectBeforeStepType"/> is
        /// defined, it will be used as the anchor step type for injection in the incoming step pipleine. The default is <see cref="DispatchIncomingMessageStep" />
        /// if it is not defined.
        /// </summary>
        public static void HandleMessagesInsideTransactionScope(this OptionsConfigurer configurer, TransactionOptions transactionOptions, Type injectBeforeStepType = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            injectBeforeStepType = injectBeforeStepType ?? typeof(DispatchIncomingMessageStep);

            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var stepToInject = new TransactionScopeIncomingStep(transactionOptions);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(stepToInject, PipelineRelativePosition.Before, injectBeforeStepType);
            });
        }
    }
}