# Changelog

Este proyecto sigue el formato de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y, cuando se publiquen versiones, aplicará [versionado semántico](https://semver.org/lang/es/).

## [Unreleased]

### Added

- Documentación inicial de contribución y política de seguridad.
- Licencia MIT y reglas de exclusión para Git.
- README reestructurado con vista previa visual y roadmap.
- Proyecto MSTest con pruebas de defaults, coerciones, pasos, disposición y `ValueChanged`.

### Changed

- Documentación de `StretchMode.AutoGrow` alineada con el comportamiento actual de crecimiento del control.
- El valor predeterminado efectivo de `ResetValue` pasa a ser `50`, coherente con la API documentada.
- `README.md` documenta que `SmallChange` y `LargeChange` se coaccionan también por arriba, hasta el ancho del rango, y que esa coacción es silenciosa.
- La nota `Anotaciones útiles.txt` sale de `NumericSelectorLib/` y pasa a `docs/notas-historicas/`, con una cabecera que aclara que describe el diseño anterior (etapa RangeSlider) y no el control actual.

## Desarrollo inicial

### Added

- Control WPF `NumericSelector` para valores enteros discretos y acotados.
- Interacción por barra, arrastre vertical del valor, rueda y teclado.
- Modos de presentación `BesideBar`, `OnBar` y `WithTitle`.
- Modos de interacción `ChangeOnClick`, `MustFocusFirst` e `IsDisplayOnly`.
- Aplicación WPF de demostración de las propiedades y gestos disponibles.
