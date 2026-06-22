using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
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
	public class Service1 : ServiceBase
	{
		public class Node2
		{
			public int Id { get; set; }

			public long Value1 { get; set; }

			public int Value2 { get; set; }

			public Node2 Next { get; set; }

			public Node2()
			{
			}

			public Node2(int id, long value1, int value2)
			{
				Id = id;
				Value1 = value1;
				Value2 = value2;
				Next = null;
			}
		}

		public class Node
		{
			public int Id { get; set; }

			public int Value { get; set; }

			public Node Next { get; set; }

			public Node()
			{
			}

			public Node(int id, int value)
			{
				Id = id;
				Value = value;
				Next = null;
			}
		}

		public class Node1
		{
			public int Id { get; set; }

			public long Acc_instruction_b { get; set; }

			public long Acc_aclk_b { get; set; }

			public long Acc_load_b { get; set; }

			public long Acc_store_b { get; set; }

			public long Acc_load_miss_b { get; set; }

			public long Acc_br_b { get; set; }

			public long Acc_runtime_b { get; set; }

			public long Cnt_b { get; set; }

			public long Acc_instruction_l { get; set; }

			public long Acc_aclk_l { get; set; }

			public long Acc_load_l { get; set; }

			public long Acc_load_l_perm { get; set; }

			public long Last_duration { get; set; }

			public long Now_duration { get; set; }

			public long Acc_store_l { get; set; }

			public long Acc_store_l_perm { get; set; }

			public long Acc_load_miss_l { get; set; }

			public long Acc_br_l { get; set; }

			public long Acc_runtime_l { get; set; }

			public long Cnt_l { get; set; }

			public long Ipc_b { get; set; }

			public long Max_ipc_b { get; set; }

			public long Ipc_l { get; set; }

			public long Ipc_l_perm { get; set; }

			public long Max_ipc_l { get; set; }

			public long Ipc_ratio { get; set; }

			public long Br_ratio { get; set; }

			public long Br_load_ratio { get; set; }

			public long Load_miss_ratio_b { get; set; }

			public long Min_load_miss_ratio_b { get; set; }

			public long Load_miss_ratio_l { get; set; }

			public long Avg_runtime_b { get; set; }

			public long Avg_runtime_l { get; set; }

			public long Avg_freq_b { get; set; }

			public long Avg_freq_l { get; set; }

			public long Max_ins { get; set; }

			public long Lock_data { get; set; }

			public long Tag { get; set; }

			public long Duration { get; set; }

			public long Reset_count { get; set; }

			public uint Affinity { get; set; }

			public long Residence { get; set; }

			public Node1 Next { get; set; }

			public Node1()
			{
			}

			public Node1(int id, long acc_instruction_b, long acc_aclk_b, long acc_load_b, long acc_store_b, long acc_load_miss_b, long acc_br_b, long acc_runtime_b, long cnt_b, long acc_instruction_l, long acc_aclk_l, long acc_load_l, long acc_load_l_perm, long last_duration, long now_duration, long acc_store_l, long acc_store_l_perm, long acc_load_miss_l, long acc_br_l, long acc_runtime_l, long cnt_l, long ipc_b, long max_ipc_b, long ipc_l, long ipc_l_perm, long max_ipc_l, long ipc_ratio, long br_ratio, long br_load_ratio, long load_miss_ratio_b, long min_load_miss_ratio_b, long load_miss_ratio_l, long avg_runtime_b, long avg_runtime_l, long avg_freq_b, long avg_freq_l, long max_ins, long lock_data, long tag, long duration, long reset_count, uint affinity, long residence)
			{
				Id = id;
				Acc_instruction_b = acc_instruction_b;
				Acc_aclk_b = acc_aclk_b;
				Acc_load_b = acc_load_b;
				Acc_store_b = acc_store_b;
				Acc_load_miss_b = acc_load_miss_b;
				Acc_br_b = acc_br_b;
				Acc_runtime_b = acc_runtime_b;
				Cnt_b = cnt_b;
				Acc_instruction_l = acc_instruction_l;
				Acc_aclk_l = acc_aclk_l;
				Acc_load_l = acc_load_l;
				Acc_load_l_perm = acc_load_l_perm;
				Last_duration = last_duration;
				Now_duration = now_duration;
				Acc_store_l = acc_store_l;
				Acc_store_l_perm = acc_store_l_perm;
				Acc_load_miss_l = acc_load_miss_l;
				Acc_br_l = acc_br_l;
				Acc_runtime_l = acc_runtime_l;
				Cnt_l = cnt_l;
				Ipc_b = ipc_b;
				Max_ipc_b = max_ipc_b;
				Ipc_l = ipc_l;
				Ipc_l_perm = ipc_l_perm;
				Max_ipc_l = max_ipc_l;
				Ipc_ratio = ipc_ratio;
				Br_ratio = br_ratio;
				Br_load_ratio = br_load_ratio;
				Load_miss_ratio_b = load_miss_ratio_b;
				Min_load_miss_ratio_b = min_load_miss_ratio_b;
				Load_miss_ratio_l = load_miss_ratio_l;
				Avg_runtime_b = avg_runtime_b;
				Avg_runtime_l = avg_runtime_l;
				Avg_freq_b = avg_freq_b;
				Avg_freq_l = avg_freq_l;
				Max_ins = max_ins;
				Lock_data = lock_data;
				Tag = tag;
				Duration = duration;
				Reset_count = reset_count;
				Affinity = affinity;
				Residence = residence;
				Next = null;
			}
		}

		public class NodeP
		{
			public int PId { get; set; }

			public long Ins_total { get; set; }

			public long Store_total { get; set; }

			public long Count_total { get; set; }

			public long Intval { get; set; }

			public long Nonstore_store_ratio { get; set; }

			public long Usr_sum { get; set; }

			public long Usr_count { get; set; }

			public long Usr_ratio { get; set; }

			public long Residence { get; set; }

			public long Residence1 { get; set; }

			public NodeP Next { get; set; }

			public NodeP()
			{
			}

			public NodeP(int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1)
			{
				PId = pid;
				Ins_total = ins_total;
				Store_total = store_total;
				Count_total = count_total;
				Intval = intval;
				Nonstore_store_ratio = nonstore_store_ratio;
				Usr_sum = usr_sum;
				Usr_count = usr_count;
				Usr_ratio = usr_ratio;
				Residence = residence;
				Residence1 = residence1;
				Next = null;
			}
		}

		public struct PowerStatus
		{
			public byte ACLineStatus;

			public byte BatteryFlag;

			public byte BatteryLifePercent;

			public byte Reserved;

			public int BatteryLifeTime;

			public int BatteryFullLifeTime;
		}

		private enum ThreadAccess : uint
		{
			TERMINATE = 1u,
			SUSPEND_RESUME = 2u,
			GET_CONTEXT = 8u,
			SET_CONTEXT = 0x10u,
			SET_INFORMATION = 0x20u,
			QUERY_INFORMATION = 0x40u
		}

		private Node1 record = new Node1();

		private Node wait_queue = new Node();

		private Node1[] threadrecord = new Node1[10000];

		private NodeP[] processrecord = new NodeP[10000];

		private Node[] max_ipc_queue = new Node[32];

		private Node[] max_util_queue = new Node[32];

		private Node[] wait_core = new Node[32];

		private Node2[] sched_queue_b2l = new Node2[64];

		private Node2[] sched_queue_l2b = new Node2[64];

		private Node2 schedule_queue = new Node2();

		private Node2 schedule_queue_little = new Node2();

		private Node2 schd_queue_b2l = new Node2();

		private Node2 schd_queue_b2s = new Node2();

		private Node2 schd_queue_l2b = new Node2();

		private Node2 schd_queue_s2b = new Node2();

		public long[] lowerlimit = new long[32];

		public long[] upperlimit = new long[32];

		private Guid powerscheme1 = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

		private Guid powerscheme = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

		public int node_cap = 500;

		public long num_chain;

		public long num_chain_little;

		public long num_chain_big;

		public long num_chain2;

		public long action_recored;

		public long[] current_freq = new long[32];

		public uint affinitymask;

		public uint affinitymask_little;

		public uint affinitymask_big;

		public uint affinitymask_big_phyx;

		public uint affinitymask_fake_little;

		private string number_of_cores;

		private string NumberOfLogicalProcessors;

		public uint eax;

		public uint edx;

		public long[] tsc_e = new long[32];

		public long[] tsc_l = new long[32];

		public long[] tsc = new long[32];

		public long[] tsc_total = new long[32];

		public long[] result_ins_e = new long[32];

		public long[] result_ins_l = new long[32];

		public long[] result_ins = new long[32];

		public long[] result_ins_comp_e = new long[32];

		public long[] result_ins_comp_l = new long[32];

		public long[] result_ins_comp = new long[32];

		public long max_single_ratio_big;

		public long max_single_ratio_little;

		public long max_ins_little;

		public long max_ins_big;

		public long max_br_little;

		public long max_br_far_little;

		public long max_br_big;

		public long max_br_far_big;

		public long max_util_big = 50L;

		public long[] single_tag = new long[32];

		public long[] result_br_miss_e = new long[32];

		public long[] result_br_miss_l = new long[32];

		public long[] result_cache_e = new long[32];

		public long[] result_cache_l = new long[32];

		public long[] result_cache = new long[32];

		public long[] result_load_e = new long[32];

		public long[] result_load_l = new long[32];

		public long[] result_load = new long[32];

		public long[] result_store_e = new long[32];

		public long[] result_store_l = new long[32];

		public long[] result_store = new long[32];

		public long[] result_load_l1_e = new long[32];

		public long[] result_load_l1_l = new long[32];

		public long[] result_load_l1 = new long[32];

		public long[] result_br_ins_e = new long[32];

		public long[] result_br_ins_l = new long[32];

		public long[] result_br_ins = new long[32];

		public long[] result_br_indirect_e = new long[32];

		public long[] result_br_indirect_l = new long[32];

		public long[] result_br_indirect = new long[32];

		public long[] br_indirect = new long[32];

		public long[] result_br_far_e = new long[32];

		public long[] result_br_far_l = new long[32];

		public long[] result_br_far = new long[32];

		public long[] br_far = new long[32];

		public long[] result_aclk_e = new long[32];

		public long[] result_aclk_l = new long[32];

		public long[] result_aclk = new long[32];

		public long[] result_mclk_e = new long[32];

		public long[] result_mclk_l = new long[32];

		public long[] result_mclk = new long[32];

		public long[] result_pclk_e = new long[32];

		public long[] result_pclk_l = new long[32];

		public long[] result_pclk = new long[32];

		private Ols myOls = new Ols();

		public long ipc_switch;

		public long active_cores;

		public long[] core_active = new long[32];

		public long active_big_cores;

		public long active_smt_cores;

		public long active_little_cores;

		public long[] single_ratio = new long[32];

		public long[] ht_share = new long[32];

		public long[] br_far_ratio = new long[32];

		public long[] br = new long[32];

		public long[] br_miss = new long[32];

		public long[] cache = new long[32];

		public long[] mem = new long[32];

		public long[] load = new long[32];

		public long[] load_l1 = new long[32];

		public long[] load_l2 = new long[32];

		public long[] load_l3 = new long[32];

		public long[] load_dram = new long[32];

		public long[] cache2mem = new long[32];

		public long[] ins = new long[32];

		public long util_big;

		public long ins_all;

		public long ins_all_avg;

		public long ins_all_whole;

		public long ins_all_whole_sqr;

		public long ins_all_whole_avg;

		public long perf_whole;

		public long perf_whole_old;

		public long perf_whole_avg;

		public long ins_avg;

		public long ins_sqr;

		public long ins_indicator;

		public long ins_big;

		public long ins_constr_smt;

		public long ins_little;

		public long ins_little_comp;

		public long ins_max_comp;

		public long ins_max_load;

		public long ins_max_br;

		public long ins_max;

		public long util_little_all;

		public long aclk_acc;

		public long ins_smt;

		public long ins_little_raw;

		public long ins_big_raw;

		public long ins_smt_raw;

		public long max_ipc;

		private ulong little_num;

		private ulong big_num;

		private ulong core_num;

		private long threshold;

		private long[] datetime_new = new long[32];

		private long[] datetime_old = new long[32];

		private long[] datetime_elapsed = new long[32];

		private long datetime_trigger;

		private long datetime_trigger_little;

		private long datetime_trigger_exchange;

		private long avg_ipc_trigger = 1L;

		private int e_core_position;

		private int currentprocnum_index;

		private long[] count_stat_little = new long[32];

		private long count_stat;

		private long count_stat1;

		private long count_stat2;

		private long count_stat3;

		private long count_stat4;

		private long count_stat5;

		private long count_stat6;

		private long counter_action;

		private long counter_action_switch;

		private long[] acc_instruction = new long[32];

		private long[] acc_aclk = new long[32];

		private long[] acc_instruction_comp = new long[32];

		private long[] acc_load = new long[32];

		private long[] acc_datetime = new long[32];

		private long[] acc_instruction1 = new long[32];

		private long[] acc_aclk1 = new long[32];

		private long[] acc_instruction_comp1 = new long[32];

		private long[] acc_load1 = new long[32];

		private long[] acc_datetime1 = new long[32];

		private long[] util = new long[32];

		private long cnt_findnode;

		private long cnt_not_findnode;

		private int switch_to_big;

		private int switch_to_big_cnt;

		private int[] oldthread_waittime = new int[32];

		private int[] schedule_thread = new int[32];

		private int[] max_ipc_thread = new int[32];

		private int[] max_util_thread = new int[32];

		private int[] max_util_little = new int[32];

		private int num_queue = 1;

		private long dummy;

		private int currentthread;

		private int currentprocess;

		private int counter1;

		private int counter2;

		private int counter3;

		private int[] findrecord = new int[32];

		private UIntPtr j;

		private uint mask;

		private uint valueToSet;

		private long acc_util;

		private uint ratio;

		private string ratio_string;

		private uint ratio1;

		private string ratio_string1;

		private long ipc_big_sum;

		private long ipc_little_sum;

		private long ipc_big_count;

		private long ipc_little_count;

		private long ipc_big_avg;

		private long ipc_little_avg;

		private long eff_big_sum;

		private long eff_little_sum;

		private long eff_big_count;

		private long eff_little_count;

		private long eff_big_avg;

		private long eff_little_avg;

		private long[] ins_total = new long[32];

		private long[] store_total = new long[32];

		private long[] count_total = new long[32];

		private long[] intval = new long[32];

		private long[] nonstore_store_ratio = new long[32];

		private long[] usr_sum = new long[32];

		private long[] usr_count = new long[32];

		private long[] usr_ratio = new long[32];

		private long[] residence_p = new long[32];

		private long[] residence_p1 = new long[32];

		private long[] acc_instruction_b = new long[32];

		private long[] acc_aclk_b = new long[32];

		private long[] acc_load_b = new long[32];

		private long[] acc_store_b = new long[32];

		private long[] acc_load_miss_b = new long[32];

		private long[] acc_br_b = new long[32];

		private long[] acc_runtime_b = new long[32];

		private long[] cnt_b = new long[32];

		private long[] acc_instruction_l = new long[32];

		private long[] acc_aclk_l = new long[32];

		private long[] acc_load_l = new long[32];

		private long[] acc_load_l_perm = new long[32];

		private long[] last_duration = new long[32];

		private long[] now_duration = new long[32];

		private long[] acc_store_l = new long[32];

		private long[] acc_store_l_perm = new long[32];

		private long[] acc_load_miss_l = new long[32];

		private long[] acc_br_l = new long[32];

		private long[] acc_runtime_l = new long[32];

		private long[] cnt_l = new long[32];

		private long[] ipc_b = new long[32];

		private long[] ipc_b_temp = new long[32];

		private long[] max_ipc_b = new long[32];

		private long[] max_ins = new long[32];

		private long[] ipc_l = new long[32];

		private long[] ipc_l_perm = new long[32];

		private long[] max_ipc_l = new long[32];

		private long max_ipc_little;

		private long max_ipc_big;

		private long[] ipc_ratio = new long[32];

		private long[] br_ratio = new long[32];

		private long[] ipc_ratio_temp = new long[32];

		private long[] br_ratio_temp = new long[32];

		private long br_ratio_square;

		private long br_ratio_square_bar;

		private long br_ratio_square_e;

		private long br_ratio_square_count;

		private long ipc_square;

		private long ipc_square_bar;

		private long ipc_square_e;

		private long ipc_square_count;

		private long[] br_load_ratio = new long[32];

		private long[] br_load_ratio_temp = new long[32];

		private long[] load_miss_ratio_b = new long[32];

		private long[] load_miss_ratio_b_temp = new long[32];

		private long[] min_load_miss_ratio_b = new long[32];

		private long[] load_miss_ratio_l = new long[32];

		private long[] avg_runtime_b = new long[32];

		private long[] avg_runtime_l = new long[32];

		private long[] avg_freq_b = new long[32];

		private long[] max_freq_b = new long[32];

		private long[] avg_freq_l = new long[32];

		private long[] lock_data = new long[32];

		private long[] tag = new long[32];

		private long[] duration = new long[32];

		private long[] reset_count = new long[32];

		private uint[] affinity = new uint[32];

		private long[] residence = new long[32];

		private long[] acc_instruction_b1 = new long[32];

		private long[] acc_aclk_b1 = new long[32];

		private long[] acc_load_b1 = new long[32];

		private long[] acc_store_b1 = new long[32];

		private long[] acc_load_miss_b1 = new long[32];

		private long[] acc_br_b1 = new long[32];

		private long[] acc_runtime_b1 = new long[32];

		private long[] cnt_b1 = new long[32];

		private long[] acc_instruction_l1 = new long[32];

		private long[] acc_aclk_l1 = new long[32];

		private long[] acc_load_l1 = new long[32];

		private long[] acc_load_l1_perm = new long[32];

		private long[] last_duration1 = new long[32];

		private long[] now_duration1 = new long[32];

		private long[] acc_store_l1 = new long[32];

		private long[] acc_store_l1_perm = new long[32];

		private long[] acc_load_miss_l1 = new long[32];

		private long[] acc_br_l1 = new long[32];

		private long[] acc_runtime_l1 = new long[32];

		private long[] cnt_l1 = new long[32];

		private long[] ipc_b1 = new long[32];

		private long[] max_ipc_b1 = new long[32];

		private long[] max_ins1 = new long[32];

		private long[] ipc_l1 = new long[32];

		private long[] ipc_l1_perm = new long[32];

		private long[] max_ipc_l1 = new long[32];

		private long[] ipc_ratio1 = new long[32];

		private long[] br_ratio1 = new long[32];

		private long[] br_load_ratio1 = new long[32];

		private long[] load_miss_ratio_b1 = new long[32];

		private long acc_instruction_b1_t;

		private long acc_aclk_b1_t;

		private long acc_load_b1_t;

		private long acc_store_b1_t;

		private long acc_load_miss_b1_t;

		private long acc_br_b1_t;

		private long acc_runtime_b1_t;

		private long cnt_b1_t;

		private long acc_instruction_l1_t;

		private long acc_aclk_l1_t;

		private long acc_load_l1_t;

		private long acc_load_l1_perm_t;

		private long acc_store_l1_t;

		private long acc_store_l1_perm_t;

		private long acc_load_miss_l1_t;

		private long acc_br_l1_t;

		private long acc_runtime_l1_t;

		private long cnt_l1_t;

		private long ipc_b1_t;

		private long max_ipc_b1_t;

		private long ipc_l1_t;

		private long ipc_l1_perm_t;

		private long max_ipc_l1_t;

		private long ipc_ratio1_t;

		private long br_ratio1_t;

		private long br_load_ratio1_t;

		private long load_miss_ratio_b1_t;

		private long min_load_miss_ratio_b1_t;

		private long load_miss_ratio_l1_t;

		private long avg_runtime_b1_t;

		private long avg_runtime_l1_t;

		private long avg_freq_b1_t;

		private long avg_freq_l1_t;

		private long max_ins_t;

		private long lock_data1_t;

		private long tag1_t;

		private long duration1_t;

		private long reset_count1_t;

		private uint affinity1_t;

		private long[] temp1 = new long[32];

		private long[] temp2 = new long[32];

		private long[] temp3 = new long[32];

		private long[] temp4 = new long[32];

		private long[] temp41 = new long[32];

		private long[] temp5 = new long[32];

		private long[] temp51 = new long[32];

		private long[] temp6 = new long[32];

		private long[] temp_ticks = new long[32];

		private long tempp;

		private long tempk;

		private long[] sched_ratio = new long[32];

		private long[] ins_ratio = new long[32];

		private long avg_comp_ldst_ratio;

		private long avg_comp_ldst_sum;

		private long avg_comp_ldst_count;

		private long avg_comp_br_ratio;

		private long avg_comp_br_sum;

		private long avg_comp_br_count;

		private long avg_ipc_ratio_sum;

		private long avg_ipc_ratio_count;

		private long avg_ipc_ratio;

		private long[] min_load_miss_ratio_b1 = new long[32];

		private long[] load_miss_ratio_l1 = new long[32];

		private long[] avg_runtime_b1 = new long[32];

		private long[] avg_runtime_l1 = new long[32];

		private long[] avg_freq_b1 = new long[32];

		private long[] avg_freq_l1 = new long[32];

		private long[] lock_data1 = new long[32];

		private long[] tag1 = new long[32];

		private long[] duration1 = new long[32];

		private long[] reset_count1 = new long[32];

		private uint[] affinity1 = new uint[32];

		private long[] residence1 = new long[32];

		private long[] prev_tag = new long[32];

		private uint[] prev_affinity = new uint[32];

		private long count_fast_ipc;

		private long count_fast_br;

		private long count_fast_comp;

		private long count_slow;

		private long count_heavy;

		private long _6_to_2;

		private long _6_to_1;

		private long _2_to_6;

		private long _6_to_8;

		private long _8_to_6;

		private long count_threads;

		private long count_stay_big;

		private int config;

		private int gamemode;

		private long[] core_availability_cnt = new long[32];

		private long[] test_ratio = new long[32];

		private long[] value = new long[32];

		private long max_freq;

		private long[] exclude_b = new long[32];

		private long[] exclude = new long[32];

		private long[] exclude_all = new long[32];

		private long[] allow_exclude = new long[32];

		private long[] exclude1 = new long[32];

		private long[] exclude_all1 = new long[32];

		private long[] allow_exclude1 = new long[32];

		private long avg_ipc;

		private long acc_ins;

		private long acc_loads;

		private long acc_loads_e;

		private long acc_loads_miss;

		private long acc_loads_miss_e;

		private long acc_brs;

		private long acc_brs_e;

		private long acc_brs_miss;

		private long acc_brs_miss_e;

		private long acc_aclks;

		private long acc_ins_e;

		private long acc_aclks1_e;

		private long acc_aclks_e;

		private long avg_diff;

		private long acc_aclks1;

		private long acc_date;

		private long start;

		private long numberofchain;

		private long acc_ins_b;

		private long acc_ins_l;

		private long acc_ack_b;

		private long acc_ack_l;

		private long avg_ipc_b;

		private long avg_ipc_l;

		private long avg_ipc_ratio_bak;

		private long acc_br_all;

		private long acc_cond_br_all;

		private long avg_cond_br_ratio;

		private long min_cond_br_ratio = 100L;

		private long max_cond_br_ratio;

		private long[] count_intval = new long[32];

		private long count_intval_all;

		private long count_intval_avg;

		private long max_ipc_global;

		private int[] currentprocessor = new int[32];

		private long total_aclks;

		private long total_ins;

		private long total_ins1;

		private long total_aclks1;

		private uint eeax;

		private uint eebx;

		private uint eecx;

		private uint eedx;

		private uint e_msr;

		private uint l_msr;

		private uint max_msr;

		private IContainer components;

		public int UpdateNodeP(int node_cap, ref NodeP node, int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1)
		{
			int num = 0;
			NodeP nodeP = node;
			NodeP nodeP2 = null;
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
				_ = nodeP.Next;
				nodeP2 = nodeP;
				nodeP = nodeP.Next;
				num++;
			}
			NodeP nodeP3 = new NodeP(pid, ins_total, store_total, count_total, intval, nonstore_store_ratio, usr_sum, usr_count, usr_ratio, residence, residence1);
			nodeP3.Next = node;
			node = nodeP3;
			num++;
			return 0;
		}

		public int FindNodeValueP(ref NodeP node, int pid, ref long ins_total, ref long store_total, ref long count_total, ref long intval, ref long nonstore_store_ratio, ref long usr_sum, ref long usr_count, ref long usr_ratio, ref long residence, ref long residence1)
		{
			for (NodeP nodeP = node; nodeP != null; nodeP = nodeP.Next)
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

		public int UpdateNode(int node_cap, ref Node node, int id, int value)
		{
			int num = 0;
			Node node2 = node;
			Node node3 = null;
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
				_ = node2.Next;
				node3 = node2;
				node2 = node2.Next;
				num++;
			}
			Node node4 = new Node(id, value);
			node4.Next = node;
			node = node4;
			num++;
			return 0;
		}

		public int UpdateNode2(ref Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			Node2 node4 = new Node2(id, value1, 0);
			node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (value1 <= node2.Value1)
				{
					if (node3 == null)
					{
						if (node2.Id == 0)
						{
							node = node4;
							reset_count = 1L;
						}
						else
						{
							node4.Next = node2;
							node = node4;
							reset_count = 1L;
						}
					}
					else
					{
						node4.Next = node2;
						node3.Next = node4;
						reset_count = 1L;
					}
					break;
				}
				if (node2.Id == 0)
				{
					node = node4;
					reset_count = 1L;
					return 2;
				}
				if (node2.Next != null)
				{
					node3 = node2;
					node2 = node2.Next;
					continue;
				}
				node2.Next = node4;
				node4.Next = null;
				reset_count = 1L;
				break;
			}
			return 0;
		}

		public int UpdateNode2_little(ref Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			Node2 node4 = new Node2(id, value1, 0);
			node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (value1 >= node2.Value1)
				{
					if (node3 == null)
					{
						if (node2.Id == 0)
						{
							node = node4;
							reset_count = 1L;
						}
						else
						{
							node4.Next = node2;
							node = node4;
							reset_count = 1L;
						}
					}
					else
					{
						node4.Next = node2;
						node3.Next = node4;
						reset_count = 1L;
					}
					break;
				}
				if (node2.Next != null)
				{
					node3 = node2;
					node2 = node2.Next;
					continue;
				}
				node2.Next = node4;
				node4.Next = null;
				reset_count = 1L;
				break;
			}
			return 0;
		}

		public int UpdateNode1(int node_cap, ref Node1 node, int id, long acc_instruction_b, long acc_aclk_b, long acc_load_b, long acc_store_b, long acc_load_miss_b, long acc_br_b, long acc_runtime_b, long cnt_b, long acc_instruction_l, long acc_aclk_l, long acc_load_l, long acc_load_l_perm, long last_duration, long now_duration, long acc_store_l, long acc_store_l_perm, long acc_load_miss_l, long acc_br_l, long acc_runtime_l, long cnt_l, long ipc_b, long max_ipc_b, long ipc_l, long ipc_l_perm, long max_ipc_l, long ipc_ratio, long br_ratio, long br_load_ratio, long load_miss_ratio_b, long min_load_miss_ratio_b, long load_miss_ratio_l, long avg_runtime_b, long avg_runtime_l, long avg_freq_b, long avg_freq_l, long max_ins, long lock_data, long tag, long duration, long reset_count, uint affinity, long residence)
		{
			int num = 0;
			Node1 node2 = node;
			Node1 node3 = null;
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
				_ = node2.Next;
				node3 = node2;
				node2 = node2.Next;
				num++;
			}
			Node1 node4 = new Node1(id, acc_instruction_b, acc_aclk_b, acc_load_b, acc_store_b, acc_load_miss_b, acc_br_b, acc_runtime_b, cnt_b, acc_instruction_l, acc_aclk_l, acc_load_l, acc_load_l_perm, last_duration, now_duration, acc_store_l, acc_store_l_perm, acc_load_miss_l, acc_br_l, acc_runtime_l, cnt_l, ipc_b, max_ipc_b, ipc_l, ipc_l_perm, max_ipc_l, ipc_ratio, br_ratio, br_load_ratio, load_miss_ratio_b, min_load_miss_ratio_b, load_miss_ratio_l, avg_runtime_b, avg_runtime_l, avg_freq_b, avg_freq_l, max_ins, lock_data, tag, duration, reset_count, affinity, residence);
			node4.Next = node;
			node = node4;
			num++;
			return 0;
		}

		public int DeleteNode(ref Node2 node, int id)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			for (int i = 0; i < 500; i++)
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
						node2 = null;
						return 1;
					}
					node3.Next = node2.Next;
					node2 = null;
					return 1;
				}
				if (node2.Next != null)
				{
					node3 = node2;
					node2 = node2.Next;
					continue;
				}
				return 0;
			}
			return 0;
		}

		public int GetNodeValue(ref Node2 node, ref long value)
		{
			Node2 node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2 != null)
				{
					if (node2.Id != 0)
					{
						value = node2.Value1;
						return 1;
					}
					node2 = node2.Next;
					continue;
				}
				return -1;
			}
			return -1;
		}

		public int FindNodeValue2(ref Node2 node, ref long value)
		{
			Node2 node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2 != null)
				{
					if (node2.Id != 0)
					{
						try
						{
							IntPtr intPtr = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)node2.Id);
							if (intPtr != IntPtr.Zero)
							{
								CloseHandle(intPtr);
								value = node2.Value1;
								return node2.Id;
							}
							DeleteNode(ref node, node2.Id);
							node2 = node2.Next;
						}
						catch
						{
							DeleteNode(ref node, node2.Id);
							node2 = node2.Next;
						}
					}
					else
					{
						node2 = node2.Next;
					}
					continue;
				}
				return -1;
			}
			return -1;
		}

		public int FindNodeValue(Node node, int id)
		{
			for (Node node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					return node2.Value;
				}
			}
			return -1;
		}

		public int FindMaxIpc(Node node, ref int max_ipc_thread, ref int max_ipc_little)
		{
			for (Node node2 = node; node2 != null; node2 = node2.Next)
			{
				max_ipc_thread = node2.Id;
				max_ipc_little = node2.Value;
			}
			return -1;
		}

		public int FindNodeValue1(ref Node1 node, int id, ref long acc_instruction_b, ref long acc_aclk_b, ref long acc_load_b, ref long acc_store_b, ref long acc_load_miss_b, ref long acc_br_b, ref long acc_runtime_b, ref long cnt_b, ref long acc_instruction_l, ref long acc_aclk_l, ref long acc_load_l, ref long acc_load_l_perm, ref long last_duration, ref long now_duration, ref long acc_store_l, ref long acc_store_l_perm, ref long acc_load_miss_l, ref long acc_br_l, ref long acc_runtime_l, ref long cnt_l, ref long ipc_b, ref long max_ipc_b, ref long ipc_l, ref long ipc_l_perm, ref long max_ipc_l, ref long ipc_ratio, ref long br_ratio, ref long br_load_ratio, ref long load_miss_ratio_b, ref long min_load_miss_ratio_b, ref long load_miss_ratio_l, ref long avg_runtime_b, ref long avg_runtime_l, ref long avg_freq_b, ref long avg_freq_l, ref long max_ins, ref long lock_data, ref long tag, ref long duration, ref long reset_count, ref uint affinity)
		{
			for (Node1 node2 = node; node2 != null; node2 = node2.Next)
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

		public long GetFactor(long usr_ratio, long avg_usr_ratio)
		{
			long num = 50 * usr_ratio / avg_usr_ratio;
			if (num > 100)
			{
				return 100L;
			}
			return num;
		}

		public int Intval2Limit(int oldthread, long intval, long utility, long nonstore_store_ratio, ref long usr_ratio_avg, ref long ins_big, int currentprocessor, long usr_ratio, ref long max_ins, ref long usr_ratio1, long br_sys, ref long tag, uint affinity, ref long reset_count, ref long usr_ratio_little, ref long prod_cons_ratio, ref long ins_little, ref long lock_data, ref long residence_p1, ref long residence_p)
		{
			int num = 1;
			if (usr_ratio > 0)
			{
				tempp += usr_ratio;
				tempk++;
			}
			if ((int)((uint)(1 << currentprocessor) & affinitymask_little) > 0)
			{
				if (ins_little > 100000)
				{
					residence_p <<= 1;
				}
				else if (ins_little > 0)
				{
					residence_p = (residence_p << 1) | 1;
				}
				if (tag == 2 && (((nonstore_store_ratio > 0) & (prod_cons_ratio * 100 > nonstore_store_ratio * 100)) | (ins_little > 500000)))
				{
					_6_to_8++;
					goto IL_0141;
				}
			}
			if ((int)((uint)(1 << currentprocessor) & affinitymask_big) > 0)
			{
				if (tag == 6)
				{
					if ((((usr_ratio <= 0) | (usr_ratio_avg <= 0)) || br_sys <= 0 || nonstore_store_ratio <= 0) | (ins_big <= 0))
					{
						num = 1;
					}
					else
					{
						if (ins_big > 100000)
						{
							residence_p <<= 1;
						}
						else if (ins_big > 0)
						{
							residence_p = (residence_p << 1) | 1;
						}
						num = (((br_sys * 100 > nonstore_store_ratio * 100) | (ins_big > 500000)) ? 1 : 2);
					}
				}
			}
			else
			{
				num = 0;
			}
			goto IL_0141;
			IL_0141:
			switch (num)
			{
			case 1:
				if (((int)((uint)(1 << currentprocessor) & affinitymask_little) > 0) & (tag == 2))
				{
					if (reset_count == 0L)
					{
						UpdateNode2_little(ref schd_queue_l2b, oldthread, utility * intval, 0, ref reset_count);
						return 1;
					}
					break;
				}
				lock_data = DateTime.Now.Ticks;
				return 0;
			case 2:
				if (((int)((uint)(1 << currentprocessor) & affinitymask_big) > 0) & (tag == 6))
				{
					if (reset_count == 0L)
					{
						UpdateNode2(ref schd_queue_b2l, oldthread, usr_ratio, 0, ref reset_count);
						return 2;
					}
					break;
				}
				lock_data = DateTime.Now.Ticks;
				return 0;
			default:
				return 0;
			}
			return 0;
		}

		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern bool PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid SchemeGuid);

		[DllImport("kernel32.dll")]
		public static extern bool GetSystemPowerStatus(out PowerStatus BatteryInfo);

		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadACValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadDCValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32.dll")]
		private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetThreadAffinityMask(IntPtr hThread, out uint mask);

		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentThreadId();

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentProcessId();

		public Service1()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			int[] array = new int[32];
			_ = new int[32];
			int[] takeaction = new int[32];
			long[] array2 = new long[32];
			_ = new long[32];
			long[] array3 = new long[32];
			_ = new int[32];
			int[] findresult = new int[32];
			int[] findresultp = new int[32];
			for (int i = 0; i < Convert.ToUInt32(NumberOfLogicalProcessors); i++)
			{
				array[i] = 0;
				takeaction[i] = 0;
				array2[i] = 0L;
				array3[i] = 0L;
			}
			currentthread = (int)GetCurrentProcessId();
			try
			{
				foreach (ManagementObject item in new ManagementObjectSearcher("select * from win32_processor").Get())
				{
					number_of_cores = item.GetPropertyValue("numberofcores").ToString().Trim();
					NumberOfLogicalProcessors = item.GetPropertyValue("NumberOfLogicalProcessors").ToString().Trim();
				}
			}
			catch
			{
				return;
			}
			for (int j = 0; j < Convert.ToInt32(NumberOfLogicalProcessors); j++)
			{
				if ((int)((uint)(1 << j) & affinitymask_little) > 0)
				{
					UIntPtr threadAffinityMask = (UIntPtr)(ulong)Math.Pow(2.0, j);
					myOls.WrmsrTx(390u, 4390926u, 0u, threadAffinityMask);
					myOls.WrmsrTx(391u, 4391106u, 0u, threadAffinityMask);
					myOls.WrmsrTx(392u, 4423364u, 0u, threadAffinityMask);
					myOls.WrmsrTx(393u, 4390972u, 0u, threadAffinityMask);
					myOls.WrmsrTx(394u, 4260032u, 0u, threadAffinityMask);
					myOls.WrmsrTx(395u, 4391104u, 0u, threadAffinityMask);
					myOls.WrmsrTx(396u, 4390972u, 0u, threadAffinityMask);
				}
				else
				{
					UIntPtr threadAffinityMask2 = (UIntPtr)(ulong)Math.Pow(2.0, j);
					myOls.WrmsrTx(390u, 4391342u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(391u, 4391618u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(392u, 4391342u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(393u, 4391104u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(394u, 4260032u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(395u, 4390972u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(396u, 4390972u, 0u, threadAffinityMask2);
				}
			}
			for (int k = 0; k < Convert.ToInt32(NumberOfLogicalProcessors); k++)
			{
				sched_queue_l2b[k] = new Node2(0, 0L, 0);
				sched_queue_b2l[k] = new Node2(0, 0L, 0);
			}
			for (int l = 0; l < Convert.ToInt32(NumberOfLogicalProcessors); l++)
			{
				tag[l] = 0L;
				oldthread_waittime[l] = 0;
				core_availability_cnt[l] = 1L;
				affinitymask_big |= (uint)(1 << l);
				myOls.CpuidTx(26u, ref l_msr, ref eebx, ref eecx, ref eedx, (UIntPtr)(ulong)Math.Pow(2.0, l));
				if (l_msr > max_msr)
				{
					max_msr = l_msr;
				}
			}
			for (int m = 0; m < Convert.ToInt32(NumberOfLogicalProcessors); m++)
			{
				myOls.CpuidTx(26u, ref l_msr, ref eebx, ref eecx, ref eedx, (UIntPtr)(ulong)Math.Pow(2.0, m));
				if (l_msr < max_msr)
				{
					affinitymask_little |= (uint)(1 << m);
					little_num++;
				}
			}
			affinitymask_big &= ~affinitymask_little;
			if (Convert.ToUInt64(number_of_cores) == Convert.ToUInt64(NumberOfLogicalProcessors))
			{
				affinitymask_fake_little = affinitymask_little;
			}
			else
			{
				core_num = 2 * (Convert.ToUInt64(number_of_cores) - little_num);
				for (int n = 1; n < (int)core_num; n += 2)
				{
					affinitymask_fake_little |= (uint)(1 << n);
				}
				affinitymask_fake_little |= affinitymask_little;
			}
			if (little_num >= 8)
			{
				threshold = 1000000L;
			}
			else
			{
				threshold = 500000L;
			}
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
			for (int num = 0; num < Convert.ToInt32(NumberOfLogicalProcessors); num++)
			{
				UpdateNode(1, ref wait_core[num], num, 0);
				UpdateNode(1, ref max_ipc_queue[num], -1, -1);
				UpdateNode(1, ref max_util_queue[num], -1, -1);
				exclude[num] = 0L;
				exclude_b[num] = 0L;
				exclude_all[num] = 0L;
				last_duration[num] = 0L;
				now_duration[num] = 0L;
				avg_runtime_b[num] = 0L;
				avg_runtime_l[num] = 0L;
				max_ipc_l[num] = 0L;
				max_ipc_b[num] = 0L;
				temp4[num] = 0L;
				temp5[num] = 0L;
				temp6[num] = 0L;
			}
			ratio = (uint)(100 * little_num / Convert.ToUInt64(NumberOfLogicalProcessors));
			ratio_string = ratio.ToString();
			ratio1 = (uint)(Convert.ToUInt64(number_of_cores) * 100 / Convert.ToUInt64(NumberOfLogicalProcessors));
			ratio_string1 = ratio1.ToString();
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("ProvAcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("ProvDcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("AcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("DcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("ProvAcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("ProvDcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("AcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("DcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("ProvAcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("ProvDcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("AcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("DcSettingIndex", ratio_string1, RegistryValueKind.DWord);
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
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel\\KGroups\\00").SetValue("SmallProcessorMask", affinitymask_fake_little.ToString(), RegistryValueKind.DWord);
			}
			catch
			{
			}
			PowerSetActiveScheme(IntPtr.Zero, ref powerscheme1);
			Thread.Sleep(2000);
			PowerSetActiveScheme(IntPtr.Zero, ref powerscheme);
			schedule_queue.Id = 0;
			schedule_queue.Value1 = 0L;
			schedule_queue.Value2 = 0;
			schedule_queue.Next = null;
			schedule_queue_little.Id = 0;
			schedule_queue_little.Value1 = 0L;
			schedule_queue_little.Value2 = 0;
			schedule_queue_little.Next = null;
			schd_queue_b2l.Id = 0;
			schd_queue_b2l.Value1 = 0L;
			schd_queue_b2l.Value2 = 0;
			schd_queue_b2l.Next = null;
			schd_queue_b2s.Id = 0;
			schd_queue_b2s.Value1 = 0L;
			schd_queue_b2s.Value2 = 0;
			schd_queue_b2s.Next = null;
			schd_queue_l2b.Id = 0;
			schd_queue_l2b.Value1 = 0L;
			schd_queue_l2b.Value2 = 0;
			schd_queue_l2b.Next = null;
			schd_queue_s2b.Id = 0;
			schd_queue_s2b.Value1 = 0L;
			schd_queue_s2b.Value2 = 0;
			schd_queue_s2b.Next = null;
			new Thread(thread).Start();
			new Thread(thread2).Start();
			void thread()
			{
				using TraceEventSession traceEventSession = new TraceEventSession("ThreadSwitchSession");
				traceEventSession.EnableKernelProvider(KernelTraceEventParser.Keywords.ContextSwitch);
				traceEventSession.Source.Kernel.ThreadCSwitch += delegate(CSwitchTraceData data)
				{
					long num2 = 0L;
					int num3 = 0;
					uint num4 = 0u;
					uint num5 = 0u;
					int num6 = 0;
					int num7 = 0;
					int num8 = 0;
					num6 = data.OldThreadID;
					num8 = data.NewThreadID;
					num7 = data.OldProcessID;
					num3 = data.ProcessorNumber;
					currentprocessor[num3] = num3;
					exclude_all[num3] = 0L;
					allow_exclude[num3] = 0L;
					UIntPtr threadAffinityMask3 = (UIntPtr)(ulong)Math.Pow(2.0, num3);
					currentprocnum_index = num3 / 2;
					datetime_new[num3] = DateTime.Now.Ticks;
					datetime_elapsed[num3] = (datetime_new[num3] - datetime_old[num3]) / 10;
					datetime_old[num3] = datetime_new[num3];
					acc_date += datetime_elapsed[num3];
					long num9 = Math.Abs(num6 % 10000);
					long num10 = Math.Abs(num7 % 10000);
					if (!(currentthread == num7 || num8 == num6 || num7 == 0))
					{
						findresult[num3] = FindNodeValue1(ref threadrecord[num9], num6, ref acc_instruction_b[num3], ref acc_aclk_b[num3], ref acc_load_b[num3], ref acc_store_b[num3], ref acc_load_miss_b[num3], ref acc_br_b[num3], ref acc_runtime_b[num3], ref cnt_b[num3], ref acc_instruction_l[num3], ref acc_aclk_l[num3], ref acc_load_l[num3], ref acc_load_l_perm[num3], ref last_duration[num3], ref now_duration[num3], ref acc_store_l[num3], ref acc_store_l_perm[num3], ref acc_load_miss_l[num3], ref acc_br_l[num3], ref acc_runtime_l[num3], ref cnt_l[num3], ref ipc_b[num3], ref max_ipc_b[num3], ref ipc_l[num3], ref ipc_l_perm[num3], ref max_ipc_l[num3], ref ipc_ratio[num3], ref br_ratio[num3], ref br_load_ratio[num3], ref load_miss_ratio_b[num3], ref min_load_miss_ratio_b[num3], ref load_miss_ratio_l[num3], ref avg_runtime_b[num3], ref avg_runtime_l[num3], ref avg_freq_b[num3], ref avg_freq_l[num3], ref max_ins[num3], ref lock_data[num3], ref tag[num3], ref duration[num3], ref reset_count[num3], ref affinity[num3]);
						cnt_findnode++;
						if (findresult[num3] == 0)
						{
							cnt_not_findnode++;
							count_threads++;
							acc_instruction_b[num3] = 0L;
							acc_aclk_b[num3] = 0L;
							acc_load_b[num3] = 0L;
							acc_store_b[num3] = 0L;
							acc_load_miss_b[num3] = 0L;
							acc_br_b[num3] = 0L;
							acc_runtime_b[num3] = 0L;
							cnt_b[num3] = DateTime.Now.Ticks;
							acc_instruction_l[num3] = 0L;
							acc_aclk_l[num3] = 0L;
							acc_load_l[num3] = 0L;
							acc_load_l_perm[num3] = 0L;
							last_duration[num3] = 0L;
							now_duration[num3] = 0L;
							acc_store_l[num3] = 0L;
							acc_store_l_perm[num3] = 0L;
							acc_load_miss_l[num3] = 0L;
							acc_br_l[num3] = 0L;
							acc_runtime_l[num3] = 0L;
							cnt_l[num3] = 0L;
							ipc_b[num3] = 0L;
							max_ipc_b[num3] = 0L;
							ipc_l[num3] = 0L;
							ipc_l_perm[num3] = 0L;
							max_ipc_l[num3] = 0L;
							ipc_ratio[num3] = 0L;
							br_ratio[num3] = 0L;
							br_load_ratio[num3] = 0L;
							load_miss_ratio_b[num3] = 0L;
							min_load_miss_ratio_b[num3] = 0L;
							load_miss_ratio_l[num3] = 0L;
							avg_runtime_b[num3] = 0L;
							avg_runtime_l[num3] = 0L;
							avg_freq_b[num3] = 0L;
							avg_freq_l[num3] = 0L;
							max_ins[num3] = 0L;
							lock_data[num3] = 0L;
							tag[num3] = 6L;
							duration[num3] = 0L;
							reset_count[num3] = 0L;
							affinity[num3] = affinitymask_little | affinitymask_big;
							UpdateNode1(500, ref threadrecord[num9], num6, acc_instruction_b[num3], acc_aclk_b[num3], acc_load_b[num3], acc_store_b[num3], acc_load_miss_b[num3], acc_br_b[num3], acc_runtime_b[num3], cnt_b[num3], acc_instruction_l[num3], acc_aclk_l[num3], acc_load_l[num3], acc_load_l_perm[num3], last_duration[num3], now_duration[num3], acc_store_l[num3], acc_store_l_perm[num3], acc_load_miss_l[num3], acc_br_l[num3], acc_runtime_l[num3], cnt_l[num3], ipc_b[num3], max_ipc_b[num3], ipc_l[num3], ipc_l_perm[num3], max_ipc_l[num3], ipc_ratio[num3], br_ratio[num3], br_load_ratio[num3], load_miss_ratio_b[num3], min_load_miss_ratio_b[num3], load_miss_ratio_l[num3], avg_runtime_b[num3], avg_runtime_l[num3], avg_freq_b[num3], avg_freq_l[num3], max_ins[num3], lock_data[num3], tag[num3], duration[num3], reset_count[num3], affinity[num3], residence[num3]);
							try
							{
								IntPtr intPtr = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)num6);
								if (intPtr != IntPtr.Zero)
								{
									SetThreadAffinityMask(intPtr, (IntPtr)(affinitymask_little | affinitymask_big));
									CloseHandle(intPtr);
								}
							}
							catch
							{
							}
						}
						findresultp[num3] = FindNodeValueP(ref processrecord[num10], num7, ref ins_total[num3], ref store_total[num3], ref count_total[num3], ref intval[num3], ref nonstore_store_ratio[num3], ref usr_sum[num3], ref usr_count[num3], ref usr_ratio[num3], ref residence_p[num3], ref residence_p1[num3]);
						if (findresultp[num3] == 0)
						{
							ins_total[num3] = 0L;
							store_total[num3] = 0L;
							count_total[num3] = 0L;
							intval[num3] = DateTime.Now.Ticks;
							nonstore_store_ratio[num3] = 0L;
							usr_sum[num3] = 0L;
							usr_count[num3] = 0L;
							usr_ratio[num3] = 0L;
							residence_p[num3] = 0L;
							residence_p1[num3] = 0L;
							UpdateNodeP(500, ref processrecord[num10], num7, ins_total[num3], store_total[num3], count_total[num3], intval[num3], nonstore_store_ratio[num3], usr_sum[num3], usr_count[num3], usr_ratio[num3], residence_p[num3], residence_p1[num3]);
						}
						if ((datetime_trigger > 0) & ((int)((uint)(1 << num3) & affinitymask_little) > 0))
						{
							schedule_thread[num3] = FindNodeValue2(ref schd_queue_l2b, ref num2);
							if (schedule_thread[num3] > 0)
							{
								long num11 = schedule_thread[num3] % 10000;
								findresult[num3] = FindNodeValue1(ref threadrecord[num11], schedule_thread[num3], ref acc_instruction_b1[num3], ref acc_aclk_b1[num3], ref acc_load_b1[num3], ref acc_store_b1[num3], ref acc_load_miss_b1[num3], ref acc_br_b1[num3], ref acc_runtime_b1[num3], ref cnt_b1[num3], ref acc_instruction_l1[num3], ref acc_aclk_l1[num3], ref acc_load_l1[num3], ref acc_load_l1_perm[num3], ref last_duration1[num3], ref now_duration1[num3], ref acc_store_l1[num3], ref acc_store_l1_perm[num3], ref acc_load_miss_l1[num3], ref acc_br_l1[num3], ref acc_runtime_l1[num3], ref cnt_l1[num3], ref ipc_b1[num3], ref max_ipc_b1[num3], ref ipc_l1[num3], ref ipc_l1_perm[num3], ref max_ipc_l1[num3], ref ipc_ratio1[num3], ref br_ratio1[num3], ref br_load_ratio1[num3], ref load_miss_ratio_b1[num3], ref min_load_miss_ratio_b1[num3], ref load_miss_ratio_l1[num3], ref avg_runtime_b1[num3], ref avg_runtime_l1[num3], ref avg_freq_b1[num3], ref avg_freq_l1[num3], ref max_ins1[num3], ref lock_data1[num3], ref tag1[num3], ref duration1[num3], ref reset_count1[num3], ref affinity1[num3]);
								if (findresult[num3] > 0)
								{
									try
									{
										IntPtr intPtr2 = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)schedule_thread[num3]);
										if (intPtr2 != IntPtr.Zero)
										{
											IntPtr intPtr3 = SetThreadAffinityMask(intPtr2, (IntPtr)(affinitymask_little | affinitymask_big));
											CloseHandle(intPtr2);
											if (intPtr3 != IntPtr.Zero)
											{
												_2_to_6++;
												reset_count1[num3] = 0L;
												tag1[num3] = 6L;
												affinity1[num3] = affinitymask_little | affinitymask_big;
												datetime_trigger--;
												acc_aclk1[num3] = 0L;
												acc_runtime_b1[num3] = 0L;
												acc_runtime_l1[num3] = 0L;
												acc_load_l1_perm[num3] = 0L;
												acc_store_l1_perm[num3] = 0L;
												max_ipc_l1[num3] = 0L;
												acc_instruction_l1[num3] = 0L;
												avg_runtime_l1[num3] = 0L;
												max_ins1[num3] = 0L;
												acc_load_l1[num3] = 0L;
												avg_freq_l1[num3] = acc_load_miss_b1[num3];
												max_ipc_b1[num3] = now_duration1[num3];
											}
										}
									}
									catch
									{
									}
									duration1[num3] = 100000L;
									UpdateNode1(500, ref threadrecord[num11], schedule_thread[num3], acc_instruction_b1[num3], acc_aclk_b1[num3], acc_load_b1[num3], acc_store_b1[num3], acc_load_miss_b1[num3], acc_br_b1[num3], acc_runtime_b1[num3], cnt_b1[num3], acc_instruction_l1[num3], acc_aclk_l1[num3], acc_load_l1[num3], acc_load_l1_perm[num3], last_duration1[num3], now_duration1[num3], acc_store_l1[num3], acc_store_l1_perm[num3], acc_load_miss_l1[num3], acc_br_l1[num3], acc_runtime_l1[num3], cnt_l1[num3], ipc_b1[num3], max_ipc_b1[num3], ipc_l1[num3], ipc_l1_perm[num3], max_ipc_l1[num3], ipc_ratio1[num3], br_ratio1[num3], br_load_ratio1[num3], load_miss_ratio_b1[num3], min_load_miss_ratio_b1[num3], load_miss_ratio_l1[num3], avg_runtime_b1[num3], avg_runtime_l1[num3], avg_freq_b1[num3], avg_freq_l1[num3], max_ins1[num3], lock_data1[num3], tag1[num3], duration1[num3], reset_count1[num3], affinity1[num3], residence1[num3]);
									DeleteNode(ref schd_queue_l2b, schedule_thread[num3]);
								}
								else
								{
									DeleteNode(ref schd_queue_l2b, schedule_thread[num3]);
								}
							}
						}
						if ((datetime_trigger > 0) & ((int)((uint)(1 << num3) & affinitymask_big) > 0))
						{
							schedule_thread[num3] = FindNodeValue2(ref schd_queue_b2l, ref num2);
							if (schedule_thread[num3] > 0)
							{
								long num12 = schedule_thread[num3] % 10000;
								findresult[num3] = FindNodeValue1(ref threadrecord[num12], schedule_thread[num3], ref acc_instruction_b1[num3], ref acc_aclk_b1[num3], ref acc_load_b1[num3], ref acc_store_b1[num3], ref acc_load_miss_b1[num3], ref acc_br_b1[num3], ref acc_runtime_b1[num3], ref cnt_b1[num3], ref acc_instruction_l1[num3], ref acc_aclk_l1[num3], ref acc_load_l1[num3], ref acc_load_l1_perm[num3], ref last_duration1[num3], ref now_duration1[num3], ref acc_store_l1[num3], ref acc_store_l1_perm[num3], ref acc_load_miss_l1[num3], ref acc_br_l1[num3], ref acc_runtime_l1[num3], ref cnt_l1[num3], ref ipc_b1[num3], ref max_ipc_b1[num3], ref ipc_l1[num3], ref ipc_l1_perm[num3], ref max_ipc_l1[num3], ref ipc_ratio1[num3], ref br_ratio1[num3], ref br_load_ratio1[num3], ref load_miss_ratio_b1[num3], ref min_load_miss_ratio_b1[num3], ref load_miss_ratio_l1[num3], ref avg_runtime_b1[num3], ref avg_runtime_l1[num3], ref avg_freq_b1[num3], ref avg_freq_l1[num3], ref max_ins1[num3], ref lock_data1[num3], ref tag1[num3], ref duration1[num3], ref reset_count1[num3], ref affinity1[num3]);
								if (findresult[num3] > 0)
								{
									try
									{
										IntPtr intPtr4 = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)schedule_thread[num3]);
										if (intPtr4 != IntPtr.Zero)
										{
											IntPtr intPtr5 = SetThreadAffinityMask(intPtr4, (IntPtr)affinitymask_little);
											CloseHandle(intPtr4);
											if (intPtr5 != IntPtr.Zero)
											{
												reset_count1[num3] = 0L;
												_6_to_2++;
												tag1[num3] = 2L;
												affinity1[num3] = affinitymask_little;
												datetime_trigger--;
												acc_load_b1[num3] = 0L;
												acc_aclk_b1[num3] = 0L;
												acc_instruction_b1[num3] = 0L;
												acc_store_b1[num3] = 0L;
												acc_br_b1[num3] = 0L;
												acc_aclk_l1[num3] = 0L;
												last_duration1[num3] = 0L;
												acc_load_b1[num3] = 0L;
												acc_load_miss_b1[num3] = avg_freq_l1[num3];
												now_duration1[num3] = max_ipc_b1[num3];
												acc_store_l1[num3] = 0L;
											}
										}
									}
									catch
									{
									}
									duration1[num3] = 100000L;
									UpdateNode1(500, ref threadrecord[num12], schedule_thread[num3], acc_instruction_b1[num3], acc_aclk_b1[num3], acc_load_b1[num3], acc_store_b1[num3], acc_load_miss_b1[num3], acc_br_b1[num3], acc_runtime_b1[num3], cnt_b1[num3], acc_instruction_l1[num3], acc_aclk_l1[num3], acc_load_l1[num3], acc_load_l1_perm[num3], last_duration1[num3], now_duration1[num3], acc_store_l1[num3], acc_store_l1_perm[num3], acc_load_miss_l1[num3], acc_br_l1[num3], acc_runtime_l1[num3], cnt_l1[num3], ipc_b1[num3], max_ipc_b1[num3], ipc_l1[num3], ipc_l1_perm[num3], max_ipc_l1[num3], ipc_ratio1[num3], br_ratio1[num3], br_load_ratio1[num3], load_miss_ratio_b1[num3], min_load_miss_ratio_b1[num3], load_miss_ratio_l1[num3], avg_runtime_b1[num3], avg_runtime_l1[num3], avg_freq_b1[num3], avg_freq_l1[num3], max_ins1[num3], lock_data1[num3], tag1[num3], duration1[num3], reset_count1[num3], affinity1[num3], residence1[num3]);
									DeleteNode(ref schd_queue_b2l, schedule_thread[num3]);
								}
								else
								{
									DeleteNode(ref schd_queue_b2l, schedule_thread[num3]);
								}
							}
						}
						if (duration[num3] > 0)
						{
							duration[num3] -= datetime_elapsed[num3];
							if (duration[num3] < 0)
							{
								duration[num3] = 0L;
							}
						}
						cnt_l[num3]++;
						ipc_ratio[num3] += datetime_elapsed[num3];
						if ((DateTime.Now.Ticks - cnt_b[num3] > 300000) & (cnt_l[num3] > 0))
						{
							temp_ticks[num3] = 1000 * ipc_ratio[num3] / (DateTime.Now.Ticks - cnt_b[num3]);
							br_load_ratio[num3] = temp_ticks[num3];
							cnt_l[num3] = (DateTime.Now.Ticks - cnt_b[num3]) / (10 * cnt_l[num3]);
							br_ratio[num3] = cnt_l[num3];
							cnt_b[num3] = DateTime.Now.Ticks;
							cnt_l[num3] = 0L;
							temp_ticks[num3] = 0L;
							ipc_ratio[num3] = 0L;
						}
						if (counter_action_switch == 1)
						{
							takeaction[num3] = 0;
						}
						else if (takeaction[num3] == 2)
						{
							myOls.RdmsrTx(198u, ref num4, ref num5, threadAffinityMask3);
							result_aclk_l[num3] = num5 | num4;
							myOls.RdmsrTx(195u, ref num4, ref num5, threadAffinityMask3);
							result_mclk_l[num3] = num5 | num4;
							myOls.RdmsrTx(196u, ref num4, ref num5, threadAffinityMask3);
							result_br_ins_l[num3] = num5 | num4;
							myOls.RdmsrTx(194u, ref num4, ref num5, threadAffinityMask3);
							result_load_l[num3] = num5 | num4;
							myOls.RdmsrTx(199u, ref num4, ref num5, threadAffinityMask3);
							result_store_l[num3] = num5 | num4;
							myOls.RdmsrTx(197u, ref num4, ref num5, threadAffinityMask3);
							result_load_l1_l[num3] = num5 | num4;
							myOls.RdmsrTx(193u, ref num4, ref num5, threadAffinityMask3);
							result_ins_l[num3] = num5 | num4;
							result_aclk[num3] = result_aclk_l[num3] - result_aclk_e[num3];
							if (result_aclk[num3] < 0)
							{
								result_aclk[num3] = 0L;
							}
							result_mclk[num3] = result_mclk_l[num3] - result_mclk_e[num3];
							if (result_mclk[num3] < 0)
							{
								result_mclk[num3] = 0L;
							}
							result_br_ins[num3] = result_br_ins_l[num3] - result_br_ins_e[num3];
							if (result_br_ins[num3] < 0)
							{
								result_br_ins[num3] = 0L;
							}
							result_load[num3] = result_load_l[num3] - result_load_e[num3];
							if (result_load[num3] < 0)
							{
								result_load[num3] = 0L;
							}
							result_store[num3] = result_store_l[num3] - result_store_e[num3];
							if (result_store[num3] < 0)
							{
								result_store[num3] = 0L;
							}
							result_load_l1[num3] = result_load_l1_l[num3] - result_load_l1_e[num3];
							if (result_load_l1[num3] < 0)
							{
								result_load_l1[num3] = 0L;
							}
							result_ins[num3] = result_ins_l[num3] - result_ins_e[num3];
							if (result_ins[num3] < 0)
							{
								result_ins[num3] = 0L;
							}
							result_ins_comp[num3] = result_br_ins[num3];
							if ((int)((uint)(1 << num3) & affinitymask_little) > 0)
							{
								acc_aclk_b[num3] += result_ins[num3];
								acc_instruction_b[num3] += result_load[num3];
								acc_store_b[num3] += result_mclk[num3];
								acc_br_b[num3] += result_br_ins[num3];
								acc_aclk_l[num3] += result_load_l1[num3];
								last_duration[num3] += result_aclk[num3];
								acc_load_b[num3] += datetime_elapsed[num3];
								acc_store_l[num3]++;
							}
							if ((int)((uint)(1 << num3) & affinitymask_big) > 0)
							{
								acc_runtime_b[num3] += result_br_ins[num3];
								acc_runtime_l[num3] += result_aclk[num3];
								acc_load_l_perm[num3] += result_ins[num3];
								acc_store_l_perm[num3] += result_load[num3];
								max_ipc_l[num3] += result_mclk[num3];
								acc_instruction_l[num3] += result_aclk[num3];
								avg_runtime_l[num3]++;
								acc_load_l[num3] += result_load_l1[num3];
							}
							if ((int)((uint)(1 << num3) & affinitymask_big) > 0)
							{
								acc_aclk[num3] += datetime_elapsed[num3];
								if (acc_aclk[num3] > 300000)
								{
									if (acc_instruction_l[num3] > 0)
									{
										avg_freq_l[num3] = 100 * acc_load_l[num3] / acc_instruction_l[num3];
									}
									if (acc_load_l_perm[num3] > 0)
									{
										avg_runtime_b[num3] = 100 * acc_store_l_perm[num3] / acc_load_l_perm[num3];
										eff_big_sum += avg_runtime_b[num3];
										eff_big_count++;
									}
									if (acc_runtime_l[num3] > 0)
									{
										min_load_miss_ratio_b[num3] = 100 * acc_runtime_b[num3] / acc_runtime_l[num3];
										ipc_big_sum += min_load_miss_ratio_b[num3];
										ipc_big_count++;
									}
									if ((avg_runtime_l[num3] > 0) & (usr_ratio[num3] > 0) & (avg_freq_l[num3] > 0) & (min_load_miss_ratio_b[num3] > 0) & (avg_runtime_b[num3] > 0) & (ipc_big_avg > 0) & (eff_big_avg > 0))
									{
										long factor = GetFactor(avg_freq_l[num3], usr_ratio[num3]);
										max_ipc_b[num3] = (50 * min_load_miss_ratio_b[num3] / ipc_big_avg + acc_runtime_b[num3] / (avg_runtime_l[num3] * 6000)) * factor + avg_runtime_b[num3] * 100 * (100 - factor) / eff_big_avg;
									}
								}
								if (acc_aclk[num3] > 300000000)
								{
									acc_aclk[num3] = 0L;
									acc_runtime_b[num3] = 0L;
									acc_runtime_l[num3] = 0L;
									acc_load_l_perm[num3] = 0L;
									acc_store_l_perm[num3] = 0L;
									max_ipc_l[num3] = 0L;
									acc_instruction_l[num3] = 0L;
									avg_runtime_l[num3] = 0L;
									acc_load_l[num3] = 0L;
								}
								if (avg_freq_l[num3] > 0)
								{
									count_total[num3]++;
									usr_sum[num3] += avg_freq_l[num3];
									usr_count[num3]++;
								}
								if (usr_count[num3] > 1999)
								{
									usr_ratio[num3] = usr_sum[num3] / usr_count[num3];
								}
								if (max_ipc_b[num3] > 0)
								{
									ins_total[num3] += max_ipc_b[num3];
									store_total[num3]++;
									count_total[num3]++;
								}
								if (store_total[num3] > 1999)
								{
									nonstore_store_ratio[num3] = ins_total[num3] / store_total[num3];
									if (store_total[num3] == 2000)
									{
										intval[num3] = DateTime.Now.Ticks;
									}
								}
								if (DateTime.Now.Ticks - intval[num3] > 600000000)
								{
									ins_total[num3] = 0L;
									store_total[num3] = 0L;
									count_total[num3] = 0L;
									usr_sum[num3] = 0L;
									usr_count[num3] = 0L;
								}
							}
							if ((int)((uint)(1 << num3) & affinitymask_little) > 0)
							{
								if (acc_load_b[num3] > 300000)
								{
									if (last_duration[num3] > 0)
									{
										acc_load_miss_b[num3] = 100 * acc_aclk_l[num3] / last_duration[num3];
									}
									if (acc_br_b[num3] > 0)
									{
										ipc_l[num3] = 100 * last_duration[num3] / acc_br_b[num3];
										ipc_little_sum += ipc_l[num3];
										ipc_little_count++;
									}
									if (acc_aclk_b[num3] > 0)
									{
										acc_br_l[num3] = 100 * acc_instruction_b[num3] / acc_aclk_b[num3];
										eff_little_sum += acc_br_l[num3];
										eff_little_count++;
									}
									if ((acc_store_l[num3] > 0) & (acc_load_miss_b[num3] > 0) & (usr_ratio[num3] > 0) & (ipc_little_avg > 0) & (eff_little_avg > 0) & (ipc_l[num3] > 0) & (acc_br_l[num3] > 0))
									{
										long factor2 = GetFactor(acc_load_miss_b[num3], usr_ratio[num3]);
										now_duration[num3] = (50 * ipc_l[num3] / ipc_little_avg + last_duration[num3] / (acc_store_l[num3] * 6000)) * factor2 + acc_br_l[num3] * 100 * (100 - factor2) / eff_little_avg;
									}
									if (acc_store_l[num3] > 0)
									{
										acc_load_miss_l[num3] = last_duration[num3] / acc_store_l[num3];
									}
								}
								if (acc_load_b[num3] > 300000000)
								{
									acc_load_b[num3] = 0L;
									acc_aclk_b[num3] = 0L;
									acc_instruction_b[num3] = 0L;
									acc_store_b[num3] = 0L;
									acc_br_b[num3] = 0L;
									acc_aclk_l[num3] = 0L;
									last_duration[num3] = 0L;
									acc_load_b[num3] = 0L;
									acc_store_l[num3] = 0L;
								}
								if (acc_load_miss_b[num3] > 0)
								{
									count_total[num3]++;
									usr_sum[num3] += acc_load_miss_b[num3];
									usr_count[num3]++;
								}
								if (usr_count[num3] > 1999)
								{
									usr_ratio[num3] = usr_sum[num3] / usr_count[num3];
								}
								if (now_duration[num3] > 0)
								{
									ins_total[num3] += now_duration[num3];
									store_total[num3]++;
									count_total[num3]++;
								}
								if (store_total[num3] > 1999)
								{
									nonstore_store_ratio[num3] = ins_total[num3] / store_total[num3];
									if (store_total[num3] == 2000)
									{
										intval[num3] = DateTime.Now.Ticks;
									}
								}
								if (DateTime.Now.Ticks - intval[num3] > 600000000)
								{
									ins_total[num3] = 0L;
									store_total[num3] = 0L;
									count_total[num3] = 0L;
									usr_sum[num3] = 0L;
									usr_count[num3] = 0L;
								}
							}
						}
						if (((oldthread_waittime[num3] > 10000) & (duration[num3] == 0)) && (((int)((uint)(1 << num3) & affinitymask_little) > 0) & (tag[num3] == 2)))
						{
							if (reset_count[num3] == 0L)
							{
								UpdateNode2_little(ref sched_queue_l2b[num3], num6, br_ratio[num3] * br_load_ratio[num3], 0, ref reset_count[num3]);
							}
						}
						else if ((DateTime.Now.Ticks - lock_data[num3] > 1000000) & (duration[num3] == 0))
						{
							temp2[num3] = Intval2Limit(num6, br_ratio[num3], br_load_ratio[num3], nonstore_store_ratio[num3], ref usr_ratio[num3], ref avg_runtime_b[num3], currentprocessor[num3], avg_freq_l[num3], ref max_ins[num3], ref avg_freq_b[num3], max_ipc_b[num3], ref tag[num3], affinity[num3], ref reset_count[num3], ref acc_load_miss_b[num3], ref now_duration[num3], ref acc_load_miss_l[num3], ref lock_data[num3], ref residence_p1[num3], ref residence_p[num3]);
						}
						UpdateNodeP(500, ref processrecord[num10], num7, ins_total[num3], store_total[num3], count_total[num3], intval[num3], nonstore_store_ratio[num3], usr_sum[num3], usr_count[num3], usr_ratio[num3], residence_p[num3], residence_p1[num3]);
						if (reset_count[num3] == 0L)
						{
							UpdateNode1(500, ref threadrecord[num9], num6, acc_instruction_b[num3], acc_aclk_b[num3], acc_load_b[num3], acc_store_b[num3], acc_load_miss_b[num3], acc_br_b[num3], acc_runtime_b[num3], cnt_b[num3], acc_instruction_l[num3], acc_aclk_l[num3], acc_load_l[num3], acc_load_l_perm[num3], last_duration[num3], now_duration[num3], acc_store_l[num3], acc_store_l_perm[num3], acc_load_miss_l[num3], acc_br_l[num3], acc_runtime_l[num3], cnt_l[num3], ipc_b[num3], max_ipc_b[num3], ipc_l[num3], ipc_l_perm[num3], max_ipc_l[num3], ipc_ratio[num3], br_ratio[num3], br_load_ratio[num3], load_miss_ratio_b[num3], min_load_miss_ratio_b[num3], load_miss_ratio_l[num3], avg_runtime_b[num3], avg_runtime_l[num3], avg_freq_b[num3], avg_freq_l[num3], max_ins[num3], lock_data[num3], tag[num3], duration[num3], reset_count[num3], affinity[num3], residence[num3]);
						}
					}
					if (counter_action_switch == 1)
					{
						takeaction[num3] = 0;
					}
					else
					{
						takeaction[num3] = 2;
						myOls.RdmsrTx(198u, ref num4, ref num5, threadAffinityMask3);
						result_aclk_l[num3] = num5 | num4;
						result_aclk_e[num3] = result_aclk_l[num3];
						myOls.RdmsrTx(195u, ref num4, ref num5, threadAffinityMask3);
						result_mclk_l[num3] = num5 | num4;
						result_mclk_e[num3] = result_mclk_l[num3];
						myOls.RdmsrTx(196u, ref num4, ref num5, threadAffinityMask3);
						result_br_ins_l[num3] = num5 | num4;
						result_br_ins_e[num3] = result_br_ins_l[num3];
						myOls.RdmsrTx(194u, ref num4, ref num5, threadAffinityMask3);
						result_load_l[num3] = num5 | num4;
						result_load_e[num3] = result_load_l[num3];
						myOls.RdmsrTx(199u, ref num4, ref num5, threadAffinityMask3);
						result_store_l[num3] = num5 | num4;
						result_store_e[num3] = result_store_l[num3];
						myOls.RdmsrTx(197u, ref num4, ref num5, threadAffinityMask3);
						result_load_l1_l[num3] = num5 | num4;
						result_load_l1_e[num3] = result_load_l1_l[num3];
						myOls.RdmsrTx(193u, ref num4, ref num5, threadAffinityMask3);
						result_ins_l[num3] = num5 | num4;
						result_ins_e[num3] = result_ins_l[num3];
					}
					oldthread_waittime[num3] = data.NewThreadWaitTime;
				};
				traceEventSession.Source.Process();
			}
			void thread2()
			{
				System.Timers.Timer timer = new System.Timers.Timer(30.0);
				timer.Elapsed += OnTimedEvent;
				timer.Start();
			}
		}

		protected override void OnStop()
		{
		}

		private void OnTimedEvent(object sender, ElapsedEventArgs e)
		{
			count_stat6++;
			if (count_stat6 > 3840)
			{
				count_stat6 = 0L;
				ipc_big_sum = 0L;
				ipc_big_count = 0L;
				ipc_little_sum = 0L;
				ipc_little_count = 0L;
				eff_big_sum = 0L;
				eff_big_count = 0L;
				eff_little_sum = 0L;
				eff_little_count = 0L;
			}
			count_stat5++;
			count_stat5 = 0L;
			datetime_trigger = 15L;
			datetime_trigger_little = 15L;
			datetime_trigger_exchange = 15L;
			if (ipc_big_count > 1000)
			{
				ipc_big_avg = ipc_big_sum / ipc_big_count;
			}
			if (ipc_little_count > 1000)
			{
				ipc_little_avg = ipc_little_sum / ipc_little_count;
			}
			if (eff_big_count > 1000)
			{
				eff_big_avg = eff_big_sum / eff_big_count;
			}
			if (eff_little_count > 1000)
			{
				eff_little_avg = eff_little_sum / eff_little_count;
			}
			if (counter_action_switch == 1)
			{
				count_stat1++;
				if (count_stat1 > 96)
				{
					count_stat1 = 0L;
					counter_action_switch = 0L;
					for (int i = 0; i < Convert.ToInt32(NumberOfLogicalProcessors); i++)
					{
						if ((int)((uint)(1 << i) & affinitymask_little) > 0)
						{
							UIntPtr threadAffinityMask = (UIntPtr)(ulong)Math.Pow(2.0, i);
							myOls.WrmsrTx(390u, 4390926u, 0u, threadAffinityMask);
							myOls.WrmsrTx(391u, 4391106u, 0u, threadAffinityMask);
							myOls.WrmsrTx(392u, 4423364u, 0u, threadAffinityMask);
							myOls.WrmsrTx(393u, 4390972u, 0u, threadAffinityMask);
							myOls.WrmsrTx(394u, 4260032u, 0u, threadAffinityMask);
							myOls.WrmsrTx(395u, 4391104u, 0u, threadAffinityMask);
							myOls.WrmsrTx(396u, 4390972u, 0u, threadAffinityMask);
						}
						else
						{
							UIntPtr threadAffinityMask2 = (UIntPtr)(ulong)Math.Pow(2.0, i);
							myOls.WrmsrTx(390u, 4391342u, 0u, threadAffinityMask2);
							myOls.WrmsrTx(391u, 4391618u, 0u, threadAffinityMask2);
							myOls.WrmsrTx(392u, 4391342u, 0u, threadAffinityMask2);
							myOls.WrmsrTx(393u, 4391104u, 0u, threadAffinityMask2);
							myOls.WrmsrTx(394u, 4260032u, 0u, threadAffinityMask2);
							myOls.WrmsrTx(395u, 4390972u, 0u, threadAffinityMask2);
							myOls.WrmsrTx(396u, 4390972u, 0u, threadAffinityMask2);
						}
					}
				}
			}
			if (counter_action_switch == 0L)
			{
				count_stat2++;
				if (count_stat2 > 32)
				{
					count_stat2 = 0L;
					counter_action_switch = 1L;
					for (int j = 0; j < Convert.ToInt32(NumberOfLogicalProcessors); j++)
					{
						if ((int)((uint)(1 << j) & affinitymask_little) > 0)
						{
							UIntPtr threadAffinityMask3 = (UIntPtr)(ulong)Math.Pow(2.0, j);
							myOls.WrmsrTx(390u, 196622u, 0u, threadAffinityMask3);
							myOls.WrmsrTx(391u, 196802u, 0u, threadAffinityMask3);
							myOls.WrmsrTx(392u, 229060u, 0u, threadAffinityMask3);
							myOls.WrmsrTx(393u, 196668u, 0u, threadAffinityMask3);
							myOls.WrmsrTx(394u, 65728u, 0u, threadAffinityMask3);
							myOls.WrmsrTx(395u, 196800u, 0u, threadAffinityMask3);
							myOls.WrmsrTx(396u, 196668u, 0u, threadAffinityMask3);
						}
						else
						{
							UIntPtr threadAffinityMask4 = (UIntPtr)(ulong)Math.Pow(2.0, j);
							myOls.WrmsrTx(390u, 197038u, 0u, threadAffinityMask4);
							myOls.WrmsrTx(391u, 197314u, 0u, threadAffinityMask4);
							myOls.WrmsrTx(392u, 197038u, 0u, threadAffinityMask4);
							myOls.WrmsrTx(393u, 196800u, 0u, threadAffinityMask4);
							myOls.WrmsrTx(394u, 65728u, 0u, threadAffinityMask4);
							myOls.WrmsrTx(395u, 196668u, 0u, threadAffinityMask4);
							myOls.WrmsrTx(396u, 196668u, 0u, threadAffinityMask4);
						}
					}
				}
			}
			count_stat3++;
			if (count_stat3 > 3840)
			{
				avg_ipc_trigger = 0L;
				count_stat3 = 0L;
				GC.Collect();
			}
			count_stat++;
			if (count_stat > 1920)
			{
				count_stat = 0L;
				string path = "统计数据.txt";
				string contents = "统计数据" + Environment.NewLine + "调度成功次数:" + _6_to_2;
				File.WriteAllText(path, contents);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new Container();
			base.ServiceName = "Service1";
		}
	}
}
