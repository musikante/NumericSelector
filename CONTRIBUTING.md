# Contributing to NumericSelector

Thanks for wanting to improve NumericSelector. The project is under active development, and contributions that keep the control predictable, accessible and easy to customize are welcome.

## Before you start

- Check the open issues and avoid duplicating work.
- For large changes to the API, the behavior or the template, open a proposal issue first.
- Keep changes narrow: one fix, one improvement or one design decision per pull request.

## Development environment

You need Windows, the SDK stated in `global.json` and WPF support.

```powershell
dotnet build .\NumericSelector.slnx --configuration Release
dotnet run --project .\NumericSelector.Demo\NumericSelector.Demo.csproj
```

The `NumericSelector.Demo` application is the manual test bench for the control's experience.

## Criteria for a pull request

- The solution must build in `Release` with no new warnings.
- Add or update automated tests whenever the change alters verifiable behavior.
- Manually try the affected gestures in the demo: mouse, wheel, keyboard, focus and `InteractionMode = ReadOnly`.
- Preserve binding and dependency-property compatibility unless the issue agrees on an API break.
- Update `README.md` and `CHANGELOG.md` if the public API, a gesture or a known limit changes.
- Use clear names, comments only where they explain a non-obvious decision, and formatting consistent with the existing code.

## Especially sensitive areas

- Coercions of `Minimum`, `Maximum`, `Value`, the steps and `ResetValue`.
- Text measurement, culture, fonts and the commitment never to clip the value.
- Template reapplication and customization through WPF styles.
- Mouse capture, focus and coexistence with a `ScrollViewer`.

## Reporting bugs

Include, as far as possible:

- .NET and Windows versions;
- a minimal XAML/C# snippet that reproduces the problem;
- the values of the relevant properties;
- expected behavior, observed behavior and a screenshot if it adds context.

By contributing you accept that your contributions are distributed under the project's MIT license.
