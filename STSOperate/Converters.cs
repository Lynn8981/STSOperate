using System;
using System.Globalization;
using System.Windows.Data;

namespace STSOperatorTool
{
	// Uses the list of failure analysis ranges defined in the ViewModel to convert from a failure rate to the opacity defined
	// for the particular failure rate.
	public class FailureRateToOpacityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			foreach(var range in ViewModel.FailureAnalysisRangesList)
			{
				if (range.RangeContains((double)value))
				{
					return range.Opacity;
				}
			}

			return 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion(typeof(bool), typeof(bool))]
	public class InvertBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool original = (bool)value;
			return !original;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool original = (bool)value;
			return !original;
		}
	}

	[ValueConversion(typeof(double), typeof(string))]
	public class PercentageConverter : IValueConverter
	{
		private static Utilities mUtilities;

		public static void Initialize(Utilities utilities)
		{
			mUtilities = utilities;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var original = (double)value;
			var converted = mUtilities.GetFormattedValue(original);
			return converted;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
