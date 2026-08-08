# NumericSelector — Agent guide

*Lookless* WPF control for picking an integer value bounded to a range. The control is called `BoundedNumericSelector` and lives in the `NumericSelector` assembly.

## Repository structure

```
NumericSelector/          → Control library (main project)
NumericSelector.Demo/     → WPF demonstration application (manual test bench)
                            MainWindow: the Master on top and the selectors that drive it
                            HelpWindow: the help (F1), plain text, does not use the control
NumericSelector.Tests/    → Automated tests (MSTest)
docs/images/              → Screenshot and animation illustrating the README
docs/render-animation.ps1 → regenerates that animation (needs Release + ffmpeg)
.github/workflows/ci.yml       → CI: build and test on a Windows runner
.github/workflows/release.yml  → publishes to nuget.org when a v* tag is pushed
```

Published at <https://github.com/musikante/NumericSelector> (public, MIT). The remote is
`origin` and the default branch is `main`.

## Essential commands

```powershell
# Build the whole solution. Release is THE verification build: it is run AFTER EVERY CHANGE,
# not just before a PR, and the standard is 0 errors and 0 warnings.
dotnet build .\NumericSelector.slnx --configuration Release

# Run the demonstration application
dotnet run --project .\NumericSelector.Demo\NumericSelector.Demo.csproj

# Run every test
dotnet test .\NumericSelector.slnx

# Run only one project's tests
dotnet test .\NumericSelector.Tests\NumericSelector.Tests.csproj
```

## Stack and configuration

- **SDK**: .NET 10.0.302 (`global.json`, rollForward: latestFeature)
- **Target**: `net10.0-windows` with `UseWPF=true`
- **Platform**: Windows only (WPF is not cross-platform)
- **Tests**: MSTest 4.3.2 + Microsoft.NET.Test.Sdk 18.8.1
- **Solution**: `NumericSelector.slnx` (new SDK-style format)

## Control architecture

### Main source files

| File | Contents |
|---------|-----------|
| `BoundedNumericSelector.cs` | Interaction logic, measurement, cursors, event handling |
| `BoundedNumericSelector.Dependencies.cs` | Dependency properties, coercions, ValueChanged event |
| `Themes/Generic.xaml` | Default template (style, triggers, parts) |
| `Converters.cs` | `ValueBorderResolver` (the seam matrix): a pure function |
| `ValueBoxDock.cs`, `MouseInteractionBehavior.cs`, `UserInteractionMode.cs` | Public API enums |

### Cell model

The control is **four sibling cells**, each with its own frame:
- The bar, with its `MainText` drawn on top (upper row, always present)
- The value box next to the bar (upper row, on the right — or on the left with `ValueBoxDock=Left`)
- The detail label (bottom row, only with `ShowDetail`)
- The value box in the detail row (bottom row, when `ValueFollowsDetail` makes it drop)

The value box is declared in both rows, but only one is visible at a time: `ShowDetail && ValueFollowsDetail` collapses the upper one and shows the lower one.

**Single rule**: the bar and the detail are the fixed frame (the bar carries its four sides, the detail gives up the top one) and the value box gives up the side facing its row partner. No edge is drawn twice. The actual split is computed by `ValueBorderResolver.Resolve`, a pure function holding the complete seam matrix (see its `Converters.cs`).

### Template parts (PART_)

The code-behind resolves **four** of them by name, in `OnApplyTemplate`: `PART_BarGrid`, `PART_BarRect`, `PART_ValueText` and `PART_ValueDetailText`. Those four —and only those— are declared on the class with `[TemplatePart]`: that attribute is the contract with a replacement template, and the rest of the names exist so that the triggers of the default template can point at them with `TargetName`; nobody looks them up from C#.

`TemplateContractTests` reads those attributes by reflection and checks the default template provides each part with the declared type, so renaming one in the XAML —which compiles, and fails in silence— shows up as a failing test.

