using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Freecount.Checkers;
using Freecount.Checkers.Certificate;
using Freecount.Checkers.Disk;
using Freecount.Checkers.Ram;
using Freecount.Helpers;
using Freecount.Email;

namespace Freecount
{
	class Program
	{

		#region Private

		private static long _secondsPassedWatching;
		private const string CONFIG_PATH = "config.xml";

		private static int _tmrIntervalSeconds;
		private static long _certificcateCheckIntervalSeconds;

		private static Dictionary<string, decimal> _watchedDisksThresholds;
		private static Dictionary<string, ThresholdType> _watchedDisksThresholdTypes;
		private static Dictionary<string, bool> _watchedDisksWarnings;
		private static Dictionary<string, string> _watchedDisksScripts;

		private static Dictionary<string, X509Certificate2> _watchedCertificates;
		private static Dictionary<string, int> _watchedCertificatesDaysBeforeAlert;
		private static Dictionary<string, string> _watchedCertificatesScripts;
		private static bool _certificateWatcherIsConfigured = false;

		private static bool _virtualMemoryIsConfigured = false;
		private static decimal _virtualMemoryThresholdGb;
		private static ThresholdType _virtualMemoryThresholdType;
		private static bool _virtualMemoryWarning = false;
		private static string _virtualMemoryThresholdScript;

		private static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
		private static readonly string ProgramVersion = $"{Version.Major}.{Version.Minor}.{Version.Build}";

		private static ThreadStart _getNumbers;
		private static EmailNotifier _emailNotifier;
		private static ManagementObjectSearcher _searcher;

		private static List<ResourceChecker> _checkers = new List<ResourceChecker>();

		#endregion

		private static void Main(string[] args)
		{
			_secondsPassedWatching = 0;
			Console.WriteLine($"Freecount v{ProgramVersion}{Environment.NewLine}{Environment.NewLine}Loading config...");
			if (ReadConfig2())
			{
				while (true)
				{
					if (Console.KeyAvailable)
					{
						if (Console.ReadKey().Key == ConsoleKey.Escape)
						{
							Console.WriteLine("Press ESC again to exit");
							if (Console.ReadKey().Key == ConsoleKey.Escape)
							{
								return;
							}
						}
					}
#if DEBUG
					_tmrIntervalSeconds = 2;
#endif
					Thread tr = new Thread(_getNumbers);
					tr.Start();
					Thread.Sleep(_tmrIntervalSeconds * 1000);
					_secondsPassedWatching += _tmrIntervalSeconds;
				}
			}
		}
		
		#region Methods for working with configuration

