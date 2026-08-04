# Contribuir a NumericSelector

Gracias por querer mejorar NumericSelector. El proyecto está en desarrollo activo y las contribuciones que mantengan el control predecible, accesible y fácil de personalizar son bienvenidas.

## Antes de empezar

- Revisá los issues abiertos y evitá duplicar trabajo.
- Para cambios grandes de API, comportamiento o plantilla, abrí primero un issue de propuesta.
- Mantené los cambios acotados: una corrección, una mejora o una decisión de diseño por pull request.

## Entorno de desarrollo

Se necesita Windows, el SDK indicado en `global.json` y soporte WPF.

```powershell
dotnet build .\NumericSelector.slnx --configuration Release
dotnet run --project .\NumericSelector.Demo\NumericSelector.Demo.csproj
```

La aplicación `NumericSelector.Demo` es el banco de pruebas manual de la experiencia del control.

## Criterios para un pull request

- La solución debe compilar en `Release` sin advertencias nuevas.
- Agregá o actualizá pruebas automatizadas cuando el cambio altere comportamiento verificable.
- Probá manualmente los gestos afectados en el demo: mouse, rueda, teclado, foco y modo `IsDisplayOnly`.
- Conservá la compatibilidad de binding y de las propiedades de dependencia salvo que el issue acuerde una ruptura de API.
- Actualizá `README.md` y `CHANGELOG.md` si cambia la API pública, un gesto o un límite conocido.
- Usá nombres claros, comentarios sólo donde expliquen una decisión no obvia y formato consistente con el código existente.

## Áreas especialmente sensibles

- Coerciones de `Minimum`, `Maximum`, `Value`, pasos y `ResetValue`.
- Medición de texto, cultura, fuentes y el compromiso de no recortar el valor.
- Reaplicación de plantillas y personalización mediante estilos WPF.
- Captura de mouse, foco y convivencia con `ScrollViewer`.

## Reportar errores

Incluí, en lo posible:

- versión de .NET y Windows;
- fragmento mínimo de XAML/C# que reproduzca el problema;
- valores de las propiedades relevantes;
- comportamiento esperado, comportamiento observado y una captura si aporta contexto.

Al contribuir aceptás que tus aportes se distribuyan bajo la licencia MIT del proyecto.
