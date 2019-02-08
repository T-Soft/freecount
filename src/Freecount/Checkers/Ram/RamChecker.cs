using System;
using System.Management;
using Freecount.Helpers;

namespace Freecount.Checkers.Ram
{
	internal class RamChecker : ResourceChecker
	{
		private readonly RamCheckerSettings _settings;
		private readonly ManagementObjectSearcher _ramQuery;

		public RamChecker(RamCheckerSettings settings)
		{
			_settings = settings;
			_ramQuery = new ManagementObjectSearcher(
				new ObjectQuery("SELECT * FROM Win32_OperatingSystem")
			);
		}

		public override ResourceCheckResult Check()
		{
			RamCheckResult result = null;

			foreach (var o in _ramQuery.Get())
			{
				var item = (ManagementObject) o;
				var freeVirtualMemoryGb = SpaceHelper.BytesToGigabytes((double) item["FreeVirtualMemory"]);
				var usedVirtualMemoryGb = SpaceHelper.BytesToGigabytes((double) item["TotalVirtualMemorySize"])
					- freeVirtualMemoryGb;

				if (!_wasWarning)
				{
					if ((_settings.ThresholdType == ThresholdType.Free
							&& freeVirtualMemoryGb < _settings.CriticalThreshold)
						|| (_settings.ThresholdType == ThresholdType.Used
							&& usedVirtualMemoryGb > _settings.CriticalThreshold)
					)
					{
						result = new RamCheckResult(
							Name,
							_settings,
							freeVirtualMemoryGb,
							usedVirtualMemoryGb,
							false,
							_wasWarning);
						_wasWarning = true;
					}
				}
				else
				{
					if ((_settings.ThresholdType == ThresholdType.Free
							&& freeVirtualMemoryGb > _settings.CriticalThreshold)
						|| (_settings.ThresholdType == ThresholdType.Used
							&& usedVirtualMemoryGb < _settings.CriticalThreshold)
					)
					{

						result = new RamCheckResult(Name,
							_settings,
							freeVirtualMemoryGb,
							usedVirtualMemoryGb,
							true,
							_wasWarning);
						_wasWarning = false;
					}
				}

				if (result != null)
				{
					break;
				}
			}

			return result;
		}

		public override string ReportConfiguration() => _settings.GetString;

		public override string Name => "Virtual memory checker";
	}
}
