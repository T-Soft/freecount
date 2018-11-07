using System;

namespace Freecount.Checkers.Disk
{
	internal class DiskCheckerSettings : ResourceCheckerSettings
	{
		/*<Obj type='disk' criticalThresholdGb="100">
			<Value>D:</Value>
			<Exec>cmd.exe ..\..\a.html type=dskD value=%value% limit=%limit%</Exec>
		</Obj>*/

		public string DriveLetter { set; get; }
		public double CriticalThreshold { set; get; }
		public ThresholdType ThresholdType { set; get; }
		public override string GetString => $"Disk {DriveLetter}{Environment.NewLine}"
			+ $"\t{ThresholdType.ToString()} space threshold {CriticalThreshold} GB{Environment.NewLine}"
			+ $"\tScript string {CommandLineToExecute}";
	}
}
