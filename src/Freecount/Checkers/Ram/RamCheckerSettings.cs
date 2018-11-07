using System;

namespace Freecount.Checkers.Ram
{
	internal class RamCheckerSettings : ResourceCheckerSettings
	{
		/*<Obj type='ram' criticalThresholdGb="1" thresholdType="free">
			<Exec>cmd.exe ..\..\a.html type=ram value=%value% limit=%limit%</Exec>
		</Obj>*/

		public double CriticalThreshold { set; get; }
		public ThresholdType ThresholdType { set; get; }
		public override string GetString => $"Virtual memory:{Environment.NewLine}"
			+ $"\t{ThresholdType.ToString()} space threshold {CriticalThreshold} GB{Environment.NewLine}"
			+ $"\tScript string {CommandLineToExecute}";
	}
}
