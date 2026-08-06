# NumericSelector — Guía para agentes

Control WPF *lookless* para seleccionar un valor entero acotado a un rango. El control se llama `BoundedNumericSelector` y vive en el ensamblado `NumericSelector`.

## Estructura del repositorio

```
NumericSelector/          → Librería del control (proyecto principal)
NumericSelector.Demo/     → Aplicación WPF de demostración (banco de pruebas manual)
NumericSelector.Tests/    → Pruebas automatizadas (MSTest)
docs/notas-historicas/    → Documentación histórica (NO describe el control actual)
```

## Comandos esenciales

```powershell
# Compilar toda la solución (Release sin advertencias es el estándar para PRs)
dotnet build .\NumericSelector.slnx --configuration Release

# Ejecutar la aplicación de demostración
dotnet run --project .\NumericSelector.Demo\NumericSelector.Demo.csproj

# Ejecutar todas las pruebas
dotnet test .\NumericSelector.slnx

# Ejecutar solo las pruebas de un proyecto
dotnet test .\NumericSelector.Tests\NumericSelector.Tests.csproj
```

## Stack y configuración

- **SDK**: .NET 10.0.302 (`global.json`, rollForward: latestFeature)
- **Target**: `net10.0-windows` con `UseWPF=true`
- **Plataforma**: Windows únicamente (WPF no es multiplataforma)
- **Tests**: MSTest 4.3.2 + Microsoft.NET.Test.Sdk 18.8.1
- **Solución**: `NumericSelector.slnx` (formato SDK-style nuevo)

## Arquitectura del control

### Archivos fuente principales

| Archivo | Contenido |
|---------|-----------|
| `BoundedNumericSelector.cs` | Lógica de interacción, medición, cursores, manejo de eventos |
| `BoundedNumericSelector.Dependencies.cs` | Propiedades de dependencia, coerciones, evento ValueChanged |
| `Themes/Generic.xaml` | Plantilla por defecto (estilo, triggers, partes) |
| `Converters.cs` | `ValueBorderResolver`: la matriz de costuras como función pura |
| `ValueBoxSide.cs`, `StretchMode.cs`, `ValueChangeMode.cs` | Enums de la API pública |

### Modelo de celdas

El control son **cuatro celdas hermanas** con marco propio:
- Etiqueta del título (arriba, izquierda)
- Caja del valor (arriba junto al título, o al lado de la barra)
- Barra (abajo, izquierda)
- Caja del valor (abajo, derecha — o izquierda con `ValueBoxSide=Left`)

**Regla única**: la caja del valor tiene prioridad y define sus lados; los vecinos ceden el lado que tocan. Ningún filo se dibuja dos veces. El reparto concreto lo calcula `ValueBorderResolver.Resolve`, una función pura con la matriz de costuras completa (ver su `Converters.cs`).

### Partes de la plantilla (PART_)

La plantilla define estas partes que el code-behind referencia por nombre:
- `PART_MainGrid`, `PART_TopRow`, `PART_TitleCell`, `PART_TitleText`
- `PART_BarAndValueGrid`, `PART_BarCell`, `PART_BarGrid`, `PART_BarRect`
- `PART_LegendText`, `PART_ValueSideCell`, `PART_ValueText`
- `PART_ValueTopCell`, `PART_ValueWithTitle`
- Medidores ocultos: `PART_TopSizerMin/Max`, `PART_ValueSizerMin/Max`

**No se deben renombrar**: los nombres `PART_*` son contrato público.

### Propiedades de dependencia y coerciones

Las coerciones son silenciosas y mutuamente restrictivas:

| Propiedad | Coerción |
|-----------|----------|
| `Minimum` | ≤ `Maximum - 1` (el rango siempre tiene al menos 1 de ancho) |
| `Maximum` | ≥ `Minimum + 1` |
| `Value` | Acotado a `[Minimum, Maximum]` |
| `ResetValue` | Acotado a `[Minimum, Maximum]` |
| `SmallChange`, `LargeChange` | Entre `1` y el ancho del rango |
| `Focusable` | `false` cuando `IsDisplayOnly=true` (coerción, no asignación) |

### Cultura y formato

- El control usa `Language` (no la cultura del sistema) para formatear números
- Por defecto se asigna `CultureInfo.CurrentCulture.IetfLanguageTag`
- El `MinWidth` se calcula a partir del número más largo del rango según la cultura

### Modos de interacción

- `ValueChangeMode.ChangeOnClick` (default): el mouse actúa siempre
- `ValueChangeMode.MustFocusFirst`: el mouse actúa solo con foco previo; el click que enfoca no cambia el valor
- `IsDisplayOnly`: bloquea mouse, teclado y tabulación; conserva aspecto; cambios por código siguen funcionando

Teclado: `←`/`↓`/`-` y `→`/`↑`/`+` cambian o restan `SmallChange`; `PageUp`/`PageDown` de a `LargeChange`; `Home`/`End` a los extremos; `Delete`/`Insert` a `ResetValue`. Se manejan las teclas de la fila principal (`Key.OemPlus`/`OemMinus`) y las del teclado numérico (`Key.Add`/`Subtract`).

### AutoGrow

- `StretchMode.AutoGrow` asigna `Width` directamente (no negocia con el layout)
- El crecimiento se difiere con `Dispatcher.BeginInvoke(Render)` para evitar mutar el layout durante la medición
- `Width` nunca se asigna en `NaN` (rompería bindings)

## Pruebas

### Tipos de pruebas

1. **Lógica pura** (`BoundedNumericSelectorLogicTests.cs`): no necesitan ventana; validan defaults, coerciones, pasos, disposición, ValueChanged
2. **Interacción** (`InteractionModeTests.cs`): necesitan ventana real (STA + Dispatcher) porque los gestos llegan por eventos ruteados y el foco no existe fuera del visual tree

### Helper `StaTest`

Las pruebas que crean controles WPF usan `StaTest.Run()` para ejecutarse en un hilo STA aislado, independientemente del modelo de apartamento del runner.

### Cobertura actual

- Defaults, coerciones de rango, pasos, disposición, ValueChanged
- Valor de la matriz de costuras (`ValueBorderResolver.Resolve`) en todas las configuraciones
- Disposición (`ShowTitle`, `ValueFollowsTitle`, `ValueBoxSide`) y trazo separador del valor
- ValueChangeMode (ChangeOnClick, MustFocusFirst)
- IsDisplayOnly (focusable, liberación de foco, bloqueo de input)
- IsEnabled (liberación de foco, bloqueo de teclado)

### Pendiente (según roadmap)

- Pruebas de cultura, foco, gestos de mouse y AutoGrow

## Convenciones del proyecto

- Comentarios y documentación en español
- Nombres claros, comentarios solo donde expliquen una decisión no obvia
- Formato consistente con el código existente
- Una corrección/mejora por PR
- Actualizar `README.md` y `CHANGELOG.md` si cambia la API pública
- No hay CI configurado aún (planeado: GitHub Actions)

## Advertencias

- `docs/notas-historicas/` describe un diseño **anterior** (RangeSlider/UserControl) y no debe leerse como referencia vigente
- El control es **horizontal únicamente** (la orientación vertical se descartó en el rediseño)
- El dominio es **intencionalmente entero** (sin soporte para decimales)
- `FocusVisualStyle` se desactiva por defecto; el foco lo indica el borde (`FocusBorderColor`)