Parts the template defines:
- `PART_RootGrid` (the root of the template; it is named that way and not `PART_MainGrid` so as not to be confused with `PART_MainText`, which is something else), `PART_DetailRow`
- `PART_BarAndValueGrid`, `PART_BarCell`, `PART_BarGrid`, `PART_BarRect`, `PART_BarRowDef`
- `PART_MainText`, `PART_ValueCell`, `PART_ValueText`, `PART_ValueSizerMin/Max`
- The detail row, with its own value box: `PART_DetailCell`, `PART_DetailText`, `PART_ValueDetailCell`, `PART_ValueDetailText`, `PART_DetailSizerMin/Max`
- The columns are named **by position**, not by occupant, because with `ValueBoxDock=Left` the cells swap: `PART_Column0`/`PART_Column1` (bar row) and `PART_DetailColumn0`/`PART_DetailColumn1` (detail row)

**They must not be renamed**: the `PART_*` names are a public contract.

### Dependency properties and coercions

The coercions are silent and mutually restrictive:

| Property | Coercion |
|-----------|----------|
| `Minimum` | ≤ `Maximum - 1` (the range always has at least 1 of width) |
| `Maximum` | ≥ `Minimum + 1` |
| `Value` | Bounded to `[Minimum, Maximum]` |
| `ResetValue` | Bounded to `[Minimum, Maximum]` |
| `SmallChange`, `LargeChange` | Between `1` and the width of the range |
| `Focusable` | `false` when `InteractionMode=ReadOnly` (by coercion, not assignment) |

The three enum properties (`ValueBoxDock`, `MouseBehavior`, `InteractionMode`) are the exception: they are not coerced but **validated** with a `ValidateValueCallback` (`IsDefinedEnumValue<TEnum>`). An undefined value is not a value to be adjusted, it is one that does not exist, so WPF throws an `ArgumentException` and the property keeps what it had.

### Culture and formatting

- The control uses `Language` (not the system culture) to format numbers
- By default it assigns `CultureInfo.CurrentCulture.IetfLanguageTag`
- The width of the value box is reserved with **hidden sizers** in the template (two `TextBlock`s with `Minimum` and `Maximum` in the same cell as the value); WPF takes the largest and the cell never ends up narrower than the longest number in the range, according to the culture

### Interaction modes

- `MouseInteractionBehavior.ChangeOnClick` (default): the mouse always acts
- `MouseInteractionBehavior.MustFocusFirst`: the mouse acts only with focus already taken; the click that focuses does not change the value
- `InteractionMode = ReadOnly`: blocks mouse, keyboard and tabbing; keeps the appearance; changes from code keep working

Keyboard: `←`/`↓`/`-` and `→`/`↑`/`+` subtract or add `SmallChange`; `PageUp`/`PageDown` by `LargeChange`; `Home`/`End` to the ends; `Delete`/`Insert` to `ResetValue`. Both the main-row keys (`Key.OemPlus`/`OemMinus`) and the numeric-pad ones (`Key.Add`/`Subtract`) are handled.

### Width: `BaseWidth` (floor) and fitting the container

- `BaseWidth` (`double`, default `NaN` = automatic) is the base width the control grows from; it is **not** a hard constraint: it is read as a floor in `MeasureOverride`, which asks for `max(BaseWidth, content)` but **never wider than the available slot** (the CharacterEllipsis on `MainText` and `DetailText` truncates whatever does not fit)
- The control **does not write** `FrameworkElement.Width`/`MinWidth` at runtime: doing so forced the element to that size and overflowed/clipped the borders in a narrow container (it was the cause of the frame clipping). The floor for the number is given by the template's hidden sizers
- The natural width comes from `base.MeasureOverride(new Size(infinity, height))`; the bar lives in a `*` column, which with infinite width behaves like `Auto` (it does not stretch to the whole container)
- There is no `Viewbox` in the template: it would scale the typography instead of truncating with an ellipsis, which is the requested behavior
- A consumer who wants to force a minimum width uses `BaseWidth`, not `Width`/`MinWidth` (those are hard and bring the clipping back)

## Tests

### Kinds of tests

1. **Pure logic** (`BoundedNumericSelectorLogicTests.cs`, `ValueBorderResolverTests.cs`): they need no window; they validate defaults, coercions, steps, layout, enum validation, ValueChanged and the seam matrix
2. **Interaction** (`InteractionModeTests.cs`): they need a real window (STA + Dispatcher) because gestures arrive through routed events and focus does not exist outside the visual tree
3. **Template contract** (`TemplateContractTests.cs`): it needs a window too, because the parts only exist once the template is applied

