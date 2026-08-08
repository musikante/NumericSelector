using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Tests for the pure function of the seam matrix (ValueBorderResolver.Resolve).
/// They need no window: the matrix is a stateless function and that is exactly the point of
/// isolating it — every configuration of (ShowDetail, ValueFollowsDetail, ValueBoxDock) must
/// hand out the sides of the control's cells without any edge being drawn twice.
/// </summary>
[TestClass]
public class ValueBorderResolverTests
{
	private static Thickness Resolve(
		Thickness pixels,
		bool showDetail,
		bool followsDetail,
		ValueBoxDock side,
		string cell)
		=> ValueBorderResolver.Resolve(pixels, showDetail, followsDetail, side, cell);

	// --- Fixed frame: the bar (on top) does not depend on the layout ---

	[TestMethod]
	public void The_bar_always_draws_its_four_sides()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(pixels, Resolve(pixels, false, false, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, false, true, ValueBoxDock.Left, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, true, false, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, true, true, ValueBoxDock.Left, "Bar"));
	}

	// --- The detail row (at the bottom) yields its top border to the bar ---

	[TestMethod]
	public void The_detail_row_yields_its_top_edge_to_the_bar()
	{
		// The bar is above and draws the seam: the detail does not keep its top side.
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, false, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Left, "Detail"));
	}

	// --- Value next to the bar (it does not drop) ---

	[TestMethod]
	public void Docked_right_the_value_yields_the_shared_edge_to_the_bar()
	{
		var pixels = new Thickness(2);

		// It does not drop (down=false): it keeps its top side. The bar draws the shared edge.
		Assert.AreEqual(new Thickness(0, 2, 2, 2), Resolve(pixels, false, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, false, true, ValueBoxDock.Right, "Bar"));
	}

	[TestMethod]
	public void Docked_left_the_value_yields_the_shared_edge_to_the_bar()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 2, 0, 2), Resolve(pixels, false, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, false, true, ValueBoxDock.Left, "Bar"));
	}

	// --- Value in the detail row (it drops: ShowDetail && ValueFollowsDetail) ---

	[TestMethod]
	public void Value_in_the_detail_row_yields_its_top_and_its_inner_side()
	{
		var pixels = new Thickness(2);

		// It drops to the detail row: it yields the top side (drawn by the bar above) and the
		// side facing the detail label.
		Assert.AreEqual(new Thickness(0, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, true, true, ValueBoxDock.Right, "Bar"));
	}

	[TestMethod]
	public void Value_in_the_detail_row_docked_left_yields_its_top_and_its_inner_side()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 0, 0, 2), Resolve(pixels, true, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, true, true, ValueBoxDock.Left, "Bar"));
	}

	[TestMethod]
	public void The_detail_row_draws_the_shared_side()
	{
		var pixels = new Thickness(2);

		// The detail row is a fixed frame: it carries left/right and the bottom, and yields the
		// top one.
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Left, "Detail"));
	}

	// --- The sides are yielded one at a time, without merging the per-side thickness ---

	[TestMethod]
	public void An_asymmetric_thickness_is_carried_side_by_side()
	{
		var pixels = new Thickness(1, 2, 3, 4);

		Assert.AreEqual(pixels, Resolve(pixels, false, true, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, false, true, ValueBoxDock.Left, "Bar"));
		Assert.AreEqual(new Thickness(0, 2, 3, 4), Resolve(pixels, false, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 2, 0, 4), Resolve(pixels, false, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(1, 0, 3, 4), Resolve(pixels, true, true, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(0, 0, 3, 4), Resolve(pixels, true, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 0, 0, 4), Resolve(pixels, true, true, ValueBoxDock.Left, "Value"));
	}

	// --- A cell that does not exist is an error, not a plausible value ---

	[TestMethod]
	public void An_unknown_cell_is_not_taken_for_a_valid_one()
	{
		// The name of the cell is a string typed by hand in the template's ConverterParameter.
		// If a typo returned the Thickness of another cell, the frame would come out drawn
		// wrong with no warning at all; that is why it complains.
		Assert.ThrowsExactly<ArgumentException>(
			() => Resolve(new Thickness(2), true, true, ValueBoxDock.Right, "Detai"));

		// Nor is the empty string: that is what arrives if the ConverterParameter is missing.
		Assert.ThrowsExactly<ArgumentException>(
			() => Resolve(new Thickness(2), false, false, ValueBoxDock.Right, ""));
	}
}
