using System.Diagnostics.CodeAnalysis;
using NationalInstruments.TestStand.Interop.API;

namespace STSOperatorTool
{
	public class Utilities
	{
		private IEngine mEngine;

		public Utilities(IEngine engine)
		{
			mEngine = engine;
		}

		public void Cleanup()
		{
			mEngine = null;
		}

		[SuppressMessage("Microsoft.Design", "CA1026")]
		public string GetStringResource(string id, string category = "NI_SEMICONDUCTOR_OPERATOR_INTERFACE")
		{
			object unused;
			return mEngine.GetResourceString(category, id, id, out unused);
		}

		public User ValidateUserNameAndPassword(string userName, string password)
		{
			var user = mEngine.GetUser(userName);
			if (user != null && user.ValidatePassword(password))
			{
				return user;
			}
			return null;
		}

		public bool UserPrivilegeCheckingIsDisabled()
		{
			return !mEngine.StationOptions.EnableUserPrivilegeChecking;
		}

		public string GetFormattedValue(double value)
		{
			var numberObject = mEngine.NewPropertyObject(PropertyValueTypes.PropValType_Number, false, "", 0);
			numberObject.SetValNumber("", 0, value);
			return numberObject.GetFormattedValue("", PropertyOptions.PropOption_DecimalPoint_UsePreference, "%0.1f");
		}
	}
}
