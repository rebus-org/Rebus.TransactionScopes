using System.Transactions;
using NUnit.Framework;

namespace Rebus.TransactionScopes.Tests;

[TestFixture]
public class TransactionScopeAssumptions
{
    [Test]
    public void CanDisposeTwiceWIthNoProblems()
    {
        using (var scope = new TransactionScope())
        {

            scope.Dispose();
        }
    }
}