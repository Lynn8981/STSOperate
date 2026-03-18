using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace STSOperatorTool
{
	/*
	 * This class exposes a NationalInstruments.TestStand.SemiconductorModule.ICommand as a System.Windows.Input.ICommand. To
	 * attain the full functionality needed, a property for the command text is included.
	 */
	internal interface ISemiconductorModuleCommandHandler : ICommand
	{
		string SemiconductorModuleCommandText { get; }
	}

	internal class SemiconductorModuleCommandHandler : ISemiconductorModuleCommandHandler, INotifyPropertyChanged
	{
		private readonly NationalInstruments.TestStand.SemiconductorModule.ICommand mSemiconductorModuleCommand;
		private bool mCachedCanExecute;

		internal SemiconductorModuleCommandHandler(NationalInstruments.TestStand.SemiconductorModule.ICommand command)
		{
			mSemiconductorModuleCommand = command;
			if (mSemiconductorModuleCommand != null)
			{
				mSemiconductorModuleCommand.EnabledChanged += SemiconductorModuleCommand_EnabledChanged;
				mSemiconductorModuleCommand.TextChanged += SemiconductorModuleCommand_TextChanged;
				mSemiconductorModuleCommand.TooltipTextChanged += SemiconductorModuleCommand_ToolTipTextChanged;
			}
		}

		private void SemiconductorModuleCommand_TextChanged()
		{
			OnPropertyChanged("SemiconductorModuleCommandText");
		}

		private void SemiconductorModuleCommand_ToolTipTextChanged()
		{
			OnPropertyChanged("SemiconductorModuleCommandToolTipText");
		}

		private void SemiconductorModuleCommand_EnabledChanged()
		{
			CanExecute(null);
		}

		public string SemiconductorModuleCommandText
		{
			get { return mSemiconductorModuleCommand?.Text ?? ""; }
		}

		public string SemiconductorModuleCommandToolTipText
		{
			get
			{
				return mSemiconductorModuleCommand?.TooltipText ?? "";
			}
		}

		public void Execute(object parameter)
		{
			mSemiconductorModuleCommand?.Execute();
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			bool canExecute = mSemiconductorModuleCommand != null && mSemiconductorModuleCommand.Enabled;
			if (canExecute != mCachedCanExecute)
			{
				mCachedCanExecute = canExecute;

				NotifyCanExecuteChanged();
			}
			return mCachedCanExecute;
		}

		private void NotifyCanExecuteChanged()
		{
			var invokeCanExecuteChanged = new Action(() => CanExecuteChanged?.Invoke(this, null));

			Application.Current?.Dispatcher?.Invoke(invokeCanExecuteChanged);

			// This is only used for unit testing
			if (invokeCanExecuteChangedLocally)
			{
				invokeCanExecuteChanged();
			}
		}

		#region INotifyPropertyChanged OnPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		// The section below is only used for unit testing
		private readonly bool invokeCanExecuteChangedLocally = false;
		internal SemiconductorModuleCommandHandler(NationalInstruments.TestStand.SemiconductorModule.ICommand command, bool invokeCanExecuteChangedLocally) :
			this(command)
		{
			this.invokeCanExecuteChangedLocally = invokeCanExecuteChangedLocally;
		}
	}
}
