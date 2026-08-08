using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

[TestClass]
public class MeasureAndBaseWidthTests
{
	// Runs the full layout once so that the control's Template is applied (without a template,
	// MeasureOverride of a Control measures 0 and the tests would be vacuous).
	private static void ShowWindow(UIElement content)
	{
		var window = new Window { Width = 300, Height = 200, Content = content, ShowInTaskbar = false };
		window.Show();
		Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
		window.UpdateLayout();
	}

	// The control must not report more width than the container offers it: the natural width
	// of the main text can be huge, but if there are only 120 pixels the control measures to
	// 120 (and the template's CharacterEllipsis truncates the text, instead of overflowing).
	[TestMethod]
	public void DesiredWidth_never_exceeds_constrained_width()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector
			{
				Minimum = 0, Maximum = 100000, Value = 50000,
				ShowDetail = true,
				MainText = "A very long main text that by no means fits in a container one hundred and twenty pixels wide",
				DetailText = "Another fairly extensive detail, to make sure the content exceeds the available room by a lot",
			};

			ShowWindow(selector);

			// Direct measurement with a fixed width of 120: what the DesiredSize reports must
			// not exceed it, that is, it never asks for more than what is available (it does
			// not overflow).
			selector.Measure(new Size(120, double.PositiveInfinity));

			Assert.IsTrue(selector.DesiredSize.Width <= 120,
				$"DesiredWidth {selector.DesiredSize.Width} exceeds the slot of 120");
		});
	}

	// BaseWidth is a floor: with plenty of room the control keeps at least that width, even if
	// the natural content is narrower. With infinite room nothing is clipped.
	[TestMethod]
	public void BaseWidth_acts_as_floor_when_there_is_room()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector
			{
				BaseWidth = 300,
				Minimum = 0, Maximum = 100, Value = 50,
				MainText = "Short",
			};

			ShowWindow(selector);

			// Measured with infinite width (e.g. inside a wide StackPanel).
			selector.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			Assert.IsTrue(selector.DesiredSize.Width >= 300,
				$"DesiredWidth {selector.DesiredSize.Width} fell below the BaseWidth floor of 300");
		});
	}
}
