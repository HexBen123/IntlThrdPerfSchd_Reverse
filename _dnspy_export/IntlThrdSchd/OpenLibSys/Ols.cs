using System;
using System.Runtime.InteropServices;

namespace OpenLibSys
{
	// Token: 0x02000005 RID: 5
	public class Ols : IDisposable
	{
		// Token: 0x06000024 RID: 36 RVA: 0x000049D5 File Offset: 0x00002BD5
		public uint PciBusDevFunc(uint bus, uint dev, uint func)
		{
			return ((bus & 255U) << 8) | ((dev & 31U) << 3) | (func & 7U);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000049EB File Offset: 0x00002BEB
		public uint PciGetBus(uint address)
		{
			return (address >> 8) & 255U;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000049F6 File Offset: 0x00002BF6
		public uint PciGetDev(uint address)
		{
			return (address >> 3) & 31U;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000049FE File Offset: 0x00002BFE
		public uint PciGetFunc(uint address)
		{
			return address & 7U;
		}

		// Token: 0x06000028 RID: 40
		[DllImport("kernel32")]
		public static extern IntPtr LoadLibrary(string lpFileName);

		// Token: 0x06000029 RID: 41
		[DllImport("kernel32", SetLastError = true)]
		private static extern bool FreeLibrary(IntPtr hModule);

		// Token: 0x0600002A RID: 42
		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		// Token: 0x0600002B RID: 43 RVA: 0x00004A04 File Offset: 0x00002C04
		public Ols()
		{
			string text;
			if (IntPtr.Size == 8)
			{
				text = "WinRing0x64.dll";
			}
			else
			{
				text = "WinRing0.dll";
			}
			this.module = Ols.LoadLibrary(text);
			if (this.module == IntPtr.Zero)
			{
				this.status = 1U;
				return;
			}
			this.GetDllStatus = (Ols._GetDllStatus)this.GetDelegate("GetDllStatus", typeof(Ols._GetDllStatus));
			this.GetDllVersion = (Ols._GetDllVersion)this.GetDelegate("GetDllVersion", typeof(Ols._GetDllVersion));
			this.GetDriverVersion = (Ols._GetDriverVersion)this.GetDelegate("GetDriverVersion", typeof(Ols._GetDriverVersion));
			this.GetDriverType = (Ols._GetDriverType)this.GetDelegate("GetDriverType", typeof(Ols._GetDriverType));
			this.InitializeOls = (Ols._InitializeOls)this.GetDelegate("InitializeOls", typeof(Ols._InitializeOls));
			this.DeinitializeOls = (Ols._DeinitializeOls)this.GetDelegate("DeinitializeOls", typeof(Ols._DeinitializeOls));
			this.IsCpuid = (Ols._IsCpuid)this.GetDelegate("IsCpuid", typeof(Ols._IsCpuid));
			this.IsMsr = (Ols._IsMsr)this.GetDelegate("IsMsr", typeof(Ols._IsMsr));
			this.IsTsc = (Ols._IsTsc)this.GetDelegate("IsTsc", typeof(Ols._IsTsc));
			this.Hlt = (Ols._Hlt)this.GetDelegate("Hlt", typeof(Ols._Hlt));
			this.HltTx = (Ols._HltTx)this.GetDelegate("HltTx", typeof(Ols._HltTx));
			this.HltPx = (Ols._HltPx)this.GetDelegate("HltPx", typeof(Ols._HltPx));
			this.Rdmsr = (Ols._Rdmsr)this.GetDelegate("Rdmsr", typeof(Ols._Rdmsr));
			this.RdmsrTx = (Ols._RdmsrTx)this.GetDelegate("RdmsrTx", typeof(Ols._RdmsrTx));
			this.RdmsrPx = (Ols._RdmsrPx)this.GetDelegate("RdmsrPx", typeof(Ols._RdmsrPx));
			this.Wrmsr = (Ols._Wrmsr)this.GetDelegate("Wrmsr", typeof(Ols._Wrmsr));
			this.WrmsrTx = (Ols._WrmsrTx)this.GetDelegate("WrmsrTx", typeof(Ols._WrmsrTx));
			this.WrmsrPx = (Ols._WrmsrPx)this.GetDelegate("WrmsrPx", typeof(Ols._WrmsrPx));
			this.Rdpmc = (Ols._Rdpmc)this.GetDelegate("Rdpmc", typeof(Ols._Rdpmc));
			this.RdpmcTx = (Ols._RdpmcTx)this.GetDelegate("RdpmcTx", typeof(Ols._RdpmcTx));
			this.RdpmcPx = (Ols._RdpmcPx)this.GetDelegate("RdpmcPx", typeof(Ols._RdpmcPx));
			this.Cpuid = (Ols._Cpuid)this.GetDelegate("Cpuid", typeof(Ols._Cpuid));
			this.CpuidTx = (Ols._CpuidTx)this.GetDelegate("CpuidTx", typeof(Ols._CpuidTx));
			this.CpuidPx = (Ols._CpuidPx)this.GetDelegate("CpuidPx", typeof(Ols._CpuidPx));
			this.Rdtsc = (Ols._Rdtsc)this.GetDelegate("Rdtsc", typeof(Ols._Rdtsc));
			this.RdtscTx = (Ols._RdtscTx)this.GetDelegate("RdtscTx", typeof(Ols._RdtscTx));
			this.RdtscPx = (Ols._RdtscPx)this.GetDelegate("RdtscPx", typeof(Ols._RdtscPx));
			this.ReadIoPortByte = (Ols._ReadIoPortByte)this.GetDelegate("ReadIoPortByte", typeof(Ols._ReadIoPortByte));
			this.ReadIoPortWord = (Ols._ReadIoPortWord)this.GetDelegate("ReadIoPortWord", typeof(Ols._ReadIoPortWord));
			this.ReadIoPortDword = (Ols._ReadIoPortDword)this.GetDelegate("ReadIoPortDword", typeof(Ols._ReadIoPortDword));
			this.ReadIoPortByteEx = (Ols._ReadIoPortByteEx)this.GetDelegate("ReadIoPortByteEx", typeof(Ols._ReadIoPortByteEx));
			this.ReadIoPortWordEx = (Ols._ReadIoPortWordEx)this.GetDelegate("ReadIoPortWordEx", typeof(Ols._ReadIoPortWordEx));
			this.ReadIoPortDwordEx = (Ols._ReadIoPortDwordEx)this.GetDelegate("ReadIoPortDwordEx", typeof(Ols._ReadIoPortDwordEx));
			this.WriteIoPortByte = (Ols._WriteIoPortByte)this.GetDelegate("WriteIoPortByte", typeof(Ols._WriteIoPortByte));
			this.WriteIoPortWord = (Ols._WriteIoPortWord)this.GetDelegate("WriteIoPortWord", typeof(Ols._WriteIoPortWord));
			this.WriteIoPortDword = (Ols._WriteIoPortDword)this.GetDelegate("WriteIoPortDword", typeof(Ols._WriteIoPortDword));
			this.WriteIoPortByteEx = (Ols._WriteIoPortByteEx)this.GetDelegate("WriteIoPortByteEx", typeof(Ols._WriteIoPortByteEx));
			this.WriteIoPortWordEx = (Ols._WriteIoPortWordEx)this.GetDelegate("WriteIoPortWordEx", typeof(Ols._WriteIoPortWordEx));
			this.WriteIoPortDwordEx = (Ols._WriteIoPortDwordEx)this.GetDelegate("WriteIoPortDwordEx", typeof(Ols._WriteIoPortDwordEx));
			this.SetPciMaxBusIndex = (Ols._SetPciMaxBusIndex)this.GetDelegate("SetPciMaxBusIndex", typeof(Ols._SetPciMaxBusIndex));
			this.ReadPciConfigByte = (Ols._ReadPciConfigByte)this.GetDelegate("ReadPciConfigByte", typeof(Ols._ReadPciConfigByte));
			this.ReadPciConfigWord = (Ols._ReadPciConfigWord)this.GetDelegate("ReadPciConfigWord", typeof(Ols._ReadPciConfigWord));
			this.ReadPciConfigDword = (Ols._ReadPciConfigDword)this.GetDelegate("ReadPciConfigDword", typeof(Ols._ReadPciConfigDword));
			this.ReadPciConfigByteEx = (Ols._ReadPciConfigByteEx)this.GetDelegate("ReadPciConfigByteEx", typeof(Ols._ReadPciConfigByteEx));
			this.ReadPciConfigWordEx = (Ols._ReadPciConfigWordEx)this.GetDelegate("ReadPciConfigWordEx", typeof(Ols._ReadPciConfigWordEx));
			this.ReadPciConfigDwordEx = (Ols._ReadPciConfigDwordEx)this.GetDelegate("ReadPciConfigDwordEx", typeof(Ols._ReadPciConfigDwordEx));
			this.WritePciConfigByte = (Ols._WritePciConfigByte)this.GetDelegate("WritePciConfigByte", typeof(Ols._WritePciConfigByte));
			this.WritePciConfigWord = (Ols._WritePciConfigWord)this.GetDelegate("WritePciConfigWord", typeof(Ols._WritePciConfigWord));
			this.WritePciConfigDword = (Ols._WritePciConfigDword)this.GetDelegate("WritePciConfigDword", typeof(Ols._WritePciConfigDword));
			this.WritePciConfigByteEx = (Ols._WritePciConfigByteEx)this.GetDelegate("WritePciConfigByteEx", typeof(Ols._WritePciConfigByteEx));
			this.WritePciConfigWordEx = (Ols._WritePciConfigWordEx)this.GetDelegate("WritePciConfigWordEx", typeof(Ols._WritePciConfigWordEx));
			this.WritePciConfigDwordEx = (Ols._WritePciConfigDwordEx)this.GetDelegate("WritePciConfigDwordEx", typeof(Ols._WritePciConfigDwordEx));
			this.FindPciDeviceById = (Ols._FindPciDeviceById)this.GetDelegate("FindPciDeviceById", typeof(Ols._FindPciDeviceById));
			this.FindPciDeviceByClass = (Ols._FindPciDeviceByClass)this.GetDelegate("FindPciDeviceByClass", typeof(Ols._FindPciDeviceByClass));
			if (this.GetDllStatus == null || this.GetDllVersion == null || this.GetDriverVersion == null || this.GetDriverType == null || this.InitializeOls == null || this.DeinitializeOls == null || this.IsCpuid == null || this.IsMsr == null || this.IsTsc == null || this.Hlt == null || this.HltTx == null || this.HltPx == null || this.Rdmsr == null || this.RdmsrTx == null || this.RdmsrPx == null || this.Wrmsr == null || this.WrmsrTx == null || this.WrmsrPx == null || this.Rdpmc == null || this.RdpmcTx == null || this.RdpmcPx == null || this.Cpuid == null || this.CpuidTx == null || this.CpuidPx == null || this.Rdtsc == null || this.RdtscTx == null || this.RdtscPx == null || this.ReadIoPortByte == null || this.ReadIoPortWord == null || this.ReadIoPortDword == null || this.ReadIoPortByteEx == null || this.ReadIoPortWordEx == null || this.ReadIoPortDwordEx == null || this.WriteIoPortByte == null || this.WriteIoPortWord == null || this.WriteIoPortDword == null || this.WriteIoPortByteEx == null || this.WriteIoPortWordEx == null || this.WriteIoPortDwordEx == null || this.SetPciMaxBusIndex == null || this.ReadPciConfigByte == null || this.ReadPciConfigWord == null || this.ReadPciConfigDword == null || this.ReadPciConfigByteEx == null || this.ReadPciConfigWordEx == null || this.ReadPciConfigDwordEx == null || this.WritePciConfigByte == null || this.WritePciConfigWord == null || this.WritePciConfigDword == null || this.WritePciConfigByteEx == null || this.WritePciConfigWordEx == null || this.WritePciConfigDwordEx == null || this.FindPciDeviceById == null || this.FindPciDeviceByClass == null)
			{
				this.status = 2U;
			}
			if (this.InitializeOls() == 0)
			{
				this.status = 3U;
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000535B File Offset: 0x0000355B
		public uint GetStatus()
		{
			return this.status;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00005363 File Offset: 0x00003563
		public void Dispose()
		{
			if (this.module != IntPtr.Zero)
			{
				this.DeinitializeOls();
				Ols.FreeLibrary(this.module);
				this.module = IntPtr.Zero;
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x0000539C File Offset: 0x0000359C
		public Delegate GetDelegate(string procName, Type delegateType)
		{
			IntPtr procAddress = Ols.GetProcAddress(this.module, procName);
			if (procAddress != IntPtr.Zero)
			{
				return Marshal.GetDelegateForFunctionPointer(procAddress, delegateType);
			}
			throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
		}

		// Token: 0x040001CA RID: 458
		private const string dllNameX64 = "WinRing0x64.dll";

		// Token: 0x040001CB RID: 459
		private const string dllName = "WinRing0.dll";

		// Token: 0x040001CC RID: 460
		private IntPtr module = IntPtr.Zero;

		// Token: 0x040001CD RID: 461
		private uint status;

		// Token: 0x040001CE RID: 462
		public Ols._GetDllStatus GetDllStatus;

		// Token: 0x040001CF RID: 463
		public Ols._GetDriverType GetDriverType;

		// Token: 0x040001D0 RID: 464
		public Ols._GetDllVersion GetDllVersion;

		// Token: 0x040001D1 RID: 465
		public Ols._GetDriverVersion GetDriverVersion;

		// Token: 0x040001D2 RID: 466
		public Ols._InitializeOls InitializeOls;

		// Token: 0x040001D3 RID: 467
		public Ols._DeinitializeOls DeinitializeOls;

		// Token: 0x040001D4 RID: 468
		public Ols._IsCpuid IsCpuid;

		// Token: 0x040001D5 RID: 469
		public Ols._IsMsr IsMsr;

		// Token: 0x040001D6 RID: 470
		public Ols._IsTsc IsTsc;

		// Token: 0x040001D7 RID: 471
		public Ols._Hlt Hlt;

		// Token: 0x040001D8 RID: 472
		public Ols._HltTx HltTx;

		// Token: 0x040001D9 RID: 473
		public Ols._HltPx HltPx;

		// Token: 0x040001DA RID: 474
		public Ols._Rdmsr Rdmsr;

		// Token: 0x040001DB RID: 475
		public Ols._RdmsrTx RdmsrTx;

		// Token: 0x040001DC RID: 476
		public Ols._RdmsrPx RdmsrPx;

		// Token: 0x040001DD RID: 477
		public Ols._Wrmsr Wrmsr;

		// Token: 0x040001DE RID: 478
		public Ols._WrmsrTx WrmsrTx;

		// Token: 0x040001DF RID: 479
		public Ols._WrmsrPx WrmsrPx;

		// Token: 0x040001E0 RID: 480
		public Ols._Rdpmc Rdpmc;

		// Token: 0x040001E1 RID: 481
		public Ols._RdpmcTx RdpmcTx;

		// Token: 0x040001E2 RID: 482
		public Ols._RdpmcPx RdpmcPx;

		// Token: 0x040001E3 RID: 483
		public Ols._Cpuid Cpuid;

		// Token: 0x040001E4 RID: 484
		public Ols._CpuidTx CpuidTx;

		// Token: 0x040001E5 RID: 485
		public Ols._CpuidPx CpuidPx;

		// Token: 0x040001E6 RID: 486
		public Ols._Rdtsc Rdtsc;

		// Token: 0x040001E7 RID: 487
		public Ols._RdtscTx RdtscTx;

		// Token: 0x040001E8 RID: 488
		public Ols._RdtscPx RdtscPx;

		// Token: 0x040001E9 RID: 489
		public Ols._ReadIoPortByte ReadIoPortByte;

		// Token: 0x040001EA RID: 490
		public Ols._ReadIoPortWord ReadIoPortWord;

		// Token: 0x040001EB RID: 491
		public Ols._ReadIoPortDword ReadIoPortDword;

		// Token: 0x040001EC RID: 492
		public Ols._ReadIoPortByteEx ReadIoPortByteEx;

		// Token: 0x040001ED RID: 493
		public Ols._ReadIoPortWordEx ReadIoPortWordEx;

		// Token: 0x040001EE RID: 494
		public Ols._ReadIoPortDwordEx ReadIoPortDwordEx;

		// Token: 0x040001EF RID: 495
		public Ols._WriteIoPortByte WriteIoPortByte;

		// Token: 0x040001F0 RID: 496
		public Ols._WriteIoPortWord WriteIoPortWord;

		// Token: 0x040001F1 RID: 497
		public Ols._WriteIoPortDword WriteIoPortDword;

		// Token: 0x040001F2 RID: 498
		public Ols._WriteIoPortByteEx WriteIoPortByteEx;

		// Token: 0x040001F3 RID: 499
		public Ols._WriteIoPortWordEx WriteIoPortWordEx;

		// Token: 0x040001F4 RID: 500
		public Ols._WriteIoPortDwordEx WriteIoPortDwordEx;

		// Token: 0x040001F5 RID: 501
		public Ols._SetPciMaxBusIndex SetPciMaxBusIndex;

		// Token: 0x040001F6 RID: 502
		public Ols._ReadPciConfigByte ReadPciConfigByte;

		// Token: 0x040001F7 RID: 503
		public Ols._ReadPciConfigWord ReadPciConfigWord;

		// Token: 0x040001F8 RID: 504
		public Ols._ReadPciConfigDword ReadPciConfigDword;

		// Token: 0x040001F9 RID: 505
		public Ols._ReadPciConfigByteEx ReadPciConfigByteEx;

		// Token: 0x040001FA RID: 506
		public Ols._ReadPciConfigWordEx ReadPciConfigWordEx;

		// Token: 0x040001FB RID: 507
		public Ols._ReadPciConfigDwordEx ReadPciConfigDwordEx;

		// Token: 0x040001FC RID: 508
		public Ols._WritePciConfigByte WritePciConfigByte;

		// Token: 0x040001FD RID: 509
		public Ols._WritePciConfigWord WritePciConfigWord;

		// Token: 0x040001FE RID: 510
		public Ols._WritePciConfigDword WritePciConfigDword;

		// Token: 0x040001FF RID: 511
		public Ols._WritePciConfigByteEx WritePciConfigByteEx;

		// Token: 0x04000200 RID: 512
		public Ols._WritePciConfigWordEx WritePciConfigWordEx;

		// Token: 0x04000201 RID: 513
		public Ols._WritePciConfigDwordEx WritePciConfigDwordEx;

		// Token: 0x04000202 RID: 514
		public Ols._FindPciDeviceById FindPciDeviceById;

		// Token: 0x04000203 RID: 515
		public Ols._FindPciDeviceByClass FindPciDeviceByClass;

		// Token: 0x0200000D RID: 13
		public enum Status
		{
			// Token: 0x04000255 RID: 597
			NO_ERROR,
			// Token: 0x04000256 RID: 598
			DLL_NOT_FOUND,
			// Token: 0x04000257 RID: 599
			DLL_INCORRECT_VERSION,
			// Token: 0x04000258 RID: 600
			DLL_INITIALIZE_ERROR
		}

		// Token: 0x0200000E RID: 14
		public enum OlsDllStatus
		{
			// Token: 0x0400025A RID: 602
			OLS_DLL_NO_ERROR,
			// Token: 0x0400025B RID: 603
			OLS_DLL_UNSUPPORTED_PLATFORM,
			// Token: 0x0400025C RID: 604
			OLS_DLL_DRIVER_NOT_LOADED,
			// Token: 0x0400025D RID: 605
			OLS_DLL_DRIVER_NOT_FOUND,
			// Token: 0x0400025E RID: 606
			OLS_DLL_DRIVER_UNLOADED,
			// Token: 0x0400025F RID: 607
			OLS_DLL_DRIVER_NOT_LOADED_ON_NETWORK,
			// Token: 0x04000260 RID: 608
			OLS_DLL_UNKNOWN_ERROR = 9
		}

		// Token: 0x0200000F RID: 15
		public enum OlsDriverType
		{
			// Token: 0x04000262 RID: 610
			OLS_DRIVER_TYPE_UNKNOWN,
			// Token: 0x04000263 RID: 611
			OLS_DRIVER_TYPE_WIN_9X,
			// Token: 0x04000264 RID: 612
			OLS_DRIVER_TYPE_WIN_NT,
			// Token: 0x04000265 RID: 613
			OLS_DRIVER_TYPE_WIN_NT4,
			// Token: 0x04000266 RID: 614
			OLS_DRIVER_TYPE_WIN_NT_X64,
			// Token: 0x04000267 RID: 615
			OLS_DRIVER_TYPE_WIN_NT_IA64
		}

		// Token: 0x02000010 RID: 16
		public enum OlsErrorPci : uint
		{
			// Token: 0x04000269 RID: 617
			OLS_ERROR_PCI_BUS_NOT_EXIST = 3758096385U,
			// Token: 0x0400026A RID: 618
			OLS_ERROR_PCI_NO_DEVICE,
			// Token: 0x0400026B RID: 619
			OLS_ERROR_PCI_WRITE_CONFIG,
			// Token: 0x0400026C RID: 620
			OLS_ERROR_PCI_READ_CONFIG
		}

		// Token: 0x02000011 RID: 17
		// (Invoke) Token: 0x060000BA RID: 186
		public delegate uint _GetDllStatus();

		// Token: 0x02000012 RID: 18
		// (Invoke) Token: 0x060000BE RID: 190
		public delegate uint _GetDllVersion(ref byte major, ref byte minor, ref byte revision, ref byte release);

		// Token: 0x02000013 RID: 19
		// (Invoke) Token: 0x060000C2 RID: 194
		public delegate uint _GetDriverVersion(ref byte major, ref byte minor, ref byte revision, ref byte release);

		// Token: 0x02000014 RID: 20
		// (Invoke) Token: 0x060000C6 RID: 198
		public delegate uint _GetDriverType();

		// Token: 0x02000015 RID: 21
		// (Invoke) Token: 0x060000CA RID: 202
		public delegate int _InitializeOls();

		// Token: 0x02000016 RID: 22
		// (Invoke) Token: 0x060000CE RID: 206
		public delegate void _DeinitializeOls();

		// Token: 0x02000017 RID: 23
		// (Invoke) Token: 0x060000D2 RID: 210
		public delegate int _IsCpuid();

		// Token: 0x02000018 RID: 24
		// (Invoke) Token: 0x060000D6 RID: 214
		public delegate int _IsMsr();

		// Token: 0x02000019 RID: 25
		// (Invoke) Token: 0x060000DA RID: 218
		public delegate int _IsTsc();

		// Token: 0x0200001A RID: 26
		// (Invoke) Token: 0x060000DE RID: 222
		public delegate int _Hlt();

		// Token: 0x0200001B RID: 27
		// (Invoke) Token: 0x060000E2 RID: 226
		public delegate int _HltTx(UIntPtr threadAffinityMask);

		// Token: 0x0200001C RID: 28
		// (Invoke) Token: 0x060000E6 RID: 230
		public delegate int _HltPx(UIntPtr processAffinityMask);

		// Token: 0x0200001D RID: 29
		// (Invoke) Token: 0x060000EA RID: 234
		public delegate int _Rdmsr(uint index, ref uint eax, ref uint edx);

		// Token: 0x0200001E RID: 30
		// (Invoke) Token: 0x060000EE RID: 238
		public delegate int _RdmsrTx(uint index, ref uint eax, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x0200001F RID: 31
		// (Invoke) Token: 0x060000F2 RID: 242
		public delegate int _RdmsrPx(uint index, ref uint eax, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000020 RID: 32
		// (Invoke) Token: 0x060000F6 RID: 246
		public delegate int _Wrmsr(uint index, uint eax, uint edx);

		// Token: 0x02000021 RID: 33
		// (Invoke) Token: 0x060000FA RID: 250
		public delegate int _WrmsrTx(uint index, uint eax, uint edx, UIntPtr threadAffinityMask);

		// Token: 0x02000022 RID: 34
		// (Invoke) Token: 0x060000FE RID: 254
		public delegate int _WrmsrPx(uint index, uint eax, uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000023 RID: 35
		// (Invoke) Token: 0x06000102 RID: 258
		public delegate int _Rdpmc(uint index, ref uint eax, ref uint edx);

		// Token: 0x02000024 RID: 36
		// (Invoke) Token: 0x06000106 RID: 262
		public delegate int _RdpmcTx(uint index, ref uint eax, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x02000025 RID: 37
		// (Invoke) Token: 0x0600010A RID: 266
		public delegate int _RdpmcPx(uint index, ref uint eax, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000026 RID: 38
		// (Invoke) Token: 0x0600010E RID: 270
		public delegate int _Cpuid(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx);

		// Token: 0x02000027 RID: 39
		// (Invoke) Token: 0x06000112 RID: 274
		public delegate int _CpuidTx(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x02000028 RID: 40
		// (Invoke) Token: 0x06000116 RID: 278
		public delegate int _CpuidPx(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000029 RID: 41
		// (Invoke) Token: 0x0600011A RID: 282
		public delegate int _Rdtsc(ref uint eax, ref uint edx);

		// Token: 0x0200002A RID: 42
		// (Invoke) Token: 0x0600011E RID: 286
		public delegate int _RdtscTx(ref uint eax, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x0200002B RID: 43
		// (Invoke) Token: 0x06000122 RID: 290
		public delegate int _RdtscPx(ref uint eax, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x0200002C RID: 44
		// (Invoke) Token: 0x06000126 RID: 294
		public delegate byte _ReadIoPortByte(ushort port);

		// Token: 0x0200002D RID: 45
		// (Invoke) Token: 0x0600012A RID: 298
		public delegate ushort _ReadIoPortWord(ushort port);

		// Token: 0x0200002E RID: 46
		// (Invoke) Token: 0x0600012E RID: 302
		public delegate uint _ReadIoPortDword(ushort port);

		// Token: 0x0200002F RID: 47
		// (Invoke) Token: 0x06000132 RID: 306
		public delegate int _ReadIoPortByteEx(ushort port, ref byte value);

		// Token: 0x02000030 RID: 48
		// (Invoke) Token: 0x06000136 RID: 310
		public delegate int _ReadIoPortWordEx(ushort port, ref ushort value);

		// Token: 0x02000031 RID: 49
		// (Invoke) Token: 0x0600013A RID: 314
		public delegate int _ReadIoPortDwordEx(ushort port, ref uint value);

		// Token: 0x02000032 RID: 50
		// (Invoke) Token: 0x0600013E RID: 318
		public delegate void _WriteIoPortByte(ushort port, byte value);

		// Token: 0x02000033 RID: 51
		// (Invoke) Token: 0x06000142 RID: 322
		public delegate void _WriteIoPortWord(ushort port, ushort value);

		// Token: 0x02000034 RID: 52
		// (Invoke) Token: 0x06000146 RID: 326
		public delegate void _WriteIoPortDword(ushort port, uint value);

		// Token: 0x02000035 RID: 53
		// (Invoke) Token: 0x0600014A RID: 330
		public delegate int _WriteIoPortByteEx(ushort port, byte value);

		// Token: 0x02000036 RID: 54
		// (Invoke) Token: 0x0600014E RID: 334
		public delegate int _WriteIoPortWordEx(ushort port, ushort value);

		// Token: 0x02000037 RID: 55
		// (Invoke) Token: 0x06000152 RID: 338
		public delegate int _WriteIoPortDwordEx(ushort port, uint value);

		// Token: 0x02000038 RID: 56
		// (Invoke) Token: 0x06000156 RID: 342
		public delegate void _SetPciMaxBusIndex(byte max);

		// Token: 0x02000039 RID: 57
		// (Invoke) Token: 0x0600015A RID: 346
		public delegate byte _ReadPciConfigByte(uint pciAddress, byte regAddress);

		// Token: 0x0200003A RID: 58
		// (Invoke) Token: 0x0600015E RID: 350
		public delegate ushort _ReadPciConfigWord(uint pciAddress, byte regAddress);

		// Token: 0x0200003B RID: 59
		// (Invoke) Token: 0x06000162 RID: 354
		public delegate uint _ReadPciConfigDword(uint pciAddress, byte regAddress);

		// Token: 0x0200003C RID: 60
		// (Invoke) Token: 0x06000166 RID: 358
		public delegate int _ReadPciConfigByteEx(uint pciAddress, uint regAddress, ref byte value);

		// Token: 0x0200003D RID: 61
		// (Invoke) Token: 0x0600016A RID: 362
		public delegate int _ReadPciConfigWordEx(uint pciAddress, uint regAddress, ref ushort value);

		// Token: 0x0200003E RID: 62
		// (Invoke) Token: 0x0600016E RID: 366
		public delegate int _ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value);

		// Token: 0x0200003F RID: 63
		// (Invoke) Token: 0x06000172 RID: 370
		public delegate void _WritePciConfigByte(uint pciAddress, byte regAddress, byte value);

		// Token: 0x02000040 RID: 64
		// (Invoke) Token: 0x06000176 RID: 374
		public delegate void _WritePciConfigWord(uint pciAddress, byte regAddress, ushort value);

		// Token: 0x02000041 RID: 65
		// (Invoke) Token: 0x0600017A RID: 378
		public delegate void _WritePciConfigDword(uint pciAddress, byte regAddress, uint value);

		// Token: 0x02000042 RID: 66
		// (Invoke) Token: 0x0600017E RID: 382
		public delegate int _WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value);

		// Token: 0x02000043 RID: 67
		// (Invoke) Token: 0x06000182 RID: 386
		public delegate int _WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value);

		// Token: 0x02000044 RID: 68
		// (Invoke) Token: 0x06000186 RID: 390
		public delegate int _WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value);

		// Token: 0x02000045 RID: 69
		// (Invoke) Token: 0x0600018A RID: 394
		public delegate uint _FindPciDeviceById(ushort vendorId, ushort deviceId, byte index);

		// Token: 0x02000046 RID: 70
		// (Invoke) Token: 0x0600018E RID: 398
		public delegate uint _FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index);
	}
}
