using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Token: 0x02000002 RID: 2
public class CoreIndexMapper
{
	// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
	public CoreIndexMapper(List<uint> bigPhysicalIndices, List<uint> bigSmtIndices, List<uint> littleIndices, List<uint> exlittleIndices)
	{
		this._originalToNumberedMap = new Dictionary<int, int>();
		this._numberedToOriginalMap = new Dictionary<int, int>();
		this._bigPhysicalMap = new Dictionary<int, int>();
		this._bigSmtMap = new Dictionary<int, int>();
		this._littleMap = new Dictionary<int, int>();
		this._exlittleMap = new Dictionary<int, int>();
		int num = 0;
		for (int i = 0; i < bigPhysicalIndices.Count; i++)
		{
			int indexFromMask = this.GetIndexFromMask(bigPhysicalIndices[i]);
			this._originalToNumberedMap[indexFromMask] = num;
			this._numberedToOriginalMap[num] = indexFromMask;
			this._bigPhysicalMap[indexFromMask] = i;
			num++;
		}
		for (int j = 0; j < littleIndices.Count; j++)
		{
			int indexFromMask2 = this.GetIndexFromMask(littleIndices[j]);
			this._originalToNumberedMap[indexFromMask2] = num;
			this._numberedToOriginalMap[num] = indexFromMask2;
			this._littleMap[indexFromMask2] = j;
			num++;
		}
		for (int k = 0; k < bigSmtIndices.Count; k++)
		{
			int indexFromMask3 = this.GetIndexFromMask(bigSmtIndices[k]);
			this._originalToNumberedMap[indexFromMask3] = num;
			this._numberedToOriginalMap[num] = indexFromMask3;
			this._bigSmtMap[indexFromMask3] = k;
			num++;
		}
		for (int l = 0; l < exlittleIndices.Count; l++)
		{
			int indexFromMask4 = this.GetIndexFromMask(exlittleIndices[l]);
			this._originalToNumberedMap[indexFromMask4] = num;
			this._numberedToOriginalMap[num] = indexFromMask4;
			this._exlittleMap[indexFromMask4] = l;
			num++;
		}
	}

