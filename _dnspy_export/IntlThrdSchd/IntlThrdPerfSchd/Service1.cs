using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Win32;
using OpenLibSys;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000011 RID: 17
	public class Service1 : ServiceBase
	{
		// Token: 0x06000101 RID: 257 RVA: 0x0000BF44 File Offset: 0x0000A144
		public uint GetLevel(int type, int currentlevel)
		{
			List<uint> list = ((type == 1) ? this.level_nodes_p : this.level_nodes_l);
			for (int i = 0; i < list.Count; i++)
			{
				if ((long)currentlevel < (long)((ulong)list[i]))
				{
					return Math.Max(0U, list[i] - 1U);
				}
			}
			return Math.Max(0U, list[list.Count - 1] - 1U);
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000BFA8 File Offset: 0x0000A1A8
		private Service1.GroupInfo[] CreateGroup(List<int> indices, int groupCount)
		{
			if (groupCount <= 0 || indices.Count == 0)
			{
				return new Service1.GroupInfo[0];
			}
			Service1.GroupInfo[] array = new Service1.GroupInfo[groupCount];
			for (int i = 0; i < groupCount; i++)
			{
				uint num = 0U;
				for (int j = i; j < indices.Count; j += groupCount)
				{
					num |= 1U << indices[j];
				}
				array[i] = new Service1.GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, (long)((ulong)num), DateTime.Now.Ticks, 0L);
			}
			return array;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000C02C File Offset: 0x0000A22C
		public int ScheduleCore(Service1.ThreadMetrics metrics)
		{
			float factor = this.GetFactor((long)metrics.SmallCoreIPC);
			if ((float)metrics.InstructionsPerCycle > 300000f * factor)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000C05A File Offset: 0x0000A25A
		public Service1.ThreadInfoSimp test1112(int node_cap, Service1.ThreadInfoSimp head, Service1.ThreadInfoSimp threadInfo)
		{
			return threadInfo;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000C060 File Offset: 0x0000A260
		public uint GetAffinity(long missrate)
		{
			uint num = 0U;
			for (int i = 0; i < this.little_num; i++)
			{
				num |= this.coreinfo[this.index2procnum[i]].Affinity;
			}
			for (int j = 0; j < this.big_num; j++)
			{
				num |= this.coreinfo[this.index2procnum4big_p[j]].Affinity;
			}
			for (int k = 0; k < this.big_num; k++)
			{
				num |= this.coreinfo[this.index2procnum4big_s[k]].Affinity;
			}
			return num;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000C0F4 File Offset: 0x0000A2F4
		public uint GetAffinity4BP(long missrate)
		{
			uint num = 0U;
			for (int i = 0; i < this.big_num; i++)
			{
				num |= this.coreinfo[this.index2procnum4big_p[i]].Affinity;
			}
			return num;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000C130 File Offset: 0x0000A330
		public uint GetAffinity4BS(long missrate)
		{
			uint num = 0U;
			for (int i = 0; i < this.big_num; i++)
			{
				num |= this.coreinfo[this.index2procnum4big_s[i]].Affinity;
			}
			return num;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000C16C File Offset: 0x0000A36C
		public int TestAffinity(uint tempaff)
		{
			for (int i = 0; i < 4; i++)
			{
				if ((this.Lgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000C19C File Offset: 0x0000A39C
		public int TestAffinity4BP(uint tempaff)
		{
			for (int i = 0; i < this.big_num; i++)
			{
				if ((this.BPgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000C1D4 File Offset: 0x0000A3D4
		public int TestAffinity4BS(uint tempaff)
		{
			for (int i = 0; i < this.big_num; i++)
			{
				if ((this.BSgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000C20C File Offset: 0x0000A40C
		public int TestAffinity4perf(uint tempaff)
		{
			for (int i = 0; i < 2 * this.big_num + 4; i++)
			{
				if ((this.Perfgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					return i;
				}
			}
			return 2 * this.big_num + 3;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000C250 File Offset: 0x0000A450
		public int TestAffinity4eff(uint tempaff)
		{
			for (int i = 0; i < 2 * this.big_num + 4; i++)
			{
				if ((this.Effgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					return i;
				}
			}
			return 2 * this.big_num + 3;
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000C294 File Offset: 0x0000A494
		public int TestAffinity4smt(uint tempaff)
		{
			for (int i = 0; i < 2 * this.big_num + 4; i++)
			{
				if ((this.Smtgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					return i;
				}
			}
			return 2 * this.big_num + 3;
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0000C2D8 File Offset: 0x0000A4D8
		[return: TupleElementNames(new string[] { "perfResult", "effResult" })]
		public ValueTuple<int, int> TestAffinity4all(uint tempaff, uint tempaff1)
		{
			int num = this.perfstatenum - 1;
			int num2 = this.perfstatenum - 1;
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < this.perfstatenum; i++)
			{
				if (!flag && (this.Perfgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					num = i;
					flag = true;
				}
				if (!flag2 && (this.Effgroup[i].G_affinity & (long)((ulong)tempaff1)) > 0L)
				{
					num2 = i;
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
			return new ValueTuple<int, int>(num, num2);
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000C358 File Offset: 0x0000A558
		[return: TupleElementNames(new string[] { "perfResult", "effResult" })]
		public ValueTuple<int, int> TestAffinity4allnosmt(uint tempaff)
		{
			int num = this.big_num + 3;
			int num2 = this.big_num + 3;
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < this.big_num + 4; i++)
			{
				if (!flag && (this.Perfgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					num = i;
					flag = true;
				}
				if (!flag2 && (this.Effgroup[i].G_affinity & (long)((ulong)tempaff)) > 0L)
				{
					num2 = i;
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
			return new ValueTuple<int, int>(num, num2);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000C3D8 File Offset: 0x0000A5D8
		public Service1.ThreadInfoSimp UpdateThreadInfoSimp_ascend(int node_cap, ref Service1.ThreadInfoSimp head, Service1.ThreadInfoSimp threadInfo)
		{
			Service1.ThreadInfoSimp threadInfoSimp = null;
			for (Service1.ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				Service1.ThreadInfoSimp threadInfoSimp2 = head;
				while (threadInfoSimp2 != null)
				{
					if (threadInfo.Ipc <= threadInfoSimp2.Ipc)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
							break;
						}
						threadInfo.Next = threadInfoSimp2;
						head = threadInfo;
						break;
					}
					else
					{
						threadInfoSimp = threadInfoSimp2;
						threadInfoSimp2 = threadInfoSimp2.Next;
					}
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000C484 File Offset: 0x0000A684
		public Service1.GroupInfo UpdateGroupInfo(int node_cap, ref Service1.GroupInfo head, Service1.GroupInfo groupInfo)
		{
			Service1.GroupInfo groupInfo2 = null;
			for (Service1.GroupInfo groupInfo3 = head; groupInfo3 != null; groupInfo3 = groupInfo3.Next)
			{
				if (groupInfo.Gid == groupInfo3.Gid)
				{
					if (groupInfo2 != null)
					{
						groupInfo2.Next = groupInfo3.Next;
					}
					else
					{
						head = groupInfo3.Next;
					}
					groupInfo3.Next = null;
					break;
				}
				groupInfo2 = groupInfo3;
			}
			if (head == null)
			{
				head = groupInfo;
			}
			else
			{
				groupInfo2 = null;
				Service1.GroupInfo groupInfo3 = head;
				while (groupInfo3 != null)
				{
					if (groupInfo.Datetime <= groupInfo3.Datetime)
					{
						if (groupInfo2 != null)
						{
							groupInfo2.Next = groupInfo;
							groupInfo.Next = groupInfo3;
							break;
						}
						groupInfo.Next = groupInfo3;
						head = groupInfo;
						break;
					}
					else
					{
						groupInfo2 = groupInfo3;
						groupInfo3 = groupInfo3.Next;
					}
				}
				if (groupInfo3 == null)
				{
					groupInfo2.Next = groupInfo;
					groupInfo.Next = null;
				}
			}
			return groupInfo;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000C530 File Offset: 0x0000A730
		public Service1.ThreadInfoSimp UpdateThreadInfoSimp(int node_cap, ref Service1.ThreadInfoSimp head, Service1.ThreadInfoSimp threadInfo)
		{
			Service1.ThreadInfoSimp threadInfoSimp = null;
			for (Service1.ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				Service1.ThreadInfoSimp threadInfoSimp2 = head;
				while (threadInfoSimp2 != null)
				{
					if (threadInfo.Ins_per_count >= threadInfoSimp2.Ins_per_count)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
							break;
						}
						threadInfo.Next = threadInfoSimp2;
						head = threadInfo;
						break;
					}
					else
					{
						threadInfoSimp = threadInfoSimp2;
						threadInfoSimp2 = threadInfoSimp2.Next;
					}
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000C5DC File Offset: 0x0000A7DC
		public Service1.ThreadInfoSimp UpdateThreadInfoSimp1(int node_cap, ref Service1.ThreadInfoSimp head, Service1.ThreadInfoSimp threadInfo)
		{
			Service1.ThreadInfoSimp threadInfoSimp = null;
			for (Service1.ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				Service1.ThreadInfoSimp threadInfoSimp2 = head;
				while (threadInfoSimp2 != null)
				{
					if (threadInfo.InsPressure >= threadInfoSimp2.InsPressure)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
							break;
						}
						threadInfo.Next = threadInfoSimp2;
						head = threadInfo;
						break;
					}
					else
					{
						threadInfoSimp = threadInfoSimp2;
						threadInfoSimp2 = threadInfoSimp2.Next;
					}
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000C688 File Offset: 0x0000A888
		public Service1.ThreadInfoSimp UpdateThreadInfoSimp2(int node_cap, ref Service1.ThreadInfoSimp head, Service1.ThreadInfoSimp threadInfo)
		{
			Service1.ThreadInfoSimp threadInfoSimp = null;
			for (Service1.ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				Service1.ThreadInfoSimp threadInfoSimp2 = head;
				while (threadInfoSimp2 != null)
				{
					if (threadInfo.InsPressure1 >= threadInfoSimp2.InsPressure1)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
							break;
						}
						threadInfo.Next = threadInfoSimp2;
						head = threadInfo;
						break;
					}
					else
					{
						threadInfoSimp = threadInfoSimp2;
						threadInfoSimp2 = threadInfoSimp2.Next;
					}
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000C734 File Offset: 0x0000A934
		public Service1.ThreadInfoSimp UpdateThreadInfoSimp3(int node_cap, ref Service1.ThreadInfoSimp head, Service1.ThreadInfoSimp threadInfo)
		{
			Service1.ThreadInfoSimp threadInfoSimp = null;
			for (Service1.ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				Service1.ThreadInfoSimp threadInfoSimp2 = head;
				while (threadInfoSimp2 != null)
				{
					if (threadInfo.InsPressure2 >= threadInfoSimp2.InsPressure2)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
							break;
						}
						threadInfo.Next = threadInfoSimp2;
						head = threadInfo;
						break;
					}
					else
					{
						threadInfoSimp = threadInfoSimp2;
						threadInfoSimp2 = threadInfoSimp2.Next;
					}
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000C7E0 File Offset: 0x0000A9E0
		public Service1.ThreadInfo UpdateThreadInfo(int node_cap, ref Service1.ThreadInfo head, Service1.ThreadInfo threadInfo)
		{
			int num = 0;
			Service1.ThreadInfo threadInfo2 = head;
			Service1.ThreadInfo threadInfo3 = null;
			int num2 = 0;
			while (num2 < 500 && threadInfo2 != null)
			{
				if (threadInfo2.Tid == threadInfo.Tid)
				{
					if (threadInfo3 != null)
					{
						threadInfo3.NextThread = threadInfo2.NextThread;
						threadInfo.NextThread = head;
						head = threadInfo;
						threadInfo2.NextThread = null;
						return threadInfo;
					}
					threadInfo.NextThread = threadInfo2.NextThread;
					head = threadInfo;
					threadInfo2.NextThread = null;
					return threadInfo;
				}
				else
				{
					threadInfo3 = threadInfo2;
					threadInfo2 = threadInfo2.NextThread;
					num++;
					num2++;
				}
			}
			threadInfo2 = head;
			if (head == null)
			{
				head = threadInfo;
			}
			else if (head.Tid > 0)
			{
				threadInfo.NextThread = head;
				head = threadInfo;
			}
			else
			{
				threadInfo.NextThread = head.NextThread;
				head = threadInfo;
				threadInfo2.NextThread = null;
			}
			return threadInfo;
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000C898 File Offset: 0x0000AA98
		public Service1.ProcessInfo UpdateProcessInfo(int node_cap, ref Service1.ProcessInfo head, Service1.ProcessInfo processInfo)
		{
			int num = 0;
			Service1.ProcessInfo processInfo2 = head;
			Service1.ProcessInfo processInfo3 = null;
			int num2 = 0;
			while (num2 < 500 && processInfo2 != null)
			{
				if (processInfo2.Pid == processInfo.Pid)
				{
					if (processInfo3 != null)
					{
						processInfo3.NextProcess = processInfo2.NextProcess;
						processInfo.NextProcess = head;
						head = processInfo;
						processInfo2.NextProcess = null;
						return processInfo;
					}
					processInfo.NextProcess = processInfo2.NextProcess;
					head = processInfo;
					processInfo2.NextProcess = null;
					return processInfo;
				}
				else
				{
					processInfo3 = processInfo2;
					processInfo2 = processInfo2.NextProcess;
					num++;
					num2++;
				}
			}
			processInfo2 = head;
			if (head == null)
			{
				head = processInfo;
			}
			else if (head.Pid > 0)
			{
				processInfo.NextProcess = head;
				head = processInfo;
			}
			else
			{
				processInfo.NextProcess = head.NextProcess;
				head = processInfo;
				processInfo2.NextProcess = null;
			}
			return processInfo;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000C950 File Offset: 0x0000AB50
		public int UpdateNodeP(int node_cap, ref Service1.NodeP node, int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1, Service1.Node2 compare, Service1.Node2 compare_final)
		{
			int num = 0;
			Service1.NodeP nodeP = node;
			Service1.NodeP nodeP2 = null;
			while (nodeP != null)
			{
				if (nodeP.PId == pid)
				{
					nodeP.Ins_total = ins_total;
					nodeP.Store_total = store_total;
					nodeP.Count_total = count_total;
					nodeP.Intval = intval;
					nodeP.Nonstore_store_ratio = nonstore_store_ratio;
					nodeP.Usr_sum = usr_sum;
					nodeP.Usr_count = usr_count;
					nodeP.Usr_ratio = usr_ratio;
					nodeP.Residence = residence;
					nodeP.Residence1 = residence1;
					nodeP.Compare = compare;
					nodeP.Compare_final = compare_final;
					if (nodeP2 != null)
					{
						nodeP2.Next = nodeP.Next;
						nodeP.Next = node;
						node = nodeP;
						return 1;
					}
					return 1;
				}
				else
				{
					Service1.NodeP next = nodeP.Next;
					nodeP2 = nodeP;
					nodeP = nodeP.Next;
					num++;
				}
			}
			node = new Service1.NodeP(pid, ins_total, store_total, count_total, intval, nonstore_store_ratio, usr_sum, usr_count, usr_ratio, residence, residence1, compare, compare_final)
			{
				Next = node
			};
			num++;
			return 0;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0000CA40 File Offset: 0x0000AC40
		public int FindNodeValueP(ref Service1.NodeP node, int pid, ref long ins_total, ref long store_total, ref long count_total, ref long intval, ref long nonstore_store_ratio, ref long usr_sum, ref long usr_count, ref long usr_ratio, ref long residence, ref long residence1, ref Service1.Node2 compare, ref Service1.Node2 compare_final)
		{
			for (Service1.NodeP nodeP = node; nodeP != null; nodeP = nodeP.Next)
			{
				if (nodeP.PId == pid)
				{
					ins_total = nodeP.Ins_total;
					store_total = nodeP.Store_total;
					count_total = nodeP.Count_total;
					intval = nodeP.Intval;
					nonstore_store_ratio = nodeP.Nonstore_store_ratio;
					usr_sum = nodeP.Usr_sum;
					usr_count = nodeP.Usr_count;
					usr_ratio = nodeP.Usr_ratio;
					residence = nodeP.Residence;
					residence1 = nodeP.Residence1;
					compare = nodeP.Compare;
					compare_final = nodeP.Compare_final;
					return 1;
				}
			}
			return 0;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000CAD4 File Offset: 0x0000ACD4
		public void RemoveThread(int tid, int pid)
		{
			int num = Math.Abs(tid % 10000);
			if (this.threadinfo[num] != null && this.threadinfo[num].Tid == tid)
			{
				this.threadinfo[num] = null;
			}
			int num2 = Math.Abs(pid % 10000);
			Service1.ProcessInfo processInfo = this.processinfo[num2];
			if (processInfo != null)
			{
				processInfo.ThreadSet = this.DeleteThreadSimpFromList(processInfo.ThreadSet, tid);
			}
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000CB40 File Offset: 0x0000AD40
		private Service1.ThreadInfoSimp DeleteThreadSimpFromList(Service1.ThreadInfoSimp head, int tid)
		{
			Service1.ThreadInfoSimp threadInfoSimp = null;
			Service1.ThreadInfoSimp threadInfoSimp2 = head;
			while (threadInfoSimp2 != null)
			{
				if (threadInfoSimp2.Tid == tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
						break;
					}
					head = threadInfoSimp2.Next;
					break;
				}
				else
				{
					threadInfoSimp = threadInfoSimp2;
					threadInfoSimp2 = threadInfoSimp2.Next;
				}
			}
			return head;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x0000CB84 File Offset: 0x0000AD84
		public void RemoveProcessAndThreads(int pid)
		{
			int num = Math.Abs(pid % 10000);
			Service1.ProcessInfo processInfo = this.processinfo[num];
			if (processInfo == null || processInfo.Pid != pid)
			{
				return;
			}
			for (Service1.ThreadInfoSimp threadInfoSimp = processInfo.ThreadSet; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				Service1.ThreadInfo belong2thread = threadInfoSimp.Belong2thread;
				if (belong2thread != null)
				{
					int num2 = Math.Abs(belong2thread.Tid % 10000);
					if (this.threadinfo[num2] != null && this.threadinfo[num2].Tid == belong2thread.Tid)
					{
						this.threadinfo[num2] = null;
					}
				}
			}
			processInfo.ThreadSet = null;
			this.processinfo[num] = null;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000CC20 File Offset: 0x0000AE20
		public int UpdateNode(int node_cap, ref Service1.Node node, int id, int value)
		{
			int num = 0;
			Service1.Node node2 = node;
			Service1.Node node3 = null;
			while (node2 != null)
			{
				if (node2.Id == id)
				{
					node2.Value = value;
					if (node3 != null)
					{
						node3.Next = node2.Next;
						node2.Next = node;
						node = node2;
						return 1;
					}
					return 1;
				}
				else
				{
					Service1.Node next = node2.Next;
					node3 = node2;
					node2 = node2.Next;
					num++;
				}
			}
			node = new Service1.Node(id, value)
			{
				Next = node
			};
			num++;
			return 0;
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000CC9C File Offset: 0x0000AE9C
		public int UpdateNode2(ref Service1.Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Service1.Node2 node2 = node;
			Service1.Node2 node3 = null;
			Service1.Node2 node4 = new Service1.Node2(id, value1, 0);
			node2 = node;
			int i = 0;
			while (i < 500)
			{
				if (value1 <= node2.Value1)
				{
					if (node3 != null)
					{
						node4.Next = node2;
						node3.Next = node4;
						reset_count = 1L;
						break;
					}
					if (node2.Id == 0)
					{
						node = node4;
						reset_count = 1L;
						break;
					}
					node4.Next = node2;
					node = node4;
					reset_count = 1L;
					break;
				}
				else
				{
					if (node2.Id == 0)
					{
						node = node4;
						reset_count = 1L;
						return 2;
					}
					if (node2.Next == null)
					{
						node2.Next = node4;
						node4.Next = null;
						reset_count = 1L;
						break;
					}
					node3 = node2;
					node2 = node2.Next;
					i++;
				}
			}
			return 0;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x0000CD50 File Offset: 0x0000AF50
		public int UpdateNode2_little(ref Service1.Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Service1.Node2 node2 = node;
			Service1.Node2 node3 = null;
			Service1.Node2 node4 = new Service1.Node2(id, value1, 0);
			node2 = node;
			int i = 0;
			while (i < 500)
			{
				if (node2.Id == -1)
				{
					node = node4;
					node.Next = null;
					reset_count = 1L;
					return 1;
				}
				if (value1 >= node2.Value1)
				{
					if (node3 == null)
					{
						node4.Next = node2;
						node = node4;
						reset_count = 1L;
						return 1;
					}
					node4.Next = node2;
					node3.Next = node4;
					reset_count = 1L;
					return 1;
				}
				else
				{
					if (node2.Next == null)
					{
						node2.Next = node4;
						node4.Next = null;
						reset_count = 1L;
						return 1;
					}
					node3 = node2;
					node2 = node2.Next;
					i++;
				}
			}
			return 0;
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000CDF8 File Offset: 0x0000AFF8
		public int UpdateNodeP(ref Service1.Node2 node, int id, long value1, int value2)
		{
			Service1.Node2 node2 = node;
			Service1.Node2 node3 = null;
			Service1.Node2 node4 = new Service1.Node2(id, value1, value2);
			node2 = node;
			int i = 0;
			while (i < 500)
			{
				if (node2.Id == -1)
				{
					node = node4;
					node.Next = null;
					return 1;
				}
				if (value1 >= node2.Value1)
				{
					if (node3 == null)
					{
						node4.Next = node2;
						node = node4;
						return 1;
					}
					node4.Next = node2;
					node3.Next = node4;
					return 1;
				}
				else
				{
					if (node2.Next == null)
					{
						node2.Next = node4;
						node4.Next = null;
						return 1;
					}
					node3 = node2;
					node2 = node2.Next;
					i++;
				}
			}
			return 0;
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000CE8C File Offset: 0x0000B08C
		public int ProcessSysinfo(Service1.GroupInfo groupInfo, ref Service1.SysInfo sysInfo)
		{
			Service1.GroupInfo groupInfo2 = groupInfo;
			long num = 0L;
			if (groupInfo2 == null)
			{
				return -1;
			}
			while (groupInfo2 != null)
			{
				num += 1L;
				if (num == 1L)
				{
					sysInfo.Max_gid = groupInfo2.Gid;
				}
				if (groupInfo2.Next == null)
				{
					sysInfo.Min_gid = groupInfo2.Gid;
				}
				groupInfo2 = groupInfo2.Next;
			}
			if (this.groupinfo[sysInfo.Max_gid].L_affinity % 2L == 0L)
			{
				if (this.groupinfo[sysInfo.Max_gid].ThreadSet2 == null)
				{
					return -1;
				}
				this.groupinfo[sysInfo.Max_gid].ThreadSet2.Belong2thread.Affinity = (uint)this.groupinfo[sysInfo.Min_gid].Gid;
			}
			else
			{
				if (this.groupinfo[sysInfo.Max_gid].ThreadSet1 == null)
				{
					return -1;
				}
				this.groupinfo[sysInfo.Max_gid].ThreadSet1.Belong2thread.Affinity = (uint)this.groupinfo[sysInfo.Min_gid].Gid;
			}
			return 0;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000CF84 File Offset: 0x0000B184
		public int ProcessCompare(Service1.ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			for (Service1.ThreadInfoSimp threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
					if (num == 1L)
					{
						long ins_per_count = threadInfoSimp.Ins_per_count;
						threadInfoSimp.Belong2thread.Groupinfo = this.totalgroup;
						threadInfoSimp.Belong2thread.Sched = 1;
						threadInfoSimp.Belong2thread.Duration = 30000L;
						threadInfoSimp.Belong2thread.CoreType = 1;
					}
				}
				threadInfoSimp.Belong2thread.Lockdata = 0;
			}
			return (int)num;
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000D004 File Offset: 0x0000B204
		public int ProcessCompare1(Service1.ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			Service1.ThreadInfoSimp threadInfoSimp;
			for (threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
				}
				Service1.ThreadInfoSimp next = threadInfoSimp.Next;
			}
			threadInfoSimp = node;
			num = 0L;
			while (threadInfoSimp != null)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
					if (num == 1L)
					{
						threadInfoSimp.Belong2thread.Ipc_ratio = 100L;
					}
				}
				threadInfoSimp = threadInfoSimp.Next;
			}
			return (int)num;
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0000D07C File Offset: 0x0000B27C
		public int ProcessCompare2(Service1.ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			Service1.ThreadInfoSimp threadInfoSimp;
			for (threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
				}
				Service1.ThreadInfoSimp next = threadInfoSimp.Next;
			}
			threadInfoSimp = node;
			num = 0L;
			while (threadInfoSimp != null)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
					if (num == 1L)
					{
						threadInfoSimp.Belong2thread.Ipc_ratio1 = 100L;
					}
				}
				threadInfoSimp = threadInfoSimp.Next;
			}
			return (int)num;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0000D0F4 File Offset: 0x0000B2F4
		public int ProcessCompare3(Service1.ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			Service1.ThreadInfoSimp threadInfoSimp;
			for (threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
				}
				Service1.ThreadInfoSimp next = threadInfoSimp.Next;
			}
			threadInfoSimp = node;
			num = 0L;
			while (threadInfoSimp != null)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num += 1L;
					if (num == 1L)
					{
						threadInfoSimp.Belong2thread.Ipc_ratio2 = 100L;
					}
				}
				threadInfoSimp = threadInfoSimp.Next;
			}
			return (int)num;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x0000D16C File Offset: 0x0000B36C
		public int UpdateNode1(int node_cap, ref Service1.Node1 node, int id, long acc_instruction_b, long acc_aclk_b, long acc_load_b, long acc_store_b, long acc_load_miss_b, long acc_br_b, long acc_runtime_b, long cnt_b, long acc_instruction_l, long acc_aclk_l, long acc_load_l, long acc_load_l_perm, long last_duration, long now_duration, long acc_store_l, long acc_store_l_perm, long acc_load_miss_l, long acc_br_l, long acc_runtime_l, long cnt_l, long ipc_b, long max_ipc_b, long ipc_l, long ipc_l_perm, long max_ipc_l, long ipc_ratio, long br_ratio, long br_load_ratio, long load_miss_ratio_b, long min_load_miss_ratio_b, long load_miss_ratio_l, long avg_runtime_b, long avg_runtime_l, long avg_freq_b, long avg_freq_l, long max_ins, long lock_data, long tag, long duration, long reset_count, uint affinity, long residence)
		{
			int num = 0;
			Service1.Node1 node2 = node;
			Service1.Node1 node3 = null;
			while (node2 != null)
			{
				if (node2.Id == id)
				{
					node2.Acc_instruction_b = acc_instruction_b;
					node2.Acc_aclk_b = acc_aclk_b;
					node2.Acc_load_b = acc_load_b;
					node2.Acc_store_b = acc_store_b;
					node2.Acc_load_miss_b = acc_load_miss_b;
					node2.Acc_br_b = acc_br_b;
					node2.Acc_runtime_b = acc_runtime_b;
					node2.Cnt_b = cnt_b;
					node2.Acc_instruction_l = acc_instruction_l;
					node2.Acc_aclk_l = acc_aclk_l;
					node2.Acc_load_l = acc_load_l;
					node2.Acc_load_l_perm = acc_load_l_perm;
					node2.Last_duration = last_duration;
					node2.Now_duration = now_duration;
					node2.Acc_store_l = acc_store_l;
					node2.Acc_store_l_perm = acc_store_l_perm;
					node2.Acc_load_miss_l = acc_load_miss_l;
					node2.Acc_br_l = acc_br_l;
					node2.Acc_runtime_l = acc_runtime_l;
					node2.Cnt_l = cnt_l;
					node2.Ipc_b = ipc_b;
					node2.Max_ipc_b = max_ipc_b;
					node2.Ipc_l = ipc_l;
					node2.Ipc_l_perm = ipc_l_perm;
					node2.Max_ipc_l = max_ipc_l;
					node2.Ipc_ratio = ipc_ratio;
					node2.Br_ratio = br_ratio;
					node2.Br_load_ratio = br_load_ratio;
					node2.Load_miss_ratio_b = load_miss_ratio_b;
					node2.Min_load_miss_ratio_b = min_load_miss_ratio_b;
					node2.Load_miss_ratio_l = load_miss_ratio_l;
					node2.Avg_runtime_b = avg_runtime_b;
					node2.Avg_runtime_l = avg_runtime_l;
					node2.Avg_freq_b = avg_freq_b;
					node2.Avg_freq_l = avg_freq_l;
					node2.Max_ins = max_ins;
					node2.Lock_data = lock_data;
					node2.Tag = tag;
					node2.Duration = duration;
					node2.Reset_count = reset_count;
					node2.Affinity = affinity;
					node2.Residence = residence;
					if (node3 != null)
					{
						node3.Next = node2.Next;
						node2.Next = node;
						node = node2;
						return 1;
					}
					return 1;
				}
				else
				{
					Service1.Node1 next = node2.Next;
					node3 = node2;
					node2 = node2.Next;
					num++;
				}
			}
			node = new Service1.Node1(id, acc_instruction_b, acc_aclk_b, acc_load_b, acc_store_b, acc_load_miss_b, acc_br_b, acc_runtime_b, cnt_b, acc_instruction_l, acc_aclk_l, acc_load_l, acc_load_l_perm, last_duration, now_duration, acc_store_l, acc_store_l_perm, acc_load_miss_l, acc_br_l, acc_runtime_l, cnt_l, ipc_b, max_ipc_b, ipc_l, ipc_l_perm, max_ipc_l, ipc_ratio, br_ratio, br_load_ratio, load_miss_ratio_b, min_load_miss_ratio_b, load_miss_ratio_l, avg_runtime_b, avg_runtime_l, avg_freq_b, avg_freq_l, max_ins, lock_data, tag, duration, reset_count, affinity, residence)
			{
				Next = node
			};
			num++;
			return 0;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x0000D388 File Offset: 0x0000B588
		public int DeleteNode(ref Service1.Node2 node, int id)
		{
			Service1.Node2 node2 = node;
			Service1.Node2 node3 = null;
			while (node2 != null)
			{
				if (node2.Id == id)
				{
					if ((node3 == null) & (node2.Next == null))
					{
						node2.Id = -1;
						node2.Value1 = 0L;
						node2.Value2 = -1;
						node2.Next = null;
						return -1;
					}
					if ((node3 == null) & (node2.Next != null))
					{
						node = node2.Next;
						return 1;
					}
					if ((node3 != null) & (node2.Next == null))
					{
						node3.Next = null;
						return 1;
					}
					node3.Next = node2.Next;
					return 1;
				}
				else
				{
					node3 = node2;
					node2 = node2.Next;
				}
			}
			return 0;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000D428 File Offset: 0x0000B628
		public int GetNodeValue(ref Service1.Node2 node, ref long value)
		{
			Service1.Node2 node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2 == null)
				{
					return -1;
				}
				if (node2.Id != 0)
				{
					value = node2.Value1;
					return 1;
				}
				node2 = node2.Next;
			}
			return -1;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000D46C File Offset: 0x0000B66C
		public int FindNodeValue2(ref Service1.Node2 node, ref long value)
		{
			Service1.Node2 node2 = node;
			int i = 0;
			while (i < 500)
			{
				if (node2 != null)
				{
					if (node2.Id != 0)
					{
						try
						{
							IntPtr intPtr = Service1.OpenThread((Service1.ThreadAccess)96U, false, (uint)node2.Id);
							if (intPtr != IntPtr.Zero)
							{
								Service1.CloseHandle(intPtr);
								value = node2.Value1;
								return node2.Id;
							}
							this.DeleteNode(ref node, node2.Id);
							node2 = node2.Next;
							goto IL_0080;
						}
						catch
						{
							this.DeleteNode(ref node, node2.Id);
							node2 = node2.Next;
							goto IL_0080;
						}
						goto IL_0075;
					}
					goto IL_0075;
					IL_0080:
					i++;
					continue;
					IL_0075:
					node2 = node2.Next;
					goto IL_0080;
				}
				return -1;
			}
			return -1;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000D51C File Offset: 0x0000B71C
		public int FindCompareValue(ref Service1.Node2 node, int id)
		{
			for (Service1.Node2 node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					return node2.Value2;
				}
			}
			return -1;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000D54C File Offset: 0x0000B74C
		public int FindNodeValue(Service1.Node node, int id)
		{
			for (Service1.Node node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					return node2.Value;
				}
			}
			return -1;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000D578 File Offset: 0x0000B778
		public int FindMaxIpc(Service1.Node node, ref int max_ipc_thread, ref int max_ipc_little)
		{
			for (Service1.Node node2 = node; node2 != null; node2 = node2.Next)
			{
				max_ipc_thread = node2.Id;
				max_ipc_little = node2.Value;
			}
			return -1;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000D5A4 File Offset: 0x0000B7A4
		public Service1.ThreadInfo FindThread(ref Service1.ThreadInfo threadInfo, int tid)
		{
			for (Service1.ThreadInfo threadInfo2 = threadInfo; threadInfo2 != null; threadInfo2 = threadInfo2.NextThread)
			{
				if (threadInfo2.Tid == tid)
				{
					return threadInfo2;
				}
			}
			return null;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0000D5CC File Offset: 0x0000B7CC
		public Service1.ProcessInfo FindProcess(ref Service1.ProcessInfo processInfo, int pid)
		{
			for (Service1.ProcessInfo processInfo2 = processInfo; processInfo2 != null; processInfo2 = processInfo2.NextProcess)
			{
				if (processInfo2.Pid == pid)
				{
					return processInfo2;
				}
			}
			return null;
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000D5F4 File Offset: 0x0000B7F4
		public int FindNodeValue1(ref Service1.Node1 node, int id, ref long acc_instruction_b, ref long acc_aclk_b, ref long acc_load_b, ref long acc_store_b, ref long acc_load_miss_b, ref long acc_br_b, ref long acc_runtime_b, ref long cnt_b, ref long acc_instruction_l, ref long acc_aclk_l, ref long acc_load_l, ref long acc_load_l_perm, ref long last_duration, ref long now_duration, ref long acc_store_l, ref long acc_store_l_perm, ref long acc_load_miss_l, ref long acc_br_l, ref long acc_runtime_l, ref long cnt_l, ref long ipc_b, ref long max_ipc_b, ref long ipc_l, ref long ipc_l_perm, ref long max_ipc_l, ref long ipc_ratio, ref long br_ratio, ref long br_load_ratio, ref long load_miss_ratio_b, ref long min_load_miss_ratio_b, ref long load_miss_ratio_l, ref long avg_runtime_b, ref long avg_runtime_l, ref long avg_freq_b, ref long avg_freq_l, ref long max_ins, ref long lock_data, ref long tag, ref long duration, ref long reset_count, ref uint affinity)
		{
			for (Service1.Node1 node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					acc_instruction_b = node2.Acc_instruction_b;
					acc_aclk_b = node2.Acc_aclk_b;
					acc_load_b = node2.Acc_load_b;
					acc_store_b = node2.Acc_store_b;
					acc_load_miss_b = node2.Acc_load_miss_b;
					acc_br_b = node2.Acc_br_b;
					acc_runtime_b = node2.Acc_runtime_b;
					cnt_b = node2.Cnt_b;
					acc_instruction_l = node2.Acc_instruction_l;
					acc_aclk_l = node2.Acc_aclk_l;
					acc_load_l = node2.Acc_load_l;
					acc_load_l_perm = node2.Acc_load_l_perm;
					last_duration = node2.Last_duration;
					now_duration = node2.Now_duration;
					acc_store_l = node2.Acc_store_l;
					acc_store_l_perm = node2.Acc_store_l_perm;
					acc_load_miss_l = node2.Acc_load_miss_l;
					acc_br_l = node2.Acc_br_l;
					acc_runtime_l = node2.Acc_runtime_l;
					cnt_l = node2.Cnt_l;
					ipc_b = node2.Ipc_b;
					max_ipc_b = node2.Max_ipc_b;
					ipc_l = node2.Ipc_l;
					ipc_l_perm = node2.Ipc_l_perm;
					max_ipc_l = node2.Max_ipc_l;
					ipc_ratio = node2.Ipc_ratio;
					br_ratio = node2.Br_ratio;
					br_load_ratio = node2.Br_load_ratio;
					load_miss_ratio_b = node2.Load_miss_ratio_b;
					min_load_miss_ratio_b = node2.Min_load_miss_ratio_b;
					load_miss_ratio_l = node2.Load_miss_ratio_l;
					avg_runtime_b = node2.Avg_runtime_b;
					avg_runtime_l = node2.Avg_runtime_l;
					avg_freq_b = node2.Avg_freq_b;
					avg_freq_l = node2.Avg_freq_l;
					max_ins = node2.Max_ins;
					lock_data = node2.Lock_data;
					tag = node2.Tag;
					duration = node2.Duration;
					reset_count = node2.Reset_count;
					affinity = node2.Affinity;
					return 1;
				}
			}
			return 0;
		}

		// Token: 0x06000130 RID: 304 RVA: 0x0000D795 File Offset: 0x0000B995
		public float GetFactor(long missrate)
		{
			return (float)missrate / 100f;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000D7A0 File Offset: 0x0000B9A0
		public int Intval2Limit(int oldthread, long intval, long utility, long nonstore_store_ratio, ref long usr_ratio_avg, ref long ins_big, int currentprocessor, long usr_ratio, ref long max_ins, ref long usr_ratio1, long br_sys, ref long tag, uint affinity, ref long reset_count, ref long usr_ratio_little, ref long prod_cons_ratio, ref long switch1, ref long lock_data, ref long residence_p1, ref long residence_p)
		{
			int num;
			if (switch1 == 2L)
			{
				num = 2;
			}
			else
			{
				num = 1;
			}
			if (num == 1)
			{
				if (!((((1U << currentprocessor) & this.affinitymask_little) > 0U) & (tag == 2L)))
				{
					return 0;
				}
				if (reset_count == 0L)
				{
					this.UpdateNode2_little(ref this.schd_queue_l2b, oldthread, usr_ratio, 0, ref reset_count);
					return 1;
				}
			}
			else
			{
				if (num != 2)
				{
					return 0;
				}
				if (!((((1U << currentprocessor) & this.affinitymask_big) > 0U) & (tag == 6L)))
				{
					return 0;
				}
				if (reset_count == 0L)
				{
					this.UpdateNode2_little(ref this.schd_queue_b2l, oldthread, usr_ratio, 0, ref reset_count);
					return 2;
				}
			}
			return 0;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000D838 File Offset: 0x0000BA38
		public long GetLevel(int type, long active_threads_cnt, long current_level)
		{
			if (type == 3)
			{
				long num;
				if (active_threads_cnt <= (long)this.little_num)
				{
					num = Math.Max((long)Math.Ceiling((double)active_threads_cnt / (double)this.little_per_group_count) - 1L, 0L);
				}
				else
				{
					num = 3L + active_threads_cnt - (long)this.little_num;
				}
				long num2 = Math.Min(Math.Max(num, current_level), (long)(this.perfstatenum - 1));
				long num3 = 3L;
				long num4 = (long)(3 + this.big_num);
				if (num2 <= num3)
				{
					return num2;
				}
				if (num2 <= num4)
				{
					if (current_level > num3)
					{
						return num2;
					}
					return num3;
				}
				else
				{
					if (current_level <= num3)
					{
						return num3;
					}
					if (current_level <= num4)
					{
						return num4;
					}
					return num2;
				}
			}
			else
			{
				long num5;
				if (active_threads_cnt <= (long)this.big_num)
				{
					num5 = Math.Max(active_threads_cnt - 1L, 0L);
				}
				else
				{
					long num6 = Math.Max((long)Math.Ceiling((double)(active_threads_cnt - (long)this.big_num) / (double)this.little_per_group_count) - 1L, 0L);
					num5 = Math.Min((long)this.big_num + num6, (long)(this.perfstatenum - 1));
				}
				long num7 = Math.Max(num5, current_level);
				long num8 = (long)(this.big_num - 1);
				long num9 = (long)(3 + this.big_num);
				if (num7 <= num8)
				{
					return num7;
				}
				if (num7 <= num9)
				{
					if (current_level > num8)
					{
						return num7;
					}
					return num8;
				}
				else
				{
					if (current_level <= num8)
					{
						return num8;
					}
					if (current_level <= num9)
					{
						return num9;
					}
					return num7;
				}
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000133 RID: 307 RVA: 0x0000D963 File Offset: 0x0000BB63
		// (set) Token: 0x06000134 RID: 308 RVA: 0x0000D96B File Offset: 0x0000BB6B
		public OnlineLearningManager learner { get; private set; }

		// Token: 0x06000135 RID: 309
		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern bool PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid SchemeGuid);

		// Token: 0x06000136 RID: 310
		[DllImport("kernel32.dll")]
		public static extern bool GetSystemPowerStatus(out Service1.PowerStatus BatteryInfo);

		// Token: 0x06000137 RID: 311
		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadACValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		// Token: 0x06000138 RID: 312
		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadDCValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		// Token: 0x06000139 RID: 313
		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenThread(Service1.ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		// Token: 0x0600013A RID: 314
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(Service1.ProcessAccess dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		// Token: 0x0600013B RID: 315
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int GetThreadPriority(IntPtr hThread);

		// Token: 0x0600013C RID: 316
		[DllImport("kernel32.dll")]
		public static extern int GetPriorityClass(IntPtr hProcess);

		// Token: 0x0600013D RID: 317
		[DllImport("kernel32.dll")]
		private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		// Token: 0x0600013E RID: 318
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetThreadAffinityMask(IntPtr hThread, out uint mask);

		// Token: 0x0600013F RID: 319
		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr handle);

		// Token: 0x06000140 RID: 320
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentThreadId();

		// Token: 0x06000141 RID: 321
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentProcessId();

		// Token: 0x06000142 RID: 322
		[DllImport("kernel32.dll")]
		public static extern bool SetThreadIdealProcessor(IntPtr hThread, int dwIdealProcessor);

		// Token: 0x06000143 RID: 323
		[DllImport("kernel32.dll")]
		private static extern bool SetProcessInformation(IntPtr hProcess, int ProcessInformationClass, IntPtr pProcessInformation, uint dwSize);

		// Token: 0x06000144 RID: 324 RVA: 0x0000D974 File Offset: 0x0000BB74
		public Service1()
		{
			this.InitializeComponent();
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000E78C File Offset: 0x0000C98C
		protected override void OnStart(string[] args)
		{
			int[] array = new int[32];
			new int[32];
			int[] array2 = new int[32];
			long[] array3 = new long[32];
			new long[32];
			long[] array4 = new long[32];
			new int[32];
			new int[32];
			new int[32];
			int num = 0;
			while ((long)num < (long)((ulong)Convert.ToUInt32(this.NumberOfLogicalProcessors)))
			{
				array[num] = 0;
				array2[num] = 0;
				array3[num] = 0L;
				array4[num] = 0L;
				num++;
			}
			this.currentthread = (int)Service1.GetCurrentProcessId();
			try
			{
				foreach (ManagementBaseObject managementBaseObject in new ManagementObjectSearcher("select * from win32_processor").Get())
				{
					ManagementObject managementObject = (ManagementObject)managementBaseObject;
					this.number_of_cores = managementObject.GetPropertyValue("numberofcores").ToString().Trim();
					this.NumberOfLogicalProcessors = managementObject.GetPropertyValue("NumberOfLogicalProcessors").ToString().Trim();
				}
			}
			catch
			{
				return;
			}
			this.ratio1 = (uint)(Convert.ToUInt64(this.NumberOfLogicalProcessors) * 100UL / Convert.ToUInt64(this.NumberOfLogicalProcessors));
			this.ratio_string1 = this.ratio1.ToString();
			this.ratio = this.ratio1;
			this.ratio_string = this.ratio.ToString();
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("ProvAcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("ProvDcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("AcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("DcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("ProvAcSettingIndex", this.ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("ProvDcSettingIndex", this.ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("AcSettingIndex", this.ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("DcSettingIndex", this.ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("ProvAcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("ProvDcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("AcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("DcSettingIndex", this.ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("Attributes", 1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("FriendlyName", "@%SystemRoot%\\system32\\powrprof.dll,-767,Processor performance core parking min cores", RegistryValueKind.ExpandString);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueIncrement", 1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueMax", 100, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueMin", 0, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueUnits", "@%SystemRoot%\\system32\\powrprof.dll,-81,percent", RegistryValueKind.ExpandString);
			try
			{
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DefaultDynamicHeteroCpuPolicy", 2, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyImportant", 2, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyImportantShort", 2, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyImportantPriority", 8, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyMask", 2, RegistryValueKind.DWord);
			}
			catch
			{
			}
			for (int i = 0; i < Convert.ToInt32(this.NumberOfLogicalProcessors); i++)
			{
				this.sched_queue_l2b[i] = new Service1.Node2(0, 0L, 0);
				this.sched_queue_b2l[i] = new Service1.Node2(0, 0L, 0);
				this.compare[i] = new Service1.Node2(0, 0L, 0);
				this.compare_final[i] = new Service1.Node2(0, 0L, 0);
			}
			for (int j = 0; j < Convert.ToInt32(this.NumberOfLogicalProcessors); j++)
			{
				this.tag[j] = 0L;
				this.oldthread_waittime[j] = 0;
				this.core_availability_cnt[j] = 1L;
				this.affinitymask_big |= 1U << j;
				this.myOls.CpuidTx(26U, ref this.l_msr, ref this.eebx, ref this.eecx, ref this.eedx, (UIntPtr)((ulong)Math.Pow(2.0, (double)j)));
				if (this.l_msr == 0U)
				{
					string text = "IntlThrdSchedErrorInfo.txt";
					string text2 = "无法读取寄存器，受限的权限！";
					File.WriteAllText(text, text2);
					Environment.Exit(0);
				}
			}
			if (Convert.ToInt32(this.NumberOfLogicalProcessors) == Convert.ToInt32(this.number_of_cores))
			{
				this.Mode = 1;
			}
			else
			{
				this.Mode = 0;
			}
			this.maxLP = Convert.ToInt32(this.NumberOfLogicalProcessors);
			this.coreFeatures = new float[this.maxLP][];
			for (int k = 0; k < this.maxLP; k++)
			{
				this.coreFeatures[k] = new float[8];
			}
			for (int l = 0; l < this.maxLP; l++)
			{
				this.myOls.CpuidTx(26U, ref this.l_msr, ref this.eebx, ref this.eecx, ref this.eedx, (UIntPtr)(1U << l));
				if (((ulong)this.l_msr & 18446744073692774400UL) >> 24 != 64UL)
				{
					this.lgroupIndices4sl.Add((uint)l);
				}
			}
			for (int m = 0; m < this.lgroupIndices4sl.Count; m++)
			{
				this.myOls.RdmsrTx(1905U, ref this.eax, ref this.edx, (UIntPtr)(1U << (int)this.lgroupIndices4sl[m]));
				if (this.l_max_freq < (this.eax & 255U))
				{
					this.l_max_freq = this.eax & 255U;
				}
			}
			for (int n = 0; n < this.maxLP; n++)
			{
				this.myOls.CpuidTx(26U, ref this.l_msr, ref this.eebx, ref this.eecx, ref this.eedx, (UIntPtr)(1U << n));
				if (((ulong)this.l_msr & 18446744073692774400UL) >> 24 != 64UL)
				{
					this.myOls.RdmsrTx(1905U, ref this.eax, ref this.edx, (UIntPtr)(1U << n));
					if ((this.eax & 255U) < this.l_max_freq)
					{
						this.exlittleIndices.Add(1U << n);
					}
					else
					{
						this.littleIndices.Add(1U << n);
						this.affinitymask_little_p |= 1U << n;
					}
					this.affinitymask_little |= 1U << n;
					this.little_num++;
				}
				else if (this.Mode == 0)
				{
					if (this.bigPhysicalIndices.Count == this.bigSmtIndices.Count)
					{
						this.bigPhysicalIndices.Add(1U << n);
						this.affinitymask_big_phyx |= 1U << n;
					}
					else
					{
						this.bigSmtIndices.Add(1U << n);
						this.affinitymask_big_smt |= 1U << n;
					}
				}
				else
				{
					this.bigPhysicalIndices.Add(1U << n);
					this.affinitymask_big_phyx |= 1U << n;
				}
			}
			this.affinitymask_all = this.affinitymask_big_phyx | this.affinitymask_big | this.affinitymask_little;
			uint[] array5 = new uint[6];
			for (int num2 = 0; num2 < 4; num2++)
			{
				for (int num3 = 0; num3 < this.littleIndices.Count; num3++)
				{
					if (num3 % 4 == num2)
					{
						array5[num2] |= this.littleIndices[num3];
					}
				}
				this.lgroupIndices.Add(array5[num2]);
			}
			List<uint> list = new List<uint>();
			List<uint> list2 = new List<uint>();
			list.AddRange(this.bigPhysicalIndices);
			list.AddRange(this.littleIndices);
			list2.AddRange(this.littleIndices);
			list2.AddRange(this.bigPhysicalIndices);
			if (this.Mode == 0)
			{
				list.AddRange(this.bigSmtIndices);
				list2.AddRange(this.bigSmtIndices);
			}
			list.AddRange(this.exlittleIndices);
			list2.AddRange(this.exlittleIndices);
			this.level_nodes_p.Add((uint)this.bigPhysicalIndices.Count);
			this.level_nodes_p.Add((uint)(this.littleIndices.Count + this.bigPhysicalIndices.Count));
			this.level_nodes_p.Add((uint)(this.littleIndices.Count + this.bigPhysicalIndices.Count + this.bigSmtIndices.Count));
			this.level_nodes_p.Add((uint)(this.littleIndices.Count + this.bigPhysicalIndices.Count + this.bigSmtIndices.Count + this.exlittleIndices.Count));
			this.level_nodes_l.Add((uint)this.littleIndices.Count);
			this.level_nodes_l.Add((uint)(this.littleIndices.Count + this.bigPhysicalIndices.Count));
			this.level_nodes_l.Add((uint)(this.littleIndices.Count + this.bigPhysicalIndices.Count + this.bigSmtIndices.Count));
			this.level_nodes_l.Add((uint)(this.littleIndices.Count + this.bigPhysicalIndices.Count + this.bigSmtIndices.Count + this.exlittleIndices.Count));
			this.coreIndex = new CoreIndexMapper(this.bigPhysicalIndices, this.bigSmtIndices, this.littleIndices, this.exlittleIndices);
			uint[] array6 = new uint[list.Count + 2];
			for (int num4 = 0; num4 < this.maxLP; num4++)
			{
				for (int num5 = 0; num5 <= num4; num5++)
				{
					array6[num4] |= list[num5];
				}
				this.Perfgroup[num4] = new Service1.GroupInfo(0, 0L, 0L, (long)((ulong)list[num4]), 0L, 0L, 0L, 0L, 0L, (long)((ulong)array6[num4]), DateTime.Now.Ticks, 0L);
			}
			uint[] array7 = new uint[list2.Count + 2];
			for (int num6 = 0; num6 < this.maxLP; num6++)
			{
				for (int num7 = 0; num7 <= num6; num7++)
				{
					array7[num6] |= list2[num7];
				}
				this.Effgroup[num6] = new Service1.GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, (long)((ulong)array7[num6]), DateTime.Now.Ticks, 0L);
			}
			this.big_num = Convert.ToInt32(this.number_of_cores) - this.little_num;
			this.perfstatenum = list.Count;
			for (int num8 = 0; num8 < Convert.ToInt32(this.NumberOfLogicalProcessors); num8++)
			{
				UIntPtr uintPtr = (UIntPtr)((ulong)(1L << (num8 & 31)));
				uint num9 = 0U;
				uint num10 = 0U;
				this.myOls.RdmsrTx(1908U, ref num9, ref num10, uintPtr);
				this.myOls.WrmsrTx(1908U, (num9 & 16777215U) | 2147483648U, num10, uintPtr);
				if (((1U << num8) & this.affinitymask_little) > 0U)
				{
					UIntPtr uintPtr2 = (UIntPtr)((ulong)Math.Pow(2.0, (double)num8));
					this.myOls.WrmsrTx(390U, 4391027U, 0U, uintPtr2);
					this.myOls.WrmsrTx(391U, 4391028U, 0U, uintPtr2);
					this.myOls.WrmsrTx(392U, 4391025U, 0U, uintPtr2);
					this.myOls.WrmsrTx(393U, 4391619U, 0U, uintPtr2);
					this.myOls.WrmsrTx(394U, 4391104U, 0U, uintPtr2);
					this.myOls.WrmsrTx(395U, 4390972U, 0U, uintPtr2);
					this.myOls.WrmsrTx(396U, 4259900U, 0U, uintPtr2);
				}
				if (((1U << num8) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
				{
					UIntPtr uintPtr3 = (UIntPtr)((ulong)Math.Pow(2.0, (double)num8));
					this.myOls.WrmsrTx(390U, 4391332U, 0U, uintPtr3);
					this.myOls.WrmsrTx(391U, 4391588U, 0U, uintPtr3);
					this.myOls.WrmsrTx(392U, 4391618U, 0U, uintPtr3);
					this.myOls.WrmsrTx(393U, 4391619U, 0U, uintPtr3);
					this.myOls.WrmsrTx(394U, 4391104U, 0U, uintPtr3);
					this.myOls.WrmsrTx(395U, 4390972U, 0U, uintPtr3);
					this.myOls.WrmsrTx(396U, 4259900U, 0U, uintPtr3);
				}
			}
			this.littleLgroup = new Service1.GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, (long)((ulong)this.affinitymask_little), DateTime.Now.Ticks, 0L);
			this.totalgroup = new Service1.GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, (long)((ulong)(this.affinitymask_little | this.affinitymask_big_phyx | this.affinitymask_big_smt)), DateTime.Now.Ticks, 0L);
			this.little_per_group_count = Math.Max(1, this.little_num / 4);
			Process currentProcess = Process.GetCurrentProcess();
			currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
			try
			{
				Service1.PROCESS_POWER_THROTTLING_STATE process_POWER_THROTTLING_STATE = new Service1.PROCESS_POWER_THROTTLING_STATE
				{
					Version = 1,
					ControlMask = 1U,
					StateMask = 1U
				};
				IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Service1.PROCESS_POWER_THROTTLING_STATE>(process_POWER_THROTTLING_STATE));
				try
				{
					Marshal.StructureToPtr<Service1.PROCESS_POWER_THROTTLING_STATE>(process_POWER_THROTTLING_STATE, intPtr, false);
					Service1.SetProcessInformation(currentProcess.Handle, 4, intPtr, (uint)Marshal.SizeOf<Service1.PROCESS_POWER_THROTTLING_STATE>(process_POWER_THROTTLING_STATE));
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			catch
			{
			}
			long ticks = DateTime.Now.Ticks;
			for (int num11 = 0; num11 < Convert.ToInt32(this.NumberOfLogicalProcessors); num11++)
			{
				this.UpdateNode(1, ref this.wait_core[num11], num11, 0);
				this.UpdateNode(1, ref this.max_ipc_queue[num11], -1, -1);
				this.UpdateNode(1, ref this.max_util_queue[num11], -1, -1);
				this.coreinfo[num11] = new Service1.CoreInfo(num11, ticks, 0L, 0L, 0L, 0L, 0L, 0L, 1U << num11, 1U << num11, ticks, ticks);
				this.exclude[num11] = 0L;
				this.exclude_b[num11] = 0L;
				this.exclude_all[num11] = 0L;
				this.last_duration[num11] = 0L;
				this.now_duration[num11] = 0L;
				this.avg_runtime_b[num11] = 0L;
				this.avg_runtime_l[num11] = 0L;
				this.max_ipc_l[num11] = 0L;
				this.max_ipc_b[num11] = 0L;
				this.temp4[num11] = 0L;
				this.temp5[num11] = 0L;
				this.temp6[num11] = 0L;
			}
			this.sysinfo = new Service1.SysInfo(-1, -1, this.affinitymask_big_phyx | this.affinitymask_big_smt | this.affinitymask_little, this.affinitymask_little);
			this.myOls.RdmsrTx(1905U, ref this.eax, ref this.edx, (UIntPtr)this.littleIndices[0]);
			this.indices = this.littleIndices[0];
			this.max_freq = (long)((ulong)(this.eax & 255U));
			this.insthres = 100L * this.max_freq;
			this.insthres1 = 100L * this.max_freq;
			this.insthres_lower = 5L * this.max_freq;
			this.transformerScheduler.SetTopK(5);
			this.SchedulerRuntime = this.transformerScheduler.GetRuntime();
			if (this.SchedulerRuntime > 19L)
			{
				this.sysinfo.IsModelSaved = true;
				this.transformerScheduler.SetLearningEnabled(false);
				this.transformerScheduler.SetTopK(1);
			}
			this.switchvalue = (this.sysinfo.IsModelSaved ? 100L : 30L);
			this.schedule_queue.Id = 0;
			this.schedule_queue.Value1 = 0L;
			this.schedule_queue.Value2 = 0;
			this.schedule_queue.Next = null;
			this.schedule_queue_little.Id = 0;
			this.schedule_queue_little.Value1 = 0L;
			this.schedule_queue_little.Value2 = 0;
			this.schedule_queue_little.Next = null;
			this.schd_queue_b2l.Id = 0;
			this.schd_queue_b2l.Value1 = 0L;
			this.schd_queue_b2l.Value2 = 0;
			this.schd_queue_b2l.Next = null;
			this.schd_queue_b2s.Id = 0;
			this.schd_queue_b2s.Value1 = 0L;
			this.schd_queue_b2s.Value2 = 0;
			this.schd_queue_b2s.Next = null;
			this.schd_queue_l2b.Id = 0;
			this.schd_queue_l2b.Value1 = 0L;
			this.schd_queue_l2b.Value2 = 0;
			this.schd_queue_l2b.Next = null;
			this.schd_queue_s2b.Id = 0;
			this.schd_queue_s2b.Value1 = 0L;
			this.schd_queue_s2b.Value2 = 0;
			this.schd_queue_s2b.Next = null;
			new Thread(new ThreadStart(this.<OnStart>g__thread1|713_0)).Start();
			new Thread(new ThreadStart(this.<OnStart>g__thread2|713_1)).Start();
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000FAC0 File Offset: 0x0000DCC0
		protected override void OnStop()
		{
			this.transformerScheduler.SaveModel("./scheduler_model.bin");
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000FAD4 File Offset: 0x0000DCD4
		private void OnTimedEvent(object sender, ElapsedEventArgs e)
		{
			this.count_stat1 += 1L;
			if (this.count_stat1 > 32L)
			{
				this.count_stat1 = 0L;
				this.counter_sys += 1L;
				if (this.counter_sys % 4L < 3L)
				{
					this.sysinfo.Counter_sys_enabled = 0;
				}
				else
				{
					this.sysinfo.Counter_sys_enabled = 1;
				}
				this.avg_ins_big = ((this.acc_ins_big_cnt > 0L) ? (this.acc_ins_big / this.acc_ins_big_cnt) : this.avg_ins_big);
			}
			this.count_stat5 += 1L;
			if (this.count_stat5 > 3L)
			{
				this.count_stat5 = 0L;
				ValueTuple<int, int> valueTuple = this.TestAffinity4all(this.sysinfo.Availaff, this.sysinfo.Availaff1);
				this.currentperflvl = valueTuple.Item1;
				this.currentefflvl = valueTuple.Item2;
			}
			this.count_stat3 += 1L;
			if (this.count_stat3 > 3840L)
			{
				this.avg_ipc_trigger = 0L;
				this.count_stat3 = 0L;
				GC.Collect();
				this.learner.SaveModel();
			}
			this.count_stat6 += 1L;
			if (this.count_stat6 > 1920L)
			{
				this.count_stat6 = 0L;
			}
			this.count_stat7 += 1L;
			if (this.count_stat7 > 1920L)
			{
				this.count_stat7 = 0L;
				this.SchedulerRuntime = this.transformerScheduler.GetRuntime();
				if (this.SchedulerRuntime > 19L && !this.sysinfo.IsModelSaved)
				{
					this.transformerScheduler.SaveModel("./scheduler_model.bin");
					this.sysinfo.IsModelSaved = true;
					this.switchvalue = 100L;
					this.transformerScheduler.SetLearningEnabled(false);
					this.transformerScheduler.SetTopK(1);
				}
				if (this.SchedulerRuntime >= 2L && this.SchedulerRuntime <= 19L)
				{
					this.transformerScheduler.SetTopK(3);
				}
			}
			this.count_stat += 1L;
			if (this.count_stat > 320L)
			{
				this.count_stat = 0L;
				string text = "统计数据.txt";
				string text2 = string.Concat(new string[]
				{
					"统计数据",
					Environment.NewLine,
					"tempp:",
					this.tempp.ToString(),
					Environment.NewLine,
					"tempk:",
					this.tempk.ToString(),
					Environment.NewLine,
					"人工调度override:",
					this.tempj.ToString(),
					Environment.NewLine,
					"神经网络统计信息1:",
					this.transformerScheduler.GetStatistics(this.maxLP),
					Environment.NewLine,
					"神经网络统计信息2:",
					this.transformerScheduler.GetLearningReport(),
					Environment.NewLine,
					"神经网络统计信息3:",
					this.transformerScheduler.GetAttentionHeadReport(this.maxLP),
					Environment.NewLine,
					"实际分配大核:",
					this.big_actual.ToString(),
					Environment.NewLine,
					"实际分配小核:",
					this.little_actual.ToString(),
					Environment.NewLine,
					"大核高性能状态:",
					this.perflevel1.ToString(),
					Environment.NewLine,
					"大核能效状态:",
					this.perflevel2.ToString(),
					Environment.NewLine,
					"小核高性能状态:",
					this.perflevel01.ToString(),
					Environment.NewLine,
					"小核能效状态:",
					this.perflevel02.ToString()
				});
				File.WriteAllText(text, text2);
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000FE9F File Offset: 0x0000E09F
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000FEBE File Offset: 0x0000E0BE
		private void InitializeComponent()
		{
			this.components = new Container();
			base.ServiceName = "Service1";
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000FF0C File Offset: 0x0000E10C
		[CompilerGenerated]
		private void <OnStart>g__thread1|713_0()
		{
			using (TraceEventSession traceEventSession = new TraceEventSession("ThreadSwitchSession", TraceEventSessionOptions.Create))
			{
				traceEventSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread | KernelTraceEventParser.Keywords.ContextSwitch, KernelTraceEventParser.Keywords.None);
				traceEventSession.Source.Kernel.ThreadStop += delegate(ThreadTraceData data)
				{
					int threadID = data.ThreadID;
					int processID = data.ProcessID;
					this.RemoveThread(threadID, processID);
				};
				traceEventSession.Source.Kernel.ProcessStop += delegate(ProcessTraceData data)
				{
					int processID2 = data.ProcessID;
					this.RemoveProcessAndThreads(processID2);
				};
				traceEventSession.Source.Kernel.ThreadCSwitch += delegate(CSwitchTraceData data)
				{
					int num = 0;
					uint num2 = 0U;
					uint num3 = 0U;
					int num4 = 0;
					int num5 = 0;
					num4 = data.OldThreadID;
					int newThreadID = data.NewThreadID;
					num5 = data.OldProcessID;
					int newProcessID = data.NewProcessID;
					num = data.ProcessorNumber;
					this.currentprocessor[num] = num;
					uint num6 = (1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt);
					UIntPtr uintPtr = (UIntPtr)((ulong)Math.Pow(2.0, (double)num));
					this.currentprocnum_index = num / 2;
					this.datetime_new[num] = DateTime.Now.Ticks;
					this.datetime_elapsed[num] = (this.datetime_new[num] - this.datetime_old[num]) / 10L;
					this.datetime_old[num] = this.datetime_new[num];
					long num7 = (long)Math.Abs(num4 % 10000);
					long num8 = (long)Math.Abs(num5 % 10000);
					if (num5 != 0)
					{
						Service1.CoreInfo coreInfo = this.coreinfo[num];
						long num9 = coreInfo.Threadcount;
						coreInfo.Threadcount = num9 + 1L;
						this.coreinfo[num].RunTime4queque += this.datetime_elapsed[num];
						this.coreinfo[num].RunTime4queque4sched += this.datetime_elapsed[num];
						this.coreinfo[num].RunTime4usage += this.datetime_elapsed[num];
						this.coreinfo[num].threadexecinfo.AddOrUpdate(num4, this.datetime_elapsed[num]);
						this.coreinfo[num].numberProcessor.AddData(this.datetime_elapsed[num]);
						this.coreinfo[num].accRuntimePerQ += this.datetime_elapsed[num];
						if (this.coreinfo[num].threadContrib.ContainsKey(num4))
						{
							this.coreinfo[num].accRewardPerQ -= this.coreinfo[num].threadContrib[num4];
						}
						long num10 = this.coreinfo[num].accRuntimePerQ * (long)(data.OldThreadPriority + 15) / 15L;
						this.coreinfo[num].accRewardPerQ += num10;
						this.coreinfo[num].threadContrib[num4] = num10;
					}
					if (num5 != 0 && newProcessID == 0)
					{
						Service1.CoreInfo coreInfo2 = this.coreinfo[num];
						long num9 = coreInfo2.SustainedThreadcount;
						coreInfo2.SustainedThreadcount = num9 + 1L;
						Service1.CoreInfo coreInfo3 = this.coreinfo[num];
						num9 = coreInfo3.SustainedThreadcount4sched;
						coreInfo3.SustainedThreadcount4sched = num9 + 1L;
						Service1.CoreInfo coreInfo4 = this.coreinfo[num];
						num9 = coreInfo4.Cycle;
						coreInfo4.Cycle = num9 + 1L;
						this.coreinfo[num].Accthreadcount += (long)this.coreinfo[num].threadexecinfo.Count;
						this.coreinfo[num].threadexecinfo.Clear();
						this.coreinfo[num].AccMaxTime += Math.Max(this.coreinfo[num].numberProcessor.GetMax(), 0L);
						this.coreinfo[num].numberProcessor.Clear();
						this.coreinfo[num].accRunTime4usage += this.coreinfo[num].RunTime4usage;
						this.coreinfo[num].Threadcount = 0L;
						this.coreinfo[num].RunTime4usage = 0L;
						this.sysinfo.accRewordPerS += ((this.coreinfo[num].threadContrib.Count > 0) ? (this.coreinfo[num].accRewardPerQ / (long)this.coreinfo[num].threadContrib.Count) : 0L);
						Service1.SysInfo sysInfo = this.sysinfo;
						num9 = sysInfo.accQcount;
						sysInfo.accQcount = num9 + 1L;
						this.coreinfo[num].accRuntimePerQ = 0L;
						this.coreinfo[num].accRewardPerQ = 0L;
						this.coreinfo[num].threadContrib.Clear();
					}
					long num11 = DateTime.Now.Ticks - this.coreinfo[num].DateTime;
					long num12 = DateTime.Now.Ticks - this.coreinfo[num].DateTime4sched;
					if (num11 > 1000000L && this.coreinfo[num].SustainedThreadcount > 10L)
					{
						this.coreinfo[num].Utilization = 1000L * this.coreinfo[num].RunTime4queque / num11;
						this.coreinfo[num].Utilization4q = this.coreinfo[num].RunTime4queque / this.coreinfo[num].SustainedThreadcount;
						this.coreinfo[num].AvgMaxTime = this.coreinfo[num].AccMaxTime / this.coreinfo[num].SustainedThreadcount;
						this.coreinfo[num].Avgthreadcount = 100L * this.coreinfo[num].Accthreadcount / this.coreinfo[num].SustainedThreadcount;
						this.coreinfo[num].QueueInterval = num11 / (this.coreinfo[num].SustainedThreadcount * 10L);
						if (!this.sysinfo.IsModelSaved)
						{
							if (((1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
							{
								UIntPtr uintPtr2 = (UIntPtr)((ulong)(1L << (num & 31)));
								uint num13 = 0U;
								uint num14 = 0U;
								this.myOls.RdmsrTx(1908U, ref num13, ref num14, uintPtr2);
								this.myOls.WrmsrTx(1908U, (num13 & 16777215U) | 0U, num14, uintPtr2);
							}
							else if (this.coreinfo[num].Utilization4q > 1000L)
							{
								this.count4level3 = 0;
								this.coreinfo[num].P_state = 1;
								UIntPtr uintPtr3 = (UIntPtr)((ulong)(1L << (num & 31)));
								uint num15 = 0U;
								uint num16 = 0U;
								this.myOls.RdmsrTx(1908U, ref num15, ref num16, uintPtr3);
								this.myOls.WrmsrTx(1908U, (num15 & 16777215U) | 1426063360U, num16, uintPtr3);
								this.perflevel01++;
							}
							else
							{
								this.count4level3 = 0;
								this.coreinfo[num].P_state = 2;
								UIntPtr uintPtr4 = (UIntPtr)((ulong)(1L << (num & 31)));
								uint num17 = 0U;
								uint num18 = 0U;
								this.myOls.RdmsrTx(1908U, ref num17, ref num18, uintPtr4);
								this.myOls.WrmsrTx(1908U, (num17 & 16777215U) | 2147483648U, num18, uintPtr4);
								this.perflevel02++;
							}
						}
						else if (((1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
						{
							if (this.coreinfo[num].Utilization4q > 1000L)
							{
								this.count4level3 = 0;
								this.coreinfo[num].P_state = 1;
								UIntPtr uintPtr5 = (UIntPtr)((ulong)(1L << (num & 31)));
								uint num19 = 0U;
								uint num20 = 0U;
								this.myOls.RdmsrTx(1908U, ref num19, ref num20, uintPtr5);
								this.myOls.WrmsrTx(1908U, (num19 & 16777215U) | 1426063360U, num20, uintPtr5);
								this.perflevel1++;
							}
							else
							{
								this.count4level3 = 0;
								this.coreinfo[num].P_state = 2;
								UIntPtr uintPtr6 = (UIntPtr)((ulong)(1L << (num & 31)));
								uint num21 = 0U;
								uint num22 = 0U;
								this.myOls.RdmsrTx(1908U, ref num21, ref num22, uintPtr6);
								this.myOls.WrmsrTx(1908U, (num21 & 16777215U) | 2147483648U, num22, uintPtr6);
								this.perflevel2++;
							}
						}
						else if (this.coreinfo[num].Utilization4q > 1000L)
						{
							this.count4level3 = 0;
							this.coreinfo[num].P_state = 1;
							UIntPtr uintPtr7 = (UIntPtr)((ulong)(1L << (num & 31)));
							uint num23 = 0U;
							uint num24 = 0U;
							this.myOls.RdmsrTx(1908U, ref num23, ref num24, uintPtr7);
							this.myOls.WrmsrTx(1908U, (num23 & 16777215U) | 1426063360U, num24, uintPtr7);
							this.perflevel01++;
						}
						else
						{
							this.count4level3 = 0;
							this.coreinfo[num].P_state = 2;
							UIntPtr uintPtr8 = (UIntPtr)((ulong)(1L << (num & 31)));
							uint num25 = 0U;
							uint num26 = 0U;
							this.myOls.RdmsrTx(1908U, ref num25, ref num26, uintPtr8);
							this.myOls.WrmsrTx(1908U, (num25 & 16777215U) | 2147483648U, num26, uintPtr8);
							this.perflevel02++;
						}
						this.myOls.RdmsrTx(197U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].instructions4sys_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].instructions4sys = Math.Max(this.coreinfo[num].instructions4sys_l - this.coreinfo[num].instructions4sys_e, 0L);
						this.sysinfo.total_instructions += this.coreinfo[num].instructions4sys;
						this.coreinfo[num].instructions4sys_e = this.coreinfo[num].instructions4sys_l;
						this.myOls.RdmsrTx(198U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].cycles4sys_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].cycles4sys = Math.Max(this.coreinfo[num].cycles4sys_l - this.coreinfo[num].cycles4sys_e, 0L);
						this.coreinfo[num].cycles4sys_e = this.coreinfo[num].cycles4sys_l;
						this.myOls.RdmsrTx(194U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].missrate4c_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].missrate4c = Math.Max(this.coreinfo[num].missrate4c_l - this.coreinfo[num].missrate4c_e, 0L);
						this.coreinfo[num].missrate4c_e = this.coreinfo[num].missrate4c_l;
						this.myOls.RdmsrTx(195U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].load_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].load = Math.Max(this.coreinfo[num].load_l - this.coreinfo[num].load_e, 0L);
						this.coreinfo[num].load_e = this.coreinfo[num].load_l;
						this.myOls.RdmsrTx(196U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].store_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].store = Math.Max(this.coreinfo[num].store_l - this.coreinfo[num].store_e, 0L);
						this.coreinfo[num].store_e = this.coreinfo[num].store_l;
						this.myOls.RdmsrTx(193U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].mem_ordering_count_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].mem_ordering_count = Math.Max(this.coreinfo[num].mem_ordering_count_l - this.coreinfo[num].mem_ordering_count_e, 0L);
						this.coreinfo[num].mem_ordering_count_e = this.coreinfo[num].mem_ordering_count_l;
						this.sysinfo.total_runtime += this.coreinfo[num].accRunTime4usage + this.coreinfo[num].RunTime4usage;
						this.coreinfo[num].accRunTime4usage = 0L;
						this.coreinfo[num].RunTime4usage = 0L;
						this.coreinfo[num].ipc4c = this.coreinfo[num].CalcRatio1(this.coreinfo[num].instructions4sys, this.coreinfo[num].cycles4sys, this.coreinfo[num].ipc4c);
						this.coreinfo[num].perf4c = this.coreinfo[num].CalcRatio1(this.coreinfo[num].instructions4sys, this.coreinfo[num].RunTime4queque, this.coreinfo[num].perf4c);
						if (((1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
						{
							this.coreinfo[num].missrateratio4c = this.coreinfo[num].CalcRatio1(this.coreinfo[num].mem_ordering_count - this.coreinfo[num].load, this.coreinfo[num].instructions4sys, this.coreinfo[num].missrateratio4c);
							this.sysinfo.total_llcmiss += this.coreinfo[num].mem_ordering_count - this.coreinfo[num].load;
						}
						else
						{
							this.coreinfo[num].missrateratio4c = this.coreinfo[num].CalcRatio1(this.coreinfo[num].mem_ordering_count + this.coreinfo[num].missrate4c + this.coreinfo[num].load, this.coreinfo[num].instructions4sys, this.coreinfo[num].missrateratio4c);
							this.sysinfo.total_llcmiss += this.coreinfo[num].mem_ordering_count + this.coreinfo[num].missrate4c + this.coreinfo[num].load;
						}
						this.coreinfo[num].mem_ordering = this.coreinfo[num].CalcRatio1(this.coreinfo[num].store, this.coreinfo[num].instructions4sys, this.coreinfo[num].mem_ordering);
						this.coreFeatures[num] = new float[]
						{
							(float)this.coreinfo[num].Utilization,
							(float)this.coreinfo[num].Utilization4q,
							(float)this.coreinfo[num].Avgthreadcount,
							(float)this.coreinfo[num].QueueInterval,
							this.coreinfo[num].missrateratio4c,
							this.coreinfo[num].mem_ordering,
							this.coreinfo[num].ipc4c,
							this.coreinfo[num].perf4c,
							(float)num
						};
						this.coreinfo[num].RunTime4queque = 0L;
						this.coreinfo[num].SustainedThreadcount = 0L;
						this.coreinfo[num].AccMaxTime = 0L;
						this.coreinfo[num].Accthreadcount = 0L;
						this.coreinfo[num].DateTime = DateTime.Now.Ticks;
					}
					if (num12 > 1000000L && this.coreinfo[num].SustainedThreadcount4sched > 10L)
					{
						this.coreinfo[num].Utilization4sched = 1000L * this.coreinfo[num].RunTime4queque4sched / num12;
						this.coreinfo[num].Utilization4q4sched = this.coreinfo[num].RunTime4queque4sched / this.coreinfo[num].SustainedThreadcount4sched;
						this.coreinfo[num].RunTime4queque4sched = 0L;
						this.coreinfo[num].SustainedThreadcount4sched = 0L;
						this.sysinfo.CoreLoadSeq.AddOrUpdate(num, this.coreinfo[num].Utilization4sched);
						if (((1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
						{
							if (this.coreinfo[num].Utilization4sched > 70L)
							{
								this.sysinfo.Availaff = this.sysinfo.Availaff & ~(1U << num);
							}
							if (this.coreinfo[num].Utilization4sched > 70L)
							{
								this.sysinfo.Availaff1 = this.sysinfo.Availaff1 & ~(1U << num);
							}
							if (this.coreinfo[num].Utilization4sched < 37L)
							{
								this.sysinfo.Availaff = this.sysinfo.Availaff | (1U << num);
							}
							if (this.coreinfo[num].Utilization4sched < 37L)
							{
								this.sysinfo.Availaff1 = this.sysinfo.Availaff1 | (1U << num);
							}
						}
						else
						{
							if (this.coreinfo[num].Utilization4sched > 70L)
							{
								this.sysinfo.Availaff = this.sysinfo.Availaff & ~(1U << num);
							}
							if (this.coreinfo[num].Utilization4sched > 70L)
							{
								this.sysinfo.Availaff1 = this.sysinfo.Availaff1 & ~(1U << num);
							}
							if (this.coreinfo[num].Utilization4sched < 37L)
							{
								this.sysinfo.Availaff = this.sysinfo.Availaff | (1U << num);
							}
							if (this.coreinfo[num].Utilization4sched < 37L)
							{
								this.sysinfo.Availaff1 = this.sysinfo.Availaff1 | (1U << num);
							}
						}
						this.coreinfo[num].DateTime4sched = DateTime.Now.Ticks;
					}
					if ((this.currentthread != num5) & (num5 != 0))
					{
						object obj = Service1.lockProcessCreation;
						checked
						{
							lock (obj)
							{
								this.findprocessinfo[num] = this.FindProcess(ref this.processinfo[(int)((IntPtr)num8)], num5);
								if (this.findprocessinfo[num] == null)
								{
									this.findprocessinfo[num] = this.UpdateProcessInfo(500, ref this.processinfo[(int)((IntPtr)num8)], new Service1.ProcessInfo(num5, DateTime.Now.Ticks, 0L, 0L, 0L, 0L, 0L, 0L, 0, 0, 0L, 0L, 0L, 0, 0, DateTime.Now.Ticks, DateTime.Now.Ticks));
									this.findprocessinfo[num].datetime_elapse = DateTime.Now.Ticks;
								}
							}
							this.findthreadinfo[num] = this.FindThread(ref this.threadinfo[(int)((IntPtr)num7)], num4);
						}
						this.cnt_findnode += 1L;
						if (this.findthreadinfo[num] == null)
						{
							this.cnt_not_findnode += 1L;
							this.findthreadinfo[num] = this.UpdateThreadInfo(500, ref this.threadinfo[(int)(checked((IntPtr)num7))], new Service1.ThreadInfo(num4, DateTime.Now.Ticks, 0L, 0L, 0L, 0L, 0L, 50000L, 0L, 0L, 0L, 1, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0U, 0, new Service1.ThreadInfoSimp(num4, 0L, 0L, 0L, -1, 0, null), 0L, 0L, 0L, 0, 0L, 0L, 1, 0L, 0L, 0L, 0L, 0L, 0L, DateTime.Now.Ticks, DateTime.Now.Ticks));
							this.findthreadinfo[num].Processinfo = this.findprocessinfo[num];
							Service1.ThreadInfoSimp threadInfoSimp = new Service1.ThreadInfoSimp(this.findthreadinfo[num].Tid, 0L, 0L, 0L, 1, this.findthreadinfo[num].Group, this.findthreadinfo[num]);
							if (this.findprocessinfo[num] != null)
							{
								Service1.ThreadInfoSimp threadSet = this.findprocessinfo[num].ThreadSet;
								this.findprocessinfo[num].ThreadSet = this.UpdateThreadInfoSimp(500, ref threadSet, threadInfoSimp);
							}
							this.findthreadinfo[num].Groupinfo = this.Perfgroup[this.perfstatenum - 1];
							this.findthreadinfo[num].Perflvl = this.perfstatenum - 1;
							this.findthreadinfo[num].Efflvl = 3;
							this.findthreadinfo[num].demoteacc = DateTime.Now.Ticks;
							this.findthreadinfo[num].SchedType = 1L;
							this.findthreadinfo[num].Sched = 1;
						}
						Service1.ThreadInfo threadInfo = this.findthreadinfo[num];
						long num9 = threadInfo.Count_sample;
						threadInfo.Count_sample = num9 + 1L;
						this.findthreadinfo[num].RunTime += this.datetime_elapsed[num];
						this.findthreadinfo[num].WaitTime += (long)this.oldthread_waittime[num];
						Service1.ThreadInfo threadInfo2 = this.findthreadinfo[num];
						num9 = threadInfo2.Count_sample1;
						threadInfo2.Count_sample1 = num9 + 1L;
						this.findthreadinfo[num].Duration -= this.datetime_elapsed[num];
						if (this.findthreadinfo[num].Duration < 0L)
						{
							this.findthreadinfo[num].Duration = 0L;
						}
						if (this.findthreadinfo[num].Duration == 0L && this.findthreadinfo[num].RunTime > 300000L)
						{
							this.findthreadinfo[num].Ipc_reset_count = this.findthreadinfo[num].RunTime / this.findthreadinfo[num].Count_sample;
							this.findthreadinfo[num].Count_sample = 0L;
							this.findthreadinfo[num].RunTime = 0L;
							this.findthreadinfo[num].DateTime = DateTime.Now.Ticks;
						}
					}
					if ((this.sysinfo.Counter_sys_enabled != 0 || this.coreinfo[num].CounterEnabled != 0) && this.coreinfo[num].CounterEnabled == 1)
					{
						this.myOls.RdmsrTx(198U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register6_l = (long)((ulong)(num3 | num2));
						this.myOls.RdmsrTx(195U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register3_l = (long)((ulong)(num3 | num2));
						this.myOls.RdmsrTx(196U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register4_l = (long)((ulong)(num3 | num2));
						this.myOls.RdmsrTx(194U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register2_l = (long)((ulong)(num3 | num2));
						this.myOls.RdmsrTx(197U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register5_l = (long)((ulong)(num3 | num2));
						this.myOls.RdmsrTx(193U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register1_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register6 = Math.Max(this.coreinfo[num].Register6_l - this.coreinfo[num].Register6_e, 0L);
						this.coreinfo[num].Register5 = Math.Max(this.coreinfo[num].Register5_l - this.coreinfo[num].Register5_e, 0L);
						this.coreinfo[num].Register4 = Math.Max(this.coreinfo[num].Register4_l - this.coreinfo[num].Register4_e, 0L);
						this.coreinfo[num].Register3 = Math.Max(this.coreinfo[num].Register3_l - this.coreinfo[num].Register3_e, 0L);
						this.coreinfo[num].Register2 = Math.Max(this.coreinfo[num].Register2_l - this.coreinfo[num].Register2_e, 0L);
						this.coreinfo[num].Register1 = Math.Max(this.coreinfo[num].Register1_l - this.coreinfo[num].Register1_e, 0L);
						if ((this.currentthread != num5) & (num5 != 0))
						{
							this.findthreadinfo[num].L4_miss += this.coreinfo[num].Register3;
							this.findthreadinfo[num].Ins_retire += this.coreinfo[num].Register5;
							Service1.ThreadInfo threadInfo3 = this.findthreadinfo[num];
							long num9 = threadInfo3.PriorityAcc;
							threadInfo3.PriorityAcc = num9 + 1L;
							this.findthreadinfo[num].avgruntime_total += this.datetime_elapsed[num];
							if (((1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
							{
								this.findthreadinfo[num].L3_miss += this.coreinfo[num].Register1 - this.coreinfo[num].Register3;
								this.findthreadinfo[num].CodeFootPrint_counter1 += this.coreinfo[num].Register5;
								this.findthreadinfo[num].L1_miss1 += this.coreinfo[num].Register4;
								this.findthreadinfo[num].L3_miss1 += this.coreinfo[num].Register5;
							}
							if (((1U << num) & this.affinitymask_little) > 0U)
							{
								this.findthreadinfo[num].L3_miss += this.coreinfo[num].Register1 + this.coreinfo[num].Register2 + this.coreinfo[num].Register3;
								this.findthreadinfo[num].CodeFootPrint_counter1 += this.coreinfo[num].Register5;
								this.findthreadinfo[num].L1_miss1 += this.coreinfo[num].Register4;
								this.findthreadinfo[num].L3_miss1 += this.coreinfo[num].Register5;
							}
							this.findthreadinfo[num].IntVal = (DateTime.Now.Ticks - this.findthreadinfo[num].DateTime4interval) / 10L;
							this.findthreadinfo[num].L2_miss += this.coreinfo[num].Register6;
							try
							{
								IntPtr intPtr = Service1.OpenThread((Service1.ThreadAccess)96U, false, (uint)num4);
								if (intPtr != IntPtr.Zero)
								{
									this.thread_priority[num] = Service1.GetThreadPriority(intPtr);
									Service1.CloseHandle(intPtr);
								}
								else
								{
									this.thread_priority[num] = 0;
								}
							}
							catch
							{
								this.thread_priority[num] = 0;
							}
							try
							{
								IntPtr intPtr2 = Service1.OpenProcess((Service1.ProcessAccess)1536U, false, (uint)num5);
								if (intPtr2 != IntPtr.Zero)
								{
									this.process_priority[num] = Service1.GetPriorityClass(intPtr2);
									Service1.CloseHandle(intPtr2);
								}
								else
								{
									this.process_priority[num] = 0;
								}
							}
							catch
							{
								this.process_priority[num] = 0;
							}
							this.findthreadinfo[num].Clock_big += (long)Service1.ThreadPriorityMapper.GetFinalPriority(this.process_priority[num], this.thread_priority[num]);
							if (this.findthreadinfo[num].PriorityAcc > 1000L || this.findthreadinfo[num].Ins_retire > 300000L)
							{
								this.findthreadinfo[num].Ins_per_count = this.findthreadinfo[num].CalcRatio(this.findthreadinfo[num].Ins_retire, this.findthreadinfo[num].PriorityAcc, this.findthreadinfo[num].Ins_per_count);
								this.findthreadinfo[num].UserModeRatio = (double)((float)this.findthreadinfo[num].L4_miss / (float)this.findthreadinfo[num].Ins_retire);
								this.findthreadinfo[num].Ins_big1 = this.findthreadinfo[num].CalcRatio1(this.findthreadinfo[num].L1_miss1, this.findthreadinfo[num].L3_miss1, this.findthreadinfo[num].Ins_big1);
								this.findthreadinfo[num].CodeFootPrint = this.findthreadinfo[num].CalcRatio(1000000L * this.findthreadinfo[num].CodeFootPrint_counter1, this.findthreadinfo[num].Ins_retire, this.findthreadinfo[num].CodeFootPrint);
								this.findthreadinfo[num].Ins_big = this.findthreadinfo[num].CalcRatio1(this.findthreadinfo[num].L3_miss, this.findthreadinfo[num].CodeFootPrint_counter1, this.findthreadinfo[num].Ins_big);
								this.findthreadinfo[num].Ipc = this.findthreadinfo[num].CalcRatio(this.findthreadinfo[num].IntVal, this.findthreadinfo[num].Count_sample1, this.findthreadinfo[num].Ipc);
								this.findthreadinfo[num].Clock = this.findthreadinfo[num].CalcRatio1(this.findthreadinfo[num].Ins_retire, this.findthreadinfo[num].L2_miss, this.findthreadinfo[num].Clock);
								this.findthreadinfo[num].InsPressure = this.findthreadinfo[num].CalcRatio1(this.findthreadinfo[num].Clock_big, this.findthreadinfo[num].PriorityAcc, this.findthreadinfo[num].InsPressure);
								this.findthreadinfo[num].avgruntime = this.findthreadinfo[num].CalcRatio1(this.findthreadinfo[num].WaitTime, this.findthreadinfo[num].Count_sample1, this.findthreadinfo[num].avgruntime);
								float[] array = new float[]
								{
									(float)this.findthreadinfo[num].Ins_per_count,
									(float)(data.OldThreadPriority + 15),
									(float)this.findthreadinfo[num].avgruntime,
									(float)this.findthreadinfo[num].Ipc,
									(float)this.findthreadinfo[num].Clock,
									(float)this.findthreadinfo[num].Ins_big,
									(float)this.findthreadinfo[num].Ins_big1,
									(float)num
								};
								this.findthreadinfo[num].PrevCoreType = this.transformerScheduler.Schedule(array, this.coreFeatures, this.maxLP, num4, this.findthreadinfo[num].PrevCoreType, num);
								if (((1U << this.findthreadinfo[num].PrevCoreType) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) == 0U)
								{
									this.tempk += 1L;
									this.findthreadinfo[num].CoreType = 0;
								}
								else
								{
									this.findthreadinfo[num].CoreType = 1;
									this.tempp += 1L;
								}
								this.findthreadinfo[num].Ins_retire = 0L;
								this.findthreadinfo[num].PriorityAcc = 0L;
								this.findthreadinfo[num].L3_miss1 = 0L;
								this.findthreadinfo[num].L3_miss = 0L;
								this.findthreadinfo[num].L1_miss1 = 0L;
								this.findthreadinfo[num].L2_miss = 0L;
								this.findthreadinfo[num].WaitTime = 0L;
								this.findthreadinfo[num].CodeFootPrint_counter1 = 0L;
								this.findthreadinfo[num].Count_sample1 = 0L;
								this.findthreadinfo[num].Clock_big = 0L;
								this.findthreadinfo[num].avgruntime_total = 0L;
								this.findthreadinfo[num].DateTime4interval = DateTime.Now.Ticks;
							}
							if (((1U << num) & this.affinitymask_little) > 0U)
							{
								Service1.ThreadInfo threadInfo4 = this.findthreadinfo[num];
								num9 = threadInfo4.Count_internal1;
								threadInfo4.Count_internal1 = num9 + 1L;
								this.findthreadinfo[num].ins_little += this.coreinfo[num].Register5;
								this.findthreadinfo[num].clock_little += this.coreinfo[num].Register6;
								if (this.findthreadinfo[num].ins_little > 300000L || this.findthreadinfo[num].Count_internal1 > 1000L)
								{
									this.findthreadinfo[num].Clock_litte = this.findthreadinfo[num].CalcRatio(100L * this.findthreadinfo[num].ins_little, this.findthreadinfo[num].clock_little, this.findthreadinfo[num].Clock_litte);
									this.findthreadinfo[num].ins_little = 0L;
									this.findthreadinfo[num].clock_little = 0L;
									this.findthreadinfo[num].Count_internal1 = 0L;
								}
							}
							if (((1U << num) & (this.affinitymask_big_phyx | this.affinitymask_big_smt)) > 0U)
							{
								this.findthreadinfo[num].Dummy += this.coreinfo[num].Register5;
								Service1.ThreadInfo threadInfo5 = this.findthreadinfo[num];
								num9 = threadInfo5.Count_internal2;
								threadInfo5.Count_internal2 = num9 + 1L;
								if ((this.findthreadinfo[num].Dummy > 300000L || this.findthreadinfo[num].Count_internal2 > 1000L) && this.findthreadinfo[num].L2_miss > 0L)
								{
									this.findthreadinfo[num].Dummy = 0L;
									this.findthreadinfo[num].Count_internal2 = 0L;
								}
							}
						}
					}
					if ((this.currentthread != num5) & (num5 != 0))
					{
						float num27 = Math.Max(this.GetFactor(this.findthreadinfo[num].Clock_litte), 0.5f);
						int num28 = this.currentefflvl;
						int num29 = this.currentperflvl;
						int num30 = this.currentsmtlvl;
						int coreType = this.findthreadinfo[num].CoreType;
						if (coreType == 0)
						{
							if (this.findthreadinfo[num].Duration == 0L)
							{
								if (num28 >= this.findthreadinfo[num].Efflvl)
								{
									this.findthreadinfo[num].Efflvl = num28;
								}
								else
								{
									this.findthreadinfo[num].Efflvl = ((this.findthreadinfo[num].Efflvl > 0) ? (this.findthreadinfo[num].Efflvl - 1) : 0);
								}
								this.findthreadinfo[num].Efflvl = (int)(((long)this.findthreadinfo[num].Efflvl < (long)((ulong)(this.level_nodes_l[0] - 1U))) ? (this.level_nodes_l[0] - 1U) : ((uint)this.findthreadinfo[num].Efflvl));
								if (this.findthreadinfo[num].Efflvl > (int)(this.level_nodes_l[0] - 1U))
								{
									this.L2Bresidency += 1L;
								}
								else
								{
									this.Lresidency += 1L;
								}
								this.findthreadinfo[num].Groupinfo = this.Effgroup[this.findthreadinfo[num].Efflvl];
								this.findthreadinfo[num].Sched = 1;
								this.findthreadinfo[num].Duration = 100000L;
								this.findthreadinfo[num].CoreType = 0;
							}
						}
						else if (coreType == 1)
						{
							if (num29 > this.big_num - 1)
							{
								if (num29 > 3 + this.big_num)
								{
									this.B2L2Sresidency += 1L;
								}
								else
								{
									this.B2Lresidency += 1L;
								}
							}
							else
							{
								this.Bresidency += 1L;
							}
							if (this.findthreadinfo[num].Duration == 0L || num29 > this.findthreadinfo[num].Perflvl)
							{
								if (num29 >= this.findthreadinfo[num].Perflvl)
								{
									this.findthreadinfo[num].Perflvl = num29;
								}
								else
								{
									this.findthreadinfo[num].Perflvl = ((this.findthreadinfo[num].Perflvl > 0) ? (this.findthreadinfo[num].Perflvl - 1) : 0);
								}
								this.findthreadinfo[num].Perflvl = (int)this.GetLevel(1, this.findthreadinfo[num].Perflvl);
								this.findthreadinfo[num].Groupinfo = this.Perfgroup[this.findthreadinfo[num].Perflvl];
								this.findthreadinfo[num].Sched = 1;
								this.findthreadinfo[num].Duration = 100000L;
								this.findthreadinfo[num].CoreType = 1;
							}
						}
						if (this.findthreadinfo[num].Sched == 1)
						{
							try
							{
								IntPtr intPtr3 = Service1.OpenThread((Service1.ThreadAccess)96U, false, (uint)num4);
								if (intPtr3 != IntPtr.Zero)
								{
									if (this.SchedulerRuntime > 19L)
									{
										Service1.SetThreadIdealProcessor(intPtr3, this.findthreadinfo[num].PrevCoreType);
										if (this.findthreadinfo[num].CoreType == 1)
										{
											Service1.SetThreadAffinityMask(intPtr3, (IntPtr)((long)((ulong)this.affinitymask_all)));
										}
										else if (this.findthreadinfo[num].Ins_per_count > 800000L)
										{
											this.tempj += 1L;
											Service1.SetThreadAffinityMask(intPtr3, (IntPtr)((long)((ulong)this.affinitymask_all)));
										}
										else
										{
											Service1.SetThreadAffinityMask(intPtr3, (IntPtr)((long)((ulong)this.affinitymask_little)));
										}
										Service1.CloseHandle(intPtr3);
									}
									else
									{
										Service1.SetThreadAffinityMask(intPtr3, (IntPtr)(1 << this.findthreadinfo[num].PrevCoreType));
										Service1.SetThreadIdealProcessor(intPtr3, this.findthreadinfo[num].PrevCoreType);
										Service1.CloseHandle(intPtr3);
									}
								}
							}
							catch
							{
							}
							this.findthreadinfo[num].Sched = 0;
						}
					}
					if (this.sysinfo.Counter_sys_enabled == 1 || this.coreinfo[num].CounterEnabled == 1)
					{
						this.myOls.RdmsrTx(198U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register6_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register6_e = this.coreinfo[num].Register6_l;
						this.myOls.RdmsrTx(195U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register3_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register3_e = this.coreinfo[num].Register3_l;
						this.myOls.RdmsrTx(196U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register4_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register4_e = this.coreinfo[num].Register4_l;
						this.myOls.RdmsrTx(194U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register2_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register2_e = this.coreinfo[num].Register2_l;
						this.myOls.RdmsrTx(197U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register5_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register5_e = this.coreinfo[num].Register5_l;
						this.myOls.RdmsrTx(193U, ref num2, ref num3, uintPtr);
						this.coreinfo[num].Register1_l = (long)((ulong)(num3 | num2));
						this.coreinfo[num].Register1_e = this.coreinfo[num].Register1_l;
					}
					if (this.sysinfo.Counter_sys_enabled == 1 && this.coreinfo[num].CounterEnabled == 0)
					{
						this.coreinfo[num].CounterEnabled = 1;
					}
					else if (this.sysinfo.Counter_sys_enabled == 0 && this.coreinfo[num].CounterEnabled == 1)
					{
						this.coreinfo[num].CounterEnabled = 0;
					}
					this.oldthread_waittime[num] = data.NewThreadWaitTime;
					if (((1U << num) & this.affinitymask_big_phyx) > 0U || ((1U << num) & this.affinitymask_big_smt) > 0U)
					{
						this.big_actual++;
					}
					else
					{
						this.little_actual++;
					}
					if (DateTime.Now.Ticks - this.sysinfo.Datetime > 10000000L && this.sysinfo.update)
					{
						this.sysinfo.update = false;
						this.myOls.RdmsrTx(1553U, ref num2, ref num3, uintPtr);
						this.sysinfo.total_energy_l = (long)((ulong)(num3 | num2));
						this.sysinfo.total_energy = Math.Max(this.sysinfo.total_energy_l - this.sysinfo.total_energy_e, 0L);
						this.sysinfo.total_energy_e = this.sysinfo.total_energy_l;
						if (this.sysinfo.accQcount > 0L && this.sysinfo.total_instructions > 0L)
						{
							this.transformerScheduler.UpdateTAT((float)this.sysinfo.accRewordPerS / (float)this.sysinfo.accQcount, (float)(-(float)this.sysinfo.total_llcmiss / this.sysinfo.total_instructions));
							this.sysinfo.accQcount = 0L;
							this.sysinfo.accRewordPerS = 0L;
							this.sysinfo.total_energy = 0L;
							this.sysinfo.total_instructions = 0L;
							this.sysinfo.total_llcmiss = 0L;
						}
						this.sysinfo.total_runtime = 0L;
						this.sysinfo.Datetime = DateTime.Now.Ticks;
						this.sysinfo.update = true;
					}
				};
				traceEventSession.Source.Process();
			}
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00012880 File Offset: 0x00010A80
		[CompilerGenerated]
		private void <OnStart>g__thread2|713_1()
		{
			global::System.Timers.Timer timer = new global::System.Timers.Timer(30.0);
			timer.Elapsed += this.OnTimedEvent;
			timer.Start();
		}

		// Token: 0x0400016E RID: 366
		private static readonly object lockProcessCreation = new object();

		// Token: 0x0400016F RID: 367
		private static readonly object thread = new object();

		// Token: 0x04000170 RID: 368
		private static readonly object lockObject = new object();

		// Token: 0x04000171 RID: 369
		private static readonly object Group = new object();

		// Token: 0x04000172 RID: 370
		private static readonly object counts = new object();

		// Token: 0x04000173 RID: 371
		private Service1.Node1 record = new Service1.Node1();

		// Token: 0x04000174 RID: 372
		private Service1.Node wait_queue = new Service1.Node();

		// Token: 0x04000175 RID: 373
		private Service1.Node1[] threadrecord = new Service1.Node1[10000];

		// Token: 0x04000176 RID: 374
		private Service1.NodeP[] processrecord = new Service1.NodeP[10000];

		// Token: 0x04000177 RID: 375
		private Service1.ThreadInfo[] threadinfo = new Service1.ThreadInfo[10000];

		// Token: 0x04000178 RID: 376
		private Service1.ProcessInfo[] processinfo = new Service1.ProcessInfo[10000];

		// Token: 0x04000179 RID: 377
		private Service1.Node[] max_ipc_queue = new Service1.Node[32];

		// Token: 0x0400017A RID: 378
		private Service1.Node[] max_util_queue = new Service1.Node[32];

		// Token: 0x0400017B RID: 379
		private Service1.Node[] wait_core = new Service1.Node[32];

		// Token: 0x0400017C RID: 380
		private Service1.Node2[] sched_queue_b2l = new Service1.Node2[64];

		// Token: 0x0400017D RID: 381
		private Service1.Node2[] sched_queue_l2b = new Service1.Node2[64];

		// Token: 0x0400017E RID: 382
		private Service1.Node2[] compare = new Service1.Node2[64];

		// Token: 0x0400017F RID: 383
		private Service1.Node2[] compare_final = new Service1.Node2[64];

		// Token: 0x04000180 RID: 384
		private Service1.Node2 schedule_queue = new Service1.Node2();

		// Token: 0x04000181 RID: 385
		private Service1.Node2 schedule_queue_little = new Service1.Node2();

		// Token: 0x04000182 RID: 386
		private Service1.Node2 schd_queue_b2l = new Service1.Node2();

		// Token: 0x04000183 RID: 387
		private Service1.Node2 schd_queue_b2s = new Service1.Node2();

		// Token: 0x04000184 RID: 388
		private Service1.Node2 schd_queue_l2b = new Service1.Node2();

		// Token: 0x04000185 RID: 389
		private Service1.Node2 schd_queue_s2b = new Service1.Node2();

		// Token: 0x04000186 RID: 390
		public long[] lowerlimit = new long[32];

		// Token: 0x04000187 RID: 391
		public long[] upperlimit = new long[32];

		// Token: 0x04000188 RID: 392
		private Guid powerscheme1 = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

		// Token: 0x04000189 RID: 393
		private Guid powerscheme = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

		// Token: 0x0400018A RID: 394
		public int node_cap = 500;

		// Token: 0x0400018B RID: 395
		public long num_chain;

		// Token: 0x0400018C RID: 396
		public long num_chain_little;

		// Token: 0x0400018D RID: 397
		public long num_chain_big;

		// Token: 0x0400018E RID: 398
		public long num_chain2;

		// Token: 0x0400018F RID: 399
		public long action_recored;

		// Token: 0x04000190 RID: 400
		public long[] current_freq = new long[32];

		// Token: 0x04000191 RID: 401
		public uint affinitymask;

		// Token: 0x04000192 RID: 402
		public uint affinitymask_little;

		// Token: 0x04000193 RID: 403
		public uint affinitymask_little_p;

		// Token: 0x04000194 RID: 404
		public uint affinitymask_big;

		// Token: 0x04000195 RID: 405
		public uint affinitymask_big_phyx;

		// Token: 0x04000196 RID: 406
		public uint affinitymask_big_smt;

		// Token: 0x04000197 RID: 407
		public uint affinitymask_fake_little;

		// Token: 0x04000198 RID: 408
		public uint affinitymask_all;

		// Token: 0x04000199 RID: 409
		private string number_of_cores;

		// Token: 0x0400019A RID: 410
		private string NumberOfLogicalProcessors;

		// Token: 0x0400019B RID: 411
		public uint eax;

		// Token: 0x0400019C RID: 412
		public uint edx;

		// Token: 0x0400019D RID: 413
		public uint indices;

		// Token: 0x0400019E RID: 414
		public long[] tsc_e = new long[32];

		// Token: 0x0400019F RID: 415
		public long[] tsc_l = new long[32];

		// Token: 0x040001A0 RID: 416
		public long[] tsc = new long[32];

		// Token: 0x040001A1 RID: 417
		public long[] tsc_total = new long[32];

		// Token: 0x040001A2 RID: 418
		public long[] result_ins_e = new long[32];

		// Token: 0x040001A3 RID: 419
		public long[] result_ins_l = new long[32];

		// Token: 0x040001A4 RID: 420
		public long[] result_ins = new long[32];

		// Token: 0x040001A5 RID: 421
		public long[] result_ins_comp_e = new long[32];

		// Token: 0x040001A6 RID: 422
		public long[] result_ins_comp_l = new long[32];

		// Token: 0x040001A7 RID: 423
		public long[] result_ins_comp = new long[32];

		// Token: 0x040001A8 RID: 424
		public long max_single_ratio_big;

		// Token: 0x040001A9 RID: 425
		public long max_single_ratio_little;

		// Token: 0x040001AA RID: 426
		public long max_ins_little;

		// Token: 0x040001AB RID: 427
		public long max_ins_big;

		// Token: 0x040001AC RID: 428
		public long max_br_little;

		// Token: 0x040001AD RID: 429
		public long max_br_far_little;

		// Token: 0x040001AE RID: 430
		public long max_br_big;

		// Token: 0x040001AF RID: 431
		public long max_br_far_big;

		// Token: 0x040001B0 RID: 432
		public long max_util_big = 50L;

		// Token: 0x040001B1 RID: 433
		public long[] single_tag = new long[32];

		// Token: 0x040001B2 RID: 434
		public long[] result_br_miss_e = new long[32];

		// Token: 0x040001B3 RID: 435
		public long[] result_br_miss_l = new long[32];

		// Token: 0x040001B4 RID: 436
		public long[] result_cache_e = new long[32];

		// Token: 0x040001B5 RID: 437
		public long[] result_cache_l = new long[32];

		// Token: 0x040001B6 RID: 438
		public long[] result_cache = new long[32];

		// Token: 0x040001B7 RID: 439
		public long[] result_load_e = new long[32];

		// Token: 0x040001B8 RID: 440
		public long[] result_load_l = new long[32];

		// Token: 0x040001B9 RID: 441
		public long[] result_load = new long[32];

		// Token: 0x040001BA RID: 442
		public long[] result_store_e = new long[32];

		// Token: 0x040001BB RID: 443
		public long[] result_store_l = new long[32];

		// Token: 0x040001BC RID: 444
		public long[] result_store = new long[32];

		// Token: 0x040001BD RID: 445
		public long[] result_load_l1_e = new long[32];

		// Token: 0x040001BE RID: 446
		public long[] result_load_l1_l = new long[32];

		// Token: 0x040001BF RID: 447
		public long[] result_load_l1 = new long[32];

		// Token: 0x040001C0 RID: 448
		public long[] result_br_ins_e = new long[32];

		// Token: 0x040001C1 RID: 449
		public long[] result_br_ins_l = new long[32];

		// Token: 0x040001C2 RID: 450
		public long[] result_br_ins = new long[32];

		// Token: 0x040001C3 RID: 451
		public long[] result_br_indirect_e = new long[32];

		// Token: 0x040001C4 RID: 452
		public long[] result_br_indirect_l = new long[32];

		// Token: 0x040001C5 RID: 453
		public long[] result_br_indirect = new long[32];

		// Token: 0x040001C6 RID: 454
		public long[] br_indirect = new long[32];

		// Token: 0x040001C7 RID: 455
		public long[] result_br_far_e = new long[32];

		// Token: 0x040001C8 RID: 456
		public long[] result_br_far_l = new long[32];

		// Token: 0x040001C9 RID: 457
		public long[] result_br_far = new long[32];

		// Token: 0x040001CA RID: 458
		public long[] br_far = new long[32];

		// Token: 0x040001CB RID: 459
		public long[] result_aclk_e = new long[32];

		// Token: 0x040001CC RID: 460
		public long[] result_aclk_l = new long[32];

		// Token: 0x040001CD RID: 461
		public long[] result_aclk = new long[32];

		// Token: 0x040001CE RID: 462
		public long[] result_mclk_e = new long[32];

		// Token: 0x040001CF RID: 463
		public long[] result_mclk_l = new long[32];

		// Token: 0x040001D0 RID: 464
		public long[] result_mclk = new long[32];

		// Token: 0x040001D1 RID: 465
		public long[] result_pclk_e = new long[32];

		// Token: 0x040001D2 RID: 466
		public long[] result_pclk_l = new long[32];

		// Token: 0x040001D3 RID: 467
		public long[] result_pclk = new long[32];

		// Token: 0x040001D4 RID: 468
		private Ols myOls = new Ols();

		// Token: 0x040001D5 RID: 469
		public long ipc_switch;

		// Token: 0x040001D6 RID: 470
		public long active_cores;

		// Token: 0x040001D7 RID: 471
		public long[] core_active = new long[32];

		// Token: 0x040001D8 RID: 472
		public long active_big_cores;

		// Token: 0x040001D9 RID: 473
		public long active_smt_cores;

		// Token: 0x040001DA RID: 474
		public long active_little_cores;

		// Token: 0x040001DB RID: 475
		public long[] single_ratio = new long[32];

		// Token: 0x040001DC RID: 476
		public long[] ht_share = new long[32];

		// Token: 0x040001DD RID: 477
		public long[] br_far_ratio = new long[32];

		// Token: 0x040001DE RID: 478
		public long[] br = new long[32];

		// Token: 0x040001DF RID: 479
		public long[] br_miss = new long[32];

		// Token: 0x040001E0 RID: 480
		public long[] cache = new long[32];

		// Token: 0x040001E1 RID: 481
		public long[] mem = new long[32];

		// Token: 0x040001E2 RID: 482
		public long[] load = new long[32];

		// Token: 0x040001E3 RID: 483
		public long[] load_l1 = new long[32];

		// Token: 0x040001E4 RID: 484
		public long[] load_l2 = new long[32];

		// Token: 0x040001E5 RID: 485
		public long[] load_l3 = new long[32];

		// Token: 0x040001E6 RID: 486
		public long[] load_dram = new long[32];

		// Token: 0x040001E7 RID: 487
		public long[] cache2mem = new long[32];

		// Token: 0x040001E8 RID: 488
		public long[] ins = new long[32];

		// Token: 0x040001E9 RID: 489
		public long util_big;

		// Token: 0x040001EA RID: 490
		public long ins_all;

		// Token: 0x040001EB RID: 491
		public long ins_all_avg;

		// Token: 0x040001EC RID: 492
		public long ins_all_whole;

		// Token: 0x040001ED RID: 493
		public long ins_all_whole_sqr;

		// Token: 0x040001EE RID: 494
		public long ins_all_whole_avg;

		// Token: 0x040001EF RID: 495
		public long perf_whole;

		// Token: 0x040001F0 RID: 496
		public long perf_whole_old;

		// Token: 0x040001F1 RID: 497
		public long perf_whole_avg;

		// Token: 0x040001F2 RID: 498
		public long ins_avg;

		// Token: 0x040001F3 RID: 499
		public long ins_sqr;

		// Token: 0x040001F4 RID: 500
		public long ins_indicator;

		// Token: 0x040001F5 RID: 501
		public long ins_big;

		// Token: 0x040001F6 RID: 502
		public long ins_big_perm;

		// Token: 0x040001F7 RID: 503
		public long ins_constr_smt;

		// Token: 0x040001F8 RID: 504
		public long ins_little;

		// Token: 0x040001F9 RID: 505
		public long ins_little_perm;

		// Token: 0x040001FA RID: 506
		public long ins_little_comp;

		// Token: 0x040001FB RID: 507
		public long ins_ratio11_perm;

		// Token: 0x040001FC RID: 508
		public long ins_ratio11;

		// Token: 0x040001FD RID: 509
		public long ins_max_comp;

		// Token: 0x040001FE RID: 510
		public long ins_max_load;

		// Token: 0x040001FF RID: 511
		public long ins_max_br;

		// Token: 0x04000200 RID: 512
		public long ins_max;

		// Token: 0x04000201 RID: 513
		public long util_little_all;

		// Token: 0x04000202 RID: 514
		public long aclk_acc;

		// Token: 0x04000203 RID: 515
		public long ins_smt;

		// Token: 0x04000204 RID: 516
		public long ins_little_raw;

		// Token: 0x04000205 RID: 517
		public long ins_big_raw;

		// Token: 0x04000206 RID: 518
		public long ins_smt_raw;

		// Token: 0x04000207 RID: 519
		public long max_ipc;

		// Token: 0x04000208 RID: 520
		private int little_num;

		// Token: 0x04000209 RID: 521
		private int big_num;

		// Token: 0x0400020A RID: 522
		private int core_num;

		// Token: 0x0400020B RID: 523
		private long threshold;

		// Token: 0x0400020C RID: 524
		private long[] datetime_new = new long[32];

		// Token: 0x0400020D RID: 525
		private long[] datetime_old = new long[32];

		// Token: 0x0400020E RID: 526
		private long[] datetime_elapsed = new long[32];

		// Token: 0x0400020F RID: 527
		private long datetime_trigger;

		// Token: 0x04000210 RID: 528
		private long datetime_trigger_little;

		// Token: 0x04000211 RID: 529
		private long datetime_trigger_exchange;

		// Token: 0x04000212 RID: 530
		private long avg_ipc_trigger = 1L;

		// Token: 0x04000213 RID: 531
		private int e_core_position;

		// Token: 0x04000214 RID: 532
		private int currentprocnum_index;

		// Token: 0x04000215 RID: 533
		private long[] count_stat_little = new long[32];

		// Token: 0x04000216 RID: 534
		private long counter_sys;

		// Token: 0x04000217 RID: 535
		private long count_stat;

		// Token: 0x04000218 RID: 536
		private long count_stat1;

		// Token: 0x04000219 RID: 537
		private long count_stat2;

		// Token: 0x0400021A RID: 538
		private long count_stat3;

		// Token: 0x0400021B RID: 539
		private long count_stat4;

		// Token: 0x0400021C RID: 540
		private long count_stat5;

		// Token: 0x0400021D RID: 541
		private long count_stat6;

		// Token: 0x0400021E RID: 542
		private long count_stat7;

		// Token: 0x0400021F RID: 543
		private long counter_action;

		// Token: 0x04000220 RID: 544
		private long counter_action_switch;

		// Token: 0x04000221 RID: 545
		private long[] acc_instruction = new long[32];

		// Token: 0x04000222 RID: 546
		private long[] acc_aclk = new long[32];

		// Token: 0x04000223 RID: 547
		private long[] acc_instruction_comp = new long[32];

		// Token: 0x04000224 RID: 548
		private long[] acc_load = new long[32];

		// Token: 0x04000225 RID: 549
		private long[] acc_datetime = new long[32];

		// Token: 0x04000226 RID: 550
		private long[] acc_instruction1 = new long[32];

		// Token: 0x04000227 RID: 551
		private long[] acc_aclk1 = new long[32];

		// Token: 0x04000228 RID: 552
		private long[] acc_instruction_comp1 = new long[32];

		// Token: 0x04000229 RID: 553
		private long[] acc_load1 = new long[32];

		// Token: 0x0400022A RID: 554
		private long[] acc_datetime1 = new long[32];

		// Token: 0x0400022B RID: 555
		private long[] util = new long[32];

		// Token: 0x0400022C RID: 556
		private long cnt_findnode;

		// Token: 0x0400022D RID: 557
		private long cnt_not_findnode;

		// Token: 0x0400022E RID: 558
		private int switch_to_big;

		// Token: 0x0400022F RID: 559
		private int switch_to_big_cnt;

		// Token: 0x04000230 RID: 560
		private int[] oldthread_waittime = new int[32];

		// Token: 0x04000231 RID: 561
		private int[] schedule_thread = new int[32];

		// Token: 0x04000232 RID: 562
		private int[] max_ipc_thread = new int[32];

		// Token: 0x04000233 RID: 563
		private int[] max_util_thread = new int[32];

		// Token: 0x04000234 RID: 564
		private int[] max_util_little = new int[32];

		// Token: 0x04000235 RID: 565
		private int num_queue = 1;

		// Token: 0x04000236 RID: 566
		private long dummy;

		// Token: 0x04000237 RID: 567
		private int currentthread;

		// Token: 0x04000238 RID: 568
		private int currentprocess;

		// Token: 0x04000239 RID: 569
		private int counter1;

		// Token: 0x0400023A RID: 570
		private int counter2;

		// Token: 0x0400023B RID: 571
		private int counter3;

		// Token: 0x0400023C RID: 572
		private int[] findrecord = new int[32];

		// Token: 0x0400023D RID: 573
		private UIntPtr j;

		// Token: 0x0400023E RID: 574
		private uint mask;

		// Token: 0x0400023F RID: 575
		private uint valueToSet;

		// Token: 0x04000240 RID: 576
		private long acc_util;

		// Token: 0x04000241 RID: 577
		private uint ratio;

		// Token: 0x04000242 RID: 578
		private string ratio_string;

		// Token: 0x04000243 RID: 579
		private uint ratio1;

		// Token: 0x04000244 RID: 580
		private string ratio_string1;

		// Token: 0x04000245 RID: 581
		private long ipc_big_sum;

		// Token: 0x04000246 RID: 582
		private long ipc_little_sum;

		// Token: 0x04000247 RID: 583
		private long ipc_big_count;

		// Token: 0x04000248 RID: 584
		private long ipc_little_count;

		// Token: 0x04000249 RID: 585
		private long ipc_big_avg;

		// Token: 0x0400024A RID: 586
		private long ipc_little_avg;

		// Token: 0x0400024B RID: 587
		private long eff_big_sum;

		// Token: 0x0400024C RID: 588
		private long eff_little_sum;

		// Token: 0x0400024D RID: 589
		private long eff_big_count;

		// Token: 0x0400024E RID: 590
		private long eff_little_count;

		// Token: 0x0400024F RID: 591
		private long eff_big_avg;

		// Token: 0x04000250 RID: 592
		private long eff_little_avg;

		// Token: 0x04000251 RID: 593
		private long ins_big_sum;

		// Token: 0x04000252 RID: 594
		private long ins_little_sum;

		// Token: 0x04000253 RID: 595
		private long ins_big_count;

		// Token: 0x04000254 RID: 596
		private long ins_little_count;

		// Token: 0x04000255 RID: 597
		private long ins_big_avg;

		// Token: 0x04000256 RID: 598
		private long ins_little_avg;

		// Token: 0x04000257 RID: 599
		private long[] ins_total = new long[32];

		// Token: 0x04000258 RID: 600
		private long[] store_total = new long[32];

		// Token: 0x04000259 RID: 601
		private long[] count_total = new long[32];

		// Token: 0x0400025A RID: 602
		private long[] intval = new long[32];

		// Token: 0x0400025B RID: 603
		private long[] nonstore_store_ratio = new long[32];

		// Token: 0x0400025C RID: 604
		private long[] usr_sum = new long[32];

		// Token: 0x0400025D RID: 605
		private long[] usr_count = new long[32];

		// Token: 0x0400025E RID: 606
		private long[] usr_ratio = new long[32];

		// Token: 0x0400025F RID: 607
		private long[] residence_p = new long[32];

		// Token: 0x04000260 RID: 608
		private long[] residence_p1 = new long[32];

		// Token: 0x04000261 RID: 609
		private long[] acc_instruction_b = new long[32];

		// Token: 0x04000262 RID: 610
		private long[] acc_aclk_b = new long[32];

		// Token: 0x04000263 RID: 611
		private long[] acc_load_b = new long[32];

		// Token: 0x04000264 RID: 612
		private long[] acc_store_b = new long[32];

		// Token: 0x04000265 RID: 613
		private long[] acc_load_miss_b = new long[32];

		// Token: 0x04000266 RID: 614
		private long[] acc_br_b = new long[32];

		// Token: 0x04000267 RID: 615
		private long[] acc_runtime_b = new long[32];

		// Token: 0x04000268 RID: 616
		private long[] cnt_b = new long[32];

		// Token: 0x04000269 RID: 617
		private long[] acc_instruction_l = new long[32];

		// Token: 0x0400026A RID: 618
		private long[] acc_aclk_l = new long[32];

		// Token: 0x0400026B RID: 619
		private long[] acc_load_l = new long[32];

		// Token: 0x0400026C RID: 620
		private long[] acc_load_l_perm = new long[32];

		// Token: 0x0400026D RID: 621
		private long[] last_duration = new long[32];

		// Token: 0x0400026E RID: 622
		private long[] now_duration = new long[32];

		// Token: 0x0400026F RID: 623
		private long[] acc_store_l = new long[32];

		// Token: 0x04000270 RID: 624
		private long[] acc_store_l_perm = new long[32];

		// Token: 0x04000271 RID: 625
		private long[] acc_load_miss_l = new long[32];

		// Token: 0x04000272 RID: 626
		private long[] acc_br_l = new long[32];

		// Token: 0x04000273 RID: 627
		private long[] acc_runtime_l = new long[32];

		// Token: 0x04000274 RID: 628
		private long[] cnt_l = new long[32];

		// Token: 0x04000275 RID: 629
		private long[] ipc_b = new long[32];

		// Token: 0x04000276 RID: 630
		private long[] ipc_b_temp = new long[32];

		// Token: 0x04000277 RID: 631
		private long[] max_ipc_b = new long[32];

		// Token: 0x04000278 RID: 632
		private long[] max_ins = new long[32];

		// Token: 0x04000279 RID: 633
		private long[] ipc_l = new long[32];

		// Token: 0x0400027A RID: 634
		private long[] ipc_l_perm = new long[32];

		// Token: 0x0400027B RID: 635
		private long[] max_ipc_l = new long[32];

		// Token: 0x0400027C RID: 636
		private long max_ipc_little;

		// Token: 0x0400027D RID: 637
		private long max_ipc_big;

		// Token: 0x0400027E RID: 638
		private long[] ipc_ratio = new long[32];

		// Token: 0x0400027F RID: 639
		private long[] br_ratio = new long[32];

		// Token: 0x04000280 RID: 640
		private long[] ipc_ratio_temp = new long[32];

		// Token: 0x04000281 RID: 641
		private long[] br_ratio_temp = new long[32];

		// Token: 0x04000282 RID: 642
		private long br_ratio_square;

		// Token: 0x04000283 RID: 643
		private long br_ratio_square_bar;

		// Token: 0x04000284 RID: 644
		private long br_ratio_square_e;

		// Token: 0x04000285 RID: 645
		private long br_ratio_square_count;

		// Token: 0x04000286 RID: 646
		private long ipc_square;

		// Token: 0x04000287 RID: 647
		private long ipc_square_bar;

		// Token: 0x04000288 RID: 648
		private long ipc_square_e;

		// Token: 0x04000289 RID: 649
		private long ipc_square_count;

		// Token: 0x0400028A RID: 650
		private long[] br_load_ratio = new long[32];

		// Token: 0x0400028B RID: 651
		private long[] br_load_ratio_temp = new long[32];

		// Token: 0x0400028C RID: 652
		private long[] load_miss_ratio_b = new long[32];

		// Token: 0x0400028D RID: 653
		private long[] load_miss_ratio_b_temp = new long[32];

		// Token: 0x0400028E RID: 654
		private long[] min_load_miss_ratio_b = new long[32];

		// Token: 0x0400028F RID: 655
		private long[] load_miss_ratio_l = new long[32];

		// Token: 0x04000290 RID: 656
		private long[] avg_runtime_b = new long[32];

		// Token: 0x04000291 RID: 657
		private long[] avg_runtime_l = new long[32];

		// Token: 0x04000292 RID: 658
		private long[] avg_freq_b = new long[32];

		// Token: 0x04000293 RID: 659
		private long[] max_freq_b = new long[32];

		// Token: 0x04000294 RID: 660
		private long[] avg_freq_l = new long[32];

		// Token: 0x04000295 RID: 661
		private long[] lock_data = new long[32];

		// Token: 0x04000296 RID: 662
		private long[] tag = new long[32];

		// Token: 0x04000297 RID: 663
		private long[] duration = new long[32];

		// Token: 0x04000298 RID: 664
		private long[] reset_count = new long[32];

		// Token: 0x04000299 RID: 665
		private uint[] affinity = new uint[32];

		// Token: 0x0400029A RID: 666
		private long[] residence = new long[32];

		// Token: 0x0400029B RID: 667
		private long[] acc_instruction_b1 = new long[32];

		// Token: 0x0400029C RID: 668
		private long[] acc_aclk_b1 = new long[32];

		// Token: 0x0400029D RID: 669
		private long[] acc_load_b1 = new long[32];

		// Token: 0x0400029E RID: 670
		private long[] acc_store_b1 = new long[32];

		// Token: 0x0400029F RID: 671
		private long[] acc_load_miss_b1 = new long[32];

		// Token: 0x040002A0 RID: 672
		private long[] acc_br_b1 = new long[32];

		// Token: 0x040002A1 RID: 673
		private long[] acc_runtime_b1 = new long[32];

		// Token: 0x040002A2 RID: 674
		private long[] cnt_b1 = new long[32];

		// Token: 0x040002A3 RID: 675
		private long[] acc_instruction_l1 = new long[32];

		// Token: 0x040002A4 RID: 676
		private long[] acc_aclk_l1 = new long[32];

		// Token: 0x040002A5 RID: 677
		private long[] acc_load_l1 = new long[32];

		// Token: 0x040002A6 RID: 678
		private long[] acc_load_l1_perm = new long[32];

		// Token: 0x040002A7 RID: 679
		private long[] last_duration1 = new long[32];

		// Token: 0x040002A8 RID: 680
		private long[] now_duration1 = new long[32];

		// Token: 0x040002A9 RID: 681
		private long[] acc_store_l1 = new long[32];

		// Token: 0x040002AA RID: 682
		private long[] acc_store_l1_perm = new long[32];

		// Token: 0x040002AB RID: 683
		private long[] acc_load_miss_l1 = new long[32];

		// Token: 0x040002AC RID: 684
		private long[] acc_br_l1 = new long[32];

		// Token: 0x040002AD RID: 685
		private long[] acc_runtime_l1 = new long[32];

		// Token: 0x040002AE RID: 686
		private long[] cnt_l1 = new long[32];

		// Token: 0x040002AF RID: 687
		private long[] ipc_b1 = new long[32];

		// Token: 0x040002B0 RID: 688
		private long[] max_ipc_b1 = new long[32];

		// Token: 0x040002B1 RID: 689
		private long[] max_ins1 = new long[32];

		// Token: 0x040002B2 RID: 690
		private long[] ipc_l1 = new long[32];

		// Token: 0x040002B3 RID: 691
		private long[] ipc_l1_perm = new long[32];

		// Token: 0x040002B4 RID: 692
		private long[] max_ipc_l1 = new long[32];

		// Token: 0x040002B5 RID: 693
		private long[] ipc_ratio1 = new long[32];

		// Token: 0x040002B6 RID: 694
		private long[] br_ratio1 = new long[32];

		// Token: 0x040002B7 RID: 695
		private long[] br_load_ratio1 = new long[32];

		// Token: 0x040002B8 RID: 696
		private long[] load_miss_ratio_b1 = new long[32];

		// Token: 0x040002B9 RID: 697
		private long acc_instruction_b1_t;

		// Token: 0x040002BA RID: 698
		private long acc_aclk_b1_t;

		// Token: 0x040002BB RID: 699
		private long acc_load_b1_t;

		// Token: 0x040002BC RID: 700
		private long acc_store_b1_t;

		// Token: 0x040002BD RID: 701
		private long acc_load_miss_b1_t;

		// Token: 0x040002BE RID: 702
		private long acc_br_b1_t;

		// Token: 0x040002BF RID: 703
		private long acc_runtime_b1_t;

		// Token: 0x040002C0 RID: 704
		private long cnt_b1_t;

		// Token: 0x040002C1 RID: 705
		private long acc_instruction_l1_t;

		// Token: 0x040002C2 RID: 706
		private long acc_aclk_l1_t;

		// Token: 0x040002C3 RID: 707
		private long acc_load_l1_t;

		// Token: 0x040002C4 RID: 708
		private long acc_load_l1_perm_t;

		// Token: 0x040002C5 RID: 709
		private long acc_store_l1_t;

		// Token: 0x040002C6 RID: 710
		private long acc_store_l1_perm_t;

		// Token: 0x040002C7 RID: 711
		private long acc_load_miss_l1_t;

		// Token: 0x040002C8 RID: 712
		private long acc_br_l1_t;

		// Token: 0x040002C9 RID: 713
		private long acc_runtime_l1_t;

		// Token: 0x040002CA RID: 714
		private long cnt_l1_t;

		// Token: 0x040002CB RID: 715
		private long ipc_b1_t;

		// Token: 0x040002CC RID: 716
		private long max_ipc_b1_t;

		// Token: 0x040002CD RID: 717
		private long ipc_l1_t;

		// Token: 0x040002CE RID: 718
		private long ipc_l1_perm_t;

		// Token: 0x040002CF RID: 719
		private long max_ipc_l1_t;

		// Token: 0x040002D0 RID: 720
		private long ipc_ratio1_t;

		// Token: 0x040002D1 RID: 721
		private long br_ratio1_t;

		// Token: 0x040002D2 RID: 722
		private long br_load_ratio1_t;

		// Token: 0x040002D3 RID: 723
		private long load_miss_ratio_b1_t;

		// Token: 0x040002D4 RID: 724
		private long min_load_miss_ratio_b1_t;

		// Token: 0x040002D5 RID: 725
		private long load_miss_ratio_l1_t;

		// Token: 0x040002D6 RID: 726
		private long avg_runtime_b1_t;

		// Token: 0x040002D7 RID: 727
		private long avg_runtime_l1_t;

		// Token: 0x040002D8 RID: 728
		private long avg_freq_b1_t;

		// Token: 0x040002D9 RID: 729
		private long avg_freq_l1_t;

		// Token: 0x040002DA RID: 730
		private long max_ins_t;

		// Token: 0x040002DB RID: 731
		private long lock_data1_t;

		// Token: 0x040002DC RID: 732
		private long tag1_t;

		// Token: 0x040002DD RID: 733
		private long duration1_t;

		// Token: 0x040002DE RID: 734
		private long reset_count1_t;

		// Token: 0x040002DF RID: 735
		private uint affinity1_t;

		// Token: 0x040002E0 RID: 736
		private long[] temp1 = new long[32];

		// Token: 0x040002E1 RID: 737
		private long[] temp2 = new long[32];

		// Token: 0x040002E2 RID: 738
		private long[] temp3 = new long[32];

		// Token: 0x040002E3 RID: 739
		private long[] temp4 = new long[32];

		// Token: 0x040002E4 RID: 740
		private long[] temp41 = new long[32];

		// Token: 0x040002E5 RID: 741
		private long[] temp5 = new long[32];

		// Token: 0x040002E6 RID: 742
		private long[] temp51 = new long[32];

		// Token: 0x040002E7 RID: 743
		private long[] temp6 = new long[32];

		// Token: 0x040002E8 RID: 744
		private long[] temp_ticks = new long[32];

		// Token: 0x040002E9 RID: 745
		private long tempp;

		// Token: 0x040002EA RID: 746
		private long tempk;

		// Token: 0x040002EB RID: 747
		private long tempj;

		// Token: 0x040002EC RID: 748
		private long templ;

		// Token: 0x040002ED RID: 749
		private long artif;

		// Token: 0x040002EE RID: 750
		private long neuro;

		// Token: 0x040002EF RID: 751
		private long[] sched_ratio = new long[32];

		// Token: 0x040002F0 RID: 752
		private long[] ins_ratio = new long[32];

		// Token: 0x040002F1 RID: 753
		private long avg_comp_ldst_ratio;

		// Token: 0x040002F2 RID: 754
		private long avg_comp_ldst_sum;

		// Token: 0x040002F3 RID: 755
		private long avg_comp_ldst_count;

		// Token: 0x040002F4 RID: 756
		private long avg_comp_br_ratio;

		// Token: 0x040002F5 RID: 757
		private long avg_comp_br_sum;

		// Token: 0x040002F6 RID: 758
		private long avg_comp_br_count;

		// Token: 0x040002F7 RID: 759
		private long avg_ipc_ratio_sum;

		// Token: 0x040002F8 RID: 760
		private long avg_ipc_ratio_count;

		// Token: 0x040002F9 RID: 761
		private long avg_ipc_ratio;

		// Token: 0x040002FA RID: 762
		private long[] min_load_miss_ratio_b1 = new long[32];

		// Token: 0x040002FB RID: 763
		private long[] load_miss_ratio_l1 = new long[32];

		// Token: 0x040002FC RID: 764
		private long[] avg_runtime_b1 = new long[32];

		// Token: 0x040002FD RID: 765
		private long[] avg_runtime_l1 = new long[32];

		// Token: 0x040002FE RID: 766
		private long[] avg_freq_b1 = new long[32];

		// Token: 0x040002FF RID: 767
		private long[] avg_freq_l1 = new long[32];

		// Token: 0x04000300 RID: 768
		private long[] lock_data1 = new long[32];

		// Token: 0x04000301 RID: 769
		private long[] tag1 = new long[32];

		// Token: 0x04000302 RID: 770
		private long[] duration1 = new long[32];

		// Token: 0x04000303 RID: 771
		private long[] reset_count1 = new long[32];

		// Token: 0x04000304 RID: 772
		private uint[] affinity1 = new uint[32];

		// Token: 0x04000305 RID: 773
		private long[] residence1 = new long[32];

		// Token: 0x04000306 RID: 774
		private long[] prev_tag = new long[32];

		// Token: 0x04000307 RID: 775
		private uint[] prev_affinity = new uint[32];

		// Token: 0x04000308 RID: 776
		private long count_fast_ipc;

		// Token: 0x04000309 RID: 777
		private long count_fast_br;

		// Token: 0x0400030A RID: 778
		private long count_fast_comp;

		// Token: 0x0400030B RID: 779
		private long count_slow;

		// Token: 0x0400030C RID: 780
		private long count_heavy;

		// Token: 0x0400030D RID: 781
		private long _6_to_2;

		// Token: 0x0400030E RID: 782
		private long _6_to_1;

		// Token: 0x0400030F RID: 783
		private long _2_to_6;

		// Token: 0x04000310 RID: 784
		private long _6_to_8;

		// Token: 0x04000311 RID: 785
		private long _8_to_6;

		// Token: 0x04000312 RID: 786
		private long type0;

		// Token: 0x04000313 RID: 787
		private long type10;

		// Token: 0x04000314 RID: 788
		private long type1;

		// Token: 0x04000315 RID: 789
		private long type2;

		// Token: 0x04000316 RID: 790
		private long type3;

		// Token: 0x04000317 RID: 791
		private long type4;

		// Token: 0x04000318 RID: 792
		private long type5;

		// Token: 0x04000319 RID: 793
		private long type6;

		// Token: 0x0400031A RID: 794
		private long type7;

		// Token: 0x0400031B RID: 795
		private long type8;

		// Token: 0x0400031C RID: 796
		private long neuro_count;

		// Token: 0x0400031D RID: 797
		private long count_threads;

		// Token: 0x0400031E RID: 798
		private long count_stay_big;

		// Token: 0x0400031F RID: 799
		private int config;

		// Token: 0x04000320 RID: 800
		private int gamemode;

		// Token: 0x04000321 RID: 801
		private long[] core_availability_cnt = new long[32];

		// Token: 0x04000322 RID: 802
		private long[] test_ratio = new long[32];

		// Token: 0x04000323 RID: 803
		private long[] value = new long[32];

		// Token: 0x04000324 RID: 804
		private long max_freq;

		// Token: 0x04000325 RID: 805
		private long insthres;

		// Token: 0x04000326 RID: 806
		private long insthres1;

		// Token: 0x04000327 RID: 807
		private long insthres_lower;

		// Token: 0x04000328 RID: 808
		private long[] exclude_b = new long[32];

		// Token: 0x04000329 RID: 809
		private long[] exclude = new long[32];

		// Token: 0x0400032A RID: 810
		private long[] exclude_all = new long[32];

		// Token: 0x0400032B RID: 811
		private long[] allow_exclude = new long[32];

		// Token: 0x0400032C RID: 812
		private long[] exclude1 = new long[32];

		// Token: 0x0400032D RID: 813
		private long[] exclude_all1 = new long[32];

		// Token: 0x0400032E RID: 814
		private long[] allow_exclude1 = new long[32];

		// Token: 0x0400032F RID: 815
		private long avg_ipc;

		// Token: 0x04000330 RID: 816
		private long acc_ins;

		// Token: 0x04000331 RID: 817
		private long acc_loads;

		// Token: 0x04000332 RID: 818
		private long acc_loads_e;

		// Token: 0x04000333 RID: 819
		private long acc_loads_miss;

		// Token: 0x04000334 RID: 820
		private long acc_loads_miss_e;

		// Token: 0x04000335 RID: 821
		private long acc_brs;

		// Token: 0x04000336 RID: 822
		private long acc_brs_e;

		// Token: 0x04000337 RID: 823
		private long acc_brs_miss;

		// Token: 0x04000338 RID: 824
		private long acc_brs_miss_e;

		// Token: 0x04000339 RID: 825
		private long acc_aclks;

		// Token: 0x0400033A RID: 826
		private long acc_ins_e;

		// Token: 0x0400033B RID: 827
		private long acc_aclks1_e;

		// Token: 0x0400033C RID: 828
		private long acc_aclks_e;

		// Token: 0x0400033D RID: 829
		private long avg_diff;

		// Token: 0x0400033E RID: 830
		private long acc_aclks1;

		// Token: 0x0400033F RID: 831
		private long acc_date;

		// Token: 0x04000340 RID: 832
		private long start;

		// Token: 0x04000341 RID: 833
		private long numberofchain;

		// Token: 0x04000342 RID: 834
		private long acc_ins_b;

		// Token: 0x04000343 RID: 835
		private long acc_ins_l;

		// Token: 0x04000344 RID: 836
		private long acc_ack_b;

		// Token: 0x04000345 RID: 837
		private long acc_ack_l;

		// Token: 0x04000346 RID: 838
		private long avg_ipc_b;

		// Token: 0x04000347 RID: 839
		private long avg_ipc_l;

		// Token: 0x04000348 RID: 840
		private long avg_ipc_ratio_bak;

		// Token: 0x04000349 RID: 841
		private long acc_br_all;

		// Token: 0x0400034A RID: 842
		private long acc_cond_br_all;

		// Token: 0x0400034B RID: 843
		private long avg_cond_br_ratio;

		// Token: 0x0400034C RID: 844
		private long min_cond_br_ratio = 100L;

		// Token: 0x0400034D RID: 845
		private long max_cond_br_ratio;

		// Token: 0x0400034E RID: 846
		private long[] count_intval = new long[32];

		// Token: 0x0400034F RID: 847
		private long count_intval_all;

		// Token: 0x04000350 RID: 848
		private long count_intval_avg;

		// Token: 0x04000351 RID: 849
		private long max_ipc_global;

		// Token: 0x04000352 RID: 850
		private int[] currentprocessor = new int[32];

		// Token: 0x04000353 RID: 851
		private long total_aclks;

		// Token: 0x04000354 RID: 852
		private long total_ins;

		// Token: 0x04000355 RID: 853
		private long total_ins1;

		// Token: 0x04000356 RID: 854
		private long total_aclks1;

		// Token: 0x04000357 RID: 855
		private uint eeax;

		// Token: 0x04000358 RID: 856
		private uint eebx;

		// Token: 0x04000359 RID: 857
		private uint eecx;

		// Token: 0x0400035A RID: 858
		private uint eedx;

		// Token: 0x0400035B RID: 859
		private uint e_msr;

		// Token: 0x0400035C RID: 860
		private uint l_msr;

		// Token: 0x0400035D RID: 861
		private uint l_max_freq;

		// Token: 0x0400035E RID: 862
		private uint max_msr;

		// Token: 0x0400035F RID: 863
		private int[] thread_priority = new int[32];

		// Token: 0x04000360 RID: 864
		private int[] process_priority = new int[32];

		// Token: 0x04000361 RID: 865
		private int[] group_num = new int[32];

		// Token: 0x04000362 RID: 866
		public Service1.ThreadInfo[] findthreadinfo = new Service1.ThreadInfo[32];

		// Token: 0x04000363 RID: 867
		public Service1.GroupInfo[] groupinfo = new Service1.GroupInfo[32];

		// Token: 0x04000364 RID: 868
		public Service1.GroupInfo primeLgroup = new Service1.GroupInfo();

		// Token: 0x04000365 RID: 869
		public Service1.GroupInfo subLgroup = new Service1.GroupInfo();

		// Token: 0x04000366 RID: 870
		public Service1.GroupInfo littleLgroup = new Service1.GroupInfo();

		// Token: 0x04000367 RID: 871
		public Service1.GroupInfo[] Lgroup = new Service1.GroupInfo[32];

		// Token: 0x04000368 RID: 872
		public Service1.GroupInfo[] BPgroup = new Service1.GroupInfo[32];

		// Token: 0x04000369 RID: 873
		public Service1.GroupInfo[] BSgroup = new Service1.GroupInfo[32];

		// Token: 0x0400036A RID: 874
		public Service1.GroupInfo[] BP_L_group = new Service1.GroupInfo[32];

		// Token: 0x0400036B RID: 875
		public Service1.GroupInfo[] L_BP_group = new Service1.GroupInfo[32];

		// Token: 0x0400036C RID: 876
		public Service1.GroupInfo[] BP_L_BS_group = new Service1.GroupInfo[32];

		// Token: 0x0400036D RID: 877
		public Service1.GroupInfo[] L_BP_BS_group = new Service1.GroupInfo[32];

		// Token: 0x0400036E RID: 878
		public Service1.GroupInfo[] Perfgroup = new Service1.GroupInfo[32];

		// Token: 0x0400036F RID: 879
		public Service1.GroupInfo[] Effgroup = new Service1.GroupInfo[32];

		// Token: 0x04000370 RID: 880
		public Service1.GroupInfo[] Smtgroup = new Service1.GroupInfo[32];

		// Token: 0x04000371 RID: 881
		public Service1.GroupInfo totalgroup = new Service1.GroupInfo();

		// Token: 0x04000372 RID: 882
		public Service1.CoreInfo[] coreinfo = new Service1.CoreInfo[32];

		// Token: 0x04000373 RID: 883
		public Service1.SysInfo sysinfo = new Service1.SysInfo();

		// Token: 0x04000374 RID: 884
		public Service1.ProcessInfo[] findprocessinfo = new Service1.ProcessInfo[32];

		// Token: 0x04000375 RID: 885
		public long[] avg_inspressure = new long[32];

		// Token: 0x04000376 RID: 886
		public int[] activecount = new int[32];

		// Token: 0x04000377 RID: 887
		public int[] activecount1 = new int[32];

		// Token: 0x04000378 RID: 888
		public int numofgroup;

		// Token: 0x04000379 RID: 889
		public long index;

		// Token: 0x0400037A RID: 890
		public int tempgroup;

		// Token: 0x0400037B RID: 891
		public long global_indicator;

		// Token: 0x0400037C RID: 892
		private Dictionary<int, int> index2procnum = new Dictionary<int, int>();

		// Token: 0x0400037D RID: 893
		private Dictionary<int, int> index2procnum4big_p = new Dictionary<int, int>();

		// Token: 0x0400037E RID: 894
		private Dictionary<int, int> index2procnum4big_s = new Dictionary<int, int>();

		// Token: 0x0400037F RID: 895
		private Dictionary<int, int> groupnum2duration = new Dictionary<int, int>();

		// Token: 0x04000380 RID: 896
		public int availLgroupnum = 3;

		// Token: 0x04000381 RID: 897
		public int availBPgroupnum;

		// Token: 0x04000382 RID: 898
		public int availBSgroupnum;

		// Token: 0x04000383 RID: 899
		public int currentperflvl;

		// Token: 0x04000384 RID: 900
		public int currentefflvl;

		// Token: 0x04000385 RID: 901
		public int currentsmtlvl;

		// Token: 0x04000386 RID: 902
		public long Bresidency;

		// Token: 0x04000387 RID: 903
		public long B2Lresidency;

		// Token: 0x04000388 RID: 904
		public long B2L2Sresidency;

		// Token: 0x04000389 RID: 905
		public long B2Sresidency;

		// Token: 0x0400038A RID: 906
		public long B2S2Lresidency;

		// Token: 0x0400038B RID: 907
		public long Lresidency;

		// Token: 0x0400038C RID: 908
		public long L2Bresidency;

		// Token: 0x0400038D RID: 909
		public long L2B2Sresidency;

		// Token: 0x0400038E RID: 910
		public int big;

		// Token: 0x0400038F RID: 911
		public int little;

		// Token: 0x04000390 RID: 912
		public float schedule_little_ratio;

		// Token: 0x04000391 RID: 913
		public int big_actual;

		// Token: 0x04000392 RID: 914
		public int little_actual;

		// Token: 0x04000393 RID: 915
		public long smt;

		// Token: 0x04000394 RID: 916
		public long smt1;

		// Token: 0x04000395 RID: 917
		public long smtt;

		// Token: 0x04000396 RID: 918
		public long smtt1;

		// Token: 0x04000397 RID: 919
		public long smttt;

		// Token: 0x04000398 RID: 920
		public long smttt1;

		// Token: 0x04000399 RID: 921
		public long cycles_per_miss;

		// Token: 0x0400039A RID: 922
		public int perflevel0;

		// Token: 0x0400039B RID: 923
		public int perflevel1;

		// Token: 0x0400039C RID: 924
		public int perflevel2;

		// Token: 0x0400039D RID: 925
		public int perflevel00;

		// Token: 0x0400039E RID: 926
		public int perflevel01;

		// Token: 0x0400039F RID: 927
		public int perflevel02;

		// Token: 0x040003A0 RID: 928
		public int perflevel3;

		// Token: 0x040003A1 RID: 929
		public int count4level3;

		// Token: 0x040003A2 RID: 930
		public int Mode;

		// Token: 0x040003A3 RID: 931
		public int perfstatenum;

		// Token: 0x040003A4 RID: 932
		public int little_per_group_count = 1;

		// Token: 0x040003A5 RID: 933
		public int maxLP;

		// Token: 0x040003A6 RID: 934
		public int[] temp_msr1 = new int[32];

		// Token: 0x040003A7 RID: 935
		public long acc_ins_big;

		// Token: 0x040003A8 RID: 936
		public long acc_ins_big_cnt;

		// Token: 0x040003A9 RID: 937
		public long avg_ins_big;

		// Token: 0x040003AA RID: 938
		public long collected_threads_cnt;

		// Token: 0x040003AB RID: 939
		public long condition_cache_miss_enabled;

		// Token: 0x040003AC RID: 940
		private List<uint> level_nodes_l = new List<uint>();

		// Token: 0x040003AD RID: 941
		private List<uint> level_nodes_p = new List<uint>();

		// Token: 0x040003AE RID: 942
		private List<uint> lgroupIndices4sl = new List<uint>();

		// Token: 0x040003AF RID: 943
		private List<uint> lgroupIndices = new List<uint>();

		// Token: 0x040003B0 RID: 944
		private List<uint> littleIndices = new List<uint>();

		// Token: 0x040003B1 RID: 945
		private List<uint> exlittleIndices = new List<uint>();

		// Token: 0x040003B2 RID: 946
		private List<uint> bigPhysicalIndices = new List<uint>();

		// Token: 0x040003B3 RID: 947
		private List<uint> bigSmtIndices = new List<uint>();

		// Token: 0x040003B4 RID: 948
		public Service1.ThreadClassifier threadClassifier = new Service1.ThreadClassifier();

		// Token: 0x040003B5 RID: 949
		public long accdatalinkage;

		// Token: 0x040003B6 RID: 950
		public Service1.ThreadPriorityMapper threadmapper = new Service1.ThreadPriorityMapper();

		// Token: 0x040003B7 RID: 951
		public Service1.CrossAttentionScheduler scheduler = new Service1.CrossAttentionScheduler();

		// Token: 0x040003B8 RID: 952
		public RealtimeScheduler realtimeScheduler = new RealtimeScheduler(64);

		// Token: 0x040003BA RID: 954
		public TransformerScheduler transformerScheduler = new TransformerScheduler("./scheduler_model.bin");

		// Token: 0x040003BB RID: 955
		private float[][] coreFeatures;

		// Token: 0x040003BC RID: 956
		private long SchedulerRuntime;

		// Token: 0x040003BD RID: 957
		private long switchvalue;

		// Token: 0x040003BE RID: 958
		private CoreIndexMapper coreIndex;

		// Token: 0x040003BF RID: 959
		private const int ProcessPowerThrottling = 4;

		// Token: 0x040003C0 RID: 960
		private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 1U;

		// Token: 0x040003C1 RID: 961
		private IContainer components;

		// Token: 0x02000065 RID: 101
		public class SchedulerThreadData
		{
			// Token: 0x17000044 RID: 68
			// (get) Token: 0x06000330 RID: 816 RVA: 0x0001ED00 File Offset: 0x0001CF00
			// (set) Token: 0x06000331 RID: 817 RVA: 0x0001ED08 File Offset: 0x0001CF08
			public float InstructionCount { get; set; }

			// Token: 0x17000045 RID: 69
			// (get) Token: 0x06000332 RID: 818 RVA: 0x0001ED11 File Offset: 0x0001CF11
			// (set) Token: 0x06000333 RID: 819 RVA: 0x0001ED19 File Offset: 0x0001CF19
			public float Ipc { get; set; }

			// Token: 0x17000046 RID: 70
			// (get) Token: 0x06000334 RID: 820 RVA: 0x0001ED22 File Offset: 0x0001CF22
			// (set) Token: 0x06000335 RID: 821 RVA: 0x0001ED2A File Offset: 0x0001CF2A
			public float BranchMiss { get; set; }

			// Token: 0x17000047 RID: 71
			// (get) Token: 0x06000336 RID: 822 RVA: 0x0001ED33 File Offset: 0x0001CF33
			// (set) Token: 0x06000337 RID: 823 RVA: 0x0001ED3B File Offset: 0x0001CF3B
			public float CacheMiss { get; set; }

			// Token: 0x17000048 RID: 72
			// (get) Token: 0x06000338 RID: 824 RVA: 0x0001ED44 File Offset: 0x0001CF44
			// (set) Token: 0x06000339 RID: 825 RVA: 0x0001ED4C File Offset: 0x0001CF4C
			public float Priority { get; set; }

			// Token: 0x17000049 RID: 73
			// (get) Token: 0x0600033A RID: 826 RVA: 0x0001ED55 File Offset: 0x0001CF55
			// (set) Token: 0x0600033B RID: 827 RVA: 0x0001ED5D File Offset: 0x0001CF5D
			public int ArtificialDecision { get; set; }

			// Token: 0x0600033C RID: 828 RVA: 0x0001ED66 File Offset: 0x0001CF66
			public float[] ToArray()
			{
				return new float[] { this.InstructionCount, this.Ipc, this.BranchMiss, this.CacheMiss, this.Priority };
			}

			// Token: 0x0600033D RID: 829 RVA: 0x0001ED9B File Offset: 0x0001CF9B
			public string DecisionStr()
			{
				if (this.ArtificialDecision != 1)
				{
					return "小核";
				}
				return "大核";
			}
		}

		// Token: 0x02000066 RID: 102
		public class SchedulerDataset
		{
			// Token: 0x1700004A RID: 74
			// (get) Token: 0x0600033F RID: 831 RVA: 0x0001EDB9 File Offset: 0x0001CFB9
			public int Size
			{
				get
				{
					return this._data.Count;
				}
			}

			// Token: 0x1700004B RID: 75
			// (get) Token: 0x06000340 RID: 832 RVA: 0x0001EDC6 File Offset: 0x0001CFC6
			public float[] Mean
			{
				get
				{
					return this._mean;
				}
			}

			// Token: 0x1700004C RID: 76
			// (get) Token: 0x06000341 RID: 833 RVA: 0x0001EDCE File Offset: 0x0001CFCE
			public float[] Std
			{
				get
				{
					return this._std;
				}
			}

			// Token: 0x06000342 RID: 834 RVA: 0x0001EDD8 File Offset: 0x0001CFD8
			public void GenerateData(int count = 1000)
			{
				Random random = new Random(42);
				for (int i = 0; i < count; i++)
				{
					Service1.SchedulerThreadData schedulerThreadData = new Service1.SchedulerThreadData
					{
						InstructionCount = (float)random.Next(1000, 100000),
						Ipc = (float)(random.NextDouble() * 3.0),
						BranchMiss = (float)random.Next(0, 10000),
						CacheMiss = (float)random.Next(0, 5000),
						Priority = (float)random.Next(1, 10)
					};
					float num = schedulerThreadData.Priority * 0.3f + schedulerThreadData.InstructionCount / 100000f * 0.2f + schedulerThreadData.Ipc * 0.3f + (schedulerThreadData.BranchMiss + schedulerThreadData.CacheMiss) / 10000f * 0.2f;
					schedulerThreadData.ArtificialDecision = (((double)num > 2.8) ? 1 : 0);
					this._data.Add(schedulerThreadData);
				}
				this.ComputeNorm();
				Console.WriteLine(string.Format("生成 {0} 条数据 (大核 {1}, 小核 {2})", count, this._data.Count((Service1.SchedulerThreadData d) => d.ArtificialDecision == 1), this._data.Count((Service1.SchedulerThreadData d) => d.ArtificialDecision == 0)));
			}

			// Token: 0x06000343 RID: 835 RVA: 0x0001EF48 File Offset: 0x0001D148
			private void ComputeNorm()
			{
				if (this._data.Count == 0)
				{
					return;
				}
				float[][] array = this._data.Select((Service1.SchedulerThreadData d) => d.ToArray()).ToArray<float[]>();
				int i;
				Func<float[], float> <>9__1;
				Func<float[], float> <>9__2;
				int i3;
				for (i = 0; i < 5; i = i3 + 1)
				{
					float[] mean = this._mean;
					int j = i;
					IEnumerable<float[]> enumerable = array;
					Func<float[], float> func;
					if ((func = <>9__1) == null)
					{
						func = (<>9__1 = (float[] x) => x[i]);
					}
					mean[j] = enumerable.Average(func);
					float[] std = this._std;
					int i2 = i;
					IEnumerable<float[]> enumerable2 = array;
					Func<float[], float> func2;
					if ((func2 = <>9__2) == null)
					{
						func2 = (<>9__2 = (float[] x) => (x[i] - this._mean[i]) * (x[i] - this._mean[i]));
					}
					std[i2] = (float)Math.Sqrt((double)enumerable2.Average(func2));
					if (this._std[i] < 1E-06f)
					{
						this._std[i] = 1f;
					}
					i3 = i;
				}
			}

			// Token: 0x06000344 RID: 836 RVA: 0x0001F05C File Offset: 0x0001D25C
			[return: TupleElementNames(new string[] { "input", "label" })]
			public ValueTuple<float[], int> GetItem(int idx)
			{
				float[] array = this._data[idx].ToArray();
				float[] array2 = new float[5];
				for (int i = 0; i < 5; i++)
				{
					array2[i] = (array[i] - this._mean[i]) / this._std[i];
				}
				return new ValueTuple<float[], int>(array2, this._data[idx].ArtificialDecision);
			}

			// Token: 0x040004A4 RID: 1188
			private readonly List<Service1.SchedulerThreadData> _data = new List<Service1.SchedulerThreadData>();

			// Token: 0x040004A5 RID: 1189
			private float[] _mean = new float[5];

			// Token: 0x040004A6 RID: 1190
			private float[] _std = new float[5];
		}

		// Token: 0x02000067 RID: 103
		public static class Mat
		{
			// Token: 0x06000346 RID: 838 RVA: 0x0001F0E8 File Offset: 0x0001D2E8
			public static float[,] Rand(int r, int c, float s)
			{
				float[,] array = new float[r, c];
				Random random = new Random();
				for (int i = 0; i < r; i++)
				{
					for (int j = 0; j < c; j++)
					{
						array[i, j] = (float)(random.NextDouble() * 2.0 - 1.0) * s;
					}
				}
				return array;
			}
		}

		// Token: 0x02000068 RID: 104
		public class CrossAttentionScheduler
		{
			// Token: 0x06000347 RID: 839 RVA: 0x0001F144 File Offset: 0x0001D344
			public CrossAttentionScheduler()
			{
				float num = 0.2f;
				this._emb = Service1.Mat.Rand(5, 16, num);
				this._coreK = Service1.Mat.Rand(2, 16, 0.1f);
				this._coreV = Service1.Mat.Rand(2, 16, 0.1f);
				this._ff1 = Service1.Mat.Rand(16, 32, num);
				this._ff2 = Service1.Mat.Rand(32, 16, num);
				this._out = Service1.Mat.Rand(16, 2, num);
			}

			// Token: 0x06000348 RID: 840 RVA: 0x0001F278 File Offset: 0x0001D478
			public void SetNormalization(float[] mean, float[] std)
			{
				for (int i = 0; i < 5; i++)
				{
					this._mean[i] = mean[i];
					this._std[i] = std[i];
				}
			}

			// Token: 0x06000349 RID: 841 RVA: 0x0001F2A8 File Offset: 0x0001D4A8
			private float[] Normalize(float[] raw)
			{
				float[] array = new float[5];
				for (int i = 0; i < 5; i++)
				{
					array[i] = ((this._std[i] > 0f) ? ((raw[i] - this._mean[i]) / this._std[i]) : raw[i]);
				}
				return array;
			}

			// Token: 0x0600034A RID: 842 RVA: 0x0001F2F8 File Offset: 0x0001D4F8
			[return: TupleElementNames(new string[] { "pred", "p" })]
			public ValueTuple<int, float[]> Forward(float[] x)
			{
				float[] array = new float[16];
				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 5; j++)
					{
						array[i] += x[j] * this._emb[j, i];
					}
				}
				float[] array2 = new float[2];
				for (int k = 0; k < 2; k++)
				{
					array2[k] = 0f;
					for (int l = 0; l < 16; l++)
					{
						array2[k] += array[l] * this._coreK[k, l];
					}
				}
				float[] array3 = new float[2];
				float num = Math.Max(array2[0], array2[1]);
				float num2 = 0f;
				for (int m = 0; m < 2; m++)
				{
					array3[m] = (float)Math.Exp((double)(array2[m] - num));
					num2 += array3[m];
				}
				for (int n = 0; n < 2; n++)
				{
					array3[n] /= num2;
				}
				if (float.IsNaN(array3[0]) || float.IsNaN(array3[1]))
				{
					array3[0] = 0.5f;
					array3[1] = 0.5f;
				}
				this._lastAttention = array3;
				float[] array4 = new float[16];
				for (int num3 = 0; num3 < 16; num3++)
				{
					for (int num4 = 0; num4 < 2; num4++)
					{
						array4[num3] += array3[num4] * this._coreV[num4, num3];
					}
				}
				float[] array5 = new float[32];
				for (int num5 = 0; num5 < 32; num5++)
				{
					for (int num6 = 0; num6 < 16; num6++)
					{
						array5[num5] += Math.Max(0f, array4[num6] * this._ff1[num6, num5]);
					}
				}
				float[] array6 = new float[16];
				for (int num7 = 0; num7 < 16; num7++)
				{
					for (int num8 = 0; num8 < 32; num8++)
					{
						array6[num7] += array5[num8] * this._ff2[num8, num7];
					}
				}
				for (int num9 = 0; num9 < 16; num9++)
				{
					array6[num9] += array[num9];
				}
				float[] array7 = new float[2];
				for (int num10 = 0; num10 < 2; num10++)
				{
					for (int num11 = 0; num11 < 16; num11++)
					{
						array7[num10] += array6[num11] * this._out[num11, num10];
					}
				}
				float[] array8 = new float[2];
				num = Math.Max(array7[0], array7[1]);
				num2 = 0f;
				for (int num12 = 0; num12 < 2; num12++)
				{
					array8[num12] = (float)Math.Exp((double)(array7[num12] - num));
					num2 += array8[num12];
				}
				for (int num13 = 0; num13 < 2; num13++)
				{
					array8[num13] /= num2;
				}
				if (float.IsNaN(array8[0]) || float.IsNaN(array8[1]))
				{
					array8[0] = 0.5f;
					array8[1] = 0.5f;
				}
				return new ValueTuple<int, float[]>((array8[0] <= array8[1]) ? 1 : 0, array8);
			}

			// Token: 0x0600034B RID: 843 RVA: 0x0001F630 File Offset: 0x0001D830
			public void Train(Service1.SchedulerDataset ds, int epochs, float lr)
			{
				Random rnd = new Random(123);
				float num = 0f;
				this._mean = ds.Mean;
				this._std = ds.Std;
				Func<int, int> <>9__0;
				for (int i = 0; i < epochs; i++)
				{
					float num2 = 0f;
					int num3 = 0;
					float num4 = lr * (float)Math.Pow(0.949999988079071, (double)i);
					IEnumerable<int> enumerable = Enumerable.Range(0, ds.Size);
					Func<int, int> func;
					if ((func = <>9__0) == null)
					{
						func = (<>9__0 = (int _) => rnd.Next());
					}
					List<int> list = enumerable.OrderBy(func).ToList<int>();
					for (int j = 0; j < ds.Size; j++)
					{
						ValueTuple<float[], int> item = ds.GetItem(list[j]);
						float[] item2 = item.Item1;
						int item3 = item.Item2;
						ValueTuple<int, float[]> valueTuple = this.Forward(item2);
						int item4 = valueTuple.Item1;
						float[] item5 = valueTuple.Item2;
						float num5 = (float)(-(float)Math.Log((double)Math.Max((item3 == 0) ? item5[0] : item5[1], 1E-10f)));
						num2 += num5;
						if (item4 == item3)
						{
							num3++;
						}
						float num6 = item5[0] - ((item3 == 0) ? 1f : 0f);
						float num7 = item5[1] - ((item3 == 1) ? 1f : 0f);
						float num8 = num6 - num7;
						for (int k = 0; k < 16; k++)
						{
							this._out[k, 0] -= num4 * num6 * 0.1f;
							this._out[k, 1] -= num4 * num7 * 0.1f;
						}
						for (int l = 0; l < 16; l++)
						{
							float num9 = num8 * this._out[l, 0] * 0.01f;
							for (int m = 0; m < 5; m++)
							{
								this._emb[m, l] -= num4 * num9 * item2[m] * 0.01f;
							}
						}
						float[] array = new float[16];
						for (int n = 0; n < 16; n++)
						{
							for (int num10 = 0; num10 < 5; num10++)
							{
								array[n] += item2[num10] * this._emb[num10, n];
							}
						}
						for (int num11 = 0; num11 < 2; num11++)
						{
							float num12 = ((num11 == item3) ? 0.1f : (-0.02f));
							for (int num13 = 0; num13 < 16; num13++)
							{
								float num14 = array[num13] - this._coreK[num11, num13];
								this._coreK[num11, num13] += num4 * num12 * num14;
								this._coreK[num11, num13] = ((this._coreK[num11, num13] < -10f) ? (-10f) : ((this._coreK[num11, num13] > 10f) ? 10f : this._coreK[num11, num13]));
							}
						}
					}
					float num15 = (float)num3 * 100f / (float)ds.Size;
					if (num15 > num)
					{
						num = num15;
					}
					Console.WriteLine(string.Format("Epoch {0}: Loss={1:F4} Acc={2:F1}%", i + 1, num2 / (float)ds.Size, num15));
				}
				Console.WriteLine(string.Format("\nCross-Attention训练完成! 最佳准确率: {0:F1}%", num));
				this.PrintCoreTypeTemplates();
			}

			// Token: 0x0600034C RID: 844 RVA: 0x0001F9B4 File Offset: 0x0001DBB4
			public void PrintCoreTypeTemplates()
			{
				Console.WriteLine("\n=== 核心类型能力标签（K向量）===");
				for (int i = 0; i < 2; i++)
				{
					string text = ((i == 0) ? "小核(E-core)" : "大核(P-core)");
					float num = 0f;
					for (int j = 0; j < 16; j++)
					{
						num += Math.Abs(this._coreK[i, j]);
					}
					Console.WriteLine(string.Format("{0}: 能量={1:F2}", text, num));
				}
				Console.WriteLine("\n=== 调度动作向量（V向量）===");
				for (int k = 0; k < 2; k++)
				{
					string text2 = ((k == 0) ? "小核(E-core)" : "大核(P-core)");
					float num2 = 0f;
					for (int l = 0; l < 16; l++)
					{
						num2 += Math.Abs(this._coreV[k, l]);
					}
					Console.WriteLine(string.Format("{0}: 能量={1:F2}", text2, num2));
				}
			}

			// Token: 0x0600034D RID: 845 RVA: 0x0001FA9C File Offset: 0x0001DC9C
			public void TrainOnline(float[] x, int label, float lr = 0.1f)
			{
				float[] item = this.Forward(x).Item2;
				if (float.IsNaN(item[0]) || float.IsNaN(item[1]))
				{
					Console.WriteLine("[警告] 检测到 NaN，跳过本次更新");
					return;
				}
				float num = item[0] - ((label == 0) ? 1f : 0f);
				float num2 = item[1] - ((label == 1) ? 1f : 0f);
				float num3 = item[0];
				float num4 = item[1];
				for (int i = 0; i < 16; i++)
				{
					this._out[i, 0] -= lr * num * 0.1f;
					this._out[i, 1] -= lr * num2 * 0.1f;
					this._out[i, 0] = Math.Max(-5f, Math.Min(5f, this._out[i, 0]));
					this._out[i, 1] = Math.Max(-5f, Math.Min(5f, this._out[i, 1]));
				}
				for (int j = 0; j < 16; j++)
				{
					float num5 = (num - num2) * this._out[j, 0] * 0.01f;
					for (int k = 0; k < 5; k++)
					{
						this._emb[k, j] -= lr * num5 * x[k] * 0.01f;
						this._emb[k, j] = Math.Max(-3f, Math.Min(3f, this._emb[k, j]));
					}
				}
				float[] array = new float[16];
				for (int l = 0; l < 16; l++)
				{
					for (int m = 0; m < 5; m++)
					{
						array[l] += x[m] * this._emb[m, l];
					}
				}
				for (int n = 0; n < 2; n++)
				{
					float num6 = ((n == label) ? 0.1f : (-0.02f));
					for (int num7 = 0; num7 < 16; num7++)
					{
						float num8 = array[num7] - this._coreK[n, num7];
						this._coreK[n, num7] += lr * num6 * num8;
						this._coreK[n, num7] = Math.Max(-10f, Math.Min(10f, this._coreK[n, num7]));
					}
				}
			}

			// Token: 0x0600034E RID: 846 RVA: 0x0001FD30 File Offset: 0x0001DF30
			public void TrainOnlineRaw(float[] raw, int label, float lr = 0.1f)
			{
				for (int i = 0; i < 5; i++)
				{
					if (float.IsNaN(raw[i]) || float.IsInfinity(raw[i]))
					{
						Console.WriteLine(string.Format("[警告] 收到无效的原始数据特征[{0}]: {1}，跳过本次学习", i, raw[i]));
						return;
					}
				}
				if (!this._isModelReady)
				{
					this._sampleCount++;
					for (int j = 0; j < 5; j++)
					{
						float num = raw[j] - this._runningMean[j];
						this._runningMean[j] += num / (float)this._sampleCount;
						float num2 = raw[j] - this._runningMean[j];
						this._runningM2[j] += num * num2;
						this._mean[j] = this._runningMean[j];
						if (this._sampleCount > 1)
						{
							float num3 = Math.Max(0f, this._runningM2[j]);
							this._std[j] = (float)Math.Sqrt((double)(num3 / (float)(this._sampleCount - 1)));
						}
						else
						{
							this._std[j] = 1f;
						}
						if (this._std[j] < 1E-06f)
						{
							this._std[j] = 1f;
						}
					}
				}
				float[] array = this.Normalize(raw);
				this.TrainOnline(array, label, lr);
			}

			// Token: 0x0600034F RID: 847 RVA: 0x0001FE74 File Offset: 0x0001E074
			public void Evaluate(Service1.SchedulerDataset ds)
			{
				int num = 0;
				Console.WriteLine("\n========== 评估结果 ==========");
				Console.WriteLine(string.Format("{0,-45} {1,-6} {2,-6} {3}", new object[] { "属性", "人工", "预测", "✓/✗" }));
				Console.WriteLine(new string('-', 70));
				for (int i = 0; i < Math.Min(12, ds.Size); i++)
				{
					ValueTuple<float[], int> item = ds.GetItem(i);
					float[] item2 = item.Item1;
					int item3 = item.Item2;
					int item4 = this.Forward(item2).Item1;
					bool flag = item4 == item3;
					if (flag)
					{
						num++;
					}
					float[] item5 = ds.GetItem(i).Item1;
					Console.WriteLine(string.Format("IC:{0:F1} IPC:{1:F2} BM:{2:F0} CM:{3:F0} P:{4:F0} 人工:{5} 预测:{6} {7}", new object[]
					{
						item5[0],
						item5[1],
						item5[2],
						item5[3],
						item5[4],
						(item3 == 1) ? "大核" : "小核",
						(item4 == 1) ? "大核" : "小核",
						flag ? "✓" : "✗"
					}));
				}
				Console.WriteLine(new string('-', 70));
				Console.WriteLine(string.Format("总体准确率: {0:F1}%", (float)num * 100f / 12f));
			}

			// Token: 0x06000350 RID: 848 RVA: 0x0001FFE8 File Offset: 0x0001E1E8
			public void Predict(Service1.SchedulerThreadData t)
			{
				float[] array = t.ToArray();
				float[] array2 = this.Normalize(array);
				ValueTuple<int, float[]> valueTuple = this.Forward(array2);
				int item = valueTuple.Item1;
				float[] item2 = valueTuple.Item2;
				Console.WriteLine("\n========== 预测结果 ==========");
				Console.WriteLine(string.Format("线程属性: IC={0} IPC={1:F2} BM={2} CM={3} P={4}", new object[] { t.InstructionCount, t.Ipc, t.BranchMiss, t.CacheMiss, t.Priority }));
				Console.WriteLine("人工决策: " + t.DecisionStr());
				Console.WriteLine(string.Format("模型预测: {0} (大核={1:F1}% 小核={2:F1}%)", (item == 1) ? "大核" : "小核", item2[1] * 100f, item2[0] * 100f));
				Console.WriteLine(string.Format("注意力权重: 小核={0:F1}% 大核={1:F1}%", this._lastAttention[0] * 100f, this._lastAttention[1] * 100f));
				Console.WriteLine("预测正确: " + ((item == t.ArtificialDecision) ? "✓" : "✗"));
			}

			// Token: 0x06000351 RID: 849 RVA: 0x00020128 File Offset: 0x0001E328
			public int Schedule(Service1.SchedulerThreadData thread)
			{
				float[] array = thread.ToArray();
				int item = this.PredictRaw(array).Item1;
				if (thread.ArtificialDecision >= 0)
				{
					this._totalPredictions++;
					if (item == thread.ArtificialDecision)
					{
						this._correctPredictions++;
					}
				}
				return item;
			}

			// Token: 0x06000352 RID: 850 RVA: 0x0002017C File Offset: 0x0001E37C
			[return: TupleElementNames(new string[] { "pred", "p" })]
			public ValueTuple<int, float[]> PredictRaw(float[] raw)
			{
				float[] array = this.Normalize(raw);
				return this.Forward(array);
			}

			// Token: 0x06000353 RID: 851 RVA: 0x00020198 File Offset: 0x0001E398
			public bool IsModelReady(float minAccuracy = 0.6f, int minSamples = 1000)
			{
				if (this._isModelReady)
				{
					return true;
				}
				if (this._sampleCount < minSamples)
				{
					Console.WriteLine(string.Format("[学习状态] 训练样本不足: {0}/{1}", this._sampleCount, minSamples));
					return false;
				}
				this._isModelReady = true;
				this._energyLearningEnabled = true;
				Console.WriteLine(string.Format("[学习状态] ✓ 模型已准备好! 训练样本: {0}，能效学习已启用", this._sampleCount));
				return true;
			}

			// Token: 0x06000354 RID: 852 RVA: 0x00020203 File Offset: 0x0001E403
			public float[] GetAttention()
			{
				return this._lastAttention;
			}

			// Token: 0x06000355 RID: 853 RVA: 0x0002020C File Offset: 0x0001E40C
			public void Learn(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority, int decision, float learningRate = 0.1f)
			{
				float[] array = new float[] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				this.TrainOnlineRaw(array, decision, learningRate);
			}

			// Token: 0x06000356 RID: 854 RVA: 0x00020241 File Offset: 0x0001E441
			public void Learn(Service1.SchedulerThreadData thread, float learningRate = 0.1f)
			{
				this.Learn(thread.InstructionCount, thread.Ipc, thread.BranchMiss, thread.CacheMiss, thread.Priority, thread.ArtificialDecision, learningRate);
			}

			// Token: 0x06000357 RID: 855 RVA: 0x00020270 File Offset: 0x0001E470
			public int Schedule(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority)
			{
				float[] array = new float[] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				return this.PredictRaw(array).Item1;
			}

			// Token: 0x06000358 RID: 856 RVA: 0x000202A8 File Offset: 0x0001E4A8
			[return: TupleElementNames(new string[] { "coreType", "bigCoreProb", "attention" })]
			public ValueTuple<int, float, float[]> ScheduleWithDetails(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority)
			{
				float[] array = new float[] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				ValueTuple<int, float[]> valueTuple = this.PredictRaw(array);
				int item = valueTuple.Item1;
				float[] item2 = valueTuple.Item2;
				return new ValueTuple<int, float, float[]>(item, item2[1], this._lastAttention);
			}

			// Token: 0x06000359 RID: 857 RVA: 0x000202F5 File Offset: 0x0001E4F5
			[return: TupleElementNames(new string[] { "coreType", "bigCoreProb", "attention" })]
			public ValueTuple<int, float, float[]> ScheduleWithDetails(Service1.SchedulerThreadData thread)
			{
				return this.ScheduleWithDetails(thread.InstructionCount, thread.Ipc, thread.BranchMiss, thread.CacheMiss, thread.Priority);
			}

			// Token: 0x0600035A RID: 858 RVA: 0x0002031B File Offset: 0x0001E51B
			public void EnableEnergyLearning()
			{
				this._energyLearningEnabled = true;
				this._baselineMetric = 0f;
				this._currentWindow.Clear();
				Console.WriteLine("能效学习模式已启用");
			}

			// Token: 0x0600035B RID: 859 RVA: 0x00020344 File Offset: 0x0001E544
			public void DisableEnergyLearning()
			{
				this._energyLearningEnabled = false;
				Console.WriteLine("能效学习模式已禁用");
			}

			// Token: 0x0600035C RID: 860 RVA: 0x00020358 File Offset: 0x0001E558
			public int ScheduleAndRecord(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority)
			{
				this._scheduleCount++;
				if (!this._energyLearningEnabled)
				{
					return this.Schedule(instructionCount, ipc, branchMiss, cacheMiss, priority);
				}
				float[] array = new float[] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				bool flag = false;
				for (int i = 0; i < 5; i++)
				{
					if (float.IsNaN(array[i]) || float.IsInfinity(array[i]))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					Console.WriteLine("[警告] ScheduleAndRecord 收到无效输入，返回随机决策");
					return this._rnd.Next(2);
				}
				float[] array2 = this.Normalize(array);
				ValueTuple<int, float[]> valueTuple = this.PredictRaw(array);
				int item = valueTuple.Item1;
				float[] item2 = valueTuple.Item2;
				int num;
				if (this._rnd.NextDouble() < (double)this._explorationRate)
				{
					num = this._rnd.Next(2);
					this._explorationCount++;
				}
				else
				{
					num = item;
				}
				Service1.CrossAttentionScheduler.ScheduleRecord scheduleRecord = new Service1.CrossAttentionScheduler.ScheduleRecord
				{
					Features = array2,
					RawFeatures = array,
					Decision = num,
					Attention = (float[])this._lastAttention.Clone(),
					Probabilities = item2,
					Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
					HasReward = false,
					Reward = 0f
				};
				this._currentWindow.Add(scheduleRecord);
				return num;
			}

			// Token: 0x0600035D RID: 861 RVA: 0x000204AE File Offset: 0x0001E6AE
			public int ScheduleAndRecord(Service1.SchedulerThreadData thread)
			{
				return this.ScheduleAndRecord(thread.InstructionCount, thread.Ipc, thread.BranchMiss, thread.CacheMiss, thread.Priority);
			}

			// Token: 0x0600035E RID: 862 RVA: 0x000204D4 File Offset: 0x0001E6D4
			public void ReceiveEnergyFeedback(float energyMetric)
			{
				if (float.IsNaN(energyMetric) || float.IsInfinity(energyMetric) || energyMetric <= 0f)
				{
					Console.WriteLine(string.Format("[警告] 收到无效的能效指标: {0}，已忽略", energyMetric));
					return;
				}
				long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				this._metricHistory.Add(new ValueTuple<long, float>(num, energyMetric));
				long cutoff = num - this._metricWindowMs;
				this._metricHistory.RemoveAll(([TupleElementNames(new string[] { "timestamp", "metric" })] ValueTuple<long, float> m) => m.Item1 < cutoff);
				if (!this._energyLearningEnabled || this._currentWindow.Count == 0)
				{
					return;
				}
				this._energyLearningCount++;
				float num2 = this.ComputeReward(energyMetric);
				this.DistributeReward(num2);
				this.UpdateBaseline(energyMetric);
				this.ReplayLearning();
				this._explorationRate = Math.Max(this._minExplorationRate, this._explorationRate * this._explorationDecay);
				this._currentWindow.Clear();
			}

			// Token: 0x0600035F RID: 863 RVA: 0x000205C4 File Offset: 0x0001E7C4
			private float ComputeReward(float metric)
			{
				if (this._baselineMetric <= 0f || float.IsNaN(this._baselineMetric) || float.IsInfinity(this._baselineMetric))
				{
					return 0f;
				}
				if (metric <= 0f || float.IsNaN(metric) || float.IsInfinity(metric))
				{
					return 0f;
				}
				return (metric - this._baselineMetric) / this._baselineMetric * 10f;
			}

			// Token: 0x06000360 RID: 864 RVA: 0x00020634 File Offset: 0x0001E834
			private void DistributeReward(float reward)
			{
				if (this._currentWindow.Count == 0)
				{
					return;
				}
				if (float.IsNaN(reward) || float.IsInfinity(reward))
				{
					Console.WriteLine(string.Format("[警告] 无效的奖励值: {0}，跳过分配", reward));
					return;
				}
				long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				float num2 = 0f;
				float[] array = new float[this._currentWindow.Count];
				for (int i = 0; i < this._currentWindow.Count; i++)
				{
					Service1.CrossAttentionScheduler.ScheduleRecord scheduleRecord = this._currentWindow[i];
					if (scheduleRecord.Probabilities == null || scheduleRecord.Probabilities.Length < 2)
					{
						array[i] = 0.5f;
						num2 += array[i];
					}
					else
					{
						float num3 = (float)Math.Exp((double)(-(double)((float)Math.Max(0L, num - scheduleRecord.Timestamp)) / (float)this._windowMs));
						float num4 = ((scheduleRecord.Decision == 1) ? scheduleRecord.Probabilities[1] : scheduleRecord.Probabilities[0]);
						num4 = Math.Max(0.01f, Math.Min(0.99f, num4));
						float num5 = num4;
						array[i] = num3 * num5;
						num2 += array[i];
					}
				}
				for (int j = 0; j < this._currentWindow.Count; j++)
				{
					if (num2 > 0f)
					{
						this._currentWindow[j].Reward = reward * array[j] / num2;
					}
					else
					{
						this._currentWindow[j].Reward = 0f;
					}
					this._currentWindow[j].HasReward = true;
					this._replayBuffer.Add(this._currentWindow[j]);
				}
				while (this._replayBuffer.Count > this._replayBufferSize)
				{
					this._replayBuffer.RemoveAt(0);
				}
			}

			// Token: 0x06000361 RID: 865 RVA: 0x00020800 File Offset: 0x0001EA00
			private void UpdateBaseline(float metric)
			{
				if (metric <= 0f || float.IsNaN(metric) || float.IsInfinity(metric))
				{
					Console.WriteLine(string.Format("[警告] 无效的能效指标: {0}，跳过基线更新", metric));
					return;
				}
				if (this._baselineMetric == 0f || float.IsNaN(this._baselineMetric) || float.IsInfinity(this._baselineMetric))
				{
					this._baselineMetric = metric;
					return;
				}
				this._baselineMetric = this._baselineDecay * this._baselineMetric + (1f - this._baselineDecay) * metric;
			}

			// Token: 0x06000362 RID: 866 RVA: 0x0002088C File Offset: 0x0001EA8C
			private void ReplayLearning()
			{
				if (this._replayBuffer.Count < 10)
				{
					return;
				}
				int num = Math.Min(32, this._replayBuffer.Count);
				float num2 = 0.05f;
				for (int i = 0; i < num; i++)
				{
					int num3 = this._rnd.Next(this._replayBuffer.Count);
					Service1.CrossAttentionScheduler.ScheduleRecord scheduleRecord = this._replayBuffer[num3];
					if (scheduleRecord.HasReward && scheduleRecord.RawFeatures != null && scheduleRecord.RawFeatures.Length == 5 && !float.IsNaN(scheduleRecord.Reward) && !float.IsInfinity(scheduleRecord.Reward))
					{
						int num4 = ((scheduleRecord.Reward > 0f) ? scheduleRecord.Decision : (1 - scheduleRecord.Decision));
						float num5 = Math.Min(Math.Abs(scheduleRecord.Reward), 1f);
						float num6 = num2 * num5;
						this.TrainOnlineRaw(scheduleRecord.RawFeatures, num4, num6);
					}
				}
			}

			// Token: 0x06000363 RID: 867 RVA: 0x00020987 File Offset: 0x0001EB87
			[return: TupleElementNames(new string[] { "bufferSize", "explorationRate", "baselineMetric" })]
			public ValueTuple<int, float, float> GetEnergyLearningStats()
			{
				return new ValueTuple<int, float, float>(this._replayBuffer.Count, this._explorationRate, this._baselineMetric);
			}

			// Token: 0x06000364 RID: 868 RVA: 0x000209A8 File Offset: 0x0001EBA8
			public string GetStatistics()
			{
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				int count = this._metricHistory.Count;
				if (count > 0)
				{
					num4 = this._metricHistory[count - 1].Item2;
					num = this._metricHistory.Sum(([TupleElementNames(new string[] { "timestamp", "metric" })] ValueTuple<long, float> m) => m.Item2) / (float)count;
					num2 = this._metricHistory.Min(([TupleElementNames(new string[] { "timestamp", "metric" })] ValueTuple<long, float> m) => m.Item2);
					num3 = this._metricHistory.Max(([TupleElementNames(new string[] { "timestamp", "metric" })] ValueTuple<long, float> m) => m.Item2);
				}
				return string.Concat(new string[]
				{
					"[能效统计 - 5分钟窗口]\n",
					string.Format("  指标 - 当前: {0:F2}, 平均: {1:F2}, 最小: {2:F2}, 最大: {3:F2}, 次数: {4}\n", new object[] { num4, num, num2, num3, count }),
					"[学习状态]\n",
					string.Format("  模型就绪: {0}, 能效学习: {1}, 学习次数: {2}\n", this._isModelReady, this._energyLearningEnabled, this._energyLearningCount),
					string.Format("  调度次数: {0}, 探索次数: {1}, 探索率: {2:P2}\n", this._scheduleCount, this._explorationCount, this._explorationRate),
					string.Format("  缓冲区: {0}/{1}, 基线: {2:F2}", this._replayBuffer.Count, this._replayBufferSize, this._baselineMetric)
				});
			}

			// Token: 0x040004A7 RID: 1191
			private const int DM = 16;

			// Token: 0x040004A8 RID: 1192
			private const int IN = 5;

			// Token: 0x040004A9 RID: 1193
			private const int FF = 32;

			// Token: 0x040004AA RID: 1194
			private const int CoreTypes = 2;

			// Token: 0x040004AB RID: 1195
			private readonly float[,] _emb;

			// Token: 0x040004AC RID: 1196
			private readonly float[,] _coreK;

			// Token: 0x040004AD RID: 1197
			private readonly float[,] _coreV;

			// Token: 0x040004AE RID: 1198
			private readonly float[,] _ff1;

			// Token: 0x040004AF RID: 1199
			private readonly float[,] _ff2;

			// Token: 0x040004B0 RID: 1200
			private readonly float[,] _out;

			// Token: 0x040004B1 RID: 1201
			private float[] _mean = new float[5];

			// Token: 0x040004B2 RID: 1202
			private float[] _std = new float[5];

			// Token: 0x040004B3 RID: 1203
			private int _sampleCount;

			// Token: 0x040004B4 RID: 1204
			private float[] _runningMean = new float[5];

			// Token: 0x040004B5 RID: 1205
			private float[] _runningM2 = new float[5];

			// Token: 0x040004B6 RID: 1206
			private int _totalPredictions;

			// Token: 0x040004B7 RID: 1207
			private int _correctPredictions;

			// Token: 0x040004B8 RID: 1208
			private bool _isModelReady;

			// Token: 0x040004B9 RID: 1209
			private float[] _lastAttention = new float[2];

			// Token: 0x040004BA RID: 1210
			private readonly List<Service1.CrossAttentionScheduler.ScheduleRecord> _currentWindow = new List<Service1.CrossAttentionScheduler.ScheduleRecord>();

			// Token: 0x040004BB RID: 1211
			private readonly List<Service1.CrossAttentionScheduler.ScheduleRecord> _replayBuffer = new List<Service1.CrossAttentionScheduler.ScheduleRecord>();

			// Token: 0x040004BC RID: 1212
			private readonly int _replayBufferSize = 10000;

			// Token: 0x040004BD RID: 1213
			private readonly long _windowMs = 1000L;

			// Token: 0x040004BE RID: 1214
			private float _baselineMetric;

			// Token: 0x040004BF RID: 1215
			private float _baselineDecay = 0.99f;

			// Token: 0x040004C0 RID: 1216
			private float _explorationRate = 0.1f;

			// Token: 0x040004C1 RID: 1217
			private float _explorationDecay = 0.999f;

			// Token: 0x040004C2 RID: 1218
			private float _minExplorationRate = 0.01f;

			// Token: 0x040004C3 RID: 1219
			private readonly Random _rnd = new Random();

			// Token: 0x040004C4 RID: 1220
			private bool _energyLearningEnabled;

			// Token: 0x040004C5 RID: 1221
			[TupleElementNames(new string[] { "timestamp", "metric" })]
			private readonly List<ValueTuple<long, float>> _metricHistory = new List<ValueTuple<long, float>>();

			// Token: 0x040004C6 RID: 1222
			private readonly long _metricWindowMs = 300000L;

			// Token: 0x040004C7 RID: 1223
			private int _scheduleCount;

			// Token: 0x040004C8 RID: 1224
			private int _energyLearningCount;

			// Token: 0x040004C9 RID: 1225
			private int _explorationCount;

			// Token: 0x020000A0 RID: 160
			public class ScheduleRecord
			{
				// Token: 0x1700022B RID: 555
				// (get) Token: 0x060007EB RID: 2027 RVA: 0x00026FB8 File Offset: 0x000251B8
				// (set) Token: 0x060007EC RID: 2028 RVA: 0x00026FC0 File Offset: 0x000251C0
				public float[] Features { get; set; } = Array.Empty<float>();

				// Token: 0x1700022C RID: 556
				// (get) Token: 0x060007ED RID: 2029 RVA: 0x00026FC9 File Offset: 0x000251C9
				// (set) Token: 0x060007EE RID: 2030 RVA: 0x00026FD1 File Offset: 0x000251D1
				public float[] RawFeatures { get; set; } = Array.Empty<float>();

				// Token: 0x1700022D RID: 557
				// (get) Token: 0x060007EF RID: 2031 RVA: 0x00026FDA File Offset: 0x000251DA
				// (set) Token: 0x060007F0 RID: 2032 RVA: 0x00026FE2 File Offset: 0x000251E2
				public int Decision { get; set; }

				// Token: 0x1700022E RID: 558
				// (get) Token: 0x060007F1 RID: 2033 RVA: 0x00026FEB File Offset: 0x000251EB
				// (set) Token: 0x060007F2 RID: 2034 RVA: 0x00026FF3 File Offset: 0x000251F3
				public float[] Attention { get; set; } = Array.Empty<float>();

				// Token: 0x1700022F RID: 559
				// (get) Token: 0x060007F3 RID: 2035 RVA: 0x00026FFC File Offset: 0x000251FC
				// (set) Token: 0x060007F4 RID: 2036 RVA: 0x00027004 File Offset: 0x00025204
				public float[] Probabilities { get; set; } = Array.Empty<float>();

				// Token: 0x17000230 RID: 560
				// (get) Token: 0x060007F5 RID: 2037 RVA: 0x0002700D File Offset: 0x0002520D
				// (set) Token: 0x060007F6 RID: 2038 RVA: 0x00027015 File Offset: 0x00025215
				public long Timestamp { get; set; }

				// Token: 0x17000231 RID: 561
				// (get) Token: 0x060007F7 RID: 2039 RVA: 0x0002701E File Offset: 0x0002521E
				// (set) Token: 0x060007F8 RID: 2040 RVA: 0x00027026 File Offset: 0x00025226
				public float Reward { get; set; }

				// Token: 0x17000232 RID: 562
				// (get) Token: 0x060007F9 RID: 2041 RVA: 0x0002702F File Offset: 0x0002522F
				// (set) Token: 0x060007FA RID: 2042 RVA: 0x00027037 File Offset: 0x00025237
				public bool HasReward { get; set; }
			}
		}

		// Token: 0x02000069 RID: 105
		public class ThreadPriorityMapper
		{
			// Token: 0x06000365 RID: 869 RVA: 0x00020B68 File Offset: 0x0001ED68
			public static int GetFinalPriority(int priorityClass, int threadPriority)
			{
				int num;
				if (!Service1.ThreadPriorityMapper.PriorityClassIndex.TryGetValue((Service1.ThreadPriorityMapper.PriorityClass)priorityClass, out num))
				{
					num = 2;
				}
				int num2;
				if (!Service1.ThreadPriorityMapper.ThreadPriorityIndex.TryGetValue((Service1.ThreadPriorityMapper.ThreadPriorityLevel)threadPriority, out num2))
				{
					num2 = 3;
				}
				return Service1.ThreadPriorityMapper.PriorityMatrix[num2, num];
			}

			// Token: 0x06000366 RID: 870 RVA: 0x00020BA7 File Offset: 0x0001EDA7
			public static Service1.ThreadPriorityMapper.PriorityClass GetPriorityClass(int value)
			{
				return (Service1.ThreadPriorityMapper.PriorityClass)value;
			}

			// Token: 0x06000367 RID: 871 RVA: 0x00020BAA File Offset: 0x0001EDAA
			public static Service1.ThreadPriorityMapper.ThreadPriorityLevel GetThreadPriority(int value)
			{
				return (Service1.ThreadPriorityMapper.ThreadPriorityLevel)value;
			}

			// Token: 0x040004CA RID: 1226
			private static readonly int[,] PriorityMatrix = new int[,]
			{
				{ 15, 15, 15, 15, 15, 31 },
				{ 6, 8, 10, 12, 15, 26 },
				{ 5, 7, 9, 11, 14, 25 },
				{ 4, 6, 8, 10, 13, 24 },
				{ 3, 5, 7, 9, 12, 23 },
				{ 2, 4, 6, 8, 11, 22 },
				{ 1, 1, 1, 1, 1, 16 }
			};

			// Token: 0x040004CB RID: 1227
			private static readonly Dictionary<Service1.ThreadPriorityMapper.PriorityClass, int> PriorityClassIndex = new Dictionary<Service1.ThreadPriorityMapper.PriorityClass, int>
			{
				{
					Service1.ThreadPriorityMapper.PriorityClass.Idle,
					0
				},
				{
					Service1.ThreadPriorityMapper.PriorityClass.BelowNormal,
					1
				},
				{
					Service1.ThreadPriorityMapper.PriorityClass.Normal,
					2
				},
				{
					Service1.ThreadPriorityMapper.PriorityClass.AboveNormal,
					3
				},
				{
					Service1.ThreadPriorityMapper.PriorityClass.High,
					4
				},
				{
					Service1.ThreadPriorityMapper.PriorityClass.Realtime,
					5
				}
			};

			// Token: 0x040004CC RID: 1228
			private static readonly Dictionary<Service1.ThreadPriorityMapper.ThreadPriorityLevel, int> ThreadPriorityIndex = new Dictionary<Service1.ThreadPriorityMapper.ThreadPriorityLevel, int>
			{
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.TimeCritical,
					0
				},
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.Highest,
					1
				},
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.AboveNormal,
					2
				},
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.Normal,
					3
				},
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.BelowNormal,
					4
				},
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.Lowest,
					5
				},
				{
					Service1.ThreadPriorityMapper.ThreadPriorityLevel.Idle,
					6
				}
			};

			// Token: 0x020000A4 RID: 164
			public enum PriorityClass
			{
				// Token: 0x04000767 RID: 1895
				Idle = 64,
				// Token: 0x04000768 RID: 1896
				BelowNormal = 16384,
				// Token: 0x04000769 RID: 1897
				Normal = 32,
				// Token: 0x0400076A RID: 1898
				AboveNormal = 32768,
				// Token: 0x0400076B RID: 1899
				High = 128,
				// Token: 0x0400076C RID: 1900
				Realtime = 4096
			}

			// Token: 0x020000A5 RID: 165
			public enum ThreadPriorityLevel
			{
				// Token: 0x0400076E RID: 1902
				Idle = -15,
				// Token: 0x0400076F RID: 1903
				Lowest = -2,
				// Token: 0x04000770 RID: 1904
				BelowNormal,
				// Token: 0x04000771 RID: 1905
				Normal,
				// Token: 0x04000772 RID: 1906
				AboveNormal,
				// Token: 0x04000773 RID: 1907
				Highest,
				// Token: 0x04000774 RID: 1908
				TimeCritical = 15
			}
		}

		// Token: 0x0200006A RID: 106
		public class ThreadData
		{
			// Token: 0x1700004D RID: 77
			// (get) Token: 0x0600036A RID: 874 RVA: 0x00020C6D File Offset: 0x0001EE6D
			// (set) Token: 0x0600036B RID: 875 RVA: 0x00020C75 File Offset: 0x0001EE75
			public int ThreadId { get; set; }

			// Token: 0x1700004E RID: 78
			// (get) Token: 0x0600036C RID: 876 RVA: 0x00020C7E File Offset: 0x0001EE7E
			// (set) Token: 0x0600036D RID: 877 RVA: 0x00020C86 File Offset: 0x0001EE86
			public long InstructionCount { get; set; }

			// Token: 0x1700004F RID: 79
			// (get) Token: 0x0600036E RID: 878 RVA: 0x00020C8F File Offset: 0x0001EE8F
			// (set) Token: 0x0600036F RID: 879 RVA: 0x00020C97 File Offset: 0x0001EE97
			public double MemoryAccessFrequency { get; set; }

			// Token: 0x17000050 RID: 80
			// (get) Token: 0x06000370 RID: 880 RVA: 0x00020CA0 File Offset: 0x0001EEA0
			// (set) Token: 0x06000371 RID: 881 RVA: 0x00020CA8 File Offset: 0x0001EEA8
			public double BranchMispredictionRate { get; set; }

			// Token: 0x17000051 RID: 81
			// (get) Token: 0x06000372 RID: 882 RVA: 0x00020CB1 File Offset: 0x0001EEB1
			// (set) Token: 0x06000373 RID: 883 RVA: 0x00020CB9 File Offset: 0x0001EEB9
			public double Ipc { get; set; }

			// Token: 0x17000052 RID: 82
			// (get) Token: 0x06000374 RID: 884 RVA: 0x00020CC2 File Offset: 0x0001EEC2
			// (set) Token: 0x06000375 RID: 885 RVA: 0x00020CCA File Offset: 0x0001EECA
			public long Timestamp { get; set; }
		}

		// Token: 0x0200006B RID: 107
		public enum ThreadDimension
		{
			// Token: 0x040004D4 RID: 1236
			InstructionCount,
			// Token: 0x040004D5 RID: 1237
			MemoryAccessFrequency,
			// Token: 0x040004D6 RID: 1238
			BranchMispredictionRate,
			// Token: 0x040004D7 RID: 1239
			Ipc
		}

		// Token: 0x0200006C RID: 108
		public class ThreadClassifier
		{
			// Token: 0x06000377 RID: 887 RVA: 0x00020CDB File Offset: 0x0001EEDB
			public ThreadClassifier()
			{
				this._threadData = new Dictionary<int, Service1.ThreadData>();
			}

			// Token: 0x06000378 RID: 888 RVA: 0x00020CF0 File Offset: 0x0001EEF0
			public void AddThread(Service1.ThreadData thread)
			{
				if (thread == null)
				{
					return;
				}
				this.CleanExpiredData();
				thread.Timestamp = DateTime.Now.Ticks;
				if (this._threadData.ContainsKey(thread.ThreadId))
				{
					this._threadData[thread.ThreadId] = thread;
					return;
				}
				if (this._threadData.Count >= 3000)
				{
					this.RemoveOldestData();
				}
				this._threadData[thread.ThreadId] = thread;
			}

			// Token: 0x06000379 RID: 889 RVA: 0x00020D6C File Offset: 0x0001EF6C
			public int IsAboveThreshold(int threadId, int dimension, bool useQuartile = true)
			{
				this.CleanExpiredData();
				if (!this._threadData.ContainsKey(threadId))
				{
					return 0;
				}
				if (dimension < 0 || dimension > 3)
				{
					return 0;
				}
				Service1.ThreadData threadData = this._threadData[threadId];
				double dimensionValue = this.GetDimensionValue(threadData, dimension);
				double num = (useQuartile ? this.CalculateQuartile(dimension) : this.CalculateMedian(dimension));
				return (dimensionValue > num) ? 1 : 0;
			}

			// Token: 0x0600037A RID: 890 RVA: 0x00020DC5 File Offset: 0x0001EFC5
			private double GetDimensionValue(Service1.ThreadData thread, int dimension)
			{
				switch (dimension)
				{
				case 0:
					return (double)thread.InstructionCount;
				case 1:
					return thread.MemoryAccessFrequency;
				case 2:
					return thread.BranchMispredictionRate;
				case 3:
					return thread.Ipc;
				default:
					return 0.0;
				}
			}

			// Token: 0x0600037B RID: 891 RVA: 0x00020E08 File Offset: 0x0001F008
			private double CalculateMedian(int dimension)
			{
				List<double> list = (from t in this._threadData.Values
					select this.GetDimensionValue(t, dimension) into v
					orderby v
					select v).ToList<double>();
				if (list.Count == 0)
				{
					return 0.0;
				}
				int count = list.Count;
				int num = count / 2;
				if (count % 2 == 0)
				{
					return (list[num - 1] + list[num]) / 2.0;
				}
				return list[num];
			}

			// Token: 0x0600037C RID: 892 RVA: 0x00020EB4 File Offset: 0x0001F0B4
			private double CalculateQuartile(int dimension)
			{
				List<double> list = (from t in this._threadData.Values
					select this.GetDimensionValue(t, dimension) into v
					orderby v
					select v).ToList<double>();
				if (list.Count == 0)
				{
					return 0.0;
				}
				int num = (int)((double)list.Count * 0.75);
				if (num >= list.Count)
				{
					num = list.Count - 1;
				}
				return list[num];
			}

			// Token: 0x0600037D RID: 893 RVA: 0x00020F5C File Offset: 0x0001F15C
			private void CleanExpiredData()
			{
				long currentTime = DateTime.Now.Ticks;
				foreach (int num in (from kvp in this._threadData
					where currentTime - kvp.Value.Timestamp > 300000000L
					select kvp.Key).ToList<int>())
				{
					this._threadData.Remove(num);
				}
			}

			// Token: 0x0600037E RID: 894 RVA: 0x00021008 File Offset: 0x0001F208
			private void RemoveOldestData()
			{
				if (this._threadData.Count == 0)
				{
					return;
				}
				KeyValuePair<int, Service1.ThreadData> keyValuePair = this._threadData.OrderBy((KeyValuePair<int, Service1.ThreadData> kvp) => kvp.Value.Timestamp).First<KeyValuePair<int, Service1.ThreadData>>();
				this._threadData.Remove(keyValuePair.Key);
			}

			// Token: 0x0600037F RID: 895 RVA: 0x00021066 File Offset: 0x0001F266
			public int GetThreadCount()
			{
				return this._threadData.Count;
			}

			// Token: 0x040004D8 RID: 1240
			private const int MaxCapacity = 3000;

			// Token: 0x040004D9 RID: 1241
			private const long TtlTicks = 300000000L;

			// Token: 0x040004DA RID: 1242
			private Dictionary<int, Service1.ThreadData> _threadData;
		}

		// Token: 0x0200006D RID: 109
		public class ThreadPerformanceData
		{
			// Token: 0x17000053 RID: 83
			// (get) Token: 0x06000380 RID: 896 RVA: 0x00021073 File Offset: 0x0001F273
			// (set) Token: 0x06000381 RID: 897 RVA: 0x0002107B File Offset: 0x0001F27B
			public long InstructionCount { get; set; }

			// Token: 0x17000054 RID: 84
			// (get) Token: 0x06000382 RID: 898 RVA: 0x00021084 File Offset: 0x0001F284
			// (set) Token: 0x06000383 RID: 899 RVA: 0x0002108C File Offset: 0x0001F28C
			public double MemoryAccessFrequency { get; set; }

			// Token: 0x17000055 RID: 85
			// (get) Token: 0x06000384 RID: 900 RVA: 0x00021095 File Offset: 0x0001F295
			// (set) Token: 0x06000385 RID: 901 RVA: 0x0002109D File Offset: 0x0001F29D
			public double BranchMispredictRate { get; set; }

			// Token: 0x17000056 RID: 86
			// (get) Token: 0x06000386 RID: 902 RVA: 0x000210A6 File Offset: 0x0001F2A6
			// (set) Token: 0x06000387 RID: 903 RVA: 0x000210AE File Offset: 0x0001F2AE
			public double IPC { get; set; }

			// Token: 0x17000057 RID: 87
			// (get) Token: 0x06000388 RID: 904 RVA: 0x000210B7 File Offset: 0x0001F2B7
			// (set) Token: 0x06000389 RID: 905 RVA: 0x000210BF File Offset: 0x0001F2BF
			public long Timestamp { get; set; }
		}

		// Token: 0x0200006E RID: 110
		public class ThreadDataCollector
		{
			// Token: 0x0600038B RID: 907 RVA: 0x000210D0 File Offset: 0x0001F2D0
			public void AddData(long instructionCount, double memoryAccessFrequency, double branchMispredictRate, double ipc)
			{
				Service1.ThreadPerformanceData threadPerformanceData = new Service1.ThreadPerformanceData
				{
					InstructionCount = instructionCount,
					MemoryAccessFrequency = memoryAccessFrequency,
					BranchMispredictRate = branchMispredictRate,
					IPC = ipc,
					Timestamp = DateTime.Now.Ticks
				};
				this._dataList.Add(threadPerformanceData);
				while (this._dataList.Count > 200)
				{
					this._dataList.RemoveAt(0);
				}
			}

			// Token: 0x0600038C RID: 908 RVA: 0x00021140 File Offset: 0x0001F340
			public int GetMostInfluentialVariableIndex()
			{
				if (this._dataList.Count < 2)
				{
					return -1;
				}
				double[] array = this._dataList.Select((Service1.ThreadPerformanceData d) => d.IPC).ToArray<double>();
				double[] array2 = this._dataList.Select((Service1.ThreadPerformanceData d) => (double)d.InstructionCount).ToArray<double>();
				double[] array3 = this._dataList.Select((Service1.ThreadPerformanceData d) => d.MemoryAccessFrequency).ToArray<double>();
				double[] array4 = this._dataList.Select((Service1.ThreadPerformanceData d) => d.BranchMispredictRate).ToArray<double>();
				double num = Math.Abs(this.CalculatePearsonCorrelation(array2, array));
				double num2 = Math.Abs(this.CalculatePearsonCorrelation(array3, array));
				double num3 = Math.Abs(this.CalculatePearsonCorrelation(array4, array));
				if (num >= num2 && num >= num3)
				{
					return 0;
				}
				if (num2 >= num && num2 >= num3)
				{
					return 1;
				}
				return 2;
			}

			// Token: 0x0600038D RID: 909 RVA: 0x00021268 File Offset: 0x0001F468
			private double CalculatePearsonCorrelation(double[] x, double[] y)
			{
				if (x.Length != y.Length || x.Length < 2)
				{
					return 0.0;
				}
				int num = x.Length;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				double num6 = 0.0;
				for (int i = 0; i < num; i++)
				{
					num2 += x[i];
					num3 += y[i];
					num4 += x[i] * y[i];
					num5 += x[i] * x[i];
					num6 += y[i] * y[i];
				}
				double num7 = (double)num * num4 - num2 * num3;
				double num8 = Math.Sqrt(((double)num * num5 - num2 * num2) * ((double)num * num6 - num3 * num3));
				if (num8 != 0.0)
				{
					return num7 / num8;
				}
				return 0.0;
			}

			// Token: 0x0600038E RID: 910 RVA: 0x0002134B File Offset: 0x0001F54B
			public int GetDataCount()
			{
				return this._dataList.Count;
			}

			// Token: 0x0600038F RID: 911 RVA: 0x00021358 File Offset: 0x0001F558
			public void Clear()
			{
				this._dataList.Clear();
			}

			// Token: 0x06000390 RID: 912 RVA: 0x00021365 File Offset: 0x0001F565
			public Service1.ThreadPerformanceData GetData(int index)
			{
				if (index >= 0 && index < this._dataList.Count)
				{
					return this._dataList[index];
				}
				return null;
			}

			// Token: 0x06000391 RID: 913 RVA: 0x00021387 File Offset: 0x0001F587
			public IReadOnlyList<Service1.ThreadPerformanceData> GetAllData()
			{
				return this._dataList.AsReadOnly();
			}

			// Token: 0x040004E0 RID: 1248
			private const int MaxDataCount = 200;

			// Token: 0x040004E1 RID: 1249
			private readonly List<Service1.ThreadPerformanceData> _dataList = new List<Service1.ThreadPerformanceData>();
		}

		// Token: 0x0200006F RID: 111
		public class CausalityAnalyzer
		{
			// Token: 0x06000393 RID: 915 RVA: 0x000213A8 File Offset: 0x0001F5A8
			public CausalityAnalyzer(int dimension)
			{
				if (dimension < 3)
				{
					throw new ArgumentException("维度必须至少为3（data1, data2和至少一个控制变量）");
				}
				this._dimension = dimension;
				this._dataPoints = new List<double[]>();
				this._coreFlags = new List<int>();
				this._cachedCorrelation = 0.0;
				this._cachedElasticity = 0.0;
				this._cacheValid = false;
			}

			// Token: 0x06000394 RID: 916 RVA: 0x0002140C File Offset: 0x0001F60C
			public bool AddDataPoint(int coreFlag, params double[] dataValues)
			{
				if (coreFlag != 0 && coreFlag != 1)
				{
					return false;
				}
				if (dataValues == null || dataValues.Length != this._dimension)
				{
					return false;
				}
				double[] array = new double[this._dimension];
				Array.Copy(dataValues, array, this._dimension);
				this._dataPoints.Add(array);
				this._coreFlags.Add(coreFlag);
				if (this._dataPoints.Count > 30)
				{
					this._dataPoints.RemoveAt(0);
					this._coreFlags.RemoveAt(0);
				}
				this._cacheValid = false;
				return true;
			}

			// Token: 0x06000395 RID: 917 RVA: 0x00021492 File Offset: 0x0001F692
			public double GetPartialCorrelation()
			{
				if (this._dataPoints.Count < 10)
				{
					return 0.0;
				}
				if (!this._cacheValid)
				{
					this.UpdateCache();
				}
				return this._cachedCorrelation;
			}

			// Token: 0x06000396 RID: 918 RVA: 0x000214C1 File Offset: 0x0001F6C1
			public double GetElasticity()
			{
				if (this._dataPoints.Count < 10)
				{
					return 0.0;
				}
				if (!this._cacheValid)
				{
					this.UpdateCache();
				}
				return this._cachedElasticity;
			}

			// Token: 0x06000397 RID: 919 RVA: 0x000214F0 File Offset: 0x0001F6F0
			public void Clear()
			{
				this._dataPoints.Clear();
				this._coreFlags.Clear();
				this._cacheValid = false;
			}

			// Token: 0x17000058 RID: 88
			// (get) Token: 0x06000398 RID: 920 RVA: 0x0002150F File Offset: 0x0001F70F
			public int DataPointCount
			{
				get
				{
					return this._dataPoints.Count;
				}
			}

			// Token: 0x17000059 RID: 89
			// (get) Token: 0x06000399 RID: 921 RVA: 0x0002151C File Offset: 0x0001F71C
			public int Dimension
			{
				get
				{
					return this._dimension;
				}
			}

			// Token: 0x0600039A RID: 922 RVA: 0x00021524 File Offset: 0x0001F724
			private void UpdateCache()
			{
				this._cachedCorrelation = this.CalculatePartialCorrelation();
				this._cachedElasticity = this.CalculatePartialElasticity();
				this._cacheValid = true;
			}

			// Token: 0x0600039B RID: 923 RVA: 0x00021548 File Offset: 0x0001F748
			private double CalculatePartialCorrelation()
			{
				int count = this._dataPoints.Count;
				int num = this._dimension - 2 + 1;
				double[] array = new double[count];
				double[] array2 = new double[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = this._dataPoints[i][0];
					array2[i] = this._dataPoints[i][1];
				}
				double[,] array3 = new double[count, num + 1];
				for (int j = 0; j < count; j++)
				{
					array3[j, 0] = 1.0;
					array3[j, 1] = (double)this._coreFlags[j];
					for (int k = 0; k < num - 1; k++)
					{
						array3[j, k + 2] = this._dataPoints[j][k + 2];
					}
				}
				double[] residuals = this.GetResiduals(array, array3);
				double[] residuals2 = this.GetResiduals(array2, array3);
				return this.CalculateCorrelationFromResiduals(residuals, residuals2);
			}

			// Token: 0x0600039C RID: 924 RVA: 0x00021648 File Offset: 0x0001F848
			private double CalculatePartialElasticity()
			{
				int count = this._dataPoints.Count;
				int num = this._dimension - 2 + 1;
				double[] array = new double[count];
				double[] array2 = new double[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = this._dataPoints[i][0];
					array2[i] = this._dataPoints[i][1];
				}
				double[,] array3 = new double[count, num + 1];
				for (int j = 0; j < count; j++)
				{
					array3[j, 0] = 1.0;
					array3[j, 1] = (double)this._coreFlags[j];
					for (int k = 0; k < num - 1; k++)
					{
						array3[j, k + 2] = this._dataPoints[j][k + 2];
					}
				}
				double[] residuals = this.GetResiduals(array, array3);
				double[] residuals2 = this.GetResiduals(array2, array3);
				double num2 = 0.0;
				double num3 = 0.0;
				for (int l = 0; l < count; l++)
				{
					num2 += this._dataPoints[l][0];
					num3 += this._dataPoints[l][1];
				}
				num2 /= (double)count;
				num3 /= (double)count;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int m = 0; m < count; m++)
				{
					num4 += residuals[m] * residuals2[m];
					num5 += residuals[m] * residuals[m];
				}
				if (num2 == 0.0 || num5 == 0.0)
				{
					return 0.0;
				}
				return num4 / num5 * (num2 / num3);
			}

			// Token: 0x0600039D RID: 925 RVA: 0x00021810 File Offset: 0x0001FA10
			private double[] GetResiduals(double[] y, double[,] X)
			{
				int num = y.Length;
				int length = X.GetLength(1);
				double[,] array = new double[length, length];
				double[] array2 = new double[length];
				for (int i = 0; i < length; i++)
				{
					for (int j = 0; j < length; j++)
					{
						double num2 = 0.0;
						for (int k = 0; k < num; k++)
						{
							num2 += X[k, i] * X[k, j];
						}
						array[i, j] = num2;
					}
					double num3 = 0.0;
					for (int l = 0; l < num; l++)
					{
						num3 += X[l, i] * y[l];
					}
					array2[i] = num3;
				}
				double[] array3 = this.SolveLinearSystem(array, array2, length);
				double[] array4 = new double[num];
				for (int m = 0; m < num; m++)
				{
					double num4 = 0.0;
					for (int n = 0; n < length; n++)
					{
						num4 += X[m, n] * array3[n];
					}
					array4[m] = y[m] - num4;
				}
				return array4;
			}

			// Token: 0x0600039E RID: 926 RVA: 0x00021938 File Offset: 0x0001FB38
			private double[] SolveLinearSystem(double[,] A, double[] b, int n)
			{
				for (int i = 0; i < n - 1; i++)
				{
					int num = i;
					double num2 = Math.Abs(A[i, i]);
					for (int j = i + 1; j < n; j++)
					{
						if (Math.Abs(A[j, i]) > num2)
						{
							num = j;
							num2 = Math.Abs(A[j, i]);
						}
					}
					if (num2 < 1E-10)
					{
						return new double[n];
					}
					if (num != i)
					{
						for (int k = i; k < n; k++)
						{
							double num3 = A[i, k];
							A[i, k] = A[num, k];
							A[num, k] = num3;
						}
						double num4 = b[i];
						b[i] = b[num];
						b[num] = num4;
					}
					for (int l = i + 1; l < n; l++)
					{
						double num5 = A[l, i] / A[i, i];
						for (int m = i; m < n; m++)
						{
							A[l, m] -= num5 * A[i, m];
						}
						b[l] -= num5 * b[i];
					}
				}
				double[] array = new double[n];
				for (int num6 = n - 1; num6 >= 0; num6--)
				{
					double num7 = b[num6];
					for (int num8 = num6 + 1; num8 < n; num8++)
					{
						num7 -= A[num6, num8] * array[num8];
					}
					array[num6] = num7 / A[num6, num6];
				}
				return array;
			}

			// Token: 0x0600039F RID: 927 RVA: 0x00021AB8 File Offset: 0x0001FCB8
			private double CalculateCorrelationFromResiduals(double[] r1, double[] r2)
			{
				int num = r1.Length;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				double num6 = 0.0;
				for (int i = 0; i < num; i++)
				{
					num2 += r1[i];
					num3 += r2[i];
					num4 += r1[i] * r2[i];
					num5 += r1[i] * r1[i];
					num6 += r2[i] * r2[i];
				}
				double num7 = (double)num * num4 - num2 * num3;
				double num8 = Math.Sqrt(((double)num * num5 - num2 * num2) * ((double)num * num6 - num3 * num3));
				if (num8 == 0.0)
				{
					return 0.0;
				}
				return num7 / num8;
			}

			// Token: 0x060003A0 RID: 928 RVA: 0x00021B84 File Offset: 0x0001FD84
			private double CalculatePearsonCorrelation(int index1, int index2)
			{
				int count = this._dataPoints.Count;
				double num = 0.0;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int i = 0; i < count; i++)
				{
					double num6 = this._dataPoints[i][index1];
					double num7 = this._dataPoints[i][index2];
					num += num6;
					num2 += num7;
					num3 += num6 * num7;
					num4 += num6 * num6;
					num5 += num7 * num7;
				}
				double num8 = (double)count * num3 - num * num2;
				double num9 = Math.Sqrt(((double)count * num4 - num * num) * ((double)count * num5 - num2 * num2));
				if (num9 == 0.0)
				{
					return 0.0;
				}
				return num8 / num9;
			}

			// Token: 0x040004E2 RID: 1250
			private const int MinDataPoints = 10;

			// Token: 0x040004E3 RID: 1251
			private const int MaxDataPoints = 30;

			// Token: 0x040004E4 RID: 1252
			private List<double[]> _dataPoints;

			// Token: 0x040004E5 RID: 1253
			private List<int> _coreFlags;

			// Token: 0x040004E6 RID: 1254
			private readonly int _dimension;

			// Token: 0x040004E7 RID: 1255
			private double _cachedCorrelation;

			// Token: 0x040004E8 RID: 1256
			private double _cachedElasticity;

			// Token: 0x040004E9 RID: 1257
			private bool _cacheValid;
		}

		// Token: 0x02000070 RID: 112
		public class NumberProcessor
		{
			// Token: 0x060003A1 RID: 929 RVA: 0x00021C69 File Offset: 0x0001FE69
			public NumberProcessor()
			{
				this._numbers = new List<long>();
			}

			// Token: 0x060003A2 RID: 930 RVA: 0x00021C7C File Offset: 0x0001FE7C
			public void AddData(long data)
			{
				this._numbers.Add(data);
			}

			// Token: 0x060003A3 RID: 931 RVA: 0x00021C8A File Offset: 0x0001FE8A
			public void AddDataRange(IEnumerable<long> dataList)
			{
				this._numbers.AddRange(dataList);
			}

			// Token: 0x060003A4 RID: 932 RVA: 0x00021C98 File Offset: 0x0001FE98
			public long GetMax()
			{
				if (this._numbers.Count == 0)
				{
					return -1L;
				}
				return this._numbers.Max();
			}

			// Token: 0x060003A5 RID: 933 RVA: 0x00021CB5 File Offset: 0x0001FEB5
			public void Clear()
			{
				this._numbers.Clear();
			}

			// Token: 0x1700005A RID: 90
			// (get) Token: 0x060003A6 RID: 934 RVA: 0x00021CC2 File Offset: 0x0001FEC2
			public int Count
			{
				get
				{
					return this._numbers.Count;
				}
			}

			// Token: 0x040004EA RID: 1258
			private readonly List<long> _numbers;
		}

		// Token: 0x02000071 RID: 113
		public class DataLinkageAnalyzer
		{
			// Token: 0x1700005B RID: 91
			// (get) Token: 0x060003A7 RID: 935 RVA: 0x00021CCF File Offset: 0x0001FECF
			public int DataCount
			{
				get
				{
					return this._data1Queue.Count;
				}
			}

			// Token: 0x1700005C RID: 92
			// (get) Token: 0x060003A8 RID: 936 RVA: 0x00021CDC File Offset: 0x0001FEDC
			public bool CanAnalyze
			{
				get
				{
					return this.DataCount >= this._windowSize;
				}
			}

			// Token: 0x060003A9 RID: 937 RVA: 0x00021CEF File Offset: 0x0001FEEF
			public DataLinkageAnalyzer(int windowSize = 5)
			{
				if (windowSize <= 0)
				{
					throw new ArgumentException("窗口大小必须大于0", "windowSize");
				}
				this._windowSize = windowSize;
				this._data1Queue = new Queue<double>(windowSize + 1);
				this._data2Queue = new Queue<double>(windowSize + 1);
			}

			// Token: 0x060003AA RID: 938 RVA: 0x00021D30 File Offset: 0x0001FF30
			public void AddData(double data1, double data2)
			{
				this._data1Queue.Enqueue(data1);
				this._data2Queue.Enqueue(data2);
				if (this._data1Queue.Count > this._windowSize)
				{
					this._data1Queue.Dequeue();
					this._data2Queue.Dequeue();
				}
			}

			// Token: 0x060003AB RID: 939 RVA: 0x00021D80 File Offset: 0x0001FF80
			public Service1.LinkageAnalysisResult Analyze()
			{
				if (!this.CanAnalyze)
				{
					throw new InvalidOperationException(string.Format("需要至少{0}个数据点才能进行分析，当前只有{1}个", this._windowSize, this.DataCount));
				}
				double[] array = this._data1Queue.ToArray();
				double[] array2 = this._data2Queue.ToArray();
				return new Service1.LinkageAnalysisResult
				{
					CorrelationCoefficient = this.CalculatePearsonCorrelation(array, array2),
					Covariance = this.CalculateCovariance(array, array2),
					Data1Mean = array.Average(),
					Data2Mean = array2.Average(),
					Data1StdDev = this.CalculateStandardDeviation(array),
					Data2StdDev = this.CalculateStandardDeviation(array2),
					AnalysisWindowSize = this._windowSize,
					Timestamp = DateTime.Now
				};
			}

			// Token: 0x060003AC RID: 940 RVA: 0x00021E40 File Offset: 0x00020040
			private double CalculatePearsonCorrelation(double[] data1, double[] data2)
			{
				if (data1.Length != data2.Length)
				{
					throw new ArgumentException("两个数据数组长度必须相同");
				}
				double num = data1.Average();
				double num2 = data2.Average();
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int i = 0; i < data1.Length; i++)
				{
					double num6 = data1[i] - num;
					double num7 = data2[i] - num2;
					num3 += num6 * num7;
					num4 += num6 * num6;
					num5 += num7 * num7;
				}
				if (num4 == 0.0 || num5 == 0.0)
				{
					return 0.0;
				}
				return num3 / Math.Sqrt(num4 * num5);
			}

			// Token: 0x060003AD RID: 941 RVA: 0x00021EF8 File Offset: 0x000200F8
			private double CalculateCovariance(double[] data1, double[] data2)
			{
				if (data1.Length != data2.Length)
				{
					throw new ArgumentException("两个数据数组长度必须相同");
				}
				double num = data1.Average();
				double num2 = data2.Average();
				double num3 = 0.0;
				for (int i = 0; i < data1.Length; i++)
				{
					num3 += (data1[i] - num) * (data2[i] - num2);
				}
				return num3 / (double)data1.Length;
			}

			// Token: 0x060003AE RID: 942 RVA: 0x00021F54 File Offset: 0x00020154
			private double CalculateStandardDeviation(double[] data)
			{
				double mean = data.Average();
				return Math.Sqrt(data.Sum((double x) => Math.Pow(x - mean, 2.0)) / (double)data.Length);
			}

			// Token: 0x060003AF RID: 943 RVA: 0x00021F8F File Offset: 0x0002018F
			public void Clear()
			{
				this._data1Queue.Clear();
				this._data2Queue.Clear();
			}

			// Token: 0x060003B0 RID: 944 RVA: 0x00021FA7 File Offset: 0x000201A7
			[return: TupleElementNames(new string[] { "data1", "data2" })]
			public ValueTuple<double[], double[]> GetCurrentWindowData()
			{
				return new ValueTuple<double[], double[]>(this._data1Queue.ToArray(), this._data2Queue.ToArray());
			}

			// Token: 0x060003B1 RID: 945 RVA: 0x00021FC4 File Offset: 0x000201C4
			public int GetLinkageStrengthValue()
			{
				if (!this.CanAnalyze)
				{
					return -1;
				}
				return (Math.Abs(this.CalculatePearsonCorrelation(this._data1Queue.ToArray(), this._data2Queue.ToArray())) > 0.5) ? 1 : 0;
			}

			// Token: 0x060003B2 RID: 946 RVA: 0x00021FFC File Offset: 0x000201FC
			public double GetElasticity()
			{
				if (!this.CanAnalyze)
				{
					return 0.0;
				}
				double[] array = this._data1Queue.ToArray();
				double[] array2 = this._data2Queue.ToArray();
				double num = array.Average();
				double num2 = array2.Average();
				if (num == 0.0 || num2 == 0.0)
				{
					return 0.0;
				}
				double num3 = this.CalculateStandardDeviation(array) / num;
				double num4 = this.CalculateStandardDeviation(array2) / num2;
				double num5 = this.CalculatePearsonCorrelation(array, array2);
				return num4 / num3 * num5;
			}

			// Token: 0x060003B3 RID: 947 RVA: 0x00022088 File Offset: 0x00020288
			public string GetElasticityDescription()
			{
				double num = this.GetElasticity() * 1.0;
				string text = ((num >= 0.0) ? "增加" : "减少");
				return string.Format("当data1增加1%时，data2预计{0}{1:F2}%", text, Math.Abs(num));
			}

			// Token: 0x040004EB RID: 1259
			private readonly int _windowSize;

			// Token: 0x040004EC RID: 1260
			private readonly Queue<double> _data1Queue;

			// Token: 0x040004ED RID: 1261
			private readonly Queue<double> _data2Queue;
		}

		// Token: 0x02000072 RID: 114
		public class LinkageAnalysisResult
		{
			// Token: 0x1700005D RID: 93
			// (get) Token: 0x060003B4 RID: 948 RVA: 0x000220D5 File Offset: 0x000202D5
			// (set) Token: 0x060003B5 RID: 949 RVA: 0x000220DD File Offset: 0x000202DD
			public double CorrelationCoefficient { get; set; }

			// Token: 0x1700005E RID: 94
			// (get) Token: 0x060003B6 RID: 950 RVA: 0x000220E6 File Offset: 0x000202E6
			// (set) Token: 0x060003B7 RID: 951 RVA: 0x000220EE File Offset: 0x000202EE
			public double Covariance { get; set; }

			// Token: 0x1700005F RID: 95
			// (get) Token: 0x060003B8 RID: 952 RVA: 0x000220F7 File Offset: 0x000202F7
			// (set) Token: 0x060003B9 RID: 953 RVA: 0x000220FF File Offset: 0x000202FF
			public double Data1Mean { get; set; }

			// Token: 0x17000060 RID: 96
			// (get) Token: 0x060003BA RID: 954 RVA: 0x00022108 File Offset: 0x00020308
			// (set) Token: 0x060003BB RID: 955 RVA: 0x00022110 File Offset: 0x00020310
			public double Data2Mean { get; set; }

			// Token: 0x17000061 RID: 97
			// (get) Token: 0x060003BC RID: 956 RVA: 0x00022119 File Offset: 0x00020319
			// (set) Token: 0x060003BD RID: 957 RVA: 0x00022121 File Offset: 0x00020321
			public double Data1StdDev { get; set; }

			// Token: 0x17000062 RID: 98
			// (get) Token: 0x060003BE RID: 958 RVA: 0x0002212A File Offset: 0x0002032A
			// (set) Token: 0x060003BF RID: 959 RVA: 0x00022132 File Offset: 0x00020332
			public double Data2StdDev { get; set; }

			// Token: 0x17000063 RID: 99
			// (get) Token: 0x060003C0 RID: 960 RVA: 0x0002213B File Offset: 0x0002033B
			// (set) Token: 0x060003C1 RID: 961 RVA: 0x00022143 File Offset: 0x00020343
			public int AnalysisWindowSize { get; set; }

			// Token: 0x17000064 RID: 100
			// (get) Token: 0x060003C2 RID: 962 RVA: 0x0002214C File Offset: 0x0002034C
			// (set) Token: 0x060003C3 RID: 963 RVA: 0x00022154 File Offset: 0x00020354
			public DateTime Timestamp { get; set; }

			// Token: 0x060003C4 RID: 964 RVA: 0x00022160 File Offset: 0x00020360
			public string GetLinkageStrength()
			{
				double num = Math.Abs(this.CorrelationCoefficient);
				if (num >= 0.8)
				{
					return "强联动";
				}
				if (num >= 0.5)
				{
					return "中等联动";
				}
				if (num >= 0.3)
				{
					return "弱联动";
				}
				return "无显著联动";
			}

			// Token: 0x060003C5 RID: 965 RVA: 0x000221B4 File Offset: 0x000203B4
			public string GetLinkageDirection()
			{
				if (this.CorrelationCoefficient > 0.0)
				{
					return "正相关";
				}
				if (this.CorrelationCoefficient < 0.0)
				{
					return "负相关";
				}
				return "无相关";
			}

			// Token: 0x060003C6 RID: 966 RVA: 0x000221EC File Offset: 0x000203EC
			public override string ToString()
			{
				return string.Concat(new string[]
				{
					"联动性分析结果:\n",
					string.Format("相关系数: {0:F4}\n", this.CorrelationCoefficient),
					string.Format("协方差: {0:F4}\n", this.Covariance),
					string.Format("Data1均值: {0:F4}, 标准差: {1:F4}\n", this.Data1Mean, this.Data1StdDev),
					string.Format("Data2均值: {0:F4}, 标准差: {1:F4}\n", this.Data2Mean, this.Data2StdDev),
					"联动强度: ",
					this.GetLinkageStrength(),
					"\n联动方向: ",
					this.GetLinkageDirection(),
					"\n",
					string.Format("分析时间: {0:yyyy-MM-dd HH:mm:ss}", this.Timestamp)
				});
			}
		}

		// Token: 0x02000073 RID: 115
		public class ElasticityResult
		{
			// Token: 0x17000065 RID: 101
			// (get) Token: 0x060003C8 RID: 968 RVA: 0x000222CF File Offset: 0x000204CF
			// (set) Token: 0x060003C9 RID: 969 RVA: 0x000222D7 File Offset: 0x000204D7
			public double Elasticity { get; set; }

			// Token: 0x17000066 RID: 102
			// (get) Token: 0x060003CA RID: 970 RVA: 0x000222E0 File Offset: 0x000204E0
			// (set) Token: 0x060003CB RID: 971 RVA: 0x000222E8 File Offset: 0x000204E8
			public double PercentageChange { get; set; }

			// Token: 0x17000067 RID: 103
			// (get) Token: 0x060003CC RID: 972 RVA: 0x000222F1 File Offset: 0x000204F1
			// (set) Token: 0x060003CD RID: 973 RVA: 0x000222F9 File Offset: 0x000204F9
			public string Direction { get; set; }

			// Token: 0x17000068 RID: 104
			// (get) Token: 0x060003CE RID: 974 RVA: 0x00022302 File Offset: 0x00020502
			// (set) Token: 0x060003CF RID: 975 RVA: 0x0002230A File Offset: 0x0002050A
			public string Description { get; set; }

			// Token: 0x17000069 RID: 105
			// (get) Token: 0x060003D0 RID: 976 RVA: 0x00022313 File Offset: 0x00020513
			// (set) Token: 0x060003D1 RID: 977 RVA: 0x0002231B File Offset: 0x0002051B
			public DateTime Timestamp { get; set; }

			// Token: 0x060003D2 RID: 978 RVA: 0x00022324 File Offset: 0x00020524
			public override string ToString()
			{
				return string.Concat(new string[]
				{
					"弹性分析结果:\n",
					string.Format("弹性系数: {0:F4}\n", this.Elasticity),
					string.Format("Data1增加1%时，Data2预计{0}{1:F2}%\n", this.Direction, Math.Abs(this.PercentageChange)),
					"描述: ",
					this.Description,
					"\n",
					string.Format("分析时间: {0:yyyy-MM-dd HH:mm:ss}", this.Timestamp)
				});
			}
		}

		// Token: 0x02000074 RID: 116
		public class ThreadExecutionRegistry
		{
			// Token: 0x060003D4 RID: 980 RVA: 0x000223B8 File Offset: 0x000205B8
			public void AddOrUpdate(int threadId, long execTime)
			{
				this._data[threadId] = execTime;
			}

			// Token: 0x1700006A RID: 106
			// (get) Token: 0x060003D5 RID: 981 RVA: 0x000223C7 File Offset: 0x000205C7
			public int Count
			{
				get
				{
					return this._data.Count;
				}
			}

			// Token: 0x060003D6 RID: 982 RVA: 0x000223D4 File Offset: 0x000205D4
			public void Clear()
			{
				this._data.Clear();
			}

			// Token: 0x060003D7 RID: 983 RVA: 0x000223E1 File Offset: 0x000205E1
			public IEnumerable<KeyValuePair<int, long>> GetAllData()
			{
				return this._data;
			}

			// Token: 0x040004FB RID: 1275
			public Dictionary<int, long> _data = new Dictionary<int, long>();
		}

		// Token: 0x02000075 RID: 117
		public class CoreEntry
		{
			// Token: 0x1700006B RID: 107
			// (get) Token: 0x060003D9 RID: 985 RVA: 0x000223FC File Offset: 0x000205FC
			public double Uti { get; }

			// Token: 0x1700006C RID: 108
			// (get) Token: 0x060003DA RID: 986 RVA: 0x00022404 File Offset: 0x00020604
			public int Cid { get; }

			// Token: 0x060003DB RID: 987 RVA: 0x0002240C File Offset: 0x0002060C
			public CoreEntry(double uti, int cid)
			{
				this.Uti = uti;
				this.Cid = cid;
			}
		}

		// Token: 0x02000076 RID: 118
		public class CoreEntryComparer : IComparer<Service1.CoreEntry>
		{
			// Token: 0x060003DC RID: 988 RVA: 0x00022424 File Offset: 0x00020624
			public int Compare(Service1.CoreEntry x, Service1.CoreEntry y)
			{
				int num = x.Uti.CompareTo(y.Uti);
				if (num != 0)
				{
					return num;
				}
				return x.Cid.CompareTo(y.Cid);
			}
		}

		// Token: 0x02000077 RID: 119
		public class CoreManager
		{
			// Token: 0x060003DE RID: 990 RVA: 0x00022468 File Offset: 0x00020668
			public CoreManager(int numCores)
			{
				this._coreSet = new SortedSet<Service1.CoreEntry>(this._comparer);
				this._currentEntries = new Service1.CoreEntry[numCores];
				for (int i = 0; i < numCores; i++)
				{
					Service1.CoreEntry coreEntry = new Service1.CoreEntry(0.0, i);
					this._coreSet.Add(coreEntry);
					this._currentEntries[i] = coreEntry;
				}
			}

			// Token: 0x060003DF RID: 991 RVA: 0x000224D8 File Offset: 0x000206D8
			public void UpdateUtilization(int cid, double newUti)
			{
				if (cid < 0 || cid >= this._currentEntries.Length)
				{
					throw new ArgumentOutOfRangeException("cid", "Invalid core ID");
				}
				this._coreSet.Remove(this._currentEntries[cid]);
				Service1.CoreEntry coreEntry = new Service1.CoreEntry(newUti, cid);
				this._coreSet.Add(coreEntry);
				this._currentEntries[cid] = coreEntry;
			}

			// Token: 0x060003E0 RID: 992 RVA: 0x00022536 File Offset: 0x00020736
			public int GetMinUtilCore()
			{
				if (this._coreSet.Count == 0)
				{
					return -1;
				}
				return this._coreSet.Min.Cid;
			}

			// Token: 0x060003E1 RID: 993 RVA: 0x00022558 File Offset: 0x00020758
			public void PrintAllCores()
			{
				Console.WriteLine("Core utilization ranking:");
				int num = 1;
				foreach (Service1.CoreEntry coreEntry in this._coreSet)
				{
					Console.WriteLine(string.Format("{0}. Core {1}: {2:P1}", num++, coreEntry.Cid, coreEntry.Uti));
				}
			}

			// Token: 0x040004FE RID: 1278
			public readonly SortedSet<Service1.CoreEntry> _coreSet;

			// Token: 0x040004FF RID: 1279
			public readonly Service1.CoreEntry[] _currentEntries;

			// Token: 0x04000500 RID: 1280
			public readonly Service1.CoreEntryComparer _comparer = new Service1.CoreEntryComparer();
		}

		// Token: 0x02000078 RID: 120
		public class GaussianRandom
		{
			// Token: 0x060003E2 RID: 994 RVA: 0x000225E0 File Offset: 0x000207E0
			public GaussianRandom(int? seed = null)
			{
				this._random = ((seed != null) ? new Random(seed.Value) : new Random());
			}

			// Token: 0x060003E3 RID: 995 RVA: 0x0002260C File Offset: 0x0002080C
			public double NextGaussian(double mean = 0.0, double stdDev = 1.0)
			{
				if (this._hasSpare)
				{
					this._hasSpare = false;
					return this._spareValue * stdDev + mean;
				}
				double num;
				double num2;
				double num3;
				do
				{
					num = this._random.NextDouble() * 2.0 - 1.0;
					num2 = this._random.NextDouble() * 2.0 - 1.0;
					num3 = num * num + num2 * num2;
				}
				while (num3 >= 1.0 || num3 == 0.0);
				num3 = Math.Sqrt(-2.0 * Math.Log(num3) / num3);
				this._spareValue = num2 * num3;
				this._hasSpare = true;
				return num * num3 * stdDev + mean;
			}

			// Token: 0x04000501 RID: 1281
			private readonly Random _random;

			// Token: 0x04000502 RID: 1282
			private bool _hasSpare;

			// Token: 0x04000503 RID: 1283
			private double _spareValue;
		}

		// Token: 0x02000079 RID: 121
		public class NeuralNetwork
		{
			// Token: 0x060003E4 RID: 996 RVA: 0x000226C4 File Offset: 0x000208C4
			public void InitializeWeights_calc_gauss(ref int[,] weights, int input, int output)
			{
				Service1.GaussianRandom gaussianRandom = new Service1.GaussianRandom(null);
				double num = Math.Sqrt(2.0 / (double)(input + output)) * 1000.0;
				for (int i = 0; i < output; i++)
				{
					for (int j = 0; j < input; j++)
					{
						weights[i, j] = (int)(gaussianRandom.NextGaussian(0.0, 1.0) * num);
					}
				}
			}

			// Token: 0x060003E5 RID: 997 RVA: 0x00022740 File Offset: 0x00020940
			public void InitializeWeights_calc(ref int[,] weights, int input, int output)
			{
				Random random = new Random();
				double num = Math.Sqrt(6.0 / (double)(input + output)) * 1000.0;
				for (int i = 0; i < output; i++)
				{
					for (int j = 0; j < input; j++)
					{
						weights[i, j] = (int)(random.NextDouble() * 2.0 * num - num);
					}
				}
			}

			// Token: 0x060003E6 RID: 998 RVA: 0x000227A8 File Offset: 0x000209A8
			public void InitializeWeights()
			{
				new Random();
				new Service1.GaussianRandom(null);
				this.InitializeWeights_calc(ref this.weights_I_f, 7, 6);
				this.InitializeWeights_calc(ref this.weights_f_s, 6, 5);
				this.InitializeWeights_calc(ref this.weights_s_t, 5, 3);
				this.InitializeWeights_calc(ref this.weights_t_p1, 3, 6);
				this.InitializeWeights_calc(ref this.weights_t_p2, 3, 5);
				this.InitializeWeights_calc(ref this.weights_p1_q1, 6, 6);
				this.InitializeWeights_calc(ref this.weights_p2_q2, 5, 6);
				this.InitializeWeights_calc_gauss(ref this.weights_q1_o, 6, 1);
				this.InitializeWeights_calc_gauss(ref this.weights_q2_m, 6, 7);
			}

			// Token: 0x060003E7 RID: 999 RVA: 0x00022848 File Offset: 0x00020A48
			public NeuralNetwork()
			{
				for (int i = 0; i <= 2000; i++)
				{
					this.sigmoidTable[i] = (int)(1000.0 / (1.0 + Math.Exp((double)(-(double)(i - 1000)) / 100.0)));
				}
				this.InitializeWeights();
			}

			// Token: 0x060003E8 RID: 1000 RVA: 0x0002292C File Offset: 0x00020B2C
			public int Predict(int[] inputs)
			{
				int[] array = this.ComputeLayer(inputs, this.weights_I_f);
				int[] array2 = this.ComputeLayer(array, this.weights_f_s);
				int[] array3 = this.ComputeLayer(array2, this.weights_s_t);
				int[] array4 = this.ComputeLayer(array3, this.weights_t_p1);
				int[] array5 = this.ComputeLayer(array4, this.weights_p1_q1);
				int num = 0;
				for (int i = 0; i < 6; i++)
				{
					num += array5[i] * this.weights_q1_o[0, i];
				}
				return this.sigmoidTable[this.ClampIndex(num / 1000)];
			}

			// Token: 0x060003E9 RID: 1001 RVA: 0x000229C1 File Offset: 0x00020BC1
			private int ClampIndex(int value)
			{
				return Math.Min(Math.Max(value + 1000, 0), 2000);
			}

			// Token: 0x060003EA RID: 1002 RVA: 0x000229DC File Offset: 0x00020BDC
			public void Update(int[] inputs, int action, int reward, int neuro_on, int iscomp)
			{
				int[] array = this.ComputeLayer(inputs, this.weights_I_f);
				int[] array2 = this.ComputeLayer(array, this.weights_f_s);
				int[] array3 = this.ComputeLayer(array2, this.weights_s_t);
				if (neuro_on != 0)
				{
					int[] array4 = this.ComputeLayer(array3, this.weights_t_p1);
					int[] array5 = this.ComputeLayer(array4, this.weights_p1_q1);
					int num = 0;
					for (int i = 0; i < 6; i++)
					{
						num += array5[i] * this.weights_q1_o[0, i];
					}
					num = this.sigmoidTable[this.ClampIndex(num / 1000)];
					int num2 = ((action == 1) ? (1000 - num) : num) * reward * this.SigmoidDerivative(num) / 1000;
					this.UpdateWeights(ref this.weights_q1_o, num2, array5, 0, 1);
					int[] array6 = new int[6];
					for (int j = 0; j < 6; j++)
					{
						array6[j] = num2 * this.SigmoidDerivative(array5[j]) / 1000;
					}
					int[] array7 = this.BackwardBranch_single(array6, array4, ref this.weights_p1_q1, 1, 1);
					this.BackwardBranch_single(array7, array3, ref this.weights_t_p1, 2, 1);
					return;
				}
				if (neuro_on == 0)
				{
					int[] array8 = this.ComputeLayer(array3, this.weights_t_p2);
					int[] array9 = this.ComputeLayer(array8, this.weights_p2_q2);
					int[] array10 = this.ComputeLayer(array9, this.weights_q2_m);
					int[] array11 = new int[7];
					for (int k = 0; k < 7; k++)
					{
						array11[k] = (inputs[k] - array10[k]) * (inputs[k] - array10[k]);
					}
					int[] array12 = this.BackwardBranch_single(array11, array9, ref this.weights_q2_m, 0, 0);
					int[] array13 = this.BackwardBranch_single(array12, array8, ref this.weights_p2_q2, 1, 0);
					int[] array14 = this.BackwardBranch_single(array13, array3, ref this.weights_t_p2, 2, 0);
					int[] array15 = this.BackwardBranch_single(array14, array2, ref this.weights_s_t, 3, 0);
					int[] array16 = this.BackwardBranch_single(array15, array, ref this.weights_f_s, 4, 0);
					this.BackwardBranch_single(array16, inputs, ref this.weights_I_f, 5, 0);
				}
			}

			// Token: 0x060003EB RID: 1003 RVA: 0x00022BE4 File Offset: 0x00020DE4
			private int[] ComputeLayer(int[] input, int[,] weights)
			{
				int[] array = new int[weights.GetLength(0)];
				for (int i = 0; i < array.Length; i++)
				{
					int num = 0;
					for (int j = 0; j < input.Length; j++)
					{
						num += input[j] * weights[i, j];
					}
					array[i] = this.sigmoidTable[this.ClampIndex(num / 1000)];
				}
				return array;
			}

			// Token: 0x060003EC RID: 1004 RVA: 0x00022C44 File Offset: 0x00020E44
			private void BackwardBranch1(int delta, int[] tLayer, int[] sLayer, int[] fLayer, int[] mLayer, ref int[,] s_t_Weights, ref int[,] f_s_Weights, ref int[,] m_f_Weights, int iscomp, ref int[] deltaM)
			{
				int[] array = new int[tLayer.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = delta * this.SigmoidDerivative(tLayer[i]) / 1000;
				}
				this.UpdateWeights(ref s_t_Weights, array, sLayer, 1, iscomp);
				int[] array2 = this.ComputeHiddenDelta(array, s_t_Weights, sLayer);
				this.UpdateWeights(ref f_s_Weights, array2, fLayer, 2, iscomp);
				int[] array3 = this.ComputeHiddenDelta(array2, f_s_Weights, fLayer);
				this.UpdateWeights(ref m_f_Weights, array3, mLayer, 3, iscomp);
				deltaM = this.ComputeHiddenDelta(array3, m_f_Weights, mLayer);
			}

			// Token: 0x060003ED RID: 1005 RVA: 0x00022CCD File Offset: 0x00020ECD
			private int[] BackwardBranch_single(int[] delta, int[] prevlayer, ref int[,] weights, int layerDepth, int iscomp)
			{
				this.UpdateWeights(ref weights, delta, prevlayer, layerDepth, iscomp);
				return this.ComputeHiddenDelta(delta, weights, prevlayer);
			}

			// Token: 0x060003EE RID: 1006 RVA: 0x00022CE8 File Offset: 0x00020EE8
			private int[] BackwardBranch_merge(int[] delta1, int[] delta2, int[] prevlayer, ref int[,] weights1, ref int[,] weights2, int layerDepth1, int layerDepth2)
			{
				this.UpdateWeights(ref weights1, delta1, prevlayer, layerDepth1, 0);
				this.UpdateWeights(ref weights2, delta2, prevlayer, layerDepth2, 0);
				int[] array = this.ComputeHiddenDelta(delta1, weights1, prevlayer);
				int[] array2 = this.ComputeHiddenDelta(delta2, weights2, prevlayer);
				int[] array3 = new int[prevlayer.Length];
				for (int i = 0; i < prevlayer.Length; i++)
				{
					array3[i] = array[i] + array2[i];
				}
				return array3;
			}

			// Token: 0x060003EF RID: 1007 RVA: 0x00022D4C File Offset: 0x00020F4C
			private int[] BackwardBranch_merge3(int[] delta1, int[] delta2, int[] delta3, int[] prevlayer, ref int[,] weights1, ref int[,] weights2, ref int[,] weights3, int layerDepth1, int layerDepth2, int layerDepth3)
			{
				this.UpdateWeights(ref weights1, delta1, prevlayer, layerDepth1, 0);
				this.UpdateWeights(ref weights2, delta2, prevlayer, layerDepth2, 0);
				this.UpdateWeights(ref weights3, delta3, prevlayer, layerDepth3, 0);
				int[] array = this.ComputeHiddenDelta(delta1, weights1, prevlayer);
				int[] array2 = this.ComputeHiddenDelta(delta2, weights2, prevlayer);
				int[] array3 = this.ComputeHiddenDelta(delta3, weights3, prevlayer);
				int[] array4 = new int[prevlayer.Length];
				for (int i = 0; i < prevlayer.Length; i++)
				{
					array4[i] = array[i] + array2[i] + array3[i];
				}
				return array4;
			}

			// Token: 0x060003F0 RID: 1008 RVA: 0x00022DDC File Offset: 0x00020FDC
			private void BackwardBranch2(int delta, int[] tLayer, int[] s3Layer, int[] s2Layer, int[] s1Layer, ref int[,] s3_t_Weights, ref int[,] s2_s3_Weights, ref int[,] s1_s2_Weights, ref int[,] i_s1_Weights, ref int[] inputs, int iscomp)
			{
				int[] array = new int[tLayer.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = delta * this.SigmoidDerivative(tLayer[i]) / 1000;
				}
				this.UpdateWeights(ref s3_t_Weights, array, s3Layer, 1, iscomp);
				int[] array2 = this.ComputeHiddenDelta(array, s3_t_Weights, s3Layer);
				this.UpdateWeights(ref s2_s3_Weights, array2, s2Layer, 2, iscomp);
				int[] array3 = this.ComputeHiddenDelta(array2, s2_s3_Weights, s2Layer);
				this.UpdateWeights(ref s1_s2_Weights, array3, s1Layer, 3, iscomp);
				int[] array4 = this.ComputeHiddenDelta(array3, s1_s2_Weights, s1Layer);
				this.UpdateWeights(ref i_s1_Weights, array4, inputs, 4, iscomp);
			}

			// Token: 0x060003F1 RID: 1009 RVA: 0x00022E78 File Offset: 0x00021078
			private void UpdateWeights(ref int[,] weights, int delta, int[] prevLayer, int layerDepth, int iscomp)
			{
				float num;
				if (iscomp == 1)
				{
					num = 0.1f * (1f / (float)Math.Pow(2.0, (double)layerDepth));
				}
				else
				{
					num = 1f * (1f / (float)Math.Pow(2.0, (double)layerDepth));
				}
				for (int i = 0; i < layerDepth; i++)
				{
					int num2 = delta * prevLayer[i] / 1000;
					weights[0, i] += (int)(num * (float)num2);
				}
			}

			// Token: 0x060003F2 RID: 1010 RVA: 0x00022EF8 File Offset: 0x000210F8
			private void UpdateWeights(ref int[,] weights, int[] delta, int[] prevLayer, int layerDepth, int iscomp)
			{
				float num;
				if (iscomp == 1)
				{
					num = 0.1f * (1f / (float)Math.Pow(2.0, (double)layerDepth));
				}
				else
				{
					num = 1f * (1f / (float)Math.Pow(2.0, (double)layerDepth));
				}
				for (int i = 0; i < weights.GetLength(0); i++)
				{
					for (int j = 0; j < weights.GetLength(1); j++)
					{
						int num2 = delta[i] * prevLayer[j] / 1000;
						weights[i, j] += (int)(num * (float)num2);
					}
				}
			}

			// Token: 0x060003F3 RID: 1011 RVA: 0x00022F90 File Offset: 0x00021190
			private int SigmoidDerivative(int activatedValue)
			{
				int num = activatedValue / 1;
				return num * (1000 - num) / 1000 * 1000 / 1000;
			}

			// Token: 0x060003F4 RID: 1012 RVA: 0x00022FBC File Offset: 0x000211BC
			private int[] ComputeHiddenDelta(int[] nextDelta, int[,] nextWeights, int[] currentLayer)
			{
				int[] array = new int[currentLayer.Length];
				for (int i = 0; i < currentLayer.Length; i++)
				{
					int num = 0;
					for (int j = 0; j < nextDelta.Length; j++)
					{
						num += nextDelta[j] * nextWeights[j, i];
					}
					array[i] = num * this.SigmoidDerivative(currentLayer[i]) / 10000;
				}
				return array;
			}

			// Token: 0x060003F5 RID: 1013 RVA: 0x00023014 File Offset: 0x00021214
			private int ComputeFinalOutput(int[] t1, int[] t2, int[] outputWeights)
			{
				int num = 0;
				for (int i = 0; i < t1.Length; i++)
				{
					num += t1[i] * outputWeights[i];
				}
				for (int j = 0; j < t2.Length; j++)
				{
					num += t2[j] * outputWeights[t1.Length + j];
				}
				return this.sigmoidTable[this.ClampIndex(num / 1000)];
			}

			// Token: 0x060003F6 RID: 1014 RVA: 0x0002306C File Offset: 0x0002126C
			private int[] CombineLayers(int[] layer1, int[] layer2)
			{
				int[] array = new int[layer1.Length + layer2.Length];
				Array.Copy(layer1, 0, array, 0, layer1.Length);
				Array.Copy(layer2, 0, array, layer1.Length, layer2.Length);
				return array;
			}

			// Token: 0x04000504 RID: 1284
			private const int SCALE = 1000;

			// Token: 0x04000505 RID: 1285
			private const int NUM_IN = 7;

			// Token: 0x04000506 RID: 1286
			private const int f_NODES = 6;

			// Token: 0x04000507 RID: 1287
			private const int s_NODES = 5;

			// Token: 0x04000508 RID: 1288
			private const int t_NODES = 3;

			// Token: 0x04000509 RID: 1289
			private const int p1_NODES = 6;

			// Token: 0x0400050A RID: 1290
			private const int q1_NODES = 6;

			// Token: 0x0400050B RID: 1291
			private const int p2_NODES = 5;

			// Token: 0x0400050C RID: 1292
			private const int q2_NODES = 6;

			// Token: 0x0400050D RID: 1293
			private const int m_NODES = 7;

			// Token: 0x0400050E RID: 1294
			private int[,] weights_I_f = new int[6, 7];

			// Token: 0x0400050F RID: 1295
			private int[,] weights_f_s = new int[5, 6];

			// Token: 0x04000510 RID: 1296
			private int[,] weights_s_t = new int[3, 5];

			// Token: 0x04000511 RID: 1297
			private int[,] weights_t_p1 = new int[6, 3];

			// Token: 0x04000512 RID: 1298
			private int[,] weights_t_p2 = new int[5, 3];

			// Token: 0x04000513 RID: 1299
			private int[,] weights_p1_q1 = new int[6, 6];

			// Token: 0x04000514 RID: 1300
			private int[,] weights_p2_q2 = new int[6, 5];

			// Token: 0x04000515 RID: 1301
			private int[,] weights_q1_o = new int[1, 6];

			// Token: 0x04000516 RID: 1302
			private int[,] weights_q2_m = new int[7, 6];

			// Token: 0x04000517 RID: 1303
			private int[] sigmoidTable = new int[2001];
		}

		// Token: 0x0200007A RID: 122
		public struct ThreadMetrics
		{
			// Token: 0x04000518 RID: 1304
			public int BigCoreIPC;

			// Token: 0x04000519 RID: 1305
			public int SmallCoreIPC;

			// Token: 0x0400051A RID: 1306
			public long InstructionsPerCycle;

			// Token: 0x0400051B RID: 1307
			public int Priority;

			// Token: 0x0400051C RID: 1308
			public double BigCoreCacheMissRate;

			// Token: 0x0400051D RID: 1309
			public double SmallCoreCacheMissRate;

			// Token: 0x0400051E RID: 1310
			public long ExecutionTimeMicroseconds;
		}

		// Token: 0x0200007B RID: 123
		public class ThreadSchedulerCore
		{
			// Token: 0x060003F7 RID: 1015 RVA: 0x000230A4 File Offset: 0x000212A4
			public ThreadSchedulerCore(int inputSize = 7, int hiddenSize = 64)
			{
				this.inputSize = inputSize;
				this.hiddenSize = hiddenSize;
				this.lstm = new Service1.LSTMCell(inputSize, hiddenSize);
				this.bigCoreWeights = this.InitializeWeights(hiddenSize);
				this.smallCoreWeights = this.InitializeWeights(hiddenSize);
				this.learningRate = 0.01;
				this.learningHistory = new List<Service1.LearningRecord>();
				this.decisionHistory = new List<Service1.DecisionRecord>();
			}

			// Token: 0x060003F8 RID: 1016 RVA: 0x00023114 File Offset: 0x00021314
			private double[] InitializeWeights(int size)
			{
				Random random = new Random();
				double[] array = new double[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = (random.NextDouble() - 0.5) * 2.0 / Math.Sqrt((double)size);
				}
				return array;
			}

			// Token: 0x060003F9 RID: 1017 RVA: 0x00023160 File Offset: 0x00021360
			private double[] ConvertToInput(Service1.ThreadMetrics threadInfo)
			{
				return new double[]
				{
					(double)threadInfo.BigCoreIPC / 100.0,
					(double)threadInfo.SmallCoreIPC / 100.0,
					this.NormalizeInstructions(threadInfo.InstructionsPerCycle),
					(double)threadInfo.Priority / 1500.0,
					threadInfo.BigCoreCacheMissRate,
					threadInfo.SmallCoreCacheMissRate,
					this.NormalizeExecutionTime(threadInfo.ExecutionTimeMicroseconds)
				};
			}

			// Token: 0x060003FA RID: 1018 RVA: 0x000231DF File Offset: 0x000213DF
			private double NormalizeInstructions(long instructions)
			{
				return Math.Min((double)instructions / 1000000.0, 1.0);
			}

			// Token: 0x060003FB RID: 1019 RVA: 0x000231FB File Offset: 0x000213FB
			private double NormalizeExecutionTime(long timeMicroseconds)
			{
				return Math.Min((double)timeMicroseconds / 1000000.0, 1.0);
			}

			// Token: 0x060003FC RID: 1020 RVA: 0x00023218 File Offset: 0x00021418
			[return: TupleElementNames(new string[] { "neuralDecision", "confidence", "adoptNeural", "finalDecision" })]
			public ValueTuple<int, double, bool, int> MakeDecision(Service1.ThreadMetrics currentThreadInfo, int humanDecision)
			{
				double[] array = this.ConvertToInput(currentThreadInfo);
				double[] array2 = this.lstm.Forward(array);
				double num = this.CalculateScore(array2, 1);
				double num2 = this.CalculateScore(array2, 0);
				int num3 = ((num > num2) ? 1 : 0);
				double num4 = Math.Abs(num - num2) / (Math.Abs(num) + Math.Abs(num2) + 1E-08);
				bool flag = num4 > 0.7;
				int num5 = (flag ? num3 : humanDecision);
				this.RecordDecision(currentThreadInfo, humanDecision, num3, num4, flag, num5);
				return new ValueTuple<int, double, bool, int>(num3, num4, flag, num5);
			}

			// Token: 0x060003FD RID: 1021 RVA: 0x000232AC File Offset: 0x000214AC
			private double CalculateScore(double[] hiddenState, int decision)
			{
				double[] array = ((decision == 1) ? this.bigCoreWeights : this.smallCoreWeights);
				double num = 0.0;
				for (int i = 0; i < hiddenState.Length; i++)
				{
					num += hiddenState[i] * array[i];
				}
				Random random = new Random();
				return num + (random.NextDouble() - 0.5) * 0.1;
			}

			// Token: 0x060003FE RID: 1022 RVA: 0x00023314 File Offset: 0x00021514
			public void OnlineLearning(Service1.ThreadMetrics currentThreadInfo, Service1.ThreadMetrics previousThreadInfo, int previousDecision, bool wasNeuralDecision)
			{
				double num = this.CalculateReward(currentThreadInfo, previousThreadInfo, previousDecision);
				double[] array = this.ConvertToInput(previousThreadInfo);
				double[] previousHiddenState = this.lstm.GetPreviousHiddenState();
				this.UpdateWeights(num, previousHiddenState, previousDecision);
				this.learningHistory.Add(new Service1.LearningRecord
				{
					PreviousThreadInfo = previousThreadInfo,
					CurrentThreadInfo = currentThreadInfo,
					Decision = previousDecision,
					WasNeuralDecision = wasNeuralDecision,
					Reward = num,
					Timestamp = DateTime.Now
				});
				if (this.learningHistory.Count > 500)
				{
					this.learningHistory.RemoveAt(0);
				}
				if (wasNeuralDecision)
				{
					this.LearnFromNeuralDecision(num, array, previousDecision);
					return;
				}
				this.LearnFromObservation(currentThreadInfo, previousThreadInfo, previousDecision, num);
			}

			// Token: 0x060003FF RID: 1023 RVA: 0x000233C8 File Offset: 0x000215C8
			private double CalculateReward(Service1.ThreadMetrics current, Service1.ThreadMetrics previous, int previousDecision)
			{
				double num = 0.0;
				double num2 = (double)(previous.ExecutionTimeMicroseconds - current.ExecutionTimeMicroseconds);
				num += num2 / 1000.0;
				if (previousDecision == 1)
				{
					num += (double)(current.BigCoreIPC - previous.BigCoreIPC) / 100.0;
				}
				else
				{
					num += (double)(current.SmallCoreIPC - previous.SmallCoreIPC) / 100.0;
				}
				if (previousDecision == 1)
				{
					num += (previous.BigCoreCacheMissRate - current.BigCoreCacheMissRate) * 10.0;
				}
				else
				{
					num += (previous.SmallCoreCacheMissRate - current.SmallCoreCacheMissRate) * 10.0;
				}
				return Math.Max(-1.0, Math.Min(1.0, num));
			}

			// Token: 0x06000400 RID: 1024 RVA: 0x00023490 File Offset: 0x00021690
			private void UpdateWeights(double reward, double[] hiddenState, int decision)
			{
				double[] array = ((decision == 1) ? this.bigCoreWeights : this.smallCoreWeights);
				for (int i = 0; i < array.Length; i++)
				{
					double num = reward * hiddenState[i];
					array[i] += this.learningRate * num;
				}
			}

			// Token: 0x06000401 RID: 1025 RVA: 0x000234D7 File Offset: 0x000216D7
			private void LearnFromNeuralDecision(double reward, double[] input, int decision)
			{
				this.lstm.Backward(reward, input);
			}

			// Token: 0x06000402 RID: 1026 RVA: 0x000234E8 File Offset: 0x000216E8
			private void LearnFromObservation(Service1.ThreadMetrics current, Service1.ThreadMetrics previous, int humanDecision, double reward)
			{
				double[] array = this.ConvertToInput(previous);
				double[] array2 = this.lstm.Forward(array);
				double num = this.CalculateScore(array2, 1);
				double num2 = this.CalculateScore(array2, 0);
				int num3 = ((num > num2) ? 1 : 0);
				if (num3 != humanDecision)
				{
					double num4 = reward * (double)((humanDecision == num3) ? 1 : (-1));
					this.LearnFromNeuralDecision(num4, array, num3);
				}
			}

			// Token: 0x06000403 RID: 1027 RVA: 0x0002353C File Offset: 0x0002173C
			public Service1.LearningStatistics GetLearningStatistics()
			{
				Service1.LearningStatistics learningStatistics;
				if (this.learningHistory.Count == 0)
				{
					learningStatistics = default(Service1.LearningStatistics);
					return learningStatistics;
				}
				learningStatistics = default(Service1.LearningStatistics);
				learningStatistics.TotalLearningSessions = this.learningHistory.Count;
				learningStatistics.NeuralDecisionCount = this.learningHistory.Count((Service1.LearningRecord r) => r.WasNeuralDecision);
				learningStatistics.AverageReward = this.learningHistory.Average((Service1.LearningRecord r) => r.Reward);
				learningStatistics.LastLearningTime = this.learningHistory.Last<Service1.LearningRecord>().Timestamp;
				return learningStatistics;
			}

			// Token: 0x06000404 RID: 1028 RVA: 0x000235F4 File Offset: 0x000217F4
			public void ResetLearningHistory()
			{
				this.learningHistory.Clear();
			}

			// Token: 0x06000405 RID: 1029 RVA: 0x00023601 File Offset: 0x00021801
			public void SetLearningRate(double rate)
			{
				this.learningRate = Math.Max(0.0001, Math.Min(1.0, rate));
			}

			// Token: 0x06000406 RID: 1030 RVA: 0x00023626 File Offset: 0x00021826
			public double GetLearningRate()
			{
				return this.learningRate;
			}

			// Token: 0x06000407 RID: 1031 RVA: 0x00023630 File Offset: 0x00021830
			public Service1.ModelInfo GetModelInfo()
			{
				return new Service1.ModelInfo
				{
					InputSize = this.inputSize,
					HiddenSize = this.hiddenSize,
					WeightCount = this.bigCoreWeights.Length + this.smallCoreWeights.Length,
					ConfidenceThreshold = 0.7
				};
			}

			// Token: 0x06000408 RID: 1032 RVA: 0x00023688 File Offset: 0x00021888
			private void RecordDecision(Service1.ThreadMetrics threadInfo, int humanDecision, int neuralDecision, double confidence, bool adoptNeural, int finalDecision)
			{
				Service1.DecisionRecord decisionRecord = new Service1.DecisionRecord
				{
					ThreadInfo = threadInfo,
					HumanDecision = humanDecision,
					NeuralDecision = neuralDecision,
					Confidence = confidence,
					AdoptNeural = adoptNeural,
					FinalDecision = finalDecision,
					Timestamp = DateTime.Now
				};
				this.decisionHistory.Add(decisionRecord);
				if (this.decisionHistory.Count > 1000)
				{
					this.decisionHistory.RemoveAt(0);
				}
			}

			// Token: 0x06000409 RID: 1033 RVA: 0x00023708 File Offset: 0x00021908
			public Service1.SchedulerStatistics GetSchedulerStatistics()
			{
				if (this.decisionHistory.Count == 0)
				{
					return new Service1.SchedulerStatistics
					{
						TotalDecisions = 0,
						NeuralAdoptedCount = 0,
						HumanAdoptedCount = 0,
						DisagreementCount = 0,
						NeuralAdoptionRate = 0.0,
						DisagreementRate = 0.0,
						AverageConfidence = 0.0,
						FirstDecisionTime = DateTime.MinValue,
						LastDecisionTime = DateTime.MinValue
					};
				}
				List<Service1.DecisionRecord> list = this.decisionHistory.Skip(Math.Max(0, this.decisionHistory.Count - 1000)).ToList<Service1.DecisionRecord>();
				int count = list.Count;
				int num = list.Count((Service1.DecisionRecord r) => r.AdoptNeural);
				int num2 = count - num;
				int num3 = list.Count((Service1.DecisionRecord r) => r.NeuralDecision != r.HumanDecision);
				double num4 = list.Average((Service1.DecisionRecord r) => r.Confidence);
				return new Service1.SchedulerStatistics
				{
					TotalDecisions = count,
					NeuralAdoptedCount = num,
					HumanAdoptedCount = num2,
					DisagreementCount = num3,
					NeuralAdoptionRate = (double)num / (double)count,
					DisagreementRate = (double)num3 / (double)count,
					AverageConfidence = num4,
					FirstDecisionTime = list.First<Service1.DecisionRecord>().Timestamp,
					LastDecisionTime = list.Last<Service1.DecisionRecord>().Timestamp
				};
			}

			// Token: 0x0600040A RID: 1034 RVA: 0x000238B0 File Offset: 0x00021AB0
			public Service1.SchedulerStatistics GetRecentStatistics(int count)
			{
				if (this.decisionHistory.Count == 0 || count <= 0)
				{
					return this.GetSchedulerStatistics();
				}
				List<Service1.DecisionRecord> list = this.decisionHistory.Skip(Math.Max(0, this.decisionHistory.Count - Math.Min(count, this.decisionHistory.Count))).ToList<Service1.DecisionRecord>();
				int count2 = list.Count;
				int num = list.Count((Service1.DecisionRecord r) => r.AdoptNeural);
				int num2 = count2 - num;
				int num3 = list.Count((Service1.DecisionRecord r) => r.NeuralDecision != r.HumanDecision);
				double num4 = list.Average((Service1.DecisionRecord r) => r.Confidence);
				return new Service1.SchedulerStatistics
				{
					TotalDecisions = count2,
					NeuralAdoptedCount = num,
					HumanAdoptedCount = num2,
					DisagreementCount = num3,
					NeuralAdoptionRate = (double)num / (double)count2,
					DisagreementRate = (double)num3 / (double)count2,
					AverageConfidence = num4,
					FirstDecisionTime = list.First<Service1.DecisionRecord>().Timestamp,
					LastDecisionTime = list.Last<Service1.DecisionRecord>().Timestamp
				};
			}

			// Token: 0x0600040B RID: 1035 RVA: 0x000239F9 File Offset: 0x00021BF9
			public void ResetDecisionHistory()
			{
				this.decisionHistory.Clear();
			}

			// Token: 0x0600040C RID: 1036 RVA: 0x00023A06 File Offset: 0x00021C06
			public int GetDecisionHistoryCount()
			{
				return this.decisionHistory.Count;
			}

			// Token: 0x0600040D RID: 1037 RVA: 0x00023A13 File Offset: 0x00021C13
			public int GetLearningHistoryCount()
			{
				return this.learningHistory.Count;
			}

			// Token: 0x0600040E RID: 1038 RVA: 0x00023A20 File Offset: 0x00021C20
			public int GetMaxDecisionHistorySize()
			{
				return 1000;
			}

			// Token: 0x0600040F RID: 1039 RVA: 0x00023A27 File Offset: 0x00021C27
			public int GetMaxLearningHistorySize()
			{
				return 500;
			}

			// Token: 0x06000410 RID: 1040 RVA: 0x00023A2E File Offset: 0x00021C2E
			public void SetMaxDecisionHistorySize(int maxSize)
			{
				if (maxSize < 10)
				{
					maxSize = 10;
				}
				if (maxSize > 10000)
				{
					maxSize = 10000;
				}
				while (this.decisionHistory.Count > maxSize)
				{
					this.decisionHistory.RemoveAt(0);
				}
			}

			// Token: 0x06000411 RID: 1041 RVA: 0x00023A64 File Offset: 0x00021C64
			public void SetMaxLearningHistorySize(int maxSize)
			{
				if (maxSize < 10)
				{
					maxSize = 10;
				}
				if (maxSize > 10000)
				{
					maxSize = 10000;
				}
				while (this.learningHistory.Count > maxSize)
				{
					this.learningHistory.RemoveAt(0);
				}
			}

			// Token: 0x06000412 RID: 1042 RVA: 0x00023A9C File Offset: 0x00021C9C
			[return: TupleElementNames(new string[] { "decisionUsage", "learningUsage" })]
			public ValueTuple<double, double> GetHistoryUsage()
			{
				double num = (double)this.decisionHistory.Count / 1000.0;
				double num2 = (double)this.learningHistory.Count / 500.0;
				return new ValueTuple<double, double>(num, num2);
			}

			// Token: 0x0400051F RID: 1311
			private Service1.LSTMCell lstm;

			// Token: 0x04000520 RID: 1312
			private double[] bigCoreWeights;

			// Token: 0x04000521 RID: 1313
			private double[] smallCoreWeights;

			// Token: 0x04000522 RID: 1314
			private double learningRate;

			// Token: 0x04000523 RID: 1315
			private int inputSize;

			// Token: 0x04000524 RID: 1316
			private int hiddenSize;

			// Token: 0x04000525 RID: 1317
			private const int BIG_CORE = 1;

			// Token: 0x04000526 RID: 1318
			private const int SMALL_CORE = 0;

			// Token: 0x04000527 RID: 1319
			private const double CONFIDENCE_THRESHOLD = 0.7;

			// Token: 0x04000528 RID: 1320
			private List<Service1.LearningRecord> learningHistory;

			// Token: 0x04000529 RID: 1321
			private const int MAX_LEARNING_HISTORY_SIZE = 500;

			// Token: 0x0400052A RID: 1322
			private List<Service1.DecisionRecord> decisionHistory;

			// Token: 0x0400052B RID: 1323
			private const int MAX_DECISION_HISTORY_SIZE = 1000;
		}

		// Token: 0x0200007C RID: 124
		public struct LearningRecord
		{
			// Token: 0x0400052C RID: 1324
			public Service1.ThreadMetrics PreviousThreadInfo;

			// Token: 0x0400052D RID: 1325
			public Service1.ThreadMetrics CurrentThreadInfo;

			// Token: 0x0400052E RID: 1326
			public int Decision;

			// Token: 0x0400052F RID: 1327
			public bool WasNeuralDecision;

			// Token: 0x04000530 RID: 1328
			public double Reward;

			// Token: 0x04000531 RID: 1329
			public DateTime Timestamp;
		}

		// Token: 0x0200007D RID: 125
		public struct LearningStatistics
		{
			// Token: 0x04000532 RID: 1330
			public int TotalLearningSessions;

			// Token: 0x04000533 RID: 1331
			public int NeuralDecisionCount;

			// Token: 0x04000534 RID: 1332
			public double AverageReward;

			// Token: 0x04000535 RID: 1333
			public DateTime LastLearningTime;
		}

		// Token: 0x0200007E RID: 126
		public struct ModelInfo
		{
			// Token: 0x04000536 RID: 1334
			public int InputSize;

			// Token: 0x04000537 RID: 1335
			public int HiddenSize;

			// Token: 0x04000538 RID: 1336
			public int WeightCount;

			// Token: 0x04000539 RID: 1337
			public double ConfidenceThreshold;
		}

		// Token: 0x0200007F RID: 127
		public struct DecisionRecord
		{
			// Token: 0x0400053A RID: 1338
			public Service1.ThreadMetrics ThreadInfo;

			// Token: 0x0400053B RID: 1339
			public int HumanDecision;

			// Token: 0x0400053C RID: 1340
			public int NeuralDecision;

			// Token: 0x0400053D RID: 1341
			public double Confidence;

			// Token: 0x0400053E RID: 1342
			public bool AdoptNeural;

			// Token: 0x0400053F RID: 1343
			public int FinalDecision;

			// Token: 0x04000540 RID: 1344
			public DateTime Timestamp;
		}

		// Token: 0x02000080 RID: 128
		public struct SchedulerStatistics
		{
			// Token: 0x04000541 RID: 1345
			public int TotalDecisions;

			// Token: 0x04000542 RID: 1346
			public int NeuralAdoptedCount;

			// Token: 0x04000543 RID: 1347
			public int HumanAdoptedCount;

			// Token: 0x04000544 RID: 1348
			public int DisagreementCount;

			// Token: 0x04000545 RID: 1349
			public double NeuralAdoptionRate;

			// Token: 0x04000546 RID: 1350
			public double DisagreementRate;

			// Token: 0x04000547 RID: 1351
			public double AverageConfidence;

			// Token: 0x04000548 RID: 1352
			public DateTime FirstDecisionTime;

			// Token: 0x04000549 RID: 1353
			public DateTime LastDecisionTime;
		}

		// Token: 0x02000081 RID: 129
		internal class LSTMCell
		{
			// Token: 0x06000413 RID: 1043 RVA: 0x00023ADC File Offset: 0x00021CDC
			public LSTMCell(int inputSize, int hiddenSize)
			{
				this.inputSize = inputSize;
				this.hiddenSize = hiddenSize;
				this.InitializeWeights();
				this.previousHiddenState = new double[hiddenSize];
				this.previousCellState = new double[hiddenSize];
			}

			// Token: 0x06000414 RID: 1044 RVA: 0x00023B98 File Offset: 0x00021D98
			private void InitializeWeights()
			{
				Random random = new Random();
				this.Wf = this.InitializeWeightMatrix(this.hiddenSize, this.inputSize + this.hiddenSize, random);
				this.Wi = this.InitializeWeightMatrix(this.hiddenSize, this.inputSize + this.hiddenSize, random);
				this.Wc = this.InitializeWeightMatrix(this.hiddenSize, this.inputSize + this.hiddenSize, random);
				this.Wo = this.InitializeWeightMatrix(this.hiddenSize, this.inputSize + this.hiddenSize, random);
				this.bf = this.InitializeBiasVector(this.hiddenSize, random);
				this.bi = this.InitializeBiasVector(this.hiddenSize, random);
				this.bc = this.InitializeBiasVector(this.hiddenSize, random);
				this.bo = this.InitializeBiasVector(this.hiddenSize, random);
			}

			// Token: 0x06000415 RID: 1045 RVA: 0x00023C78 File Offset: 0x00021E78
			private double[,] InitializeWeightMatrix(int rows, int cols, Random rand)
			{
				double[,] array = new double[rows, cols];
				double num = Math.Sqrt(2.0 / (double)(rows + cols));
				for (int i = 0; i < rows; i++)
				{
					for (int j = 0; j < cols; j++)
					{
						array[i, j] = (rand.NextDouble() - 0.5) * 2.0 * num;
					}
				}
				return array;
			}

			// Token: 0x06000416 RID: 1046 RVA: 0x00023CE0 File Offset: 0x00021EE0
			private double[] InitializeBiasVector(int size, Random rand)
			{
				double[] array = new double[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = (rand.NextDouble() - 0.5) * 0.1;
				}
				return array;
			}

			// Token: 0x06000417 RID: 1047 RVA: 0x00023D20 File Offset: 0x00021F20
			public double[] Forward(double[] input)
			{
				double[] array = new double[this.inputSize + this.hiddenSize];
				Array.Copy(input, 0, array, 0, this.inputSize);
				Array.Copy(this.previousHiddenState, 0, array, this.inputSize, this.hiddenSize);
				double[] array2 = this.Sigmoid(this.MatrixVectorMultiply(this.Wf, array, this.bf));
				double[] array3 = this.Sigmoid(this.MatrixVectorMultiply(this.Wi, array, this.bi));
				double[] array4 = this.Tanh(this.MatrixVectorMultiply(this.Wc, array, this.bc));
				double[] array5 = new double[this.hiddenSize];
				for (int i = 0; i < this.hiddenSize; i++)
				{
					array5[i] = array2[i] * this.previousCellState[i] + array3[i] * array4[i];
				}
				double[] array6 = this.Sigmoid(this.MatrixVectorMultiply(this.Wo, array, this.bo));
				double[] array7 = new double[this.hiddenSize];
				for (int j = 0; j < this.hiddenSize; j++)
				{
					array7[j] = array6[j] * this.Tanh(array5[j]);
				}
				this.previousCellState = array5;
				this.previousHiddenState = array7;
				return array7;
			}

			// Token: 0x06000418 RID: 1048 RVA: 0x00023E5C File Offset: 0x0002205C
			public void Backward(double reward, double[] input)
			{
				double num = 0.001;
				this.AdjustWeights(this.Wf, reward * num);
				this.AdjustWeights(this.Wi, reward * num);
				this.AdjustWeights(this.Wc, reward * num);
				this.AdjustWeights(this.Wo, reward * num);
			}

			// Token: 0x06000419 RID: 1049 RVA: 0x00023EB0 File Offset: 0x000220B0
			private void AdjustWeights(double[,] weights, double adjustment)
			{
				for (int i = 0; i < weights.GetLength(0); i++)
				{
					for (int j = 0; j < weights.GetLength(1); j++)
					{
						weights[i, j] += adjustment * (new Random().NextDouble() - 0.5);
					}
				}
			}

			// Token: 0x0600041A RID: 1050 RVA: 0x00023F03 File Offset: 0x00022103
			public double[] GetPreviousHiddenState()
			{
				return this.previousHiddenState;
			}

			// Token: 0x0600041B RID: 1051 RVA: 0x00023F0C File Offset: 0x0002210C
			private double[] MatrixVectorMultiply(double[,] matrix, double[] vector, double[] bias)
			{
				int length = matrix.GetLength(0);
				int length2 = matrix.GetLength(1);
				double[] array = new double[length];
				for (int i = 0; i < length; i++)
				{
					double num = 0.0;
					for (int j = 0; j < length2; j++)
					{
						num += matrix[i, j] * vector[j];
					}
					array[i] = num + bias[i];
				}
				return array;
			}

			// Token: 0x0600041C RID: 1052 RVA: 0x00023F74 File Offset: 0x00022174
			private double[] Sigmoid(double[] x)
			{
				double[] array = new double[x.Length];
				for (int i = 0; i < x.Length; i++)
				{
					array[i] = 1.0 / (1.0 + Math.Exp(-x[i]));
				}
				return array;
			}

			// Token: 0x0600041D RID: 1053 RVA: 0x00023FBC File Offset: 0x000221BC
			private double[] Tanh(double[] x)
			{
				double[] array = new double[x.Length];
				for (int i = 0; i < x.Length; i++)
				{
					array[i] = Math.Tanh(x[i]);
				}
				return array;
			}

			// Token: 0x0600041E RID: 1054 RVA: 0x00023FEC File Offset: 0x000221EC
			private double Tanh(double x)
			{
				return Math.Tanh(x);
			}

			// Token: 0x0400054A RID: 1354
			private int inputSize;

			// Token: 0x0400054B RID: 1355
			private int hiddenSize;

			// Token: 0x0400054C RID: 1356
			private double[,] Wf = new double[0, 0];

			// Token: 0x0400054D RID: 1357
			private double[,] Wi = new double[0, 0];

			// Token: 0x0400054E RID: 1358
			private double[,] Wc = new double[0, 0];

			// Token: 0x0400054F RID: 1359
			private double[,] Wo = new double[0, 0];

			// Token: 0x04000550 RID: 1360
			private double[] bf = new double[0];

			// Token: 0x04000551 RID: 1361
			private double[] bi = new double[0];

			// Token: 0x04000552 RID: 1362
			private double[] bc = new double[0];

			// Token: 0x04000553 RID: 1363
			private double[] bo = new double[0];

			// Token: 0x04000554 RID: 1364
			private double[] previousHiddenState = new double[0];

			// Token: 0x04000555 RID: 1365
			private double[] previousCellState = new double[0];
		}

		// Token: 0x02000082 RID: 130
		public class SysInfo
		{
			// Token: 0x0600041F RID: 1055 RVA: 0x00023FF4 File Offset: 0x000221F4
			public SysInfo()
			{
			}

			// Token: 0x06000420 RID: 1056 RVA: 0x00023FFC File Offset: 0x000221FC
			public SysInfo(int max_gid, int min_gid, uint availaff, uint availaff1)
			{
				this.Max_gid = max_gid;
				this.Min_gid = min_gid;
				this.Groupset = null;
				this.Lock1 = 0;
				this.Datetime = DateTime.Now.Ticks;
				this.Maxtime = 0L;
				this.Availaff = availaff;
				this.Qtimeavg = 1500L;
				this.Availaff1 = availaff1;
				this.CoreLoadSeq = new Service1.ThreadLoadManager4l();
				this.update = true;
				this.p_e_ratio = 0.0;
				this.total_runtime = 0L;
				this.total_llcmiss = 0L;
				this.IsModelSaved = false;
			}

			// Token: 0x1700006D RID: 109
			// (get) Token: 0x06000421 RID: 1057 RVA: 0x00024099 File Offset: 0x00022299
			// (set) Token: 0x06000422 RID: 1058 RVA: 0x000240A1 File Offset: 0x000222A1
			public long accRewordPerS { get; set; }

			// Token: 0x1700006E RID: 110
			// (get) Token: 0x06000423 RID: 1059 RVA: 0x000240AA File Offset: 0x000222AA
			// (set) Token: 0x06000424 RID: 1060 RVA: 0x000240B2 File Offset: 0x000222B2
			public long accQcount { get; set; }

			// Token: 0x1700006F RID: 111
			// (get) Token: 0x06000425 RID: 1061 RVA: 0x000240BB File Offset: 0x000222BB
			// (set) Token: 0x06000426 RID: 1062 RVA: 0x000240C3 File Offset: 0x000222C3
			public bool IsModelSaved { get; set; }

			// Token: 0x17000070 RID: 112
			// (get) Token: 0x06000427 RID: 1063 RVA: 0x000240CC File Offset: 0x000222CC
			// (set) Token: 0x06000428 RID: 1064 RVA: 0x000240D4 File Offset: 0x000222D4
			public long total_runtime { get; set; }

			// Token: 0x17000071 RID: 113
			// (get) Token: 0x06000429 RID: 1065 RVA: 0x000240DD File Offset: 0x000222DD
			// (set) Token: 0x0600042A RID: 1066 RVA: 0x000240E5 File Offset: 0x000222E5
			public long total_instructions { get; set; }

			// Token: 0x17000072 RID: 114
			// (get) Token: 0x0600042B RID: 1067 RVA: 0x000240EE File Offset: 0x000222EE
			// (set) Token: 0x0600042C RID: 1068 RVA: 0x000240F6 File Offset: 0x000222F6
			public long total_llcmiss { get; set; }

			// Token: 0x17000073 RID: 115
			// (get) Token: 0x0600042D RID: 1069 RVA: 0x000240FF File Offset: 0x000222FF
			// (set) Token: 0x0600042E RID: 1070 RVA: 0x00024107 File Offset: 0x00022307
			public bool update { get; set; }

			// Token: 0x17000074 RID: 116
			// (get) Token: 0x0600042F RID: 1071 RVA: 0x00024110 File Offset: 0x00022310
			// (set) Token: 0x06000430 RID: 1072 RVA: 0x00024118 File Offset: 0x00022318
			public long total_energy { get; set; }

			// Token: 0x17000075 RID: 117
			// (get) Token: 0x06000431 RID: 1073 RVA: 0x00024121 File Offset: 0x00022321
			// (set) Token: 0x06000432 RID: 1074 RVA: 0x00024129 File Offset: 0x00022329
			public long total_energy_l { get; set; }

			// Token: 0x17000076 RID: 118
			// (get) Token: 0x06000433 RID: 1075 RVA: 0x00024132 File Offset: 0x00022332
			// (set) Token: 0x06000434 RID: 1076 RVA: 0x0002413A File Offset: 0x0002233A
			public long total_energy_e { get; set; }

			// Token: 0x17000077 RID: 119
			// (get) Token: 0x06000435 RID: 1077 RVA: 0x00024143 File Offset: 0x00022343
			// (set) Token: 0x06000436 RID: 1078 RVA: 0x0002414B File Offset: 0x0002234B
			public double p_e_ratio { get; set; }

			// Token: 0x17000078 RID: 120
			// (get) Token: 0x06000437 RID: 1079 RVA: 0x00024154 File Offset: 0x00022354
			// (set) Token: 0x06000438 RID: 1080 RVA: 0x0002415C File Offset: 0x0002235C
			public Service1.ThreadLoadManager4l CoreLoadSeq { get; set; }

			// Token: 0x17000079 RID: 121
			// (get) Token: 0x06000439 RID: 1081 RVA: 0x00024165 File Offset: 0x00022365
			// (set) Token: 0x0600043A RID: 1082 RVA: 0x0002416D File Offset: 0x0002236D
			public int Lock1 { get; set; }

			// Token: 0x1700007A RID: 122
			// (get) Token: 0x0600043B RID: 1083 RVA: 0x00024176 File Offset: 0x00022376
			// (set) Token: 0x0600043C RID: 1084 RVA: 0x0002417E File Offset: 0x0002237E
			public int Max_gid { get; set; }

			// Token: 0x1700007B RID: 123
			// (get) Token: 0x0600043D RID: 1085 RVA: 0x00024187 File Offset: 0x00022387
			// (set) Token: 0x0600043E RID: 1086 RVA: 0x0002418F File Offset: 0x0002238F
			public int Min_gid { get; set; }

			// Token: 0x1700007C RID: 124
			// (get) Token: 0x0600043F RID: 1087 RVA: 0x00024198 File Offset: 0x00022398
			// (set) Token: 0x06000440 RID: 1088 RVA: 0x000241A0 File Offset: 0x000223A0
			public long Datetime { get; set; }

			// Token: 0x1700007D RID: 125
			// (get) Token: 0x06000441 RID: 1089 RVA: 0x000241A9 File Offset: 0x000223A9
			// (set) Token: 0x06000442 RID: 1090 RVA: 0x000241B1 File Offset: 0x000223B1
			public long Maxtime { get; set; }

			// Token: 0x1700007E RID: 126
			// (get) Token: 0x06000443 RID: 1091 RVA: 0x000241BA File Offset: 0x000223BA
			// (set) Token: 0x06000444 RID: 1092 RVA: 0x000241C2 File Offset: 0x000223C2
			public long Qtimeavg { get; set; }

			// Token: 0x1700007F RID: 127
			// (get) Token: 0x06000445 RID: 1093 RVA: 0x000241CB File Offset: 0x000223CB
			// (set) Token: 0x06000446 RID: 1094 RVA: 0x000241D3 File Offset: 0x000223D3
			public long Qtimeacc { get; set; }

			// Token: 0x17000080 RID: 128
			// (get) Token: 0x06000447 RID: 1095 RVA: 0x000241DC File Offset: 0x000223DC
			// (set) Token: 0x06000448 RID: 1096 RVA: 0x000241E4 File Offset: 0x000223E4
			public long Qtimecount { get; set; }

			// Token: 0x17000081 RID: 129
			// (get) Token: 0x06000449 RID: 1097 RVA: 0x000241ED File Offset: 0x000223ED
			// (set) Token: 0x0600044A RID: 1098 RVA: 0x000241F5 File Offset: 0x000223F5
			public uint Availaff { get; set; }

			// Token: 0x17000082 RID: 130
			// (get) Token: 0x0600044B RID: 1099 RVA: 0x000241FE File Offset: 0x000223FE
			// (set) Token: 0x0600044C RID: 1100 RVA: 0x00024206 File Offset: 0x00022406
			public uint Availaff1 { get; set; }

			// Token: 0x17000083 RID: 131
			// (get) Token: 0x0600044D RID: 1101 RVA: 0x0002420F File Offset: 0x0002240F
			// (set) Token: 0x0600044E RID: 1102 RVA: 0x00024217 File Offset: 0x00022417
			public Service1.GroupInfo Groupset { get; set; }

			// Token: 0x17000084 RID: 132
			// (get) Token: 0x0600044F RID: 1103 RVA: 0x00024220 File Offset: 0x00022420
			// (set) Token: 0x06000450 RID: 1104 RVA: 0x00024228 File Offset: 0x00022428
			public int Counter_sys_enabled { get; set; }

			// Token: 0x17000085 RID: 133
			// (get) Token: 0x06000451 RID: 1105 RVA: 0x00024231 File Offset: 0x00022431
			// (set) Token: 0x06000452 RID: 1106 RVA: 0x00024239 File Offset: 0x00022439
			public long Acc_perflvl { get; set; }

			// Token: 0x17000086 RID: 134
			// (get) Token: 0x06000453 RID: 1107 RVA: 0x00024242 File Offset: 0x00022442
			// (set) Token: 0x06000454 RID: 1108 RVA: 0x0002424A File Offset: 0x0002244A
			public long Avg_perflvl { get; set; }

			// Token: 0x17000087 RID: 135
			// (get) Token: 0x06000455 RID: 1109 RVA: 0x00024253 File Offset: 0x00022453
			// (set) Token: 0x06000456 RID: 1110 RVA: 0x0002425B File Offset: 0x0002245B
			public long Acc_perfcnt { get; set; }

			// Token: 0x17000088 RID: 136
			// (get) Token: 0x06000457 RID: 1111 RVA: 0x00024264 File Offset: 0x00022464
			// (set) Token: 0x06000458 RID: 1112 RVA: 0x0002426C File Offset: 0x0002246C
			public long Acc_efflvl { get; set; }

			// Token: 0x17000089 RID: 137
			// (get) Token: 0x06000459 RID: 1113 RVA: 0x00024275 File Offset: 0x00022475
			// (set) Token: 0x0600045A RID: 1114 RVA: 0x0002427D File Offset: 0x0002247D
			public long Avg_efflvl { get; set; }

			// Token: 0x1700008A RID: 138
			// (get) Token: 0x0600045B RID: 1115 RVA: 0x00024286 File Offset: 0x00022486
			// (set) Token: 0x0600045C RID: 1116 RVA: 0x0002428E File Offset: 0x0002248E
			public long Acc_effcnt { get; set; }
		}

		// Token: 0x02000083 RID: 131
		public class GroupInfo
		{
			// Token: 0x0600045D RID: 1117 RVA: 0x00024297 File Offset: 0x00022497
			public GroupInfo()
			{
			}

			// Token: 0x0600045E RID: 1118 RVA: 0x000242A0 File Offset: 0x000224A0
			public GroupInfo(int gid, long b_runtime, long b_waittime, long b_affinity, long l_runtime, long l_waittime, long l_affinity, long g_runtime, long g_waittime, long g_affinity, long datetime, long intval)
			{
				this.Gid = gid;
				this.B_runtime = b_runtime;
				this.B_waittime = b_waittime;
				this.B_affinity = b_affinity;
				this.L_runtime = l_runtime;
				this.L_waittime = l_waittime;
				this.L_affinity = l_affinity;
				this.G_runtime = g_runtime;
				this.G_waittime = g_waittime;
				this.G_affinity = g_affinity;
				this.Datetime = datetime;
				this.Intval = intval;
				this.Next = null;
				this.ThreadSet1 = null;
				this.ThreadSet2 = null;
				this.B_available = 1L;
				this.L_available = 1L;
				this.G_available = 1L;
				this.B_utilization = 0L;
				this.L_utilization = 0L;
				this.G_utilization = 0L;
				this.OnlyBcore = 0;
			}

			// Token: 0x1700008B RID: 139
			// (get) Token: 0x0600045F RID: 1119 RVA: 0x0002435C File Offset: 0x0002255C
			// (set) Token: 0x06000460 RID: 1120 RVA: 0x00024364 File Offset: 0x00022564
			public long B_runtime { get; set; }

			// Token: 0x1700008C RID: 140
			// (get) Token: 0x06000461 RID: 1121 RVA: 0x0002436D File Offset: 0x0002256D
			// (set) Token: 0x06000462 RID: 1122 RVA: 0x00024375 File Offset: 0x00022575
			public long B_waittime { get; set; }

			// Token: 0x1700008D RID: 141
			// (get) Token: 0x06000463 RID: 1123 RVA: 0x0002437E File Offset: 0x0002257E
			// (set) Token: 0x06000464 RID: 1124 RVA: 0x00024386 File Offset: 0x00022586
			public long B_affinity { get; set; }

			// Token: 0x1700008E RID: 142
			// (get) Token: 0x06000465 RID: 1125 RVA: 0x0002438F File Offset: 0x0002258F
			// (set) Token: 0x06000466 RID: 1126 RVA: 0x00024397 File Offset: 0x00022597
			public long B_available { get; set; }

			// Token: 0x1700008F RID: 143
			// (get) Token: 0x06000467 RID: 1127 RVA: 0x000243A0 File Offset: 0x000225A0
			// (set) Token: 0x06000468 RID: 1128 RVA: 0x000243A8 File Offset: 0x000225A8
			public long B_utilization { get; set; }

			// Token: 0x17000090 RID: 144
			// (get) Token: 0x06000469 RID: 1129 RVA: 0x000243B1 File Offset: 0x000225B1
			// (set) Token: 0x0600046A RID: 1130 RVA: 0x000243B9 File Offset: 0x000225B9
			public long L_runtime { get; set; }

			// Token: 0x17000091 RID: 145
			// (get) Token: 0x0600046B RID: 1131 RVA: 0x000243C2 File Offset: 0x000225C2
			// (set) Token: 0x0600046C RID: 1132 RVA: 0x000243CA File Offset: 0x000225CA
			public long L_waittime { get; set; }

			// Token: 0x17000092 RID: 146
			// (get) Token: 0x0600046D RID: 1133 RVA: 0x000243D3 File Offset: 0x000225D3
			// (set) Token: 0x0600046E RID: 1134 RVA: 0x000243DB File Offset: 0x000225DB
			public long L_affinity { get; set; }

			// Token: 0x17000093 RID: 147
			// (get) Token: 0x0600046F RID: 1135 RVA: 0x000243E4 File Offset: 0x000225E4
			// (set) Token: 0x06000470 RID: 1136 RVA: 0x000243EC File Offset: 0x000225EC
			public long L_available { get; set; }

			// Token: 0x17000094 RID: 148
			// (get) Token: 0x06000471 RID: 1137 RVA: 0x000243F5 File Offset: 0x000225F5
			// (set) Token: 0x06000472 RID: 1138 RVA: 0x000243FD File Offset: 0x000225FD
			public long L_utilization { get; set; }

			// Token: 0x17000095 RID: 149
			// (get) Token: 0x06000473 RID: 1139 RVA: 0x00024406 File Offset: 0x00022606
			// (set) Token: 0x06000474 RID: 1140 RVA: 0x0002440E File Offset: 0x0002260E
			public long G_runtime { get; set; }

			// Token: 0x17000096 RID: 150
			// (get) Token: 0x06000475 RID: 1141 RVA: 0x00024417 File Offset: 0x00022617
			// (set) Token: 0x06000476 RID: 1142 RVA: 0x0002441F File Offset: 0x0002261F
			public long G_waittime { get; set; }

			// Token: 0x17000097 RID: 151
			// (get) Token: 0x06000477 RID: 1143 RVA: 0x00024428 File Offset: 0x00022628
			// (set) Token: 0x06000478 RID: 1144 RVA: 0x00024430 File Offset: 0x00022630
			public long G_available { get; set; }

			// Token: 0x17000098 RID: 152
			// (get) Token: 0x06000479 RID: 1145 RVA: 0x00024439 File Offset: 0x00022639
			// (set) Token: 0x0600047A RID: 1146 RVA: 0x00024441 File Offset: 0x00022641
			public long G_affinity { get; set; }

			// Token: 0x17000099 RID: 153
			// (get) Token: 0x0600047B RID: 1147 RVA: 0x0002444A File Offset: 0x0002264A
			// (set) Token: 0x0600047C RID: 1148 RVA: 0x00024452 File Offset: 0x00022652
			public long G_utilization { get; set; }

			// Token: 0x1700009A RID: 154
			// (get) Token: 0x0600047D RID: 1149 RVA: 0x0002445B File Offset: 0x0002265B
			// (set) Token: 0x0600047E RID: 1150 RVA: 0x00024463 File Offset: 0x00022663
			public long Datetime { get; set; }

			// Token: 0x1700009B RID: 155
			// (get) Token: 0x0600047F RID: 1151 RVA: 0x0002446C File Offset: 0x0002266C
			// (set) Token: 0x06000480 RID: 1152 RVA: 0x00024474 File Offset: 0x00022674
			public long Intval { get; set; }

			// Token: 0x1700009C RID: 156
			// (get) Token: 0x06000481 RID: 1153 RVA: 0x0002447D File Offset: 0x0002267D
			// (set) Token: 0x06000482 RID: 1154 RVA: 0x00024485 File Offset: 0x00022685
			public Service1.GroupInfo Next { get; set; }

			// Token: 0x1700009D RID: 157
			// (get) Token: 0x06000483 RID: 1155 RVA: 0x0002448E File Offset: 0x0002268E
			// (set) Token: 0x06000484 RID: 1156 RVA: 0x00024496 File Offset: 0x00022696
			public Service1.ThreadInfoSimp ThreadSet1 { get; set; }

			// Token: 0x1700009E RID: 158
			// (get) Token: 0x06000485 RID: 1157 RVA: 0x0002449F File Offset: 0x0002269F
			// (set) Token: 0x06000486 RID: 1158 RVA: 0x000244A7 File Offset: 0x000226A7
			public Service1.ThreadInfoSimp ThreadSet2 { get; set; }

			// Token: 0x1700009F RID: 159
			// (get) Token: 0x06000487 RID: 1159 RVA: 0x000244B0 File Offset: 0x000226B0
			// (set) Token: 0x06000488 RID: 1160 RVA: 0x000244B8 File Offset: 0x000226B8
			public int OnlyBcore { get; set; }

			// Token: 0x04000574 RID: 1396
			public int Gid;
		}

		// Token: 0x02000084 RID: 132
		public class ThreadInfoSimp
		{
			// Token: 0x06000489 RID: 1161 RVA: 0x000244C1 File Offset: 0x000226C1
			public ThreadInfoSimp()
			{
			}

			// Token: 0x0600048A RID: 1162 RVA: 0x000244CC File Offset: 0x000226CC
			public ThreadInfoSimp(int tid, long insPressure, long ipc, long ins_per_count, int coretype, int group, Service1.ThreadInfo belong2thread)
			{
				this.Tid = tid;
				this.InsPressure = insPressure;
				this.Ipc = ipc;
				this.Ins_per_count = ins_per_count;
				this.Group = group;
				this.CoreType = coretype;
				this.Belong2thread = belong2thread;
				this.Next = null;
				this.InsPressure1 = 0L;
				this.InsPressure2 = 0L;
			}

			// Token: 0x170000A0 RID: 160
			// (get) Token: 0x0600048B RID: 1163 RVA: 0x0002452B File Offset: 0x0002272B
			// (set) Token: 0x0600048C RID: 1164 RVA: 0x00024533 File Offset: 0x00022733
			public int Tid { get; set; }

			// Token: 0x170000A1 RID: 161
			// (get) Token: 0x0600048D RID: 1165 RVA: 0x0002453C File Offset: 0x0002273C
			// (set) Token: 0x0600048E RID: 1166 RVA: 0x00024544 File Offset: 0x00022744
			public long InsPressure { get; set; }

			// Token: 0x170000A2 RID: 162
			// (get) Token: 0x0600048F RID: 1167 RVA: 0x0002454D File Offset: 0x0002274D
			// (set) Token: 0x06000490 RID: 1168 RVA: 0x00024555 File Offset: 0x00022755
			public long InsPressure1 { get; set; }

			// Token: 0x170000A3 RID: 163
			// (get) Token: 0x06000491 RID: 1169 RVA: 0x0002455E File Offset: 0x0002275E
			// (set) Token: 0x06000492 RID: 1170 RVA: 0x00024566 File Offset: 0x00022766
			public long InsPressure2 { get; set; }

			// Token: 0x170000A4 RID: 164
			// (get) Token: 0x06000493 RID: 1171 RVA: 0x0002456F File Offset: 0x0002276F
			// (set) Token: 0x06000494 RID: 1172 RVA: 0x00024577 File Offset: 0x00022777
			public long Ins_per_count { get; set; }

			// Token: 0x170000A5 RID: 165
			// (get) Token: 0x06000495 RID: 1173 RVA: 0x00024580 File Offset: 0x00022780
			// (set) Token: 0x06000496 RID: 1174 RVA: 0x00024588 File Offset: 0x00022788
			public long Ipc { get; set; }

			// Token: 0x170000A6 RID: 166
			// (get) Token: 0x06000497 RID: 1175 RVA: 0x00024591 File Offset: 0x00022791
			// (set) Token: 0x06000498 RID: 1176 RVA: 0x00024599 File Offset: 0x00022799
			public int CoreType { get; set; }

			// Token: 0x170000A7 RID: 167
			// (get) Token: 0x06000499 RID: 1177 RVA: 0x000245A2 File Offset: 0x000227A2
			// (set) Token: 0x0600049A RID: 1178 RVA: 0x000245AA File Offset: 0x000227AA
			public int Group { get; set; }

			// Token: 0x170000A8 RID: 168
			// (get) Token: 0x0600049B RID: 1179 RVA: 0x000245B3 File Offset: 0x000227B3
			// (set) Token: 0x0600049C RID: 1180 RVA: 0x000245BB File Offset: 0x000227BB
			public Service1.ThreadInfo Belong2thread { get; set; }

			// Token: 0x170000A9 RID: 169
			// (get) Token: 0x0600049D RID: 1181 RVA: 0x000245C4 File Offset: 0x000227C4
			// (set) Token: 0x0600049E RID: 1182 RVA: 0x000245CC File Offset: 0x000227CC
			public Service1.ThreadInfoSimp Next { get; set; }
		}

		// Token: 0x02000085 RID: 133
		public class StructThreadInfo
		{
			// Token: 0x170000AA RID: 170
			// (get) Token: 0x0600049F RID: 1183 RVA: 0x000245D5 File Offset: 0x000227D5
			// (set) Token: 0x060004A0 RID: 1184 RVA: 0x000245DD File Offset: 0x000227DD
			public Service1.StructThreadInfo.BasicInfo ThreadBasicInfo { get; set; } = new Service1.StructThreadInfo.BasicInfo();

			// Token: 0x170000AB RID: 171
			// (get) Token: 0x060004A1 RID: 1185 RVA: 0x000245E6 File Offset: 0x000227E6
			// (set) Token: 0x060004A2 RID: 1186 RVA: 0x000245EE File Offset: 0x000227EE
			public Service1.StructThreadInfo.Ipc IpcInfo { get; set; } = new Service1.StructThreadInfo.Ipc();

			// Token: 0x170000AC RID: 172
			// (get) Token: 0x060004A3 RID: 1187 RVA: 0x000245F7 File Offset: 0x000227F7
			// (set) Token: 0x060004A4 RID: 1188 RVA: 0x000245FF File Offset: 0x000227FF
			public Service1.StructThreadInfo.ExecTime ExecutionTime { get; set; } = new Service1.StructThreadInfo.ExecTime();

			// Token: 0x170000AD RID: 173
			// (get) Token: 0x060004A5 RID: 1189 RVA: 0x00024608 File Offset: 0x00022808
			// (set) Token: 0x060004A6 RID: 1190 RVA: 0x00024610 File Offset: 0x00022810
			public Service1.StructThreadInfo.Cache CacheInfo { get; set; } = new Service1.StructThreadInfo.Cache();

			// Token: 0x020000AD RID: 173
			public class BasicInfo
			{
				// Token: 0x17000233 RID: 563
				// (get) Token: 0x06000823 RID: 2083 RVA: 0x0002721A File Offset: 0x0002541A
				// (set) Token: 0x06000824 RID: 2084 RVA: 0x00027222 File Offset: 0x00025422
				public long ThreadId { get; set; }

				// Token: 0x17000234 RID: 564
				// (get) Token: 0x06000825 RID: 2085 RVA: 0x0002722B File Offset: 0x0002542B
				// (set) Token: 0x06000826 RID: 2086 RVA: 0x00027233 File Offset: 0x00025433
				public long DateTime { get; set; }

				// Token: 0x17000235 RID: 565
				// (get) Token: 0x06000827 RID: 2087 RVA: 0x0002723C File Offset: 0x0002543C
				// (set) Token: 0x06000828 RID: 2088 RVA: 0x00027244 File Offset: 0x00025444
				public long CoreType { get; set; }

				// Token: 0x17000236 RID: 566
				// (get) Token: 0x06000829 RID: 2089 RVA: 0x0002724D File Offset: 0x0002544D
				// (set) Token: 0x0600082A RID: 2090 RVA: 0x00027255 File Offset: 0x00025455
				public long IsSched { get; set; }

				// Token: 0x0600082B RID: 2091 RVA: 0x0002725E File Offset: 0x0002545E
				public void SchedulingThread()
				{
				}
			}

			// Token: 0x020000AE RID: 174
			public class IpcProcessor
			{
				// Token: 0x17000237 RID: 567
				// (get) Token: 0x0600082D RID: 2093 RVA: 0x00027268 File Offset: 0x00025468
				// (set) Token: 0x0600082E RID: 2094 RVA: 0x00027270 File Offset: 0x00025470
				public long Count { get; set; }

				// Token: 0x17000238 RID: 568
				// (get) Token: 0x0600082F RID: 2095 RVA: 0x00027279 File Offset: 0x00025479
				// (set) Token: 0x06000830 RID: 2096 RVA: 0x00027281 File Offset: 0x00025481
				public long Instructions { get; set; }

				// Token: 0x17000239 RID: 569
				// (get) Token: 0x06000831 RID: 2097 RVA: 0x0002728A File Offset: 0x0002548A
				// (set) Token: 0x06000832 RID: 2098 RVA: 0x00027292 File Offset: 0x00025492
				public long Cycles { get; set; }

				// Token: 0x1700023A RID: 570
				// (get) Token: 0x06000833 RID: 2099 RVA: 0x0002729B File Offset: 0x0002549B
				// (set) Token: 0x06000834 RID: 2100 RVA: 0x000272A3 File Offset: 0x000254A3
				public long AvgIpc { get; set; }

				// Token: 0x06000835 RID: 2101 RVA: 0x000272AC File Offset: 0x000254AC
				public void Reset()
				{
					this.Count = 0L;
					this.Instructions = 0L;
					this.Cycles = 0L;
				}

				// Token: 0x06000836 RID: 2102 RVA: 0x000272C8 File Offset: 0x000254C8
				public long CalcIpc()
				{
					if (this.Cycles == 0L)
					{
						return this.AvgIpc;
					}
					if (this.Count > 1000L || this.Instructions > 300000L)
					{
						this.AvgIpc = 100L * this.Instructions / this.Cycles;
						this.Reset();
						return this.AvgIpc;
					}
					return this.AvgIpc;
				}
			}

			// Token: 0x020000AF RID: 175
			public class Ipc
			{
				// Token: 0x1700023B RID: 571
				// (get) Token: 0x06000838 RID: 2104 RVA: 0x00027332 File Offset: 0x00025532
				// (set) Token: 0x06000839 RID: 2105 RVA: 0x0002733A File Offset: 0x0002553A
				public Service1.StructThreadInfo.IpcProcessor IpcBig { get; set; } = new Service1.StructThreadInfo.IpcProcessor();

				// Token: 0x1700023C RID: 572
				// (get) Token: 0x0600083A RID: 2106 RVA: 0x00027343 File Offset: 0x00025543
				// (set) Token: 0x0600083B RID: 2107 RVA: 0x0002734B File Offset: 0x0002554B
				public Service1.StructThreadInfo.IpcProcessor IpcLittle { get; set; } = new Service1.StructThreadInfo.IpcProcessor();

				// Token: 0x1700023D RID: 573
				// (get) Token: 0x0600083C RID: 2108 RVA: 0x00027354 File Offset: 0x00025554
				// (set) Token: 0x0600083D RID: 2109 RVA: 0x0002735C File Offset: 0x0002555C
				public long AvgIpcRatio { get; set; }

				// Token: 0x0600083E RID: 2110 RVA: 0x00027365 File Offset: 0x00025565
				public void ResetRatio()
				{
					this.AvgIpcRatio = 0L;
				}

				// Token: 0x0600083F RID: 2111 RVA: 0x0002736F File Offset: 0x0002556F
				public long CalcIpcRatio()
				{
					if (this.IpcLittle.AvgIpc == 0L)
					{
						return this.AvgIpcRatio;
					}
					this.AvgIpcRatio = 100L * this.IpcBig.AvgIpc / this.IpcLittle.AvgIpc;
					return this.AvgIpcRatio;
				}

				// Token: 0x06000840 RID: 2112 RVA: 0x000273AC File Offset: 0x000255AC
				public void ResetAll()
				{
					this.IpcBig.Reset();
					this.IpcLittle.Reset();
					this.ResetRatio();
				}
			}

			// Token: 0x020000B0 RID: 176
			public class ExecTime
			{
				// Token: 0x1700023E RID: 574
				// (get) Token: 0x06000842 RID: 2114 RVA: 0x000273E8 File Offset: 0x000255E8
				// (set) Token: 0x06000843 RID: 2115 RVA: 0x000273F0 File Offset: 0x000255F0
				public long TotalTime { get; set; }

				// Token: 0x1700023F RID: 575
				// (get) Token: 0x06000844 RID: 2116 RVA: 0x000273F9 File Offset: 0x000255F9
				// (set) Token: 0x06000845 RID: 2117 RVA: 0x00027401 File Offset: 0x00025601
				public long AverageTime { get; set; }

				// Token: 0x17000240 RID: 576
				// (get) Token: 0x06000846 RID: 2118 RVA: 0x0002740A File Offset: 0x0002560A
				// (set) Token: 0x06000847 RID: 2119 RVA: 0x00027412 File Offset: 0x00025612
				public long MaxTime { get; set; }

				// Token: 0x17000241 RID: 577
				// (get) Token: 0x06000848 RID: 2120 RVA: 0x0002741B File Offset: 0x0002561B
				// (set) Token: 0x06000849 RID: 2121 RVA: 0x00027423 File Offset: 0x00025623
				public long MinTime { get; set; }
			}

			// Token: 0x020000B1 RID: 177
			public class Cache
			{
				// Token: 0x17000242 RID: 578
				// (get) Token: 0x0600084B RID: 2123 RVA: 0x00027434 File Offset: 0x00025634
				// (set) Token: 0x0600084C RID: 2124 RVA: 0x0002743C File Offset: 0x0002563C
				public long CacheHits { get; set; }

				// Token: 0x17000243 RID: 579
				// (get) Token: 0x0600084D RID: 2125 RVA: 0x00027445 File Offset: 0x00025645
				// (set) Token: 0x0600084E RID: 2126 RVA: 0x0002744D File Offset: 0x0002564D
				public long CacheMisses { get; set; }

				// Token: 0x17000244 RID: 580
				// (get) Token: 0x0600084F RID: 2127 RVA: 0x00027456 File Offset: 0x00025656
				// (set) Token: 0x06000850 RID: 2128 RVA: 0x0002745E File Offset: 0x0002565E
				public double HitRate { get; set; }
			}
		}

		// Token: 0x02000086 RID: 134
		public class ThreadInfo
		{
			// Token: 0x060004A8 RID: 1192 RVA: 0x0002464D File Offset: 0x0002284D
			public ThreadInfo()
			{
			}

			// Token: 0x060004A9 RID: 1193 RVA: 0x00024658 File Offset: 0x00022858
			public ThreadInfo(int tid, long datetime, long intval, long count_internal, long count_sample, long runtime, long waittime, long duration, long instruction, long inspressure, long utilization, int coreType, long dummy, long l1_miss, long l2_miss, long miss_rate, long instruction_big, long clock, long ipc, long priorityacc, long ins_per_count, uint affinity, int group, Service1.ThreadInfoSimp infoSimp, long ins_issue, long ins_retire, long br_eff, int sched, long duration_ing, long ins_total, int group_original, long ins_big, long ins_little, long clock_big, long clock_little, long ipc_ratio, long ipc_reset_count, long datetime4interval, long dateTime4sched)
			{
				this.Tid = tid;
				this.DateTime = datetime;
				this.IntVal = intval;
				this.Count_internal = count_internal;
				this.Count_sample = count_sample;
				this.RunTime = runtime;
				this.WaitTime = waittime;
				this.Duration = duration;
				this.Duration_ing = duration_ing;
				this.Instruction = instruction;
				this.InsPressure = 749.0;
				this.Utilization = utilization;
				this.Instruction_big = instruction_big;
				this.Clock = 0.0;
				this.Ins_total = ins_total;
				this.Ipc = ipc;
				this.PriorityAcc = priorityacc;
				this.CoreType = coreType;
				this.Ins_per_count = ins_per_count;
				this.Ins_issue = ins_issue;
				this.Ins_retire = ins_retire;
				this.Dummy = dummy;
				this.Sched = sched;
				this.Lockdata = 0;
				this.L1_miss = l1_miss;
				this.Group_original = group_original;
				this.L2_miss = l2_miss;
				this.Miss_rate = miss_rate;
				this.Affinity = affinity;
				this.Br_eff = br_eff;
				this.Group = group;
				this.Groupinfo = null;
				this.Processinfo = null;
				this.SimpThread = infoSimp;
				this.NextThread = null;
				this.PrevCoreType = 0;
				this.Ins_big = 0.0;
				this.Ins_big1 = 0.0;
				this.Ins_little = ins_little;
				this.Clock_big = clock_big;
				this.Clock_litte = 100L;
				this.Ipc_ratio = this.Ipc_ratio;
				this.Ipc_reset_count = this.Ipc_reset_count;
				this.threshold = 60000L;
				this.prevThreadType = 1L;
				this.ThreadType = 1L;
				this.Ipc_ratio1 = 0L;
				this.Ipc_ratio2 = 0L;
				this.newScore = 0f;
				this.oldScore = 0f;
				this.sched_count = 0L;
				this.sched_little_count = 0L;
				this.inputs = new int[7];
				this.previnputs = new int[7];
				this.trial_switch = 0L;
				this.trial_count = 0L;
				this.PrevSchedInfo = new Service1.PrevSchedInfo();
				this.sched_correct = 0L;
				this.sched_wrong = 0L;
				this.sched_corr_ratio = 100L;
				this.rfo_counters = 0L;
				this.l2ref_counters = 0L;
				this.rfo_counters1 = 0L;
				this.l2ref_counters1 = 0L;
				this.l2cr_counters = 0L;
				this.rfo_ratio = 1000L;
				this.rfo_ratio1 = 1000L;
				this.rfo_ratio2 = 1000L;
				this.rfo_ratio3 = 1000L;
				this.update_signal = 0L;
				this.ipc_big = 100L;
				this.ratio3 = 50000L;
				this.ratio4 = 100L;
				this.SchedType = 1L;
				this.Is_important_threads = 1;
				this.WasNeuroDecision = true;
				this.Type = null;
				this.UserModeRatio = 0.5;
				this.DataLinkage = 0.0;
				this.DateTime4sched = dateTime4sched;
				this.Elasticity = 0.0;
				this.InfluenceIndex = 0L;
			}

			// Token: 0x170000AE RID: 174
			// (get) Token: 0x060004AA RID: 1194 RVA: 0x00024956 File Offset: 0x00022B56
			// (set) Token: 0x060004AB RID: 1195 RVA: 0x0002495E File Offset: 0x00022B5E
			public long InfluenceIndex { get; set; }

			// Token: 0x170000AF RID: 175
			// (get) Token: 0x060004AC RID: 1196 RVA: 0x00024967 File Offset: 0x00022B67
			// (set) Token: 0x060004AD RID: 1197 RVA: 0x0002496F File Offset: 0x00022B6F
			public double DataLinkage { get; set; }

			// Token: 0x170000B0 RID: 176
			// (get) Token: 0x060004AE RID: 1198 RVA: 0x00024978 File Offset: 0x00022B78
			// (set) Token: 0x060004AF RID: 1199 RVA: 0x00024980 File Offset: 0x00022B80
			public double Elasticity { get; set; }

			// Token: 0x170000B1 RID: 177
			// (get) Token: 0x060004B0 RID: 1200 RVA: 0x00024989 File Offset: 0x00022B89
			// (set) Token: 0x060004B1 RID: 1201 RVA: 0x00024991 File Offset: 0x00022B91
			public double DataLinkage1 { get; set; }

			// Token: 0x170000B2 RID: 178
			// (get) Token: 0x060004B2 RID: 1202 RVA: 0x0002499A File Offset: 0x00022B9A
			// (set) Token: 0x060004B3 RID: 1203 RVA: 0x000249A2 File Offset: 0x00022BA2
			public double Elasticity1 { get; set; }

			// Token: 0x170000B3 RID: 179
			// (get) Token: 0x060004B4 RID: 1204 RVA: 0x000249AB File Offset: 0x00022BAB
			// (set) Token: 0x060004B5 RID: 1205 RVA: 0x000249B3 File Offset: 0x00022BB3
			public long CodeFootPrint_counter2 { get; set; }

			// Token: 0x170000B4 RID: 180
			// (get) Token: 0x060004B6 RID: 1206 RVA: 0x000249BC File Offset: 0x00022BBC
			// (set) Token: 0x060004B7 RID: 1207 RVA: 0x000249C4 File Offset: 0x00022BC4
			public long CodeFootPrint_counter1 { get; set; }

			// Token: 0x170000B5 RID: 181
			// (get) Token: 0x060004B8 RID: 1208 RVA: 0x000249CD File Offset: 0x00022BCD
			// (set) Token: 0x060004B9 RID: 1209 RVA: 0x000249D5 File Offset: 0x00022BD5
			public long CodeFootPrint { get; set; }

			// Token: 0x170000B6 RID: 182
			// (get) Token: 0x060004BA RID: 1210 RVA: 0x000249DE File Offset: 0x00022BDE
			// (set) Token: 0x060004BB RID: 1211 RVA: 0x000249E6 File Offset: 0x00022BE6
			public double UserModeRatio { get; set; }

			// Token: 0x170000B7 RID: 183
			// (get) Token: 0x060004BC RID: 1212 RVA: 0x000249EF File Offset: 0x00022BEF
			// (set) Token: 0x060004BD RID: 1213 RVA: 0x000249F7 File Offset: 0x00022BF7
			public string Type { get; set; }

			// Token: 0x170000B8 RID: 184
			// (get) Token: 0x060004BE RID: 1214 RVA: 0x00024A00 File Offset: 0x00022C00
			// (set) Token: 0x060004BF RID: 1215 RVA: 0x00024A08 File Offset: 0x00022C08
			public int Tid { get; set; }

			// Token: 0x170000B9 RID: 185
			// (get) Token: 0x060004C0 RID: 1216 RVA: 0x00024A11 File Offset: 0x00022C11
			// (set) Token: 0x060004C1 RID: 1217 RVA: 0x00024A19 File Offset: 0x00022C19
			public long DateTime { get; set; }

			// Token: 0x170000BA RID: 186
			// (get) Token: 0x060004C2 RID: 1218 RVA: 0x00024A22 File Offset: 0x00022C22
			// (set) Token: 0x060004C3 RID: 1219 RVA: 0x00024A2A File Offset: 0x00022C2A
			public long DateTime4interval { get; set; }

			// Token: 0x170000BB RID: 187
			// (get) Token: 0x060004C4 RID: 1220 RVA: 0x00024A33 File Offset: 0x00022C33
			// (set) Token: 0x060004C5 RID: 1221 RVA: 0x00024A3B File Offset: 0x00022C3B
			public long DateTime4sched { get; set; }

			// Token: 0x170000BC RID: 188
			// (get) Token: 0x060004C6 RID: 1222 RVA: 0x00024A44 File Offset: 0x00022C44
			// (set) Token: 0x060004C7 RID: 1223 RVA: 0x00024A4C File Offset: 0x00022C4C
			public long IntVal { get; set; }

			// Token: 0x170000BD RID: 189
			// (get) Token: 0x060004C8 RID: 1224 RVA: 0x00024A55 File Offset: 0x00022C55
			// (set) Token: 0x060004C9 RID: 1225 RVA: 0x00024A5D File Offset: 0x00022C5D
			public long Count_internal { get; set; }

			// Token: 0x170000BE RID: 190
			// (get) Token: 0x060004CA RID: 1226 RVA: 0x00024A66 File Offset: 0x00022C66
			// (set) Token: 0x060004CB RID: 1227 RVA: 0x00024A6E File Offset: 0x00022C6E
			public long Count_internal1 { get; set; }

			// Token: 0x170000BF RID: 191
			// (get) Token: 0x060004CC RID: 1228 RVA: 0x00024A77 File Offset: 0x00022C77
			// (set) Token: 0x060004CD RID: 1229 RVA: 0x00024A7F File Offset: 0x00022C7F
			public long Count_internal2 { get; set; }

			// Token: 0x170000C0 RID: 192
			// (get) Token: 0x060004CE RID: 1230 RVA: 0x00024A88 File Offset: 0x00022C88
			// (set) Token: 0x060004CF RID: 1231 RVA: 0x00024A90 File Offset: 0x00022C90
			public long Count_sample { get; set; }

			// Token: 0x170000C1 RID: 193
			// (get) Token: 0x060004D0 RID: 1232 RVA: 0x00024A99 File Offset: 0x00022C99
			// (set) Token: 0x060004D1 RID: 1233 RVA: 0x00024AA1 File Offset: 0x00022CA1
			public long Count_sample1 { get; set; }

			// Token: 0x170000C2 RID: 194
			// (get) Token: 0x060004D2 RID: 1234 RVA: 0x00024AAA File Offset: 0x00022CAA
			// (set) Token: 0x060004D3 RID: 1235 RVA: 0x00024AB2 File Offset: 0x00022CB2
			public long RunTime { get; set; }

			// Token: 0x170000C3 RID: 195
			// (get) Token: 0x060004D4 RID: 1236 RVA: 0x00024ABB File Offset: 0x00022CBB
			// (set) Token: 0x060004D5 RID: 1237 RVA: 0x00024AC3 File Offset: 0x00022CC3
			public long WaitTime { get; set; }

			// Token: 0x170000C4 RID: 196
			// (get) Token: 0x060004D6 RID: 1238 RVA: 0x00024ACC File Offset: 0x00022CCC
			// (set) Token: 0x060004D7 RID: 1239 RVA: 0x00024AD4 File Offset: 0x00022CD4
			public long wait_ratio { get; set; }

			// Token: 0x170000C5 RID: 197
			// (get) Token: 0x060004D8 RID: 1240 RVA: 0x00024ADD File Offset: 0x00022CDD
			// (set) Token: 0x060004D9 RID: 1241 RVA: 0x00024AE5 File Offset: 0x00022CE5
			public long Duration { get; set; }

			// Token: 0x170000C6 RID: 198
			// (get) Token: 0x060004DA RID: 1242 RVA: 0x00024AEE File Offset: 0x00022CEE
			// (set) Token: 0x060004DB RID: 1243 RVA: 0x00024AF6 File Offset: 0x00022CF6
			public long Duration_ing { get; set; }

			// Token: 0x170000C7 RID: 199
			// (get) Token: 0x060004DC RID: 1244 RVA: 0x00024AFF File Offset: 0x00022CFF
			// (set) Token: 0x060004DD RID: 1245 RVA: 0x00024B07 File Offset: 0x00022D07
			public long Instruction { get; set; }

			// Token: 0x170000C8 RID: 200
			// (get) Token: 0x060004DE RID: 1246 RVA: 0x00024B10 File Offset: 0x00022D10
			// (set) Token: 0x060004DF RID: 1247 RVA: 0x00024B18 File Offset: 0x00022D18
			public double InsPressure { get; set; }

			// Token: 0x170000C9 RID: 201
			// (get) Token: 0x060004E0 RID: 1248 RVA: 0x00024B21 File Offset: 0x00022D21
			// (set) Token: 0x060004E1 RID: 1249 RVA: 0x00024B29 File Offset: 0x00022D29
			public long Utilization { get; set; }

			// Token: 0x170000CA RID: 202
			// (get) Token: 0x060004E2 RID: 1250 RVA: 0x00024B32 File Offset: 0x00022D32
			// (set) Token: 0x060004E3 RID: 1251 RVA: 0x00024B3A File Offset: 0x00022D3A
			public long L1_miss { get; set; }

			// Token: 0x170000CB RID: 203
			// (get) Token: 0x060004E4 RID: 1252 RVA: 0x00024B43 File Offset: 0x00022D43
			// (set) Token: 0x060004E5 RID: 1253 RVA: 0x00024B4B File Offset: 0x00022D4B
			public long L1_miss1 { get; set; }

			// Token: 0x170000CC RID: 204
			// (get) Token: 0x060004E6 RID: 1254 RVA: 0x00024B54 File Offset: 0x00022D54
			// (set) Token: 0x060004E7 RID: 1255 RVA: 0x00024B5C File Offset: 0x00022D5C
			public long L2_miss { get; set; }

			// Token: 0x170000CD RID: 205
			// (get) Token: 0x060004E8 RID: 1256 RVA: 0x00024B65 File Offset: 0x00022D65
			// (set) Token: 0x060004E9 RID: 1257 RVA: 0x00024B6D File Offset: 0x00022D6D
			public long L3_miss { get; set; }

			// Token: 0x170000CE RID: 206
			// (get) Token: 0x060004EA RID: 1258 RVA: 0x00024B76 File Offset: 0x00022D76
			// (set) Token: 0x060004EB RID: 1259 RVA: 0x00024B7E File Offset: 0x00022D7E
			public long L3_miss1 { get; set; }

			// Token: 0x170000CF RID: 207
			// (get) Token: 0x060004EC RID: 1260 RVA: 0x00024B87 File Offset: 0x00022D87
			// (set) Token: 0x060004ED RID: 1261 RVA: 0x00024B8F File Offset: 0x00022D8F
			public float Block_avg_ins { get; set; }

			// Token: 0x170000D0 RID: 208
			// (get) Token: 0x060004EE RID: 1262 RVA: 0x00024B98 File Offset: 0x00022D98
			// (set) Token: 0x060004EF RID: 1263 RVA: 0x00024BA0 File Offset: 0x00022DA0
			public float Avg_Block_avg_ins { get; set; }

			// Token: 0x170000D1 RID: 209
			// (get) Token: 0x060004F0 RID: 1264 RVA: 0x00024BA9 File Offset: 0x00022DA9
			// (set) Token: 0x060004F1 RID: 1265 RVA: 0x00024BB1 File Offset: 0x00022DB1
			public float Acc_Block_avg_ins { get; set; }

			// Token: 0x170000D2 RID: 210
			// (get) Token: 0x060004F2 RID: 1266 RVA: 0x00024BBA File Offset: 0x00022DBA
			// (set) Token: 0x060004F3 RID: 1267 RVA: 0x00024BC2 File Offset: 0x00022DC2
			public long Ins_sample { get; set; }

			// Token: 0x170000D3 RID: 211
			// (get) Token: 0x060004F4 RID: 1268 RVA: 0x00024BCB File Offset: 0x00022DCB
			// (set) Token: 0x060004F5 RID: 1269 RVA: 0x00024BD3 File Offset: 0x00022DD3
			public long Branchs_taken { get; set; }

			// Token: 0x170000D4 RID: 212
			// (get) Token: 0x060004F6 RID: 1270 RVA: 0x00024BDC File Offset: 0x00022DDC
			// (set) Token: 0x060004F7 RID: 1271 RVA: 0x00024BE4 File Offset: 0x00022DE4
			public long L4_miss { get; set; }

			// Token: 0x170000D5 RID: 213
			// (get) Token: 0x060004F8 RID: 1272 RVA: 0x00024BED File Offset: 0x00022DED
			// (set) Token: 0x060004F9 RID: 1273 RVA: 0x00024BF5 File Offset: 0x00022DF5
			public double avgruntime { get; set; }

			// Token: 0x170000D6 RID: 214
			// (get) Token: 0x060004FA RID: 1274 RVA: 0x00024BFE File Offset: 0x00022DFE
			// (set) Token: 0x060004FB RID: 1275 RVA: 0x00024C06 File Offset: 0x00022E06
			public long avgruntime_total { get; set; }

			// Token: 0x170000D7 RID: 215
			// (get) Token: 0x060004FC RID: 1276 RVA: 0x00024C0F File Offset: 0x00022E0F
			// (set) Token: 0x060004FD RID: 1277 RVA: 0x00024C17 File Offset: 0x00022E17
			public long avgruntime_count { get; set; }

			// Token: 0x170000D8 RID: 216
			// (get) Token: 0x060004FE RID: 1278 RVA: 0x00024C20 File Offset: 0x00022E20
			// (set) Token: 0x060004FF RID: 1279 RVA: 0x00024C28 File Offset: 0x00022E28
			public long Miss_rate { get; set; }

			// Token: 0x170000D9 RID: 217
			// (get) Token: 0x06000500 RID: 1280 RVA: 0x00024C31 File Offset: 0x00022E31
			// (set) Token: 0x06000501 RID: 1281 RVA: 0x00024C39 File Offset: 0x00022E39
			public int CoreType { get; set; }

			// Token: 0x170000DA RID: 218
			// (get) Token: 0x06000502 RID: 1282 RVA: 0x00024C42 File Offset: 0x00022E42
			// (set) Token: 0x06000503 RID: 1283 RVA: 0x00024C4A File Offset: 0x00022E4A
			public int PrevCoreType { get; set; }

			// Token: 0x170000DB RID: 219
			// (get) Token: 0x06000504 RID: 1284 RVA: 0x00024C53 File Offset: 0x00022E53
			// (set) Token: 0x06000505 RID: 1285 RVA: 0x00024C5B File Offset: 0x00022E5B
			public long Dummy { get; set; }

			// Token: 0x170000DC RID: 220
			// (get) Token: 0x06000506 RID: 1286 RVA: 0x00024C64 File Offset: 0x00022E64
			// (set) Token: 0x06000507 RID: 1287 RVA: 0x00024C6C File Offset: 0x00022E6C
			public long Instruction_big { get; set; }

			// Token: 0x170000DD RID: 221
			// (get) Token: 0x06000508 RID: 1288 RVA: 0x00024C75 File Offset: 0x00022E75
			// (set) Token: 0x06000509 RID: 1289 RVA: 0x00024C7D File Offset: 0x00022E7D
			public double Clock { get; set; }

			// Token: 0x170000DE RID: 222
			// (get) Token: 0x0600050A RID: 1290 RVA: 0x00024C86 File Offset: 0x00022E86
			// (set) Token: 0x0600050B RID: 1291 RVA: 0x00024C8E File Offset: 0x00022E8E
			public long Ipc { get; set; }

			// Token: 0x170000DF RID: 223
			// (get) Token: 0x0600050C RID: 1292 RVA: 0x00024C97 File Offset: 0x00022E97
			// (set) Token: 0x0600050D RID: 1293 RVA: 0x00024C9F File Offset: 0x00022E9F
			public int Is_important_threads { get; set; }

			// Token: 0x170000E0 RID: 224
			// (get) Token: 0x0600050E RID: 1294 RVA: 0x00024CA8 File Offset: 0x00022EA8
			// (set) Token: 0x0600050F RID: 1295 RVA: 0x00024CB0 File Offset: 0x00022EB0
			public long rate_mem { get; set; }

			// Token: 0x170000E1 RID: 225
			// (get) Token: 0x06000510 RID: 1296 RVA: 0x00024CB9 File Offset: 0x00022EB9
			// (set) Token: 0x06000511 RID: 1297 RVA: 0x00024CC1 File Offset: 0x00022EC1
			public long Ins_per_count { get; set; }

			// Token: 0x170000E2 RID: 226
			// (get) Token: 0x06000512 RID: 1298 RVA: 0x00024CCA File Offset: 0x00022ECA
			// (set) Token: 0x06000513 RID: 1299 RVA: 0x00024CD2 File Offset: 0x00022ED2
			public long Ins_flow { get; set; }

			// Token: 0x170000E3 RID: 227
			// (get) Token: 0x06000514 RID: 1300 RVA: 0x00024CDB File Offset: 0x00022EDB
			// (set) Token: 0x06000515 RID: 1301 RVA: 0x00024CE3 File Offset: 0x00022EE3
			public long Ins_flow1 { get; set; }

			// Token: 0x170000E4 RID: 228
			// (get) Token: 0x06000516 RID: 1302 RVA: 0x00024CEC File Offset: 0x00022EEC
			// (set) Token: 0x06000517 RID: 1303 RVA: 0x00024CF4 File Offset: 0x00022EF4
			public long Ins_issue { get; set; }

			// Token: 0x170000E5 RID: 229
			// (get) Token: 0x06000518 RID: 1304 RVA: 0x00024CFD File Offset: 0x00022EFD
			// (set) Token: 0x06000519 RID: 1305 RVA: 0x00024D05 File Offset: 0x00022F05
			public long Ins_retire { get; set; }

			// Token: 0x170000E6 RID: 230
			// (get) Token: 0x0600051A RID: 1306 RVA: 0x00024D0E File Offset: 0x00022F0E
			// (set) Token: 0x0600051B RID: 1307 RVA: 0x00024D16 File Offset: 0x00022F16
			public long Br_eff { get; set; }

			// Token: 0x170000E7 RID: 231
			// (get) Token: 0x0600051C RID: 1308 RVA: 0x00024D1F File Offset: 0x00022F1F
			// (set) Token: 0x0600051D RID: 1309 RVA: 0x00024D27 File Offset: 0x00022F27
			public long PriorityAcc { get; set; }

			// Token: 0x170000E8 RID: 232
			// (get) Token: 0x0600051E RID: 1310 RVA: 0x00024D30 File Offset: 0x00022F30
			// (set) Token: 0x0600051F RID: 1311 RVA: 0x00024D38 File Offset: 0x00022F38
			public long PriorityAcc1 { get; set; }

			// Token: 0x170000E9 RID: 233
			// (get) Token: 0x06000520 RID: 1312 RVA: 0x00024D41 File Offset: 0x00022F41
			// (set) Token: 0x06000521 RID: 1313 RVA: 0x00024D49 File Offset: 0x00022F49
			public long Ins_total { get; set; }

			// Token: 0x170000EA RID: 234
			// (get) Token: 0x06000522 RID: 1314 RVA: 0x00024D52 File Offset: 0x00022F52
			// (set) Token: 0x06000523 RID: 1315 RVA: 0x00024D5A File Offset: 0x00022F5A
			public uint Affinity { get; set; }

			// Token: 0x170000EB RID: 235
			// (get) Token: 0x06000524 RID: 1316 RVA: 0x00024D63 File Offset: 0x00022F63
			// (set) Token: 0x06000525 RID: 1317 RVA: 0x00024D6B File Offset: 0x00022F6B
			public int Group { get; set; }

			// Token: 0x170000EC RID: 236
			// (get) Token: 0x06000526 RID: 1318 RVA: 0x00024D74 File Offset: 0x00022F74
			// (set) Token: 0x06000527 RID: 1319 RVA: 0x00024D7C File Offset: 0x00022F7C
			public int Group_original { get; set; }

			// Token: 0x170000ED RID: 237
			// (get) Token: 0x06000528 RID: 1320 RVA: 0x00024D85 File Offset: 0x00022F85
			// (set) Token: 0x06000529 RID: 1321 RVA: 0x00024D8D File Offset: 0x00022F8D
			public int Sched { get; set; }

			// Token: 0x170000EE RID: 238
			// (get) Token: 0x0600052A RID: 1322 RVA: 0x00024D96 File Offset: 0x00022F96
			// (set) Token: 0x0600052B RID: 1323 RVA: 0x00024D9E File Offset: 0x00022F9E
			public int Lockdata { get; set; }

			// Token: 0x170000EF RID: 239
			// (get) Token: 0x0600052C RID: 1324 RVA: 0x00024DA7 File Offset: 0x00022FA7
			// (set) Token: 0x0600052D RID: 1325 RVA: 0x00024DAF File Offset: 0x00022FAF
			public double Ins_big { get; set; }

			// Token: 0x170000F0 RID: 240
			// (get) Token: 0x0600052E RID: 1326 RVA: 0x00024DB8 File Offset: 0x00022FB8
			// (set) Token: 0x0600052F RID: 1327 RVA: 0x00024DC0 File Offset: 0x00022FC0
			public double Ins_big1 { get; set; }

			// Token: 0x170000F1 RID: 241
			// (get) Token: 0x06000530 RID: 1328 RVA: 0x00024DC9 File Offset: 0x00022FC9
			// (set) Token: 0x06000531 RID: 1329 RVA: 0x00024DD1 File Offset: 0x00022FD1
			public long Avg_ins_big { get; set; }

			// Token: 0x170000F2 RID: 242
			// (get) Token: 0x06000532 RID: 1330 RVA: 0x00024DDA File Offset: 0x00022FDA
			// (set) Token: 0x06000533 RID: 1331 RVA: 0x00024DE2 File Offset: 0x00022FE2
			public long Acc_ins_big { get; set; }

			// Token: 0x170000F3 RID: 243
			// (get) Token: 0x06000534 RID: 1332 RVA: 0x00024DEB File Offset: 0x00022FEB
			// (set) Token: 0x06000535 RID: 1333 RVA: 0x00024DF3 File Offset: 0x00022FF3
			public long ratio3 { get; set; }

			// Token: 0x170000F4 RID: 244
			// (get) Token: 0x06000536 RID: 1334 RVA: 0x00024DFC File Offset: 0x00022FFC
			// (set) Token: 0x06000537 RID: 1335 RVA: 0x00024E04 File Offset: 0x00023004
			public long ratio4 { get; set; }

			// Token: 0x170000F5 RID: 245
			// (get) Token: 0x06000538 RID: 1336 RVA: 0x00024E0D File Offset: 0x0002300D
			// (set) Token: 0x06000539 RID: 1337 RVA: 0x00024E15 File Offset: 0x00023015
			public long Ins_little { get; set; }

			// Token: 0x170000F6 RID: 246
			// (get) Token: 0x0600053A RID: 1338 RVA: 0x00024E1E File Offset: 0x0002301E
			// (set) Token: 0x0600053B RID: 1339 RVA: 0x00024E26 File Offset: 0x00023026
			public long Clock_big { get; set; }

			// Token: 0x170000F7 RID: 247
			// (get) Token: 0x0600053C RID: 1340 RVA: 0x00024E2F File Offset: 0x0002302F
			// (set) Token: 0x0600053D RID: 1341 RVA: 0x00024E37 File Offset: 0x00023037
			public long Clock_litte { get; set; }

			// Token: 0x170000F8 RID: 248
			// (get) Token: 0x0600053E RID: 1342 RVA: 0x00024E40 File Offset: 0x00023040
			// (set) Token: 0x0600053F RID: 1343 RVA: 0x00024E48 File Offset: 0x00023048
			public long Ipc_big { get; set; }

			// Token: 0x170000F9 RID: 249
			// (get) Token: 0x06000540 RID: 1344 RVA: 0x00024E51 File Offset: 0x00023051
			// (set) Token: 0x06000541 RID: 1345 RVA: 0x00024E59 File Offset: 0x00023059
			public long Ipc_ratio { get; set; }

			// Token: 0x170000FA RID: 250
			// (get) Token: 0x06000542 RID: 1346 RVA: 0x00024E62 File Offset: 0x00023062
			// (set) Token: 0x06000543 RID: 1347 RVA: 0x00024E6A File Offset: 0x0002306A
			public long Ipc_ratio1 { get; set; }

			// Token: 0x170000FB RID: 251
			// (get) Token: 0x06000544 RID: 1348 RVA: 0x00024E73 File Offset: 0x00023073
			// (set) Token: 0x06000545 RID: 1349 RVA: 0x00024E7B File Offset: 0x0002307B
			public long Ipc_ratio2 { get; set; }

			// Token: 0x170000FC RID: 252
			// (get) Token: 0x06000546 RID: 1350 RVA: 0x00024E84 File Offset: 0x00023084
			// (set) Token: 0x06000547 RID: 1351 RVA: 0x00024E8C File Offset: 0x0002308C
			public long Ipc_reset_count { get; set; }

			// Token: 0x170000FD RID: 253
			// (get) Token: 0x06000548 RID: 1352 RVA: 0x00024E95 File Offset: 0x00023095
			// (set) Token: 0x06000549 RID: 1353 RVA: 0x00024E9D File Offset: 0x0002309D
			public long threshold { get; set; }

			// Token: 0x170000FE RID: 254
			// (get) Token: 0x0600054A RID: 1354 RVA: 0x00024EA6 File Offset: 0x000230A6
			// (set) Token: 0x0600054B RID: 1355 RVA: 0x00024EAE File Offset: 0x000230AE
			public long SchedType { get; set; }

			// Token: 0x170000FF RID: 255
			// (get) Token: 0x0600054C RID: 1356 RVA: 0x00024EB7 File Offset: 0x000230B7
			// (set) Token: 0x0600054D RID: 1357 RVA: 0x00024EBF File Offset: 0x000230BF
			public bool WasNeuroDecision { get; set; }

			// Token: 0x17000100 RID: 256
			// (get) Token: 0x0600054E RID: 1358 RVA: 0x00024EC8 File Offset: 0x000230C8
			// (set) Token: 0x0600054F RID: 1359 RVA: 0x00024ED0 File Offset: 0x000230D0
			public long ThreadType { get; set; }

			// Token: 0x17000101 RID: 257
			// (get) Token: 0x06000550 RID: 1360 RVA: 0x00024ED9 File Offset: 0x000230D9
			// (set) Token: 0x06000551 RID: 1361 RVA: 0x00024EE1 File Offset: 0x000230E1
			public long prevThreadType { get; set; }

			// Token: 0x17000102 RID: 258
			// (get) Token: 0x06000552 RID: 1362 RVA: 0x00024EEA File Offset: 0x000230EA
			// (set) Token: 0x06000553 RID: 1363 RVA: 0x00024EF2 File Offset: 0x000230F2
			public float newScore { get; set; }

			// Token: 0x17000103 RID: 259
			// (get) Token: 0x06000554 RID: 1364 RVA: 0x00024EFB File Offset: 0x000230FB
			// (set) Token: 0x06000555 RID: 1365 RVA: 0x00024F03 File Offset: 0x00023103
			public float oldScore { get; set; }

			// Token: 0x17000104 RID: 260
			// (get) Token: 0x06000556 RID: 1366 RVA: 0x00024F0C File Offset: 0x0002310C
			// (set) Token: 0x06000557 RID: 1367 RVA: 0x00024F14 File Offset: 0x00023114
			public long maxins { get; set; }

			// Token: 0x17000105 RID: 261
			// (get) Token: 0x06000558 RID: 1368 RVA: 0x00024F1D File Offset: 0x0002311D
			// (set) Token: 0x06000559 RID: 1369 RVA: 0x00024F25 File Offset: 0x00023125
			public int[] inputs { get; set; }

			// Token: 0x17000106 RID: 262
			// (get) Token: 0x0600055A RID: 1370 RVA: 0x00024F2E File Offset: 0x0002312E
			// (set) Token: 0x0600055B RID: 1371 RVA: 0x00024F36 File Offset: 0x00023136
			public int[] previnputs { get; set; }

			// Token: 0x17000107 RID: 263
			// (get) Token: 0x0600055C RID: 1372 RVA: 0x00024F3F File Offset: 0x0002313F
			// (set) Token: 0x0600055D RID: 1373 RVA: 0x00024F47 File Offset: 0x00023147
			public int rs { get; set; }

			// Token: 0x17000108 RID: 264
			// (get) Token: 0x0600055E RID: 1374 RVA: 0x00024F50 File Offset: 0x00023150
			// (set) Token: 0x0600055F RID: 1375 RVA: 0x00024F58 File Offset: 0x00023158
			public int prevrs { get; set; }

			// Token: 0x17000109 RID: 265
			// (get) Token: 0x06000560 RID: 1376 RVA: 0x00024F61 File Offset: 0x00023161
			// (set) Token: 0x06000561 RID: 1377 RVA: 0x00024F69 File Offset: 0x00023169
			public long trial_count { get; set; }

			// Token: 0x1700010A RID: 266
			// (get) Token: 0x06000562 RID: 1378 RVA: 0x00024F72 File Offset: 0x00023172
			// (set) Token: 0x06000563 RID: 1379 RVA: 0x00024F7A File Offset: 0x0002317A
			public long trial_switch { get; set; }

			// Token: 0x1700010B RID: 267
			// (get) Token: 0x06000564 RID: 1380 RVA: 0x00024F83 File Offset: 0x00023183
			// (set) Token: 0x06000565 RID: 1381 RVA: 0x00024F8B File Offset: 0x0002318B
			public long sched_count { get; set; }

			// Token: 0x1700010C RID: 268
			// (get) Token: 0x06000566 RID: 1382 RVA: 0x00024F94 File Offset: 0x00023194
			// (set) Token: 0x06000567 RID: 1383 RVA: 0x00024F9C File Offset: 0x0002319C
			public long sched_little_count { get; set; }

			// Token: 0x1700010D RID: 269
			// (get) Token: 0x06000568 RID: 1384 RVA: 0x00024FA5 File Offset: 0x000231A5
			// (set) Token: 0x06000569 RID: 1385 RVA: 0x00024FAD File Offset: 0x000231AD
			public long rfo_counters { get; set; }

			// Token: 0x1700010E RID: 270
			// (get) Token: 0x0600056A RID: 1386 RVA: 0x00024FB6 File Offset: 0x000231B6
			// (set) Token: 0x0600056B RID: 1387 RVA: 0x00024FBE File Offset: 0x000231BE
			public long l2ref_counters { get; set; }

			// Token: 0x1700010F RID: 271
			// (get) Token: 0x0600056C RID: 1388 RVA: 0x00024FC7 File Offset: 0x000231C7
			// (set) Token: 0x0600056D RID: 1389 RVA: 0x00024FCF File Offset: 0x000231CF
			public long rfo_counters1 { get; set; }

			// Token: 0x17000110 RID: 272
			// (get) Token: 0x0600056E RID: 1390 RVA: 0x00024FD8 File Offset: 0x000231D8
			// (set) Token: 0x0600056F RID: 1391 RVA: 0x00024FE0 File Offset: 0x000231E0
			public long rfo_counters2 { get; set; }

			// Token: 0x17000111 RID: 273
			// (get) Token: 0x06000570 RID: 1392 RVA: 0x00024FE9 File Offset: 0x000231E9
			// (set) Token: 0x06000571 RID: 1393 RVA: 0x00024FF1 File Offset: 0x000231F1
			public long rfo_counters3 { get; set; }

			// Token: 0x17000112 RID: 274
			// (get) Token: 0x06000572 RID: 1394 RVA: 0x00024FFA File Offset: 0x000231FA
			// (set) Token: 0x06000573 RID: 1395 RVA: 0x00025002 File Offset: 0x00023202
			public long l2ref_counters1 { get; set; }

			// Token: 0x17000113 RID: 275
			// (get) Token: 0x06000574 RID: 1396 RVA: 0x0002500B File Offset: 0x0002320B
			// (set) Token: 0x06000575 RID: 1397 RVA: 0x00025013 File Offset: 0x00023213
			public long l2ref_counters2 { get; set; }

			// Token: 0x17000114 RID: 276
			// (get) Token: 0x06000576 RID: 1398 RVA: 0x0002501C File Offset: 0x0002321C
			// (set) Token: 0x06000577 RID: 1399 RVA: 0x00025024 File Offset: 0x00023224
			public long l2ref_counters3 { get; set; }

			// Token: 0x17000115 RID: 277
			// (get) Token: 0x06000578 RID: 1400 RVA: 0x0002502D File Offset: 0x0002322D
			// (set) Token: 0x06000579 RID: 1401 RVA: 0x00025035 File Offset: 0x00023235
			public long demoteacc { get; set; }

			// Token: 0x17000116 RID: 278
			// (get) Token: 0x0600057A RID: 1402 RVA: 0x0002503E File Offset: 0x0002323E
			// (set) Token: 0x0600057B RID: 1403 RVA: 0x00025046 File Offset: 0x00023246
			public long avg_ins { get; set; }

			// Token: 0x17000117 RID: 279
			// (get) Token: 0x0600057C RID: 1404 RVA: 0x0002504F File Offset: 0x0002324F
			// (set) Token: 0x0600057D RID: 1405 RVA: 0x00025057 File Offset: 0x00023257
			public long avg_ins1 { get; set; }

			// Token: 0x17000118 RID: 280
			// (get) Token: 0x0600057E RID: 1406 RVA: 0x00025060 File Offset: 0x00023260
			// (set) Token: 0x0600057F RID: 1407 RVA: 0x00025068 File Offset: 0x00023268
			public long avg_ins2 { get; set; }

			// Token: 0x17000119 RID: 281
			// (get) Token: 0x06000580 RID: 1408 RVA: 0x00025071 File Offset: 0x00023271
			// (set) Token: 0x06000581 RID: 1409 RVA: 0x00025079 File Offset: 0x00023279
			public long avg_ins3 { get; set; }

			// Token: 0x1700011A RID: 282
			// (get) Token: 0x06000582 RID: 1410 RVA: 0x00025082 File Offset: 0x00023282
			// (set) Token: 0x06000583 RID: 1411 RVA: 0x0002508A File Offset: 0x0002328A
			public long l2cr_counters { get; set; }

			// Token: 0x1700011B RID: 283
			// (get) Token: 0x06000584 RID: 1412 RVA: 0x00025093 File Offset: 0x00023293
			// (set) Token: 0x06000585 RID: 1413 RVA: 0x0002509B File Offset: 0x0002329B
			public long rfo_ratio { get; set; }

			// Token: 0x1700011C RID: 284
			// (get) Token: 0x06000586 RID: 1414 RVA: 0x000250A4 File Offset: 0x000232A4
			// (set) Token: 0x06000587 RID: 1415 RVA: 0x000250AC File Offset: 0x000232AC
			public long rfo_ratio1 { get; set; }

			// Token: 0x1700011D RID: 285
			// (get) Token: 0x06000588 RID: 1416 RVA: 0x000250B5 File Offset: 0x000232B5
			// (set) Token: 0x06000589 RID: 1417 RVA: 0x000250BD File Offset: 0x000232BD
			public long rfo_ratio2 { get; set; }

			// Token: 0x1700011E RID: 286
			// (get) Token: 0x0600058A RID: 1418 RVA: 0x000250C6 File Offset: 0x000232C6
			// (set) Token: 0x0600058B RID: 1419 RVA: 0x000250CE File Offset: 0x000232CE
			public long rfo_ratio3 { get; set; }

			// Token: 0x1700011F RID: 287
			// (get) Token: 0x0600058C RID: 1420 RVA: 0x000250D7 File Offset: 0x000232D7
			// (set) Token: 0x0600058D RID: 1421 RVA: 0x000250DF File Offset: 0x000232DF
			public long ins_little { get; set; }

			// Token: 0x17000120 RID: 288
			// (get) Token: 0x0600058E RID: 1422 RVA: 0x000250E8 File Offset: 0x000232E8
			// (set) Token: 0x0600058F RID: 1423 RVA: 0x000250F0 File Offset: 0x000232F0
			public long clock_little { get; set; }

			// Token: 0x17000121 RID: 289
			// (get) Token: 0x06000590 RID: 1424 RVA: 0x000250F9 File Offset: 0x000232F9
			// (set) Token: 0x06000591 RID: 1425 RVA: 0x00025101 File Offset: 0x00023301
			public long ipc_big { get; set; }

			// Token: 0x17000122 RID: 290
			// (get) Token: 0x06000592 RID: 1426 RVA: 0x0002510A File Offset: 0x0002330A
			// (set) Token: 0x06000593 RID: 1427 RVA: 0x00025112 File Offset: 0x00023312
			public Service1.ProcessInfo Processinfo { get; set; }

			// Token: 0x17000123 RID: 291
			// (get) Token: 0x06000594 RID: 1428 RVA: 0x0002511B File Offset: 0x0002331B
			// (set) Token: 0x06000595 RID: 1429 RVA: 0x00025123 File Offset: 0x00023323
			public Service1.GroupInfo Groupinfo { get; set; }

			// Token: 0x17000124 RID: 292
			// (get) Token: 0x06000596 RID: 1430 RVA: 0x0002512C File Offset: 0x0002332C
			// (set) Token: 0x06000597 RID: 1431 RVA: 0x00025134 File Offset: 0x00023334
			public Service1.ThreadInfoSimp SimpThread { get; set; }

			// Token: 0x17000125 RID: 293
			// (get) Token: 0x06000598 RID: 1432 RVA: 0x0002513D File Offset: 0x0002333D
			// (set) Token: 0x06000599 RID: 1433 RVA: 0x00025145 File Offset: 0x00023345
			public Service1.PrevSchedInfo PrevSchedInfo { get; set; }

			// Token: 0x17000126 RID: 294
			// (get) Token: 0x0600059A RID: 1434 RVA: 0x0002514E File Offset: 0x0002334E
			// (set) Token: 0x0600059B RID: 1435 RVA: 0x00025156 File Offset: 0x00023356
			public Service1.ThreadInfo NextThread { get; set; }

			// Token: 0x17000127 RID: 295
			// (get) Token: 0x0600059C RID: 1436 RVA: 0x0002515F File Offset: 0x0002335F
			// (set) Token: 0x0600059D RID: 1437 RVA: 0x00025167 File Offset: 0x00023367
			public long sched_correct { get; set; }

			// Token: 0x17000128 RID: 296
			// (get) Token: 0x0600059E RID: 1438 RVA: 0x00025170 File Offset: 0x00023370
			// (set) Token: 0x0600059F RID: 1439 RVA: 0x00025178 File Offset: 0x00023378
			public long sched_wrong { get; set; }

			// Token: 0x17000129 RID: 297
			// (get) Token: 0x060005A0 RID: 1440 RVA: 0x00025181 File Offset: 0x00023381
			// (set) Token: 0x060005A1 RID: 1441 RVA: 0x00025189 File Offset: 0x00023389
			public long sched_corr_ratio { get; set; }

			// Token: 0x1700012A RID: 298
			// (get) Token: 0x060005A2 RID: 1442 RVA: 0x00025192 File Offset: 0x00023392
			// (set) Token: 0x060005A3 RID: 1443 RVA: 0x0002519A File Offset: 0x0002339A
			public long update_signal { get; set; }

			// Token: 0x1700012B RID: 299
			// (get) Token: 0x060005A4 RID: 1444 RVA: 0x000251A3 File Offset: 0x000233A3
			// (set) Token: 0x060005A5 RID: 1445 RVA: 0x000251AB File Offset: 0x000233AB
			public int Perflvl { get; set; }

			// Token: 0x1700012C RID: 300
			// (get) Token: 0x060005A6 RID: 1446 RVA: 0x000251B4 File Offset: 0x000233B4
			// (set) Token: 0x060005A7 RID: 1447 RVA: 0x000251BC File Offset: 0x000233BC
			public int Efflvl { get; set; }

			// Token: 0x060005A8 RID: 1448 RVA: 0x000251C5 File Offset: 0x000233C5
			public long CalcRatio(long data1, long data2, long source)
			{
				if (data2 > 0L)
				{
					return data1 / data2;
				}
				return source;
			}

			// Token: 0x060005A9 RID: 1449 RVA: 0x000251D1 File Offset: 0x000233D1
			public double CalcRatio1(long data1, long data2, double source)
			{
				if (data2 > 0L)
				{
					return (double)data1 / (double)data2;
				}
				return source;
			}
		}

		// Token: 0x02000087 RID: 135
		public class PrevSchedInfo
		{
			// Token: 0x1700012D RID: 301
			// (get) Token: 0x060005AA RID: 1450 RVA: 0x000251DF File Offset: 0x000233DF
			// (set) Token: 0x060005AB RID: 1451 RVA: 0x000251E7 File Offset: 0x000233E7
			public long PrevCoreType { get; set; }

			// Token: 0x1700012E RID: 302
			// (get) Token: 0x060005AC RID: 1452 RVA: 0x000251F0 File Offset: 0x000233F0
			// (set) Token: 0x060005AD RID: 1453 RVA: 0x000251F8 File Offset: 0x000233F8
			public long Ipc { get; set; }

			// Token: 0x1700012F RID: 303
			// (get) Token: 0x060005AE RID: 1454 RVA: 0x00025201 File Offset: 0x00023401
			// (set) Token: 0x060005AF RID: 1455 RVA: 0x00025209 File Offset: 0x00023409
			public long Ins_per_count { get; set; }

			// Token: 0x17000130 RID: 304
			// (get) Token: 0x060005B0 RID: 1456 RVA: 0x00025212 File Offset: 0x00023412
			// (set) Token: 0x060005B1 RID: 1457 RVA: 0x0002521A File Offset: 0x0002341A
			public long InsPressure { get; set; }

			// Token: 0x17000131 RID: 305
			// (get) Token: 0x060005B2 RID: 1458 RVA: 0x00025223 File Offset: 0x00023423
			// (set) Token: 0x060005B3 RID: 1459 RVA: 0x0002522B File Offset: 0x0002342B
			public long Clock { get; set; }

			// Token: 0x17000132 RID: 306
			// (get) Token: 0x060005B4 RID: 1460 RVA: 0x00025234 File Offset: 0x00023434
			// (set) Token: 0x060005B5 RID: 1461 RVA: 0x0002523C File Offset: 0x0002343C
			public long Ins_big { get; set; }

			// Token: 0x17000133 RID: 307
			// (get) Token: 0x060005B6 RID: 1462 RVA: 0x00025245 File Offset: 0x00023445
			// (set) Token: 0x060005B7 RID: 1463 RVA: 0x0002524D File Offset: 0x0002344D
			public long Clock_litte { get; set; }

			// Token: 0x17000134 RID: 308
			// (get) Token: 0x060005B8 RID: 1464 RVA: 0x00025256 File Offset: 0x00023456
			// (set) Token: 0x060005B9 RID: 1465 RVA: 0x0002525E File Offset: 0x0002345E
			public long Ipc_reset_count { get; set; }
		}

		// Token: 0x02000088 RID: 136
		public class ProcessInfo
		{
			// Token: 0x060005BB RID: 1467 RVA: 0x0002526F File Offset: 0x0002346F
			public ProcessInfo()
			{
			}

			// Token: 0x060005BC RID: 1468 RVA: 0x00025278 File Offset: 0x00023478
			public ProcessInfo(int pid, long datetime, long intval, long count, long runtime, long waittime, long duration, long instruction, int coreType, int lock1, long avg_Inspressure, long instruction_little, long instruction_ratio, int index, int perflvl, long maxinsdatetime, long activethreadstat_datetime)
			{
				this.Pid = pid;
				this.DateTime = datetime;
				this.IntVal = intval;
				this.Count = count;
				this.RunTime = runtime;
				this.WaitTime = waittime;
				this.Duration = duration;
				this.Instruction = instruction;
				this.Avg_Inspressure = avg_Inspressure;
				this.Perflvl = perflvl;
				this.Neuro_on = 0;
				this.Neuro_count = 0;
				this.sched_correct = 0L;
				this.sched_wrong = 0L;
				this.sched_revert = 0;
				this.MaxinsDatetime = maxinsdatetime;
				this.sched_corr_ratio = 100L;
				this.CoreType = coreType;
				this.Lock1 = lock1;
				this.NextProcess = null;
				this.ThreadSet = null;
				this.rfo_counters = 0L;
				this.rfo_counters1 = 0L;
				this.l2cr_counters = 0L;
				this.l2ref_counters = 0L;
				this.l2ref_counters1 = 0L;
				this.rfo_ratio = 0L;
				this.Avg_Inspressure = avg_Inspressure;
				this.Instruction_little = instruction_little;
				this.Instruction_ratio = instruction_ratio;
				this.Index = index;
				this.maxtime = 100L;
				this.maxtime1 = 100L;
				this.option = 1;
				this.update_signal = 0L;
				this.MaxinsLock = 0L;
				this.MaxinsOption1 = 0L;
				this.MaxinsOption2 = 0L;
				this.Maxins = 500000L;
				this.Activethreadstatcnt = 0L;
				this.Activethreadstat_datetime = activethreadstat_datetime;
				this.Activethreadstat_lock = 0;
				this.Avg_activethreadstatcnt = 100L;
				this.Initial_state = 1;
				this.MaxThreadId4lat = -1;
				this.MaxThreadId = -1;
				this.UpdateMaxThread = true;
				this.MaxThreadType = null;
			}

			// Token: 0x17000135 RID: 309
			// (get) Token: 0x060005BD RID: 1469 RVA: 0x00025404 File Offset: 0x00023604
			// (set) Token: 0x060005BE RID: 1470 RVA: 0x0002540C File Offset: 0x0002360C
			public bool UpdateMaxThread { get; set; }

			// Token: 0x17000136 RID: 310
			// (get) Token: 0x060005BF RID: 1471 RVA: 0x00025415 File Offset: 0x00023615
			// (set) Token: 0x060005C0 RID: 1472 RVA: 0x0002541D File Offset: 0x0002361D
			public string MaxThreadType { get; set; }

			// Token: 0x17000137 RID: 311
			// (get) Token: 0x060005C1 RID: 1473 RVA: 0x00025426 File Offset: 0x00023626
			// (set) Token: 0x060005C2 RID: 1474 RVA: 0x0002542E File Offset: 0x0002362E
			public int MaxThreadId { get; set; }

			// Token: 0x17000138 RID: 312
			// (get) Token: 0x060005C3 RID: 1475 RVA: 0x00025437 File Offset: 0x00023637
			// (set) Token: 0x060005C4 RID: 1476 RVA: 0x0002543F File Offset: 0x0002363F
			public int MaxThreadId4lat { get; set; }

			// Token: 0x17000139 RID: 313
			// (get) Token: 0x060005C5 RID: 1477 RVA: 0x00025448 File Offset: 0x00023648
			// (set) Token: 0x060005C6 RID: 1478 RVA: 0x00025450 File Offset: 0x00023650
			public int Initial_state { get; set; }

			// Token: 0x1700013A RID: 314
			// (get) Token: 0x060005C7 RID: 1479 RVA: 0x00025459 File Offset: 0x00023659
			// (set) Token: 0x060005C8 RID: 1480 RVA: 0x00025461 File Offset: 0x00023661
			public long Maxins { get; set; }

			// Token: 0x1700013B RID: 315
			// (get) Token: 0x060005C9 RID: 1481 RVA: 0x0002546A File Offset: 0x0002366A
			// (set) Token: 0x060005CA RID: 1482 RVA: 0x00025472 File Offset: 0x00023672
			public long MaxinsOption1 { get; set; }

			// Token: 0x1700013C RID: 316
			// (get) Token: 0x060005CB RID: 1483 RVA: 0x0002547B File Offset: 0x0002367B
			// (set) Token: 0x060005CC RID: 1484 RVA: 0x00025483 File Offset: 0x00023683
			public long MaxinsOption2 { get; set; }

			// Token: 0x1700013D RID: 317
			// (get) Token: 0x060005CD RID: 1485 RVA: 0x0002548C File Offset: 0x0002368C
			// (set) Token: 0x060005CE RID: 1486 RVA: 0x00025494 File Offset: 0x00023694
			public long MaxinsCount { get; set; }

			// Token: 0x1700013E RID: 318
			// (get) Token: 0x060005CF RID: 1487 RVA: 0x0002549D File Offset: 0x0002369D
			// (set) Token: 0x060005D0 RID: 1488 RVA: 0x000254A5 File Offset: 0x000236A5
			public long MaxinsDatetime { get; set; }

			// Token: 0x1700013F RID: 319
			// (get) Token: 0x060005D1 RID: 1489 RVA: 0x000254AE File Offset: 0x000236AE
			// (set) Token: 0x060005D2 RID: 1490 RVA: 0x000254B6 File Offset: 0x000236B6
			public long MaxinsLock { get; set; }

			// Token: 0x17000140 RID: 320
			// (get) Token: 0x060005D3 RID: 1491 RVA: 0x000254BF File Offset: 0x000236BF
			// (set) Token: 0x060005D4 RID: 1492 RVA: 0x000254C7 File Offset: 0x000236C7
			public long Observation_count { get; set; }

			// Token: 0x17000141 RID: 321
			// (get) Token: 0x060005D5 RID: 1493 RVA: 0x000254D0 File Offset: 0x000236D0
			// (set) Token: 0x060005D6 RID: 1494 RVA: 0x000254D8 File Offset: 0x000236D8
			public int Neuro_on { get; set; }

			// Token: 0x17000142 RID: 322
			// (get) Token: 0x060005D7 RID: 1495 RVA: 0x000254E1 File Offset: 0x000236E1
			// (set) Token: 0x060005D8 RID: 1496 RVA: 0x000254E9 File Offset: 0x000236E9
			public int Neuro_count { get; set; }

			// Token: 0x17000143 RID: 323
			// (get) Token: 0x060005D9 RID: 1497 RVA: 0x000254F2 File Offset: 0x000236F2
			// (set) Token: 0x060005DA RID: 1498 RVA: 0x000254FA File Offset: 0x000236FA
			public int Pid { get; set; }

			// Token: 0x17000144 RID: 324
			// (get) Token: 0x060005DB RID: 1499 RVA: 0x00025503 File Offset: 0x00023703
			// (set) Token: 0x060005DC RID: 1500 RVA: 0x0002550B File Offset: 0x0002370B
			public long DateTime { get; set; }

			// Token: 0x17000145 RID: 325
			// (get) Token: 0x060005DD RID: 1501 RVA: 0x00025514 File Offset: 0x00023714
			// (set) Token: 0x060005DE RID: 1502 RVA: 0x0002551C File Offset: 0x0002371C
			public long IntVal { get; set; }

			// Token: 0x17000146 RID: 326
			// (get) Token: 0x060005DF RID: 1503 RVA: 0x00025525 File Offset: 0x00023725
			// (set) Token: 0x060005E0 RID: 1504 RVA: 0x0002552D File Offset: 0x0002372D
			public long Count { get; set; }

			// Token: 0x17000147 RID: 327
			// (get) Token: 0x060005E1 RID: 1505 RVA: 0x00025536 File Offset: 0x00023736
			// (set) Token: 0x060005E2 RID: 1506 RVA: 0x0002553E File Offset: 0x0002373E
			public long RunTime { get; set; }

			// Token: 0x17000148 RID: 328
			// (get) Token: 0x060005E3 RID: 1507 RVA: 0x00025547 File Offset: 0x00023747
			// (set) Token: 0x060005E4 RID: 1508 RVA: 0x0002554F File Offset: 0x0002374F
			public long WaitTime { get; set; }

			// Token: 0x17000149 RID: 329
			// (get) Token: 0x060005E5 RID: 1509 RVA: 0x00025558 File Offset: 0x00023758
			// (set) Token: 0x060005E6 RID: 1510 RVA: 0x00025560 File Offset: 0x00023760
			public long Duration { get; set; }

			// Token: 0x1700014A RID: 330
			// (get) Token: 0x060005E7 RID: 1511 RVA: 0x00025569 File Offset: 0x00023769
			// (set) Token: 0x060005E8 RID: 1512 RVA: 0x00025571 File Offset: 0x00023771
			public long Instruction { get; set; }

			// Token: 0x1700014B RID: 331
			// (get) Token: 0x060005E9 RID: 1513 RVA: 0x0002557A File Offset: 0x0002377A
			// (set) Token: 0x060005EA RID: 1514 RVA: 0x00025582 File Offset: 0x00023782
			public long Instruction_little { get; set; }

			// Token: 0x1700014C RID: 332
			// (get) Token: 0x060005EB RID: 1515 RVA: 0x0002558B File Offset: 0x0002378B
			// (set) Token: 0x060005EC RID: 1516 RVA: 0x00025593 File Offset: 0x00023793
			public long Instruction_ratio { get; set; }

			// Token: 0x1700014D RID: 333
			// (get) Token: 0x060005ED RID: 1517 RVA: 0x0002559C File Offset: 0x0002379C
			// (set) Token: 0x060005EE RID: 1518 RVA: 0x000255A4 File Offset: 0x000237A4
			public long Avg_Inspressure { get; set; }

			// Token: 0x1700014E RID: 334
			// (get) Token: 0x060005EF RID: 1519 RVA: 0x000255AD File Offset: 0x000237AD
			// (set) Token: 0x060005F0 RID: 1520 RVA: 0x000255B5 File Offset: 0x000237B5
			public int CoreType { get; set; }

			// Token: 0x1700014F RID: 335
			// (get) Token: 0x060005F1 RID: 1521 RVA: 0x000255BE File Offset: 0x000237BE
			// (set) Token: 0x060005F2 RID: 1522 RVA: 0x000255C6 File Offset: 0x000237C6
			public int Lock1 { get; set; }

			// Token: 0x17000150 RID: 336
			// (get) Token: 0x060005F3 RID: 1523 RVA: 0x000255CF File Offset: 0x000237CF
			// (set) Token: 0x060005F4 RID: 1524 RVA: 0x000255D7 File Offset: 0x000237D7
			public long sched_correct { get; set; }

			// Token: 0x17000151 RID: 337
			// (get) Token: 0x060005F5 RID: 1525 RVA: 0x000255E0 File Offset: 0x000237E0
			// (set) Token: 0x060005F6 RID: 1526 RVA: 0x000255E8 File Offset: 0x000237E8
			public long sched_wrong { get; set; }

			// Token: 0x17000152 RID: 338
			// (get) Token: 0x060005F7 RID: 1527 RVA: 0x000255F1 File Offset: 0x000237F1
			// (set) Token: 0x060005F8 RID: 1528 RVA: 0x000255F9 File Offset: 0x000237F9
			public long sched_corr_ratio { get; set; }

			// Token: 0x17000153 RID: 339
			// (get) Token: 0x060005F9 RID: 1529 RVA: 0x00025602 File Offset: 0x00023802
			// (set) Token: 0x060005FA RID: 1530 RVA: 0x0002560A File Offset: 0x0002380A
			public int sched_revert { get; set; }

			// Token: 0x17000154 RID: 340
			// (get) Token: 0x060005FB RID: 1531 RVA: 0x00025613 File Offset: 0x00023813
			// (set) Token: 0x060005FC RID: 1532 RVA: 0x0002561B File Offset: 0x0002381B
			public int Index { get; set; }

			// Token: 0x17000155 RID: 341
			// (get) Token: 0x060005FD RID: 1533 RVA: 0x00025624 File Offset: 0x00023824
			// (set) Token: 0x060005FE RID: 1534 RVA: 0x0002562C File Offset: 0x0002382C
			public int Perflvl { get; set; }

			// Token: 0x17000156 RID: 342
			// (get) Token: 0x060005FF RID: 1535 RVA: 0x00025635 File Offset: 0x00023835
			// (set) Token: 0x06000600 RID: 1536 RVA: 0x0002563D File Offset: 0x0002383D
			public long rfo_counters { get; set; }

			// Token: 0x17000157 RID: 343
			// (get) Token: 0x06000601 RID: 1537 RVA: 0x00025646 File Offset: 0x00023846
			// (set) Token: 0x06000602 RID: 1538 RVA: 0x0002564E File Offset: 0x0002384E
			public long l2ref_counters { get; set; }

			// Token: 0x17000158 RID: 344
			// (get) Token: 0x06000603 RID: 1539 RVA: 0x00025657 File Offset: 0x00023857
			// (set) Token: 0x06000604 RID: 1540 RVA: 0x0002565F File Offset: 0x0002385F
			public long rfo_counters1 { get; set; }

			// Token: 0x17000159 RID: 345
			// (get) Token: 0x06000605 RID: 1541 RVA: 0x00025668 File Offset: 0x00023868
			// (set) Token: 0x06000606 RID: 1542 RVA: 0x00025670 File Offset: 0x00023870
			public long rfo_counters2 { get; set; }

			// Token: 0x1700015A RID: 346
			// (get) Token: 0x06000607 RID: 1543 RVA: 0x00025679 File Offset: 0x00023879
			// (set) Token: 0x06000608 RID: 1544 RVA: 0x00025681 File Offset: 0x00023881
			public long rfo_counters3 { get; set; }

			// Token: 0x1700015B RID: 347
			// (get) Token: 0x06000609 RID: 1545 RVA: 0x0002568A File Offset: 0x0002388A
			// (set) Token: 0x0600060A RID: 1546 RVA: 0x00025692 File Offset: 0x00023892
			public long l2ref_counters1 { get; set; }

			// Token: 0x1700015C RID: 348
			// (get) Token: 0x0600060B RID: 1547 RVA: 0x0002569B File Offset: 0x0002389B
			// (set) Token: 0x0600060C RID: 1548 RVA: 0x000256A3 File Offset: 0x000238A3
			public long l2ref_counters2 { get; set; }

			// Token: 0x1700015D RID: 349
			// (get) Token: 0x0600060D RID: 1549 RVA: 0x000256AC File Offset: 0x000238AC
			// (set) Token: 0x0600060E RID: 1550 RVA: 0x000256B4 File Offset: 0x000238B4
			public long l2ref_counters3 { get; set; }

			// Token: 0x1700015E RID: 350
			// (get) Token: 0x0600060F RID: 1551 RVA: 0x000256BD File Offset: 0x000238BD
			// (set) Token: 0x06000610 RID: 1552 RVA: 0x000256C5 File Offset: 0x000238C5
			public long avg_ins { get; set; }

			// Token: 0x1700015F RID: 351
			// (get) Token: 0x06000611 RID: 1553 RVA: 0x000256CE File Offset: 0x000238CE
			// (set) Token: 0x06000612 RID: 1554 RVA: 0x000256D6 File Offset: 0x000238D6
			public long avg_ins1 { get; set; }

			// Token: 0x17000160 RID: 352
			// (get) Token: 0x06000613 RID: 1555 RVA: 0x000256DF File Offset: 0x000238DF
			// (set) Token: 0x06000614 RID: 1556 RVA: 0x000256E7 File Offset: 0x000238E7
			public long avg_ins2 { get; set; }

			// Token: 0x17000161 RID: 353
			// (get) Token: 0x06000615 RID: 1557 RVA: 0x000256F0 File Offset: 0x000238F0
			// (set) Token: 0x06000616 RID: 1558 RVA: 0x000256F8 File Offset: 0x000238F8
			public long avg_ins3 { get; set; }

			// Token: 0x17000162 RID: 354
			// (get) Token: 0x06000617 RID: 1559 RVA: 0x00025701 File Offset: 0x00023901
			// (set) Token: 0x06000618 RID: 1560 RVA: 0x00025709 File Offset: 0x00023909
			public long l2cr_counters { get; set; }

			// Token: 0x17000163 RID: 355
			// (get) Token: 0x06000619 RID: 1561 RVA: 0x00025712 File Offset: 0x00023912
			// (set) Token: 0x0600061A RID: 1562 RVA: 0x0002571A File Offset: 0x0002391A
			public long rfo_ratio { get; set; }

			// Token: 0x17000164 RID: 356
			// (get) Token: 0x0600061B RID: 1563 RVA: 0x00025723 File Offset: 0x00023923
			// (set) Token: 0x0600061C RID: 1564 RVA: 0x0002572B File Offset: 0x0002392B
			public long maxtime { get; set; }

			// Token: 0x17000165 RID: 357
			// (get) Token: 0x0600061D RID: 1565 RVA: 0x00025734 File Offset: 0x00023934
			// (set) Token: 0x0600061E RID: 1566 RVA: 0x0002573C File Offset: 0x0002393C
			public long maxtime1 { get; set; }

			// Token: 0x17000166 RID: 358
			// (get) Token: 0x0600061F RID: 1567 RVA: 0x00025745 File Offset: 0x00023945
			// (set) Token: 0x06000620 RID: 1568 RVA: 0x0002574D File Offset: 0x0002394D
			public long maxtime2 { get; set; }

			// Token: 0x17000167 RID: 359
			// (get) Token: 0x06000621 RID: 1569 RVA: 0x00025756 File Offset: 0x00023956
			// (set) Token: 0x06000622 RID: 1570 RVA: 0x0002575E File Offset: 0x0002395E
			public long runtime_counter { get; set; }

			// Token: 0x17000168 RID: 360
			// (get) Token: 0x06000623 RID: 1571 RVA: 0x00025767 File Offset: 0x00023967
			// (set) Token: 0x06000624 RID: 1572 RVA: 0x0002576F File Offset: 0x0002396F
			public long runtime { get; set; }

			// Token: 0x17000169 RID: 361
			// (get) Token: 0x06000625 RID: 1573 RVA: 0x00025778 File Offset: 0x00023978
			// (set) Token: 0x06000626 RID: 1574 RVA: 0x00025780 File Offset: 0x00023980
			public long runtime_counter1 { get; set; }

			// Token: 0x1700016A RID: 362
			// (get) Token: 0x06000627 RID: 1575 RVA: 0x00025789 File Offset: 0x00023989
			// (set) Token: 0x06000628 RID: 1576 RVA: 0x00025791 File Offset: 0x00023991
			public long runtime1 { get; set; }

			// Token: 0x1700016B RID: 363
			// (get) Token: 0x06000629 RID: 1577 RVA: 0x0002579A File Offset: 0x0002399A
			// (set) Token: 0x0600062A RID: 1578 RVA: 0x000257A2 File Offset: 0x000239A2
			public long datetime_elapse { get; set; }

			// Token: 0x1700016C RID: 364
			// (get) Token: 0x0600062B RID: 1579 RVA: 0x000257AB File Offset: 0x000239AB
			// (set) Token: 0x0600062C RID: 1580 RVA: 0x000257B3 File Offset: 0x000239B3
			public long update_signal { get; set; }

			// Token: 0x1700016D RID: 365
			// (get) Token: 0x0600062D RID: 1581 RVA: 0x000257BC File Offset: 0x000239BC
			// (set) Token: 0x0600062E RID: 1582 RVA: 0x000257C4 File Offset: 0x000239C4
			public int option { get; set; }

			// Token: 0x1700016E RID: 366
			// (get) Token: 0x0600062F RID: 1583 RVA: 0x000257CD File Offset: 0x000239CD
			// (set) Token: 0x06000630 RID: 1584 RVA: 0x000257D5 File Offset: 0x000239D5
			public Service1.ProcessInfo NextProcess { get; set; }

			// Token: 0x1700016F RID: 367
			// (get) Token: 0x06000631 RID: 1585 RVA: 0x000257DE File Offset: 0x000239DE
			// (set) Token: 0x06000632 RID: 1586 RVA: 0x000257E6 File Offset: 0x000239E6
			public Service1.ThreadInfoSimp ThreadSet { get; set; }

			// Token: 0x17000170 RID: 368
			// (get) Token: 0x06000633 RID: 1587 RVA: 0x000257EF File Offset: 0x000239EF
			// (set) Token: 0x06000634 RID: 1588 RVA: 0x000257F7 File Offset: 0x000239F7
			public long Activethread4perf_count { get; set; }

			// Token: 0x17000171 RID: 369
			// (get) Token: 0x06000635 RID: 1589 RVA: 0x00025800 File Offset: 0x00023A00
			// (set) Token: 0x06000636 RID: 1590 RVA: 0x00025808 File Offset: 0x00023A08
			public long Activethread4eff_count { get; set; }

			// Token: 0x17000172 RID: 370
			// (get) Token: 0x06000637 RID: 1591 RVA: 0x00025811 File Offset: 0x00023A11
			// (set) Token: 0x06000638 RID: 1592 RVA: 0x00025819 File Offset: 0x00023A19
			public long Activethreadstatcnt { get; set; }

			// Token: 0x17000173 RID: 371
			// (get) Token: 0x06000639 RID: 1593 RVA: 0x00025822 File Offset: 0x00023A22
			// (set) Token: 0x0600063A RID: 1594 RVA: 0x0002582A File Offset: 0x00023A2A
			public long Acc_Activethreadstatcnt { get; set; }

			// Token: 0x17000174 RID: 372
			// (get) Token: 0x0600063B RID: 1595 RVA: 0x00025833 File Offset: 0x00023A33
			// (set) Token: 0x0600063C RID: 1596 RVA: 0x0002583B File Offset: 0x00023A3B
			public long Activethreadstat_datetime { get; set; }

			// Token: 0x17000175 RID: 373
			// (get) Token: 0x0600063D RID: 1597 RVA: 0x00025844 File Offset: 0x00023A44
			// (set) Token: 0x0600063E RID: 1598 RVA: 0x0002584C File Offset: 0x00023A4C
			public int Activethreadstat_lock { get; set; }

			// Token: 0x17000176 RID: 374
			// (get) Token: 0x0600063F RID: 1599 RVA: 0x00025855 File Offset: 0x00023A55
			// (set) Token: 0x06000640 RID: 1600 RVA: 0x0002585D File Offset: 0x00023A5D
			public long Avg_activethreadstatcnt { get; set; }

			// Token: 0x17000177 RID: 375
			// (get) Token: 0x06000641 RID: 1601 RVA: 0x00025866 File Offset: 0x00023A66
			// (set) Token: 0x06000642 RID: 1602 RVA: 0x0002586E File Offset: 0x00023A6E
			public int SchedMode { get; set; }
		}

		// Token: 0x02000089 RID: 137
		public class CoreInfo
		{
			// Token: 0x06000643 RID: 1603 RVA: 0x00025877 File Offset: 0x00023A77
			public CoreInfo()
			{
			}

			// Token: 0x06000644 RID: 1604 RVA: 0x0002588C File Offset: 0x00023A8C
			public CoreInfo(int cid, long datetime, long intval, long count, long runtime, long waittime, long duration, long utilization, uint affinity, uint affinity_origin, long dateTime4q, long dateTime4sched)
			{
				this.Cid = cid;
				this.DateTime = datetime;
				this.IntVal = intval;
				this.RunTime = runtime;
				this.WaitTime = waittime;
				this.Duration = duration;
				this.Count = count;
				this.Utilization = utilization;
				this.Affinity = affinity;
				this.Affinity_origin = affinity_origin;
				this.ThreadSet = null;
				this.Threadinfosimp = null;
				this.Idletime = 0L;
				this.RunTime4queque = 0L;
				this.Threadcount = 0L;
				this.SustainedThreadcount = 0L;
				this.Utilization4q = 0L;
				this.DateTime4q = dateTime4q;
				this.CounterEnabled = 0;
				this.DateTime4sched = dateTime4sched;
				this.threadexecinfo = new Service1.ThreadExecutionRegistry();
				this.Accthreadcount = 0L;
				this.Avgthreadcount = 0L;
				this.Avgthreadtime = 0L;
				this.numberProcessor = new Service1.NumberProcessor();
				this.AccMaxTime = 0L;
				this.AvgMaxTime = 0L;
				this.instructions4sys = 0L;
				this.instructions4sys_e = 0L;
				this.instructions4sys_l = 0L;
				this.total_energy = 0L;
				this.total_energy_e = 0L;
				this.total_energy_l = 0L;
				this.RunTime4usage = 0L;
				this.accRunTime4usage = 0L;
				this.cycles4sys = 0L;
				this.cycles4sys_e = 0L;
				this.cycles4sys_l = 0L;
				this.missrate4c = 0L;
				this.missrate4c_e = 0L;
				this.missrate4c_l = 0L;
				this.ipc4c = 0f;
				this.missrateratio4c = 0f;
				this.mem_ordering = 0f;
				this.perf4c = 0f;
				this.load = 0L;
				this.load_e = 0L;
				this.load_l = 0L;
				this.store = 0L;
				this.store_e = 0L;
				this.store_l = 0L;
				this.mem_ordering_count = 0L;
				this.mem_ordering_count_e = 0L;
				this.mem_ordering_count_l = 0L;
				this.QueueInterval = 0L;
			}

			// Token: 0x17000178 RID: 376
			// (get) Token: 0x06000645 RID: 1605 RVA: 0x00025A6E File Offset: 0x00023C6E
			// (set) Token: 0x06000646 RID: 1606 RVA: 0x00025A76 File Offset: 0x00023C76
			public long accRewardPerQ { get; set; }

			// Token: 0x17000179 RID: 377
			// (get) Token: 0x06000647 RID: 1607 RVA: 0x00025A7F File Offset: 0x00023C7F
			// (set) Token: 0x06000648 RID: 1608 RVA: 0x00025A87 File Offset: 0x00023C87
			public long accRuntimePerQ { get; set; }

			// Token: 0x1700017A RID: 378
			// (get) Token: 0x06000649 RID: 1609 RVA: 0x00025A90 File Offset: 0x00023C90
			// (set) Token: 0x0600064A RID: 1610 RVA: 0x00025A98 File Offset: 0x00023C98
			public long mem_ordering_count { get; set; }

			// Token: 0x1700017B RID: 379
			// (get) Token: 0x0600064B RID: 1611 RVA: 0x00025AA1 File Offset: 0x00023CA1
			// (set) Token: 0x0600064C RID: 1612 RVA: 0x00025AA9 File Offset: 0x00023CA9
			public long mem_ordering_count_e { get; set; }

			// Token: 0x1700017C RID: 380
			// (get) Token: 0x0600064D RID: 1613 RVA: 0x00025AB2 File Offset: 0x00023CB2
			// (set) Token: 0x0600064E RID: 1614 RVA: 0x00025ABA File Offset: 0x00023CBA
			public long mem_ordering_count_l { get; set; }

			// Token: 0x1700017D RID: 381
			// (get) Token: 0x0600064F RID: 1615 RVA: 0x00025AC3 File Offset: 0x00023CC3
			// (set) Token: 0x06000650 RID: 1616 RVA: 0x00025ACB File Offset: 0x00023CCB
			public float mem_ordering { get; set; }

			// Token: 0x1700017E RID: 382
			// (get) Token: 0x06000651 RID: 1617 RVA: 0x00025AD4 File Offset: 0x00023CD4
			// (set) Token: 0x06000652 RID: 1618 RVA: 0x00025ADC File Offset: 0x00023CDC
			public float perf4c { get; set; }

			// Token: 0x1700017F RID: 383
			// (get) Token: 0x06000653 RID: 1619 RVA: 0x00025AE5 File Offset: 0x00023CE5
			// (set) Token: 0x06000654 RID: 1620 RVA: 0x00025AED File Offset: 0x00023CED
			public float missrateratio4c { get; set; }

			// Token: 0x17000180 RID: 384
			// (get) Token: 0x06000655 RID: 1621 RVA: 0x00025AF6 File Offset: 0x00023CF6
			// (set) Token: 0x06000656 RID: 1622 RVA: 0x00025AFE File Offset: 0x00023CFE
			public float ipc4c { get; set; }

			// Token: 0x17000181 RID: 385
			// (get) Token: 0x06000657 RID: 1623 RVA: 0x00025B07 File Offset: 0x00023D07
			// (set) Token: 0x06000658 RID: 1624 RVA: 0x00025B0F File Offset: 0x00023D0F
			public long missrate4c { get; set; }

			// Token: 0x17000182 RID: 386
			// (get) Token: 0x06000659 RID: 1625 RVA: 0x00025B18 File Offset: 0x00023D18
			// (set) Token: 0x0600065A RID: 1626 RVA: 0x00025B20 File Offset: 0x00023D20
			public long missrate4c_e { get; set; }

			// Token: 0x17000183 RID: 387
			// (get) Token: 0x0600065B RID: 1627 RVA: 0x00025B29 File Offset: 0x00023D29
			// (set) Token: 0x0600065C RID: 1628 RVA: 0x00025B31 File Offset: 0x00023D31
			public long missrate4c_l { get; set; }

			// Token: 0x17000184 RID: 388
			// (get) Token: 0x0600065D RID: 1629 RVA: 0x00025B3A File Offset: 0x00023D3A
			// (set) Token: 0x0600065E RID: 1630 RVA: 0x00025B42 File Offset: 0x00023D42
			public long load { get; set; }

			// Token: 0x17000185 RID: 389
			// (get) Token: 0x0600065F RID: 1631 RVA: 0x00025B4B File Offset: 0x00023D4B
			// (set) Token: 0x06000660 RID: 1632 RVA: 0x00025B53 File Offset: 0x00023D53
			public long load_e { get; set; }

			// Token: 0x17000186 RID: 390
			// (get) Token: 0x06000661 RID: 1633 RVA: 0x00025B5C File Offset: 0x00023D5C
			// (set) Token: 0x06000662 RID: 1634 RVA: 0x00025B64 File Offset: 0x00023D64
			public long load_l { get; set; }

			// Token: 0x17000187 RID: 391
			// (get) Token: 0x06000663 RID: 1635 RVA: 0x00025B6D File Offset: 0x00023D6D
			// (set) Token: 0x06000664 RID: 1636 RVA: 0x00025B75 File Offset: 0x00023D75
			public long store { get; set; }

			// Token: 0x17000188 RID: 392
			// (get) Token: 0x06000665 RID: 1637 RVA: 0x00025B7E File Offset: 0x00023D7E
			// (set) Token: 0x06000666 RID: 1638 RVA: 0x00025B86 File Offset: 0x00023D86
			public long store_e { get; set; }

			// Token: 0x17000189 RID: 393
			// (get) Token: 0x06000667 RID: 1639 RVA: 0x00025B8F File Offset: 0x00023D8F
			// (set) Token: 0x06000668 RID: 1640 RVA: 0x00025B97 File Offset: 0x00023D97
			public long store_l { get; set; }

			// Token: 0x1700018A RID: 394
			// (get) Token: 0x06000669 RID: 1641 RVA: 0x00025BA0 File Offset: 0x00023DA0
			// (set) Token: 0x0600066A RID: 1642 RVA: 0x00025BA8 File Offset: 0x00023DA8
			public long cycles4sys { get; set; }

			// Token: 0x1700018B RID: 395
			// (get) Token: 0x0600066B RID: 1643 RVA: 0x00025BB1 File Offset: 0x00023DB1
			// (set) Token: 0x0600066C RID: 1644 RVA: 0x00025BB9 File Offset: 0x00023DB9
			public long cycles4sys_e { get; set; }

			// Token: 0x1700018C RID: 396
			// (get) Token: 0x0600066D RID: 1645 RVA: 0x00025BC2 File Offset: 0x00023DC2
			// (set) Token: 0x0600066E RID: 1646 RVA: 0x00025BCA File Offset: 0x00023DCA
			public long cycles4sys_l { get; set; }

			// Token: 0x1700018D RID: 397
			// (get) Token: 0x0600066F RID: 1647 RVA: 0x00025BD3 File Offset: 0x00023DD3
			// (set) Token: 0x06000670 RID: 1648 RVA: 0x00025BDB File Offset: 0x00023DDB
			public long instructions4sys { get; set; }

			// Token: 0x1700018E RID: 398
			// (get) Token: 0x06000671 RID: 1649 RVA: 0x00025BE4 File Offset: 0x00023DE4
			// (set) Token: 0x06000672 RID: 1650 RVA: 0x00025BEC File Offset: 0x00023DEC
			public long instructions4sys_e { get; set; }

			// Token: 0x1700018F RID: 399
			// (get) Token: 0x06000673 RID: 1651 RVA: 0x00025BF5 File Offset: 0x00023DF5
			// (set) Token: 0x06000674 RID: 1652 RVA: 0x00025BFD File Offset: 0x00023DFD
			public long instructions4sys_l { get; set; }

			// Token: 0x17000190 RID: 400
			// (get) Token: 0x06000675 RID: 1653 RVA: 0x00025C06 File Offset: 0x00023E06
			// (set) Token: 0x06000676 RID: 1654 RVA: 0x00025C0E File Offset: 0x00023E0E
			public long total_energy { get; set; }

			// Token: 0x17000191 RID: 401
			// (get) Token: 0x06000677 RID: 1655 RVA: 0x00025C17 File Offset: 0x00023E17
			// (set) Token: 0x06000678 RID: 1656 RVA: 0x00025C1F File Offset: 0x00023E1F
			public long total_energy_e { get; set; }

			// Token: 0x17000192 RID: 402
			// (get) Token: 0x06000679 RID: 1657 RVA: 0x00025C28 File Offset: 0x00023E28
			// (set) Token: 0x0600067A RID: 1658 RVA: 0x00025C30 File Offset: 0x00023E30
			public long total_energy_l { get; set; }

			// Token: 0x17000193 RID: 403
			// (get) Token: 0x0600067B RID: 1659 RVA: 0x00025C39 File Offset: 0x00023E39
			// (set) Token: 0x0600067C RID: 1660 RVA: 0x00025C41 File Offset: 0x00023E41
			public long AccMaxTime { get; set; }

			// Token: 0x17000194 RID: 404
			// (get) Token: 0x0600067D RID: 1661 RVA: 0x00025C4A File Offset: 0x00023E4A
			// (set) Token: 0x0600067E RID: 1662 RVA: 0x00025C52 File Offset: 0x00023E52
			public long AvgMaxTime { get; set; }

			// Token: 0x17000195 RID: 405
			// (get) Token: 0x0600067F RID: 1663 RVA: 0x00025C5B File Offset: 0x00023E5B
			// (set) Token: 0x06000680 RID: 1664 RVA: 0x00025C63 File Offset: 0x00023E63
			public Service1.NumberProcessor numberProcessor { get; set; }

			// Token: 0x17000196 RID: 406
			// (get) Token: 0x06000681 RID: 1665 RVA: 0x00025C6C File Offset: 0x00023E6C
			// (set) Token: 0x06000682 RID: 1666 RVA: 0x00025C74 File Offset: 0x00023E74
			public int Cid { get; set; }

			// Token: 0x17000197 RID: 407
			// (get) Token: 0x06000683 RID: 1667 RVA: 0x00025C7D File Offset: 0x00023E7D
			// (set) Token: 0x06000684 RID: 1668 RVA: 0x00025C85 File Offset: 0x00023E85
			public long DateTime { get; set; }

			// Token: 0x17000198 RID: 408
			// (get) Token: 0x06000685 RID: 1669 RVA: 0x00025C8E File Offset: 0x00023E8E
			// (set) Token: 0x06000686 RID: 1670 RVA: 0x00025C96 File Offset: 0x00023E96
			public long DateTime4sched { get; set; }

			// Token: 0x17000199 RID: 409
			// (get) Token: 0x06000687 RID: 1671 RVA: 0x00025C9F File Offset: 0x00023E9F
			// (set) Token: 0x06000688 RID: 1672 RVA: 0x00025CA7 File Offset: 0x00023EA7
			public long DateTime4q { get; set; }

			// Token: 0x1700019A RID: 410
			// (get) Token: 0x06000689 RID: 1673 RVA: 0x00025CB0 File Offset: 0x00023EB0
			// (set) Token: 0x0600068A RID: 1674 RVA: 0x00025CB8 File Offset: 0x00023EB8
			public long IntVal { get; set; }

			// Token: 0x1700019B RID: 411
			// (get) Token: 0x0600068B RID: 1675 RVA: 0x00025CC1 File Offset: 0x00023EC1
			// (set) Token: 0x0600068C RID: 1676 RVA: 0x00025CC9 File Offset: 0x00023EC9
			public long Count { get; set; }

			// Token: 0x1700019C RID: 412
			// (get) Token: 0x0600068D RID: 1677 RVA: 0x00025CD2 File Offset: 0x00023ED2
			// (set) Token: 0x0600068E RID: 1678 RVA: 0x00025CDA File Offset: 0x00023EDA
			public int CounterEnabled { get; set; }

			// Token: 0x1700019D RID: 413
			// (get) Token: 0x0600068F RID: 1679 RVA: 0x00025CE3 File Offset: 0x00023EE3
			// (set) Token: 0x06000690 RID: 1680 RVA: 0x00025CEB File Offset: 0x00023EEB
			public long Idletime { get; set; }

			// Token: 0x1700019E RID: 414
			// (get) Token: 0x06000691 RID: 1681 RVA: 0x00025CF4 File Offset: 0x00023EF4
			// (set) Token: 0x06000692 RID: 1682 RVA: 0x00025CFC File Offset: 0x00023EFC
			public uint Affinity { get; set; }

			// Token: 0x1700019F RID: 415
			// (get) Token: 0x06000693 RID: 1683 RVA: 0x00025D05 File Offset: 0x00023F05
			// (set) Token: 0x06000694 RID: 1684 RVA: 0x00025D0D File Offset: 0x00023F0D
			public uint Affinity_origin { get; set; }

			// Token: 0x170001A0 RID: 416
			// (get) Token: 0x06000695 RID: 1685 RVA: 0x00025D16 File Offset: 0x00023F16
			// (set) Token: 0x06000696 RID: 1686 RVA: 0x00025D1E File Offset: 0x00023F1E
			public long RunTime { get; set; }

			// Token: 0x170001A1 RID: 417
			// (get) Token: 0x06000697 RID: 1687 RVA: 0x00025D27 File Offset: 0x00023F27
			// (set) Token: 0x06000698 RID: 1688 RVA: 0x00025D2F File Offset: 0x00023F2F
			public long RunTime4queque { get; set; }

			// Token: 0x170001A2 RID: 418
			// (get) Token: 0x06000699 RID: 1689 RVA: 0x00025D38 File Offset: 0x00023F38
			// (set) Token: 0x0600069A RID: 1690 RVA: 0x00025D40 File Offset: 0x00023F40
			public long RunTime4queque4sched { get; set; }

			// Token: 0x170001A3 RID: 419
			// (get) Token: 0x0600069B RID: 1691 RVA: 0x00025D49 File Offset: 0x00023F49
			// (set) Token: 0x0600069C RID: 1692 RVA: 0x00025D51 File Offset: 0x00023F51
			public long RunTime4usage { get; set; }

			// Token: 0x170001A4 RID: 420
			// (get) Token: 0x0600069D RID: 1693 RVA: 0x00025D5A File Offset: 0x00023F5A
			// (set) Token: 0x0600069E RID: 1694 RVA: 0x00025D62 File Offset: 0x00023F62
			public long accRunTime4usage { get; set; }

			// Token: 0x170001A5 RID: 421
			// (get) Token: 0x0600069F RID: 1695 RVA: 0x00025D6B File Offset: 0x00023F6B
			// (set) Token: 0x060006A0 RID: 1696 RVA: 0x00025D73 File Offset: 0x00023F73
			public long Threadcount { get; set; }

			// Token: 0x170001A6 RID: 422
			// (get) Token: 0x060006A1 RID: 1697 RVA: 0x00025D7C File Offset: 0x00023F7C
			// (set) Token: 0x060006A2 RID: 1698 RVA: 0x00025D84 File Offset: 0x00023F84
			public long SustainedThreadcount { get; set; }

			// Token: 0x170001A7 RID: 423
			// (get) Token: 0x060006A3 RID: 1699 RVA: 0x00025D8D File Offset: 0x00023F8D
			// (set) Token: 0x060006A4 RID: 1700 RVA: 0x00025D95 File Offset: 0x00023F95
			public long SustainedThreadcount4sched { get; set; }

			// Token: 0x170001A8 RID: 424
			// (get) Token: 0x060006A5 RID: 1701 RVA: 0x00025D9E File Offset: 0x00023F9E
			// (set) Token: 0x060006A6 RID: 1702 RVA: 0x00025DA6 File Offset: 0x00023FA6
			public long WaitTime { get; set; }

			// Token: 0x170001A9 RID: 425
			// (get) Token: 0x060006A7 RID: 1703 RVA: 0x00025DAF File Offset: 0x00023FAF
			// (set) Token: 0x060006A8 RID: 1704 RVA: 0x00025DB7 File Offset: 0x00023FB7
			public long Duration { get; set; }

			// Token: 0x170001AA RID: 426
			// (get) Token: 0x060006A9 RID: 1705 RVA: 0x00025DC0 File Offset: 0x00023FC0
			// (set) Token: 0x060006AA RID: 1706 RVA: 0x00025DC8 File Offset: 0x00023FC8
			public long Intvaltime { get; set; }

			// Token: 0x170001AB RID: 427
			// (get) Token: 0x060006AB RID: 1707 RVA: 0x00025DD1 File Offset: 0x00023FD1
			// (set) Token: 0x060006AC RID: 1708 RVA: 0x00025DD9 File Offset: 0x00023FD9
			public long QueueInterval { get; set; }

			// Token: 0x170001AC RID: 428
			// (get) Token: 0x060006AD RID: 1709 RVA: 0x00025DE2 File Offset: 0x00023FE2
			// (set) Token: 0x060006AE RID: 1710 RVA: 0x00025DEA File Offset: 0x00023FEA
			public long Utilization { get; set; }

			// Token: 0x170001AD RID: 429
			// (get) Token: 0x060006AF RID: 1711 RVA: 0x00025DF3 File Offset: 0x00023FF3
			// (set) Token: 0x060006B0 RID: 1712 RVA: 0x00025DFB File Offset: 0x00023FFB
			public long Utilization4sched { get; set; }

			// Token: 0x170001AE RID: 430
			// (get) Token: 0x060006B1 RID: 1713 RVA: 0x00025E04 File Offset: 0x00024004
			// (set) Token: 0x060006B2 RID: 1714 RVA: 0x00025E0C File Offset: 0x0002400C
			public long Utilization4q { get; set; }

			// Token: 0x170001AF RID: 431
			// (get) Token: 0x060006B3 RID: 1715 RVA: 0x00025E15 File Offset: 0x00024015
			// (set) Token: 0x060006B4 RID: 1716 RVA: 0x00025E1D File Offset: 0x0002401D
			public long Utilization4q4sched { get; set; }

			// Token: 0x170001B0 RID: 432
			// (get) Token: 0x060006B5 RID: 1717 RVA: 0x00025E26 File Offset: 0x00024026
			// (set) Token: 0x060006B6 RID: 1718 RVA: 0x00025E2E File Offset: 0x0002402E
			public int P_state { get; set; }

			// Token: 0x170001B1 RID: 433
			// (get) Token: 0x060006B7 RID: 1719 RVA: 0x00025E37 File Offset: 0x00024037
			// (set) Token: 0x060006B8 RID: 1720 RVA: 0x00025E3F File Offset: 0x0002403F
			public long Instruction_e { get; set; }

			// Token: 0x170001B2 RID: 434
			// (get) Token: 0x060006B9 RID: 1721 RVA: 0x00025E48 File Offset: 0x00024048
			// (set) Token: 0x060006BA RID: 1722 RVA: 0x00025E50 File Offset: 0x00024050
			public long Cycle_e { get; set; }

			// Token: 0x170001B3 RID: 435
			// (get) Token: 0x060006BB RID: 1723 RVA: 0x00025E59 File Offset: 0x00024059
			// (set) Token: 0x060006BC RID: 1724 RVA: 0x00025E61 File Offset: 0x00024061
			public long Instruction_l { get; set; }

			// Token: 0x170001B4 RID: 436
			// (get) Token: 0x060006BD RID: 1725 RVA: 0x00025E6A File Offset: 0x0002406A
			// (set) Token: 0x060006BE RID: 1726 RVA: 0x00025E72 File Offset: 0x00024072
			public long Cycle_l { get; set; }

			// Token: 0x170001B5 RID: 437
			// (get) Token: 0x060006BF RID: 1727 RVA: 0x00025E7B File Offset: 0x0002407B
			// (set) Token: 0x060006C0 RID: 1728 RVA: 0x00025E83 File Offset: 0x00024083
			public long Instruction { get; set; }

			// Token: 0x170001B6 RID: 438
			// (get) Token: 0x060006C1 RID: 1729 RVA: 0x00025E8C File Offset: 0x0002408C
			// (set) Token: 0x060006C2 RID: 1730 RVA: 0x00025E94 File Offset: 0x00024094
			public long Cycle { get; set; }

			// Token: 0x170001B7 RID: 439
			// (get) Token: 0x060006C3 RID: 1731 RVA: 0x00025E9D File Offset: 0x0002409D
			// (set) Token: 0x060006C4 RID: 1732 RVA: 0x00025EA5 File Offset: 0x000240A5
			public long Ipc { get; set; }

			// Token: 0x170001B8 RID: 440
			// (get) Token: 0x060006C5 RID: 1733 RVA: 0x00025EAE File Offset: 0x000240AE
			// (set) Token: 0x060006C6 RID: 1734 RVA: 0x00025EB6 File Offset: 0x000240B6
			public long Register1 { get; set; }

			// Token: 0x170001B9 RID: 441
			// (get) Token: 0x060006C7 RID: 1735 RVA: 0x00025EBF File Offset: 0x000240BF
			// (set) Token: 0x060006C8 RID: 1736 RVA: 0x00025EC7 File Offset: 0x000240C7
			public long Register2 { get; set; }

			// Token: 0x170001BA RID: 442
			// (get) Token: 0x060006C9 RID: 1737 RVA: 0x00025ED0 File Offset: 0x000240D0
			// (set) Token: 0x060006CA RID: 1738 RVA: 0x00025ED8 File Offset: 0x000240D8
			public long Register3 { get; set; }

			// Token: 0x170001BB RID: 443
			// (get) Token: 0x060006CB RID: 1739 RVA: 0x00025EE1 File Offset: 0x000240E1
			// (set) Token: 0x060006CC RID: 1740 RVA: 0x00025EE9 File Offset: 0x000240E9
			public long Register4 { get; set; }

			// Token: 0x170001BC RID: 444
			// (get) Token: 0x060006CD RID: 1741 RVA: 0x00025EF2 File Offset: 0x000240F2
			// (set) Token: 0x060006CE RID: 1742 RVA: 0x00025EFA File Offset: 0x000240FA
			public long Register5 { get; set; }

			// Token: 0x170001BD RID: 445
			// (get) Token: 0x060006CF RID: 1743 RVA: 0x00025F03 File Offset: 0x00024103
			// (set) Token: 0x060006D0 RID: 1744 RVA: 0x00025F0B File Offset: 0x0002410B
			public long Register6 { get; set; }

			// Token: 0x170001BE RID: 446
			// (get) Token: 0x060006D1 RID: 1745 RVA: 0x00025F14 File Offset: 0x00024114
			// (set) Token: 0x060006D2 RID: 1746 RVA: 0x00025F1C File Offset: 0x0002411C
			public long Register1_e { get; set; }

			// Token: 0x170001BF RID: 447
			// (get) Token: 0x060006D3 RID: 1747 RVA: 0x00025F25 File Offset: 0x00024125
			// (set) Token: 0x060006D4 RID: 1748 RVA: 0x00025F2D File Offset: 0x0002412D
			public long Register2_e { get; set; }

			// Token: 0x170001C0 RID: 448
			// (get) Token: 0x060006D5 RID: 1749 RVA: 0x00025F36 File Offset: 0x00024136
			// (set) Token: 0x060006D6 RID: 1750 RVA: 0x00025F3E File Offset: 0x0002413E
			public long Register3_e { get; set; }

			// Token: 0x170001C1 RID: 449
			// (get) Token: 0x060006D7 RID: 1751 RVA: 0x00025F47 File Offset: 0x00024147
			// (set) Token: 0x060006D8 RID: 1752 RVA: 0x00025F4F File Offset: 0x0002414F
			public long Register4_e { get; set; }

			// Token: 0x170001C2 RID: 450
			// (get) Token: 0x060006D9 RID: 1753 RVA: 0x00025F58 File Offset: 0x00024158
			// (set) Token: 0x060006DA RID: 1754 RVA: 0x00025F60 File Offset: 0x00024160
			public long Register5_e { get; set; }

			// Token: 0x170001C3 RID: 451
			// (get) Token: 0x060006DB RID: 1755 RVA: 0x00025F69 File Offset: 0x00024169
			// (set) Token: 0x060006DC RID: 1756 RVA: 0x00025F71 File Offset: 0x00024171
			public long Register6_e { get; set; }

			// Token: 0x170001C4 RID: 452
			// (get) Token: 0x060006DD RID: 1757 RVA: 0x00025F7A File Offset: 0x0002417A
			// (set) Token: 0x060006DE RID: 1758 RVA: 0x00025F82 File Offset: 0x00024182
			public long Register1_l { get; set; }

			// Token: 0x170001C5 RID: 453
			// (get) Token: 0x060006DF RID: 1759 RVA: 0x00025F8B File Offset: 0x0002418B
			// (set) Token: 0x060006E0 RID: 1760 RVA: 0x00025F93 File Offset: 0x00024193
			public long Register2_l { get; set; }

			// Token: 0x170001C6 RID: 454
			// (get) Token: 0x060006E1 RID: 1761 RVA: 0x00025F9C File Offset: 0x0002419C
			// (set) Token: 0x060006E2 RID: 1762 RVA: 0x00025FA4 File Offset: 0x000241A4
			public long Register3_l { get; set; }

			// Token: 0x170001C7 RID: 455
			// (get) Token: 0x060006E3 RID: 1763 RVA: 0x00025FAD File Offset: 0x000241AD
			// (set) Token: 0x060006E4 RID: 1764 RVA: 0x00025FB5 File Offset: 0x000241B5
			public long Register4_l { get; set; }

			// Token: 0x170001C8 RID: 456
			// (get) Token: 0x060006E5 RID: 1765 RVA: 0x00025FBE File Offset: 0x000241BE
			// (set) Token: 0x060006E6 RID: 1766 RVA: 0x00025FC6 File Offset: 0x000241C6
			public long Register5_l { get; set; }

			// Token: 0x170001C9 RID: 457
			// (get) Token: 0x060006E7 RID: 1767 RVA: 0x00025FCF File Offset: 0x000241CF
			// (set) Token: 0x060006E8 RID: 1768 RVA: 0x00025FD7 File Offset: 0x000241D7
			public long Register6_l { get; set; }

			// Token: 0x170001CA RID: 458
			// (get) Token: 0x060006E9 RID: 1769 RVA: 0x00025FE0 File Offset: 0x000241E0
			// (set) Token: 0x060006EA RID: 1770 RVA: 0x00025FE8 File Offset: 0x000241E8
			public Service1.ThreadExecutionRegistry threadexecinfo { get; set; }

			// Token: 0x170001CB RID: 459
			// (get) Token: 0x060006EB RID: 1771 RVA: 0x00025FF1 File Offset: 0x000241F1
			// (set) Token: 0x060006EC RID: 1772 RVA: 0x00025FF9 File Offset: 0x000241F9
			public long Accthreadcount { get; set; }

			// Token: 0x170001CC RID: 460
			// (get) Token: 0x060006ED RID: 1773 RVA: 0x00026002 File Offset: 0x00024202
			// (set) Token: 0x060006EE RID: 1774 RVA: 0x0002600A File Offset: 0x0002420A
			public long Avgthreadcount { get; set; }

			// Token: 0x170001CD RID: 461
			// (get) Token: 0x060006EF RID: 1775 RVA: 0x00026013 File Offset: 0x00024213
			// (set) Token: 0x060006F0 RID: 1776 RVA: 0x0002601B File Offset: 0x0002421B
			public long Avgthreadtime { get; set; }

			// Token: 0x170001CE RID: 462
			// (get) Token: 0x060006F1 RID: 1777 RVA: 0x00026024 File Offset: 0x00024224
			// (set) Token: 0x060006F2 RID: 1778 RVA: 0x0002602C File Offset: 0x0002422C
			public Service1.Node2 ThreadSet { get; set; }

			// Token: 0x170001CF RID: 463
			// (get) Token: 0x060006F3 RID: 1779 RVA: 0x00026035 File Offset: 0x00024235
			// (set) Token: 0x060006F4 RID: 1780 RVA: 0x0002603D File Offset: 0x0002423D
			public Service1.ThreadInfoSimp Threadinfosimp { get; set; }

			// Token: 0x060006F5 RID: 1781 RVA: 0x00026046 File Offset: 0x00024246
			public float CalcRatio1(long data1, long data2, float source)
			{
				if (data2 > 0L)
				{
					return (float)data1 / (float)data2;
				}
				return source;
			}

			// Token: 0x04000664 RID: 1636
			public Dictionary<int, long> threadContrib = new Dictionary<int, long>();
		}

		// Token: 0x0200008A RID: 138
		public class CoreQueue
		{
			// Token: 0x060006F6 RID: 1782 RVA: 0x00026054 File Offset: 0x00024254
			public CoreQueue()
			{
			}

			// Token: 0x060006F7 RID: 1783 RVA: 0x0002605C File Offset: 0x0002425C
			public CoreQueue(int cid, long datetime, long intval, long runtime, long waittime, long duration)
			{
				this.Cid = cid;
				this.DateTime = datetime;
				this.IntVal = intval;
				this.RunTime = runtime;
				this.WaitTime = waittime;
				this.Duration = duration;
				this.Next = null;
			}

			// Token: 0x170001D0 RID: 464
			// (get) Token: 0x060006F8 RID: 1784 RVA: 0x00026098 File Offset: 0x00024298
			// (set) Token: 0x060006F9 RID: 1785 RVA: 0x000260A0 File Offset: 0x000242A0
			public int Cid { get; set; }

			// Token: 0x170001D1 RID: 465
			// (get) Token: 0x060006FA RID: 1786 RVA: 0x000260A9 File Offset: 0x000242A9
			// (set) Token: 0x060006FB RID: 1787 RVA: 0x000260B1 File Offset: 0x000242B1
			public long DateTime { get; set; }

			// Token: 0x170001D2 RID: 466
			// (get) Token: 0x060006FC RID: 1788 RVA: 0x000260BA File Offset: 0x000242BA
			// (set) Token: 0x060006FD RID: 1789 RVA: 0x000260C2 File Offset: 0x000242C2
			public long IntVal { get; set; }

			// Token: 0x170001D3 RID: 467
			// (get) Token: 0x060006FE RID: 1790 RVA: 0x000260CB File Offset: 0x000242CB
			// (set) Token: 0x060006FF RID: 1791 RVA: 0x000260D3 File Offset: 0x000242D3
			public long RunTime { get; set; }

			// Token: 0x170001D4 RID: 468
			// (get) Token: 0x06000700 RID: 1792 RVA: 0x000260DC File Offset: 0x000242DC
			// (set) Token: 0x06000701 RID: 1793 RVA: 0x000260E4 File Offset: 0x000242E4
			public long WaitTime { get; set; }

			// Token: 0x170001D5 RID: 469
			// (get) Token: 0x06000702 RID: 1794 RVA: 0x000260ED File Offset: 0x000242ED
			// (set) Token: 0x06000703 RID: 1795 RVA: 0x000260F5 File Offset: 0x000242F5
			public long Duration { get; set; }

			// Token: 0x170001D6 RID: 470
			// (get) Token: 0x06000704 RID: 1796 RVA: 0x000260FE File Offset: 0x000242FE
			// (set) Token: 0x06000705 RID: 1797 RVA: 0x00026106 File Offset: 0x00024306
			public Service1.Node2 Next { get; set; }
		}

		// Token: 0x0200008B RID: 139
		public class Node2
		{
			// Token: 0x06000706 RID: 1798 RVA: 0x0002610F File Offset: 0x0002430F
			public Node2()
			{
			}

			// Token: 0x06000707 RID: 1799 RVA: 0x00026117 File Offset: 0x00024317
			public Node2(int id, long value1, int value2)
			{
				this.Id = id;
				this.Value1 = value1;
				this.Value2 = value2;
				this.Next = null;
			}

			// Token: 0x170001D7 RID: 471
			// (get) Token: 0x06000708 RID: 1800 RVA: 0x0002613B File Offset: 0x0002433B
			// (set) Token: 0x06000709 RID: 1801 RVA: 0x00026143 File Offset: 0x00024343
			public int Id { get; set; }

			// Token: 0x170001D8 RID: 472
			// (get) Token: 0x0600070A RID: 1802 RVA: 0x0002614C File Offset: 0x0002434C
			// (set) Token: 0x0600070B RID: 1803 RVA: 0x00026154 File Offset: 0x00024354
			public long Value1 { get; set; }

			// Token: 0x170001D9 RID: 473
			// (get) Token: 0x0600070C RID: 1804 RVA: 0x0002615D File Offset: 0x0002435D
			// (set) Token: 0x0600070D RID: 1805 RVA: 0x00026165 File Offset: 0x00024365
			public int Value2 { get; set; }

			// Token: 0x170001DA RID: 474
			// (get) Token: 0x0600070E RID: 1806 RVA: 0x0002616E File Offset: 0x0002436E
			// (set) Token: 0x0600070F RID: 1807 RVA: 0x00026176 File Offset: 0x00024376
			public Service1.Node2 Next { get; set; }
		}

		// Token: 0x0200008C RID: 140
		public class Node
		{
			// Token: 0x06000710 RID: 1808 RVA: 0x0002617F File Offset: 0x0002437F
			public Node()
			{
			}

			// Token: 0x06000711 RID: 1809 RVA: 0x00026187 File Offset: 0x00024387
			public Node(int id, int value)
			{
				this.Id = id;
				this.Value = value;
				this.Next = null;
			}

			// Token: 0x170001DB RID: 475
			// (get) Token: 0x06000712 RID: 1810 RVA: 0x000261A4 File Offset: 0x000243A4
			// (set) Token: 0x06000713 RID: 1811 RVA: 0x000261AC File Offset: 0x000243AC
			public int Id { get; set; }

			// Token: 0x170001DC RID: 476
			// (get) Token: 0x06000714 RID: 1812 RVA: 0x000261B5 File Offset: 0x000243B5
			// (set) Token: 0x06000715 RID: 1813 RVA: 0x000261BD File Offset: 0x000243BD
			public int Value { get; set; }

			// Token: 0x170001DD RID: 477
			// (get) Token: 0x06000716 RID: 1814 RVA: 0x000261C6 File Offset: 0x000243C6
			// (set) Token: 0x06000717 RID: 1815 RVA: 0x000261CE File Offset: 0x000243CE
			public Service1.Node Next { get; set; }
		}

		// Token: 0x0200008D RID: 141
		public class Node1
		{
			// Token: 0x06000718 RID: 1816 RVA: 0x000261D7 File Offset: 0x000243D7
			public Node1()
			{
			}

			// Token: 0x06000719 RID: 1817 RVA: 0x000261E0 File Offset: 0x000243E0
			public Node1(int id, long acc_instruction_b, long acc_aclk_b, long acc_load_b, long acc_store_b, long acc_load_miss_b, long acc_br_b, long acc_runtime_b, long cnt_b, long acc_instruction_l, long acc_aclk_l, long acc_load_l, long acc_load_l_perm, long last_duration, long now_duration, long acc_store_l, long acc_store_l_perm, long acc_load_miss_l, long acc_br_l, long acc_runtime_l, long cnt_l, long ipc_b, long max_ipc_b, long ipc_l, long ipc_l_perm, long max_ipc_l, long ipc_ratio, long br_ratio, long br_load_ratio, long load_miss_ratio_b, long min_load_miss_ratio_b, long load_miss_ratio_l, long avg_runtime_b, long avg_runtime_l, long avg_freq_b, long avg_freq_l, long max_ins, long lock_data, long tag, long duration, long reset_count, uint affinity, long residence)
			{
				this.Id = id;
				this.Acc_instruction_b = acc_instruction_b;
				this.Acc_aclk_b = acc_aclk_b;
				this.Acc_load_b = acc_load_b;
				this.Acc_store_b = acc_store_b;
				this.Acc_load_miss_b = acc_load_miss_b;
				this.Acc_br_b = acc_br_b;
				this.Acc_runtime_b = acc_runtime_b;
				this.Cnt_b = cnt_b;
				this.Acc_instruction_l = acc_instruction_l;
				this.Acc_aclk_l = acc_aclk_l;
				this.Acc_load_l = acc_load_l;
				this.Acc_load_l_perm = acc_load_l_perm;
				this.Last_duration = last_duration;
				this.Now_duration = now_duration;
				this.Acc_store_l = acc_store_l;
				this.Acc_store_l_perm = acc_store_l_perm;
				this.Acc_load_miss_l = acc_load_miss_l;
				this.Acc_br_l = acc_br_l;
				this.Acc_runtime_l = acc_runtime_l;
				this.Cnt_l = cnt_l;
				this.Ipc_b = ipc_b;
				this.Max_ipc_b = max_ipc_b;
				this.Ipc_l = ipc_l;
				this.Ipc_l_perm = ipc_l_perm;
				this.Max_ipc_l = max_ipc_l;
				this.Ipc_ratio = ipc_ratio;
				this.Br_ratio = br_ratio;
				this.Br_load_ratio = br_load_ratio;
				this.Load_miss_ratio_b = load_miss_ratio_b;
				this.Min_load_miss_ratio_b = min_load_miss_ratio_b;
				this.Load_miss_ratio_l = load_miss_ratio_l;
				this.Avg_runtime_b = avg_runtime_b;
				this.Avg_runtime_l = avg_runtime_l;
				this.Avg_freq_b = avg_freq_b;
				this.Avg_freq_l = avg_freq_l;
				this.Max_ins = max_ins;
				this.Lock_data = lock_data;
				this.Tag = tag;
				this.Duration = duration;
				this.Reset_count = reset_count;
				this.Affinity = affinity;
				this.Residence = residence;
				this.Next = null;
			}

			// Token: 0x170001DE RID: 478
			// (get) Token: 0x0600071A RID: 1818 RVA: 0x0002634F File Offset: 0x0002454F
			// (set) Token: 0x0600071B RID: 1819 RVA: 0x00026357 File Offset: 0x00024557
			public int Id { get; set; }

			// Token: 0x170001DF RID: 479
			// (get) Token: 0x0600071C RID: 1820 RVA: 0x00026360 File Offset: 0x00024560
			// (set) Token: 0x0600071D RID: 1821 RVA: 0x00026368 File Offset: 0x00024568
			public long Acc_instruction_b { get; set; }

			// Token: 0x170001E0 RID: 480
			// (get) Token: 0x0600071E RID: 1822 RVA: 0x00026371 File Offset: 0x00024571
			// (set) Token: 0x0600071F RID: 1823 RVA: 0x00026379 File Offset: 0x00024579
			public long Acc_aclk_b { get; set; }

			// Token: 0x170001E1 RID: 481
			// (get) Token: 0x06000720 RID: 1824 RVA: 0x00026382 File Offset: 0x00024582
			// (set) Token: 0x06000721 RID: 1825 RVA: 0x0002638A File Offset: 0x0002458A
			public long Acc_load_b { get; set; }

			// Token: 0x170001E2 RID: 482
			// (get) Token: 0x06000722 RID: 1826 RVA: 0x00026393 File Offset: 0x00024593
			// (set) Token: 0x06000723 RID: 1827 RVA: 0x0002639B File Offset: 0x0002459B
			public long Acc_store_b { get; set; }

			// Token: 0x170001E3 RID: 483
			// (get) Token: 0x06000724 RID: 1828 RVA: 0x000263A4 File Offset: 0x000245A4
			// (set) Token: 0x06000725 RID: 1829 RVA: 0x000263AC File Offset: 0x000245AC
			public long Acc_load_miss_b { get; set; }

			// Token: 0x170001E4 RID: 484
			// (get) Token: 0x06000726 RID: 1830 RVA: 0x000263B5 File Offset: 0x000245B5
			// (set) Token: 0x06000727 RID: 1831 RVA: 0x000263BD File Offset: 0x000245BD
			public long Acc_br_b { get; set; }

			// Token: 0x170001E5 RID: 485
			// (get) Token: 0x06000728 RID: 1832 RVA: 0x000263C6 File Offset: 0x000245C6
			// (set) Token: 0x06000729 RID: 1833 RVA: 0x000263CE File Offset: 0x000245CE
			public long Acc_runtime_b { get; set; }

			// Token: 0x170001E6 RID: 486
			// (get) Token: 0x0600072A RID: 1834 RVA: 0x000263D7 File Offset: 0x000245D7
			// (set) Token: 0x0600072B RID: 1835 RVA: 0x000263DF File Offset: 0x000245DF
			public long Cnt_b { get; set; }

			// Token: 0x170001E7 RID: 487
			// (get) Token: 0x0600072C RID: 1836 RVA: 0x000263E8 File Offset: 0x000245E8
			// (set) Token: 0x0600072D RID: 1837 RVA: 0x000263F0 File Offset: 0x000245F0
			public long Acc_instruction_l { get; set; }

			// Token: 0x170001E8 RID: 488
			// (get) Token: 0x0600072E RID: 1838 RVA: 0x000263F9 File Offset: 0x000245F9
			// (set) Token: 0x0600072F RID: 1839 RVA: 0x00026401 File Offset: 0x00024601
			public long Acc_aclk_l { get; set; }

			// Token: 0x170001E9 RID: 489
			// (get) Token: 0x06000730 RID: 1840 RVA: 0x0002640A File Offset: 0x0002460A
			// (set) Token: 0x06000731 RID: 1841 RVA: 0x00026412 File Offset: 0x00024612
			public long Acc_load_l { get; set; }

			// Token: 0x170001EA RID: 490
			// (get) Token: 0x06000732 RID: 1842 RVA: 0x0002641B File Offset: 0x0002461B
			// (set) Token: 0x06000733 RID: 1843 RVA: 0x00026423 File Offset: 0x00024623
			public long Acc_load_l_perm { get; set; }

			// Token: 0x170001EB RID: 491
			// (get) Token: 0x06000734 RID: 1844 RVA: 0x0002642C File Offset: 0x0002462C
			// (set) Token: 0x06000735 RID: 1845 RVA: 0x00026434 File Offset: 0x00024634
			public long Last_duration { get; set; }

			// Token: 0x170001EC RID: 492
			// (get) Token: 0x06000736 RID: 1846 RVA: 0x0002643D File Offset: 0x0002463D
			// (set) Token: 0x06000737 RID: 1847 RVA: 0x00026445 File Offset: 0x00024645
			public long Now_duration { get; set; }

			// Token: 0x170001ED RID: 493
			// (get) Token: 0x06000738 RID: 1848 RVA: 0x0002644E File Offset: 0x0002464E
			// (set) Token: 0x06000739 RID: 1849 RVA: 0x00026456 File Offset: 0x00024656
			public long Acc_store_l { get; set; }

			// Token: 0x170001EE RID: 494
			// (get) Token: 0x0600073A RID: 1850 RVA: 0x0002645F File Offset: 0x0002465F
			// (set) Token: 0x0600073B RID: 1851 RVA: 0x00026467 File Offset: 0x00024667
			public long Acc_store_l_perm { get; set; }

			// Token: 0x170001EF RID: 495
			// (get) Token: 0x0600073C RID: 1852 RVA: 0x00026470 File Offset: 0x00024670
			// (set) Token: 0x0600073D RID: 1853 RVA: 0x00026478 File Offset: 0x00024678
			public long Acc_load_miss_l { get; set; }

			// Token: 0x170001F0 RID: 496
			// (get) Token: 0x0600073E RID: 1854 RVA: 0x00026481 File Offset: 0x00024681
			// (set) Token: 0x0600073F RID: 1855 RVA: 0x00026489 File Offset: 0x00024689
			public long Acc_br_l { get; set; }

			// Token: 0x170001F1 RID: 497
			// (get) Token: 0x06000740 RID: 1856 RVA: 0x00026492 File Offset: 0x00024692
			// (set) Token: 0x06000741 RID: 1857 RVA: 0x0002649A File Offset: 0x0002469A
			public long Acc_runtime_l { get; set; }

			// Token: 0x170001F2 RID: 498
			// (get) Token: 0x06000742 RID: 1858 RVA: 0x000264A3 File Offset: 0x000246A3
			// (set) Token: 0x06000743 RID: 1859 RVA: 0x000264AB File Offset: 0x000246AB
			public long Cnt_l { get; set; }

			// Token: 0x170001F3 RID: 499
			// (get) Token: 0x06000744 RID: 1860 RVA: 0x000264B4 File Offset: 0x000246B4
			// (set) Token: 0x06000745 RID: 1861 RVA: 0x000264BC File Offset: 0x000246BC
			public long Ipc_b { get; set; }

			// Token: 0x170001F4 RID: 500
			// (get) Token: 0x06000746 RID: 1862 RVA: 0x000264C5 File Offset: 0x000246C5
			// (set) Token: 0x06000747 RID: 1863 RVA: 0x000264CD File Offset: 0x000246CD
			public long Max_ipc_b { get; set; }

			// Token: 0x170001F5 RID: 501
			// (get) Token: 0x06000748 RID: 1864 RVA: 0x000264D6 File Offset: 0x000246D6
			// (set) Token: 0x06000749 RID: 1865 RVA: 0x000264DE File Offset: 0x000246DE
			public long Ipc_l { get; set; }

			// Token: 0x170001F6 RID: 502
			// (get) Token: 0x0600074A RID: 1866 RVA: 0x000264E7 File Offset: 0x000246E7
			// (set) Token: 0x0600074B RID: 1867 RVA: 0x000264EF File Offset: 0x000246EF
			public long Ipc_l_perm { get; set; }

			// Token: 0x170001F7 RID: 503
			// (get) Token: 0x0600074C RID: 1868 RVA: 0x000264F8 File Offset: 0x000246F8
			// (set) Token: 0x0600074D RID: 1869 RVA: 0x00026500 File Offset: 0x00024700
			public long Max_ipc_l { get; set; }

			// Token: 0x170001F8 RID: 504
			// (get) Token: 0x0600074E RID: 1870 RVA: 0x00026509 File Offset: 0x00024709
			// (set) Token: 0x0600074F RID: 1871 RVA: 0x00026511 File Offset: 0x00024711
			public long Ipc_ratio { get; set; }

			// Token: 0x170001F9 RID: 505
			// (get) Token: 0x06000750 RID: 1872 RVA: 0x0002651A File Offset: 0x0002471A
			// (set) Token: 0x06000751 RID: 1873 RVA: 0x00026522 File Offset: 0x00024722
			public long Br_ratio { get; set; }

			// Token: 0x170001FA RID: 506
			// (get) Token: 0x06000752 RID: 1874 RVA: 0x0002652B File Offset: 0x0002472B
			// (set) Token: 0x06000753 RID: 1875 RVA: 0x00026533 File Offset: 0x00024733
			public long Br_load_ratio { get; set; }

			// Token: 0x170001FB RID: 507
			// (get) Token: 0x06000754 RID: 1876 RVA: 0x0002653C File Offset: 0x0002473C
			// (set) Token: 0x06000755 RID: 1877 RVA: 0x00026544 File Offset: 0x00024744
			public long Load_miss_ratio_b { get; set; }

			// Token: 0x170001FC RID: 508
			// (get) Token: 0x06000756 RID: 1878 RVA: 0x0002654D File Offset: 0x0002474D
			// (set) Token: 0x06000757 RID: 1879 RVA: 0x00026555 File Offset: 0x00024755
			public long Min_load_miss_ratio_b { get; set; }

			// Token: 0x170001FD RID: 509
			// (get) Token: 0x06000758 RID: 1880 RVA: 0x0002655E File Offset: 0x0002475E
			// (set) Token: 0x06000759 RID: 1881 RVA: 0x00026566 File Offset: 0x00024766
			public long Load_miss_ratio_l { get; set; }

			// Token: 0x170001FE RID: 510
			// (get) Token: 0x0600075A RID: 1882 RVA: 0x0002656F File Offset: 0x0002476F
			// (set) Token: 0x0600075B RID: 1883 RVA: 0x00026577 File Offset: 0x00024777
			public long Avg_runtime_b { get; set; }

			// Token: 0x170001FF RID: 511
			// (get) Token: 0x0600075C RID: 1884 RVA: 0x00026580 File Offset: 0x00024780
			// (set) Token: 0x0600075D RID: 1885 RVA: 0x00026588 File Offset: 0x00024788
			public long Avg_runtime_l { get; set; }

			// Token: 0x17000200 RID: 512
			// (get) Token: 0x0600075E RID: 1886 RVA: 0x00026591 File Offset: 0x00024791
			// (set) Token: 0x0600075F RID: 1887 RVA: 0x00026599 File Offset: 0x00024799
			public long Avg_freq_b { get; set; }

			// Token: 0x17000201 RID: 513
			// (get) Token: 0x06000760 RID: 1888 RVA: 0x000265A2 File Offset: 0x000247A2
			// (set) Token: 0x06000761 RID: 1889 RVA: 0x000265AA File Offset: 0x000247AA
			public long Avg_freq_l { get; set; }

			// Token: 0x17000202 RID: 514
			// (get) Token: 0x06000762 RID: 1890 RVA: 0x000265B3 File Offset: 0x000247B3
			// (set) Token: 0x06000763 RID: 1891 RVA: 0x000265BB File Offset: 0x000247BB
			public long Max_ins { get; set; }

			// Token: 0x17000203 RID: 515
			// (get) Token: 0x06000764 RID: 1892 RVA: 0x000265C4 File Offset: 0x000247C4
			// (set) Token: 0x06000765 RID: 1893 RVA: 0x000265CC File Offset: 0x000247CC
			public long Lock_data { get; set; }

			// Token: 0x17000204 RID: 516
			// (get) Token: 0x06000766 RID: 1894 RVA: 0x000265D5 File Offset: 0x000247D5
			// (set) Token: 0x06000767 RID: 1895 RVA: 0x000265DD File Offset: 0x000247DD
			public long Tag { get; set; }

			// Token: 0x17000205 RID: 517
			// (get) Token: 0x06000768 RID: 1896 RVA: 0x000265E6 File Offset: 0x000247E6
			// (set) Token: 0x06000769 RID: 1897 RVA: 0x000265EE File Offset: 0x000247EE
			public long Duration { get; set; }

			// Token: 0x17000206 RID: 518
			// (get) Token: 0x0600076A RID: 1898 RVA: 0x000265F7 File Offset: 0x000247F7
			// (set) Token: 0x0600076B RID: 1899 RVA: 0x000265FF File Offset: 0x000247FF
			public long Reset_count { get; set; }

			// Token: 0x17000207 RID: 519
			// (get) Token: 0x0600076C RID: 1900 RVA: 0x00026608 File Offset: 0x00024808
			// (set) Token: 0x0600076D RID: 1901 RVA: 0x00026610 File Offset: 0x00024810
			public uint Affinity { get; set; }

			// Token: 0x17000208 RID: 520
			// (get) Token: 0x0600076E RID: 1902 RVA: 0x00026619 File Offset: 0x00024819
			// (set) Token: 0x0600076F RID: 1903 RVA: 0x00026621 File Offset: 0x00024821
			public long Residence { get; set; }

			// Token: 0x17000209 RID: 521
			// (get) Token: 0x06000770 RID: 1904 RVA: 0x0002662A File Offset: 0x0002482A
			// (set) Token: 0x06000771 RID: 1905 RVA: 0x00026632 File Offset: 0x00024832
			public Service1.Node1 Next { get; set; }
		}

		// Token: 0x0200008E RID: 142
		public class NodeT
		{
			// Token: 0x06000772 RID: 1906 RVA: 0x0002663B File Offset: 0x0002483B
			public NodeT()
			{
			}

			// Token: 0x06000773 RID: 1907 RVA: 0x00026643 File Offset: 0x00024843
			public NodeT(int pid, long data, Service1.NodeT next)
			{
				this.PId = pid;
				this.Data = data;
				this.Next = null;
			}

			// Token: 0x1700020A RID: 522
			// (get) Token: 0x06000774 RID: 1908 RVA: 0x00026660 File Offset: 0x00024860
			// (set) Token: 0x06000775 RID: 1909 RVA: 0x00026668 File Offset: 0x00024868
			public int PId { get; set; }

			// Token: 0x1700020B RID: 523
			// (get) Token: 0x06000776 RID: 1910 RVA: 0x00026671 File Offset: 0x00024871
			// (set) Token: 0x06000777 RID: 1911 RVA: 0x00026679 File Offset: 0x00024879
			public long Data { get; set; }

			// Token: 0x1700020C RID: 524
			// (get) Token: 0x06000778 RID: 1912 RVA: 0x00026682 File Offset: 0x00024882
			// (set) Token: 0x06000779 RID: 1913 RVA: 0x0002668A File Offset: 0x0002488A
			public Service1.NodeT Next { get; set; }
		}

		// Token: 0x0200008F RID: 143
		public class NodeP
		{
			// Token: 0x0600077A RID: 1914 RVA: 0x00026693 File Offset: 0x00024893
			public NodeP()
			{
			}

			// Token: 0x0600077B RID: 1915 RVA: 0x0002669C File Offset: 0x0002489C
			public NodeP(int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1, Service1.Node2 compare, Service1.Node2 compare_final)
			{
				this.PId = pid;
				this.Ins_total = ins_total;
				this.Store_total = store_total;
				this.Count_total = count_total;
				this.Intval = intval;
				this.Nonstore_store_ratio = nonstore_store_ratio;
				this.Usr_sum = usr_sum;
				this.Usr_count = usr_count;
				this.Usr_ratio = usr_ratio;
				this.Residence = residence;
				this.Residence1 = residence1;
				this.Compare = compare;
				this.Compare_final = compare_final;
				this.Next = null;
			}

			// Token: 0x1700020D RID: 525
			// (get) Token: 0x0600077C RID: 1916 RVA: 0x0002671B File Offset: 0x0002491B
			// (set) Token: 0x0600077D RID: 1917 RVA: 0x00026723 File Offset: 0x00024923
			public int PId { get; set; }

			// Token: 0x1700020E RID: 526
			// (get) Token: 0x0600077E RID: 1918 RVA: 0x0002672C File Offset: 0x0002492C
			// (set) Token: 0x0600077F RID: 1919 RVA: 0x00026734 File Offset: 0x00024934
			public long Ins_total { get; set; }

			// Token: 0x1700020F RID: 527
			// (get) Token: 0x06000780 RID: 1920 RVA: 0x0002673D File Offset: 0x0002493D
			// (set) Token: 0x06000781 RID: 1921 RVA: 0x00026745 File Offset: 0x00024945
			public long Store_total { get; set; }

			// Token: 0x17000210 RID: 528
			// (get) Token: 0x06000782 RID: 1922 RVA: 0x0002674E File Offset: 0x0002494E
			// (set) Token: 0x06000783 RID: 1923 RVA: 0x00026756 File Offset: 0x00024956
			public long Count_total { get; set; }

			// Token: 0x17000211 RID: 529
			// (get) Token: 0x06000784 RID: 1924 RVA: 0x0002675F File Offset: 0x0002495F
			// (set) Token: 0x06000785 RID: 1925 RVA: 0x00026767 File Offset: 0x00024967
			public long Intval { get; set; }

			// Token: 0x17000212 RID: 530
			// (get) Token: 0x06000786 RID: 1926 RVA: 0x00026770 File Offset: 0x00024970
			// (set) Token: 0x06000787 RID: 1927 RVA: 0x00026778 File Offset: 0x00024978
			public long Nonstore_store_ratio { get; set; }

			// Token: 0x17000213 RID: 531
			// (get) Token: 0x06000788 RID: 1928 RVA: 0x00026781 File Offset: 0x00024981
			// (set) Token: 0x06000789 RID: 1929 RVA: 0x00026789 File Offset: 0x00024989
			public long Usr_sum { get; set; }

			// Token: 0x17000214 RID: 532
			// (get) Token: 0x0600078A RID: 1930 RVA: 0x00026792 File Offset: 0x00024992
			// (set) Token: 0x0600078B RID: 1931 RVA: 0x0002679A File Offset: 0x0002499A
			public long Usr_count { get; set; }

			// Token: 0x17000215 RID: 533
			// (get) Token: 0x0600078C RID: 1932 RVA: 0x000267A3 File Offset: 0x000249A3
			// (set) Token: 0x0600078D RID: 1933 RVA: 0x000267AB File Offset: 0x000249AB
			public long Usr_ratio { get; set; }

			// Token: 0x17000216 RID: 534
			// (get) Token: 0x0600078E RID: 1934 RVA: 0x000267B4 File Offset: 0x000249B4
			// (set) Token: 0x0600078F RID: 1935 RVA: 0x000267BC File Offset: 0x000249BC
			public long Residence { get; set; }

			// Token: 0x17000217 RID: 535
			// (get) Token: 0x06000790 RID: 1936 RVA: 0x000267C5 File Offset: 0x000249C5
			// (set) Token: 0x06000791 RID: 1937 RVA: 0x000267CD File Offset: 0x000249CD
			public long Residence1 { get; set; }

			// Token: 0x17000218 RID: 536
			// (get) Token: 0x06000792 RID: 1938 RVA: 0x000267D6 File Offset: 0x000249D6
			// (set) Token: 0x06000793 RID: 1939 RVA: 0x000267DE File Offset: 0x000249DE
			public Service1.Node2 Compare { get; set; }

			// Token: 0x17000219 RID: 537
			// (get) Token: 0x06000794 RID: 1940 RVA: 0x000267E7 File Offset: 0x000249E7
			// (set) Token: 0x06000795 RID: 1941 RVA: 0x000267EF File Offset: 0x000249EF
			public Service1.Node2 Compare_final { get; set; }

			// Token: 0x1700021A RID: 538
			// (get) Token: 0x06000796 RID: 1942 RVA: 0x000267F8 File Offset: 0x000249F8
			// (set) Token: 0x06000797 RID: 1943 RVA: 0x00026800 File Offset: 0x00024A00
			public Service1.NodeP Next { get; set; }
		}

		// Token: 0x02000090 RID: 144
		public class ThreadLoadManager4b
		{
			// Token: 0x06000798 RID: 1944 RVA: 0x0002680C File Offset: 0x00024A0C
			public void AddOrUpdate(int threadId, long load)
			{
				Service1.ThreadLoadManager4b.ThreadLoadNode threadLoadNode;
				if (this._dict.TryGetValue(threadId, out threadLoadNode))
				{
					this._sortedSet.Remove(threadLoadNode);
					Service1.ThreadLoadManager4b.ThreadLoadNode threadLoadNode2 = new Service1.ThreadLoadManager4b.ThreadLoadNode(threadId, load);
					this._dict[threadId] = threadLoadNode2;
					this._sortedSet.Add(threadLoadNode2);
					return;
				}
				Service1.ThreadLoadManager4b.ThreadLoadNode threadLoadNode3 = new Service1.ThreadLoadManager4b.ThreadLoadNode(threadId, load);
				this._dict.Add(threadId, threadLoadNode3);
				this._sortedSet.Add(threadLoadNode3);
			}

			// Token: 0x06000799 RID: 1945 RVA: 0x0002687B File Offset: 0x00024A7B
			[return: TupleElementNames(new string[] { "threadId", "load" })]
			public List<ValueTuple<int, long>> TakeTopN(int n)
			{
				return (from node in this._sortedSet.Take(n)
					select new ValueTuple<int, long>(node.ThreadId, node.Load)).ToList<ValueTuple<int, long>>();
			}

			// Token: 0x0600079A RID: 1946 RVA: 0x000268B2 File Offset: 0x00024AB2
			[return: TupleElementNames(new string[] { "threadId", "load" })]
			public List<ValueTuple<int, long>> TakeBottomN(int n)
			{
				return (from node in this._sortedSet.Reverse().Take(n)
					select new ValueTuple<int, long>(node.ThreadId, node.Load)).ToList<ValueTuple<int, long>>();
			}

			// Token: 0x0600079B RID: 1947 RVA: 0x000268F0 File Offset: 0x00024AF0
			public int IsInTopPos(Service1.ThreadLoadManager4b.ThreadLoadNode node, int pos)
			{
				if (node == null)
				{
					return 0;
				}
				Service1.ThreadLoadManager4b.ThreadLoadNode threadLoadNode;
				if (!this._dict.TryGetValue(node.ThreadId, out threadLoadNode) || threadLoadNode.Load != node.Load)
				{
					return 0;
				}
				int num = 0;
				foreach (Service1.ThreadLoadManager4b.ThreadLoadNode threadLoadNode2 in this._sortedSet)
				{
					if (threadLoadNode2.ThreadId == node.ThreadId && threadLoadNode2.Load == node.Load)
					{
						return (num < pos) ? 1 : 0;
					}
					num++;
					if (num >= pos)
					{
						break;
					}
				}
				return 0;
			}

			// Token: 0x0600079C RID: 1948 RVA: 0x0002699C File Offset: 0x00024B9C
			public int GetNodePosition(int threadId)
			{
				if (!this._dict.ContainsKey(threadId))
				{
					return -1;
				}
				int num = 0;
				using (SortedSet<Service1.ThreadLoadManager4b.ThreadLoadNode>.Enumerator enumerator = this._sortedSet.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.ThreadId == threadId)
						{
							return num;
						}
						num++;
					}
				}
				return -1;
			}

			// Token: 0x0600079D RID: 1949 RVA: 0x00026A0C File Offset: 0x00024C0C
			public void Clear()
			{
				this._dict.Clear();
				this._sortedSet.Clear();
			}

			// Token: 0x1700021B RID: 539
			// (get) Token: 0x0600079E RID: 1950 RVA: 0x00026A24 File Offset: 0x00024C24
			public int Count
			{
				get
				{
					return this._dict.Count;
				}
			}

			// Token: 0x0600079F RID: 1951 RVA: 0x00026A31 File Offset: 0x00024C31
			public List<Service1.ThreadLoadManager4b.ThreadLoadNode> GetAllNodes()
			{
				return this._sortedSet.ToList<Service1.ThreadLoadManager4b.ThreadLoadNode>();
			}

			// Token: 0x04000706 RID: 1798
			private readonly Dictionary<int, Service1.ThreadLoadManager4b.ThreadLoadNode> _dict = new Dictionary<int, Service1.ThreadLoadManager4b.ThreadLoadNode>();

			// Token: 0x04000707 RID: 1799
			private readonly SortedSet<Service1.ThreadLoadManager4b.ThreadLoadNode> _sortedSet = new SortedSet<Service1.ThreadLoadManager4b.ThreadLoadNode>(new Service1.ThreadLoadManager4b.NodeComparer());

			// Token: 0x020000B2 RID: 178
			public class ThreadLoadNode
			{
				// Token: 0x17000245 RID: 581
				// (get) Token: 0x06000852 RID: 2130 RVA: 0x0002746F File Offset: 0x0002566F
				public int ThreadId { get; }

				// Token: 0x17000246 RID: 582
				// (get) Token: 0x06000853 RID: 2131 RVA: 0x00027477 File Offset: 0x00025677
				public long Load { get; }

				// Token: 0x06000854 RID: 2132 RVA: 0x0002747F File Offset: 0x0002567F
				public ThreadLoadNode(int threadId, long load)
				{
					this.ThreadId = threadId;
					this.Load = load;
				}
			}

			// Token: 0x020000B3 RID: 179
			private class NodeComparer : IComparer<Service1.ThreadLoadManager4b.ThreadLoadNode>
			{
				// Token: 0x06000855 RID: 2133 RVA: 0x00027498 File Offset: 0x00025698
				public int Compare(Service1.ThreadLoadManager4b.ThreadLoadNode x, Service1.ThreadLoadManager4b.ThreadLoadNode y)
				{
					int num = y.Load.CompareTo(x.Load);
					if (num != 0)
					{
						return num;
					}
					return x.ThreadId.CompareTo(y.ThreadId);
				}
			}
		}

		// Token: 0x02000091 RID: 145
		public class ThreadLoadManager4l
		{
			// Token: 0x060007A1 RID: 1953 RVA: 0x00026A64 File Offset: 0x00024C64
			public void AddOrUpdate(int threadId, long load)
			{
				Service1.ThreadLoadManager4l.ThreadLoadNode threadLoadNode;
				if (this._dict.TryGetValue(threadId, out threadLoadNode))
				{
					this._sortedSet.Remove(threadLoadNode);
					Service1.ThreadLoadManager4l.ThreadLoadNode threadLoadNode2 = new Service1.ThreadLoadManager4l.ThreadLoadNode(threadId, load);
					this._dict[threadId] = threadLoadNode2;
					this._sortedSet.Add(threadLoadNode2);
					return;
				}
				Service1.ThreadLoadManager4l.ThreadLoadNode threadLoadNode3 = new Service1.ThreadLoadManager4l.ThreadLoadNode(threadId, load);
				this._dict.Add(threadId, threadLoadNode3);
				this._sortedSet.Add(threadLoadNode3);
			}

			// Token: 0x060007A2 RID: 1954 RVA: 0x00026AD3 File Offset: 0x00024CD3
			[return: TupleElementNames(new string[] { "threadId", "load" })]
			public List<ValueTuple<int, long>> TakeTopN(int n)
			{
				return (from node in this._sortedSet.Take(n)
					select new ValueTuple<int, long>(node.ThreadId, node.Load)).ToList<ValueTuple<int, long>>();
			}

			// Token: 0x060007A3 RID: 1955 RVA: 0x00026B0A File Offset: 0x00024D0A
			[return: TupleElementNames(new string[] { "threadId", "load" })]
			public List<ValueTuple<int, long>> TakeBottomN(int n)
			{
				return (from node in this._sortedSet.Reverse().Take(n)
					select new ValueTuple<int, long>(node.ThreadId, node.Load)).ToList<ValueTuple<int, long>>();
			}

			// Token: 0x060007A4 RID: 1956 RVA: 0x00026B48 File Offset: 0x00024D48
			public int IsInTopPos(Service1.ThreadLoadManager4l.ThreadLoadNode node, int pos)
			{
				if (node == null)
				{
					return 0;
				}
				Service1.ThreadLoadManager4l.ThreadLoadNode threadLoadNode;
				if (!this._dict.TryGetValue(node.ThreadId, out threadLoadNode) || threadLoadNode.Load != node.Load)
				{
					return 0;
				}
				int num = 0;
				foreach (Service1.ThreadLoadManager4l.ThreadLoadNode threadLoadNode2 in this._sortedSet)
				{
					if (threadLoadNode2.ThreadId == node.ThreadId && threadLoadNode2.Load == node.Load)
					{
						return (num < pos) ? 1 : 0;
					}
					num++;
					if (num >= pos)
					{
						break;
					}
				}
				return 0;
			}

			// Token: 0x060007A5 RID: 1957 RVA: 0x00026BF4 File Offset: 0x00024DF4
			public int GetNodePosition(int threadId)
			{
				if (!this._dict.ContainsKey(threadId))
				{
					return -1;
				}
				int num = 0;
				using (SortedSet<Service1.ThreadLoadManager4l.ThreadLoadNode>.Enumerator enumerator = this._sortedSet.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.ThreadId == threadId)
						{
							return num;
						}
						num++;
					}
				}
				return -1;
			}

			// Token: 0x060007A6 RID: 1958 RVA: 0x00026C64 File Offset: 0x00024E64
			public void Clear()
			{
				this._dict.Clear();
				this._sortedSet.Clear();
			}

			// Token: 0x1700021C RID: 540
			// (get) Token: 0x060007A7 RID: 1959 RVA: 0x00026C7C File Offset: 0x00024E7C
			public int Count
			{
				get
				{
					return this._dict.Count;
				}
			}

			// Token: 0x060007A8 RID: 1960 RVA: 0x00026C89 File Offset: 0x00024E89
			public List<Service1.ThreadLoadManager4l.ThreadLoadNode> GetAllNodes()
			{
				return this._sortedSet.ToList<Service1.ThreadLoadManager4l.ThreadLoadNode>();
			}

			// Token: 0x060007A9 RID: 1961 RVA: 0x00026C96 File Offset: 0x00024E96
			public int GetThreadIdByIndex(int index)
			{
				if (index < 0 || index >= this._sortedSet.Count)
				{
					return -1;
				}
				return this._sortedSet.ElementAt(index).ThreadId;
			}

			// Token: 0x060007AA RID: 1962 RVA: 0x00026CBD File Offset: 0x00024EBD
			public long GetLoadByIndex(int index)
			{
				if (index < 0 || index >= this._sortedSet.Count)
				{
					return -1L;
				}
				return this._sortedSet.ElementAt(index).Load;
			}

			// Token: 0x04000708 RID: 1800
			private readonly Dictionary<int, Service1.ThreadLoadManager4l.ThreadLoadNode> _dict = new Dictionary<int, Service1.ThreadLoadManager4l.ThreadLoadNode>();

			// Token: 0x04000709 RID: 1801
			private readonly SortedSet<Service1.ThreadLoadManager4l.ThreadLoadNode> _sortedSet = new SortedSet<Service1.ThreadLoadManager4l.ThreadLoadNode>(new Service1.ThreadLoadManager4l.NodeComparer());

			// Token: 0x020000B5 RID: 181
			public class ThreadLoadNode
			{
				// Token: 0x17000247 RID: 583
				// (get) Token: 0x0600085B RID: 2139 RVA: 0x00027515 File Offset: 0x00025715
				public int ThreadId { get; }

				// Token: 0x17000248 RID: 584
				// (get) Token: 0x0600085C RID: 2140 RVA: 0x0002751D File Offset: 0x0002571D
				public long Load { get; }

				// Token: 0x0600085D RID: 2141 RVA: 0x00027525 File Offset: 0x00025725
				public ThreadLoadNode(int threadId, long load)
				{
					this.ThreadId = threadId;
					this.Load = load;
				}
			}

			// Token: 0x020000B6 RID: 182
			private class NodeComparer : IComparer<Service1.ThreadLoadManager4l.ThreadLoadNode>
			{
				// Token: 0x0600085E RID: 2142 RVA: 0x0002753C File Offset: 0x0002573C
				public int Compare(Service1.ThreadLoadManager4l.ThreadLoadNode x, Service1.ThreadLoadManager4l.ThreadLoadNode y)
				{
					int num = x.Load.CompareTo(y.Load);
					if (num != 0)
					{
						return num;
					}
					return x.ThreadId.CompareTo(y.ThreadId);
				}
			}
		}

		// Token: 0x02000092 RID: 146
		public struct PowerStatus
		{
			// Token: 0x0400070A RID: 1802
			public byte ACLineStatus;

			// Token: 0x0400070B RID: 1803
			public byte BatteryFlag;

			// Token: 0x0400070C RID: 1804
			public byte BatteryLifePercent;

			// Token: 0x0400070D RID: 1805
			public byte Reserved;

			// Token: 0x0400070E RID: 1806
			public int BatteryLifeTime;

			// Token: 0x0400070F RID: 1807
			public int BatteryFullLifeTime;
		}

		// Token: 0x02000093 RID: 147
		private enum ThreadAccess : uint
		{
			// Token: 0x04000711 RID: 1809
			TERMINATE = 1U,
			// Token: 0x04000712 RID: 1810
			SUSPEND_RESUME,
			// Token: 0x04000713 RID: 1811
			GET_CONTEXT = 8U,
			// Token: 0x04000714 RID: 1812
			SET_CONTEXT = 16U,
			// Token: 0x04000715 RID: 1813
			SET_INFORMATION = 32U,
			// Token: 0x04000716 RID: 1814
			QUERY_INFORMATION = 64U
		}

		// Token: 0x02000094 RID: 148
		public enum ProcessAccess : uint
		{
			// Token: 0x04000718 RID: 1816
			TERMINATE = 1U,
			// Token: 0x04000719 RID: 1817
			CREATE_THREAD,
			// Token: 0x0400071A RID: 1818
			OPERATION_PROTECT_MEMORY = 4U,
			// Token: 0x0400071B RID: 1819
			OPERATION_WRITE_MEMORY = 8U,
			// Token: 0x0400071C RID: 1820
			OPERATION_READ_MEMORY = 16U,
			// Token: 0x0400071D RID: 1821
			DUPLICATE_HANDLE = 64U,
			// Token: 0x0400071E RID: 1822
			CREATE_PROCESS = 128U,
			// Token: 0x0400071F RID: 1823
			SET_QUOTA = 256U,
			// Token: 0x04000720 RID: 1824
			SET_INFORMATION = 512U,
			// Token: 0x04000721 RID: 1825
			QUERY_INFORMATION = 1024U,
			// Token: 0x04000722 RID: 1826
			QUERY_LIMITED_INFORMATION = 4096U,
			// Token: 0x04000723 RID: 1827
			SYNCHRONIZE = 1048576U,
			// Token: 0x04000724 RID: 1828
			DELETE = 65536U,
			// Token: 0x04000725 RID: 1829
			READ_CONTROL = 131072U,
			// Token: 0x04000726 RID: 1830
			WRITE_DAC = 262144U,
			// Token: 0x04000727 RID: 1831
			WRITE_OWNER = 524288U,
			// Token: 0x04000728 RID: 1832
			STANDARD_RIGHTS_REQUIRED = 983040U,
			// Token: 0x04000729 RID: 1833
			PROCESS_ALL_ACCESS = 2035711U
		}

		// Token: 0x02000095 RID: 149
		private struct PROCESS_POWER_THROTTLING_STATE
		{
			// Token: 0x0400072A RID: 1834
			public int Version;

			// Token: 0x0400072B RID: 1835
			public uint ControlMask;

			// Token: 0x0400072C RID: 1836
			public uint StateMask;
		}
	}
}
