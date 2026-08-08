using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Tests for the two interaction modes: MouseBehavior and InteractionMode.
/// Unlike the pure-logic ones, these need a real window: gestures arrive through routed
/// events and focus does not exist outside the visual tree.
/// </summary>
[TestClass]
public class InteractionModeTests
{
	// --- Scaffolding ---

	/// <summary>
	/// Mounts the control in a visible window and waits for the template to be applied.
	/// Without a window there is no PART_BarGrid and no keyboard focus, so there would be
	/// nothing to test. It includes a separate button so that the focus can be taken away from
	/// the control when a test needs the "no previous focus" scenario.
	/// </summary>
	private static Scenario Host(Action<BoundedNumericSelector>? configure = null)
	{
		var selector = new BoundedNumericSelector { Minimum = 0, Maximum = 100, Value = 50 };
		configure?.Invoke(selector);

		var other = new Button { Content = "other" };
		var panel = new StackPanel();
		panel.Children.Add(other);
		panel.Children.Add(selector);

		var window = new Window
		{
			Width = 400,
			Height = 200,
			Content = panel,
			ShowInTaskbar = false,
		};

		window.Show();
		Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
		window.UpdateLayout();

		var bar = (FrameworkElement)selector.Template.FindName("PART_BarGrid", selector);
		return new Scenario(window, selector, bar, other);
	}

	private sealed record Scenario(
		Window Window, BoundedNumericSelector Selector, FrameworkElement Bar, Button Other);

