using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000014 RID: 20
	public class Tracker4lat
	{
		// Token: 0x0600015E RID: 350 RVA: 0x00012925 File Offset: 0x00010B25
		public Tracker4lat(int windowSize = 1000)
		{
			if (windowSize <= 0)
			{
				throw new ArgumentException("窗口大小必须大于0", "windowSize");
			}
			this._windowSize = windowSize;
			this._maxValue1NeedsRecalculation = false;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00012968 File Offset: 0x00010B68
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

		// Token: 0x06000160 RID: 352 RVA: 0x000129C0 File Offset: 0x00010BC0
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

		// Token: 0x06000161 RID: 353 RVA: 0x00012A50 File Offset: 0x00010C50
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

		// Token: 0x06000162 RID: 354 RVA: 0x00012AB0 File Offset: 0x00010CB0
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

		// Token: 0x06000163 RID: 355 RVA: 0x00012B04 File Offset: 0x00010D04
		private void RecalculateMaxValue()
		{
			this._currentMaxValue1 = this._dataList.OrderByDescending((Tracker4lat.ThreadData d) => d.Value1).FirstOrDefault<Tracker4lat.ThreadData>();
			this._maxValue1NeedsRecalculation = false;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00012B44 File Offset: 0x00010D44
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

		// Token: 0x06000165 RID: 357 RVA: 0x00012BB4 File Offset: 0x00010DB4
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
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00012C34 File Offset: 0x00010E34
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

		// Token: 0x06000167 RID: 359 RVA: 0x00012C7C File Offset: 0x00010E7C
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
		// (get) Token: 0x06000168 RID: 360 RVA: 0x00012CD0 File Offset: 0x00010ED0
		public int WindowSize
		{
			get
			{
				return this._windowSize;
			}
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00012CD8 File Offset: 0x00010ED8
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

		// Token: 0x0600016A RID: 362 RVA: 0x00012DEC File Offset: 0x00010FEC
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

		// Token: 0x0600016B RID: 363 RVA: 0x00012E94 File Offset: 0x00011094
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

		// Token: 0x0600016C RID: 364 RVA: 0x00012F38 File Offset: 0x00011138
		private bool Dominates(Tracker4lat.ThreadData a, Tracker4lat.ThreadData b, bool max1, bool max2)
		{
			bool flag = (max1 ? (a.Value1 >= b.Value1) : (a.Value1 <= b.Value1));
			bool flag2 = (max2 ? (a.Value2 >= b.Value2) : (a.Value2 <= b.Value2));
			bool flag3 = (max1 ? (a.Value1 > b.Value1) : (a.Value1 < b.Value1));
			bool flag4 = (max2 ? (a.Value2 > b.Value2) : (a.Value2 < b.Value2));
			return flag && flag2 && (flag3 || flag4);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00012FE4 File Offset: 0x000111E4
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

		// Token: 0x0600016E RID: 366 RVA: 0x00013178 File Offset: 0x00011378
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

		// Token: 0x0600016F RID: 367 RVA: 0x000131F8 File Offset: 0x000113F8
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

		// Token: 0x06000170 RID: 368 RVA: 0x00013278 File Offset: 0x00011478
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

		// Token: 0x040003C8 RID: 968
		private readonly object _lockObject = new object();

		// Token: 0x040003C9 RID: 969
		private readonly LinkedList<Tracker4lat.ThreadData> _dataList = new LinkedList<Tracker4lat.ThreadData>();

		// Token: 0x040003CA RID: 970
		private readonly int _windowSize;

		// Token: 0x040003CB RID: 971
		private Tracker4lat.ThreadData _currentMaxValue1;

		// Token: 0x040003CC RID: 972
		private bool _maxValue1NeedsRecalculation;

		// Token: 0x02000096 RID: 150
		private class ThreadData
		{
			// Token: 0x1700021D RID: 541
			// (get) Token: 0x060007AC RID: 1964 RVA: 0x00026D08 File Offset: 0x00024F08
			// (set) Token: 0x060007AD RID: 1965 RVA: 0x00026D10 File Offset: 0x00024F10
			public int ThreadId { get; set; }

			// Token: 0x1700021E RID: 542
			// (get) Token: 0x060007AE RID: 1966 RVA: 0x00026D19 File Offset: 0x00024F19
			// (set) Token: 0x060007AF RID: 1967 RVA: 0x00026D21 File Offset: 0x00024F21
			public double Value1 { get; set; }

			// Token: 0x1700021F RID: 543
			// (get) Token: 0x060007B0 RID: 1968 RVA: 0x00026D2A File Offset: 0x00024F2A
			// (set) Token: 0x060007B1 RID: 1969 RVA: 0x00026D32 File Offset: 0x00024F32
			public double Value2 { get; set; }

			// Token: 0x17000220 RID: 544
			// (get) Token: 0x060007B2 RID: 1970 RVA: 0x00026D3B File Offset: 0x00024F3B
			// (set) Token: 0x060007B3 RID: 1971 RVA: 0x00026D43 File Offset: 0x00024F43
			public long Timestamp { get; set; }

			// Token: 0x060007B4 RID: 1972 RVA: 0x00026D4C File Offset: 0x00024F4C
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
