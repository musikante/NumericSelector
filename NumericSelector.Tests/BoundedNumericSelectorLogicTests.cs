using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

[TestClass]
public class BoundedNumericSelectorLogicTests
{
	[TestMethod]
	public void Defaults_match_the_documented_public_contract()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();

			Assert.AreEqual(0, selector.Minimum);
			Assert.AreEqual(100, selector.Maximum);
			Assert.AreEqual(0, selector.Value);
			Assert.AreEqual(50, selector.ResetValue);
			Assert.AreEqual(1, selector.SmallChange);
			Assert.AreEqual(10, selector.LargeChange);
		});
	}

	[TestMethod]
	public void Value_and_reset_value_are_clamped_to_the_active_range()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { Minimum = 10, Maximum = 20 };

			selector.Value = -100;
			selector.ResetValue = 500;
			Assert.AreEqual(10, selector.Value);
			Assert.AreEqual(20, selector.ResetValue);

			selector.Value = 15;
			selector.Maximum = 12;
			Assert.AreEqual(12, selector.Value);
			Assert.AreEqual(12, selector.ResetValue);
		});
	}

	[TestMethod]
	public void Range_always_preserves_at_least_one_unit_of_span()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { Minimum = 10, Maximum = 20 };

			selector.Maximum = 10;
			Assert.AreEqual(11, selector.Maximum);

			selector.Minimum = 11;
			Assert.AreEqual(10, selector.Minimum);
		});
	}

	[TestMethod]
	public void Step_sizes_are_limited_to_the_current_range_span()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { Minimum = -2, Maximum = 3 };

			selector.SmallChange = 100;
			selector.LargeChange = 100;

			Assert.AreEqual(5, selector.SmallChange);
			Assert.AreEqual(5, selector.LargeChange);
		});
	}

	[TestMethod]
	public void WithTitle_is_deferred_until_the_title_row_is_visible_and_then_restored()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();

			selector.ValuePlacement = ValuePlacement.WithTitleFramed;
			Assert.AreEqual(ValuePlacement.BesideBar, selector.ValuePlacement);

			selector.TitleMode = TitleMode.Framed;
			Assert.AreEqual(ValuePlacement.WithTitleFramed, selector.ValuePlacement);

			selector.TitleMode = TitleMode.Hidden;
			Assert.AreEqual(ValuePlacement.BesideBar, selector.ValuePlacement);
		});
	}

	[TestMethod]
	public void ValueChanged_reports_only_effective_value_changes()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();
			var changes = new List<(int OldValue, int NewValue)>();
			selector.ValueChanged += (_, args) => changes.Add((args.OldValue, args.NewValue));

			selector.Value = 25;
			selector.Value = 25;
			selector.Value = 250;

			CollectionAssert.AreEqual(
				new List<(int OldValue, int NewValue)> { (0, 25), (25, 100) },
				changes);
		});
	}
}
