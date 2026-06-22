# 2.35 服务名证据链

## 结论
- 当前 2.35 发布包里，`IntlThrdSchd` 是更强、更一致的服务名主线。
- `IntlThrdPerfSchd` 目前主要出现在 `安装服务.bat` / `卸载服务.bat` 这组批处理包装脚本中。
- `Service1.Designer.cs` 里的 `base.ServiceName = "Service1"` 是 shipped 二进制本体里的真实不一致，不是恢复树手工引入的噪音。
- 但结合 Win32 官方语义，这个不一致在当前“单服务 own-process”模型下**未必阻断启动**，因此更准确的定性应是：
  - 发布物内部存在命名不一致
  - 但不应直接上升为“必然运行失败”的矛盾

## 直接支持 `IntlThrdSchd` 的证据
- `install-service.ps1`
  - `$ServiceName = "IntlThrdSchd"`
  - `$ServiceDisplayName = "IntlThrdSchd"`
- `ProjectInstaller.Designer.cs`
  - `serviceInstaller1.DisplayName = "IntlThrdSchd"`
  - `serviceInstaller1.ServiceName = "IntlThrdSchd"`
- `IntlThrdschd.reg`
  - 注册表服务键为 `HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\IntlThrdSchd`
  - `DisplayName = "IntlThrdSchd"`
- `IntlThrdSchd.InstallLog`
  - 多次明确记录“正在安装服务 IntlThrdSchd...”
  - 多次明确记录“已成功安装服务 IntlThrdSchd。”
- `如果无法安装服务，请看这个.txt`
  - 明确提示最终应在服务列表看到名为 `IntlThrdSchd` 的服务正在运行

## 支持 `IntlThrdPerfSchd` 的证据
- `安装服务.bat`
  - `net stop IntlThrdPerfSchd`
  - `sc delete IntlThrdPerfSchd`
  - `sc create IntlThrdPerfSchd binPath= "%~dp0IntlThrdSchd.exe" ...`
- `卸载服务.bat`
  - `net stop IntlThrdPerfSchd`
  - `sc delete IntlThrdPerfSchd`

## `Service1` 设计器字符串
- `Service1.Designer.cs`
  - `base.ServiceName = "Service1"`
- 这一点需要单独看待：
  - 同样的字符串在 2.34 已对齐参考树中也存在
  - shipped 2.35 EXE 的类型级反编译也直接保留了这个字符串，不是当前恢复树手工引入的噪音
  - 但当前发布包的安装链、注册表模板、InstallUtil 日志、`ProjectInstaller` 都更一致地指向 `IntlThrdSchd`
- 因此当前更合理的解释是：
  - `Service1` 设计器字符串本身不足以推翻安装链主线
  - 它更像发布物内部未清理干净的设计器/模板残留，而不是当前主服务名真相

## 框架运行时语义
- 本地 .NET Framework 4.0 的 `System.ServiceProcess.ServiceBase` 反编译结果显示：
  - 框架程序集路径：`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.ServiceProcess.dll`
  - `Run(ServiceBase[] services)` 会先对每个 service 调 `Initialize(...)`
  - `Initialize(...)` 内部执行 `handleName = Marshal.StringToHGlobalUni(ServiceName)`
  - `GetEntry()` 会把这个 `handleName` 填入 `SERVICE_TABLE_ENTRY.name`
  - `ServiceMainCallback` 又会用 `RegisterServiceCtrlHandler(ServiceName, ...)` 或 `RegisterServiceCtrlHandlerEx(ServiceName, ...)`
- 这说明：
  - `ServiceBase.ServiceName` 不是无关字符串
  - 托管层确实会把它传给底层服务调度器与控制处理器注册

## Win32 官方语义
- Microsoft Learn 对 `SERVICE_TABLE_ENTRY` 的说明指出：
  - 当服务类型是 `SERVICE_WIN32_OWN_PROCESS` 时，`lpServiceName` 会被忽略
- Microsoft Learn 对 `RegisterServiceCtrlHandler` / `RegisterServiceCtrlHandlerEx` 的说明指出：
  - 当服务类型是 `SERVICE_WIN32_OWN_PROCESS` 时，传入的服务名不会被 SCM 校验
- 结合本地 `ServiceBase` 反编译可得出更准确的结论：
  - 托管层依然会传 `ServiceName`
  - 但 Win32/SCM 在当前服务类型下，对这两个名字参数采取的是“忽略或不做一致性校验”的语义
- 对应来源：
  - `SERVICE_TABLE_ENTRYA` / `SERVICE_TABLE_ENTRYW`
    - `https://learn.microsoft.com/windows/win32/api/winsvc/ns-winsvc-service_table_entrya`
  - `RegisterServiceCtrlHandlerW`
    - `https://learn.microsoft.com/windows/win32/api/winsvc/nf-winsvc-registerservicectrlhandlerw`
  - `RegisterServiceCtrlHandlerExA`
    - `https://learn.microsoft.com/windows/win32/api/winsvc/nf-winsvc-registerservicectrlhandlerexa`

## 当前能确认的矛盾
- shipped 2.35 EXE 的 `Service1.InitializeComponent()` 写的是：
  - `base.ServiceName = "Service1"`
- shipped 2.35 EXE 的 `ProjectInstaller.InitializeComponent()` 写的是：
  - `serviceInstaller1.ServiceName = "IntlThrdSchd"`
- 再结合安装脚本、注册表模板和 InstallUtil 日志，当前可以确认：
  - 发布包内部同时存在 `Service1` 与 `IntlThrdSchd` 两条服务名路径
- 但同时也需要补一句限定：
  - 这在“单服务 own-process”模型下不等价于“启动一定失败”
  - 它更准确地属于 shipped 包内部命名未完全收敛

## 当前处理原则
- 这是 shipped 二进制本体里的真实不一致，不是当前恢复树私自制造的问题
- 因为用户目标是“尽量接近原作者原始工程”，所以当前不应为了追求表面一致而把 `Service1.Designer.cs` 强行改成 `IntlThrdSchd`
- 更稳妥的做法是：
  - 保留二进制真实还原
  - 把该问题显式记录为“发布物内部未完全收敛的命名冲突，但在当前 Win32 服务模型下未必构成运行阻断”

## 当前对高保真恢复树的影响
- 保持 `ProjectInstaller.Designer.cs` 中的：
  - `DisplayName = "IntlThrdSchd"`
  - `ServiceName = "IntlThrdSchd"`
- 不把批处理脚本里的 `IntlThrdPerfSchd` 反向灌回主工程源码
- 将 `安装服务.bat` / `卸载服务.bat` 视为发布包里的辅助包装脚本分叉证据
