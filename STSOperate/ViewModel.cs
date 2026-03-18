using System;
using System.Collections.Generic;
using NationalInstruments.TestStand.SemiconductorModule;
using NationalInstruments.TestStand.Interop.API;
using NationalInstruments.TestStand.Interop.UI;
using NationalInstruments.TestStand.Interop.UI.Ax;
using NationalInstruments.TestStand.Utility;
using System.ComponentModel;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Data;
using CommandType = NationalInstruments.TestStand.SemiconductorModule.CommandType;
using System.Windows;
using static STSOperatorTool.UserLevel;

namespace STSOperatorTool
{
	public enum YieldSelectorItems
	{
		LotYieldItem,
		WaferYieldItem,
		LastNYieldItem
	}

	internal interface IViewModel : INotifyPropertyChanged
	{
		ISemiconductorModuleCommandHandler ExitCommand { get; }
		ISemiconductorModuleCommandHandler LoginLogoutCommand { get; }
		ISemiconductorModuleCommandHandler ConfigureLotCommand { get; }
		ISemiconductorModuleCommandHandler ConfigureStationCommand { get; }
		ISemiconductorModuleCommandHandler RunOnceCommand { get; }
		ISemiconductorModuleCommandHandler StartResumeLotCommand { get; }
		ISemiconductorModuleCommandHandler PauseLotCommand { get; }
		ISemiconductorModuleCommandHandler EndLotCommand { get; }
		ISemiconductorModuleCommandHandler ViewMidLotSummaryCommand { get; }
		ISemiconductorModuleCommandHandler ViewReportCommand { get; }
		ISemiconductorModuleCommandHandler OpenSTSMaintenanceSoftwareCommand { get; }
		event EventHandler ApplicationMgrExited;
		YieldSelectorItems YieldSelectorItem { get; set; }
		ISiteInformation AllSitePartCountStatistics { get; }
		ISiteInformation WindowSitePartCountStatistics { get; }
		ISiteInformation InlineQAPartCountStatistics { get; }
		ISiteInformation WaferPartCountStatistics { get; }
		DataTable SiteBinDataTable { get; set; }
		int FailureRateTableCellWidth { get; }
		int AllSitesFailureRateTableCellWidth { get; }
		ObservableCollection<FailureAnalysisRange> FailureAnalysisRanges { get; }
		ObservableCollection<ITestProgramProperty> TestProgramInfoCollection { get; }
		Visibility ShowMoreVisible { get; }
		bool QAWarning { get; }
		double TimingSliderMaximum { get; set; }
		double AverageCycleTime { get; }
		double AverageSocketTime { get; }
		double AverageIndexTime { get; }
		string TesterStatus { get; }
		bool InlineQAEnabled { get; }
		Visibility VisibilityWhenUserIsElevated { get; }
		Visibility VisibilityWhenUserIsNotElevated { get; }
		Visibility VisibilityWhenUserCanElevate { get; }
		string UserLevelDisplayName { get; }
		bool AdministratorMode { get; }
		string LoginLogoutCommandText { get; }
		bool LoggedIn { get; }
		bool LotConfigured { get; }
		Visibility WaferVisibility { get; }
		double WaferRowHeight { get; }
		string LastNString { get; }

		void RevertUser();
		bool RequestUserElevation(User user);
		void RefreshContextMenuVisibilities();
	}

	internal class ViewModel : IViewModel
	{
		private readonly ISemiconductorModuleManager mSemiconductorModuleManager;
		private readonly List<ITestProgramProperty> mTestProgramInfo = new List<ITestProgramProperty>();
		private ObservableCollection<ISiteInformation> mSitePartCount { get; } = new ObservableCollection<ISiteInformation>();
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		private ObservableCollection<ITestBin> mTestBins { get; set; } = new ObservableCollection<ITestBin>();
		private readonly DispatcherTimer mUpdateTimer;		
		private IObserver mSemiconductorModuleManagerObserver;
		private readonly Dictionary<int, int> mBinNumberToDataTableRow = new Dictionary<int, int>();
		private static string sLogoutString;
		private readonly AxApplicationMgr mAxApplicationMgr;
		private AxSequenceFileViewMgr mAxSequenceFileViewMgr;
		private string mTesterStatus;
		private bool mInlineQAEnabled;

