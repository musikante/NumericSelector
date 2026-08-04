namespace NumericSelector
{
	/// <summary>
	/// Cómo se presenta la etiqueta del título. Reemplaza al par de propiedades
	/// booleanas que había antes (mostrar el título / enmarcarlo): con dos booleanos, el
	/// del marco quedaba inerte cuando el título no se mostraba, y una propiedad que a
	/// veces no hace nada obliga a leer la documentación para no llevarse una sorpresa.
	/// Acá los tres estados posibles están enumerados y todos hacen algo siempre.
	/// </summary>
	public enum TitleMode
	{
		/// <summary>Sin título: el control es sólo la fila de datos (por defecto).</summary>
		Hidden,

		/// <summary>Título con su marco y su fondo, integrado al rectángulo del control.</summary>
		Framed,

		/// <summary>
		/// Título sin marco ni fondo, como una etiqueta suelta por encima de la fila de
		/// datos. No cambia la geometría: el grosor del borde se sigue reservando, así que
		/// alternar entre <see cref="Framed"/> y <see cref="Frameless"/> no mueve nada.
		/// </summary>
		Frameless
	}
}
