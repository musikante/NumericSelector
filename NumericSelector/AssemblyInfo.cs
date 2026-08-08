using System.Windows;

// Tells WPF where to look for the generic resource dictionary (Themes/Generic.xaml) holding
// the default style of the control. Without this attribute the template declared in
// Generic.xaml is not applied and the control renders empty.
[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,            // where the theme-specific dictionaries are
	ResourceDictionaryLocation.SourceAssembly   // where the generic dictionary is (this assembly)
)]
