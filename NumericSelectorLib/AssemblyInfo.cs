using System.Windows;

// Indica a WPF dónde buscar el diccionario de recursos genérico (Themes/Generic.xaml)
// que contiene el estilo por defecto del control. Sin este atributo, la plantilla
// declarada en Generic.xaml no se aplica y el control se renderiza vacío.
[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,            // dónde están los diccionarios específicos de tema
	ResourceDictionaryLocation.SourceAssembly   // dónde está el diccionario genérico (este ensamblado)
)]
