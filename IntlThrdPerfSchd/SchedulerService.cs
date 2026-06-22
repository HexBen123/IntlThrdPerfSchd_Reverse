using System;
using System.Diagnostics;
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

	public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
	{
		try
		{
			return _scheduler.Schedule(threadFeatures, coreFeatures, numCores);
		}
		catch (Exception ex)
		{
			LogStatus("ERROR: " + ex.Message);
			return 0;
		}
	}

	public void UpdateTAT(float currentTAT)
	{
		try
		{
			_scheduler.UpdateTAT(currentTAT);
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
			stringBuilder.AppendLine(_scheduler?.GetStatistics() ?? "Scheduler not initialized");
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
			string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
			_statusLog.Insert(0, logLine + Environment.NewLine);
			if (_statusLog.Length > 10000)
			{
				_statusLog.Length = 10000;
			}
		}
	}
}

public class SchedulerController
{
	private readonly TransformerScheduler _scheduler;

	private readonly StringBuilder _log;

	private readonly object _lock = new object();

	public SchedulerController()
	{
		_scheduler = new TransformerScheduler();
		_log = new StringBuilder();
	}

	public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
	{
		Stopwatch sw = Stopwatch.StartNew();
		try
		{
			int core = _scheduler.Schedule(threadFeatures, coreFeatures, numCores);
			sw.Stop();
			Log($"Scheduled to Core {core} in {(double)sw.ElapsedTicks * 1000000.0 / (double)Stopwatch.Frequency:F1} μs");
			return core;
		}
		catch (Exception ex)
		{
			Log("ERROR: " + ex.Message);
			throw;
		}
	}

	public void UpdateTAT(float currentTAT)
	{
		_scheduler.UpdateTAT(currentTAT);
		Log($"TAT updated: {currentTAT:F2}μs, Reward: {_scheduler.GetRecentAvgReward():F6}");
	}

	public string GetStatusReport()
	{
		lock (_lock)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Transformer Scheduler Status");
			stringBuilder.AppendLine($"Current TAT: {_scheduler.GetCurrentTAT():F2}ms");
			stringBuilder.AppendLine($"Baseline TAT: {_scheduler.GetBaselineTAT():F2}ms");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(_scheduler.GetStatistics());
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(_scheduler.GetLearningReport());
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(_scheduler.GetRecentDecisions());
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(_scheduler.ExportModel());
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("=== Recent Log ===");
			stringBuilder.Append(_log);
			return stringBuilder.ToString();
		}
	}

	public string GetStatistics()
	{
		return _scheduler.GetStatistics();
	}

	private void Log(string message)
	{
		lock (_lock)
		{
			string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
			_log.Insert(0, line + Environment.NewLine);
			if (_log.Length > 5000)
			{
				_log.Length = 5000;
			}
		}
	}
}
}
