# Changelog

This project follows the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format and, once versions are published, will apply [semantic versioning](https://semver.org/).

## [Unreleased] — first version

Not published yet; when it is, this becomes `0.1.0`. Since none of this ever reached a consumer, the earlier development is not recorded here: the API was renamed several times before it settled, and that road is of no use to anyone picking up the control now. **The full history is in the commits.**

### Added

**The control.** `BoundedNumericSelector`, a *lookless* WPF control for discrete, **bounded** numeric input: there is no way for it to hand out a value outside `[Minimum, Maximum]`, so the consumer never has to validate the input. It lives in the `NumericSelector` assembly, for .NET 10 on Windows.

**Values and range.** `Value`, `Minimum`, `Maximum`, `SmallChange`, `LargeChange` and `ResetValue`, all `int` and all silently coerced:

- The range always keeps at least one unit of width. When one end is pushed against the other, that end stops instead of dragging its counterpart along; if they are separated again, it recovers the value that had been asked for.
- `Value` and `ResetValue` are bounded to the range; the steps, to between `1` and the width of the range.
- `Value` binds two-way by default. The **`ValueChanged`** event reports the old and the new value.

**Text and layout.** `MainText` is drawn over the bar; `DetailText` takes an optional bottom row. Three independent properties decide where the value box goes, with no invalid combinations and no degradations: `ShowDetail`, `ValueFollowsDetail` and `ValueBoxDock` (`Right` | `Left`).

**Four-cell frame.** The template does not nest sections: they are four sibling cells with their own frame, so no border stacks on another and doubles its thickness. Which sides each one draws is resolved by a single pure function (`ValueBorderResolver.Resolve`), so that no edge is drawn twice. The horizontal seam is always contributed by the bottom border of the bar, which also closes the control at the bottom when there is no detail.

**Guaranteed readability of the number.** The width of the value box is reserved by the template's **hidden sizers** (one `TextBlock` with the `Minimum` and another with the `Maximum`, hidden but still taking up room): the cell is never measured narrower than the longest number the range can produce, without depending on the current value and without flinching when going from 99 to 100. `MainText` may be truncated with an ellipsis; the number may not.

**Width: `BaseWidth`.** It is the base width the control grows from and at the same time a floor, not a hard constraint: it asks for `max(BaseWidth, content)` but never more than the slot its container gives it, so the borders are neither clipped nor overflowed. The control does not write `Width`/`MinWidth` at runtime.

**Culture.** Number formatting comes from `FrameworkElement.Language`, not from the thread culture. The control picks up the one of the system it runs on by itself, and the consumer can override it by assigning `Language` on the instance.

**Interaction.**

- *Mouse:* click and drag on the bar take the value to the pointer position; right click by zones (left 30 % → `Minimum`, middle 40 % → `ResetValue`, right 30 % → `Maximum`); double click on the number restores it; vertical drag on the number moves it by `SmallChange`.
- *Wheel:* by `SmallChange`, **only with the focus taken** and without marking the event as handled at the ends. Both precautions keep a selector inside a `ScrollViewer` from swallowing the list's scrolling or changing values as the mouse passes over it.
- *Keyboard:* arrows and `+`/`-` (main row and numeric pad) by `SmallChange`; `PageUp`/`PageDown` by `LargeChange`; `Home`/`End` to the ends; `Delete`/`Insert` to `ResetValue`.
- The cursor tells the truth about whether the gesture is going to do anything.

**Modes.** `MouseBehavior` (`ChangeOnClick` | `MustFocusFirst`) decides whether the mouse acts right away or demands focus first, in which case the click that takes the focus leaves the value alone. `InteractionMode` (`Interactive` | `ReadOnly`) blocks the user without altering the appearance and without taking the control out of the visual tree; assigning `Value` from code keeps working. `IsEnabled = false` also releases the focus if the control had it.

**Demonstration application.** `NumericSelector.Demo` is where the whole API —range, texts, layout, brushes, fonts and gestures— can be tried by hand, using the control itself as the selector for its own options. **F1** opens a help window explaining that criterion and listing the mouse and keyboard gestures.

**Tests.** 39 MSTest tests covering defaults, coercions, layout, the seam matrix, measurement and `BaseWidth`, and the interaction modes. The ones that need a window run on an isolated STA thread.

**Documentation and infrastructure.** `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `AGENTS.md`, an MIT license and Git exclusion rules. `.gitattributes` normalizes line endings (LF in the repository, each platform's own on the working disk) so that a contributor on another system does not produce whole-file differences.

### Known limits

- The domain is **deliberately integer**: there is no decimal support.
- Invalid values of the public enumerations are not validated yet.
- The template contract declared with `TemplatePart` is still missing.
