# NumericSelector — Guía para agentes

Control WPF *lookless* para seleccionar un valor entero acotado a un rango. El control se llama `BoundedNumericSelector` y vive en el ensamblado `NumericSelector`.

## Estructura del repositorio

```
NumericSelector/          → Librería del control (proyecto principal)
NumericSelector.Demo/     → Aplicación WPF de demostración (banco de pruebas manual)
NumericSelector.Tests/    → Pruebas automatizadas (MSTest)
docs/images/              → Capturas que ilustran el README
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
| `Converters.cs` | `ValueBorderResolver` (matriz de costuras): función pura |
| `ValueBoxDock.cs`, `MouseInteractionBehavior.cs`, `UserInteractionMode.cs` | Enums de la API pública |

### Modelo de celdas

El control son **cuatro celdas hermanas** con marco propio:
- Barra, con su `CaptionText` dibujado encima (arriba, la fila siempre presente)
- Caja del valor junto a la barra (arriba, a la derecha — o a la izquierda con `ValueBoxDock=Left`)
- Etiqueta del detalle (abajo, sólo con `ShowDetail`)
- Caja del valor en la fila de detalle (abajo, cuando `ValueFollowsDetail` la hace descender)

La caja del valor está declarada en las dos filas, pero sólo una es visible a la vez: `ShowDetail && ValueFollowsDetail` colapsa la de arriba y muestra la de abajo.

**Regla única**: la barra y el detalle son el marco fijo (la barra lleva sus cuatro lados, el detalle cede el superior) y la caja del valor cede el lado que mira a su compañero de fila. Ningún filo se dibuja dos veces. El reparto concreto lo calcula `ValueBorderResolver.Resolve`, una función pura con la matriz de costuras completa (ver su `Converters.cs`).

### Partes de la plantilla (PART_)

La plantilla define estas partes que el code-behind referencia por nombre:
- `PART_MainGrid`, `PART_DetailRow`
- `PART_BarAndValueGrid`, `PART_BarCell`, `PART_BarGrid`, `PART_BarRect`, `PART_BarRowDef`
- `PART_CaptionText`, `PART_ValueCell`, `PART_ValueText`, `PART_ValueSizerMin/Max`
- La fila de detalle, con su caja de valor propia: `PART_DetailCell`, `PART_DetailText`, `PART_ValueDetailCell`, `PART_ValueDetailText`, `PART_DetailSizerMin/Max`
- Las columnas se nombran **por posición**, no por ocupante, porque con `ValueBoxDock=Left` las celdas se intercambian: `PART_Column0`/`PART_Column1` (fila de la barra) y `PART_DetailColumn0`/`PART_DetailColumn1` (fila de detalle)

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
| `Focusable` | `false` cuando `InteractionMode=ReadOnly` (coerción, no asignación) |

### Cultura y formato

- El control usa `Language` (no la cultura del sistema) para formatear números
- Por defecto se asigna `CultureInfo.CurrentCulture.IetfLanguageTag`
- El ancho del casillero del valor se reserva con **medidores ocultos** en la plantilla (dos `TextBlock` con `Minimum` y `Maximum` en la misma celda que el valor); WPF toma el máximo y la celda nunca queda más angosta que el número más largo del rango, según la cultura

### Modos de interacción

- `MouseInteractionBehavior.ChangeOnClick` (default): el mouse actúa siempre
- `MouseInteractionBehavior.MustFocusFirst`: el mouse actúa solo con foco previo; el click que enfoca no cambia el valor
- `InteractionMode = ReadOnly`: bloquea mouse, teclado y tabulación; conserva aspecto; cambios por código siguen funcionando

Teclado: `←`/`↓`/`-` y `→`/`↑`/`+` cambian o restan `SmallChange`; `PageUp`/`PageDown` de a `LargeChange`; `Home`/`End` a los extremos; `Delete`/`Insert` a `ResetValue`. Se manejan las teclas de la fila principal (`Key.OemPlus`/`OemMinus`) y las del teclado numérico (`Key.Add`/`Subtract`).

### Ancho: `BaseWidth` (piso) y encaje al contenedor

- `BaseWidth` (`double`, default `NaN` = automático) es el ancho base desde el que el control crece; **no** es un constraint duro: se lee como piso en `MeasureOverride`, que pide `max(BaseWidth, contenido)` pero **nunca más ancho que el hueco disponible** (la CharacterEllipsis de la leyenda y el detalle trunca lo que no quepa)
- El control **no escribe** `FrameworkElement.Width`/`MinWidth` en runtime: hacerlo obligaba al elemento a ese tamaño y desbordaba/recortaba los bordes en un contenedor angosto (era la causa del recorte del marco). El piso del número lo dan los medidores ocultos de la plantilla
- El ancho natural sale de `base.MeasureOverride(new Size(infinito, alto))`; la barra vive en una columna `*`, que con ancho infinito se comporta como `Auto` (no se estira a todo el contenedor)
- No hay `Viewbox` en la plantilla: escalaría la tipografía en vez de truncar con elipsis, que es el comportamiento pedido
- El consumidor que quiera forzar un ancho mínimo usa `BaseWidth`, no `Width`/`MinWidth` (esos son duros y vuelven a recortar)

## Pruebas

### Tipos de pruebas

1. **Lógica pura** (`BoundedNumericSelectorLogicTests.cs`, `ValueBorderResolverTests.cs`): no necesitan ventana; validan defaults, coerciones, pasos, disposición, ValueChanged y la matriz de costuras
2. **Interacción** (`InteractionModeTests.cs`): necesitan ventana real (STA + Dispatcher) porque los gestos llegan por eventos ruteados y el foco no existe fuera del visual tree

### Helper `StaTest`

Las pruebas que crean controles WPF usan `StaTest.Run()` para ejecutarse en un hilo STA aislado, independientemente del modelo de apartamento del runner.

### Cobertura actual

- Defaults, coerciones de rango, pasos, disposición, ValueChanged
- Valor de la matriz de costuras (`ValueBorderResolver.Resolve`) en todas las configuraciones
- Disposición (`ShowDetail`, `ValueFollowsDetail`, `ValueBoxDock`) y trazo separador del valor
- Medición y `BaseWidth` (`MeasureAndBaseWidthTests.cs`): el ancho pedido nunca excede un hueco fijo y `BaseWidth` actúa como piso con espacio infinito
- MouseBehavior (ChangeOnClick, MustFocusFirst)
- InteractionMode (focusable, liberación de foco, bloqueo de input)
- IsEnabled (liberación de foco, bloqueo de teclado)

### Pendiente

- Pruebas de cultura y formato de número (el resto —foco, gestos de mouse, crecimiento— ya está cubierto)

## Convenciones del proyecto

- Comentarios y documentación en español
- Nombres claros, comentarios solo donde expliquen una decisión no obvia
- Formato consistente con el código existente
- Una corrección/mejora por PR
- Actualizar `README.md` y `CHANGELOG.md` si cambia la API pública
- No hay CI configurado aún (planeado: GitHub Actions)

## Advertencias

- El control es **horizontal únicamente** (la orientación vertical se descartó en el rediseño)
- El dominio es **intencionalmente entero** (sin soporte para decimales)
- `FocusVisualStyle` se desactiva por defecto; el foco lo indica el borde (`FocusBorderBrush`)
- **Un `TemplateBinding` mal escrito compila igual y falla en silencio.** Al tocar la plantilla o renombrar propiedades que ella consume, hay que instanciar el control y leer las partes con `Template.FindName` para comprobar que los valores llegan. Lo mismo vale para los `StaticResource` del demo: un recurso borrado revienta al **cargar** la ventana, no al compilar.

## Trabajo pendiente

### Incoherencias de texto detectadas y no corregidas

- `ValueBoxDockTextConverter` (demo) documenta `int -> "Right"/"Left"`; el orden real de la lista es `Left`(0) / `Right`(1).
- La sección "Partes de la plantilla" de este archivo dice que el code-behind las referencia **todas** por nombre; en realidad resuelve cuatro (`PART_BarGrid`, `PART_BarRect`, `PART_ValueText`, `PART_ValueDetailText`). El resto sólo existe para los triggers.

### Limpieza de código

- **Doble default de `CaptionText`/`DetailText`**: la metadata de la DP dice `"Default Caption"`/`"Default Detail Text"` y el `Style` de `Generic.xaml` dice `"DefaultCaption"`/`"DefaultDetail"`. Gana el Style; son dos fuentes de verdad para lo mismo.
- **`ValueBorderResolver.Convert` degrada en silencio**: un `ConverterParameter` con un typo cae en el `default:` del switch y devuelve el `Thickness` de la celda de detalle. En una plantilla alternativa da un marco mal dibujado sin ningún aviso; un `throw` sería más honesto y no cambia nada para la plantilla propia.
- **Nombres de columna posicionales**: con `ValueBoxDock=Left`, `PART_BarColumn` (índice 0) pasa a alojar el casillero del valor, y el trigger le pone `Auto`. El comportamiento es correcto —está verificado— pero se lee como el error que el comentario de al lado dice estar evitando. `PART_Column0`/`PART_Column1` lo aclararía.
- **Demo**: `ValueFollowsDetailIndexConverter` y `ShowDetailIndexConverter` son idénticos carácter por carácter.
- **`NumericSelector.csproj`**: tiene un `<Service Include="{508349b6-…}"/>` residual de Visual Studio.

### Funcionalidad

- `[TemplatePart]` sobre la clase, para documentar el contrato de la plantilla.
- Validación de valores inválidos en los enums públicos: hoy `(ValueBoxDock)99` se asigna sin protestar.
- `ShowBar` y el modo ultra-minimalista (sólo el casillero con arrastre vertical). El piso de ancho que necesita ya lo dan los medidores ocultos.
- Ayuda de gestos en el demo. El material está en `TempText - Interaction help.txt`, en la raíz y **sin trackear** a propósito.

### Publicación

- El repositorio **no tiene remoto todavía**: publicarlo en GitHub es decisión del usuario.
- Empaquetado NuGet y versión `0.1.0`; CI en GitHub Actions para `build` y `test`.
