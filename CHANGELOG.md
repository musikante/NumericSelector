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
- Modos de interacción `ChangeOnClick`, `MustFocusFirst` e `IsDisplayOnly`.
- Aplicación WPF de demostración de las propiedades y gestos disponibles.