		private static bool ReadConfig2()
		{
			try
			{
				XDocument cfg = XDocument.Load(CONFIG_PATH);
				_tmrIntervalSeconds = int.Parse(cfg.GetChildElementValue("CheckupIntervalMinutes")) * 60;
				_certificcateCheckIntervalSeconds = -1;
				try
				{
					_certificcateCheckIntervalSeconds =
						(long)int.Parse(cfg.GetChildElementValue("CertCheckupIntervalDays")) * 86400;
					_certificateWatcherIsConfigured = true;
				}
				catch
				{
					_certificateWatcherIsConfigured = false;
				}

#if DEBUG
				_certificcateCheckIntervalSeconds = 10;
#endif
				var diskConfigurations = cfg.GetChildElementsByAttributeValue("type", "disk");
				if (diskConfigurations != null)
				{
					foreach (var diskConfig in diskConfigurations)
					{
						Enum.TryParse(diskConfig.GetAttributeValue("thresholdType"), out ThresholdType thresholdType);

						DiskCheckerSettings diskCheckerSettings = new DiskCheckerSettings(){
							DriveLetter = diskConfig.GetChildElementValue("Value"),
							CriticalThreshold = double.Parse(diskConfig.GetAttributeValue("criticalThresholdGb").Replace(".", ",")),
							CommandLineToExecute = diskConfig.GetChildElementValue("Exec"),
							ThresholdType = thresholdType
						};
						_checkers.Add(new DiskChecker(diskCheckerSettings));
					}
				}

				var memoryConfigurations = cfg.GetChildElementsByAttributeValue("type", "ram");
				if (memoryConfigurations != null)
				{
					foreach (var memConfig in memoryConfigurations)
					{
						Enum.TryParse(memConfig.GetAttributeValue("thresholdType"), out ThresholdType thresholdType);
						RamCheckerSettings ramCheckerSettings = new RamCheckerSettings(){
							ThresholdType = thresholdType,
							CommandLineToExecute = memConfig.GetChildElementValue("Exec"),
							CriticalThreshold = double.Parse(memConfig.GetAttributeValue("criticalThresholdGb").Replace(".", ",")),
						};
						_checkers.Add(new RamChecker(ramCheckerSettings));
					}
				}

				var certificateConfigurations = cfg.GetChildElementsByAttributeValue("type", "cert");
				if (certificateConfigurations != null)
				{
					foreach (var certConfig in certificateConfigurations)
					{
						var thumb = certConfig.GetChildElementValue("Thumbprint");
						var certificate = CertificateHelper.SearchCertificateByThumbprint(thumb);

						CertificateCheckerSettings certCheckerSettings = new CertificateCheckerSettings(){
							CommandLineToExecute = certConfig.GetChildElementValue("Exec"),
							CertificateStore = certConfig.GetChildElementValue("Store"),
							Thumbprint = thumb,
							DaysBeforeAlert = int.Parse(certConfig.GetAttributeValue("daysBeforeAlert")),
							Certificate = certificate
						};
						_checkers.Add(new CertificateChecker(certCheckerSettings));
					}
				}
				
				_emailNotifier = new EmailNotifier(
					cfg.Root?.Element("SmtpServer"),
					cfg.Root?.Element("AdminEmailList"),
					cfg.Root?.Element("EmailTemplates"));
				Console.WriteLine($"Configuration loaded!{Environment.NewLine}");

				PrintConfig2();

				Console.WriteLine($"Watchers configured!{Environment.NewLine}Watching. Press ESC to exit.{Environment.NewLine}");

				_getNumbers = () =>
				{
					Console.WriteLine($"{Environment.NewLine}[{DateTime.Now.ToString("s").Replace("T", " ")}] >{new string('-', 8)}");
					foreach (var checker in _checkers)
					{
						ProcessCheckResult(checker.Check());
					}
				};

				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Configuration load failed!");
				Console.WriteLine(e.Message);
				return false;
			}
		}

		private static void PrintConfig2()
		{
			Console.WriteLine($"{new string('-', 16)}");
			Console.WriteLine($"Checkup interval: {_tmrIntervalSeconds / 60} minute(s)");
			Console.WriteLine($"Certificates checkup interval: {_certificcateCheckIntervalSeconds / 60 / 60 / 24} day(s)");
			Console.WriteLine($"Watched objects:{Environment.NewLine}");

			foreach (var checker in _checkers.OrderBy(c => c.Name))
			{
				Console.WriteLine(checker.ReportConfiguration() + Environment.NewLine);
			}

			Console.WriteLine($"{new string('-', 16)}{Environment.NewLine}");
		}

		#region // Old config reading logic

