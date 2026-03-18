using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace STSOperatorTool
{
	/// <summary>
	/// Interaction logic for SemiconductorModuleSlider.xaml
	/// </summary>
	public partial class SemiconductorModuleSlider : UserControl, INotifyPropertyChanged
	{
		public SemiconductorModuleSlider()
		{
			SizeChanged += Slider_SizeChanged;
			InitializeComponent();
		}

		public static readonly DependencyProperty ExpectedValueProperty = DependencyProperty.Register(
		  "ExpectedValue",
		  typeof(double),
		  typeof(SemiconductorModuleSlider),
		  new PropertyMetadata(default(double), UpdateRectangleWidths)
		);

		public double ExpectedValue
		{
			get
			{
				var val = GetValue(ExpectedValueProperty);
				return val == null ? 0 : (double)val;
			}
			set
			{
				SetValue(ExpectedValueProperty, value);
			}
		}

		public static readonly DependencyProperty ExpectedMarginProperty = DependencyProperty.Register(
		  "ExpectedMargin",
		  typeof(double),
		  typeof(SemiconductorModuleSlider),
		  new PropertyMetadata(default(double), UpdateRectangleWidths)
		);
		public double ExpectedMargin
		{
			get
			{
				var val = GetValue(ExpectedMarginProperty);
				return val == null ? 0 : (double)val;
			}
			set
			{
				SetValue(ExpectedMarginProperty, value);
			}
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		  "Value",
		  typeof(double),
		  typeof(SemiconductorModuleSlider),
		  new PropertyMetadata(default(double), UpdateRectangleWidths)
		);

		public double Value
		{
			get
			{
				var val = GetValue(ValueProperty);
				return val == null ? 0 : (double)val;
			}
			set
			{
				SetValue(ValueProperty, value);
			}
		}

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
		 "MinValue",
		 typeof(double),
		 typeof(SemiconductorModuleSlider),
		 new PropertyMetadata(default(double), UpdateRectangleWidths)
	   );

		public double MinValue
		{
			get
			{
				var val = GetValue(MinValueProperty);
				return val == null ? 0 : (double)val;
			}
			set
			{
				SetValue(MinValueProperty, value);
			}
		}

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
		  "MaxValue",
		  typeof(double),
		  typeof(SemiconductorModuleSlider),
		  new PropertyMetadata(default(double), UpdateRectangleWidths)
		);

		public double MaxValue
		{
			get
			{
				var val = GetValue(MaxValueProperty);
				return val == null ? 0 : (double)val;
			}
			set
			{
				SetValue(MaxValueProperty, value);
			}
		}

		public double ValueDivisor
		{
			get
			{
				return (MaxValue > 0) ? MaxValue : 100;
			}
		}

		public double ValueRectangleWidth
		{
			get
			{
				return ActualWidth * Math.Min(1, Value / ValueDivisor);
			}
		}

		public Thickness ValueRectangleMargin
		{
			get
			{
				return new Thickness(ActualWidth * Math.Min(1, MinValue / ValueDivisor),0,0,0);
			}
		}

		public double ExpectedRangeLow
		{
			get
			{
				return ActualWidth * Math.Min(1, (ExpectedValue - ExpectedMargin) / ValueDivisor);
			}
		}

		public double ExpectedRangeWidth
		{
			get
			{
				return ActualWidth * (ExpectedMargin * 2) / ValueDivisor;
			}
		}

		private static void UpdateRectangleWidths(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((SemiconductorModuleSlider)d).UpdateRectangleWidths();
		}

		private void UpdateRectangleWidths()
		{
			OnPropertyChanged("ValueRectangleWidth");
			OnPropertyChanged("ExpectedRangeLow");
			OnPropertyChanged("ExpectedRangeWidth");
			OnPropertyChanged("ValueRectangleMargin");
		}

		private void Slider_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateRectangleWidths();
		}

		#region INotifyPropertyChanged OnPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
