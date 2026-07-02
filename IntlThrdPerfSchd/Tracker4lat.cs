using System;
using System.Collections.Generic;
using System.Linq;

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
				if (_currentMaxValue1 != null && value.Timestamp == _currentMaxValue1.Timestamp)
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
				List<ThreadData> source = _dataList.ToList();
				IOrderedEnumerable<ThreadData> source2;
				if (maximizeValue1)
				{
					source2 = source.OrderByDescending((ThreadData d) => d.Value1);
				}
				else
				{
					source2 = source.OrderBy((ThreadData d) => d.Value1);
				}
				if (maximizeValue2)
				{
					source2 = source2.ThenByDescending((ThreadData d) => d.Value2);
				}
				else
				{
					source2 = source2.ThenBy((ThreadData d) => d.Value2);
				}
				return source2.FirstOrDefault()?.ThreadId ?? (-1);
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
			List<ThreadData> list = new List<ThreadData>();
			foreach (ThreadData data in dataList)
			{
				bool flag = false;
				foreach (ThreadData data2 in dataList)
				{
					if (Dominates(data2, data, maximizeValue1, maximizeValue2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(data);
				}
			}
			return list;
		}

		private bool Dominates(ThreadData a, ThreadData b, bool max1, bool max2)
		{
			bool num = (max1 ? (a.Value1 >= b.Value1) : (a.Value1 <= b.Value1));
			bool flag = (max2 ? (a.Value2 >= b.Value2) : (a.Value2 <= b.Value2));
			bool flag2 = (max1 ? (a.Value1 > b.Value1) : (a.Value1 < b.Value1));
			bool flag3 = (max2 ? (a.Value2 > b.Value2) : (a.Value2 < b.Value2));
			if (num && flag)
			{
				return flag2 || flag3;
			}
			return false;
		}

		private int SelectBestFromParetoFront(List<ThreadData> paretoFront, bool max1, bool max2, double weight1, double weight2)
		{
			double num = paretoFront.Min((ThreadData d) => d.Value1);
			double num2 = paretoFront.Max((ThreadData d) => d.Value1) - num;
			double num3 = paretoFront.Min((ThreadData d) => d.Value2);
			double num4 = paretoFront.Max((ThreadData d) => d.Value2) - num3;
			ThreadData threadData = null;
			double num5 = double.MinValue;
			foreach (ThreadData item in paretoFront)
			{
				double num6 = ((num2 > 0.0) ? ((item.Value1 - num) / num2) : 0.5);
				double num7 = ((num4 > 0.0) ? ((item.Value2 - num3) / num4) : 0.5);
				double num8 = (max1 ? num6 : (1.0 - num6));
				double num9 = (max2 ? num7 : (1.0 - num7));
				double num10 = weight1 * num8 + weight2 * num9;
				if (num10 > num5)
				{
					num5 = num10;
					threadData = item;
				}
			}
			return threadData?.ThreadId ?? (-1);
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
				List<string> values = new List<string>
				{
					$"数据总数: {_dataList.Count}",
					$"窗口大小: {_windowSize}",
					$"最早时间戳: {_dataList.First.Value.Timestamp}",
					$"最晚时间戳: {_dataList.Last.Value.Timestamp}",
					$"第一个数值最大值: {GetMaxValue1()}",
					$"第一个数值最大值线程ID: {GetMaxValue1ThreadId()}"
				};
				return string.Join(Environment.NewLine, values);
			}
		}
	}
}
