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

			// Layout API after the redesign.
			Assert.IsFalse(selector.ShowDetail);
			Assert.IsTrue(selector.ValueFollowsDetail);
			Assert.AreEqual(ValueBoxDock.Right, selector.ValueBoxDock);
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

	// A cast gets an undefined value past the compiler —(ValueBoxDock)99 compiles— and before
	// the ValidateValueCallback it went straight in: no comparison matched it and the control
	// drew as if the default had been asked for, hiding the mistake.
	[TestMethod]
	public void Undefined_enum_values_are_rejected_instead_of_being_stored()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();

			Assert.ThrowsExactly<ArgumentException>(() => selector.ValueBoxDock = (ValueBoxDock)99);
			Assert.ThrowsExactly<ArgumentException>(() => selector.MouseBehavior = (MouseInteractionBehavior)99);
			Assert.ThrowsExactly<ArgumentException>(() => selector.InteractionMode = (UserInteractionMode)99);

			// The rejection leaves the property with what it had; it does not fall back to the
			// default, because the assignment never happened.
			Assert.AreEqual(ValueBoxDock.Right, selector.ValueBoxDock);
			Assert.AreEqual(MouseInteractionBehavior.ChangeOnClick, selector.MouseBehavior);
			Assert.AreEqual(UserInteractionMode.Interactive, selector.InteractionMode);
		});
	}

	[TestMethod]
	public void Defined_enum_values_are_still_accepted()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector
			{
				ValueBoxDock = ValueBoxDock.Left,
				MouseBehavior = MouseInteractionBehavior.MustFocusFirst,
				InteractionMode = UserInteractionMode.ReadOnly,
			};

			Assert.AreEqual(ValueBoxDock.Left, selector.ValueBoxDock);
			Assert.AreEqual(MouseInteractionBehavior.MustFocusFirst, selector.MouseBehavior);
			Assert.AreEqual(UserInteractionMode.ReadOnly, selector.InteractionMode);
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