		private static bool ReadConfig()
		{
			try
			{
				XDocument cfg = XDocument.Load(CONFIG_PATH);
				_tmrIntervalSeconds = int.Parse(cfg.Root.Element("CheckupIntervalMinutes").Value) * 60;
				_certificcateCheckIntervalSeconds = -1;
				try
				{
					_certificcateCheckIntervalSeconds =
						(long)int.Parse(cfg.Root.Element("CertCheckupIntervalDays").Value) * 86400;
				}
				catch
				{
					_certificateWatcherIsConfigured = false;
				}

#if DEBUG
				_certificcateCheckIntervalSeconds = 10;
#endif

				#region [watched disks setup]
				_watchedDisksThresholds = cfg.Root
					.Element("ControlObjList")
					.Elements("Obj")
					.Where((elt => elt.Attribute("type").Value.ToLower() == "disk"))
					.ToDictionary(
						elt =>
						{
							if (DriveInfo.GetDrives().Select((di) => di.Name.TrimEnd('\\')).ToList()
								.Contains(elt.Element("Value").Value))
							{
								return elt.Element("Value").Value;
							}

							throw new Exception($"Disk {elt.Element("Value").Value} not found!");
						},
						elt =>
							decimal.Parse(
								elt.Attribute("criticalThresholdGb").Value.Replace(".", ","),
								NumberStyles.AllowDecimalPoint)
					);
				_watchedDisksThresholdTypes = cfg.Root
					.Element("ControlObjList")
					.Elements("Obj")
					.Where((elt => elt.Attribute("type").Value.ToLower() == "disk"))
					.ToDictionary(
						elt =>
						{
							if (DriveInfo.GetDrives().Select((di) => di.Name.TrimEnd('\\')).ToList()
								.Contains(elt.Element("Value").Value))
							{
								return elt.Element("Value").Value;
							}

							throw new Exception($"Disk {elt.Element("Value").Value} not found!");
						},
						elt =>
						{
							ThresholdType tt = ThresholdType.Free;
							if (elt.Attribute("thresholdType") != null)
							{
								if (!Enum.TryParse(elt.Attribute("thresholdType").Value, true, out tt))
								{
									throw new Exception(
										$"Threshold type {elt.Attribute("thresholdType").Value} is invalid!{Environment.NewLine}Possible values are 'used', 'free'.");
								}
							}

							return tt;
						}
					);

				_watchedDisksScripts = cfg.Root
					.Element("ControlObjList")
					.Elements("Obj")
					.Where((elt => elt.Attribute("type").Value.ToLower() == "disk"))
					.ToDictionary(
						elt => elt.Element("Value").Value,
						elt => elt.Element("Exec").Value
					);
				_watchedDisksWarnings = cfg.Root
					.Element("ControlObjList")
					.Elements("Obj")
					.Where((elt => elt.Attribute("type").Value.ToLower() == "disk"))
					.ToDictionary(
						elt => elt.Element("Value").Value,
						elt => false
					);
				#endregion

				#region [virtual memory setup]
				_virtualMemoryThresholdScript = cfg.Root.Element("ControlObjList")
					.Elements("Obj")
					.Where((elt) => elt.Attribute("type").Value.ToLower() == "ram")
					.Select(elt => elt.Element("Exec").Value).FirstOrDefault();

				_virtualMemoryThresholdType = cfg.Root.Element("ControlObjList")
					.Elements("Obj")
					.Where((elt) => elt.Attribute("type").Value.ToLower() == "ram")
					.Select(
						elt =>
						{
							ThresholdType tt = ThresholdType.Free;
							if (elt.Attribute("thresholdType") != null)
							{
								if (!Enum.TryParse(elt.Attribute("thresholdType").Value, true, out tt))
								{
									throw new Exception(
										$"Threshold type {elt.Attribute("thresholdType").Value} is invalid!{Environment.NewLine}Possible values are 'used', 'free'.");
								}
							}

							return tt;
						}).FirstOrDefault();

				if (!string.IsNullOrEmpty(_virtualMemoryThresholdScript))
				{
					//means ram section is configured
					_virtualMemoryThresholdGb = decimal.Parse(
						cfg.Root
							.Element("ControlObjList")
							.Elements("Obj")
							.Where((elt) => elt.Attribute("type").Value.ToLower() == "ram")
							.Select(elt => elt.Attribute("criticalThresholdGb").Value)
							.FirstOrDefault().Replace(".", ","),
						NumberStyles.AllowDecimalPoint
					);
					_virtualMemoryIsConfigured = true;
				}
				#endregion

				#region [certificates setup]
				//if certs config present
				bool wasErrorDuringCertWatcherInit = false;
				if (
					cfg.Root.Element("ControlObjList")
						.Elements("Obj")
						.Where((elt => elt.Attribute("type").Value.ToLower() == "cert"))
						.Any())
				{

					_watchedCertificates = cfg.Root.Element("ControlObjList")
						.Elements("Obj")
						.Where((elt => elt.Attribute("type").Value.ToLower() == "cert"))
						.ToDictionary(
							elt => elt.Element("Thumbprint").Value.Replace(" ", string.Empty).Trim(),
							elt =>
							{
								X509Certificate2 foundCert =
									CertificateHelper.SearchCertificateByThumbprint(elt.Element("Thumbprint").Value);
								if (foundCert == null)
								{
									wasErrorDuringCertWatcherInit = true;
								}

								return foundCert;
							}
						);
					_watchedCertificatesDaysBeforeAlert =
						cfg.Root.Element("ControlObjList")
							.Elements("Obj")
							.Where((elt => elt.Attribute("type").Value.ToLower() == "cert"))
							.ToDictionary(
								elt => elt.Element("Thumbprint").Value.Replace(" ", string.Empty).Trim(),
								elt => int.Parse(elt.Attribute("daysBeforeAlert").Value)
							);
					_watchedCertificatesScripts = cfg.Root.Element("ControlObjList")
						.Elements("Obj")
						.Where((elt => elt.Attribute("type").Value.ToLower() == "cert"))
						.ToDictionary(
							elt => elt.Element("Thumbprint").Value.Replace(" ", string.Empty).Trim(),
							elt => elt.Element("Exec").Value
						);
				}

				if (_watchedCertificates != null
					&& _certificcateCheckIntervalSeconds != -1
					&& !wasErrorDuringCertWatcherInit)
				{
					_certificateWatcherIsConfigured = true;
				}
				#endregion

				_emailNotifier = new EmailNotifier(
					cfg.Root.Element("SmtpServer"),
					cfg.Root.Element("AdminEmailList"),
					cfg.Root.Element("EmailTemplates"));
				Console.WriteLine($"Configuration loaded!{Environment.NewLine}");

				PrintConfig();

				Console.WriteLine("Configuring watchers...");

				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Configuration load failed!");
				Console.WriteLine(e.Message);
				return false;
			}
		}

