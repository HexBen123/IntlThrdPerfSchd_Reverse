using System;
using System.Text;

namespace IntlThrdPerfSchd
{
	public class SchedulerService
	{
		private TransformerScheduler _scheduler;

		private StringBuilder _statusLog;

		private readonly object _lock = new object();

		private bool _isRunning;

		public SchedulerService()
		{
			_statusLog = new StringBuilder();
			_isRunning = false;
		}

		public void Start()
		{
			_scheduler = new TransformerScheduler();
			_isRunning = true;
			LogStatus("Transformer Scheduler Service Started");
		}

		public void Stop()
		{
			_isRunning = false;
			LogStatus("Transformer Scheduler Service Stopped");
		}

		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			try
			{
				return _scheduler.Schedule(threadFeatures, coreFeatures, numCores);
			}
			catch (Exception ex)
			{
				LogStatus("ERROR: " + ex.Message);
				return new ScheduleResult
				{
					BestCoreIndex = 0,
					AffinityMask = IntPtr.Zero
				};
			}
		}

		public void UpdateTAT(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			try
			{
				_scheduler.UpdateTAT(currentTAT, energyValue, newMetricValue, extraMetricValue, little_num, big_num);
			}
			catch (Exception ex)
			{
				LogStatus("ERROR in UpdateTAT: " + ex.Message);
			}
		}

		public string GetStatusReport()
		{
			lock (_lock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Transformer Scheduler Status");
				stringBuilder.AppendLine($"Running: {_isRunning}");
				stringBuilder.AppendLine($"Current TAT: {_scheduler.GetCurrentTAT():F2}ms");
				stringBuilder.AppendLine($"Baseline TAT: {_scheduler.GetBaselineTAT():F2}ms");
				stringBuilder.AppendLine($"Recent Reward: {_scheduler.GetRecentAvgReward():F6}");
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(_scheduler?.GetLearningReport() ?? "");
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(_scheduler?.GetRecentDecisions(5) ?? "");
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("=== Recent Log ===");
				stringBuilder.Append(_statusLog);
				return stringBuilder.ToString();
			}
		}

		private void LogStatus(string message)
		{
			lock (_lock)
			{
				string text = $"[{DateTime.Now:HH:mm:ss}] {message}";
				_statusLog.Insert(0, text + Environment.NewLine);
				if (_statusLog.Length > 10000)
				{
					_statusLog.Length = 10000;
				}
			}
		}
	}
}
