# Changelog

Este proyecto sigue el formato de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y, cuando se publiquen versiones, aplicará [versionado semántico](https://semver.org/lang/es/).

## [Unreleased]

### Added

- Documentación inicial de contribución y política de seguridad.
- Licencia MIT y reglas de exclusión para Git (`.gitignore`).
- Normalización de finales de línea con `.gitattributes`: LF en el repositorio y los propios de cada plataforma en el disco de trabajo, binarios marcados como tales, y CRLF forzado en `.bat`, `.cmd` y `.ps1`, que lo necesitan para funcionar. Evita que un colaborador en otro sistema genere diferencias de archivo entero por el solo cambio de fin de línea.
- README reestructurado con vista previa visual y roadmap.
- Proyecto MSTest con pruebas de defaults, coerciones, pasos, disposición y `ValueChanged`.
- Pruebas automatizadas de los modos de interacción: `ValueChangeMode` —incluida la regla de que en `MustFocusFirst` el click que otorga el foco no mueve el valor, y que la regla alcanza también al click derecho por zonas— e `IsDisplayOnly` —coerción y restitución de `Focusable`, liberación del foco ya puesto, bloqueo de mouse, rueda y teclado, y cambios por código que siguen funcionando—.

### Changed

- Documentación de `StretchMode.AutoGrow` alineada con el comportamiento actual de crecimiento del control.
- El valor predeterminado efectivo de `ResetValue` pasa a ser `50`, coherente con la API documentada.
- `README.md` documenta que `SmallChange` y `LargeChange` se coaccionan también por arriba, hasta el ancho del rango, y que esa coacción es silenciosa.
- La nota `Anotaciones útiles.txt` sale de `NumericSelectorLib/` y pasa a `docs/notas-historicas/`, con una cabecera que aclara que describe el diseño anterior (etapa RangeSlider) y no el control actual.

### Removed

- Callback interno `OnVisualPropertyChanged` y el `InvalidateVisual()` que forzaba. `ControlBorderColor`, `BarFillColor` y `BarBorderColor` llegan a la plantilla por `TemplateBinding` y repintan solos; al ser un control lookless, que no dibuja nada propio, ese redibujo no tenía destinatario. Sin cambios de comportamiento observable.

## Desarrollo inicial

### Added

- Control WPF `NumericSelector` para valores enteros discretos y acotados.
- Interacción por barra, arrastre vertical del valor, rueda y teclado.
- Modos de presentación `BesideBar`, `OnBar` y `WithTitle`.
- Modos de interacción `ChangeOnClick`, `MustFocusFirst` e `IsDisplayOnly`.
- Aplicación WPF de demostración de las propiedades y gestos disponibles.
