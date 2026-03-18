using System.Collections.Generic;
using System.ComponentModel;
using NationalInstruments.TestStand.SemiconductorModule;

namespace STSOperatorTool
{
	internal interface ITestBin
	{
		ushort BinNumber { get; }
		string BinName { get; }
		BinType BinType { get; }
		ushort HardwareBinNumber { get; }
		long PartCount { get; }
		double PercentOfTotal { get; }
		void UpdateCounts(ISoftwareBinStatistics softwareBinStatistics, long partTotal, Dictionary<int, long> sitePartCounts, Dictionary<int, long> sitePartTotals);
		long GetSitePartCount(int siteIndex);
		double GetBinPercentageOfSiteTotal(int site);
	}

	internal class TestBin : ITestBin, INotifyPropertyChanged
	{
		private ISoftwareBinStatistics mSoftwareBinStatistics;
		private long mPartTotal = 0;
		private Dictionary<int, long> mSitePartCounts;
		private Dictionary<int, long> mSitePartTotals;

		public ushort BinNumber { get { return mSoftwareBinStatistics != null ? mSoftwareBinStatistics.BinNumber : (ushort)0; } }
		public string BinName { get { return mSoftwareBinStatistics != null ? mSoftwareBinStatistics.BinName : string.Empty; } }
		public BinType BinType { get { return mSoftwareBinStatistics != null ? mSoftwareBinStatistics.BinType : BinType.Other; } }
		public ushort HardwareBinNumber { get { return mSoftwareBinStatistics != null ? mSoftwareBinStatistics.HardwareBinNumber : (ushort)0; } }
		public long PartCount { get { return mSoftwareBinStatistics != null ? mSoftwareBinStatistics.PartCount : 0; } }
		public double PercentOfTotal
		{
			get
			{
				return mPartTotal == 0 || mSoftwareBinStatistics == null
					? 0.0
					: ((double) mSoftwareBinStatistics.PartCount / mPartTotal) * 100;
			}
		}

		public long GetSitePartCount(int siteIndex)
		{
			if (mSitePartCounts == null || !mSitePartCounts.ContainsKey(siteIndex))
			{
				return 0;
			}
			return mSitePartCounts[siteIndex];
		}

		public double GetBinPercentageOfSiteTotal(int siteIndex)
		{
			if (mSitePartTotals == null || !mSitePartTotals.ContainsKey(siteIndex) || !mSitePartCounts.ContainsKey(siteIndex))
			{
				return 0.0;
			}
			var sitePartTotal = mSitePartTotals[siteIndex];
			return sitePartTotal == 0 ? 0.0 : (((double)mSitePartCounts[siteIndex] / sitePartTotal) * 100);
		}

		public TestBin(ISoftwareBinStatistics softwareBinStatistics)
		{
			mSoftwareBinStatistics = softwareBinStatistics;
		}

		public void UpdateCounts(ISoftwareBinStatistics softwareBinStatistics, long partTotal, Dictionary<int, long> sitePartCounts, Dictionary<int, long> sitePartTotals)
		{
			mSoftwareBinStatistics = softwareBinStatistics;
			mPartTotal = partTotal;
			mSitePartCounts = sitePartCounts;
			mSitePartTotals = sitePartTotals;

			OnPropertyChanged("PartCount");
			OnPropertyChanged("PercentOfTotal");
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}