namespace NumericSelector
{
	/// <summary>
	/// Cómo se comporta el ancho del control frente a su contenido.
	/// </summary>
	public enum StretchMode
	{
		/// <summary>
		/// Ancho fijo (el de la propiedad Width). El texto que no entra se recorta con
		/// puntos suspensivos. Es lo más predecible cuando hay varios controles alineados.
		/// </summary>
		Fixed,

		/// <summary>
		/// El control se ensancha lo necesario para que entre el contenido y **nunca vuelve
		/// a achicarse**, aunque el contenido se acorte. Es el comportamiento heredado del
		/// LNSlider de VB6 ("Only Width stretching is allowed"). En este modo Width pasa a
		/// ser Auto y MinWidth actúa como piso; si se fija Width en la instancia, ese valor
		/// manda y el control no puede crecer.
		/// </summary>
		AutoGrow
	}
}
