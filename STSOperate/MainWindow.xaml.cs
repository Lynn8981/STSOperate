using System;
using System.ComponentModel;
using System.Windows;
using NationalInstruments.TestStand.Interop.UI.Ax;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using NationalInstruments.TestStand.Interop.API;
using System.Windows.Input;

namespace STSOperatorTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private ViewModel mViewModel;
		private bool mAppManagerExited;
		private DataGridCell mMouseOverCell = null;
		private DataGridRow mMouseOverRow = null;
		private bool mTestSettingsExpanded = false;
		private readonly DispatcherTimer mResizeTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 100), IsEnabled = false };
		private readonly bool mCommandLineStartup = false;

		public MainWindow(StartupEventArgs e)
		{
			InitializeComponent();
			ShowInTaskbar = false;
			mCommandLineStartup = e.Args.Length > 0;
			mResizeTimer.Tick += ResizeComplete;
		}

		/*
		 * The Manager controls need to be initialized after the window loads, so the setup occurs in this callback.
		 */
		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			base.OnSourceInitialized(e);

			mAppManagerExited = false;

			AxApplicationMgr axApplicationMgr = new AxApplicationMgr();
			ApplicationManagerHost.Child = axApplicationMgr;

			AxSequenceFileViewMgr axSequenceFileViewMgr = new AxSequenceFileViewMgr();
			SequenceFileViewManagerHost.Child = axSequenceFileViewMgr;

			mViewModel = new ViewModel(axApplicationMgr, axSequenceFileViewMgr);

			mViewModel.ApplicationMgrExited += ViewModelOnApplicationMgrExited;

			this.DataContext = mViewModel;
			PercentageConverter.Initialize(mViewModel.Utilities);

			LocalizeControls();

			// Update layout of these grids after localization to fix the header spacing
			ResizeDetailsGridsAfterLocalization();

			ShowInTaskbar = true;
		}

		/*
		 * Handles a request to close the window. If the app manager hasn't shut down, it will cancel the request and
		 * call the ExitCommand.Execute command. This causes the Semiconductor Module to stop the TestStand execution,
		 * eventually resulting in the TS Application Manager exiting. TestStand will throw an error about object
		 * leaks if we do not allow the Manager to shut down before exiting the application.
		 */
		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			// If the app manager hasn't exited, we need to cancel the close and trigger a shutdown through the
			// Semiconductor Module
			if (!mAppManagerExited)
			{
				e.Cancel = true;
				// If the exit command is enabled, execute it.
				if (mViewModel.ExitCommand.CanExecute(this))
				{
					mViewModel.ExitCommand.Execute(this);
				}
			}
		}

		private void ViewModelOnApplicationMgrExited(object sender, EventArgs eventArgs)
		{
			mAppManagerExited = true;

			// Will result in the MainWindow_OnClosing callback being called
			Close();
		}

		public Thickness CaptionButtonMargin
		{
			get
			{
				if (WindowState == WindowState.Maximized)
					return new Thickness(6, 6, 6, 0);
				else
					return new Thickness(0, 0, 0, 0);
			}
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);
			if (WindowState == WindowState.Maximized)
			{
				Maximize1Button.Visibility = Visibility.Hidden;
				Maximize2Button.Visibility = Visibility.Visible;
			}
			else
			{
				Maximize2Button.Visibility = Visibility.Hidden;
				Maximize1Button.Visibility = Visibility.Visible;
			}
			OnPropertyChanged("CaptionButtonMargin");
		}

		private void ResizeDetailsGridsAfterLocalization()
		{
			// The Auto sizing in DataGrid columns only increases the width of the columns. After localization,
			// this resets the size for when the localized string is shorter than the resource string.
			SiteFailedGrid.Columns[0].Width = 0;
			SiteFailedGrid.Columns[1].Width = 0;

			BinFailedGrid.Columns[0].Width = 0;
			BinFailedGrid.Columns[1].Width = 0;
			BinFailedGrid.Columns[2].Width = 0;

			SiteFailedGrid.UpdateLayout();
			BinFailedGrid.UpdateLayout();

			SiteFailedGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
			SiteFailedGrid.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

			BinFailedGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
			BinFailedGrid.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
			BinFailedGrid.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

			SiteFailedGrid.UpdateLayout();
			BinFailedGrid.UpdateLayout();
		}

		private void SizeBinNamesColumnToFit()
		{
			const double cDataGridMarginsTotal = 74;
			mResizeTimer.IsEnabled = false;
			if (SiteBinFailureRateDataGrid.Columns.Count > 1)
			{
				// set the bin names column to auto and update so we can measure the desired width
				SiteBinFailureRateDataGrid.Columns[1].Visibility = Visibility.Visible;
				SiteBinFailureRateDataGrid.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
				SiteBinFailureRateDataGrid.UpdateLayout();

				double totalWidth = 0;
				foreach (var column in SiteBinFailureRateDataGrid.Columns)
				{
					totalWidth += column.ActualWidth;
				}
				// if this makes the width of the datagrid + the margins greater than the window, shrink the bin names column so that everything else fits
				bool canScroll = false;
				var widthDiff = (totalWidth + cDataGridMarginsTotal) - STSOperatorToolWindow.ActualWidth;
				if (widthDiff > 0)
				{
					var binNamesWidth = SiteBinFailureRateDataGrid.Columns[1].ActualWidth - widthDiff;
					binNamesWidth = binNamesWidth < 0 ? 0 : binNamesWidth;
					SiteBinFailureRateDataGrid.Columns[1].Width = binNamesWidth;
					if (binNamesWidth == 0)
					{
						// only use scrollbar if we have already shrunk the binNames column to try to make room
						canScroll = true;
						SiteBinFailureRateDataGrid.Columns[1].Visibility = Visibility.Collapsed;
					}
				}

				if (canScroll)
				{
					SiteBinFailureRateDataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
					GetScrollViewer(SiteBinFailureRateDataGrid).CanContentScroll = true;
				}
				else
				{
					SiteBinFailureRateDataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
					GetScrollViewer(SiteBinFailureRateDataGrid).CanContentScroll = false;
				}
			}
		}

		private static ScrollViewer GetScrollViewer(DependencyObject element)
		{
			if (element == null) return null;

			ScrollViewer returnElement = null;
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element) && returnElement == null; i++)
			{
				if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
				{
					returnElement = (ScrollViewer)(VisualTreeHelper.GetChild(element, i));
				}
				else
				{
					returnElement = GetScrollViewer(VisualTreeHelper.GetChild(element, i));
				}
			}
			return returnElement;
		}

		private class FailureRateSquareTemplateColumn : DataGridTemplateColumn
		{
			public string ColumnName
			{
				get;
				set;
			}

			protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
			{
				// The DataGridTemplateColumn uses ContentPresenter with your DataTemplate.
				ContentPresenter cp = (ContentPresenter)base.GenerateElement(cell, dataItem);
				// Reset the Binding to the specific column. The default binding is to the DataRowView.
				BindingOperations.SetBinding(cp, ContentPresenter.ContentProperty, new Binding(this.ColumnName));
				return cp;
			}
		}

		#region Localization
		private void LocalizeControls()
		{
			// Localize window title bar
			this.Title = mViewModel.Utilities.GetStringResource(this.Title);

			// Localize QA strings
			LocalizeContent(LotYieldItem);
			LocalizeContent(WaferYieldItem);

			LocalizeHeader(SiteFailureRateColumn);
			LocalizeHeader(SiteFailureSiteColumn);
			LocalizeHeader(SiteFailureFailedColumn);
			LocalizeHeader(SiteFailurePercentColumn);

			LocalizeHeader(BinFailureRateColumn);
			LocalizeHeader(BinFailureBinColumn);
			LocalizeHeader(BinFailureCountColumn);
			LocalizeHeader(BinFailureNameColumn);
			LocalizeHeader(BinFailurePercentColumn);

			// Localize controls in the Status Bar
			LocalizeLabel(QAFailedLabel);

			LocalizeContent(ShowMoreButton);

			LocalizeLabel(LotLabel);
			LocalizeLabel(WaferLabel);

			LocalizeLabel(YieldLabel);
			LocalizeLabel(LotPercentLabel);
			LocalizeLabel(WaferPercentLabel);
			LocalizeLabel(LastNPercentLabel);

			LocalizeLabel(CycleTimeUnitsLabel);
			LocalizeLabel(SocketTimeUnitsLabel);
			LocalizeLabel(IndexTimeUnitsLabel);

			LocalizeLabel(CycleTimeLabel);
			LocalizeLabel(SocketTimeLabel);
			LocalizeLabel(IndexTimeLabel);

			LocalizeLabel(TimeLabel);
			LocalizeLabel(TestSettingsLabel);

			LocalizeLabel(LotPassedLabel);
			LocalizeLabel(LotTestedLabel);
			LocalizeLabel(WaferPassedLabel);
			LocalizeLabel(WaferTestedLabel);
			LocalizeLabel(WindowPassedLabel);
			LocalizeLabel(WindowTestedLabel);

			LocalizeLabel(UserLabel);
			LocalizeLabel(PasswordLabel);

			LocalizeLabel(FailureAnalysisLabel);

			LocalizeHeader(ElevateUserMenuItem);
			LocalizeHeader(RevertUserMenuItem);
		}

		private void LocalizeLabel(TextBlock textBlock)
		{
			textBlock.Text = mViewModel.Utilities.GetStringResource(textBlock.Text);
		}

		private void LocalizeHeader(DataGridColumn column)
		{
			column.Header = mViewModel.Utilities.GetStringResource(column.Header.ToString());
		}

		private void LocalizeHeader(MenuItem menuItem)
		{
			menuItem.Header = mViewModel.Utilities.GetStringResource(menuItem.Header.ToString());
		}

		private void LocalizeContent(ContentControl control)
		{
			control.Content = mViewModel.Utilities.GetStringResource(control.Content.ToString());
		}
		#endregion

		#region Event Handlers
		private void FailureSquareMouseEnter(object sender, MouseEventArgs e)
		{
			var border = sender as FrameworkElement;
			var siteBinInfo = border?.DataContext as SiteBinInfo;
			if (siteBinInfo != null)
			{
				siteBinInfo.UpdateTooltips = true;
			}
		}

		private void FailureSquareMouseLeave(object sender, MouseEventArgs e)
		{
			var border = sender as FrameworkElement;
			var siteBinInfo = border?.DataContext as SiteBinInfo;
			if (siteBinInfo != null)
			{
				siteBinInfo.UpdateTooltips = false;
			}
		}

		private void FailureSquareToolTipOpening(object sender, ToolTipEventArgs e)
		{
			if (mMouseOverCell != null)
			{
				mMouseOverCell.Column.CellStyle = (Style)SiteBinFailureRateDataGrid.FindResource("FailureRateSquareCellStyleBordered");
				mMouseOverCell.Column.HeaderStyle = (Style)SiteBinFailureRateDataGrid.FindResource("SiteDataGridColumnHeaderBordered");
			}
			if (mMouseOverRow != null)
			{
				mMouseOverRow.BorderBrush = (SolidColorBrush)Application.Current.FindResource("LightBlue");
			}
		}

		private void DataGridRow_MouseEnter(object sender, MouseEventArgs e)
		{
			mMouseOverRow = sender as DataGridRow;
		}

		private void DataGridRow_MouseLeave(object sender, MouseEventArgs e)
		{
			if (mMouseOverRow != null)
			{
				mMouseOverRow.BorderBrush = null;
				mMouseOverRow = null;
			}
		}

		private void DataGridCell_MouseEnter(object sender, MouseEventArgs e)
		{
			mMouseOverCell = sender as DataGridCell;
		}

		private void DataGridCell_MouseLeave(object sender, MouseEventArgs e)
		{
			if (mMouseOverCell != null)
			{
				mMouseOverCell.Column.CellStyle = (Style)SiteBinFailureRateDataGrid.FindResource("FailureRateSquareCellStyle");
				mMouseOverCell.Column.HeaderStyle = (Style)SiteBinFailureRateDataGrid.FindResource("SiteDataGridColumnHeader");
				mMouseOverCell = null;
			}
			if (mMouseOverRow != null)
			{
				mMouseOverRow.BorderBrush = null;
			}
		}

		private void ShowMoreButton_Click(object sender, RoutedEventArgs e)
		{
			mTestSettingsExpanded = !mTestSettingsExpanded;
			if (mTestSettingsExpanded)
			{
				Grid.SetRowSpan(TestSettingsCard, 3);
				TestSettingsCard.MaxHeight = Double.PositiveInfinity;
				TestSettingsExpandedBottomLine.Visibility = Visibility.Visible;
				ShowMoreButton.Content = "SHOW_LESS";
				LocalizeContent(ShowMoreButton);
			}
			else
			{
				Grid.SetRowSpan(TestSettingsCard, 1);
				TestSettingsExpandedBottomLine.Visibility = Visibility.Hidden;
				Binding bnd = new Binding("ActualHeight") { ElementName = "TimeCard" };
				BindingOperations.SetBinding(TestSettingsCard, Grid.MaxHeightProperty, bnd);
				ShowMoreButton.Content = "SHOW_MORE";
				LocalizeContent(ShowMoreButton);
			}
		}

		private void MainGrid_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// when user logs in, the main grid is enabled
			var loggedIn = (bool)e.NewValue;
			
			// if the app just started and the user logged in, and we did not start the OI using the command line,
			// we should show the context menu to indicate that they need to configure lot settings	
			if (loggedIn && !mCommandLineStartup)
			{
				STSOperatorToolWindow.Activate();
				HamburgerButton.ContextMenu.Placement = PlacementMode.Bottom;
				HamburgerButton.ContextMenu.PlacementTarget = HamburgerButton;
				HamburgerButton.ContextMenu.StaysOpen = true;
				HamburgerButton.ContextMenu.IsOpen = true;
			}
		}

		private void GraphsButton_Click(object sender, RoutedEventArgs e)
		{
			SwitchToGraphsView();
		}

		private void TableButton_Click(object sender, RoutedEventArgs e)
		{
			SwitchToTableView();
		}

		private void SwitchToGraphsView()
		{
			GraphsButtonBorder.BorderThickness = new Thickness(2);
			TableButtonBorder.BorderThickness = new Thickness(0);
			BarGraphViewGrid.Visibility = Visibility.Visible;
			SiteBinFailureRateDataGrid.Visibility = Visibility.Hidden;
		}

		private void SwitchToTableView()
		{
			TableButtonBorder.BorderThickness = new Thickness(2);
			GraphsButtonBorder.BorderThickness = new Thickness(0);
			SiteBinFailureRateDataGrid.Visibility = Visibility.Visible;
			BarGraphViewGrid.Visibility = Visibility.Hidden;
		}

		private void YieldSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0 && mViewModel != null)
			{
				var item = e.AddedItems[0] as ComboBoxItem;
				YieldSelectorItems result;
				Enum.TryParse<YieldSelectorItems>(item.Name, out result);
				mViewModel.YieldSelectorItem = result;
			}
		}

		private void SiteBinFailureRateDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			int siteNumber;
			var isSiteColumn = int.TryParse(e.PropertyName, out siteNumber);

			var allSitesString = mViewModel.Utilities.GetStringResource("TABLE_ALL_SITES");
			var binNamesString = mViewModel.Utilities.GetStringResource("TABLE_BIN_NAME");
			if (e.PropertyName == allSitesString || isSiteColumn)
			{
				FailureRateSquareTemplateColumn col = new FailureRateSquareTemplateColumn();
				col.ColumnName = e.PropertyName;  // so it knows from which column to get data
				if (e.PropertyName == allSitesString)
				{
					col.CellTemplate = (DataTemplate)SiteBinFailureRateDataGrid.FindResource("AllSitesFailureRateSquareTemplate");
				}
				else
				{
					col.CellTemplate = (DataTemplate)SiteBinFailureRateDataGrid.FindResource("FailureRateSquareTemplate");
				}

				col.HeaderStyle = (Style)SiteBinFailureRateDataGrid.FindResource("SiteDataGridColumnHeader");
				col.CellStyle = (Style)SiteBinFailureRateDataGrid.FindResource("FailureRateSquareCellStyle");
				e.Column = col;
				e.Column.Header = e.PropertyName;
			}
			else if (e.PropertyName == binNamesString)
			{
				e.Column.HeaderStyle = (Style)SiteBinFailureRateDataGrid.FindResource("DefaultDataGridColumnHeader");
				e.Column.CellStyle = (Style)SiteBinFailureRateDataGrid.FindResource("BinNamesCellStyle");
			}
			else // bin#
			{
				e.Column.HeaderStyle = (Style)SiteBinFailureRateDataGrid.FindResource("DefaultDataGridColumnHeader");
				e.Column.CellStyle = (Style)SiteBinFailureRateDataGrid.FindResource("DefaultCellStyle");
			}
		}

		private void SiteBinFailureRateDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
		{
			SizeBinNamesColumnToFit();
		}

		/*
		 * Using a timer and event handler prevents excessive processing during the window resize.
		 */
		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			mResizeTimer.IsEnabled = true;
			mResizeTimer.Stop();
			mResizeTimer.Start();
		}

		private void ResizeComplete(object sender, EventArgs e)
		{
			SizeBinNamesColumnToFit();
		}
		#endregion

		#region LoginLogout Menu Behavior
		private Storyboard mLoginAnimationStoryboard;
		private Storyboard LoginAnimationStoryboard
		{
			get
			{
				if (mLoginAnimationStoryboard == null)
				{
					mLoginAnimationStoryboard = new Storyboard();
					var menuItems = LogInLogOutContextMenu.Items;

					var duration = new Duration(TimeSpan.FromMilliseconds(200));

					foreach (FrameworkElement item in menuItems)
					{
						if (item.Visibility == Visibility.Visible)
						{
							var animation = new DoubleAnimation();
							animation.Duration = duration;
							Storyboard.SetTarget(animation, item);

							if (item.Equals(EnterCredentialsMenuItem))
							{
								// For the username/password prompt, just animate its maximum height to allow it to show up
								animation.From = 0;
								animation.To = 500;
								Storyboard.SetTargetProperty(animation, new PropertyPath(MaxHeightProperty));
							}
							else
							{
								// For all other items, animate height, margin, and minimum height to 0 so that they appear to slide up
								// and out of view

								// Animate the height to 0
								animation.From = item.ActualHeight;
								animation.To = 0;
								Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));

								// Animate the margin to 0
								var marginAnimation = new ThicknessAnimation();
								marginAnimation.Duration = duration;
								Storyboard.SetTarget(marginAnimation, item);

								marginAnimation.From = item.Margin;
								marginAnimation.To = new Thickness(0);

								Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(MarginProperty));
								mLoginAnimationStoryboard.Children.Add(marginAnimation);

								// Animate the MinHeight to 0
								var minHeightAnimation = new DoubleAnimation();
								minHeightAnimation.Duration = duration;
								Storyboard.SetTarget(minHeightAnimation, item);

								minHeightAnimation.From = item.MinHeight;
								minHeightAnimation.To = 0;

								Storyboard.SetTargetProperty(minHeightAnimation, new PropertyPath(MinHeightProperty));
								mLoginAnimationStoryboard.Children.Add(minHeightAnimation);
							}

							mLoginAnimationStoryboard.Children.Add(animation);
						}
					}
				}

				return mLoginAnimationStoryboard;
			}
		}

		private void LogInLogOutContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			// Only update which buttons are visible as the menu opens. Without this, the changes are
			// visible after a button is clicked but before the menu closes.
			mViewModel.RefreshContextMenuVisibilities();
		}

		private void LogInLogOutContextMenu_Closed(object sender, RoutedEventArgs e)
		{
			// This takes a noticeable amount of time, so run it at close instead of open to avoid the appearance
			// of a delay
			ResetLogInLogOutContextMenu();
		}

		private void ResetLogInLogOutContextMenu()
		{
			LoginAnimationStoryboard.Seek(new TimeSpan(0));
			LoginAnimationStoryboard.Pause();

			HideInvalidMessage();

			UserNameBox.Clear();
			PasswordBox.Clear();
		}

		private void AnimateLoginPrompt(object sender, RoutedEventArgs e)
		{
			LoginAnimationStoryboard.Begin();
			// After animation, focus on the user name entry text box
			UserNameBox.Focus();
		}

		private void RevertUser_Click(object sender, RoutedEventArgs e)
		{
			SwitchToGraphsView();
			mViewModel.RevertUser();
		}

		private void OnEnterCredentialsKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				TryElevateUser();
			}
			else
			{
				// Clear the invalid user message when user resumes typing
				HideInvalidMessage();
			}
		}

		private void ElevateUser_Click(object sender, RoutedEventArgs e)
		{
			TryElevateUser();
		}

		private void TryElevateUser()
		{
			User user = mViewModel.Utilities.ValidateUserNameAndPassword(UserNameBox.Text, PasswordBox.Password);

			if (user != null)
			{
				bool successfulElevation = mViewModel.RequestUserElevation(user);

				if (successfulElevation)
				{
					LogInLogOutContextMenu.IsOpen = false;
				}
				else
				{
					ShowCannotElevateUserMessage();
				}
			}
			else
			{
				ShowInvalidUserMessage();
			}
		}

		private void HideInvalidMessage()
		{
			// If an error message is multiple lines, need to clear the text to reset the spacing
			InvalidMessage.Text = string.Empty;
			InvalidMessage.Visibility = Visibility.Hidden;
		}

		private void ShowInvalidUserMessage()
		{
			InvalidMessage.Text = "INVALID_PASSWORD";
			LocalizeLabel(InvalidMessage);
			InvalidMessage.Visibility = Visibility.Visible;
		}

		private void ShowCannotElevateUserMessage()
		{
			InvalidMessage.Text = "CANNOT_ELEVATE_USER";
			LocalizeLabel(InvalidMessage);
			InvalidMessage.Visibility = Visibility.Visible;
		}
		#endregion

		#region INotifyPropertyChanged
		private void OnPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
	}
}
