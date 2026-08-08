using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Checks that the template contract declared with [TemplatePart] and the default template in
/// Themes/Generic.xaml say the same thing.
/// </summary>
/// <remarks>
/// The pairing is the kind that breaks in silence: renaming a part in the XAML compiles, and
/// the control goes on building because the code-behind checks every part for null — what is
/// lost is the bar reacting to the mouse or the number showing up, and only at run time. The
/// test reads the attributes by reflection instead of listing the names again, so a part added
/// to the contract tomorrow is covered without touching this file.
/// </remarks>
[TestClass]
public class TemplateContractTests
{
	private static readonly TemplatePartAttribute[] DeclaredParts =
		(TemplatePartAttribute[])typeof(BoundedNumericSelector)
			.GetCustomAttributes(typeof(TemplatePartAttribute), inherit: false);

	[TestMethod]
	public void The_control_declares_its_template_contract()
	{
		// If the attributes disappear, the tests below would pass over an empty list and prove
		// nothing at all.
		Assert.AreNotEqual(0, DeclaredParts.Length,
			"The control has to declare the parts its code-behind looks up.");
	}

	[TestMethod]
	public void Every_declared_part_exists_in_the_default_template_with_its_declared_type()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { ShowDetail = true };
			var window = new Window
			{
				Width = 400,
				Height = 200,
				Content = selector,
				ShowInTaskbar = false,
			};

			window.Show();
			Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
			window.UpdateLayout();

			try
			{
				foreach (var part in DeclaredParts)
				{
					object? found = selector.Template.FindName(part.Name, selector);

					Assert.IsNotNull(found,
						$"The default template does not provide the declared part {part.Name}.");
					Assert.IsInstanceOfType(found, part.Type,
						$"The part {part.Name} is a {found.GetType().Name} and the contract announces {part.Type.Name}.");
				}
			}
			finally { window.Close(); }
		});
	}
}
