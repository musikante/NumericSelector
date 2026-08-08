# Changelog

Este proyecto sigue el formato de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y, cuando se publiquen versiones, aplicará [versionado semántico](https://semver.org/lang/es/).

## [Unreleased] — primera versión

Todavía sin publicar; al hacerlo pasa a ser `0.1.0`. Como nada de esto llegó nunca a un consumidor, el desarrollo previo no se registra: la API cambió de nombre varias veces antes de asentarse y ese recorrido no le sirve a nadie que empiece a usar el control ahora. **La historia completa está en los commits.**

### Added

**El control.** `BoundedNumericSelector`, un control WPF *lookless* de entrada numérica discreta y **acotada**: no hay forma de que entregue un valor fuera de `[Minimum, Maximum]`, así que el consumidor no necesita validar la entrada. Vive en el ensamblado `NumericSelector`, para .NET 10 sobre Windows.

**Valores y rango.** `Value`, `Minimum`, `Maximum`, `SmallChange`, `LargeChange` y `ResetValue`, todos `int` y todos con coerción silenciosa:

- El rango conserva siempre al menos una unidad de ancho. Al empujar un extremo contra el otro, ese extremo se frena en vez de arrastrar a su par; si después se separan, recupera el valor que se le había pedido.
- `Value` y `ResetValue` se acotan al rango; los pasos, entre `1` y el ancho del rango.
- `Value` usa binding bidireccional por defecto. El evento **`ValueChanged`** informa el valor viejo y el nuevo.

**Textos y disposición.** `CaptionText` se dibuja sobre la barra; `DetailText` ocupa una fila inferior opcional. Tres propiedades independientes deciden dónde va el casillero del valor, sin combinaciones inválidas ni degradaciones: `ShowDetail`, `ValueFollowsDetail` y `ValueBoxDock` (`Right` | `Left`).

**Marco de cuatro celdas.** La plantilla no anida secciones: son cuatro celdas hermanas con marco propio, así ningún borde se apila con otro sumando grosor. Qué lados dibuja cada una lo resuelve una única función pura (`ValueBorderResolver.Resolve`), de modo que ningún filo se dibuja dos veces. La costura horizontal la aporta siempre el borde inferior de la barra, que además cierra el control por abajo cuando no hay detalle.

**Legibilidad garantizada del número.** El ancho del casillero lo reservan **medidores ocultos** de la plantilla (un `TextBlock` con el `Minimum` y otro con el `Maximum`, ocultos pero que ocupan sitio): la celda nunca se mide más angosta que el número más largo que el rango puede producir, sin depender del valor actual ni temblar al pasar de 99 a 100. La leyenda puede truncarse con elipsis; el número no.

**Ancho: `BaseWidth`.** Es el ancho base desde el que el control crece y a la vez un piso, no un constraint duro: pide `max(BaseWidth, contenido)` pero nunca más que el hueco que le da el contenedor, así que los bordes no se recortan ni desbordan. El control no escribe `Width`/`MinWidth` en runtime.

**Cultura.** El formato de los números sale de `FrameworkElement.Language`, no de la cultura del hilo. El control adopta por sí solo la del sistema donde corre, y el consumidor puede pisarla asignando `Language` en la instancia.

**Interacción.**

- *Mouse:* click y arrastre sobre la barra llevan el valor a la posición del puntero; click derecho por zonas (30 % izquierdo → `Minimum`, 40 % central → `ResetValue`, 30 % derecho → `Maximum`); doble click sobre el número lo restablece; arrastre vertical sobre el número lo mueve de a `SmallChange`.
- *Rueda:* de a `SmallChange`, **sólo con el foco puesto** y sin marcar el evento como manejado en los topes. Los dos recaudos evitan que un selector dentro de un `ScrollViewer` se coma el desplazamiento de la lista o cambie valores al pasar el mouse por encima.
- *Teclado:* flechas y `+`/`-` (fila principal y numérico) de a `SmallChange`; `PageUp`/`PageDown` de a `LargeChange`; `Home`/`End` a los extremos; `Delete`/`Insert` a `ResetValue`.
- El cursor dice la verdad sobre si el gesto va a hacer algo.

**Modos.** `MouseBehavior` (`ChangeOnClick` | `MustFocusFirst`) decide si el mouse actúa de inmediato o exige foco previo, en cuyo caso el click que da el foco no toca el valor. `InteractionMode` (`Interactive` | `ReadOnly`) bloquea al usuario sin alterar la apariencia y sin sacar el control del árbol visual; asignar `Value` por código sigue funcionando. `IsEnabled = false` también suelta el foco si el control lo tenía.

**Apariencia.** `BorderBrush` y `BorderThickness` son las heredadas de `Control`, con el valor por defecto cambiado a negro y `1`; a ellas se suman `FocusBorderBrush`, `BarFill` y `BarDividerBrush`. Todas son `Brush`, así que aceptan degradados, imágenes o cualquier otra brocha, no sólo un color liso. El foco se indica tiñendo los marcos, no con el rectángulo punteado de WPF.

**Aplicación de demostración.** `NumericSelector.Demo` permite probar a mano toda la API —rango, textos, disposición, brochas, fuentes y gestos— con el propio control como selector de sus opciones.

**Pruebas.** 38 pruebas MSTest sobre defaults, coerciones, disposición, la matriz de costuras, la medición y `BaseWidth`, y los modos de interacción. Las que necesitan ventana corren en un hilo STA aislado.

**Documentación e infraestructura.** `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `AGENTS.md`, licencia MIT y reglas de exclusión para Git. `.gitattributes` normaliza los finales de línea (LF en el repositorio, los propios de cada plataforma en el disco de trabajo) para que un colaborador en otro sistema no genere diferencias de archivo entero.

### Límites conocidos

- Sólo orientación **horizontal**; la vertical está descartada.
- El dominio es **intencionalmente entero**: no hay soporte para decimales.
- Los valores inválidos de las enumeraciones públicas todavía no se validan.
- Falta el contrato de plantilla declarado con `TemplatePart`.