### The `StaTest` helper

Tests that create WPF controls use `StaTest.Run()` to execute on an isolated STA thread, regardless of the runner's apartment model.

### Current coverage

- Defaults, range coercions, steps, layout, ValueChanged
- The value of the seam matrix (`ValueBorderResolver.Resolve`) in every configuration
- Layout (`ShowDetail`, `ValueFollowsDetail`, `ValueBoxDock`) and the value's separating stroke
- Measurement and `BaseWidth` (`MeasureAndBaseWidthTests.cs`): the requested width never exceeds a fixed slot, and `BaseWidth` acts as a floor with infinite space
- MouseBehavior (ChangeOnClick, MustFocusFirst)
- InteractionMode (focusable, focus release, input blocking)
- IsEnabled (focus release, keyboard blocking)
- Validation of the public enums (an undefined value is rejected and the property keeps what it had)
- Template contract ([TemplatePart] against the default template)

### Pending

- Culture and number-formatting tests (the rest —focus, mouse gestures, growth— is already covered)

## Project conventions

- **Everything in English**: identifiers, XAML resource keys, comments, documentation, interface texts and commit messages. The migration is **finished** — if anything in Spanish turns up, it is an oversight, not a leftover of a pending stage
- Clear names, comments only where they explain a non-obvious decision
- Formatting consistent with the existing code
- One fix/improvement per PR
- Update `README.md` and `CHANGELOG.md` if the public API changes
- CI runs the same three commands as above on a Windows runner, and adds `-warnaserror` to the build: a warning turns it red

## Warnings

- The domain is **deliberately integer** (no decimal support)
- `FocusVisualStyle` is turned off by default; focus is signalled by the border (`FocusBorderBrush`)
- **A misspelled `TemplateBinding` still compiles and fails silently.** When touching the template or renaming properties it consumes, the control has to be instantiated and its parts read with `Template.FindName` to check that the values arrive. The same goes for the demo's `StaticResource`s: a deleted resource blows up when **loading** the window, not when compiling.

## Pending work

Everything that used to be listed here is done: the text inconsistencies, the five code
cleanups, the English migration, the `[TemplatePart]` contract, the validation of the public
enums and the publication itself.

### Released

- **GitHub:** <https://github.com/musikante/NumericSelector>, public, with CI. Commits are made
  locally as the work goes; **pushing is the author's call**, not something to do unprompted.
- **NuGet:** `NumericSelector` `0.1.0`, owner `musikante`. The first version was uploaded by
  hand through *Upload Package* on the website.

### Releasing a version

`release.yml` publishes to nuget.org with **trusted publishing**: no API key is stored
anywhere. GitHub issues a short-lived OIDC token for the job, nuget.org checks it against a
policy naming this repository and that file, and returns a key that lasts one hour.

The steps for a release:

1. Bump `<Version>` in `NumericSelector/NumericSelector.csproj` and write the CHANGELOG entry.
2. Commit and push.
3. `git tag v0.1.1 && git push origin v0.1.1` — the tag is the trigger.

The version is **not** taken from the tag: it stays in the `.csproj`, and the job refuses to
publish if the two disagree. A published version **can never be replaced or deleted** and its
number can never be reused, so a mistake in `0.1.0` is fixed by releasing `0.1.1`, not by
retrying.

**If this file is ever renamed, the policy on nuget.org has to be updated**: it names the
workflow file, and the exchange fails without a match.

### What is left

- The **trusted publishing policy** on nuget.org (repository owner `musikante`, repository
  `NumericSelector`, workflow file `release.yml`, no environment). Until it exists, the release
  job fails at the login step. It is the author's to create; nothing in the repo can do it.
Nothing else is open. `release.yml` has already published `0.1.1` end to end, and the README
animation is generated by `docs/render-animation.ps1`, which hosts the real control off-screen
and renders it frame by frame — **it is not a screen recording**, so it cannot drift away from
the behaviour the control actually has, and it has to be re-run whenever the look changes.
