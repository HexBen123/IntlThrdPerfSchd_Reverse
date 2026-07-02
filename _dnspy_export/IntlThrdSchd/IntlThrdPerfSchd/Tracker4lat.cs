using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000016 RID: 22
	public class Tracker4lat
	{
		// Token: 0x0600016D RID: 365 RVA: 0x00014C69 File Offset: 0x00012E69
		public Tracker4lat(int windowSize = 1000)
		{
			if (windowSize <= 0)
			{
				throw new ArgumentException("窗口大小必须大于0", "windowSize");
			}
			this._windowSize = windowSize;
			this._maxValue1NeedsRecalculation = false;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00014CAC File Offset: 0x00012EAC
		public void AddData(int threadId, double value1, double value2)
		{
			Tracker4lat.ThreadData threadData = new Tracker4lat.ThreadData(threadId, value1, value2);
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this.InsertInOrder(threadData);
				this.MaintainWindowSize();
				this.UpdateMaxValue(threadData);
			}
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00014D04 File Offset: 0x00012F04
		private void InsertInOrder(Tracker4lat.ThreadData newData)
		{
			if (this._dataList.Count == 0 || newData.Timestamp >= this._dataList.Last.Value.Timestamp)
			{
				this._dataList.AddLast(newData);
				return;
			}
			LinkedListNode<Tracker4lat.ThreadData> linkedListNode = this._dataList.First;
			while (linkedListNode != null && linkedListNode.Value.Timestamp <= newData.Timestamp)
			{
				linkedListNode = linkedListNode.Next;
			}
			if (linkedListNode == null)
			{
				this._dataList.AddLast(newData);
				return;
			}
			this._dataList.AddBefore(linkedListNode, newData);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00014D94 File Offset: 0x00012F94
		private void MaintainWindowSize()
		{
			while (this._dataList.Count > this._windowSize)
			{
				Tracker4lat.ThreadData value = this._dataList.First.Value;
				this._dataList.RemoveFirst();
				if (this._currentMaxValue1 != null && value.Timestamp == this._currentMaxValue1.Timestamp)
				{
					this._maxValue1NeedsRecalculation = true;
				}
			}
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00014DF4 File Offset: 0x00012FF4
		private void UpdateMaxValue(Tracker4lat.ThreadData newData)
		{
			if (this._currentMaxValue1 == null)
			{
				this._currentMaxValue1 = newData;
				this._maxValue1NeedsRecalculation = false;
				return;
			}
			if (newData.Value1 > this._currentMaxValue1.Value1)
			{
				this._currentMaxValue1 = newData;
				this._maxValue1NeedsRecalculation = false;
				return;
			}
			if (this._maxValue1NeedsRecalculation)
			{
				this.RecalculateMaxValue();
			}
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00014E48 File Offset: 0x00013048
		private void RecalculateMaxValue()
		{
			this._currentMaxValue1 = this._dataList.OrderByDescending((Tracker4lat.ThreadData d) => d.Value1).FirstOrDefault<Tracker4lat.ThreadData>();
			this._maxValue1NeedsRecalculation = false;
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00014E88 File Offset: 0x00013088
		public int GetMaxValue1ThreadId()
		{
			object lockObject = this._lockObject;
			int num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = -1;
				}
				else
				{
					if (this._maxValue1NeedsRecalculation)
					{
						this.RecalculateMaxValue();
					}
					Tracker4lat.ThreadData currentMaxValue = this._currentMaxValue1;
					num = ((currentMaxValue != null) ? currentMaxValue.ThreadId : (-1));
				}
			}
			return num;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00014EF8 File Offset: 0x000130F8
		public double GetMaxValue1()
		{
			object lockObject = this._lockObject;
			double num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = double.MinValue;
				}
				else
				{
					if (this._maxValue1NeedsRecalculation)
					{
						this.RecalculateMaxValue();
					}
					Tracker4lat.ThreadData currentMaxValue = this._currentMaxValue1;
					num = ((currentMaxValue != null) ? currentMaxValue.Value1 : double.MinValue);
				}
			}
			return num;
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000175 RID: 373 RVA: 0x00014F78 File Offset: 0x00013178
		public int DataCount
		{
			get
			{
				object lockObject = this._lockObject;
				int count;
				lock (lockObject)
				{
					count = this._dataList.Count;
				}
				return count;
			}
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00014FC0 File Offset: 0x000131C0
		public void Clear()
		{
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this._dataList.Clear();
				this._currentMaxValue1 = null;
				this._maxValue1NeedsRecalculation = false;
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000177 RID: 375 RVA: 0x00015014 File Offset: 0x00013214
		public int WindowSize
		{
			get
			{
				return this._windowSize;
			}
		}

		// Token: 0x06000178 RID: 376 RVA: 0x0001501C File Offset: 0x0001321C
		public int GetOptimalThreadId(bool maximizeValue1, bool maximizeValue2)
		{
			object lockObject = this._lockObject;
			int num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = -1;
				}
				else
				{
					List<Tracker4lat.ThreadData> list = this._dataList.ToList<Tracker4lat.ThreadData>();
					IOrderedEnumerable<Tracker4lat.ThreadData> orderedEnumerable;
					if (maximizeValue1)
					{
						orderedEnumerable = list.OrderByDescending((Tracker4lat.ThreadData d) => d.Value1);
					}
					else
					{
						orderedEnumerable = list.OrderBy((Tracker4lat.ThreadData d) => d.Value1);
					}
					if (maximizeValue2)
					{
						orderedEnumerable = orderedEnumerable.ThenByDescending((Tracker4lat.ThreadData d) => d.Value2);
					}
					else
					{
						orderedEnumerable = orderedEnumerable.ThenBy((Tracker4lat.ThreadData d) => d.Value2);
					}
					Tracker4lat.ThreadData threadData = orderedEnumerable.FirstOrDefault<Tracker4lat.ThreadData>();
					num = ((threadData != null) ? threadData.ThreadId : (-1));
				}
			}
			return num;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00015130 File Offset: 0x00013330
		public int GetParetoOptimalThreadId(bool maximizeValue1, bool maximizeValue2)
		{
			object lockObject = this._lockObject;
			int num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = -1;
				}
				else
				{
					List<Tracker4lat.ThreadData> list = this._dataList.ToList<Tracker4lat.ThreadData>();
					List<Tracker4lat.ThreadData> paretoFront = this.GetParetoFront(list, maximizeValue1, maximizeValue2);
					if (paretoFront.Count == 0)
					{
						num = -1;
					}
					else if (paretoFront.Count == 1)
					{
						num = paretoFront[0].ThreadId;
					}
					else
					{
						num = this.SelectBestFromParetoFront(paretoFront, maximizeValue1, maximizeValue2, 0.5, 0.5);
					}
				}
			}
			return num;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x000151D8 File Offset: 0x000133D8
		private List<Tracker4lat.ThreadData> GetParetoFront(List<Tracker4lat.ThreadData> dataList, bool maximizeValue1, bool maximizeValue2)
		{
			List<Tracker4lat.ThreadData> list = new List<Tracker4lat.ThreadData>();
			foreach (Tracker4lat.ThreadData threadData in dataList)
			{
				bool flag = false;
				foreach (Tracker4lat.ThreadData threadData2 in dataList)
				{
					if (this.Dominates(threadData2, threadData, maximizeValue1, maximizeValue2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(threadData);
				}
			}
			return list;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0001527C File Offset: 0x0001347C
		private bool Dominates(Tracker4lat.ThreadData a, Tracker4lat.ThreadData b, bool max1, bool max2)
		{
			bool flag = (max1 ? (a.Value1 >= b.Value1) : (a.Value1 <= b.Value1));
			bool flag2 = (max2 ? (a.Value2 >= b.Value2) : (a.Value2 <= b.Value2));
			bool flag3 = (max1 ? (a.Value1 > b.Value1) : (a.Value1 < b.Value1));
			bool flag4 = (max2 ? (a.Value2 > b.Value2) : (a.Value2 < b.Value2));
			return flag && flag2 && (flag3 || flag4);
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00015328 File Offset: 0x00013528
		private int SelectBestFromParetoFront(List<Tracker4lat.ThreadData> paretoFront, bool max1, bool max2, double weight1, double weight2)
		{
			double num = paretoFront.Min((Tracker4lat.ThreadData d) => d.Value1);
			double num2 = paretoFront.Max((Tracker4lat.ThreadData d) => d.Value1) - num;
			double num3 = paretoFront.Min((Tracker4lat.ThreadData d) => d.Value2);
			double num4 = paretoFront.Max((Tracker4lat.ThreadData d) => d.Value2) - num3;
			Tracker4lat.ThreadData threadData = null;
			double num5 = double.MinValue;
			foreach (Tracker4lat.ThreadData threadData2 in paretoFront)
			{
				double num6 = ((num2 > 0.0) ? ((threadData2.Value1 - num) / num2) : 0.5);
				double num7 = ((num4 > 0.0) ? ((threadData2.Value2 - num3) / num4) : 0.5);
				double num8 = (max1 ? num6 : (1.0 - num6));
				double num9 = (max2 ? num7 : (1.0 - num7));
				double num10 = weight1 * num8 + weight2 * num9;
				if (num10 > num5)
				{
					num5 = num10;
					threadData = threadData2;
				}
			}
			if (threadData == null)
			{
				return -1;
			}
			return threadData.ThreadId;
		}

		// Token: 0x0600017D RID: 381 RVA: 0x000154BC File Offset: 0x000136BC
		public double GetMaxValue2()
		{
			object lockObject = this._lockObject;
			double num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = double.MinValue;
				}
				else
				{
					num = this._dataList.Max((Tracker4lat.ThreadData d) => d.Value2);
				}
			}
			return num;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0001553C File Offset: 0x0001373C
		public double GetMinValue2()
		{
			object lockObject = this._lockObject;
			double num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = double.MaxValue;
				}
				else
				{
					num = this._dataList.Min((Tracker4lat.ThreadData d) => d.Value2);
				}
			}
			return num;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x000155BC File Offset: 0x000137BC
		public string GetStatistics()
		{
			object lockObject = this._lockObject;
			string text;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					text = "没有数据";
				}
				else
				{
					List<string> list = new List<string>
					{
						string.Format("数据总数: {0}", this._dataList.Count),
						string.Format("窗口大小: {0}", this._windowSize),
						string.Format("最早时间戳: {0}", this._dataList.First.Value.Timestamp),
						string.Format("最晚时间戳: {0}", this._dataList.Last.Value.Timestamp),
						string.Format("第一个数值最大值: {0}", this.GetMaxValue1()),
						string.Format("第一个数值最大值线程ID: {0}", this.GetMaxValue1ThreadId())
					};
					text = string.Join(Environment.NewLine, list);
				}
			}
			return text;
		}

		// Token: 0x04000429 RID: 1065
		private readonly object _lockObject = new object();

		// Token: 0x0400042A RID: 1066
		private readonly LinkedList<Tracker4lat.ThreadData> _dataList = new LinkedList<Tracker4lat.ThreadData>();

		// Token: 0x0400042B RID: 1067
		private readonly int _windowSize;

		// Token: 0x0400042C RID: 1068
		private Tracker4lat.ThreadData _currentMaxValue1;

		// Token: 0x0400042D RID: 1069
		private bool _maxValue1NeedsRecalculation;

		// Token: 0x0200009B RID: 155
		private class ThreadData
		{
			// Token: 0x1700023C RID: 572
			// (get) Token: 0x06000812 RID: 2066 RVA: 0x0002AE98 File Offset: 0x00029098
			// (set) Token: 0x06000813 RID: 2067 RVA: 0x0002AEA0 File Offset: 0x000290A0
			public int ThreadId { get; set; }

			// Token: 0x1700023D RID: 573
			// (get) Token: 0x06000814 RID: 2068 RVA: 0x0002AEA9 File Offset: 0x000290A9
			// (set) Token: 0x06000815 RID: 2069 RVA: 0x0002AEB1 File Offset: 0x000290B1
			public double Value1 { get; set; }

			// Token: 0x1700023E RID: 574
			// (get) Token: 0x06000816 RID: 2070 RVA: 0x0002AEBA File Offset: 0x000290BA
			// (set) Token: 0x06000817 RID: 2071 RVA: 0x0002AEC2 File Offset: 0x000290C2
			public double Value2 { get; set; }

			// Token: 0x1700023F RID: 575
			// (get) Token: 0x06000818 RID: 2072 RVA: 0x0002AECB File Offset: 0x000290CB
			// (set) Token: 0x06000819 RID: 2073 RVA: 0x0002AED3 File Offset: 0x000290D3
			public long Timestamp { get; set; }

			// Token: 0x0600081A RID: 2074 RVA: 0x0002AEDC File Offset: 0x000290DC
			public ThreadData(int threadId, double value1, double value2)
			{
				this.ThreadId = threadId;
				this.Value1 = value1;
				this.Value2 = value2;
				this.Timestamp = DateTime.Now.Ticks;
			}
		}
	}
}
