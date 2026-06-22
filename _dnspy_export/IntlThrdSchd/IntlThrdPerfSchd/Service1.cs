using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using Microsoft.Win32;
using OpenLibSys;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000003 RID: 3
	public class Service1 : ServiceBase
	{
		// Token: 0x06000005 RID: 5 RVA: 0x00002140 File Offset: 0x00000340
		public int UpdateNodeP(int node_cap, ref Service1.NodeP node, int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1)
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
			node = new Service1.NodeP(pid, ins_total, store_total, count_total, intval, nonstore_store_ratio, usr_sum, usr_count, usr_ratio, residence, residence1)
			{
				Next = node
			};
			num++;
			return 0;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000221C File Offset: 0x0000041C
		public int FindNodeValueP(ref Service1.NodeP node, int pid, ref long ins_total, ref long store_total, ref long count_total, ref long intval, ref long nonstore_store_ratio, ref long usr_sum, ref long usr_count, ref long usr_ratio, ref long residence, ref long residence1)
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
					return 1;
				}
			}
			return 0;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000022A0 File Offset: 0x000004A0
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

		// Token: 0x06000008 RID: 8 RVA: 0x0000231C File Offset: 0x0000051C
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

		// Token: 0x06000009 RID: 9 RVA: 0x000023D0 File Offset: 0x000005D0
		public int UpdateNode2_little(ref Service1.Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Service1.Node2 node2 = node;
			Service1.Node2 node3 = null;
			Service1.Node2 node4 = new Service1.Node2(id, value1, 0);
			node2 = node;
			int i = 0;
			while (i < 500)
			{
				if (value1 >= node2.Value1)
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

		// Token: 0x0600000A RID: 10 RVA: 0x0000246C File Offset: 0x0000066C
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

		// Token: 0x0600000B RID: 11 RVA: 0x00002688 File Offset: 0x00000888
		public int DeleteNode(ref Service1.Node2 node, int id)
		{
			Service1.Node2 node2 = node;
			Service1.Node2 node3 = null;
			int i = 0;
			while (i < 500)
			{
				if (node2.Id == id)
				{
					if ((node3 == null) & (node2.Next == null))
					{
						node2.Id = 0;
						node2.Value1 = 0L;
						node2.Value2 = 0;
						node2.Next = null;
						return -1;
					}
					if (node3 == null)
					{
						node = node2.Next;
						return 1;
					}
					node3.Next = node2.Next;
					return 1;
				}
				else
				{
					if (node2.Next == null)
					{
						return 0;
					}
					node3 = node2;
					node2 = node2.Next;
					i++;
				}
			}
			return 0;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002718 File Offset: 0x00000918
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

		// Token: 0x0600000D RID: 13 RVA: 0x0000275C File Offset: 0x0000095C
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

		// Token: 0x0600000E RID: 14 RVA: 0x0000280C File Offset: 0x00000A0C
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

		// Token: 0x0600000F RID: 15 RVA: 0x00002838 File Offset: 0x00000A38
		public int FindMaxIpc(Service1.Node node, ref int max_ipc_thread, ref int max_ipc_little)
		{
			for (Service1.Node node2 = node; node2 != null; node2 = node2.Next)
			{
				max_ipc_thread = node2.Id;
				max_ipc_little = node2.Value;
			}
			return -1;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002864 File Offset: 0x00000A64
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

		// Token: 0x06000011 RID: 17 RVA: 0x00002A08 File Offset: 0x00000C08
		public long GetFactor(long usr_ratio, long avg_usr_ratio)
		{
			long num = 50L * usr_ratio / avg_usr_ratio;
			if (num > 100L)
			{
				return 100L;
			}
			return num;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002A28 File Offset: 0x00000C28
		public int Intval2Limit(int oldthread, long intval, long utility, long nonstore_store_ratio, ref long usr_ratio_avg, ref long ins_big, int currentprocessor, long usr_ratio, ref long max_ins, ref long usr_ratio1, long br_sys, ref long tag, uint affinity, ref long reset_count, ref long usr_ratio_little, ref long prod_cons_ratio, ref long ins_little, ref long lock_data, ref long residence_p1, ref long residence_p)
		{
			int num = 1;
			if (usr_ratio > 0L)
			{
				this.tempp += usr_ratio;
				this.tempk += 1L;
			}
			if (((1U << currentprocessor) & this.affinitymask_little) > 0U)
			{
				if (ins_little > 100000L)
				{
					residence_p <<= 1;
				}
				else if (ins_little > 0L)
				{
					residence_p = (residence_p << 1) | 1L;
				}
				if (tag == 2L && (((nonstore_store_ratio > 0L) & (prod_cons_ratio * 100L > nonstore_store_ratio * 100L)) | (ins_little > 500000L)))
				{
					this._6_to_8 += 1L;
					goto IL_0141;
				}
			}
			if (((1U << currentprocessor) & this.affinitymask_big) > 0U)
			{
				if (tag == 6L)
				{
					if ((usr_ratio <= 0L) | (usr_ratio_avg <= 0L) | (br_sys <= 0L) | (nonstore_store_ratio <= 0L) | (ins_big <= 0L))
					{
						num = 1;
					}
					else
					{
						if (ins_big > 100000L)
						{
							residence_p <<= 1;
						}
						else if (ins_big > 0L)
						{
							residence_p = (residence_p << 1) | 1L;
						}
						if ((br_sys * 100L > nonstore_store_ratio * 100L) | (ins_big > 500000L))
						{
							num = 1;
						}
						else
						{
							num = 2;
						}
					}
				}
			}
			else
			{
				num = 0;
			}
			IL_0141:
			if (num == 1)
			{
				if (!((((1U << currentprocessor) & this.affinitymask_little) > 0U) & (tag == 2L)))
				{
					lock_data = DateTime.Now.Ticks;
					return 0;
				}
				if (reset_count == 0L)
				{
					this.UpdateNode2_little(ref this.schd_queue_l2b, oldthread, utility * intval, 0, ref reset_count);
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
					lock_data = DateTime.Now.Ticks;
					return 0;
				}
				if (reset_count == 0L)
				{
					this.UpdateNode2(ref this.schd_queue_b2l, oldthread, usr_ratio, 0, ref reset_count);
					return 2;
				}
			}
			return 0;
		}

		// Token: 0x06000013 RID: 19
		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern bool PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid SchemeGuid);

		// Token: 0x06000014 RID: 20
		[DllImport("kernel32.dll")]
		public static extern bool GetSystemPowerStatus(out Service1.PowerStatus BatteryInfo);

		// Token: 0x06000015 RID: 21
		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadACValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		// Token: 0x06000016 RID: 22
		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadDCValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		// Token: 0x06000017 RID: 23
		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenThread(Service1.ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		// Token: 0x06000018 RID: 24
		[DllImport("kernel32.dll")]
		private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		// Token: 0x06000019 RID: 25
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetThreadAffinityMask(IntPtr hThread, out uint mask);

		// Token: 0x0600001A RID: 26
		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr handle);

		// Token: 0x0600001B RID: 27
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentThreadId();

		// Token: 0x0600001C RID: 28
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentProcessId();

		// Token: 0x0600001D RID: 29 RVA: 0x00002C10 File Offset: 0x00000E10
		public Service1()
		{
			this.InitializeComponent();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000037D8 File Offset: 0x000019D8
		protected override void OnStart(string[] args)
		{
			Service1.<>c__DisplayClass484_0 CS$<>8__locals1 = new Service1.<>c__DisplayClass484_0();
			CS$<>8__locals1.<>4__this = this;
			int[] array = new int[32];
			new int[32];
			CS$<>8__locals1.takeaction = new int[32];
			long[] array2 = new long[32];
			new long[32];
			long[] array3 = new long[32];
			new int[32];
			CS$<>8__locals1.findresult = new int[32];
			CS$<>8__locals1.findresultp = new int[32];
			int num = 0;
			while ((long)num < (long)((ulong)Convert.ToUInt32(this.NumberOfLogicalProcessors)))
			{
				array[num] = 0;
				CS$<>8__locals1.takeaction[num] = 0;
				array2[num] = 0L;
				array3[num] = 0L;
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
			for (int i = 0; i < Convert.ToInt32(this.NumberOfLogicalProcessors); i++)
			{
				if (((1U << i) & this.affinitymask_little) > 0U)
				{
					UIntPtr uintPtr = (UIntPtr)((ulong)Math.Pow(2.0, (double)i));
					this.myOls.WrmsrTx(390U, 4390926U, 0U, uintPtr);
					this.myOls.WrmsrTx(391U, 4391106U, 0U, uintPtr);
					this.myOls.WrmsrTx(392U, 4423364U, 0U, uintPtr);
					this.myOls.WrmsrTx(393U, 4390972U, 0U, uintPtr);
					this.myOls.WrmsrTx(394U, 4260032U, 0U, uintPtr);
					this.myOls.WrmsrTx(395U, 4391104U, 0U, uintPtr);
					this.myOls.WrmsrTx(396U, 4390972U, 0U, uintPtr);
				}
				else
				{
					UIntPtr uintPtr2 = (UIntPtr)((ulong)Math.Pow(2.0, (double)i));
					this.myOls.WrmsrTx(390U, 4391342U, 0U, uintPtr2);
					this.myOls.WrmsrTx(391U, 4391618U, 0U, uintPtr2);
					this.myOls.WrmsrTx(392U, 4391342U, 0U, uintPtr2);
					this.myOls.WrmsrTx(393U, 4391104U, 0U, uintPtr2);
					this.myOls.WrmsrTx(394U, 4260032U, 0U, uintPtr2);
					this.myOls.WrmsrTx(395U, 4390972U, 0U, uintPtr2);
					this.myOls.WrmsrTx(396U, 4390972U, 0U, uintPtr2);
				}
			}
			for (int j = 0; j < Convert.ToInt32(this.NumberOfLogicalProcessors); j++)
			{
				this.sched_queue_l2b[j] = new Service1.Node2(0, 0L, 0);
				this.sched_queue_b2l[j] = new Service1.Node2(0, 0L, 0);
			}
			for (int k = 0; k < Convert.ToInt32(this.NumberOfLogicalProcessors); k++)
			{
				this.tag[k] = 0L;
				this.oldthread_waittime[k] = 0;
				this.core_availability_cnt[k] = 1L;
				this.affinitymask_big |= 1U << k;
				this.myOls.CpuidTx(26U, ref this.l_msr, ref this.eebx, ref this.eecx, ref this.eedx, (UIntPtr)((ulong)Math.Pow(2.0, (double)k)));
				if (this.l_msr > this.max_msr)
				{
					this.max_msr = this.l_msr;
				}
			}
			for (int l = 0; l < Convert.ToInt32(this.NumberOfLogicalProcessors); l++)
			{
				this.myOls.CpuidTx(26U, ref this.l_msr, ref this.eebx, ref this.eecx, ref this.eedx, (UIntPtr)((ulong)Math.Pow(2.0, (double)l)));
				if (this.l_msr < this.max_msr)
				{
					this.affinitymask_little |= 1U << l;
					this.little_num += 1UL;
				}
			}
			this.affinitymask_big &= ~this.affinitymask_little;
			if (Convert.ToUInt64(this.number_of_cores) == Convert.ToUInt64(this.NumberOfLogicalProcessors))
			{
				this.affinitymask_fake_little = this.affinitymask_little;
			}
			else
			{
				this.core_num = 2UL * (Convert.ToUInt64(this.number_of_cores) - this.little_num);
				for (int m = 1; m < (int)this.core_num; m += 2)
				{
					this.affinitymask_fake_little |= 1U << m;
				}
				this.affinitymask_fake_little |= this.affinitymask_little;
			}
			if (this.little_num >= 8UL)
			{
				this.threshold = 1000000L;
			}
			else
			{
				this.threshold = 500000L;
			}
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
			for (int n = 0; n < Convert.ToInt32(this.NumberOfLogicalProcessors); n++)
			{
				this.UpdateNode(1, ref this.wait_core[n], n, 0);
				this.UpdateNode(1, ref this.max_ipc_queue[n], -1, -1);
				this.UpdateNode(1, ref this.max_util_queue[n], -1, -1);
				this.exclude[n] = 0L;
				this.exclude_b[n] = 0L;
				this.exclude_all[n] = 0L;
				this.last_duration[n] = 0L;
				this.now_duration[n] = 0L;
				this.avg_runtime_b[n] = 0L;
				this.avg_runtime_l[n] = 0L;
				this.max_ipc_l[n] = 0L;
				this.max_ipc_b[n] = 0L;
				this.temp4[n] = 0L;
				this.temp5[n] = 0L;
				this.temp6[n] = 0L;
			}
			this.ratio = (uint)(100UL * this.little_num / Convert.ToUInt64(this.NumberOfLogicalProcessors));
			this.ratio_string = this.ratio.ToString();
			this.ratio1 = (uint)(Convert.ToUInt64(this.number_of_cores) * 100UL / Convert.ToUInt64(this.NumberOfLogicalProcessors));
			this.ratio_string1 = this.ratio1.ToString();
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
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel\\KGroups\\00").SetValue("SmallProcessorMask", this.affinitymask_fake_little.ToString(), RegistryValueKind.DWord);
			}
			catch
			{
			}
			Service1.PowerSetActiveScheme(IntPtr.Zero, ref this.powerscheme1);
			Thread.Sleep(2000);
			Service1.PowerSetActiveScheme(IntPtr.Zero, ref this.powerscheme);
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
			new Thread(new ThreadStart(CS$<>8__locals1.<OnStart>g__thread1|0)).Start();
			new Thread(new ThreadStart(CS$<>8__locals1.<OnStart>g__thread2|1)).Start();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000435C File Offset: 0x0000255C
		protected override void OnStop()
		{
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00004360 File Offset: 0x00002560
		private void OnTimedEvent(object sender, ElapsedEventArgs e)
		{
			this.count_stat6 += 1L;
			if (this.count_stat6 > 3840L)
			{
				this.count_stat6 = 0L;
				this.ipc_big_sum = 0L;
				this.ipc_big_count = 0L;
				this.ipc_little_sum = 0L;
				this.ipc_little_count = 0L;
				this.eff_big_sum = 0L;
				this.eff_big_count = 0L;
				this.eff_little_sum = 0L;
				this.eff_little_count = 0L;
			}
			this.count_stat5 += 1L;
			this.count_stat5 = 0L;
			this.datetime_trigger = 15L;
			this.datetime_trigger_little = 15L;
			this.datetime_trigger_exchange = 15L;
			if (this.ipc_big_count > 1000L)
			{
				this.ipc_big_avg = this.ipc_big_sum / this.ipc_big_count;
			}
			if (this.ipc_little_count > 1000L)
			{
				this.ipc_little_avg = this.ipc_little_sum / this.ipc_little_count;
			}
			if (this.eff_big_count > 1000L)
			{
				this.eff_big_avg = this.eff_big_sum / this.eff_big_count;
			}
			if (this.eff_little_count > 1000L)
			{
				this.eff_little_avg = this.eff_little_sum / this.eff_little_count;
			}
			if (this.counter_action_switch == 1L)
			{
				this.count_stat1 += 1L;
				if (this.count_stat1 > 96L)
				{
					this.count_stat1 = 0L;
					this.counter_action_switch = 0L;
					for (int i = 0; i < Convert.ToInt32(this.NumberOfLogicalProcessors); i++)
					{
						if (((1U << i) & this.affinitymask_little) > 0U)
						{
							UIntPtr uintPtr = (UIntPtr)((ulong)Math.Pow(2.0, (double)i));
							this.myOls.WrmsrTx(390U, 4390926U, 0U, uintPtr);
							this.myOls.WrmsrTx(391U, 4391106U, 0U, uintPtr);
							this.myOls.WrmsrTx(392U, 4423364U, 0U, uintPtr);
							this.myOls.WrmsrTx(393U, 4390972U, 0U, uintPtr);
							this.myOls.WrmsrTx(394U, 4260032U, 0U, uintPtr);
							this.myOls.WrmsrTx(395U, 4391104U, 0U, uintPtr);
							this.myOls.WrmsrTx(396U, 4390972U, 0U, uintPtr);
						}
						else
						{
							UIntPtr uintPtr2 = (UIntPtr)((ulong)Math.Pow(2.0, (double)i));
							this.myOls.WrmsrTx(390U, 4391342U, 0U, uintPtr2);
							this.myOls.WrmsrTx(391U, 4391618U, 0U, uintPtr2);
							this.myOls.WrmsrTx(392U, 4391342U, 0U, uintPtr2);
							this.myOls.WrmsrTx(393U, 4391104U, 0U, uintPtr2);
							this.myOls.WrmsrTx(394U, 4260032U, 0U, uintPtr2);
							this.myOls.WrmsrTx(395U, 4390972U, 0U, uintPtr2);
							this.myOls.WrmsrTx(396U, 4390972U, 0U, uintPtr2);
						}
					}
				}
			}
			if (this.counter_action_switch == 0L)
			{
				this.count_stat2 += 1L;
				if (this.count_stat2 > 32L)
				{
					this.count_stat2 = 0L;
					this.counter_action_switch = 1L;
					for (int j = 0; j < Convert.ToInt32(this.NumberOfLogicalProcessors); j++)
					{
						if (((1U << j) & this.affinitymask_little) > 0U)
						{
							UIntPtr uintPtr3 = (UIntPtr)((ulong)Math.Pow(2.0, (double)j));
							this.myOls.WrmsrTx(390U, 196622U, 0U, uintPtr3);
							this.myOls.WrmsrTx(391U, 196802U, 0U, uintPtr3);
							this.myOls.WrmsrTx(392U, 229060U, 0U, uintPtr3);
							this.myOls.WrmsrTx(393U, 196668U, 0U, uintPtr3);
							this.myOls.WrmsrTx(394U, 65728U, 0U, uintPtr3);
							this.myOls.WrmsrTx(395U, 196800U, 0U, uintPtr3);
							this.myOls.WrmsrTx(396U, 196668U, 0U, uintPtr3);
						}
						else
						{
							UIntPtr uintPtr4 = (UIntPtr)((ulong)Math.Pow(2.0, (double)j));
							this.myOls.WrmsrTx(390U, 197038U, 0U, uintPtr4);
							this.myOls.WrmsrTx(391U, 197314U, 0U, uintPtr4);
							this.myOls.WrmsrTx(392U, 197038U, 0U, uintPtr4);
							this.myOls.WrmsrTx(393U, 196800U, 0U, uintPtr4);
							this.myOls.WrmsrTx(394U, 65728U, 0U, uintPtr4);
							this.myOls.WrmsrTx(395U, 196668U, 0U, uintPtr4);
							this.myOls.WrmsrTx(396U, 196668U, 0U, uintPtr4);
						}
					}
				}
			}
			this.count_stat3 += 1L;
			if (this.count_stat3 > 3840L)
			{
				this.avg_ipc_trigger = 0L;
				this.count_stat3 = 0L;
				GC.Collect();
			}
			this.count_stat += 1L;
			if (this.count_stat > 1920L)
			{
				this.count_stat = 0L;
				string text = "统计数据.txt";
				string text2 = "统计数据" + Environment.NewLine + "调度成功次数:" + this._6_to_2.ToString();
				File.WriteAllText(text, text2);
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00004989 File Offset: 0x00002B89
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000049A8 File Offset: 0x00002BA8
		private void InitializeComponent()
		{
			this.components = new Container();
			base.ServiceName = "Service1";
		}

		// Token: 0x04000004 RID: 4
		private Service1.Node1 record = new Service1.Node1();

		// Token: 0x04000005 RID: 5
		private Service1.Node wait_queue = new Service1.Node();

		// Token: 0x04000006 RID: 6
		private Service1.Node1[] threadrecord = new Service1.Node1[10000];

		// Token: 0x04000007 RID: 7
		private Service1.NodeP[] processrecord = new Service1.NodeP[10000];

		// Token: 0x04000008 RID: 8
		private Service1.Node[] max_ipc_queue = new Service1.Node[32];

		// Token: 0x04000009 RID: 9
		private Service1.Node[] max_util_queue = new Service1.Node[32];

		// Token: 0x0400000A RID: 10
		private Service1.Node[] wait_core = new Service1.Node[32];

		// Token: 0x0400000B RID: 11
		private Service1.Node2[] sched_queue_b2l = new Service1.Node2[64];

		// Token: 0x0400000C RID: 12
		private Service1.Node2[] sched_queue_l2b = new Service1.Node2[64];

		// Token: 0x0400000D RID: 13
		private Service1.Node2 schedule_queue = new Service1.Node2();

		// Token: 0x0400000E RID: 14
		private Service1.Node2 schedule_queue_little = new Service1.Node2();

		// Token: 0x0400000F RID: 15
		private Service1.Node2 schd_queue_b2l = new Service1.Node2();

		// Token: 0x04000010 RID: 16
		private Service1.Node2 schd_queue_b2s = new Service1.Node2();

		// Token: 0x04000011 RID: 17
		private Service1.Node2 schd_queue_l2b = new Service1.Node2();

		// Token: 0x04000012 RID: 18
		private Service1.Node2 schd_queue_s2b = new Service1.Node2();

		// Token: 0x04000013 RID: 19
		public long[] lowerlimit = new long[32];

		// Token: 0x04000014 RID: 20
		public long[] upperlimit = new long[32];

		// Token: 0x04000015 RID: 21
		private Guid powerscheme1 = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

		// Token: 0x04000016 RID: 22
		private Guid powerscheme = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

		// Token: 0x04000017 RID: 23
		public int node_cap = 500;

		// Token: 0x04000018 RID: 24
		public long num_chain;

		// Token: 0x04000019 RID: 25
		public long num_chain_little;

		// Token: 0x0400001A RID: 26
		public long num_chain_big;

		// Token: 0x0400001B RID: 27
		public long num_chain2;

		// Token: 0x0400001C RID: 28
		public long action_recored;

		// Token: 0x0400001D RID: 29
		public long[] current_freq = new long[32];

		// Token: 0x0400001E RID: 30
		public uint affinitymask;

		// Token: 0x0400001F RID: 31
		public uint affinitymask_little;

		// Token: 0x04000020 RID: 32
		public uint affinitymask_big;

		// Token: 0x04000021 RID: 33
		public uint affinitymask_big_phyx;

		// Token: 0x04000022 RID: 34
		public uint affinitymask_fake_little;

		// Token: 0x04000023 RID: 35
		private string number_of_cores;

		// Token: 0x04000024 RID: 36
		private string NumberOfLogicalProcessors;

		// Token: 0x04000025 RID: 37
		public uint eax;

		// Token: 0x04000026 RID: 38
		public uint edx;

		// Token: 0x04000027 RID: 39
		public long[] tsc_e = new long[32];

		// Token: 0x04000028 RID: 40
		public long[] tsc_l = new long[32];

		// Token: 0x04000029 RID: 41
		public long[] tsc = new long[32];

		// Token: 0x0400002A RID: 42
		public long[] tsc_total = new long[32];

		// Token: 0x0400002B RID: 43
		public long[] result_ins_e = new long[32];

		// Token: 0x0400002C RID: 44
		public long[] result_ins_l = new long[32];

		// Token: 0x0400002D RID: 45
		public long[] result_ins = new long[32];

		// Token: 0x0400002E RID: 46
		public long[] result_ins_comp_e = new long[32];

		// Token: 0x0400002F RID: 47
		public long[] result_ins_comp_l = new long[32];

		// Token: 0x04000030 RID: 48
		public long[] result_ins_comp = new long[32];

		// Token: 0x04000031 RID: 49
		public long max_single_ratio_big;

		// Token: 0x04000032 RID: 50
		public long max_single_ratio_little;

		// Token: 0x04000033 RID: 51
		public long max_ins_little;

		// Token: 0x04000034 RID: 52
		public long max_ins_big;

		// Token: 0x04000035 RID: 53
		public long max_br_little;

		// Token: 0x04000036 RID: 54
		public long max_br_far_little;

		// Token: 0x04000037 RID: 55
		public long max_br_big;

		// Token: 0x04000038 RID: 56
		public long max_br_far_big;

		// Token: 0x04000039 RID: 57
		public long max_util_big = 50L;

		// Token: 0x0400003A RID: 58
		public long[] single_tag = new long[32];

		// Token: 0x0400003B RID: 59
		public long[] result_br_miss_e = new long[32];

		// Token: 0x0400003C RID: 60
		public long[] result_br_miss_l = new long[32];

		// Token: 0x0400003D RID: 61
		public long[] result_cache_e = new long[32];

		// Token: 0x0400003E RID: 62
		public long[] result_cache_l = new long[32];

		// Token: 0x0400003F RID: 63
		public long[] result_cache = new long[32];

		// Token: 0x04000040 RID: 64
		public long[] result_load_e = new long[32];

		// Token: 0x04000041 RID: 65
		public long[] result_load_l = new long[32];

		// Token: 0x04000042 RID: 66
		public long[] result_load = new long[32];

		// Token: 0x04000043 RID: 67
		public long[] result_store_e = new long[32];

		// Token: 0x04000044 RID: 68
		public long[] result_store_l = new long[32];

		// Token: 0x04000045 RID: 69
		public long[] result_store = new long[32];

		// Token: 0x04000046 RID: 70
		public long[] result_load_l1_e = new long[32];

		// Token: 0x04000047 RID: 71
		public long[] result_load_l1_l = new long[32];

		// Token: 0x04000048 RID: 72
		public long[] result_load_l1 = new long[32];

		// Token: 0x04000049 RID: 73
		public long[] result_br_ins_e = new long[32];

		// Token: 0x0400004A RID: 74
		public long[] result_br_ins_l = new long[32];

		// Token: 0x0400004B RID: 75
		public long[] result_br_ins = new long[32];

		// Token: 0x0400004C RID: 76
		public long[] result_br_indirect_e = new long[32];

		// Token: 0x0400004D RID: 77
		public long[] result_br_indirect_l = new long[32];

		// Token: 0x0400004E RID: 78
		public long[] result_br_indirect = new long[32];

		// Token: 0x0400004F RID: 79
		public long[] br_indirect = new long[32];

		// Token: 0x04000050 RID: 80
		public long[] result_br_far_e = new long[32];

		// Token: 0x04000051 RID: 81
		public long[] result_br_far_l = new long[32];

		// Token: 0x04000052 RID: 82
		public long[] result_br_far = new long[32];

		// Token: 0x04000053 RID: 83
		public long[] br_far = new long[32];

		// Token: 0x04000054 RID: 84
		public long[] result_aclk_e = new long[32];

		// Token: 0x04000055 RID: 85
		public long[] result_aclk_l = new long[32];

		// Token: 0x04000056 RID: 86
		public long[] result_aclk = new long[32];

		// Token: 0x04000057 RID: 87
		public long[] result_mclk_e = new long[32];

		// Token: 0x04000058 RID: 88
		public long[] result_mclk_l = new long[32];

		// Token: 0x04000059 RID: 89
		public long[] result_mclk = new long[32];

		// Token: 0x0400005A RID: 90
		public long[] result_pclk_e = new long[32];

		// Token: 0x0400005B RID: 91
		public long[] result_pclk_l = new long[32];

		// Token: 0x0400005C RID: 92
		public long[] result_pclk = new long[32];

		// Token: 0x0400005D RID: 93
		private Ols myOls = new Ols();

		// Token: 0x0400005E RID: 94
		public long ipc_switch;

		// Token: 0x0400005F RID: 95
		public long active_cores;

		// Token: 0x04000060 RID: 96
		public long[] core_active = new long[32];

		// Token: 0x04000061 RID: 97
		public long active_big_cores;

		// Token: 0x04000062 RID: 98
		public long active_smt_cores;

		// Token: 0x04000063 RID: 99
		public long active_little_cores;

		// Token: 0x04000064 RID: 100
		public long[] single_ratio = new long[32];

		// Token: 0x04000065 RID: 101
		public long[] ht_share = new long[32];

		// Token: 0x04000066 RID: 102
		public long[] br_far_ratio = new long[32];

		// Token: 0x04000067 RID: 103
		public long[] br = new long[32];

		// Token: 0x04000068 RID: 104
		public long[] br_miss = new long[32];

		// Token: 0x04000069 RID: 105
		public long[] cache = new long[32];

		// Token: 0x0400006A RID: 106
		public long[] mem = new long[32];

		// Token: 0x0400006B RID: 107
		public long[] load = new long[32];

		// Token: 0x0400006C RID: 108
		public long[] load_l1 = new long[32];

		// Token: 0x0400006D RID: 109
		public long[] load_l2 = new long[32];

		// Token: 0x0400006E RID: 110
		public long[] load_l3 = new long[32];

		// Token: 0x0400006F RID: 111
		public long[] load_dram = new long[32];

		// Token: 0x04000070 RID: 112
		public long[] cache2mem = new long[32];

		// Token: 0x04000071 RID: 113
		public long[] ins = new long[32];

		// Token: 0x04000072 RID: 114
		public long util_big;

		// Token: 0x04000073 RID: 115
		public long ins_all;

		// Token: 0x04000074 RID: 116
		public long ins_all_avg;

		// Token: 0x04000075 RID: 117
		public long ins_all_whole;

		// Token: 0x04000076 RID: 118
		public long ins_all_whole_sqr;

		// Token: 0x04000077 RID: 119
		public long ins_all_whole_avg;

		// Token: 0x04000078 RID: 120
		public long perf_whole;

		// Token: 0x04000079 RID: 121
		public long perf_whole_old;

		// Token: 0x0400007A RID: 122
		public long perf_whole_avg;

		// Token: 0x0400007B RID: 123
		public long ins_avg;

		// Token: 0x0400007C RID: 124
		public long ins_sqr;

		// Token: 0x0400007D RID: 125
		public long ins_indicator;

		// Token: 0x0400007E RID: 126
		public long ins_big;

		// Token: 0x0400007F RID: 127
		public long ins_constr_smt;

		// Token: 0x04000080 RID: 128
		public long ins_little;

		// Token: 0x04000081 RID: 129
		public long ins_little_comp;

		// Token: 0x04000082 RID: 130
		public long ins_max_comp;

		// Token: 0x04000083 RID: 131
		public long ins_max_load;

		// Token: 0x04000084 RID: 132
		public long ins_max_br;

		// Token: 0x04000085 RID: 133
		public long ins_max;

		// Token: 0x04000086 RID: 134
		public long util_little_all;

		// Token: 0x04000087 RID: 135
		public long aclk_acc;

		// Token: 0x04000088 RID: 136
		public long ins_smt;

		// Token: 0x04000089 RID: 137
		public long ins_little_raw;

		// Token: 0x0400008A RID: 138
		public long ins_big_raw;

		// Token: 0x0400008B RID: 139
		public long ins_smt_raw;

		// Token: 0x0400008C RID: 140
		public long max_ipc;

		// Token: 0x0400008D RID: 141
		private ulong little_num;

		// Token: 0x0400008E RID: 142
		private ulong big_num;

		// Token: 0x0400008F RID: 143
		private ulong core_num;

		// Token: 0x04000090 RID: 144
		private long threshold;

		// Token: 0x04000091 RID: 145
		private long[] datetime_new = new long[32];

		// Token: 0x04000092 RID: 146
		private long[] datetime_old = new long[32];

		// Token: 0x04000093 RID: 147
		private long[] datetime_elapsed = new long[32];

		// Token: 0x04000094 RID: 148
		private long datetime_trigger;

		// Token: 0x04000095 RID: 149
		private long datetime_trigger_little;

		// Token: 0x04000096 RID: 150
		private long datetime_trigger_exchange;

		// Token: 0x04000097 RID: 151
		private long avg_ipc_trigger = 1L;

		// Token: 0x04000098 RID: 152
		private int e_core_position;

		// Token: 0x04000099 RID: 153
		private int currentprocnum_index;

		// Token: 0x0400009A RID: 154
		private long[] count_stat_little = new long[32];

		// Token: 0x0400009B RID: 155
		private long count_stat;

		// Token: 0x0400009C RID: 156
		private long count_stat1;

		// Token: 0x0400009D RID: 157
		private long count_stat2;

		// Token: 0x0400009E RID: 158
		private long count_stat3;

		// Token: 0x0400009F RID: 159
		private long count_stat4;

		// Token: 0x040000A0 RID: 160
		private long count_stat5;

		// Token: 0x040000A1 RID: 161
		private long count_stat6;

		// Token: 0x040000A2 RID: 162
		private long counter_action;

		// Token: 0x040000A3 RID: 163
		private long counter_action_switch;

		// Token: 0x040000A4 RID: 164
		private long[] acc_instruction = new long[32];

		// Token: 0x040000A5 RID: 165
		private long[] acc_aclk = new long[32];

		// Token: 0x040000A6 RID: 166
		private long[] acc_instruction_comp = new long[32];

		// Token: 0x040000A7 RID: 167
		private long[] acc_load = new long[32];

		// Token: 0x040000A8 RID: 168
		private long[] acc_datetime = new long[32];

		// Token: 0x040000A9 RID: 169
		private long[] acc_instruction1 = new long[32];

		// Token: 0x040000AA RID: 170
		private long[] acc_aclk1 = new long[32];

		// Token: 0x040000AB RID: 171
		private long[] acc_instruction_comp1 = new long[32];

		// Token: 0x040000AC RID: 172
		private long[] acc_load1 = new long[32];

		// Token: 0x040000AD RID: 173
		private long[] acc_datetime1 = new long[32];

		// Token: 0x040000AE RID: 174
		private long[] util = new long[32];

		// Token: 0x040000AF RID: 175
		private long cnt_findnode;

		// Token: 0x040000B0 RID: 176
		private long cnt_not_findnode;

		// Token: 0x040000B1 RID: 177
		private int switch_to_big;

		// Token: 0x040000B2 RID: 178
		private int switch_to_big_cnt;

		// Token: 0x040000B3 RID: 179
		private int[] oldthread_waittime = new int[32];

		// Token: 0x040000B4 RID: 180
		private int[] schedule_thread = new int[32];

		// Token: 0x040000B5 RID: 181
		private int[] max_ipc_thread = new int[32];

		// Token: 0x040000B6 RID: 182
		private int[] max_util_thread = new int[32];

		// Token: 0x040000B7 RID: 183
		private int[] max_util_little = new int[32];

		// Token: 0x040000B8 RID: 184
		private int num_queue = 1;

		// Token: 0x040000B9 RID: 185
		private long dummy;

		// Token: 0x040000BA RID: 186
		private int currentthread;

		// Token: 0x040000BB RID: 187
		private int currentprocess;

		// Token: 0x040000BC RID: 188
		private int counter1;

		// Token: 0x040000BD RID: 189
		private int counter2;

		// Token: 0x040000BE RID: 190
		private int counter3;

		// Token: 0x040000BF RID: 191
		private int[] findrecord = new int[32];

		// Token: 0x040000C0 RID: 192
		private UIntPtr j;

		// Token: 0x040000C1 RID: 193
		private uint mask;

		// Token: 0x040000C2 RID: 194
		private uint valueToSet;

		// Token: 0x040000C3 RID: 195
		private long acc_util;

		// Token: 0x040000C4 RID: 196
		private uint ratio;

		// Token: 0x040000C5 RID: 197
		private string ratio_string;

		// Token: 0x040000C6 RID: 198
		private uint ratio1;

		// Token: 0x040000C7 RID: 199
		private string ratio_string1;

		// Token: 0x040000C8 RID: 200
		private long ipc_big_sum;

		// Token: 0x040000C9 RID: 201
		private long ipc_little_sum;

		// Token: 0x040000CA RID: 202
		private long ipc_big_count;

		// Token: 0x040000CB RID: 203
		private long ipc_little_count;

		// Token: 0x040000CC RID: 204
		private long ipc_big_avg;

		// Token: 0x040000CD RID: 205
		private long ipc_little_avg;

		// Token: 0x040000CE RID: 206
		private long eff_big_sum;

		// Token: 0x040000CF RID: 207
		private long eff_little_sum;

		// Token: 0x040000D0 RID: 208
		private long eff_big_count;

		// Token: 0x040000D1 RID: 209
		private long eff_little_count;

		// Token: 0x040000D2 RID: 210
		private long eff_big_avg;

		// Token: 0x040000D3 RID: 211
		private long eff_little_avg;

		// Token: 0x040000D4 RID: 212
		private long[] ins_total = new long[32];

		// Token: 0x040000D5 RID: 213
		private long[] store_total = new long[32];

		// Token: 0x040000D6 RID: 214
		private long[] count_total = new long[32];

		// Token: 0x040000D7 RID: 215
		private long[] intval = new long[32];

		// Token: 0x040000D8 RID: 216
		private long[] nonstore_store_ratio = new long[32];

		// Token: 0x040000D9 RID: 217
		private long[] usr_sum = new long[32];

		// Token: 0x040000DA RID: 218
		private long[] usr_count = new long[32];

		// Token: 0x040000DB RID: 219
		private long[] usr_ratio = new long[32];

		// Token: 0x040000DC RID: 220
		private long[] residence_p = new long[32];

		// Token: 0x040000DD RID: 221
		private long[] residence_p1 = new long[32];

		// Token: 0x040000DE RID: 222
		private long[] acc_instruction_b = new long[32];

		// Token: 0x040000DF RID: 223
		private long[] acc_aclk_b = new long[32];

		// Token: 0x040000E0 RID: 224
		private long[] acc_load_b = new long[32];

		// Token: 0x040000E1 RID: 225
		private long[] acc_store_b = new long[32];

		// Token: 0x040000E2 RID: 226
		private long[] acc_load_miss_b = new long[32];

		// Token: 0x040000E3 RID: 227
		private long[] acc_br_b = new long[32];

		// Token: 0x040000E4 RID: 228
		private long[] acc_runtime_b = new long[32];

		// Token: 0x040000E5 RID: 229
		private long[] cnt_b = new long[32];

		// Token: 0x040000E6 RID: 230
		private long[] acc_instruction_l = new long[32];

		// Token: 0x040000E7 RID: 231
		private long[] acc_aclk_l = new long[32];

		// Token: 0x040000E8 RID: 232
		private long[] acc_load_l = new long[32];

		// Token: 0x040000E9 RID: 233
		private long[] acc_load_l_perm = new long[32];

		// Token: 0x040000EA RID: 234
		private long[] last_duration = new long[32];

		// Token: 0x040000EB RID: 235
		private long[] now_duration = new long[32];

		// Token: 0x040000EC RID: 236
		private long[] acc_store_l = new long[32];

		// Token: 0x040000ED RID: 237
		private long[] acc_store_l_perm = new long[32];

		// Token: 0x040000EE RID: 238
		private long[] acc_load_miss_l = new long[32];

		// Token: 0x040000EF RID: 239
		private long[] acc_br_l = new long[32];

		// Token: 0x040000F0 RID: 240
		private long[] acc_runtime_l = new long[32];

		// Token: 0x040000F1 RID: 241
		private long[] cnt_l = new long[32];

		// Token: 0x040000F2 RID: 242
		private long[] ipc_b = new long[32];

		// Token: 0x040000F3 RID: 243
		private long[] ipc_b_temp = new long[32];

		// Token: 0x040000F4 RID: 244
		private long[] max_ipc_b = new long[32];

		// Token: 0x040000F5 RID: 245
		private long[] max_ins = new long[32];

		// Token: 0x040000F6 RID: 246
		private long[] ipc_l = new long[32];

		// Token: 0x040000F7 RID: 247
		private long[] ipc_l_perm = new long[32];

		// Token: 0x040000F8 RID: 248
		private long[] max_ipc_l = new long[32];

		// Token: 0x040000F9 RID: 249
		private long max_ipc_little;

		// Token: 0x040000FA RID: 250
		private long max_ipc_big;

		// Token: 0x040000FB RID: 251
		private long[] ipc_ratio = new long[32];

		// Token: 0x040000FC RID: 252
		private long[] br_ratio = new long[32];

		// Token: 0x040000FD RID: 253
		private long[] ipc_ratio_temp = new long[32];

		// Token: 0x040000FE RID: 254
		private long[] br_ratio_temp = new long[32];

		// Token: 0x040000FF RID: 255
		private long br_ratio_square;

		// Token: 0x04000100 RID: 256
		private long br_ratio_square_bar;

		// Token: 0x04000101 RID: 257
		private long br_ratio_square_e;

		// Token: 0x04000102 RID: 258
		private long br_ratio_square_count;

		// Token: 0x04000103 RID: 259
		private long ipc_square;

		// Token: 0x04000104 RID: 260
		private long ipc_square_bar;

		// Token: 0x04000105 RID: 261
		private long ipc_square_e;

		// Token: 0x04000106 RID: 262
		private long ipc_square_count;

		// Token: 0x04000107 RID: 263
		private long[] br_load_ratio = new long[32];

		// Token: 0x04000108 RID: 264
		private long[] br_load_ratio_temp = new long[32];

		// Token: 0x04000109 RID: 265
		private long[] load_miss_ratio_b = new long[32];

		// Token: 0x0400010A RID: 266
		private long[] load_miss_ratio_b_temp = new long[32];

		// Token: 0x0400010B RID: 267
		private long[] min_load_miss_ratio_b = new long[32];

		// Token: 0x0400010C RID: 268
		private long[] load_miss_ratio_l = new long[32];

		// Token: 0x0400010D RID: 269
		private long[] avg_runtime_b = new long[32];

		// Token: 0x0400010E RID: 270
		private long[] avg_runtime_l = new long[32];

		// Token: 0x0400010F RID: 271
		private long[] avg_freq_b = new long[32];

		// Token: 0x04000110 RID: 272
		private long[] max_freq_b = new long[32];

		// Token: 0x04000111 RID: 273
		private long[] avg_freq_l = new long[32];

		// Token: 0x04000112 RID: 274
		private long[] lock_data = new long[32];

		// Token: 0x04000113 RID: 275
		private long[] tag = new long[32];

		// Token: 0x04000114 RID: 276
		private long[] duration = new long[32];

		// Token: 0x04000115 RID: 277
		private long[] reset_count = new long[32];

		// Token: 0x04000116 RID: 278
		private uint[] affinity = new uint[32];

		// Token: 0x04000117 RID: 279
		private long[] residence = new long[32];

		// Token: 0x04000118 RID: 280
		private long[] acc_instruction_b1 = new long[32];

		// Token: 0x04000119 RID: 281
		private long[] acc_aclk_b1 = new long[32];

		// Token: 0x0400011A RID: 282
		private long[] acc_load_b1 = new long[32];

		// Token: 0x0400011B RID: 283
		private long[] acc_store_b1 = new long[32];

		// Token: 0x0400011C RID: 284
		private long[] acc_load_miss_b1 = new long[32];

		// Token: 0x0400011D RID: 285
		private long[] acc_br_b1 = new long[32];

		// Token: 0x0400011E RID: 286
		private long[] acc_runtime_b1 = new long[32];

		// Token: 0x0400011F RID: 287
		private long[] cnt_b1 = new long[32];

		// Token: 0x04000120 RID: 288
		private long[] acc_instruction_l1 = new long[32];

		// Token: 0x04000121 RID: 289
		private long[] acc_aclk_l1 = new long[32];

		// Token: 0x04000122 RID: 290
		private long[] acc_load_l1 = new long[32];

		// Token: 0x04000123 RID: 291
		private long[] acc_load_l1_perm = new long[32];

		// Token: 0x04000124 RID: 292
		private long[] last_duration1 = new long[32];

		// Token: 0x04000125 RID: 293
		private long[] now_duration1 = new long[32];

		// Token: 0x04000126 RID: 294
		private long[] acc_store_l1 = new long[32];

		// Token: 0x04000127 RID: 295
		private long[] acc_store_l1_perm = new long[32];

		// Token: 0x04000128 RID: 296
		private long[] acc_load_miss_l1 = new long[32];

		// Token: 0x04000129 RID: 297
		private long[] acc_br_l1 = new long[32];

		// Token: 0x0400012A RID: 298
		private long[] acc_runtime_l1 = new long[32];

		// Token: 0x0400012B RID: 299
		private long[] cnt_l1 = new long[32];

		// Token: 0x0400012C RID: 300
		private long[] ipc_b1 = new long[32];

		// Token: 0x0400012D RID: 301
		private long[] max_ipc_b1 = new long[32];

		// Token: 0x0400012E RID: 302
		private long[] max_ins1 = new long[32];

		// Token: 0x0400012F RID: 303
		private long[] ipc_l1 = new long[32];

		// Token: 0x04000130 RID: 304
		private long[] ipc_l1_perm = new long[32];

		// Token: 0x04000131 RID: 305
		private long[] max_ipc_l1 = new long[32];

		// Token: 0x04000132 RID: 306
		private long[] ipc_ratio1 = new long[32];

		// Token: 0x04000133 RID: 307
		private long[] br_ratio1 = new long[32];

		// Token: 0x04000134 RID: 308
		private long[] br_load_ratio1 = new long[32];

		// Token: 0x04000135 RID: 309
		private long[] load_miss_ratio_b1 = new long[32];

		// Token: 0x04000136 RID: 310
		private long acc_instruction_b1_t;

		// Token: 0x04000137 RID: 311
		private long acc_aclk_b1_t;

		// Token: 0x04000138 RID: 312
		private long acc_load_b1_t;

		// Token: 0x04000139 RID: 313
		private long acc_store_b1_t;

		// Token: 0x0400013A RID: 314
		private long acc_load_miss_b1_t;

		// Token: 0x0400013B RID: 315
		private long acc_br_b1_t;

		// Token: 0x0400013C RID: 316
		private long acc_runtime_b1_t;

		// Token: 0x0400013D RID: 317
		private long cnt_b1_t;

		// Token: 0x0400013E RID: 318
		private long acc_instruction_l1_t;

		// Token: 0x0400013F RID: 319
		private long acc_aclk_l1_t;

		// Token: 0x04000140 RID: 320
		private long acc_load_l1_t;

		// Token: 0x04000141 RID: 321
		private long acc_load_l1_perm_t;

		// Token: 0x04000142 RID: 322
		private long acc_store_l1_t;

		// Token: 0x04000143 RID: 323
		private long acc_store_l1_perm_t;

		// Token: 0x04000144 RID: 324
		private long acc_load_miss_l1_t;

		// Token: 0x04000145 RID: 325
		private long acc_br_l1_t;

		// Token: 0x04000146 RID: 326
		private long acc_runtime_l1_t;

		// Token: 0x04000147 RID: 327
		private long cnt_l1_t;

		// Token: 0x04000148 RID: 328
		private long ipc_b1_t;

		// Token: 0x04000149 RID: 329
		private long max_ipc_b1_t;

		// Token: 0x0400014A RID: 330
		private long ipc_l1_t;

		// Token: 0x0400014B RID: 331
		private long ipc_l1_perm_t;

		// Token: 0x0400014C RID: 332
		private long max_ipc_l1_t;

		// Token: 0x0400014D RID: 333
		private long ipc_ratio1_t;

		// Token: 0x0400014E RID: 334
		private long br_ratio1_t;

		// Token: 0x0400014F RID: 335
		private long br_load_ratio1_t;

		// Token: 0x04000150 RID: 336
		private long load_miss_ratio_b1_t;

		// Token: 0x04000151 RID: 337
		private long min_load_miss_ratio_b1_t;

		// Token: 0x04000152 RID: 338
		private long load_miss_ratio_l1_t;

		// Token: 0x04000153 RID: 339
		private long avg_runtime_b1_t;

		// Token: 0x04000154 RID: 340
		private long avg_runtime_l1_t;

		// Token: 0x04000155 RID: 341
		private long avg_freq_b1_t;

		// Token: 0x04000156 RID: 342
		private long avg_freq_l1_t;

		// Token: 0x04000157 RID: 343
		private long max_ins_t;

		// Token: 0x04000158 RID: 344
		private long lock_data1_t;

		// Token: 0x04000159 RID: 345
		private long tag1_t;

		// Token: 0x0400015A RID: 346
		private long duration1_t;

		// Token: 0x0400015B RID: 347
		private long reset_count1_t;

		// Token: 0x0400015C RID: 348
		private uint affinity1_t;

		// Token: 0x0400015D RID: 349
		private long[] temp1 = new long[32];

		// Token: 0x0400015E RID: 350
		private long[] temp2 = new long[32];

		// Token: 0x0400015F RID: 351
		private long[] temp3 = new long[32];

		// Token: 0x04000160 RID: 352
		private long[] temp4 = new long[32];

		// Token: 0x04000161 RID: 353
		private long[] temp41 = new long[32];

		// Token: 0x04000162 RID: 354
		private long[] temp5 = new long[32];

		// Token: 0x04000163 RID: 355
		private long[] temp51 = new long[32];

		// Token: 0x04000164 RID: 356
		private long[] temp6 = new long[32];

		// Token: 0x04000165 RID: 357
		private long[] temp_ticks = new long[32];

		// Token: 0x04000166 RID: 358
		private long tempp;

		// Token: 0x04000167 RID: 359
		private long tempk;

		// Token: 0x04000168 RID: 360
		private long[] sched_ratio = new long[32];

		// Token: 0x04000169 RID: 361
		private long[] ins_ratio = new long[32];

		// Token: 0x0400016A RID: 362
		private long avg_comp_ldst_ratio;

		// Token: 0x0400016B RID: 363
		private long avg_comp_ldst_sum;

		// Token: 0x0400016C RID: 364
		private long avg_comp_ldst_count;

		// Token: 0x0400016D RID: 365
		private long avg_comp_br_ratio;

		// Token: 0x0400016E RID: 366
		private long avg_comp_br_sum;

		// Token: 0x0400016F RID: 367
		private long avg_comp_br_count;

		// Token: 0x04000170 RID: 368
		private long avg_ipc_ratio_sum;

		// Token: 0x04000171 RID: 369
		private long avg_ipc_ratio_count;

		// Token: 0x04000172 RID: 370
		private long avg_ipc_ratio;

		// Token: 0x04000173 RID: 371
		private long[] min_load_miss_ratio_b1 = new long[32];

		// Token: 0x04000174 RID: 372
		private long[] load_miss_ratio_l1 = new long[32];

		// Token: 0x04000175 RID: 373
		private long[] avg_runtime_b1 = new long[32];

		// Token: 0x04000176 RID: 374
		private long[] avg_runtime_l1 = new long[32];

		// Token: 0x04000177 RID: 375
		private long[] avg_freq_b1 = new long[32];

		// Token: 0x04000178 RID: 376
		private long[] avg_freq_l1 = new long[32];

		// Token: 0x04000179 RID: 377
		private long[] lock_data1 = new long[32];

		// Token: 0x0400017A RID: 378
		private long[] tag1 = new long[32];

		// Token: 0x0400017B RID: 379
		private long[] duration1 = new long[32];

		// Token: 0x0400017C RID: 380
		private long[] reset_count1 = new long[32];

		// Token: 0x0400017D RID: 381
		private uint[] affinity1 = new uint[32];

		// Token: 0x0400017E RID: 382
		private long[] residence1 = new long[32];

		// Token: 0x0400017F RID: 383
		private long[] prev_tag = new long[32];

		// Token: 0x04000180 RID: 384
		private uint[] prev_affinity = new uint[32];

		// Token: 0x04000181 RID: 385
		private long count_fast_ipc;

		// Token: 0x04000182 RID: 386
		private long count_fast_br;

		// Token: 0x04000183 RID: 387
		private long count_fast_comp;

		// Token: 0x04000184 RID: 388
		private long count_slow;

		// Token: 0x04000185 RID: 389
		private long count_heavy;

		// Token: 0x04000186 RID: 390
		private long _6_to_2;

		// Token: 0x04000187 RID: 391
		private long _6_to_1;

		// Token: 0x04000188 RID: 392
		private long _2_to_6;

		// Token: 0x04000189 RID: 393
		private long _6_to_8;

		// Token: 0x0400018A RID: 394
		private long _8_to_6;

		// Token: 0x0400018B RID: 395
		private long count_threads;

		// Token: 0x0400018C RID: 396
		private long count_stay_big;

		// Token: 0x0400018D RID: 397
		private int config;

		// Token: 0x0400018E RID: 398
		private int gamemode;

		// Token: 0x0400018F RID: 399
		private long[] core_availability_cnt = new long[32];

		// Token: 0x04000190 RID: 400
		private long[] test_ratio = new long[32];

		// Token: 0x04000191 RID: 401
		private long[] value = new long[32];

		// Token: 0x04000192 RID: 402
		private long max_freq;

		// Token: 0x04000193 RID: 403
		private long[] exclude_b = new long[32];

		// Token: 0x04000194 RID: 404
		private long[] exclude = new long[32];

		// Token: 0x04000195 RID: 405
		private long[] exclude_all = new long[32];

		// Token: 0x04000196 RID: 406
		private long[] allow_exclude = new long[32];

		// Token: 0x04000197 RID: 407
		private long[] exclude1 = new long[32];

		// Token: 0x04000198 RID: 408
		private long[] exclude_all1 = new long[32];

		// Token: 0x04000199 RID: 409
		private long[] allow_exclude1 = new long[32];

		// Token: 0x0400019A RID: 410
		private long avg_ipc;

		// Token: 0x0400019B RID: 411
		private long acc_ins;

		// Token: 0x0400019C RID: 412
		private long acc_loads;

		// Token: 0x0400019D RID: 413
		private long acc_loads_e;

		// Token: 0x0400019E RID: 414
		private long acc_loads_miss;

		// Token: 0x0400019F RID: 415
		private long acc_loads_miss_e;

		// Token: 0x040001A0 RID: 416
		private long acc_brs;

		// Token: 0x040001A1 RID: 417
		private long acc_brs_e;

		// Token: 0x040001A2 RID: 418
		private long acc_brs_miss;

		// Token: 0x040001A3 RID: 419
		private long acc_brs_miss_e;

		// Token: 0x040001A4 RID: 420
		private long acc_aclks;

		// Token: 0x040001A5 RID: 421
		private long acc_ins_e;

		// Token: 0x040001A6 RID: 422
		private long acc_aclks1_e;

		// Token: 0x040001A7 RID: 423
		private long acc_aclks_e;

		// Token: 0x040001A8 RID: 424
		private long avg_diff;

		// Token: 0x040001A9 RID: 425
		private long acc_aclks1;

		// Token: 0x040001AA RID: 426
		private long acc_date;

		// Token: 0x040001AB RID: 427
		private long start;

		// Token: 0x040001AC RID: 428
		private long numberofchain;

		// Token: 0x040001AD RID: 429
		private long acc_ins_b;

		// Token: 0x040001AE RID: 430
		private long acc_ins_l;

		// Token: 0x040001AF RID: 431
		private long acc_ack_b;

		// Token: 0x040001B0 RID: 432
		private long acc_ack_l;

		// Token: 0x040001B1 RID: 433
		private long avg_ipc_b;

		// Token: 0x040001B2 RID: 434
		private long avg_ipc_l;

		// Token: 0x040001B3 RID: 435
		private long avg_ipc_ratio_bak;

		// Token: 0x040001B4 RID: 436
		private long acc_br_all;

		// Token: 0x040001B5 RID: 437
		private long acc_cond_br_all;

		// Token: 0x040001B6 RID: 438
		private long avg_cond_br_ratio;

		// Token: 0x040001B7 RID: 439
		private long min_cond_br_ratio = 100L;

		// Token: 0x040001B8 RID: 440
		private long max_cond_br_ratio;

		// Token: 0x040001B9 RID: 441
		private long[] count_intval = new long[32];

		// Token: 0x040001BA RID: 442
		private long count_intval_all;

		// Token: 0x040001BB RID: 443
		private long count_intval_avg;

		// Token: 0x040001BC RID: 444
		private long max_ipc_global;

		// Token: 0x040001BD RID: 445
		private int[] currentprocessor = new int[32];

		// Token: 0x040001BE RID: 446
		private long total_aclks;

		// Token: 0x040001BF RID: 447
		private long total_ins;

		// Token: 0x040001C0 RID: 448
		private long total_ins1;

		// Token: 0x040001C1 RID: 449
		private long total_aclks1;

		// Token: 0x040001C2 RID: 450
		private uint eeax;

		// Token: 0x040001C3 RID: 451
		private uint eebx;

		// Token: 0x040001C4 RID: 452
		private uint eecx;

		// Token: 0x040001C5 RID: 453
		private uint eedx;

		// Token: 0x040001C6 RID: 454
		private uint e_msr;

		// Token: 0x040001C7 RID: 455
		private uint l_msr;

		// Token: 0x040001C8 RID: 456
		private uint max_msr;

		// Token: 0x040001C9 RID: 457
		private IContainer components;

		// Token: 0x02000006 RID: 6
		public class Node2
		{
			// Token: 0x0600002F RID: 47 RVA: 0x000053D5 File Offset: 0x000035D5
			public Node2()
			{
			}

			// Token: 0x06000030 RID: 48 RVA: 0x000053DD File Offset: 0x000035DD
			public Node2(int id, long value1, int value2)
			{
				this.Id = id;
				this.Value1 = value1;
				this.Value2 = value2;
				this.Next = null;
			}

			// Token: 0x17000001 RID: 1
			// (get) Token: 0x06000031 RID: 49 RVA: 0x00005401 File Offset: 0x00003601
			// (set) Token: 0x06000032 RID: 50 RVA: 0x00005409 File Offset: 0x00003609
			public int Id { get; set; }

			// Token: 0x17000002 RID: 2
			// (get) Token: 0x06000033 RID: 51 RVA: 0x00005412 File Offset: 0x00003612
			// (set) Token: 0x06000034 RID: 52 RVA: 0x0000541A File Offset: 0x0000361A
			public long Value1 { get; set; }

			// Token: 0x17000003 RID: 3
			// (get) Token: 0x06000035 RID: 53 RVA: 0x00005423 File Offset: 0x00003623
			// (set) Token: 0x06000036 RID: 54 RVA: 0x0000542B File Offset: 0x0000362B
			public int Value2 { get; set; }

			// Token: 0x17000004 RID: 4
			// (get) Token: 0x06000037 RID: 55 RVA: 0x00005434 File Offset: 0x00003634
			// (set) Token: 0x06000038 RID: 56 RVA: 0x0000543C File Offset: 0x0000363C
			public Service1.Node2 Next { get; set; }
		}

		// Token: 0x02000007 RID: 7
		public class Node
		{
			// Token: 0x06000039 RID: 57 RVA: 0x00005445 File Offset: 0x00003645
			public Node()
			{
			}

			// Token: 0x0600003A RID: 58 RVA: 0x0000544D File Offset: 0x0000364D
			public Node(int id, int value)
			{
				this.Id = id;
				this.Value = value;
				this.Next = null;
			}

			// Token: 0x17000005 RID: 5
			// (get) Token: 0x0600003B RID: 59 RVA: 0x0000546A File Offset: 0x0000366A
			// (set) Token: 0x0600003C RID: 60 RVA: 0x00005472 File Offset: 0x00003672
			public int Id { get; set; }

			// Token: 0x17000006 RID: 6
			// (get) Token: 0x0600003D RID: 61 RVA: 0x0000547B File Offset: 0x0000367B
			// (set) Token: 0x0600003E RID: 62 RVA: 0x00005483 File Offset: 0x00003683
			public int Value { get; set; }

			// Token: 0x17000007 RID: 7
			// (get) Token: 0x0600003F RID: 63 RVA: 0x0000548C File Offset: 0x0000368C
			// (set) Token: 0x06000040 RID: 64 RVA: 0x00005494 File Offset: 0x00003694
			public Service1.Node Next { get; set; }
		}

		// Token: 0x02000008 RID: 8
		public class Node1
		{
			// Token: 0x06000041 RID: 65 RVA: 0x0000549D File Offset: 0x0000369D
			public Node1()
			{
			}

			// Token: 0x06000042 RID: 66 RVA: 0x000054A8 File Offset: 0x000036A8
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

			// Token: 0x17000008 RID: 8
			// (get) Token: 0x06000043 RID: 67 RVA: 0x00005617 File Offset: 0x00003817
			// (set) Token: 0x06000044 RID: 68 RVA: 0x0000561F File Offset: 0x0000381F
			public int Id { get; set; }

			// Token: 0x17000009 RID: 9
			// (get) Token: 0x06000045 RID: 69 RVA: 0x00005628 File Offset: 0x00003828
			// (set) Token: 0x06000046 RID: 70 RVA: 0x00005630 File Offset: 0x00003830
			public long Acc_instruction_b { get; set; }

			// Token: 0x1700000A RID: 10
			// (get) Token: 0x06000047 RID: 71 RVA: 0x00005639 File Offset: 0x00003839
			// (set) Token: 0x06000048 RID: 72 RVA: 0x00005641 File Offset: 0x00003841
			public long Acc_aclk_b { get; set; }

			// Token: 0x1700000B RID: 11
			// (get) Token: 0x06000049 RID: 73 RVA: 0x0000564A File Offset: 0x0000384A
			// (set) Token: 0x0600004A RID: 74 RVA: 0x00005652 File Offset: 0x00003852
			public long Acc_load_b { get; set; }

			// Token: 0x1700000C RID: 12
			// (get) Token: 0x0600004B RID: 75 RVA: 0x0000565B File Offset: 0x0000385B
			// (set) Token: 0x0600004C RID: 76 RVA: 0x00005663 File Offset: 0x00003863
			public long Acc_store_b { get; set; }

			// Token: 0x1700000D RID: 13
			// (get) Token: 0x0600004D RID: 77 RVA: 0x0000566C File Offset: 0x0000386C
			// (set) Token: 0x0600004E RID: 78 RVA: 0x00005674 File Offset: 0x00003874
			public long Acc_load_miss_b { get; set; }

			// Token: 0x1700000E RID: 14
			// (get) Token: 0x0600004F RID: 79 RVA: 0x0000567D File Offset: 0x0000387D
			// (set) Token: 0x06000050 RID: 80 RVA: 0x00005685 File Offset: 0x00003885
			public long Acc_br_b { get; set; }

			// Token: 0x1700000F RID: 15
			// (get) Token: 0x06000051 RID: 81 RVA: 0x0000568E File Offset: 0x0000388E
			// (set) Token: 0x06000052 RID: 82 RVA: 0x00005696 File Offset: 0x00003896
			public long Acc_runtime_b { get; set; }

			// Token: 0x17000010 RID: 16
			// (get) Token: 0x06000053 RID: 83 RVA: 0x0000569F File Offset: 0x0000389F
			// (set) Token: 0x06000054 RID: 84 RVA: 0x000056A7 File Offset: 0x000038A7
			public long Cnt_b { get; set; }

			// Token: 0x17000011 RID: 17
			// (get) Token: 0x06000055 RID: 85 RVA: 0x000056B0 File Offset: 0x000038B0
			// (set) Token: 0x06000056 RID: 86 RVA: 0x000056B8 File Offset: 0x000038B8
			public long Acc_instruction_l { get; set; }

			// Token: 0x17000012 RID: 18
			// (get) Token: 0x06000057 RID: 87 RVA: 0x000056C1 File Offset: 0x000038C1
			// (set) Token: 0x06000058 RID: 88 RVA: 0x000056C9 File Offset: 0x000038C9
			public long Acc_aclk_l { get; set; }

			// Token: 0x17000013 RID: 19
			// (get) Token: 0x06000059 RID: 89 RVA: 0x000056D2 File Offset: 0x000038D2
			// (set) Token: 0x0600005A RID: 90 RVA: 0x000056DA File Offset: 0x000038DA
			public long Acc_load_l { get; set; }

			// Token: 0x17000014 RID: 20
			// (get) Token: 0x0600005B RID: 91 RVA: 0x000056E3 File Offset: 0x000038E3
			// (set) Token: 0x0600005C RID: 92 RVA: 0x000056EB File Offset: 0x000038EB
			public long Acc_load_l_perm { get; set; }

			// Token: 0x17000015 RID: 21
			// (get) Token: 0x0600005D RID: 93 RVA: 0x000056F4 File Offset: 0x000038F4
			// (set) Token: 0x0600005E RID: 94 RVA: 0x000056FC File Offset: 0x000038FC
			public long Last_duration { get; set; }

			// Token: 0x17000016 RID: 22
			// (get) Token: 0x0600005F RID: 95 RVA: 0x00005705 File Offset: 0x00003905
			// (set) Token: 0x06000060 RID: 96 RVA: 0x0000570D File Offset: 0x0000390D
			public long Now_duration { get; set; }

			// Token: 0x17000017 RID: 23
			// (get) Token: 0x06000061 RID: 97 RVA: 0x00005716 File Offset: 0x00003916
			// (set) Token: 0x06000062 RID: 98 RVA: 0x0000571E File Offset: 0x0000391E
			public long Acc_store_l { get; set; }

			// Token: 0x17000018 RID: 24
			// (get) Token: 0x06000063 RID: 99 RVA: 0x00005727 File Offset: 0x00003927
			// (set) Token: 0x06000064 RID: 100 RVA: 0x0000572F File Offset: 0x0000392F
			public long Acc_store_l_perm { get; set; }

			// Token: 0x17000019 RID: 25
			// (get) Token: 0x06000065 RID: 101 RVA: 0x00005738 File Offset: 0x00003938
			// (set) Token: 0x06000066 RID: 102 RVA: 0x00005740 File Offset: 0x00003940
			public long Acc_load_miss_l { get; set; }

			// Token: 0x1700001A RID: 26
			// (get) Token: 0x06000067 RID: 103 RVA: 0x00005749 File Offset: 0x00003949
			// (set) Token: 0x06000068 RID: 104 RVA: 0x00005751 File Offset: 0x00003951
			public long Acc_br_l { get; set; }

			// Token: 0x1700001B RID: 27
			// (get) Token: 0x06000069 RID: 105 RVA: 0x0000575A File Offset: 0x0000395A
			// (set) Token: 0x0600006A RID: 106 RVA: 0x00005762 File Offset: 0x00003962
			public long Acc_runtime_l { get; set; }

			// Token: 0x1700001C RID: 28
			// (get) Token: 0x0600006B RID: 107 RVA: 0x0000576B File Offset: 0x0000396B
			// (set) Token: 0x0600006C RID: 108 RVA: 0x00005773 File Offset: 0x00003973
			public long Cnt_l { get; set; }

			// Token: 0x1700001D RID: 29
			// (get) Token: 0x0600006D RID: 109 RVA: 0x0000577C File Offset: 0x0000397C
			// (set) Token: 0x0600006E RID: 110 RVA: 0x00005784 File Offset: 0x00003984
			public long Ipc_b { get; set; }

			// Token: 0x1700001E RID: 30
			// (get) Token: 0x0600006F RID: 111 RVA: 0x0000578D File Offset: 0x0000398D
			// (set) Token: 0x06000070 RID: 112 RVA: 0x00005795 File Offset: 0x00003995
			public long Max_ipc_b { get; set; }

			// Token: 0x1700001F RID: 31
			// (get) Token: 0x06000071 RID: 113 RVA: 0x0000579E File Offset: 0x0000399E
			// (set) Token: 0x06000072 RID: 114 RVA: 0x000057A6 File Offset: 0x000039A6
			public long Ipc_l { get; set; }

			// Token: 0x17000020 RID: 32
			// (get) Token: 0x06000073 RID: 115 RVA: 0x000057AF File Offset: 0x000039AF
			// (set) Token: 0x06000074 RID: 116 RVA: 0x000057B7 File Offset: 0x000039B7
			public long Ipc_l_perm { get; set; }

			// Token: 0x17000021 RID: 33
			// (get) Token: 0x06000075 RID: 117 RVA: 0x000057C0 File Offset: 0x000039C0
			// (set) Token: 0x06000076 RID: 118 RVA: 0x000057C8 File Offset: 0x000039C8
			public long Max_ipc_l { get; set; }

			// Token: 0x17000022 RID: 34
			// (get) Token: 0x06000077 RID: 119 RVA: 0x000057D1 File Offset: 0x000039D1
			// (set) Token: 0x06000078 RID: 120 RVA: 0x000057D9 File Offset: 0x000039D9
			public long Ipc_ratio { get; set; }

			// Token: 0x17000023 RID: 35
			// (get) Token: 0x06000079 RID: 121 RVA: 0x000057E2 File Offset: 0x000039E2
			// (set) Token: 0x0600007A RID: 122 RVA: 0x000057EA File Offset: 0x000039EA
			public long Br_ratio { get; set; }

			// Token: 0x17000024 RID: 36
			// (get) Token: 0x0600007B RID: 123 RVA: 0x000057F3 File Offset: 0x000039F3
			// (set) Token: 0x0600007C RID: 124 RVA: 0x000057FB File Offset: 0x000039FB
			public long Br_load_ratio { get; set; }

			// Token: 0x17000025 RID: 37
			// (get) Token: 0x0600007D RID: 125 RVA: 0x00005804 File Offset: 0x00003A04
			// (set) Token: 0x0600007E RID: 126 RVA: 0x0000580C File Offset: 0x00003A0C
			public long Load_miss_ratio_b { get; set; }

			// Token: 0x17000026 RID: 38
			// (get) Token: 0x0600007F RID: 127 RVA: 0x00005815 File Offset: 0x00003A15
			// (set) Token: 0x06000080 RID: 128 RVA: 0x0000581D File Offset: 0x00003A1D
			public long Min_load_miss_ratio_b { get; set; }

			// Token: 0x17000027 RID: 39
			// (get) Token: 0x06000081 RID: 129 RVA: 0x00005826 File Offset: 0x00003A26
			// (set) Token: 0x06000082 RID: 130 RVA: 0x0000582E File Offset: 0x00003A2E
			public long Load_miss_ratio_l { get; set; }

			// Token: 0x17000028 RID: 40
			// (get) Token: 0x06000083 RID: 131 RVA: 0x00005837 File Offset: 0x00003A37
			// (set) Token: 0x06000084 RID: 132 RVA: 0x0000583F File Offset: 0x00003A3F
			public long Avg_runtime_b { get; set; }

			// Token: 0x17000029 RID: 41
			// (get) Token: 0x06000085 RID: 133 RVA: 0x00005848 File Offset: 0x00003A48
			// (set) Token: 0x06000086 RID: 134 RVA: 0x00005850 File Offset: 0x00003A50
			public long Avg_runtime_l { get; set; }

			// Token: 0x1700002A RID: 42
			// (get) Token: 0x06000087 RID: 135 RVA: 0x00005859 File Offset: 0x00003A59
			// (set) Token: 0x06000088 RID: 136 RVA: 0x00005861 File Offset: 0x00003A61
			public long Avg_freq_b { get; set; }

			// Token: 0x1700002B RID: 43
			// (get) Token: 0x06000089 RID: 137 RVA: 0x0000586A File Offset: 0x00003A6A
			// (set) Token: 0x0600008A RID: 138 RVA: 0x00005872 File Offset: 0x00003A72
			public long Avg_freq_l { get; set; }

			// Token: 0x1700002C RID: 44
			// (get) Token: 0x0600008B RID: 139 RVA: 0x0000587B File Offset: 0x00003A7B
			// (set) Token: 0x0600008C RID: 140 RVA: 0x00005883 File Offset: 0x00003A83
			public long Max_ins { get; set; }

			// Token: 0x1700002D RID: 45
			// (get) Token: 0x0600008D RID: 141 RVA: 0x0000588C File Offset: 0x00003A8C
			// (set) Token: 0x0600008E RID: 142 RVA: 0x00005894 File Offset: 0x00003A94
			public long Lock_data { get; set; }

			// Token: 0x1700002E RID: 46
			// (get) Token: 0x0600008F RID: 143 RVA: 0x0000589D File Offset: 0x00003A9D
			// (set) Token: 0x06000090 RID: 144 RVA: 0x000058A5 File Offset: 0x00003AA5
			public long Tag { get; set; }

			// Token: 0x1700002F RID: 47
			// (get) Token: 0x06000091 RID: 145 RVA: 0x000058AE File Offset: 0x00003AAE
			// (set) Token: 0x06000092 RID: 146 RVA: 0x000058B6 File Offset: 0x00003AB6
			public long Duration { get; set; }

			// Token: 0x17000030 RID: 48
			// (get) Token: 0x06000093 RID: 147 RVA: 0x000058BF File Offset: 0x00003ABF
			// (set) Token: 0x06000094 RID: 148 RVA: 0x000058C7 File Offset: 0x00003AC7
			public long Reset_count { get; set; }

			// Token: 0x17000031 RID: 49
			// (get) Token: 0x06000095 RID: 149 RVA: 0x000058D0 File Offset: 0x00003AD0
			// (set) Token: 0x06000096 RID: 150 RVA: 0x000058D8 File Offset: 0x00003AD8
			public uint Affinity { get; set; }

			// Token: 0x17000032 RID: 50
			// (get) Token: 0x06000097 RID: 151 RVA: 0x000058E1 File Offset: 0x00003AE1
			// (set) Token: 0x06000098 RID: 152 RVA: 0x000058E9 File Offset: 0x00003AE9
			public long Residence { get; set; }

			// Token: 0x17000033 RID: 51
			// (get) Token: 0x06000099 RID: 153 RVA: 0x000058F2 File Offset: 0x00003AF2
			// (set) Token: 0x0600009A RID: 154 RVA: 0x000058FA File Offset: 0x00003AFA
			public Service1.Node1 Next { get; set; }
		}

		// Token: 0x02000009 RID: 9
		public class NodeP
		{
			// Token: 0x0600009B RID: 155 RVA: 0x00005903 File Offset: 0x00003B03
			public NodeP()
			{
			}

			// Token: 0x0600009C RID: 156 RVA: 0x0000590C File Offset: 0x00003B0C
			public NodeP(int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1)
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
				this.Next = null;
			}

			// Token: 0x17000034 RID: 52
			// (get) Token: 0x0600009D RID: 157 RVA: 0x0000597B File Offset: 0x00003B7B
			// (set) Token: 0x0600009E RID: 158 RVA: 0x00005983 File Offset: 0x00003B83
			public int PId { get; set; }

			// Token: 0x17000035 RID: 53
			// (get) Token: 0x0600009F RID: 159 RVA: 0x0000598C File Offset: 0x00003B8C
			// (set) Token: 0x060000A0 RID: 160 RVA: 0x00005994 File Offset: 0x00003B94
			public long Ins_total { get; set; }

			// Token: 0x17000036 RID: 54
			// (get) Token: 0x060000A1 RID: 161 RVA: 0x0000599D File Offset: 0x00003B9D
			// (set) Token: 0x060000A2 RID: 162 RVA: 0x000059A5 File Offset: 0x00003BA5
			public long Store_total { get; set; }

			// Token: 0x17000037 RID: 55
			// (get) Token: 0x060000A3 RID: 163 RVA: 0x000059AE File Offset: 0x00003BAE
			// (set) Token: 0x060000A4 RID: 164 RVA: 0x000059B6 File Offset: 0x00003BB6
			public long Count_total { get; set; }

			// Token: 0x17000038 RID: 56
			// (get) Token: 0x060000A5 RID: 165 RVA: 0x000059BF File Offset: 0x00003BBF
			// (set) Token: 0x060000A6 RID: 166 RVA: 0x000059C7 File Offset: 0x00003BC7
			public long Intval { get; set; }

			// Token: 0x17000039 RID: 57
			// (get) Token: 0x060000A7 RID: 167 RVA: 0x000059D0 File Offset: 0x00003BD0
			// (set) Token: 0x060000A8 RID: 168 RVA: 0x000059D8 File Offset: 0x00003BD8
			public long Nonstore_store_ratio { get; set; }

			// Token: 0x1700003A RID: 58
			// (get) Token: 0x060000A9 RID: 169 RVA: 0x000059E1 File Offset: 0x00003BE1
			// (set) Token: 0x060000AA RID: 170 RVA: 0x000059E9 File Offset: 0x00003BE9
			public long Usr_sum { get; set; }

			// Token: 0x1700003B RID: 59
			// (get) Token: 0x060000AB RID: 171 RVA: 0x000059F2 File Offset: 0x00003BF2
			// (set) Token: 0x060000AC RID: 172 RVA: 0x000059FA File Offset: 0x00003BFA
			public long Usr_count { get; set; }

			// Token: 0x1700003C RID: 60
			// (get) Token: 0x060000AD RID: 173 RVA: 0x00005A03 File Offset: 0x00003C03
			// (set) Token: 0x060000AE RID: 174 RVA: 0x00005A0B File Offset: 0x00003C0B
			public long Usr_ratio { get; set; }

			// Token: 0x1700003D RID: 61
			// (get) Token: 0x060000AF RID: 175 RVA: 0x00005A14 File Offset: 0x00003C14
			// (set) Token: 0x060000B0 RID: 176 RVA: 0x00005A1C File Offset: 0x00003C1C
			public long Residence { get; set; }

			// Token: 0x1700003E RID: 62
			// (get) Token: 0x060000B1 RID: 177 RVA: 0x00005A25 File Offset: 0x00003C25
			// (set) Token: 0x060000B2 RID: 178 RVA: 0x00005A2D File Offset: 0x00003C2D
			public long Residence1 { get; set; }

			// Token: 0x1700003F RID: 63
			// (get) Token: 0x060000B3 RID: 179 RVA: 0x00005A36 File Offset: 0x00003C36
			// (set) Token: 0x060000B4 RID: 180 RVA: 0x00005A3E File Offset: 0x00003C3E
			public Service1.NodeP Next { get; set; }
		}

		// Token: 0x0200000A RID: 10
		public struct PowerStatus
		{
			// Token: 0x04000243 RID: 579
			public byte ACLineStatus;

			// Token: 0x04000244 RID: 580
			public byte BatteryFlag;

			// Token: 0x04000245 RID: 581
			public byte BatteryLifePercent;

			// Token: 0x04000246 RID: 582
			public byte Reserved;

			// Token: 0x04000247 RID: 583
			public int BatteryLifeTime;

			// Token: 0x04000248 RID: 584
			public int BatteryFullLifeTime;
		}

		// Token: 0x0200000B RID: 11
		private enum ThreadAccess : uint
		{
			// Token: 0x0400024A RID: 586
			TERMINATE = 1U,
			// Token: 0x0400024B RID: 587
			SUSPEND_RESUME,
			// Token: 0x0400024C RID: 588
			GET_CONTEXT = 8U,
			// Token: 0x0400024D RID: 589
			SET_CONTEXT = 16U,
			// Token: 0x0400024E RID: 590
			SET_INFORMATION = 32U,
			// Token: 0x0400024F RID: 591
			QUERY_INFORMATION = 64U
		}
	}
}