		// Need 2 different user levels, because TestStand does not allow logging out without restarting
		// an execution & losing all data. When user level is elevated, we just check the credentials
		// of the elevated user, and the user logged into TestStand stays the same.
		private UserLevels? mLoggedInUserLevel;
		private string mLoggedInUsername;
		private UserLevels? mElevatedUserLevel;
		private string mElevatedUsername;

		private bool UserIsElevated
		{
			get { return mElevatedUserLevel != null; }
		}

		// Returns either the TestStand logged in user level or the elevated user level, if set.
		private UserLevels? UserLevel
		{
			get { return mElevatedUserLevel != null ? mElevatedUserLevel : mLoggedInUserLevel; }
		}

		public ISemiconductorModuleCommandHandler LoginLogoutCommand { get; private set; }

		public ISemiconductorModuleCommandHandler ExitCommand { get; private set; }
		public ISemiconductorModuleCommandHandler ConfigureLotCommand { get; private set; }
		public ISemiconductorModuleCommandHandler ConfigureStationCommand { get; private set; }
		public ISemiconductorModuleCommandHandler RunOnceCommand { get; private set; }
		public ISemiconductorModuleCommandHandler StartResumeLotCommand { get; private set; }
		public ISemiconductorModuleCommandHandler PauseLotCommand { get; private set; }
		public ISemiconductorModuleCommandHandler EndLotCommand { get; private set; }
		public ISemiconductorModuleCommandHandler ViewMidLotSummaryCommand { get; private set; }
		public ISemiconductorModuleCommandHandler ViewReportCommand { get; private set; }
		public ISemiconductorModuleCommandHandler OpenSTSMaintenanceSoftwareCommand { get; private set; }

		public Utilities Utilities { get; private set; }

		public event EventHandler ApplicationMgrExited;

		public YieldSelectorItems YieldSelectorItem { get; set; }

		public ISiteInformation AllSitePartCountStatistics { get; } = new SiteInformation(-1);
		public ISiteInformation WindowSitePartCountStatistics { get; } = new SiteInformation(-1);
		public ISiteInformation InlineQAPartCountStatistics { get; } = new SiteInformation(-1);
		public ISiteInformation WaferPartCountStatistics { get; } = new SiteInformation(-1);
		public DataTable SiteBinDataTable { get; set; }

		public int FailureRateTableCellWidth
		{
			get { return (mSitePartCount.Count <= 16) ? 80 : 20; }
		}

		public int AllSitesFailureRateTableCellWidth
		{
			get { return (mSitePartCount.Count <= 16) ? 160 : 80; }
		}

		public ListCollectionView SitePartCountStatisticsView { get; set; }
		public ListCollectionView TestBinsFailurePercentageSortView { get; set; }
		
		public static List<FailureAnalysisRange> FailureAnalysisRangesList = new List<FailureAnalysisRange>();

		public ObservableCollection<FailureAnalysisRange> FailureAnalysisRanges
		{
			get
			{
				return new ObservableCollection<FailureAnalysisRange>(FailureAnalysisRangesList);
			}
		}

		public ObservableCollection<ITestProgramProperty> TestProgramInfoCollection
		{
			get
			{
				return new ObservableCollection<ITestProgramProperty>(mTestProgramInfo);
			}
		}

