using System.Collections.Generic;
using System.Linq;
using System;

namespace IntlThrdPerfSchd
{

public class Tracker4lat
{
	private class ThreadData
	{
		public int ThreadId { get; set; }

		public double Value1 { get; set; }

		public double Value2 { get; set; }

		public long Timestamp { get; set; }

		public ThreadData(int threadId, double value1, double value2)
		{
			ThreadId = threadId;
			Value1 = value1;
			Value2 = value2;
			Timestamp = DateTime.Now.Ticks;
		}
	}

	private readonly object _lockObject = new object();

	private readonly LinkedList<ThreadData> _dataList = new LinkedList<ThreadData>();

	private readonly int _windowSize;

	private ThreadData _currentMaxValue1;

	private bool _maxValue1NeedsRecalculation;

	public int DataCount
	{
		get
		{
			lock (_lockObject)
			{
				return _dataList.Count;
			}
		}
	}

	public int WindowSize => _windowSize;

	public Tracker4lat(int windowSize = 1000)
	{
		if (windowSize <= 0)
		{
			throw new ArgumentException("窗口大小必须大于0", "windowSize");
		}
		_windowSize = windowSize;
		_maxValue1NeedsRecalculation = false;
	}

	public void AddData(int threadId, double value1, double value2)
	{
		ThreadData newData = new ThreadData(threadId, value1, value2);
		lock (_lockObject)
		{
			InsertInOrder(newData);
			MaintainWindowSize();
			UpdateMaxValue(newData);
		}
	}

	private void InsertInOrder(ThreadData newData)
	{
		if (_dataList.Count == 0 || newData.Timestamp >= _dataList.Last.Value.Timestamp)
		{
			_dataList.AddLast(newData);
			return;
		}
		LinkedListNode<ThreadData> current = _dataList.First;
		while (current != null && current.Value.Timestamp <= newData.Timestamp)
		{
			current = current.Next;
		}
		if (current == null)
		{
			_dataList.AddLast(newData);
		}
		else
		{
			_dataList.AddBefore(current, newData);
		}
	}

	private void MaintainWindowSize()
	{
		while (_dataList.Count > _windowSize)
		{
			ThreadData removedData = _dataList.First.Value;
			_dataList.RemoveFirst();
			if (_currentMaxValue1 != null && removedData.Timestamp == _currentMaxValue1.Timestamp)
			{
				_maxValue1NeedsRecalculation = true;
			}
		}
	}

	private void UpdateMaxValue(ThreadData newData)
	{
		if (_currentMaxValue1 == null)
		{
			_currentMaxValue1 = newData;
			_maxValue1NeedsRecalculation = false;
		}
		else if (newData.Value1 > _currentMaxValue1.Value1)
		{
			_currentMaxValue1 = newData;
			_maxValue1NeedsRecalculation = false;
		}
		else if (_maxValue1NeedsRecalculation)
		{
			RecalculateMaxValue();
		}
	}

	private void RecalculateMaxValue()
	{
		_currentMaxValue1 = _dataList.OrderByDescending((ThreadData d) => d.Value1).FirstOrDefault();
		_maxValue1NeedsRecalculation = false;
	}

