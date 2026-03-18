using System.ComponentModel;
using NationalInstruments.TestStand.SemiconductorModule;

namespace STSOperatorTool
{
	internal interface ISiteInformation
	{
		int SiteNumber { get; set; }
		long Passed { get; }
		long Failed { get; }
		long Other { get; }
		long Total { get; }
		double Yield { get; }
		double FailedPercentage { get; }
		IPartCountStatistics PartCountStatistics { get; set; }
	}

	internal class SiteInformation : ISiteInformation, INotifyPropertyChanged
	{
		public int SiteNumber { get; set; }
		public long Passed { get { return PartCountStatistics == null ? 0 : PartCountStatistics.Passed; } }
		public long Failed { get { return PartCountStatistics == null ? 0 : PartCountStatistics.Failed; } }
		public long Other { get { return PartCountStatistics == null ? 0 : PartCountStatistics.Other; } }
		public long Total { get { return PartCountStatistics == null ? 0 : PartCountStatistics.Total; } }
		public double Yield { get { return PartCountStatistics == null ? 0 : (PartCountStatistics.Total == 0 ? 0 : ((double)PartCountStatistics.Passed / PartCountStatistics.Total)) * 100; } }
		public double FailedPercentage { get { return PartCountStatistics == null ? 0 : (PartCountStatistics.Total == 0 ? 0 : ((double)PartCountStatistics.Failed / PartCountStatistics.Total)) * 100; } }

		public SiteInformation (int siteNumber)
		{
			SiteNumber = siteNumber;
		}

		private IPartCountStatistics mPartCountStatistics;
		public IPartCountStatistics PartCountStatistics
		{
			get { return mPartCountStatistics; }
			set
			{
				mPartCountStatistics = value;
				OnPropertyChanged("Passed");
				OnPropertyChanged("Failed");
				OnPropertyChanged("Other");
				OnPropertyChanged("Total");
				OnPropertyChanged("Yield");
				OnPropertyChanged("FailedPercentage");
			}
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}