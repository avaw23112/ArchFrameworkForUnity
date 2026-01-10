using Arch.Tools;
using MethodBoundaryAspect.Fody.Attributes;
using System.Transactions;

namespace Codes.Model.Ｗeavertest
{
	internal class TransactionScopeAttribute : OnMethodBoundaryAspect
	{
		public override void OnEntry(MethodExecutionArgs args)
		{
			args.MethodExecutionTag = new TransactionScope();
		}

		public override void OnExit(MethodExecutionArgs args)
		{
			var transactionScope = (TransactionScope)args.MethodExecutionTag;

			transactionScope.Complete();
			transactionScope.Dispose();
		}

		public override void OnException(MethodExecutionArgs args)
		{
			var transactionScope = (TransactionScope)args.MethodExecutionTag;

			transactionScope.Dispose();
		}
	}

	public class Sample
	{
		[TransactionScope]
		public void Method()
		{
			ArchLog.LogInfo("Do some database stuff isolated in surrounding transaction");
		}
	}
}