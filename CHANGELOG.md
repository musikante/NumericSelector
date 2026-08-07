# Changelog

Este proyecto sigue el formato de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y, cuando se publiquen versiones, aplicará [versionado semántico](https://semver.org/lang/es/).

## [Unreleased]

### Changed — afinado final

- **Teclado: las teclas `+` y `-` de la fila principal también cambian el valor** (`±SmallChange`). Las del teclado numérico (`Key.Add`/`Key.Subtract`) ya funcionaban; se suman `Key.OemPlus`/`Key.OemMinus`. El banco de pruebas documenta la combinación completa.
- **El padding de la caja del valor pasa a ser asimétrico** (`3,0,3,1` antes `4,0,4,1`). El lado izquierdo más chico compensa la impresión de dígitos corridos a la derecha y degrada la diferencia de centrado sub-píxel (~1px) entre `ValueBoxSide=Left` y `Right`, originada por el redondeo a píxel de la columna `*`. Los medidores ocultos usan el mismo padding para que el ancho de la columna `Auto` coincida con lo dibujado.
- **Demo**: el Master se ubica en una fila superior de alto fijo (no reacomoda los controles al cambiar de tamaño) y las opciones pasan a tres columnas; la guía de gestos vive en una celda del 40% junto al Master (60%).
- **Demo — selección de color por valor**: los combos de color eligén la selección por `Color` (`SelectedValuePath` + `BrushToColorConverter`) y no por referencia de brush. Con la selección por referencia, al declarar el Master después de los combos (nuevo orden del árbol) la caja aparecía vacía aunque funcionara.
- **Demo**: paleta de colores recortada y agrupada por uso (neutros → acentos → `Transparent`).
- **La propiedad de solo visualización pasa de llamarse `IsDisplayOnly` a `IsReadOnly`**: es el nombre estándar de WPF para "el usuario no modifica, el programa sí" (igual que `TextBox.IsReadOnly`). Sin cambios de comportamiento.
- **La matriz de costuras cambia de paradigma**: la barra y el detalle pasan a ser el marco fijo (la barra siempre lleva sus cuatro lados; la fila de detalle todo menos el superior) y la caja del valor pasa a ser el único elemento que cambia —cede el lado que mira a su compañero de fila y el superior cuando desciende—. Visual idéntico con `ControlBorderPixels` uniforme; con grosor no uniforme la costura vertical la dibuja ahora el vecino fijo (antes la caja), así que toma su lado.
- **La medición del texto sale del control hacia una función pura** (`TextMeasure.Measure`): lee la tipografía global del control (detalle, leyenda y valor) y la cultura de `Language` para devolver el ancho y alto netos de un texto, sin padding ni bordes. El control mantiene un `TextMeasureContext` inmutable que reconstruye sólo al cambiar fuente/idioma, y pasa el DPI por tanda de recálculo (no lo guarda). Queda así testeada sin ventana, como la matriz de costuras.

### Changed — rediseño de la API de disposición

- Se eliminan `TitleMode` (`Hidden`/`Framed`/`Frameless`), `ValuePlacement` (`BesideBar`/`OnBar`/`WithTitleFramed`/`WithTitleFrameless`) y `FillForeground`. Sus responsabilidades pasan a tres propiedades independientes, sin valores combinados ni degradaciones:
  - `ShowDetail` (`bool`, default `false`): muestra la fila de detalle inferior enmarcada.
  - `ValueFollowsDetail` (`bool`, default `true`): con `ShowDetail`, baja el casillero del valor junto al detalle; con `false` se queda junto a la barra.
  - `ValueBoxSide` (`ValueBoxSide`, default `Right`): lado del casillero del valor respecto de su compañero de fila.
- **No quedan dependencias entre propiedades**: cualquier combinación es válida y produce un contorno cerrado, sin coerción ni estados que documentar. Cuando la caja baja (`ShowDetail && ValueFollowsDetail`) el número va a la fila de detalle y la barra queda sola arriba.
- Se descarta la variante **`OnBar`** (número sobre la barra) y el modo **`Frameless`** (cajas sin marco): la caja del valor siempre está enmarcada. Con ello desaparecen las dos capas de texto del valor sobre el relleno, `FillForeground`, el recorte de la capa clara y la abstracción `OrientationAxis` (el control es horizontal únicamente).
- El reparto de lados entre celdas pasa a una **única función pura** (`ValueBorderResolver.Resolve`), la misma matriz de costuras en una sola fuente de verdad, testeada sin ventana. La plantilla la consume con `MultiBinding` en las cuatro celdas; desaparecen los tres convertidores ad hoc de `Thickness`.
- La orientación vertical deja de estar en el roadmap.

### Added

- Documentación inicial de contribución y política de seguridad.
- Licencia MIT y reglas de exclusión para Git (`.gitignore`).
- Normalización de finales de línea con `.gitattributes`: LF en el repositorio y los propios de cada plataforma en el disco de trabajo, binarios marcados como tales, y CRLF forzado en `.bat`, `.cmd` y `.ps1`, que lo necesitan para funcionar. Evita que un colaborador en otro sistema genere diferencias de archivo entero por el solo cambio de fin de línea.
- README reestructurado con vista previa visual y roadmap.
- Proyecto MSTest con pruebas de defaults, coerciones, pasos, disposición y `ValueChanged`.
- Pruebas automatizadas de los modos de interacción: `ValueChangeMode` —incluida la regla de que en `MustFocusFirst` el click que otorga el foco no mueve el valor, y que la regla alcanza también al click derecho por zonas— e `IsReadOnly` —coerción y restitución de `Focusable`, liberación del foco ya puesto, bloqueo de mouse, rueda y teclado, y cambios por código que siguen funcionando—.

### Changed

- La plantilla pasa a estar organizada en **dos secciones con marco propio**: la de datos (barra y casillero) con los cuatro lados, y la del título —opcional— con los mismos lados menos el inferior. La línea que separa el título de la barra ya no es un elemento aparte: es el borde superior de la sección de datos, que hace doble función (divisor cuando hay título, borde superior del control cuando no lo hay). Ambos marcos se tiñen juntos al recibir el foco. Con un `ControlBorderPixels` uniforme —el caso corriente— el resultado es idéntico píxel a píxel al anterior, verificado sobre nueve escenarios.
- **`ControlBorderPixels` no uniforme cambia de significado en la separación.** Antes el divisor tomaba el mayor de los cuatro lados, un valor que no correspondía a ninguno en particular; ahora toma `Top`, que es el lado que efectivamente dibuja. Con `Thickness(1,2,3,4)` la separación pasa de 4 a 2 píxeles.
- **Un `Height` mayor que el contenido ahora lo absorbe la barra.** Antes quedaba una franja vacía debajo de la barra, dentro del marco; ahora la sección de datos se estira y su borde inferior coincide con el del control. Es el comportamiento que la plantilla ya documentaba para fijar `Height` en la instancia.
- Documentación de `StretchMode.AutoGrow` alineada con el comportamiento actual de crecimiento del control.
- El valor predeterminado efectivo de `ResetValue` pasa a ser `50`, coherente con la API documentada.
- `README.md` documenta que `SmallChange` y `LargeChange` se coaccionan también por arriba, hasta el ancho del rango, y que esa coacción es silenciosa.
- La nota `Anotaciones útiles.txt` sale de la carpeta de la librería y pasa a `docs/notas-historicas/`, con una cabecera que aclara que describe el diseño anterior (etapa RangeSlider) y no el control actual.

### Changed — renombrado de la API pública

- El control pasa a llamarse **`BoundedNumericSelector`** (antes `NumericSelector`). El nombre destaca la propiedad que define al control: el valor está **acotado** al rango y no hay forma de que el control entregue uno fuera de él, así que el consumidor no necesita validar la entrada. Se eligió *Bounded* sobre *Limited* porque en inglés "limited" connota capacidad reducida.
- El ensamblado y el espacio de nombres pasan de `NumericSelectorLib` a **`NumericSelector`**: el sufijo `Lib` no es idiomático en .NET, y con la clase renombrada el tipo ya no colisiona con el espacio de nombres que lo contiene.
- El banco de pruebas manual pasa de `NumericSelectorLib_Test` a **`NumericSelector.Demo`**: no es un proyecto de pruebas —es una aplicación de demostración— y su nombre anterior se confundía con el de pruebas automatizadas, que ahora es `NumericSelector.Tests`.
- Los nombres de las partes de la plantilla (`PART_*`) **no cambian**.

Se hace antes de la primera publicación, que es el único momento en que estos cambios no rompen a ningún consumidor.

### Changed — el control pasa a ser cuatro celdas independientes

- La plantilla deja de agrupar en dos secciones anidadas. Ahora son **cuatro celdas hermanas con marco propio**: etiqueta del título y caja del valor arriba, barra y caja del valor abajo. Ninguna está dentro de otra.
- **Regla única: la caja del valor tiene prioridad y define sus lados; los vecinos ceden el lado que tocan.** Así ningún filo se dibuja dos veces. La costura horizontal entre filas la dibuja siempre el borde superior de la barra, que además hace de borde superior del control cuando no hay título.
- Esto **habilita la caja del valor en `WithTitle`**, que antes era inviable: con el modelo anidado, el borde de la caja se apilaba debajo del de la sección, sumando altura al control (+`T`) y corriendo el filo derecho hacia adentro. Como celdas hermanas, el borde de la caja *es* el borde exterior y los dos efectos desaparecen por construcción. Medido: `WithTitle` volvió a 300×37 con `T=1` y 300×43 con `T=3`, iguales que `BesideBar`.
- Verificado contando píxeles: **el perímetro es continuo** en los ocho casos auditados (tres modos más el caso sin título, con `T=1` y `T=3`) y **ninguna costura mide `2T`** — todas dan exactamente `T`. De los trece escenarios de referencia, doce quedaron idénticos píxel a píxel y sólo cambió `WithTitle`, que es el que se quería cambiar.
- El modelo prepara la orientación vertical: la regla de prioridad se traslada a la plantilla vertical aunque el marcado no se reutilice.

### Changed — trazo separador del valor

- En `ValuePlacement.BesideBar`, el casillero del valor se separa de la barra con un trazo vertical del mismo grosor que el borde del control (`ControlBorderPixels.Left`), que se tiñe junto con el marco al recibir el foco. No es opcional: es una línea de tabla, no un modo de presentación.
- **Sólo el lado izquierdo.** Los otros tres los aporta el marco de la sección de datos; dibujarlos otra vez daría línea doble.
- El trazo **no aparece en `OnBar` ni en `WithTitle`**, donde la columna del valor mide 0 y quedaría como un trazo suelto pegado al final de la barra. Verificado por comparación de píxeles: esos dos modos rinden idénticos a antes del cambio.
- El piso de ancho suma ahora el grosor del trazo. Se calcula igual para todos los modos aunque sólo se dibuje en uno: reservar de más no molesta —la barra absorbe la diferencia— y así el piso no depende de la disposición.

### Changed — dos enums en lugar de tres booleanos

- `ShowTitleText`, `ShowTitleFrame` y `ShowValueFrame` se reemplazan por **`TitleMode`** (`Hidden` | `Framed` | `Frameless`) y dos valores nuevos de **`ValuePlacement`** (`WithTitleFramed` | `WithTitleFrameless`, que sustituyen a `WithTitle`).
- **Motivo: ninguna propiedad queda inerte.** Con los booleanos había tres relaciones condicionales que sólo se descubrían leyendo la documentación —`ShowTitleFrame` no hacía nada sin título, `ShowValueFrame` no hacía nada fuera de `WithTitle`— y una propiedad que a veces no hace nada es una trampa para quien usa el control. Ahora cada valor de cada enum hace algo siempre, y la lista de valores documenta por sí sola qué combinaciones existen.
- **Sobrevive una sola dependencia, y es irreducible:** las variantes `WithTitle*` necesitan que haya título. Sin él se degradan a `BesideBar` por coerción, y al volver a mostrarlo se restaura la variante exacta —framed o frameless— porque WPF conserva el valor base.
- **Contrapartida aceptada:** `TitleMode.Hidden` olvida si el título era `Framed` o `Frameless`. Con dos booleanos esa elección sobrevivía. Recuperarla exigiría estado extra: a diferencia de `ValuePlacement`, acá no hay valor base que restaurar porque `Hidden` lo elige el consumidor. Se nota sobre todo al alternar en el banco de pruebas; una aplicación real fija esto una vez en el XAML.
- Sin cambios visuales: los veintiún escenarios de referencia rinden idénticos píxel a píxel, y las diez combinaciones válidas de los dos enums quedaron auditadas con `T=1` y `T=3` sin ninguna racha de `2T`.

### Added — `ShowValueFrame`

- Nueva propiedad `bool ShowValueFrame` (predeterminada `true`, el aspecto de siempre). Con `false`, la caja del valor deja de pintar su marco y su fondo, y el número queda como una etiqueta al lado del título.
- **Alcance acotado a `ValuePlacement.WithTitle`**, donde la caja es un distintivo dentro de la fila del título y apagarlo simplemente lo quita. En `BesideBar` esa caja forma parte del rectángulo principal del control: apagarla no daría una variante del mismo aspecto sino otro distinto —barra encajonada y número afuera—, así que la propiedad es inerte allí. Ser inerte fuera de un modo no es una rareza en esta API: `ShowTitleFrame` ya lo es cuando no hay título.
- **La regla de prioridad gana una cláusula: la caja del valor manda mientras esté enmarcada; si no lo está, devuelve la prioridad y la etiqueta del título recupera el lado que le había cedido.** Sin esto el contorno quedaría abierto sobre el ancho del número, porque en el modelo de celdas el rectángulo exterior es la unión de los bordes y ese filo lo dibuja la propia caja.
- La recuperación **sólo ocurre si el título está enmarcado**: con el título ya sin marco no hay contorno que cerrar, y cambiar su grosor movería su texto sin motivo.
- **Cuáles** lados lleva la caja lo sigue decidiendo la posición y no es configurable —si lo fuera habría filos dibujados dos veces—; esta propiedad decide **si** se pintan. No altera la geometría: el grosor se sigue reservando.
- El foco no enciende el marco de la caja mientras esté apagado, igual que con el título.
- Auditado por píxeles en las dieciséis combinaciones de modo, marcos y grosor: ninguna racha mide `2T`, el contorno cierra siempre, y los modos distintos de `WithTitle` rinden exactamente igual con la propiedad en `true` o en `false`.

### Added — `ShowTitleFrame`

- Nueva propiedad `bool ShowTitleFrame` (predeterminada `true`, el aspecto de siempre). Con `false`, el borde y el fondo de la sección del título pasan a transparentes y el título se lee como una etiqueta suelta por encima de la sección de datos, que conserva su marco.
- **No altera la geometría**: se apaga lo que se pinta, no el grosor, que se sigue reservando. Activar o desactivar el modo no mueve nada de lugar, verificado por comparación de píxeles.
- **El foco no enciende el marco del título mientras esté apagado.** La regla vive en la condición de un `MultiTrigger` y no en el orden de declaración de los triggers; se comprobó declarando el bloque antes de los del foco y confirmando que el comportamiento no cambia. El foco lo sigue señalando el marco de la sección de datos.
- Los brochazos son `Transparent` y no nulos a propósito: una brocha transparente igual pinta, y por eso la fila del título sigue recibiendo los clics que dan el foco. Con `x:Null` dejaría de ser alcanzable por el mouse.

### Fixed

- **Un control deshabilitado seguía respondiendo al teclado.** `IsEnabled = false` impide *ganar* el foco, pero no suelta el que ya estuviera puesto: al deshabilitar un control enfocado, éste conservaba `IsKeyboardFocused` y, como las teclas se rutean al elemento enfocado y no al que está bajo el puntero, las flechas seguían moviendo el valor. Además el marco de foco quedaba encendido en un control deshabilitado. Ahora el foco se suelta al deshabilitar, con una guarda adicional en el manejo de teclas. El mouse y la rueda no requerían tratamiento: con `IsEnabled = false` el hit-test de entrada ya deja de devolver partes del control, y la rueda se rutea por el mismo camino. Cubierto por tres pruebas nuevas, verificadas por mutación.

### Added

- Converter público `ThicknessWithoutBottomConverter`, que la plantilla usa para darle a la sección del título los mismos bordes que a la de datos menos el inferior. `BorderThickness` es un único `Thickness` de cuatro lados, así que anular uno solo no se puede resolver con un `TemplateBinding` pelado.

### Removed

- Parte de plantilla `PART_Divider` y el método `UpdateDividerThickness()` que le ajustaba el grosor a mano, junto con el `PART_MainBorder` que envolvía todo. Su función la cumplen ahora los marcos de las dos secciones.
- Callback interno `OnVisualPropertyChanged` y el `InvalidateVisual()` que forzaba. `ControlBorderColor`, `BarFillColor` y `BarBorderColor` llegan a la plantilla por `TemplateBinding` y repintan solos; al ser un control lookless, que no dibuja nada propio, ese redibujo no tenía destinatario. Sin cambios de comportamiento observable.

## Desarrollo inicial

### Added

- Control WPF `BoundedNumericSelector` para valores enteros discretos y acotados.
- Interacción por barra, arrastre vertical del valor, rueda y teclado.
- Modos de presentación `BesideBar`, `OnBar` y `WithTitle`.
- Modos de interacción `ChangeOnClick`, `MustFocusFirst` e `IsReadOnly`.
- Aplicación WPF de demostración de las propiedades y gestos disponibles.