	// Token: 0x06000002 RID: 2 RVA: 0x000021E4 File Offset: 0x000003E4
	private int GetIndexFromMask(uint mask)
	{
		for (int i = 0; i < 32; i++)
		{
			if ((mask & (1U << i)) != 0U)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x06000003 RID: 3 RVA: 0x0000220C File Offset: 0x0000040C
	public int GetNumberedIndex(int originalIndex)
	{
		int num;
		if (this._originalToNumberedMap.TryGetValue(originalIndex, out num))
		{
			return num;
		}
		return -1;
	}

	// Token: 0x06000004 RID: 4 RVA: 0x0000222C File Offset: 0x0000042C
	public int GetOriginalIndex(int numberedIndex)
	{
		int num;
		if (this._numberedToOriginalMap.TryGetValue(numberedIndex, out num))
		{
			return num;
		}
		return -1;
	}

	// Token: 0x06000005 RID: 5 RVA: 0x0000224C File Offset: 0x0000044C
	public CoreIndexMapper.CoreType GetCoreType(int originalIndex)
	{
		if (this._bigPhysicalMap.ContainsKey(originalIndex))
		{
			return CoreIndexMapper.CoreType.BigPhysical;
		}
		if (this._bigSmtMap.ContainsKey(originalIndex))
		{
			return CoreIndexMapper.CoreType.BigSmt;
		}
		if (this._littleMap.ContainsKey(originalIndex))
		{
			return CoreIndexMapper.CoreType.Little;
		}
		if (this._exlittleMap.ContainsKey(originalIndex))
		{
			return CoreIndexMapper.CoreType.ExLittle;
		}
		return CoreIndexMapper.CoreType.Unknown;
	}

	// Token: 0x06000006 RID: 6 RVA: 0x0000229C File Offset: 0x0000049C
	public int GetIndexInType(int originalIndex)
	{
		int num;
		if (this._bigPhysicalMap.TryGetValue(originalIndex, out num))
		{
			return num;
		}
		if (this._bigSmtMap.TryGetValue(originalIndex, out num))
		{
			return num;
		}
		if (this._littleMap.TryGetValue(originalIndex, out num))
		{
			return num;
		}
		if (this._exlittleMap.TryGetValue(originalIndex, out num))
		{
			return num;
		}
		return -1;
	}

	// Token: 0x06000007 RID: 7 RVA: 0x000022F4 File Offset: 0x000004F4
	public string GetCoreInfo(int originalIndex)
	{
		int numberedIndex = this.GetNumberedIndex(originalIndex);
		CoreIndexMapper.CoreType coreType = this.GetCoreType(originalIndex);
		int indexInType = this.GetIndexInType(originalIndex);
		return string.Format("原始Index:{0} -> 标号:{1}, 类型:{2}, 同类编号:{3}", new object[] { originalIndex, numberedIndex, coreType, indexInType });
	}

	// Token: 0x06000008 RID: 8 RVA: 0x0000234D File Offset: 0x0000054D
	public Dictionary<int, int> GetAllMappings()
	{
		return new Dictionary<int, int>(this._originalToNumberedMap);
	}

	// Token: 0x06000009 RID: 9 RVA: 0x0000235A File Offset: 0x0000055A
	public int GetTotalCoreCount()
	{
		return this._originalToNumberedMap.Count;
	}

	// Token: 0x0600000A RID: 10 RVA: 0x00002367 File Offset: 0x00000567
	[return: TupleElementNames(new string[] { "bigPhysical", "bigSmt", "little", "exlittle" })]
	public ValueTuple<int, int, int, int> GetCoreCounts()
	{
		return new ValueTuple<int, int, int, int>(this._bigPhysicalMap.Count, this._bigSmtMap.Count, this._littleMap.Count, this._exlittleMap.Count);
	}

	// Token: 0x0600000B RID: 11 RVA: 0x0000239C File Offset: 0x0000059C
	public void PrintAllMappings()
	{
		Console.WriteLine("========== 核心索引映射 ==========");
		Console.WriteLine(string.Format("总核心数: {0}", this.GetTotalCoreCount()));
		ValueTuple<int, int, int, int> coreCounts = this.GetCoreCounts();
		Console.WriteLine(string.Format("大核物理核: {0}", coreCounts.Item1));
		Console.WriteLine(string.Format("大核超线程: {0}", coreCounts.Item2));
		Console.WriteLine(string.Format("标准小核: {0}", coreCounts.Item3));
		Console.WriteLine(string.Format("低频小核: {0}", coreCounts.Item4));
		Console.WriteLine();
		Console.WriteLine("--- 映射关系 ---");
		for (int i = 0; i < this.GetTotalCoreCount(); i++)
		{
			int originalIndex = this.GetOriginalIndex(i);
			Console.WriteLine(this.GetCoreInfo(originalIndex));
		}
		Console.WriteLine("================================");
	}

	// Token: 0x04000001 RID: 1
	private Dictionary<int, int> _originalToNumberedMap;

	// Token: 0x04000002 RID: 2
	private Dictionary<int, int> _numberedToOriginalMap;

	// Token: 0x04000003 RID: 3
	private Dictionary<int, int> _bigPhysicalMap;

	// Token: 0x04000004 RID: 4
	private Dictionary<int, int> _bigSmtMap;

	// Token: 0x04000005 RID: 5
	private Dictionary<int, int> _littleMap;

	// Token: 0x04000006 RID: 6
	private Dictionary<int, int> _exlittleMap;

	// Token: 0x02000023 RID: 35
	public enum CoreType
	{
		// Token: 0x0400044B RID: 1099
		BigPhysical,
		// Token: 0x0400044C RID: 1100
		BigSmt,
		// Token: 0x0400044D RID: 1101
		Little,
		// Token: 0x0400044E RID: 1102
		ExLittle,
		// Token: 0x0400044F RID: 1103
		Unknown
	}
}
