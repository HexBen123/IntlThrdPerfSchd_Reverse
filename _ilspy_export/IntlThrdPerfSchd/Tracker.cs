using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd;

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
		LinkedListNode<ThreadData> linkedListNode = _dataList.First;
		while (linkedListNode != null && linkedListNode.Value.Timestamp <= newData.Timestamp)
		{
			linkedListNode = linkedListNode.Next;
		}
		if (linkedListNode == null)
		{
			_dataList.AddLast(newData);
		}
		else
		{
			_dataList.AddBefore(linkedListNode, newData);
		}
	}

	private void MaintainWindowSize()
	{
		while (_dataList.Count > _windowSize)
		{
			ThreadData value = _dataList.First.Value;
			_dataList.RemoveFirst();
			if (_currentMax != null && value.Timestamp == _currentMax.Timestamp)
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
			List<string> values = new List<string>
			{
				$"数据总数: {_dataList.Count}",
				$"窗口大小: {_windowSize}",
				$"最早时间戳: {_dataList.First.Value.Timestamp}",
				$"最晚时间戳: {_dataList.Last.Value.Timestamp}",
				$"最大值: {GetMaxValue()}",
				$"最大值线程ID: {GetMaxThreadId()}"
			};
			return string.Join(Environment.NewLine, values);
		}
	}
}