		public Visibility ShowMoreVisible
		{
			get
			{
				return TestProgramInfoCollection.Count > 9 ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public bool QAWarning
		{
			get
			{
				return InlineQAPartCountStatistics.Failed > 0;
			}
		}

		public double TimingSliderMaximum { get; set; } = 0;

		public double AverageCycleTime
		{
			get
			{
				double windowAverageCycleTime = mSemiconductorModuleManager.AllSiteLotStatistics.GetWindowAverageCycleTime();
				return Double.IsNaN(windowAverageCycleTime) ? 0.0 : windowAverageCycleTime;
			}
		}

		public double AverageSocketTime
		{
			get
			{
				double windowAverageSocketTime = mSemiconductorModuleManager.AllSiteLotStatistics.GetWindowAverageSocketTime();
				return Double.IsNaN(windowAverageSocketTime) ? 0.0 : windowAverageSocketTime;
			}
		}

		public double AverageIndexTime
		{
			get
			{
				var indexTime = AverageCycleTime - AverageSocketTime;
				return indexTime > 0 ? indexTime : 0;
			}
		}

		public string TesterStatus
		{
			get { return mTesterStatus; }
			private set
			{
				mTesterStatus = value;
				OnPropertyChanged("TesterStatus");
			}
		}

		public bool InlineQAEnabled
		{
			get { return mInlineQAEnabled; }
			private set
			{
				mInlineQAEnabled = value;
				OnPropertyChanged("InlineQAEnabled");
			}
		}

		public Visibility VisibilityWhenUserIsElevated
		{
			get { return UserIsElevated ? Visibility.Visible : Visibility.Collapsed; }
		}

		public Visibility VisibilityWhenUserIsNotElevated
		{
			get { return !UserIsElevated ? Visibility.Visible : Visibility.Collapsed; }
		}

		public Visibility VisibilityWhenUserCanElevate
		{
			get
			{
				Visibility visibility = Visibility.Collapsed;
				if (!UserIsElevated)
				{
					// UserLevels can define whether or not they can be elevated (e.g. Administrators cannot be elevated)
					visibility =  mLoggedInUserLevel?.CanElevate() == true ? Visibility.Visible : Visibility.Collapsed;
				}
				return visibility;
			}
		}

		public string UserLevelDisplayName
		{
			get
			{
				if (UserLevel != null)
				{
					string username = UserIsElevated ? mElevatedUsername : mLoggedInUsername;

					string userLevelString = string.Empty;

					switch (UserLevel)
					{
						case UserLevels.Administrator:
							userLevelString += Utilities.GetStringResource(UserLevels.Administrator.ToString().ToUpper());
							break;
						case UserLevels.Operator:
							userLevelString += Utilities.GetStringResource(UserLevels.Operator.ToString().ToUpper());
							break;
					}

					if (string.IsNullOrEmpty(username))
					{
						return userLevelString;
					}
					else
					{
						return username + " (" + userLevelString + ")";
					}
				}

				return string.Empty;
			}
		}

		public bool AdministratorMode
		{
			get
			{
				bool userIsAdministrator = UserLevel != null && UserLevel == UserLevels.Administrator;
				return userIsAdministrator || Utilities.UserPrivilegeCheckingIsDisabled();
			}
		}

		public string LoginLogoutCommandText
		{
			get
			{
				return LoginLogoutCommand.SemiconductorModuleCommandText == sLogoutString ? Utilities.GetStringResource("LOG_OUT") :
																							Utilities.GetStringResource("LOG_IN");
			}
		}

		public bool LoggedIn
		{
			get
			{
				return LoginLogoutCommand.SemiconductorModuleCommandText == sLogoutString;
			}
		}

		public bool LotConfigured
		{
			get { return mSemiconductorModuleManager.LotSettings != null; }
		}

		public Visibility WaferVisibility
		{
			get
			{
				return WaferInUse() ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public double WaferRowHeight
		{
			get
			{
				return WaferInUse() ? 56 : 0;
			}
		}

		public string LastNString
		{
			get
			{
				var windowSize = mSemiconductorModuleManager.AllSiteLotStatistics.PartCountWindowSize;
				var lastNString = Utilities.GetStringResource("LAST_N");
				string windowSizeString = windowSize > 0 ? windowSize.ToString() : "N";
				return lastNString.Replace("%1", windowSizeString);
			}
		}

		public void RevertUser()
		{
			mElevatedUserLevel = null;
			mElevatedUsername = null;
			RefreshLoginLogout();
		}

		public bool RequestUserElevation(User user)
		{
			// If the elevated user level isn't null, we are already elevated and will not elevate again
			// Also, don't allow elevation if no user is logged in
			if (mElevatedUserLevel == null && mLoggedInUserLevel != null)
			{
				UserLevels requestedUserLevel = GetUserLevel(user);
				bool elevationIsValid = mLoggedInUserLevel?.CanElevateToUserLevel(requestedUserLevel) ?? false;
				if (elevationIsValid)
				{
					mElevatedUserLevel = requestedUserLevel;
					mElevatedUsername = user.LoginName;

					RefreshLoginLogout();

					return true;
				}
			}

			return false;
		}

		public void RefreshContextMenuVisibilities()
		{
			OnPropertyChanged("VisibilityWhenUserIsElevated");
			OnPropertyChanged("VisibilityWhenUserCanElevate");
			OnPropertyChanged("VisibilityWhenUserIsNotElevated");
		}

		public ViewModel(AxApplicationMgr applicationMgr, AxSequenceFileViewMgr sequenceFileViewMgr, ISemiconductorModuleManager manager = null)
		{
			mAxSequenceFileViewMgr = sequenceFileViewMgr;

			mAxApplicationMgr = applicationMgr;
			mAxApplicationMgr.ExitApplication += AxApplicationMgrOnExitApplication;
			mAxApplicationMgr.UserChanged += AxApplicationMgrOnUserChanged;
			mAxApplicationMgr.Start();

			var engine = applicationMgr.GetEngine();
			Utilities = new Utilities(engine);

			mSemiconductorModuleManager = manager ??
				SemiconductorModuleManagerFactory.NewSemiconductorModuleManager(engine, (SequenceFileViewMgr)mAxSequenceFileViewMgr.GetOcx());
			
			mSemiconductorModuleManager.EndLotOnCodeModuleRuntimeError = false;
			mSemiconductorModuleManager.DisplayDialogOnCodeModuleRuntimeError = false;
			sLogoutString = Utilities.GetStringResource("FILE_LOGOUT", "TSUI_COMMANDS").Replace("&", "");

			ConnectCommands();

			UpdateTesterStatus();

			mSemiconductorModuleManagerObserver = mSemiconductorModuleManager.CreateObserver();
			mSemiconductorModuleManagerObserver.TesterStatusChanged += SemiconductorModuleManagerOnTesterStatusChanged;
			mSemiconductorModuleManagerObserver.LotSettingsChanged += SemiconductorModuleManagerOnLotSettingsChanged;
			mSemiconductorModuleManagerObserver.StationSettingsChanged += SemiconductorModuleManagerOnStationSettingsChanged;

			mSemiconductorModuleManager.GetCommand(CommandType.LoginLogout).TextChanged += RefreshLoginLogout;

			ConfigureData();
			SetupCollectionViewsAndCollectionSynchronization();
			RefreshLoginLogout();
			PopulateLegend();

			mUpdateTimer = new DispatcherTimer();
			mUpdateTimer.Interval = TimeSpan.FromMilliseconds(2000);
			mUpdateTimer.Tick += Update;
			mUpdateTimer.Start();
		}

		// Populates all of the different ranges in the legend. This determines number of entries, range of each entry, and opacity
		// value for each entry.
		private void PopulateLegend()
		{
			// Excluding the high limit means there is no upper limit.
			FailureAnalysisRangesList.Add(new FailureAnalysisRange(Utilities, name: "LEGEND_HIGH", opacity: 1, lowValue: 15));
			FailureAnalysisRangesList.Add(new FailureAnalysisRange(Utilities, name: "LEGEND_MEDIUM", opacity: 0.6, lowValue: 5, highValue: 15));
			FailureAnalysisRangesList.Add(new FailureAnalysisRange(Utilities, name: "LEGEND_LOW", opacity: 0.2, lowValue: 2, highValue: 5));
			// Excluding the low limit means there is no lower limit.
			FailureAnalysisRangesList.Add(new FailureAnalysisRange(Utilities, name: "LEGEND_VERY_LOW", opacity: 0, highValue: 2));

			OnPropertyChanged("FailureAnalysisRanges");
		}

		private bool WaferInUse()
		{
			return mSemiconductorModuleManager != null && mSemiconductorModuleManager.BatchRuntimeData != null && mSemiconductorModuleManager.BatchRuntimeData.OnWafer;
		}

		private void RefreshLoginLogout()
		{
			OnPropertyChanged("UserLevelDisplayName");
			OnPropertyChanged("AdministratorMode");

			OnPropertyChanged("LoggedInUser");
			OnPropertyChanged("LoginLogoutCommandText");
			OnPropertyChanged("LoggedIn");
		}

		internal void Update(object sender, EventArgs eventArgs)
		{
			// update stats for yield card
			AllSitePartCountStatistics.PartCountStatistics = mSemiconductorModuleManager.AllSiteLotStatistics.PartCountStatistics;
			WindowSitePartCountStatistics.PartCountStatistics = mSemiconductorModuleManager.AllSiteLotStatistics.WindowPartCountStatistics;
			InlineQAPartCountStatistics.PartCountStatistics = mSemiconductorModuleManager.AllSiteLotStatistics.InlineQAPartCountStatistics;
			WaferPartCountStatistics.PartCountStatistics = mSemiconductorModuleManager.WaferAllSiteLotStatistics.PartCountStatistics;

			ILotStatistics[] siteLotStatisticsArray = (YieldSelectorItem == YieldSelectorItems.WaferYieldItem) ?
					mSemiconductorModuleManager.GetWaferSiteLotStatistics() : mSemiconductorModuleManager.GetSiteLotStatistics();
			
			// update per site graph
			lock (mSitePartCount)
			{
				int workingSiteCount = Math.Min(siteLotStatisticsArray.Length, mSitePartCount.Count);
				for (int site = 0; site < workingSiteCount; site++)
				{
					ILotStatistics siteLotStatistics = siteLotStatisticsArray[site];
					IPartCountStatistics sitePartCountStatistics = (YieldSelectorItem == YieldSelectorItems.LastNYieldItem) ?
						siteLotStatistics.WindowPartCountStatistics : siteLotStatistics.PartCountStatistics;
					mSitePartCount[site].PartCountStatistics = sitePartCountStatistics;
				}

				// update bin graph
				lock (mTestBins)
				{
					foreach (ITestBin testBin in mTestBins)
					{
						ISoftwareBinStatistics allSiteSoftwareBinStatistics;
						IPartCountStatistics allSiteBinPartCountStatistics;
						Dictionary<int, long> sitePartCounts = new Dictionary<int, long>();
						Dictionary<int, long> sitePartTotals = new Dictionary<int, long>();
						if (YieldSelectorItem == YieldSelectorItems.LastNYieldItem)
						{
							for (int siteIndex = 0; siteIndex < workingSiteCount; siteIndex++)
							{
								sitePartCounts[siteIndex] = siteLotStatisticsArray[siteIndex].GetWindowSoftwareBinStatistics(testBin.BinNumber).PartCount;
								sitePartTotals[siteIndex] = mSitePartCount[siteIndex].Total;
							}
							allSiteSoftwareBinStatistics = mSemiconductorModuleManager.AllSiteLotStatistics.GetWindowSoftwareBinStatistics(testBin.BinNumber);
							allSiteBinPartCountStatistics = WindowSitePartCountStatistics.PartCountStatistics;
						}
						else
						{
							for (int siteIndex = 0; siteIndex < workingSiteCount; siteIndex++)
							{
								sitePartCounts[siteIndex] = siteLotStatisticsArray[siteIndex].GetSoftwareBinStatistics(testBin.BinNumber).PartCount;
								sitePartTotals[siteIndex] = mSitePartCount[siteIndex].Total;
							}
							allSiteSoftwareBinStatistics = mSemiconductorModuleManager.AllSiteLotStatistics.GetSoftwareBinStatistics(testBin.BinNumber);
							allSiteBinPartCountStatistics = AllSitePartCountStatistics.PartCountStatistics;
						}

						testBin.UpdateCounts(allSiteSoftwareBinStatistics, allSiteBinPartCountStatistics.Total, sitePartCounts, sitePartTotals);

						var rowIndex = mBinNumberToDataTableRow[testBin.BinNumber];
						var allSiteBinInfo = (SiteBinInfo)SiteBinDataTable.Rows[rowIndex][2]; // all sites column
						allSiteBinInfo.FailureCount = testBin.PartCount;
						allSiteBinInfo.FailurePercentage = testBin.PercentOfTotal;

						for (int siteIndex = 0; siteIndex < workingSiteCount; siteIndex++)
						{
							var siteBinInfo = (SiteBinInfo)SiteBinDataTable.Rows[rowIndex][3 + siteIndex];
							siteBinInfo.FailureCount = testBin.GetSitePartCount(siteIndex);
							siteBinInfo.FailurePercentage = testBin.GetBinPercentageOfSiteTotal(siteIndex);
						}
					}
				}
			}

			RescaleTimingSliderTotalWidthIfNeeded();

			OnPropertyChanged("AverageSocketTime");
			OnPropertyChanged("AverageIndexTime");
			OnPropertyChanged("AverageCycleTime");
			OnPropertyChanged("UnitsPerHour");
			OnPropertyChanged("YieldWarning");
			OnPropertyChanged("SitePartCount");
			OnPropertyChanged("TestBins");
			OnPropertyChanged("SiteBinDataTable");
			OnPropertyChanged("QAWarning");
			OnPropertyChanged("InlineQAPartCountStatistics.Failed");
			OnPropertyChanged("WaferVisibility");
			OnPropertyChanged("WaferRowHeight");
		}

		private void SetupCollectionViewsAndCollectionSynchronization()
		{
			SitePartCountStatisticsView = (ListCollectionView)CollectionViewSource.GetDefaultView(mSitePartCount);
			SitePartCountStatisticsView.SortDescriptions.Add(new SortDescription("FailedPercentage", ListSortDirection.Descending));
			SitePartCountStatisticsView.SortDescriptions.Add(new SortDescription("SiteNumber", ListSortDirection.Ascending));

			SitePartCountStatisticsView.IsLiveSorting = true;
			SitePartCountStatisticsView.LiveSortingProperties.Add("FailedPercentage");
			SitePartCountStatisticsView.LiveSortingProperties.Add("SiteNumber");

			TestBinsFailurePercentageSortView = (ListCollectionView)CollectionViewSource.GetDefaultView(mTestBins);
			TestBinsFailurePercentageSortView.SortDescriptions.Add(new SortDescription("PercentOfTotal", ListSortDirection.Descending));
			TestBinsFailurePercentageSortView.SortDescriptions.Add(new SortDescription("BinNumber", ListSortDirection.Ascending));

			TestBinsFailurePercentageSortView.IsLiveSorting = true;
			TestBinsFailurePercentageSortView.LiveSortingProperties.Add("PercentOfTotal");
			TestBinsFailurePercentageSortView.LiveSortingProperties.Add("BinNumber");

			// this allows these observable collections to be updated from a worker thread while still updating the UI thread without running into synchronization problems
			BindingOperations.EnableCollectionSynchronization(mSitePartCount, mSitePartCount);
			BindingOperations.EnableCollectionSynchronization(mTestBins, mTestBins);
		}

		private void RescaleTimingSliderTotalWidthIfNeeded()
		{
			// Constants that set the width of the cycle/socket/index time bars and decide when to rescale them.
			//
			// sliderMaximumTargetRatio - the initial ratio of the Cycle Time value (largest bar) to the TimingSliderMaximum value that
			//							  is set whenever there's a resize.
			// rescaleMarginOfError - the proportion the Average Cycle Time can deviate from its starting value before a rescale occurs
			//
			// To ensure cycle time is never larger than TimingSliderMaximum, make sure sliderMaximumTargetRatio + rescaleMarginOfError < 1
			const double sliderMaximumTargetRatio = 0.75;
			const double rescaleMarginOfError = 0.2;

			// Compute the high and low limits for AverageCycleTime values that do not need a resize
			double averageCycleTimeMidpoint = TimingSliderMaximum * sliderMaximumTargetRatio;
			double averageCycleTimeLowerLimit = averageCycleTimeMidpoint * (1 - rescaleMarginOfError);
			double averageCycleTimeUpperLimit = averageCycleTimeMidpoint * (1 + rescaleMarginOfError);
			bool needsRescaling = (AverageCycleTime < averageCycleTimeLowerLimit) || (AverageCycleTime > averageCycleTimeUpperLimit);

			if (TimingSliderMaximum == 0 || needsRescaling)
			{
				TimingSliderMaximum = sliderMaximumTargetRatio == 0 ? 0 : AverageCycleTime / sliderMaximumTargetRatio;
				OnPropertyChanged("TimingSliderMaximum");
			}
		}

		private void UpdateTesterStatus()
		{
			TesterStatus = Utilities.GetStringResource("SYSTEM_STATUS") + mSemiconductorModuleManager.TesterStatus;
		}

		/*
		 * Connect the Semiconductor Module Manager's commands to the global ICommands in this class.
		 * These ICommands are data bound to their corresponding Semiconductor Module Command Buttons or the
		 * Semiconductor Module Menu in MainWindow.xaml.
		 */
		private void ConnectCommands()
		{
			ExitCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.Exit));
			LoginLogoutCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.LoginLogout));
			ConfigureLotCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.ConfigureLot));
			ConfigureStationCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.ConfigureStation));
			RunOnceCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.PerformSinglePartTest));
			StartResumeLotCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.StartResumeLot));
			PauseLotCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.PauseLot));
			EndLotCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.EndLot));
			ViewMidLotSummaryCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.ViewMidLotSummary));
			ViewReportCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.ViewReport));
			OpenSTSMaintenanceSoftwareCommand = new SemiconductorModuleCommandHandler(mSemiconductorModuleManager.GetCommand(CommandType.OpenSTSMaintenanceSoftware));
		}

		#region ConfigureData
		/*
		 * This function updates the Test Program Info table and refreshes the test bins, sites configured, and all of the
		 * data displayed in the tables. If a lot is not configured, it clears all of the tables and data.
		 * 
		 * This function is only called when the OI starts and when the station/lot settings are changed. It should not
		 * be called in the middle of a lot.
		 */
		private void ConfigureData()
		{
			RefreshTestProgramInfo();
			if (LotConfigured)
			{
				ConfigureSitePartCountStatistics();
				ConfigureTestBins();
			}
			else
			{
				mSitePartCount.Clear();
				mTestBins.Clear();
				SiteBinDataTable = null;
			}
			TimingSliderMaximum = 0;

			OnPropertyChanged("LastNString");
			OnPropertyChanged("ShowMoreVisible");
			OnPropertyChanged("LotConfigured");
		}

		private void CreateDataTable()
		{
			var dataTable = new DataTable();

			AddColumn(dataTable, Utilities.GetStringResource("TABLE_BIN_NUMBER"), typeof(ushort));
			AddColumn(dataTable, Utilities.GetStringResource("TABLE_BIN_NAME"), typeof(string));
			AddColumn(dataTable, Utilities.GetStringResource("TABLE_ALL_SITES"), typeof(SiteBinInfo));
			foreach (var siteInfo in mSitePartCount)
			{
				AddColumn(dataTable, siteInfo.SiteNumber.ToString("00"), typeof(SiteBinInfo));
			}

			int rowIndex = 0;
			foreach (var bin in mTestBins)
			{
				int columnIndex = 0;
				var dataRow = dataTable.NewRow();

				dataRow[columnIndex++] = bin.BinNumber;
				dataRow[columnIndex++] = bin.BinName;
				dataRow[columnIndex++] = new SiteBinInfo(Utilities, bin.BinNumber, Utilities.GetStringResource("TABLE_ALL_SITES")); // all sites column
				foreach (var siteInfo in mSitePartCount)
				{
					dataRow[columnIndex++] = new SiteBinInfo(Utilities, bin.BinNumber, siteInfo.SiteNumber.ToString());
				}
				dataTable.Rows.Add(dataRow);
				mBinNumberToDataTableRow[bin.BinNumber] = rowIndex++;
			}

			SiteBinDataTable = dataTable;
		}

		private static void AddColumn(DataTable dataTable, string name, Type dataType)
		{
			var column = new DataColumn();
			column.ColumnName = name;
			column.DataType = dataType;
			dataTable.Columns.Add(column);
		}

		private void RefreshTestProgramInfo()
		{
			mTestProgramInfo.Clear();

			string[] testProgramInfoPropertyNames;
			string[] testProgramInfoPropertyValues;
			mSemiconductorModuleManager.GetSettingsToDisplay(out testProgramInfoPropertyNames, out testProgramInfoPropertyValues);
			for (int propertyIndex = 0; propertyIndex < testProgramInfoPropertyNames.Length; propertyIndex++)
			{
				string capitalizedTestProgramInfoPropertyName = testProgramInfoPropertyNames[propertyIndex]?.ToUpper();

				mTestProgramInfo.Add(new TestProgramProperty(capitalizedTestProgramInfoPropertyName, testProgramInfoPropertyValues[propertyIndex]));
			}
			InlineQAEnabled = mSemiconductorModuleManager.StationSettings.GetValBoolean("Standard.InlineQAEnabled", PropertyOptions.PropOption_NoOptions);
			OnPropertyChanged("TestProgramInfoCollection");
		}

		private void ConfigureTestBins()
		{
			lock (mTestBins)
			{
				mTestBins.Clear();

				foreach (ushort bin in mSemiconductorModuleManager.AllSiteLotStatistics.GetSoftwareBinNumbers())
				{
					var newBin = new TestBin(mSemiconductorModuleManager.AllSiteLotStatistics.GetSoftwareBinStatistics(bin));
					if (newBin.BinType != BinType.Pass)
					{
						mTestBins.Add(newBin);
					}
				}

				CreateDataTable();
			}
			OnPropertyChanged("TestBinsFailurePercentageSortView");
			OnPropertyChanged("SiteBinDataTable");
		}
  
		private void ConfigureSitePartCountStatistics()
		{
			lock (mSitePartCount)
			{
				mSitePartCount.Clear();

				var siteLotStatistics = mSemiconductorModuleManager.GetSiteLotStatistics();
				for (int siteIndex = 0; siteIndex < siteLotStatistics.Length; siteIndex++)
				{
					mSitePartCount.Add(new SiteInformation(siteLotStatistics[siteIndex].SiteNumber));
				}
			}

			OnPropertyChanged("SitePartCountStatisticsView");
			OnPropertyChanged("FailureRateTableCellWidth");
			OnPropertyChanged("AllSitesFailureRateTableCellWidth");
		}
		#endregion

		#region Event Handlers
		private void SemiconductorModuleManagerOnTesterStatusChanged()
		{
			UpdateTesterStatus();
		}

		private void SemiconductorModuleManagerOnLotSettingsChanged()
		{
			ConfigureData();
		}

		private void SemiconductorModuleManagerOnStationSettingsChanged()
		{
			ConfigureData();
		}

		/*
		 * Called every time someone logs in or out of TestStand.
		 */
		internal void AxApplicationMgrOnUserChanged(object sender, _ApplicationMgrEvents_UserChangedEvent e)
		{
			// This should always be null already, but explicitly ensure there is no elevation if someone
			// logs in or out
			mLoggedInUserLevel = mElevatedUserLevel = null;
			mLoggedInUsername = mElevatedUsername = null;

			// If a user logs out, this callback executes and the CurrentUser is null. Otherwise, someone is
			// logging in and we need to assign their user level.
			if (e.user != null)
			{
				mLoggedInUserLevel = GetUserLevel(e.user);
				mLoggedInUsername = e.user.LoginName;
			}

			RefreshLoginLogout();
		}

		/*
		 * Called when the Application Manager exits. It calls the OnApplicationMgrExited function, which causes the
		 * MainWindow to close.
		 */
		private void AxApplicationMgrOnExitApplication(object sender, EventArgs eventArgs)
		{
			Environment.ExitCode = mAxApplicationMgr.ExitCode;			
			Utilities.Cleanup();
			TSHelper.DoSynchronousGCForCOMObjectDestruction();
			OnApplicationMgrExited(EventArgs.Empty);
		}

		/*
		 * Invokes the ApplicationMgrExited event handler, which triggers a response in MainWindow.
		 */
		protected virtual void OnApplicationMgrExited(EventArgs e)
		{
			ApplicationMgrExited?.Invoke(this, e);
		}
		#endregion

		#region INotifyPropertyChanged OnPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