		private static void PrintConfig()
		{
			Console.WriteLine($"{new string('-', 16)}");
			Console.WriteLine($"Checkup interval: {_tmrIntervalSeconds / 60} minute(s)");
			if (_certificateWatcherIsConfigured)
			{
				Console.WriteLine($"Certificates checkup interval: {_certificcateCheckIntervalSeconds / 60 / 60 / 24} day(s)");
			}

			Console.WriteLine($"Watched objects:{Environment.NewLine}");

			foreach (KeyValuePair<string, decimal> disks in _watchedDisksThresholds)
			{
				Console.WriteLine(
					$"Disk {disks.Key}{Environment.NewLine}\t{_watchedDisksThresholdTypes[disks.Key].ToString()} space threshold {disks.Value} GB{Environment.NewLine}\tScript string {_watchedDisksScripts[disks.Key]}");
			}

			Console.WriteLine(
				_virtualMemoryIsConfigured
					? $"Virtual memory:{Environment.NewLine}\t{_virtualMemoryThresholdType.ToString()} space threshold {_virtualMemoryThresholdGb} GB{Environment.NewLine}\tScript string {_virtualMemoryThresholdScript}"
					: "Virtual memory watcher offline!");

			if (_certificateWatcherIsConfigured)
			{
				Console.WriteLine($"{Environment.NewLine}Certificates:{Environment.NewLine}");
				int i = 0;
				foreach (KeyValuePair<string, X509Certificate2> certificates in _watchedCertificates)
				{
					Console.WriteLine(
						$"{++i}) {certificates.Value.SubjectName.Name}{Environment.NewLine}\tDays before alert {_watchedCertificatesDaysBeforeAlert[certificates.Key]}.{Environment.NewLine}\tScript string {_watchedCertificatesScripts[certificates.Key]}{Environment.NewLine}");
				}
			}
			else
			{
				Console.WriteLine("Certificate watcher is offline!");
			}

			Console.WriteLine($"{new string('-', 16)}{Environment.NewLine}");
		} 

