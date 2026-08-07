# Política de seguridad

## Versiones compatibles

Mientras el proyecto no tenga versiones publicadas, la rama principal de desarrollo es la única versión considerada para correcciones de seguridad.

| Versión | Recibe correcciones de seguridad |
| --- | --- |
| Desarrollo actual | Sí |
| Versiones publicadas futuras | Se informará al publicar cada versión |

## Reportar una vulnerabilidad

No publiques vulnerabilidades potenciales en issues públicos. Enviá una descripción privada al mantenedor del repositorio mediante el canal de contacto que se configure al publicar el proyecto en GitHub.

El informe debería incluir:

- una explicación del impacto;
- pasos mínimos para reproducirlo;
- versión, sistema operativo y runtime afectados;
- una posible mitigación, si la conocés.

Se confirmará la recepción, se evaluará el problema y se coordinará la divulgación una vez que exista una corrección o mitigación razonable.

## Alcance actual

`BoundedNumericSelector`, el control que vive en el ensamblado `NumericSelector`, es un control de interfaz WPF sin servicios de red, persistencia ni manejo de credenciales propios. Aun así, se revisarán reportes relacionados con denegación de servicio en layout, uso inseguro desde XAML y dependencias de compilación o distribución.
