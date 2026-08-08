using System.Runtime.ExceptionServices;

namespace NumericSelector.Tests;

/// <summary>
/// Runs tests that create WPF controls on an isolated STA thread.
/// That way the tests do not depend on the apartment model of the test runner.
/// </summary>
internal static class StaTest
{
	public static void Run(Action action) => Run(() =>
	{
		action();
		return true;
	});

	public static T Run<T>(Func<T> action)
	{
		T? result = default;
		Exception? failure = null;

		var thread = new Thread(() =>
		{
			try
			{
				result = action();
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();

		if (failure is not null)
			ExceptionDispatchInfo.Capture(failure).Throw();

		return result!;
	}
}
