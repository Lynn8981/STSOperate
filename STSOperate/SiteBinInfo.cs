using System.ComponentModel;

namespace STSOperatorTool
{
	public class SiteBinInfo : INotifyPropertyChanged
	{
		private readonly Utilities mUtilities;
		public ushort Bin { get; }
		public string Site { get; }
		public string TooltipText
		{
			get
			{
				return
					$"{mUtilities.GetStringResource("TABLE_BIN_NUMBER")} : {Bin}, {mUtilities.GetStringResource("TABLE_SITE")} : {Site}\n" +
					$"{mUtilities.GetStringResource("FAILURES")} : {FailureCount}\n" +
					$"{mUtilities.GetStringResource("FAILURE_RATE")} : {FailurePercentage.ToString("00.0")}%";
			}
		}

		private bool mUpdateTooltips;
		public bool UpdateTooltips
		{
			get { return mUpdateTooltips; }
			set
			{
				mUpdateTooltips = value;
				if (mUpdateTooltips)
				{
					OnPropertyChanged("TooltipText");
				}
			}
		}

		private long mFailureCount = 0;
		public long FailureCount
		{
			get { return mFailureCount; }
			set
			{
				mFailureCount = value;
				OnPropertyChanged("FailureCount");
				if (UpdateTooltips)
				{
					OnPropertyChanged("TooltipText");
				}
			}
		}

		private double mFailureRate = 0;
		public double FailurePercentage
		{
			get { return mFailureRate; }
			set
			{
				mFailureRate = value;
				OnPropertyChanged("FailurePercentage");
				if (UpdateTooltips)
				{
					OnPropertyChanged("TooltipText");
				}
			}
		}

		public SiteBinInfo(Utilities utilities, ushort binNumber, string site)
		{
			mUtilities = utilities;
			Bin = binNumber;
			Site = site != null ? site : "";
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