	public int GetMaxValue1ThreadId()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return -1;
			}
			if (_maxValue1NeedsRecalculation)
			{
				RecalculateMaxValue();
			}
			return _currentMaxValue1?.ThreadId ?? (-1);
		}
	}

	public double GetMaxValue1()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return double.MinValue;
			}
			if (_maxValue1NeedsRecalculation)
			{
				RecalculateMaxValue();
			}
			return _currentMaxValue1?.Value1 ?? double.MinValue;
		}
	}

	public void Clear()
	{
		lock (_lockObject)
		{
			_dataList.Clear();
			_currentMaxValue1 = null;
			_maxValue1NeedsRecalculation = false;
		}
	}

	public int GetOptimalThreadId(bool maximizeValue1, bool maximizeValue2)
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return -1;
			}
			List<ThreadData> dataList = _dataList.ToList();
			IOrderedEnumerable<ThreadData> orderedData;
			if (maximizeValue1)
			{
				orderedData = dataList.OrderByDescending((ThreadData d) => d.Value1);
			}
			else
			{
				orderedData = dataList.OrderBy((ThreadData d) => d.Value1);
			}
			if (maximizeValue2)
			{
				orderedData = orderedData.ThenByDescending((ThreadData d) => d.Value2);
			}
			else
			{
				orderedData = orderedData.ThenBy((ThreadData d) => d.Value2);
			}
			return orderedData.FirstOrDefault()?.ThreadId ?? (-1);
		}
	}

	public int GetParetoOptimalThreadId(bool maximizeValue1, bool maximizeValue2)
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return -1;
			}
			List<ThreadData> dataList = _dataList.ToList();
			List<ThreadData> paretoFront = GetParetoFront(dataList, maximizeValue1, maximizeValue2);
			if (paretoFront.Count == 0)
			{
				return -1;
			}
			if (paretoFront.Count == 1)
			{
				return paretoFront[0].ThreadId;
			}
			return SelectBestFromParetoFront(paretoFront, maximizeValue1, maximizeValue2, 0.5, 0.5);
		}
	}

	private List<ThreadData> GetParetoFront(List<ThreadData> dataList, bool maximizeValue1, bool maximizeValue2)
	{
		List<ThreadData> paretoFront = new List<ThreadData>();
		foreach (ThreadData candidate in dataList)
		{
			bool isDominated = false;
			foreach (ThreadData other in dataList)
			{
				if (Dominates(other, candidate, maximizeValue1, maximizeValue2))
				{
					isDominated = true;
					break;
				}
			}
			if (!isDominated)
			{
				paretoFront.Add(candidate);
			}
		}
		return paretoFront;
	}

	private bool Dominates(ThreadData a, ThreadData b, bool max1, bool max2)
	{
		bool num = (max1 ? (a.Value1 >= b.Value1) : (a.Value1 <= b.Value1));
		bool betterOrEqual2 = (max2 ? (a.Value2 >= b.Value2) : (a.Value2 <= b.Value2));
		bool strictlyBetter1 = (max1 ? (a.Value1 > b.Value1) : (a.Value1 < b.Value1));
		bool strictlyBetter2 = (max2 ? (a.Value2 > b.Value2) : (a.Value2 < b.Value2));
		if (num && betterOrEqual2)
		{
			return strictlyBetter1 || strictlyBetter2;
		}
		return false;
	}

	private int SelectBestFromParetoFront(List<ThreadData> paretoFront, bool max1, bool max2, double weight1, double weight2)
	{
		double min1 = paretoFront.Min((ThreadData d) => d.Value1);
		double range1 = paretoFront.Max((ThreadData d) => d.Value1) - min1;
		double min2 = paretoFront.Min((ThreadData d) => d.Value2);
		double range2 = paretoFront.Max((ThreadData d) => d.Value2) - min2;
		ThreadData bestCandidate = null;
		double bestScore = double.MinValue;
		foreach (ThreadData candidate in paretoFront)
		{
			double normalized1 = ((range1 > 0.0) ? ((candidate.Value1 - min1) / range1) : 0.5);
			double normalized2 = ((range2 > 0.0) ? ((candidate.Value2 - min2) / range2) : 0.5);
			double score1 = (max1 ? normalized1 : (1.0 - normalized1));
			double score2 = (max2 ? normalized2 : (1.0 - normalized2));
			double totalScore = weight1 * score1 + weight2 * score2;
			if (totalScore > bestScore)
			{
				bestScore = totalScore;
				bestCandidate = candidate;
			}
		}
		return bestCandidate?.ThreadId ?? (-1);
	}

	public double GetMaxValue2()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return double.MinValue;
			}
			return _dataList.Max((ThreadData d) => d.Value2);
		}
	}

	public double GetMinValue2()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return double.MaxValue;
			}
			return _dataList.Min((ThreadData d) => d.Value2);
		}
	}

	public string GetStatistics()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return "没有数据";
			}
			List<string> stats = new List<string>
			{
				$"数据总数: {_dataList.Count}",
				$"窗口大小: {_windowSize}",
				$"最早时间戳: {_dataList.First.Value.Timestamp}",
				$"最晚时间戳: {_dataList.Last.Value.Timestamp}",
				$"第一个数值最大值: {GetMaxValue1()}",
				$"第一个数值最大值线程ID: {GetMaxValue1ThreadId()}"
			};
			return string.Join(Environment.NewLine, stats);
		}
	}
}

public class Tracker
{
	private class ThreadData
	{
		public int ThreadId { get; set; }

		public double Value { get; set; }

		public long Timestamp { get; set; }

		public ThreadData(int threadId, double value)
		{
			ThreadId = threadId;
			Value = value;
			Timestamp = DateTime.Now.Ticks;
		}
	}

	private readonly object _lockObject = new object();

	private readonly LinkedList<ThreadData> _dataList = new LinkedList<ThreadData>();

	private readonly int _windowSize;

	private ThreadData _currentMax;

	private bool _maxNeedsRecalculation;