	/// <summary>
	/// A complete left click: first the tunnel phase (where the control takes the focus and
	/// notes down whether it already had it) and then the bubble one (where the handler of the
	/// bar decides). Both phases are indispensable: with the bubble alone, MustFocusFirst would
	/// never see the previous focus state and the test would be testing nothing.
	/// </summary>
	private static void LeftClick(UIElement target)
	{
		target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{ RoutedEvent = Mouse.PreviewMouseDownEvent });
		target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{ RoutedEvent = UIElement.MouseLeftButtonDownEvent });
	}

	private static void RightClick(UIElement target)
	{
		target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
		{ RoutedEvent = Mouse.PreviewMouseDownEvent });
		target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
		{ RoutedEvent = UIElement.MouseRightButtonUpEvent });
	}

	/// <summary>
	/// Finds out which value the gesture produces by RUNNING it once with the control already
	/// focused (that is, with the gesture enabled in either of the two modes), and also returns
	/// a starting value guaranteed to be a different one.
	///
	/// Why this way and not by computing the position: a simulated click resolves
	/// e.GetPosition() during the routing, and that position does NOT match the one
	/// Mouse.GetPosition() returns from outside the event (measured: -181 against 392 over the
	/// same bar). Predicting the result from outside gave flaky tests. The pointer does not
	/// move during the test, so running the gesture is the only reliable source of the
	/// expectation.
	/// </summary>
	private static (int Expected, int Different) Probe(Scenario e, Action<UIElement> gesture)
	{
		e.Selector.Focus();
		gesture(e.Bar);

		int expected = e.Selector.Value;
		int different = expected == e.Selector.Maximum ? e.Selector.Minimum : e.Selector.Maximum;

		// The control is put back into the "unfocused" state so that the test can set up its
		// own scenario.
		e.Other.Focus();
		return (expected, different);
	}

	// --- MouseBehavior ---

	[TestMethod]
	public void Interaction_defaults_leave_the_control_fully_responsive()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();

			Assert.AreEqual(MouseInteractionBehavior.ChangeOnClick, selector.MouseBehavior);
			Assert.AreEqual(UserInteractionMode.Interactive, selector.InteractionMode);
		});
	}

	[TestMethod]
	public void Change_on_click_moves_the_value_without_previous_focus()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				var (expected, different) = Probe(e, LeftClick);
				e.Selector.Value = different;
				Assert.IsFalse(e.Selector.IsKeyboardFocused, "The scenario requires starting with no focus.");

				LeftClick(e.Bar);

				Assert.AreEqual(expected, e.Selector.Value,
					"In ChangeOnClick the click has to act even if the control did not have the focus.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Must_focus_first_spends_the_first_click_on_taking_focus()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.MouseBehavior = MouseInteractionBehavior.MustFocusFirst);
			try
			{
				var (expected, different) = Probe(e, LeftClick);
				e.Selector.Value = different;

				LeftClick(e.Bar);
				Assert.AreEqual(different, e.Selector.Value,
					"The click that grants the focus must not move the value as well.");
				Assert.IsTrue(e.Selector.IsKeyboardFocused,
					"That first click does have to leave the control focused.");

				LeftClick(e.Bar);
				Assert.AreEqual(expected, e.Selector.Value,
					"With the focus already taken, the next click has to act normally.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Must_focus_first_does_not_care_where_the_focus_came_from()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.MouseBehavior = MouseInteractionBehavior.MustFocusFirst);
			try
			{
				var (expected, different) = Probe(e, LeftClick);
				e.Selector.Value = different;

				// Focus handed over from code, as a tab would do: it is not a click.
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				LeftClick(e.Bar);

				Assert.AreEqual(expected, e.Selector.Value,
					"If the focus was already taken, the first click has to act even though it did not grant it.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Must_focus_first_gates_the_right_click_zones_too()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.MouseBehavior = MouseInteractionBehavior.MustFocusFirst);
			try
			{
				var (expected, different) = Probe(e, RightClick);
				e.Selector.Value = different;

				RightClick(e.Bar);
				Assert.AreEqual(different, e.Selector.Value,
					"The rule holds for every mouse gesture, not only for the left click.");

				RightClick(e.Bar);
				Assert.AreEqual(expected, e.Selector.Value,
					"With the focus taken, the right click by zones has to act.");
			}
			finally { e.Window.Close(); }
		});
	}

	// --- InteractionMode ---

	[TestMethod]
	public void Read_only_takes_the_control_out_of_the_tab_order_and_gives_it_back()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();
			Assert.IsTrue(selector.Focusable);

			selector.InteractionMode = UserInteractionMode.ReadOnly;
			Assert.IsFalse(selector.Focusable, "In display-only mode the control must not be focusable.");

			selector.InteractionMode = UserInteractionMode.Interactive;
			Assert.IsTrue(selector.Focusable, "On leaving the mode, focusability has to come back on its own.");
		});
	}

	[TestMethod]
	public void Read_only_does_not_overrule_a_consumer_that_disabled_focus()
	{
		StaTest.Run(() =>
		{
			// Focusability is removed by coercion, not by assignment, precisely so as not to
			// override the decision of whoever uses the control.
			var selector = new BoundedNumericSelector { Focusable = false };

			selector.InteractionMode = UserInteractionMode.ReadOnly;
			selector.InteractionMode = UserInteractionMode.Interactive;

			Assert.IsFalse(selector.Focusable,
				"Leaving the mode must not switch on the focusability the consumer had switched off.");
		});
	}

	[TestMethod]
	public void Read_only_releases_the_focus_it_already_had()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				e.Selector.InteractionMode = UserInteractionMode.ReadOnly;

				// Removing Focusable does not by itself release a focus already taken: if this
				// breaks, the control is left with the focus border lit and the wheel alive.
				Assert.IsFalse(e.Selector.IsKeyboardFocused,
					"Entering display-only mode has to release a focus that was already taken.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Read_only_ignores_mouse_wheel_and_keyboard()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				// It is probed BEFORE entering the mode: we need to know which value a working
				// gesture would produce, in order to claim that afterwards it produces none.
				var (expected, different) = Probe(e, LeftClick);

				e.Selector.InteractionMode = UserInteractionMode.ReadOnly;
				e.Selector.Value = different;

				LeftClick(e.Bar);
				Assert.AreEqual(different, e.Selector.Value, "The left click must do nothing.");

				RightClick(e.Bar);
				Assert.AreEqual(different, e.Selector.Value, "The right click must do nothing.");

				e.Selector.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 120)
				{ RoutedEvent = UIElement.MouseWheelEvent });
				Assert.AreEqual(different, e.Selector.Value, "The wheel must do nothing.");

				var source = PresentationSource.FromVisual(e.Window)!;
				e.Selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
				{ RoutedEvent = Keyboard.PreviewKeyDownEvent });
				Assert.AreEqual(different, e.Selector.Value, "The keyboard must do nothing.");

				// And that the probe was not degenerate: the gesture did produce a change.
				Assert.AreNotEqual(expected, different,
					"If the gesture changed nothing to begin with, the test would not be testing the blocking.");
			}
			finally { e.Window.Close(); }
		});
	}

	// --- ShowDetail and layout ---
	// These are about appearance, but their tests live here because the rule that matters is
	// the interaction with the FOCUS, and that needs a real window.

	private static Border Cell(BoundedNumericSelector selector, string part) =>
		(Border)selector.Template.FindName(part, selector);

	private static Border DetailCell(BoundedNumericSelector selector) =>
		Cell(selector, "PART_DetailCell");

	// The two value boxes are named after the row they live in, not after "upper/lower": only
	// one of them is visible at a time, and which one it is depends on ValueFollowsDetail.
	private static Border DetailValueBox(BoundedNumericSelector selector) =>
		Cell(selector, "PART_ValueDetailCell");

	private static Border BarValueBox(BoundedNumericSelector selector) =>
		Cell(selector, "PART_ValueCell");

	[TestMethod]
	public void Detail_row_is_hidden_by_default()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				Assert.AreEqual(Visibility.Collapsed,
					((FrameworkElement)e.Selector.Template.FindName("PART_DetailRow", e.Selector)).Visibility);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Show_detail_shows_a_framed_detail_that_yields_only_the_top()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => { s.ShowDetail = true; s.BorderThickness = new Thickness(2); });
			try
			{
				Assert.AreEqual(Visibility.Visible,
					((FrameworkElement)e.Selector.Template.FindName("PART_DetailRow", e.Selector)).Visibility);
				Assert.AreEqual(e.Selector.BorderBrush, DetailCell(e.Selector).BorderBrush);

				// With the default of ValueFollowsDetail (true) the value drops: the detail is a
				// fixed frame (Left,Right,Bottom) and only yields its top side to the seam drawn
				// by the bar (which is now above it).
				Assert.AreEqual(new Thickness(2, 0, 2, 2), DetailCell(e.Selector).BorderThickness,
					"The detail row carries three sides and yields the top one to the bar.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Background_reaches_every_cell()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.ShowDetail = true;
				s.Background = Brushes.Coral;
			});
			try
			{
				// The background of the control is painted through the cells: if one of them did
				// not bind it, there would be a colorless gap right there.
				Assert.AreEqual(e.Selector.Background, DetailCell(e.Selector).Background);
				Assert.AreEqual(e.Selector.Background, BarValueBox(e.Selector).Background);
				Assert.AreEqual(e.Selector.Background, Cell(e.Selector, "PART_BarCell").Background);
				Assert.AreEqual(e.Selector.Background, DetailValueBox(e.Selector).Background);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Focus_lights_up_the_whole_outline()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.ShowDetail = true);
			try
			{
				e.Selector.Focus();

				// With ShowDetail the outline is the union of the frames, and every one of them
				// is tinted: there is no "frameless" box to turn off, so the rule is a single one.
				Assert.AreEqual(e.Selector.FocusBorderBrush, DetailCell(e.Selector).BorderBrush);
				Assert.AreEqual(e.Selector.FocusBorderBrush, DetailValueBox(e.Selector).BorderBrush);
				Assert.AreEqual(e.Selector.FocusBorderBrush, Cell(e.Selector, "PART_BarCell").BorderBrush);
				Assert.AreEqual(e.Selector.FocusBorderBrush, BarValueBox(e.Selector).BorderBrush);
			}
			finally { e.Window.Close(); }
		});
	}

	// --- The separating stroke of the value box ---

	/// <summary>
	/// The rule of the whole model: the bar is the fixed frame (four sides) and the value box
	/// yields the edge they share. If both drew it, the seam would measure twice as much.
	/// </summary>
	[TestMethod]
	public void Beside_bar_the_bar_draws_the_shared_edge()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.BorderThickness = new Thickness(2));
			try
			{
				Assert.AreEqual(new Thickness(0, 2, 2, 2), BarValueBox(e.Selector).BorderThickness,
					"The value box yields the side facing the bar.");
				Assert.AreEqual(new Thickness(2), Cell(e.Selector, "PART_BarCell").BorderThickness,
					"The bar, being the fixed frame, draws the shared edge.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Left_side_moves_the_box_and_the_bar_draws_the_shared_edge()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.BorderThickness = new Thickness(2);
				s.ValueBoxDock = ValueBoxDock.Left;
			});
			try
			{
				Assert.AreEqual(new Thickness(2, 2, 0, 2), BarValueBox(e.Selector).BorderThickness,
					"The value box yields its right side, which is the one facing the bar.");
				Assert.AreEqual(new Thickness(2), Cell(e.Selector, "PART_BarCell").BorderThickness,
					"The bar, being the fixed frame, draws the shared edge.");
				Assert.AreEqual(1, Grid.GetColumn(Cell(e.Selector, "PART_BarCell")),
					"The bar moves to the second column; the value box comes first.");

				// The "*" column has to go along with the bar: if only the cells moved, the bar
				// would land in an "Auto" column and shrink to its content. The columns are named
				// by position, not by occupant, precisely because here the occupant changes: the
				// bar moves to 1 and the value box to 0.
				Assert.AreEqual(GridLength.Auto,
					((ColumnDefinition)e.Selector.Template.FindName("PART_Column0", e.Selector)).Width);
				Assert.AreEqual(new GridLength(1, GridUnitType.Star),
					((ColumnDefinition)e.Selector.Template.FindName("PART_Column1", e.Selector)).Width);
			}
			finally { e.Window.Close(); }
		});
	}

	/// <summary>
	/// Value in the detail row (ShowDetail and ValueFollowsDetail): the box drops to the detail
	/// row, yields its top side to the seam drawn by the bar above, and yields the side facing
	/// the detail label, which draws it.
	/// </summary>
	[TestMethod]
	public void Value_down_moves_the_box_to_the_detail_row_and_yields_the_shared_edges()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.BorderThickness = new Thickness(2);
				s.ShowDetail = true;
				s.ValueFollowsDetail = true;
			});
			try
			{
				Assert.AreEqual(new Thickness(0, 0, 2, 2), DetailValueBox(e.Selector).BorderThickness,
					"The detail value box yields the top one to the bar and the side facing the label.");
				Assert.AreEqual(new Thickness(2, 0, 2, 2), DetailCell(e.Selector).BorderThickness,
					"The detail row, being a fixed frame, draws the shared edge and yields the top one.");
				Assert.AreEqual(new Thickness(2), Cell(e.Selector, "PART_BarCell").BorderThickness,
					"The bar, the base frame on top, keeps its four sides.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void The_separator_disappears_when_the_value_column_collapses()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => { s.ShowDetail = true; s.ValueFollowsDetail = true; });
			try
			{
				// With the value down below, the upper value column measures 0: without turning
				// its frame off there would be a loose vertical stroke stuck to the end of the bar.
				Assert.AreEqual(new Thickness(0), BarValueBox(e.Selector).BorderThickness,
					"The upper box, being empty, must leave no separating stroke.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void The_separator_follows_the_focus_colour()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				Assert.AreEqual(e.Selector.BorderBrush, BarValueBox(e.Selector).BorderBrush);

				e.Selector.Focus();

				// It is part of the same frame: if it were not tinted, focusing would show a blue
				// box with a black line down the middle.
				Assert.AreEqual(e.Selector.FocusBorderBrush, BarValueBox(e.Selector).BorderBrush);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Show_detail_without_following_keeps_the_value_beside_the_bar()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.BorderThickness = new Thickness(2);
				s.ShowDetail = true;
				s.ValueFollowsDetail = false;
			});
			try
			{
				// The box stays up top, next to the bar, and the detail row spans the whole width
				// below the bar.
				Assert.AreEqual(new Thickness(2, 0, 2, 2), DetailCell(e.Selector).BorderThickness);
				Assert.AreEqual(new Thickness(0, 2, 2, 2), BarValueBox(e.Selector).BorderThickness);
				Assert.AreEqual(new Thickness(2), Cell(e.Selector, "PART_BarCell").BorderThickness);
			}
			finally { e.Window.Close(); }
		});
	}

	// --- IsEnabled ---
	// IsEnabled is not a property of the control but of UIElement, yet the consumer can use it
	// all the same and the control has to behave. WPF prevents GAINING the focus while
	// disabled, but it does not release one already taken, and keys are routed to the focused
	// element: with no treatment, a disabled control went on responding.

	[TestMethod]
	public void Disabling_releases_the_focus_it_already_had()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				e.Selector.IsEnabled = false;

				// If this breaks, the disabled control is left with the focus frame lit —which is
				// a lie on top of everything— and with the keyboard and the wheel alive.
				Assert.IsFalse(e.Selector.IsKeyboardFocused,
					"Disabling has to release a focus that was already taken.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Disabled_control_ignores_the_keyboard()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				e.Selector.IsEnabled = false;

				int before = e.Selector.Value;
				var source = PresentationSource.FromVisual(e.Window)!;
				e.Selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
				{ RoutedEvent = Keyboard.PreviewKeyDownEvent });

				// The event is raised directly on the control to skip the routing: that way the
				// test verifies the GUARD, not that WPF failed to deliver the key. Both defences
				// matter and this is the one left standing if the focus arrives some other way.
				Assert.AreEqual(before, e.Selector.Value,
					"With the control disabled, the keyboard must not move the value.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Re_enabling_leaves_the_keyboard_working_again()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.IsEnabled = false;
				e.Selector.IsEnabled = true;
				e.Selector.Focus();

				int before = e.Selector.Value;
				var source = PresentationSource.FromVisual(e.Window)!;
				e.Selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
				{ RoutedEvent = Keyboard.PreviewKeyDownEvent });

				// The blocking must leave no after-effects: the guard looks at IsEnabled live and
				// the focus can be taken again.
				Assert.AreEqual(before + e.Selector.SmallChange, e.Selector.Value,
					"On re-enabling, the keyboard has to work again.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Read_only_blocks_the_user_but_not_the_program()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { InteractionMode = UserInteractionMode.ReadOnly };
			var changes = new List<(int OldValue, int NewValue)>();
			selector.ValueChanged += (_, args) => changes.Add((args.OldValue, args.NewValue));

			// The point of the mode is to show a value that updates by itself: assigning it from
			// code has to keep working and keep announcing the change.
			selector.Value = 77;

			Assert.AreEqual(77, selector.Value);
			CollectionAssert.AreEqual(new List<(int OldValue, int NewValue)> { (0, 77) }, changes);
		});
	}
}
