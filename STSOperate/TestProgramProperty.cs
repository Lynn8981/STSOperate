namespace STSOperatorTool
{
	public interface ITestProgramProperty
	{
		string Name { get; }
		string Value { get; }
	}
	public class TestProgramProperty : ITestProgramProperty
	{
		public string Name { get; private set; }
		public string Value { get; private set; }

		public TestProgramProperty(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}
}