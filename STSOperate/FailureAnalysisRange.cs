using System;
using System.ComponentModel;

namespace STSOperatorTool
{
	class FailureAnalysisRange : INotifyPropertyChanged
	{
		private readonly double mLowValue;
		private readonly double mHighValue;

		private readonly Utilities mUtilities;

		public string Name { get; }
		public double Opacity { get; }
		public string RangeString
		{
			get
			{
				if (double.IsPositiveInfinity(mHighValue))
				{
					string resourceString = mUtilities.GetStringResource("LEGEND_HIGH_FAILURE_RANGE");
					return resourceString.Replace("%1", mLowValue.ToString());
				}
				else if (double.IsNegativeInfinity(mLowValue))
				{
					string resourceString = mUtilities.GetStringResource("LEGEND_FAILURE_RANGE");
					return resourceString.Replace("%1", 0.ToString()).Replace("%2", mHighValue.ToString());
				}
				else
				{
					string resourceString = mUtilities.GetStringResource("LEGEND_FAILURE_RANGE");
					return resourceString.Replace("%1", mLowValue.ToString()).Replace("%2", mHighValue.ToString());
				}
			}
		}

		public FailureAnalysisRange(Utilities utilities, string name, double opacity, double lowValue = double.NegativeInfinity, double highValue = double.PositiveInfinity)
		{
			mUtilities = utilities;

			if (lowValue > highValue)
			{
				throw new ArgumentException("High limit must be greater than low limit.");
			}

			if (opacity < 0 || opacity > 1)
			{
				throw new ArgumentException("Opacity value must be between 0 and 1.");
			}

			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			// If resource string not found, will simply return name unchanged.
			Name = mUtilities.GetStringResource(name);
			Opacity = opacity;
			mLowValue = lowValue;
			mHighValue = highValue;

			OnPropertyChanged("Name");
			OnPropertyChanged("Opacity");
			OnPropertyChanged("RangeString");
		}

		public bool RangeContains(double value)
		{
			return value >= mLowValue && value <= mHighValue;
		}

		#region INotifyPropertyChanged OnPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
