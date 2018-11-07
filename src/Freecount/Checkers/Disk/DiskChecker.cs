using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Management;
using Freecount.Helpers;

namespace Freecount.Checkers.Disk
{
	internal class DiskChecker : ResourceChecker
	{
		private readonly DiskCheckerSettings _settings;
		private bool _wasWarning;

		public override string Name => $"Disk {_settings.DriveLetter} checker";

		public DiskChecker(DiskCheckerSettings settings)
		{
			_settings = settings;
		}

		public override string ReportConfiguration() => _settings.GetString;

		public override ResourceCheckResult Check()
		{
			DiskCheckResult result = null;

			var driveInfo = DriveInfo
				.GetDrives().FirstOrDefault(di => di.Name.TrimEnd('\\') == _settings.DriveLetter);

			if (driveInfo == null)
			{
				// means no drive found
				result = new DiskCheckResult(_settings, long.MinValue, false, _wasWarning)
				{
					WasErrorDuringCheckerExecution = true,
					CheckerExecutionErrorMessage = $"Drive {_settings.DriveLetter} not found."
				};
				_wasWarning = true;
				return result;
			}

			var driveSpace = _settings.ThresholdType == ThresholdType.Used
				? driveInfo.TotalSize - driveInfo.TotalFreeSpace
				: driveInfo.TotalFreeSpace;

			double spaceLeftOnDriveGb = SpaceHelper.BytesToGigabytes(driveSpace); //free or used - depends on config

			if (!_wasWarning)
			{
				if ((_settings.ThresholdType == ThresholdType.Free
						&& spaceLeftOnDriveGb < _settings.CriticalThreshold)
					|| (_settings.ThresholdType == ThresholdType.Used
						&& spaceLeftOnDriveGb > _settings.CriticalThreshold)
				)
				{
					result = new DiskCheckResult(_settings, spaceLeftOnDriveGb, false, _wasWarning);
					_wasWarning = true;
				}
			}
			else
			{
				if ((_settings.ThresholdType == ThresholdType.Free
						&& spaceLeftOnDriveGb > _settings.CriticalThreshold)
					|| (_settings.ThresholdType == ThresholdType.Used
						&& spaceLeftOnDriveGb < _settings.CriticalThreshold)
				)
				{

					result = new DiskCheckResult(_settings, spaceLeftOnDriveGb, true, _wasWarning);
					_wasWarning = false;
				}
			}

			return result;
		}
	}
}