		#endregion

		#endregion

		#region Methods for notifying

		private static void Warn(
			string watcherName,
			decimal currentState,
			decimal limit,
			ThresholdType limitType,
			string scriptPath,
			bool isWarning)
		{
			ConsoleColor defaultColor = Console.ForegroundColor;
			if (isWarning)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("WARNING! ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write($"{watcherName} ");
				Console.ForegroundColor = defaultColor;
				Console.WriteLine(
					$"space {currentState:####.#} GB beyond limit of {limit:####.#} GB!"
				);
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("INFO! ");
				Console.ForegroundColor = defaultColor;
				Console.WriteLine(
					$"{watcherName} space {currentState:####.#} GB is back within the limit of {limit:####.#} GB!"
				);
			}

			RunScriptAndEmail(currentState, limit, limitType, scriptPath, watcherName, isWarning);
		}

		private static void Warn(X509Certificate2 cert, string scriptPath)
		{
			ConsoleColor defaultColor = Console.ForegroundColor;

			TimeSpan expiresIn = cert.NotAfter - DateTime.Now;

			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("WARNING! ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write($"Certificate {cert.SubjectName.Name} ");
			Console.ForegroundColor = defaultColor;
			bool isExpired = false;
			if (expiresIn.Days < 0)
			{
				isExpired = true;
				Console.WriteLine(
					$"expired {-expiresIn.Days} days ago!"
				);
			}
			else
			{
				Console.WriteLine(
					$"expires in {expiresIn.Days} days!"
				);
			}

			RunScriptAndEmail(cert, expiresIn.Days, scriptPath, isExpired);
		}

		private static void RunScriptAndEmail(
			decimal currentState,
			decimal limit,
			ThresholdType limitType,
			string scriptPath,
			string watcherName,
			bool isWarning)
		{
			//NOTE: isWarning == false means that notification is about watcher back within limits
			string[] scriptParts = scriptPath.Split(new[] {' '}, 2);

			ProcessStartInfo scriptInfo = new ProcessStartInfo(
				scriptParts[0],
				scriptParts[1]
					.Replace("%value%", currentState.ToString("####.#").Replace(",", "."))
					.Replace("%limit%", limit.ToString("####.#").Replace(",", "."))
					.Replace("%type%", limitType.ToString().ToLower())
			);

			try
			{
				Process.Start(scriptInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Script start failed. Message: {ex.Message}");
			}

			if (_emailNotifier.IsConfigured)
			{
				try
				{
					_emailNotifier.SendMail(
						watcherName,
						currentState,
						limit,
						isWarning
							? EventType.Warning
							: EventType.Ok);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Email send failed. Message: {ex.Message}");
				}
			}
		}

		private static void RunScriptAndEmail(
			X509Certificate2 cert,
			int expiresInDays,
			string scriptPath,
			bool isExpired)
		{
			string[] scriptParts = scriptPath.Split(new[] {' '}, 2);

			ProcessStartInfo scriptInfo = new ProcessStartInfo(
				scriptParts[0],
				scriptParts[1]
					.Replace("%cert_subject%", $"Certificate {cert.SubjectName.Name}")
					.Replace("%expires_in%", expiresInDays.ToString().Replace(",", "."))
			);

			try
			{
				Process.Start(scriptInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Script start failed. Message: {ex.Message}");
			}

			if (_emailNotifier.IsConfigured)
			{
				try
				{
					_emailNotifier.SendMail(
						cert,
						expiresInDays,
						isExpired
							? EventType.CertExpired
							: EventType.CertExpires);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Email send failed. Message: {ex.Message}");
				}
			}
		}

		#endregion
		
		#region Methods for checking various system parts

		private static void CheckRam()
		{
			if (_virtualMemoryIsConfigured)
			{
				foreach (var o in _searcher.Get())
				{
					var item = (ManagementObject)o;
					bool wasWarning = false;
					decimal freeVirtualMemoryGb = (decimal)(ulong)item["FreeVirtualMemory"] / 1024L / 1024L;
					decimal usedVirtualMemory = (decimal)(ulong)item["TotalVirtualMemorySize"] / 1024L / 1024L
						- (decimal)(ulong)item["FreeVirtualMemory"] / 1024L / 1024L;
					if (_virtualMemoryThresholdType == ThresholdType.Free)
					{
						if (!_virtualMemoryWarning)
						{
							if (freeVirtualMemoryGb < _virtualMemoryThresholdGb)
							{
								_virtualMemoryWarning = true;
								wasWarning = true;
								Warn(
									"Free virtual memory",
									freeVirtualMemoryGb,
									_virtualMemoryThresholdGb,
									_virtualMemoryThresholdType,
									_virtualMemoryThresholdScript,
									true);
							}
						}
						else
						{
							if (freeVirtualMemoryGb > _virtualMemoryThresholdGb)
							{
								_virtualMemoryWarning = false;
								wasWarning = true;
								Warn(
									"Free virtual memory",
									freeVirtualMemoryGb,
									_virtualMemoryThresholdGb,
									_virtualMemoryThresholdType,
									_virtualMemoryThresholdScript,
									false);
							}
						}

						if (!wasWarning)
						{
							Console.WriteLine($"Free Virtual Memory: {freeVirtualMemoryGb:####.#} GB");
						}

						Console.WriteLine($"Used Virtual Memory: {usedVirtualMemory:####.#} GB");
					}
					else
					{
						if (!_virtualMemoryWarning)
						{
							if (usedVirtualMemory > _virtualMemoryThresholdGb)
							{
								_virtualMemoryWarning = true;
								wasWarning = true;
								Warn(
									"Used virtual memory",
									usedVirtualMemory,
									_virtualMemoryThresholdGb,
									_virtualMemoryThresholdType,
									_virtualMemoryThresholdScript,
									true);
							}
						}
						else
						{
							if (usedVirtualMemory < _virtualMemoryThresholdGb)
							{
								_virtualMemoryWarning = false;
								wasWarning = true;
								Warn(
									"Used virtual memory",
									usedVirtualMemory,
									_virtualMemoryThresholdGb,
									_virtualMemoryThresholdType,
									_virtualMemoryThresholdScript,
									false);
							}
						}

						Console.WriteLine($"Free Virtual Memory: {freeVirtualMemoryGb:####.#} GB");
						if (!wasWarning)
						{
							Console.WriteLine($"Used Virtual Memory: {usedVirtualMemory:####.#} GB");
						}
					}
				}
			}
		}

		private static void CheckHdd()
		{
			foreach (KeyValuePair<string, long> drive in GetDriveStatuses(_watchedDisksThresholdTypes))
			{
				bool wasWarning = false;
				decimal spaceLeftOnDriveGb =
					(decimal)(drive.Value) / 1024L / 1024L / 1024L; //free or used - depends on config
				ThresholdType tt = _watchedDisksThresholdTypes[drive.Key];
				//notify hard drive
				if (!_watchedDisksWarnings[drive.Key])
				{
					if ((tt == ThresholdType.Free && spaceLeftOnDriveGb < _watchedDisksThresholds[drive.Key])
						|| (tt == ThresholdType.Used && spaceLeftOnDriveGb > _watchedDisksThresholds[drive.Key])
					)
					{
						_watchedDisksWarnings[drive.Key] = true;
						wasWarning = true;
						Warn(
							$"Drive {drive.Key} {_watchedDisksThresholdTypes[drive.Key].ToString().ToLower()}",
							spaceLeftOnDriveGb,
							_watchedDisksThresholds[drive.Key],
							tt,
							_watchedDisksScripts[drive.Key],
							_watchedDisksWarnings[drive.Key]);
					}
				}
				else
				{
					if ((tt == ThresholdType.Free && spaceLeftOnDriveGb > _watchedDisksThresholds[drive.Key])
						|| (tt == ThresholdType.Used && spaceLeftOnDriveGb < _watchedDisksThresholds[drive.Key])
					)
					{
						_watchedDisksWarnings[drive.Key] = false;
						wasWarning = true;
						Warn(
							$"Drive {drive.Key} {_watchedDisksThresholdTypes[drive.Key].ToString().ToLower()}",
							spaceLeftOnDriveGb,
							_watchedDisksThresholds[drive.Key],
							tt,
							_watchedDisksScripts[drive.Key],
							_watchedDisksWarnings[drive.Key]);
					}
				}

				if (!wasWarning)
				{
					Console.WriteLine(
						_watchedDisksThresholdTypes[drive.Key] == ThresholdType.Free
							? $"Drive {drive.Key} free space left: {spaceLeftOnDriveGb:####.#} GB"
							: $"Drive {drive.Key} used space: {spaceLeftOnDriveGb:####.#} GB");
				}

			}
		}

		private static void CheckCertificates()
		{
			if ((_secondsPassedWatching >= _certificcateCheckIntervalSeconds) && _certificateWatcherIsConfigured)
			{
				_secondsPassedWatching = 0;
				foreach (KeyValuePair<string, X509Certificate2> certs in _watchedCertificates)
				{
					if (certs.Value.NotAfter.AddDays(-_watchedCertificatesDaysBeforeAlert[certs.Key])
						< DateTime.Now)
					{
						Warn(certs.Value, _watchedCertificatesScripts[certs.Key]);
					}
					else
					{
						Console.WriteLine($"Certificate {certs.Value.SubjectName.Name} expires on {certs.Value.NotAfter}");
					}
				}
			}
		}

		static Dictionary<string, long> GetDriveStatuses(Dictionary<string, ThresholdType> driveLettersThresholdTypes)
		{
			return driveLettersThresholdTypes.ToDictionary(
				kv => kv.Key,
				kv =>
				{
					return kv.Value == ThresholdType.Used
						? DriveInfo.GetDrives()
							.Where((di) => di.Name.TrimEnd('\\') == kv.Key)
							.Select(di => di.TotalSize - di.TotalFreeSpace)
							.FirstOrDefault()
						: DriveInfo.GetDrives()
							.Where((di) => di.Name.TrimEnd('\\') == kv.Key)
							.Select(di => di.TotalFreeSpace)
							.FirstOrDefault();
				}
			);
		}

		#endregion

		#region Methods for processing check results

		private static void ProcessCheckResult(ResourceCheckResult result)
		{
			if (result.IsOk)
			{
				foreach (var statusMessage in result.GetStatusReport())
				{
					Console.WriteLine(statusMessage);
				}
				return;
			}

			var hasValidCommandLine = result.GetCommandLineParts(out string executableName, out string arguments);
			if (hasValidCommandLine)
			{
				ProcessStartInfo scriptInfo = new ProcessStartInfo(executableName, arguments);
				try
				{
					Process.Start(scriptInfo);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Script start failed. Message: {ex.Message}");
				}
			}

			_emailNotifier.SendMail(result);
		}
		
		#endregion
	}
}