	public int DataCount
	{
		get
		{
			lock (_lockObject)
			{
				return _dataList.Count;
			}
		}
	}

	public int WindowSize => _windowSize;

	public Tracker(int windowSize = 1000)
	{
		if (windowSize <= 0)
		{
			throw new ArgumentException("窗口大小必须大于0", "windowSize");
		}
		_windowSize = windowSize;
		_maxNeedsRecalculation = false;
	}

	public void AddData(int threadId, double value)
	{
		ThreadData newData = new ThreadData(threadId, value);
		lock (_lockObject)
		{
			InsertInOrder(newData);
			MaintainWindowSize();
			UpdateMaxValue(newData);
		}
	}

	private void InsertInOrder(ThreadData newData)
	{
		if (_dataList.Count == 0 || newData.Timestamp >= _dataList.Last.Value.Timestamp)
		{
			_dataList.AddLast(newData);
			return;
		}
		LinkedListNode<ThreadData> current = _dataList.First;
		while (current != null && current.Value.Timestamp <= newData.Timestamp)
		{
			current = current.Next;
		}
		if (current == null)
		{
			_dataList.AddLast(newData);
		}
		else
		{
			_dataList.AddBefore(current, newData);
		}
	}

	private void MaintainWindowSize()
	{
		while (_dataList.Count > _windowSize)
		{
			ThreadData removedData = _dataList.First.Value;
			_dataList.RemoveFirst();
			if (_currentMax != null && removedData.Timestamp == _currentMax.Timestamp)
			{
				_maxNeedsRecalculation = true;
			}
		}
	}

	private void UpdateMaxValue(ThreadData newData)
	{
		if (_currentMax == null)
		{
			_currentMax = newData;
			_maxNeedsRecalculation = false;
		}
		else if (newData.Value > _currentMax.Value)
		{
			_currentMax = newData;
			_maxNeedsRecalculation = false;
		}
		else if (_maxNeedsRecalculation)
		{
			RecalculateMaxValue();
		}
	}

	private void RecalculateMaxValue()
	{
		_currentMax = _dataList.OrderByDescending((ThreadData d) => d.Value).FirstOrDefault();
		_maxNeedsRecalculation = false;
	}

	public int GetMaxThreadId()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return -1;
			}
			if (_maxNeedsRecalculation)
			{
				RecalculateMaxValue();
			}
			return _currentMax?.ThreadId ?? (-1);
		}
	}

	public double GetMaxValue()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return double.MinValue;
			}
			if (_maxNeedsRecalculation)
			{
				RecalculateMaxValue();
			}
			return _currentMax?.Value ?? double.MinValue;
		}
	}

	public void Clear()
	{
		lock (_lockObject)
		{
			_dataList.Clear();
			_currentMax = null;
			_maxNeedsRecalculation = false;
		}
	}

	public string GetStatistics()
	{
		lock (_lockObject)
		{
			if (_dataList.Count == 0)
			{
				return "没有数据";
			}
			List<string> stats = new List<string>
			{
				$"数据总数: {_dataList.Count}",
				$"窗口大小: {_windowSize}",
				$"最早时间戳: {_dataList.First.Value.Timestamp}",
				$"最晚时间戳: {_dataList.Last.Value.Timestamp}",
				$"最大值: {GetMaxValue()}",
				$"最大值线程ID: {GetMaxThreadId()}"
			};
			return string.Join(Environment.NewLine, stats);
		}
	}
}

public class ThreadPerformanceTracker
{
	private class ThreadData
	{
		public int ThreadId { get; set; }

		public long InstructionsPerCount { get; set; }

		public long LastUpdateTicks { get; set; }
	}

	public class QueryResult
	{
		public int ThreadId { get; set; }

		public long InstructionsPerCount { get; set; }

		public DateTime WindowStartTime { get; set; }

		public DateTime WindowEndTime { get; set; }
	}

	private readonly object _lockObject = new object();

	private readonly TimeSpan _windowDuration;

	private Dictionary<int, ThreadData> _currentWindowData;

	private Dictionary<int, ThreadData> _previousWindowData;

	private long _currentWindowStartTicks;

	private long _previousWindowStartTicks;

	public ThreadPerformanceTracker(TimeSpan windowDuration)
		: this(windowDuration.Ticks)
	{
	}

	public ThreadPerformanceTracker(long windowTicks)
	{
		_windowDuration = TimeSpan.FromTicks(windowTicks);
		_currentWindowData = new Dictionary<int, ThreadData>();
		_previousWindowData = new Dictionary<int, ThreadData>();
		_currentWindowStartTicks = DateTime.Now.Ticks;
		_previousWindowStartTicks = _currentWindowStartTicks - windowTicks;
	}

