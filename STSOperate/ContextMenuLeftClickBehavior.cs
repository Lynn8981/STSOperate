using System.Windows;
using System.Windows.Controls.Primitives;

namespace STSOperatorTool
{
	public static class ContextMenuLeftClickBehavior
	{
		public static bool GetIsLeftClickEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsLeftClickEnabledProperty);
		}

		public static void SetIsLeftClickEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsLeftClickEnabledProperty, value);
		}

		public static readonly DependencyProperty IsLeftClickEnabledProperty = DependencyProperty.RegisterAttached(
			"IsLeftClickEnabled",
			typeof(bool),
			typeof(ContextMenuLeftClickBehavior),
			new UIPropertyMetadata(false, OnIsLeftClickEnabledChanged));

		private static void OnIsLeftClickEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var uiElement = sender as UIElement;

			if (uiElement != null)
			{
				bool isEnabled = e.NewValue is bool value && value;

				if (isEnabled)
				{
					if (uiElement is ButtonBase)
						((ButtonBase)uiElement).Click += OnMouseLeftButtonUp;
					else
						uiElement.MouseLeftButtonUp += OnMouseLeftButtonUp;
				}
				else
				{
					if (uiElement is ButtonBase)
						((ButtonBase)uiElement).Click -= OnMouseLeftButtonUp;
					else
						uiElement.MouseLeftButtonUp -= OnMouseLeftButtonUp;
				}
			}
		}

		private static void OnMouseLeftButtonUp(object sender, RoutedEventArgs e)
		{
			var fe = sender as FrameworkElement;
			if (fe != null)
			{
				if (fe.ContextMenu != null)
				{
					// Make sure the ContextMenu is placed below the button.
					if (fe is ButtonBase)
					{ 
						fe.ContextMenu.Placement = PlacementMode.Bottom;
						fe.ContextMenu.PlacementTarget = fe;
					}
				}

				fe.ContextMenu.IsOpen = true;
			}
		}

	}
}
