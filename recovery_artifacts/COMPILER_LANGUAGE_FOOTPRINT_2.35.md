# 2.35 编译器与语言级别证据

## 结论
- shipped `IntlThrdSchd.pdb` 的原始字符串中，直接可见一条 `CompilerInfo` 指纹：
  - `C# - 5.3.0-2.26153.122+4d3023de605a78ba3e59e50c657eed70f125c68a`
- 同一批原始字符串还直接暴露出：
  - `obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
  - `/LinkInfo`
  - `/TMCache`
  - `/names`
  - `/src/headerblock`
  - 多条 `/src/files/...`
- 这说明 2.35 的 shipped PDB 不只保留了源码文件路径，还保留了更底层的编译器/源码索引痕迹。
- 当前恢复树里原先显式写入的 `LangVersion 12.0` 已被确认过高，主要是由反编译后引入的现代化语法噪音撑出来的，而不是当前 shipped 证据直接要求的语言级别。
- 本轮收敛后的更合理恢复是：
  - 把 17 个 `file-scoped namespace` 文件恢复成旧式 block-scoped namespace
  - 把项目文件的显式语言版本从 `12.0` 下调到 `8.0`

## 直接证据

### 1. PDB 中的编译器指纹
- 对 `IntlThrdSchd.pdb` 做二进制字符串扫描后，可直接看到：
  - `C# - 5.3.0-2.26153.122+4d3023de605a78ba3e59e50c657eed70f125c68a`
- 当前只把这条字符串当作 **compiler fingerprint** 使用，不把它直接等价表述成“已证实的原始 `LangVersion`”。

### 2. PDB 中的生成文件路径
- 同一次扫描还能直接看到：
  - `C:\Users\maxpp\source\repos\IntlThrdPerfSchd3.29\IntlThrdPerfSchd2.22\IntlThrdPerfSchd\obj\Release\net48\.NETFramework,Version=v4.8.AssemblyAttributes.cs`
- 这条路径说明：
  - shipped PDB 文档列表里包含 `obj\Release\net48` 下的生成属性文件
  - 当前恢复树使用 `net48` 目标并生成同名目标框架属性文件，是与 shipped 证据一致的

### 3. PDB 中的源码索引痕迹
- 同一次扫描还能看到：
  - `/LinkInfo`
  - `/TMCache`
  - `/names`
  - `/src/headerblock`
  - 多条 `/src/files/c:\users\maxpp\source\repos\intlthrdperfschd3.29\...`
- 这些痕迹进一步旁证：
  - 当前 `SOURCE_ROOT_HIERARCHY_2.35.md` 里归纳出的单一 project root，不只是普通字符串碰撞
  - PDB 内部还保留了更接近 source indexing 的路径组织信息

## 本轮发现的恢复噪音

### 1. `file-scoped namespace`
- 当前恢复树中原先有 17 个 `.cs` 文件写成了：
  - `namespace Xxx;`
- 这类写法会把工程最低语言级别直接抬到 C# 10。
- 但它们并不是 shipped PDB 暴露的“必须如此”的证据，只是当前反编译/整理后的现代化语法表现。

### 2. 显式 `LangVersion 12.0`
- 当前恢复树之前的 `IntlThrdPerfSchd.csproj` 显式写了：
  - `LangVersion 12.0`
- 这同样不是 shipped 包直接给出的工程事实。
- 它只是为了兼容上述现代化语法噪音而被放大的恢复参数。

## 本轮验证

### 1. 去掉 `file-scoped namespace` 后的语言级别下限探测
- 命令：
  - `dotnet build .\recovered_src_2.35\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -p:LangVersion=7.3 -v minimal -m:1`
- 结果：
  - 失败
- 当前能直接确认的失败原因包括：
  - `Vector<float>` 指针相关写法在 `7.3` 下触发“非托管构造类型”错误
  - `using BinaryWriter writer = ...` / `using TraceEventSession session = ...` 这类 `using` 声明在 `7.3` 下不可用
  - `dimension switch { ... }` 这类 switch expression / recursive pattern 在 `7.3` 下不可用

### 2. `8.0` 探测
- 命令：
  - `dotnet build .\recovered_src_2.35\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -p:LangVersion=8.0 -v minimal -m:1`
- 结果：
  - `179 warnings`
  - `0 errors`

### 3. 当前项目文件的 fresh 构建
- 当前项目文件已显式恢复为：
  - `LangVersion 8.0`
- fresh 验证命令：
  - `dotnet build .\recovered_src_2.35\IntlThrdPerfSchd\IntlThrdPerfSchd.csproj -v minimal -m:1`
- 结果：
  - `179 warnings`
  - `0 errors`

## 对恢复树的影响
- 这轮不是把源码“强行降成 7.3”，而是做了两件更稳妥的事：
  - 去掉明显属于反编译现代化噪音的 `file-scoped namespace`
  - 把项目显式语言版本从 `12.0` 收敛到当前实测最小可用的 `8.0`
- 这样做的意义是：
  - 不再把一个明显偏新的语言级别误写进高保真工程元数据
  - 同时又不为了追求更老的编译器口径，去大规模手改 `using` 声明、switch expression 或 SIMD 指针写法

## 当前口径
- 当前能高置信确认的是：
  - `12.0` 过高
  - `7.3` 过低
  - 当前恢复树在现有语法形态下，`8.0` 是经 fresh build 验证可行的最低显式语言级别
- 当前仍不能高置信确认的是：
  - 原作者原始工程文件里是否显式写过 `LangVersion`
  - 原作者当时真正使用的 Roslyn / Visual Studio 组合是否恰好对应 `8.0`
