using NationalInstruments.TestStand.Interop.API;

namespace STSOperatorTool
{
	public enum UserLevels
	{
		Operator,
		Administrator
	}

	internal static class UserLevel
	{
		
		// Whether or not the UserLevel is able to be elevated at all
		public static bool CanElevate(this UserLevels level)
		{
			switch (level)
			{
				case UserLevels.Administrator:
					return false;
				default:
					return true;
			}
		}

		// Whether or the user level can elevate to the new level
		public static bool CanElevateToUserLevel(this UserLevels level, UserLevels newLevel)
		{
			if (level.CanElevate())
			{
				switch (level)
				{
					case UserLevels.Operator:
						return newLevel != UserLevels.Operator;
					default:
						break;
				}
			}
			return false;
		}

		// Contains logic to decide which user level a particular user will be assigned in the OI.
		public static UserLevels GetUserLevel(User user)
		{
			if (user != null)
			{
				if (user.HasPrivilege(UserPrivileges.Priv_ConfigApp))
				{
					return UserLevels.Administrator;
				}
			}
			return UserLevels.Operator;
		}
	}
}
