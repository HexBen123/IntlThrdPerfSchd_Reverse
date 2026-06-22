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
		int numberedIndex = 0;
		for (int i = 0; i < bigPhysicalIndices.Count; i++)
		{
			int originalIndex = GetIndexFromMask(bigPhysicalIndices[i]);
			_originalToNumberedMap[originalIndex] = numberedIndex;
			_numberedToOriginalMap[numberedIndex] = originalIndex;
			_bigPhysicalMap[originalIndex] = i;
			numberedIndex++;
		}
		for (int j = 0; j < littleIndices.Count; j++)
		{
			int originalIndex2 = GetIndexFromMask(littleIndices[j]);
			_originalToNumberedMap[originalIndex2] = numberedIndex;
			_numberedToOriginalMap[numberedIndex] = originalIndex2;
			_littleMap[originalIndex2] = j;
			numberedIndex++;
		}
		for (int k = 0; k < bigSmtIndices.Count; k++)
		{
			int originalIndex3 = GetIndexFromMask(bigSmtIndices[k]);
			_originalToNumberedMap[originalIndex3] = numberedIndex;
			_numberedToOriginalMap[numberedIndex] = originalIndex3;
			_bigSmtMap[originalIndex3] = k;
			numberedIndex++;
		}
		for (int l = 0; l < exlittleIndices.Count; l++)
		{
			int originalIndex4 = GetIndexFromMask(exlittleIndices[l]);
			_originalToNumberedMap[originalIndex4] = numberedIndex;
			_numberedToOriginalMap[numberedIndex] = originalIndex4;
			_exlittleMap[originalIndex4] = l;
			numberedIndex++;
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
		if (_originalToNumberedMap.TryGetValue(originalIndex, out var numberedIndex))
		{
			return numberedIndex;
		}
		return -1;
	}

	public int GetOriginalIndex(int numberedIndex)
	{
		if (_numberedToOriginalMap.TryGetValue(numberedIndex, out var originalIndex))
		{
			return originalIndex;
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
		if (_bigPhysicalMap.TryGetValue(originalIndex, out var idx))
		{
			return idx;
		}
		if (_bigSmtMap.TryGetValue(originalIndex, out idx))
		{
			return idx;
		}
		if (_littleMap.TryGetValue(originalIndex, out idx))
		{
			return idx;
		}
		if (_exlittleMap.TryGetValue(originalIndex, out idx))
		{
			return idx;
		}
		return -1;
	}

	public string GetCoreInfo(int originalIndex)
	{
		int numbered = GetNumberedIndex(originalIndex);
		CoreType type = GetCoreType(originalIndex);
		int idxInType = GetIndexInType(originalIndex);
		return $"原始Index:{originalIndex} -> 标号:{numbered}, 类型:{type}, 同类编号:{idxInType}";
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
		(int, int, int, int) counts = GetCoreCounts();
		Console.WriteLine($"大核物理核: {counts.Item1}");
		Console.WriteLine($"大核超线程: {counts.Item2}");
		Console.WriteLine($"标准小核: {counts.Item3}");
		Console.WriteLine($"低频小核: {counts.Item4}");
		Console.WriteLine();
		Console.WriteLine("--- 映射关系 ---");
		for (int numbered = 0; numbered < GetTotalCoreCount(); numbered++)
		{
			int original = GetOriginalIndex(numbered);
			Console.WriteLine(GetCoreInfo(original));
		}
		Console.WriteLine("================================");
	}
}
