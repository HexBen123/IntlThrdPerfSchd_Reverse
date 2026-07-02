using System;
using System.Runtime.InteropServices;

namespace OpenLibSys
{
	// Token: 0x02000003 RID: 3
	public class Ols : IDisposable
	{
		// Token: 0x0600000C RID: 12 RVA: 0x0000247A File Offset: 0x0000067A
		public uint PciBusDevFunc(uint bus, uint dev, uint func)
		{
			return ((bus & 255U) << 8) | ((dev & 31U) << 3) | (func & 7U);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002490 File Offset: 0x00000690
		public uint PciGetBus(uint address)
		{
			return (address >> 8) & 255U;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000249B File Offset: 0x0000069B
		public uint PciGetDev(uint address)
		{
			return (address >> 3) & 31U;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000024A3 File Offset: 0x000006A3
		public uint PciGetFunc(uint address)
		{
			return address & 7U;
		}

		// Token: 0x06000010 RID: 16
		[DllImport("kernel32")]
		public static extern IntPtr LoadLibrary(string lpFileName);

		// Token: 0x06000011 RID: 17
		[DllImport("kernel32", SetLastError = true)]
		private static extern bool FreeLibrary(IntPtr hModule);

		// Token: 0x06000012 RID: 18
		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		// Token: 0x06000013 RID: 19 RVA: 0x000024A8 File Offset: 0x000006A8
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

		// Token: 0x06000014 RID: 20 RVA: 0x00002DFF File Offset: 0x00000FFF
		public uint GetStatus()
		{
			return this.status;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002E07 File Offset: 0x00001007
		public void Dispose()
		{
			if (this.module != IntPtr.Zero)
			{
				this.DeinitializeOls();
				Ols.FreeLibrary(this.module);
				this.module = IntPtr.Zero;
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002E40 File Offset: 0x00001040
		public Delegate GetDelegate(string procName, Type delegateType)
		{
			IntPtr procAddress = Ols.GetProcAddress(this.module, procName);
			if (procAddress != IntPtr.Zero)
			{
				return Marshal.GetDelegateForFunctionPointer(procAddress, delegateType);
			}
			throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
		}

		// Token: 0x04000007 RID: 7
		private const string dllNameX64 = "WinRing0x64.dll";

		// Token: 0x04000008 RID: 8
		private const string dllName = "WinRing0.dll";

		// Token: 0x04000009 RID: 9
		private IntPtr module = IntPtr.Zero;

		// Token: 0x0400000A RID: 10
		private uint status;

		// Token: 0x0400000B RID: 11
		public Ols._GetDllStatus GetDllStatus;

		// Token: 0x0400000C RID: 12
		public Ols._GetDriverType GetDriverType;

		// Token: 0x0400000D RID: 13
		public Ols._GetDllVersion GetDllVersion;

		// Token: 0x0400000E RID: 14
		public Ols._GetDriverVersion GetDriverVersion;

		// Token: 0x0400000F RID: 15
		public Ols._InitializeOls InitializeOls;

		// Token: 0x04000010 RID: 16
		public Ols._DeinitializeOls DeinitializeOls;

		// Token: 0x04000011 RID: 17
		public Ols._IsCpuid IsCpuid;

		// Token: 0x04000012 RID: 18
		public Ols._IsMsr IsMsr;

		// Token: 0x04000013 RID: 19
		public Ols._IsTsc IsTsc;

		// Token: 0x04000014 RID: 20
		public Ols._Hlt Hlt;

		// Token: 0x04000015 RID: 21
		public Ols._HltTx HltTx;

		// Token: 0x04000016 RID: 22
		public Ols._HltPx HltPx;

		// Token: 0x04000017 RID: 23
		public Ols._Rdmsr Rdmsr;

		// Token: 0x04000018 RID: 24
		public Ols._RdmsrTx RdmsrTx;

		// Token: 0x04000019 RID: 25
		public Ols._RdmsrPx RdmsrPx;

		// Token: 0x0400001A RID: 26
		public Ols._Wrmsr Wrmsr;

		// Token: 0x0400001B RID: 27
		public Ols._WrmsrTx WrmsrTx;

		// Token: 0x0400001C RID: 28
		public Ols._WrmsrPx WrmsrPx;

		// Token: 0x0400001D RID: 29
		public Ols._Rdpmc Rdpmc;

		// Token: 0x0400001E RID: 30
		public Ols._RdpmcTx RdpmcTx;

		// Token: 0x0400001F RID: 31
		public Ols._RdpmcPx RdpmcPx;

		// Token: 0x04000020 RID: 32
		public Ols._Cpuid Cpuid;

		// Token: 0x04000021 RID: 33
		public Ols._CpuidTx CpuidTx;

		// Token: 0x04000022 RID: 34
		public Ols._CpuidPx CpuidPx;

		// Token: 0x04000023 RID: 35
		public Ols._Rdtsc Rdtsc;

		// Token: 0x04000024 RID: 36
		public Ols._RdtscTx RdtscTx;

		// Token: 0x04000025 RID: 37
		public Ols._RdtscPx RdtscPx;

		// Token: 0x04000026 RID: 38
		public Ols._ReadIoPortByte ReadIoPortByte;

		// Token: 0x04000027 RID: 39
		public Ols._ReadIoPortWord ReadIoPortWord;

		// Token: 0x04000028 RID: 40
		public Ols._ReadIoPortDword ReadIoPortDword;

		// Token: 0x04000029 RID: 41
		public Ols._ReadIoPortByteEx ReadIoPortByteEx;

		// Token: 0x0400002A RID: 42
		public Ols._ReadIoPortWordEx ReadIoPortWordEx;

		// Token: 0x0400002B RID: 43
		public Ols._ReadIoPortDwordEx ReadIoPortDwordEx;

		// Token: 0x0400002C RID: 44
		public Ols._WriteIoPortByte WriteIoPortByte;

		// Token: 0x0400002D RID: 45
		public Ols._WriteIoPortWord WriteIoPortWord;

		// Token: 0x0400002E RID: 46
		public Ols._WriteIoPortDword WriteIoPortDword;

		// Token: 0x0400002F RID: 47
		public Ols._WriteIoPortByteEx WriteIoPortByteEx;

		// Token: 0x04000030 RID: 48
		public Ols._WriteIoPortWordEx WriteIoPortWordEx;

		// Token: 0x04000031 RID: 49
		public Ols._WriteIoPortDwordEx WriteIoPortDwordEx;

		// Token: 0x04000032 RID: 50
		public Ols._SetPciMaxBusIndex SetPciMaxBusIndex;

		// Token: 0x04000033 RID: 51
		public Ols._ReadPciConfigByte ReadPciConfigByte;

		// Token: 0x04000034 RID: 52
		public Ols._ReadPciConfigWord ReadPciConfigWord;

		// Token: 0x04000035 RID: 53
		public Ols._ReadPciConfigDword ReadPciConfigDword;

		// Token: 0x04000036 RID: 54
		public Ols._ReadPciConfigByteEx ReadPciConfigByteEx;

		// Token: 0x04000037 RID: 55
		public Ols._ReadPciConfigWordEx ReadPciConfigWordEx;

		// Token: 0x04000038 RID: 56
		public Ols._ReadPciConfigDwordEx ReadPciConfigDwordEx;

		// Token: 0x04000039 RID: 57
		public Ols._WritePciConfigByte WritePciConfigByte;

		// Token: 0x0400003A RID: 58
		public Ols._WritePciConfigWord WritePciConfigWord;

		// Token: 0x0400003B RID: 59
		public Ols._WritePciConfigDword WritePciConfigDword;

		// Token: 0x0400003C RID: 60
		public Ols._WritePciConfigByteEx WritePciConfigByteEx;

		// Token: 0x0400003D RID: 61
		public Ols._WritePciConfigWordEx WritePciConfigWordEx;

		// Token: 0x0400003E RID: 62
		public Ols._WritePciConfigDwordEx WritePciConfigDwordEx;

		// Token: 0x0400003F RID: 63
		public Ols._FindPciDeviceById FindPciDeviceById;

		// Token: 0x04000040 RID: 64
		public Ols._FindPciDeviceByClass FindPciDeviceByClass;

		// Token: 0x02000026 RID: 38
		public enum Status
		{
			// Token: 0x040004C3 RID: 1219
			NO_ERROR,
			// Token: 0x040004C4 RID: 1220
			DLL_NOT_FOUND,
			// Token: 0x040004C5 RID: 1221
			DLL_INCORRECT_VERSION,
			// Token: 0x040004C6 RID: 1222
			DLL_INITIALIZE_ERROR
		}

		// Token: 0x02000027 RID: 39
		public enum OlsDllStatus
		{
			// Token: 0x040004C8 RID: 1224
			OLS_DLL_NO_ERROR,
			// Token: 0x040004C9 RID: 1225
			OLS_DLL_UNSUPPORTED_PLATFORM,
			// Token: 0x040004CA RID: 1226
			OLS_DLL_DRIVER_NOT_LOADED,
			// Token: 0x040004CB RID: 1227
			OLS_DLL_DRIVER_NOT_FOUND,
			// Token: 0x040004CC RID: 1228
			OLS_DLL_DRIVER_UNLOADED,
			// Token: 0x040004CD RID: 1229
			OLS_DLL_DRIVER_NOT_LOADED_ON_NETWORK,
			// Token: 0x040004CE RID: 1230
			OLS_DLL_UNKNOWN_ERROR = 9
		}

		// Token: 0x02000028 RID: 40
		public enum OlsDriverType
		{
			// Token: 0x040004D0 RID: 1232
			OLS_DRIVER_TYPE_UNKNOWN,
			// Token: 0x040004D1 RID: 1233
			OLS_DRIVER_TYPE_WIN_9X,
			// Token: 0x040004D2 RID: 1234
			OLS_DRIVER_TYPE_WIN_NT,
			// Token: 0x040004D3 RID: 1235
			OLS_DRIVER_TYPE_WIN_NT4,
			// Token: 0x040004D4 RID: 1236
			OLS_DRIVER_TYPE_WIN_NT_X64,
			// Token: 0x040004D5 RID: 1237
			OLS_DRIVER_TYPE_WIN_NT_IA64
		}

		// Token: 0x02000029 RID: 41
		public enum OlsErrorPci : uint
		{
			// Token: 0x040004D7 RID: 1239
			OLS_ERROR_PCI_BUS_NOT_EXIST = 3758096385U,
			// Token: 0x040004D8 RID: 1240
			OLS_ERROR_PCI_NO_DEVICE,
			// Token: 0x040004D9 RID: 1241
			OLS_ERROR_PCI_WRITE_CONFIG,
			// Token: 0x040004DA RID: 1242
			OLS_ERROR_PCI_READ_CONFIG
		}

		// Token: 0x0200002A RID: 42
		// (Invoke) Token: 0x06000263 RID: 611
		public delegate uint _GetDllStatus();

		// Token: 0x0200002B RID: 43
		// (Invoke) Token: 0x06000267 RID: 615
		public delegate uint _GetDllVersion(ref byte major, ref byte minor, ref byte revision, ref byte release);

		// Token: 0x0200002C RID: 44
		// (Invoke) Token: 0x0600026B RID: 619
		public delegate uint _GetDriverVersion(ref byte major, ref byte minor, ref byte revision, ref byte release);

		// Token: 0x0200002D RID: 45
		// (Invoke) Token: 0x0600026F RID: 623
		public delegate uint _GetDriverType();

		// Token: 0x0200002E RID: 46
		// (Invoke) Token: 0x06000273 RID: 627
		public delegate int _InitializeOls();

		// Token: 0x0200002F RID: 47
		// (Invoke) Token: 0x06000277 RID: 631
		public delegate void _DeinitializeOls();

		// Token: 0x02000030 RID: 48
		// (Invoke) Token: 0x0600027B RID: 635
		public delegate int _IsCpuid();

		// Token: 0x02000031 RID: 49
		// (Invoke) Token: 0x0600027F RID: 639
		public delegate int _IsMsr();

		// Token: 0x02000032 RID: 50
		// (Invoke) Token: 0x06000283 RID: 643
		public delegate int _IsTsc();

		// Token: 0x02000033 RID: 51
		// (Invoke) Token: 0x06000287 RID: 647
		public delegate int _Hlt();

		// Token: 0x02000034 RID: 52
		// (Invoke) Token: 0x0600028B RID: 651
		public delegate int _HltTx(UIntPtr threadAffinityMask);

		// Token: 0x02000035 RID: 53
		// (Invoke) Token: 0x0600028F RID: 655
		public delegate int _HltPx(UIntPtr processAffinityMask);

		// Token: 0x02000036 RID: 54
		// (Invoke) Token: 0x06000293 RID: 659
		public delegate int _Rdmsr(uint index, ref uint eax, ref uint edx);

		// Token: 0x02000037 RID: 55
		// (Invoke) Token: 0x06000297 RID: 663
		public delegate int _RdmsrTx(uint index, ref uint eax, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x02000038 RID: 56
		// (Invoke) Token: 0x0600029B RID: 667
		public delegate int _RdmsrPx(uint index, ref uint eax, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000039 RID: 57
		// (Invoke) Token: 0x0600029F RID: 671
		public delegate int _Wrmsr(uint index, uint eax, uint edx);

		// Token: 0x0200003A RID: 58
		// (Invoke) Token: 0x060002A3 RID: 675
		public delegate int _WrmsrTx(uint index, uint eax, uint edx, UIntPtr threadAffinityMask);

		// Token: 0x0200003B RID: 59
		// (Invoke) Token: 0x060002A7 RID: 679
		public delegate int _WrmsrPx(uint index, uint eax, uint edx, UIntPtr processAffinityMask);

		// Token: 0x0200003C RID: 60
		// (Invoke) Token: 0x060002AB RID: 683
		public delegate int _Rdpmc(uint index, ref uint eax, ref uint edx);

		// Token: 0x0200003D RID: 61
		// (Invoke) Token: 0x060002AF RID: 687
		public delegate int _RdpmcTx(uint index, ref uint eax, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x0200003E RID: 62
		// (Invoke) Token: 0x060002B3 RID: 691
		public delegate int _RdpmcPx(uint index, ref uint eax, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x0200003F RID: 63
		// (Invoke) Token: 0x060002B7 RID: 695
		public delegate int _Cpuid(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx);

		// Token: 0x02000040 RID: 64
		// (Invoke) Token: 0x060002BB RID: 699
		public delegate int _CpuidTx(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x02000041 RID: 65
		// (Invoke) Token: 0x060002BF RID: 703
		public delegate int _CpuidPx(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000042 RID: 66
		// (Invoke) Token: 0x060002C3 RID: 707
		public delegate int _Rdtsc(ref uint eax, ref uint edx);

		// Token: 0x02000043 RID: 67
		// (Invoke) Token: 0x060002C7 RID: 711
		public delegate int _RdtscTx(ref uint eax, ref uint edx, UIntPtr threadAffinityMask);

		// Token: 0x02000044 RID: 68
		// (Invoke) Token: 0x060002CB RID: 715
		public delegate int _RdtscPx(ref uint eax, ref uint edx, UIntPtr processAffinityMask);

		// Token: 0x02000045 RID: 69
		// (Invoke) Token: 0x060002CF RID: 719
		public delegate byte _ReadIoPortByte(ushort port);

		// Token: 0x02000046 RID: 70
		// (Invoke) Token: 0x060002D3 RID: 723
		public delegate ushort _ReadIoPortWord(ushort port);

		// Token: 0x02000047 RID: 71
		// (Invoke) Token: 0x060002D7 RID: 727
		public delegate uint _ReadIoPortDword(ushort port);

		// Token: 0x02000048 RID: 72
		// (Invoke) Token: 0x060002DB RID: 731
		public delegate int _ReadIoPortByteEx(ushort port, ref byte value);

		// Token: 0x02000049 RID: 73
		// (Invoke) Token: 0x060002DF RID: 735
		public delegate int _ReadIoPortWordEx(ushort port, ref ushort value);

		// Token: 0x0200004A RID: 74
		// (Invoke) Token: 0x060002E3 RID: 739
		public delegate int _ReadIoPortDwordEx(ushort port, ref uint value);

		// Token: 0x0200004B RID: 75
		// (Invoke) Token: 0x060002E7 RID: 743
		public delegate void _WriteIoPortByte(ushort port, byte value);

		// Token: 0x0200004C RID: 76
		// (Invoke) Token: 0x060002EB RID: 747
		public delegate void _WriteIoPortWord(ushort port, ushort value);

		// Token: 0x0200004D RID: 77
		// (Invoke) Token: 0x060002EF RID: 751
		public delegate void _WriteIoPortDword(ushort port, uint value);

		// Token: 0x0200004E RID: 78
		// (Invoke) Token: 0x060002F3 RID: 755
		public delegate int _WriteIoPortByteEx(ushort port, byte value);

		// Token: 0x0200004F RID: 79
		// (Invoke) Token: 0x060002F7 RID: 759
		public delegate int _WriteIoPortWordEx(ushort port, ushort value);

		// Token: 0x02000050 RID: 80
		// (Invoke) Token: 0x060002FB RID: 763
		public delegate int _WriteIoPortDwordEx(ushort port, uint value);

		// Token: 0x02000051 RID: 81
		// (Invoke) Token: 0x060002FF RID: 767
		public delegate void _SetPciMaxBusIndex(byte max);

		// Token: 0x02000052 RID: 82
		// (Invoke) Token: 0x06000303 RID: 771
		public delegate byte _ReadPciConfigByte(uint pciAddress, byte regAddress);

		// Token: 0x02000053 RID: 83
		// (Invoke) Token: 0x06000307 RID: 775
		public delegate ushort _ReadPciConfigWord(uint pciAddress, byte regAddress);

		// Token: 0x02000054 RID: 84
		// (Invoke) Token: 0x0600030B RID: 779
		public delegate uint _ReadPciConfigDword(uint pciAddress, byte regAddress);

		// Token: 0x02000055 RID: 85
		// (Invoke) Token: 0x0600030F RID: 783
		public delegate int _ReadPciConfigByteEx(uint pciAddress, uint regAddress, ref byte value);

		// Token: 0x02000056 RID: 86
		// (Invoke) Token: 0x06000313 RID: 787
		public delegate int _ReadPciConfigWordEx(uint pciAddress, uint regAddress, ref ushort value);

		// Token: 0x02000057 RID: 87
		// (Invoke) Token: 0x06000317 RID: 791
		public delegate int _ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value);

		// Token: 0x02000058 RID: 88
		// (Invoke) Token: 0x0600031B RID: 795
		public delegate void _WritePciConfigByte(uint pciAddress, byte regAddress, byte value);

		// Token: 0x02000059 RID: 89
		// (Invoke) Token: 0x0600031F RID: 799
		public delegate void _WritePciConfigWord(uint pciAddress, byte regAddress, ushort value);

		// Token: 0x0200005A RID: 90
		// (Invoke) Token: 0x06000323 RID: 803
		public delegate void _WritePciConfigDword(uint pciAddress, byte regAddress, uint value);

		// Token: 0x0200005B RID: 91
		// (Invoke) Token: 0x06000327 RID: 807
		public delegate int _WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value);

		// Token: 0x0200005C RID: 92
		// (Invoke) Token: 0x0600032B RID: 811
		public delegate int _WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value);

		// Token: 0x0200005D RID: 93
		// (Invoke) Token: 0x0600032F RID: 815
		public delegate int _WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value);

		// Token: 0x0200005E RID: 94
		// (Invoke) Token: 0x06000333 RID: 819
		public delegate uint _FindPciDeviceById(ushort vendorId, ushort deviceId, byte index);

		// Token: 0x0200005F RID: 95
		// (Invoke) Token: 0x06000337 RID: 823
		public delegate uint _FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index);
	}
}