	public void AddOrUpdateData(int threadId, long instructionsPerCount)
	{
		lock (_lockObject)
		{
			CheckAndSwitchWindow();
			long currentTicks = DateTime.Now.Ticks;
			if (_currentWindowData.TryGetValue(threadId, out var existingData))
			{
				if (instructionsPerCount > existingData.InstructionsPerCount)
				{
					existingData.InstructionsPerCount = instructionsPerCount;
				}
				existingData.LastUpdateTicks = currentTicks;
			}
			else
			{
				_currentWindowData[threadId] = new ThreadData
				{
					ThreadId = threadId,
					InstructionsPerCount = instructionsPerCount,
					LastUpdateTicks = currentTicks
				};
			}
		}
	}

	public int GetMaxThreadId()
	{
		lock (_lockObject)
		{
			CheckAndSwitchWindow();
			if (_previousWindowData.Any())
			{
				return _previousWindowData.Values.OrderByDescending((ThreadData data) => data.InstructionsPerCount).FirstOrDefault()?.ThreadId ?? (-1);
			}
			if (_currentWindowData.Any())
			{
				return _currentWindowData.Values.OrderByDescending((ThreadData data) => data.InstructionsPerCount).FirstOrDefault()?.ThreadId ?? (-1);
			}
			return -1;
		}
	}

	public List<QueryResult> GetPreviousWindowRanking(int topN = 0)
	{
		lock (_lockObject)
		{
			CheckAndSwitchWindow();
			IEnumerable<QueryResult> previousWindowResults = from data in _previousWindowData.Values
				orderby data.InstructionsPerCount descending
				select new QueryResult
				{
					ThreadId = data.ThreadId,
					InstructionsPerCount = data.InstructionsPerCount,
					WindowStartTime = new DateTime(_previousWindowStartTicks),
					WindowEndTime = new DateTime(_previousWindowStartTicks + _windowDuration.Ticks)
				};
			if (previousWindowResults.Any())
			{
				if (topN > 0)
				{
					previousWindowResults = previousWindowResults.Take(topN);
				}
				return previousWindowResults.ToList();
			}
			if (_currentWindowData.Any())
			{
				IEnumerable<QueryResult> currentWindowResults = from data in _currentWindowData.Values
					orderby data.InstructionsPerCount descending
					select new QueryResult
					{
						ThreadId = data.ThreadId,
						InstructionsPerCount = data.InstructionsPerCount,
						WindowStartTime = new DateTime(_currentWindowStartTicks),
						WindowEndTime = DateTime.Now
					};
				if (topN > 0)
				{
					currentWindowResults = currentWindowResults.Take(topN);
				}
				return currentWindowResults.ToList();
			}
			return new List<QueryResult>();
		}
	}

	public List<QueryResult> GetCurrentWindowPreview(int topN = 0)
	{
		lock (_lockObject)
		{
			IEnumerable<QueryResult> results = from data in _currentWindowData.Values
				orderby data.InstructionsPerCount descending
				select new QueryResult
				{
					ThreadId = data.ThreadId,
					InstructionsPerCount = data.InstructionsPerCount,
					WindowStartTime = new DateTime(_currentWindowStartTicks),
					WindowEndTime = DateTime.Now
				};
			if (topN > 0)
			{
				results = results.Take(topN);
			}
			return results.ToList();
		}
	}

	public void ForceSwitchWindow()
	{
		lock (_lockObject)
		{
			SwitchWindow();
		}
	}

	private void CheckAndSwitchWindow()
	{
		long ticks = DateTime.Now.Ticks;
		long windowEndTicks = _currentWindowStartTicks + _windowDuration.Ticks;
		if (ticks >= windowEndTicks)
		{
			SwitchWindow();
		}
	}

	private void SwitchWindow()
	{
		_previousWindowData = new Dictionary<int, ThreadData>(_currentWindowData);
		_previousWindowStartTicks = _currentWindowStartTicks;
		_currentWindowData = new Dictionary<int, ThreadData>();
		_currentWindowStartTicks = DateTime.Now.Ticks;
	}

	public string GetStatus()
	{
		lock (_lockObject)
		{
			return $"当前窗口: {_currentWindowData.Count} 个线程, 上一个窗口: {_previousWindowData.Count} 个线程, " + $"当前窗口开始: {new DateTime(_currentWindowStartTicks):yyyy-MM-dd HH:mm:ss}, " + $"上一个窗口开始: {new DateTime(_previousWindowStartTicks):yyyy-MM-dd HH:mm:ss}";
		}
	}
}
}
