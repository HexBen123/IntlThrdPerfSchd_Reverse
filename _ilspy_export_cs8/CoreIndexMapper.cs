using System;
using System.Collections.Generic;

public class CoreIndexMapper
{
	public enum CoreType
	{
		BigPhysical,
		BigSmt,
		Little,
		ExLittle,
		Unknown
	}

	private Dictionary<int, int> _originalToNumberedMap;

	private Dictionary<int, int> _numberedToOriginalMap;

	private Dictionary<int, int> _bigPhysicalMap;

	private Dictionary<int, int> _bigSmtMap;

	private Dictionary<int, int> _littleMap;

	private Dictionary<int, int> _exlittleMap;

	public CoreIndexMapper(List<uint> bigPhysicalIndices, List<uint> bigSmtIndices, List<uint> littleIndices, List<uint> exlittleIndices)
	{
		_originalToNumberedMap = new Dictionary<int, int>();
		_numberedToOriginalMap = new Dictionary<int, int>();
		_bigPhysicalMap = new Dictionary<int, int>();
		_bigSmtMap = new Dictionary<int, int>();
		_littleMap = new Dictionary<int, int>();
		_exlittleMap = new Dictionary<int, int>();
		int num = 0;
		for (int i = 0; i < bigPhysicalIndices.Count; i++)
		{
			int indexFromMask = GetIndexFromMask(bigPhysicalIndices[i]);
			_originalToNumberedMap[indexFromMask] = num;
			_numberedToOriginalMap[num] = indexFromMask;
			_bigPhysicalMap[indexFromMask] = i;
			num++;
		}
		for (int j = 0; j < littleIndices.Count; j++)
		{
			int indexFromMask2 = GetIndexFromMask(littleIndices[j]);
			_originalToNumberedMap[indexFromMask2] = num;
			_numberedToOriginalMap[num] = indexFromMask2;
			_littleMap[indexFromMask2] = j;
			num++;
		}
		for (int k = 0; k < bigSmtIndices.Count; k++)
		{
			int indexFromMask3 = GetIndexFromMask(bigSmtIndices[k]);
			_originalToNumberedMap[indexFromMask3] = num;
			_numberedToOriginalMap[num] = indexFromMask3;
			_bigSmtMap[indexFromMask3] = k;
			num++;
		}
		for (int l = 0; l < exlittleIndices.Count; l++)
		{
			int indexFromMask4 = GetIndexFromMask(exlittleIndices[l]);
			_originalToNumberedMap[indexFromMask4] = num;
			_numberedToOriginalMap[num] = indexFromMask4;
			_exlittleMap[indexFromMask4] = l;
			num++;
		}
	}

	private int GetIndexFromMask(uint mask)
	{
		for (int i = 0; i < 32; i++)
		{
			if ((mask & (uint)(1 << i)) != 0)
			{
				return i;
			}
		}
		return -1;
	}

	public int GetNumberedIndex(int originalIndex)
	{
		if (_originalToNumberedMap.TryGetValue(originalIndex, out var value))
		{
			return value;
		}
		return -1;
	}

	public int GetOriginalIndex(int numberedIndex)
	{
		if (_numberedToOriginalMap.TryGetValue(numberedIndex, out var value))
		{
			return value;
		}
		return -1;
	}

	public CoreType GetCoreType(int originalIndex)
	{
		if (_bigPhysicalMap.ContainsKey(originalIndex))
		{
			return CoreType.BigPhysical;
		}
		if (_bigSmtMap.ContainsKey(originalIndex))
		{
			return CoreType.BigSmt;
		}
		if (_littleMap.ContainsKey(originalIndex))
		{
			return CoreType.Little;
		}
		if (_exlittleMap.ContainsKey(originalIndex))
		{
			return CoreType.ExLittle;
		}
		return CoreType.Unknown;
	}

	public int GetIndexInType(int originalIndex)
	{
		if (_bigPhysicalMap.TryGetValue(originalIndex, out var value))
		{
			return value;
		}
		if (_bigSmtMap.TryGetValue(originalIndex, out value))
		{
			return value;
		}
		if (_littleMap.TryGetValue(originalIndex, out value))
		{
			return value;
		}
		if (_exlittleMap.TryGetValue(originalIndex, out value))
		{
			return value;
		}
		return -1;
	}

	public string GetCoreInfo(int originalIndex)
	{
		int numberedIndex = GetNumberedIndex(originalIndex);
		CoreType coreType = GetCoreType(originalIndex);
		int indexInType = GetIndexInType(originalIndex);
		return $"原始Index:{originalIndex} -> 标号:{numberedIndex}, 类型:{coreType}, 同类编号:{indexInType}";
	}

	public Dictionary<int, int> GetAllMappings()
	{
		return new Dictionary<int, int>(_originalToNumberedMap);
	}

	public int GetTotalCoreCount()
	{
		return _originalToNumberedMap.Count;
	}

	public (int bigPhysical, int bigSmt, int little, int exlittle) GetCoreCounts()
	{
		return (bigPhysical: _bigPhysicalMap.Count, bigSmt: _bigSmtMap.Count, little: _littleMap.Count, exlittle: _exlittleMap.Count);
	}

	public void PrintAllMappings()
	{
		Console.WriteLine("========== 核心索引映射 ==========");
		Console.WriteLine($"总核心数: {GetTotalCoreCount()}");
		(int, int, int, int) coreCounts = GetCoreCounts();
		Console.WriteLine($"大核物理核: {coreCounts.Item1}");
		Console.WriteLine($"大核超线程: {coreCounts.Item2}");
		Console.WriteLine($"标准小核: {coreCounts.Item3}");
		Console.WriteLine($"低频小核: {coreCounts.Item4}");
		Console.WriteLine();
		Console.WriteLine("--- 映射关系 ---");
		for (int i = 0; i < GetTotalCoreCount(); i++)
		{
			int originalIndex = GetOriginalIndex(i);
			Console.WriteLine(GetCoreInfo(originalIndex));
		}
		Console.WriteLine("================================");
	}
}
