# ⚡ OmniBattery

<p align="center">
  <b>现代化 WinUI 3 风格的全能无线外设电量管理中心</b><br>
  轻量 · 极速 · 纯原生 · 零 Web 依赖 · Windows 10/11 Fluent Design
</p>

---

## 📖 简介 (Introduction)

**OmniBattery** 是一款专为 Windows 10/11 设计的纯原生无线外设电量监控工具。采用现代 **WinUI 3 (Fluent Design)** 风格重构，深度适配深色模式（Mica Alt 质感）。

通过调用 Windows 原生 PnP 属性、HID++ 协议与 BLE GATT 服务，无需启动任何臃肿的第三方驱动或 Web 服务，毫秒级读取各类 2.4G 及蓝牙外设的精确电量与充电状态。

---

## ✨ 核心特性 (Features)

- 🎨 **WinUI 3 (Fluent Design) 现代化美学**
  - 深色 Mica Alt 背景（`#202020`）与 1px 微质感边框。
  - 标准 8px 圆角 Fluent 卡片与胶囊微边框按键。
  - 4.2px 高科技纤细进度环（Fluent Progress Ring），直观展示电量变化。

- ⚡ **纯原生绿色便携 (Native & Lightweight)**
  - 基于纯 C# 与原生 Windows API 实现。
  - **不启动任何 Web 服务，不监听任何端口，不弹网页浏览器**。
  - 极速编译，单文件便携绿色运行，内存占用低至几 MB。

- 🎮 **全协议多外设支持 (Broad Hardware Compatibility)**
  - **2.4G 无线外设**：深度支持 2.4G 无线接收器协议（包括达尔优、蚂蚁电竞、罗技 Lightspeed、雷蛇 HyperSpeed、VGN、迈从 MCHOSE、ATK、ROG 等）。
  - **蓝牙外设 (BLE)**：标准 Bluetooth LE 电池服务 (GATT Battery Service) 及 PnP 设备属性。
  - **游戏手柄控制器**：支持 Xbox 无线控制器及 XInput 设备电量识别。
  - **苹果生态设备**：支持 AirPods 等 BLE 广播协议。

- 🛡️ **严格在线过滤 (Strict Offline Filtering)**
  - 仅显示当前处于连接、开机且在线状态的外设。
  - 自动过滤未配对、断开连接或关机休眠的幽灵设备，界面干净清爽。

- 📌 **桌面悬浮胶囊窗 & 系统托盘**
  - **桌面悬浮胶囊**：精致半透明暗色药丸悬浮窗，支持屏幕任意位置拖拽停放，实时紧凑显示外设电量。
  - **系统托盘**：后台静默守护，鼠标悬停即显电量概览，右键提供一键刷新、悬浮窗开关及设置。

- 📊 **游戏与监控联动 (AIDA64 & RTSS)**
  - **AIDA64 传感器导出**：支持实时同步写入 AIDA64 注册表，无缝融入 SensorPanel 监控面板。
  - **RTSS RivaTuner OSD 浮层**：支持实时映射到 RTSS 共享内存，游戏中实时查看鼠标、键盘、耳机剩余电量。

- ⚙️ **双击电量校准与个性化配置**
  - 双击任意设备卡片即可开启电量微调与充电状态校准。
  - 支持设置自动刷新时间间隔（1 秒 ~ 30 秒）、开机自启、关闭到托盘等。

---

## 🛠️ 项目结构 (Project Structure)

```
OmniBattery/
├── build.bat                   # 一键编译批处理
├── build.ps1                   # PowerShell 自动化编译与签名脚本
├── app.ico                     # 应用程序高分辨率图标
├── app.manifest                # Windows DPI 与现代视觉样式清单
├── supported_devices.json      # 支持外设库与型号特征数据库
├── LICENSE                     # MIT 开源协议
├── README.md                   # 项目说明文档
└── src_native/                 # 原生 C# 源代码
    ├── Program.cs              # 程序入口点与单实例 Mutex 调度
    ├── Database/
    │   └── DeviceDatabase.cs   # 官方设备特征库与品牌归类解析
    ├── Models/
    │   └── DeviceInfo.cs       # 设备数据模型与电量格式化
    ├── Scanners/
    │   ├── DeviceScanner.cs    # 统一设备扫描调度器
    │   ├── PnpScanner.cs       # Win32 PnP 原生设备属性扫描
    │   └── XInputScanner.cs    # XInput 游戏控制器扫描
    ├── Services/
    │   ├── Aida64Exporter.cs   # AIDA64 注册表数据导出服务
    │   ├── ConfigService.cs    # 本地配置存储与自启管理
    │   └── RtssExporter.cs     # RTSS 共享内存 OSD 导出服务
    └── UI/
        ├── Theme.cs            # WinUI 3 调色板与圆角绘制引擎
        ├── DeviceCardControl.cs# WinUI 3 Fluent 风格外设卡片控件
        ├── FloatingWidgetWindow.cs # 桌面半透明悬浮药丸窗体
        ├── BatteryCalibrationDialog.cs # 外设电量与充电状态微调窗口
        ├── SettingsDialog.cs   # 参数设置与刷新率调整面板
        ├── TrayManager.cs      # 系统托盘与上下文菜单管理
        └── MainWindow.cs       # WinUI 3 无边框主窗口
```

---

## 🔨 快速编译与构建 (Build)

本项目零额外工具链依赖，只需 Windows 系统自带的 .NET Framework C# 编译器（`csc.exe`）即可瞬间构建：

### 方式 1：双击批处理
双击项目根目录下的 `build.bat`。

### 方式 2：PowerShell 终端构建
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

编译完成后将在根目录生成 `OmniBattery.exe`，并自动完成本地代码签名，直接双击即可运行。

---

## 📄 开源许可证 (License)

本项目采用 [MIT 许可证](LICENSE)。
