using System.Runtime.ExceptionServices;

namespace NumericSelectorLib.Tests;

/// <summary>
/// Ejecuta pruebas que crean controles WPF en un hilo STA aislado.
/// Así las pruebas no dependen del modelo de apartamento del runner de pruebas.
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
