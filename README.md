# 世界时钟动态壁纸

一个 Windows 10/11 桌面时钟小工具：把时间放在桌面图标层之上、普通窗口之下，并随当地时间在清晨、白天、黄昏、深夜之间渐变配色，不遮挡正在使用的窗口。

## 功能

- 显示指定时区的大号时间、日期与星期
- 国家 · 城市 标签（如 `UK · LONDON`）
- 根据当地时间自动切换天空配色
- 桌面层置顶，不抢焦点、不显示在任务栏
- 通过 `settings.ini` 配置，无需设置界面
- 可选开机自启（当前用户注册表，无需管理员）

## 运行要求

- Windows 10 / 11
- .NET Framework 4.8（Windows 10 1903+ 和 Windows 11 已内置）

## 使用

1. 从 Releases 下载压缩包，解压到 `C:\Tools\WorldClock`
2. 双击 `WorldClockWallpaper.exe` 运行
3. 修改 `settings.ini` 后重启程序生效

| 键 | 说明 | 示例 |
|---|---|---|
| `TimeZone` | Windows 时区 ID | `GMT Standard Time` |
| `Country` | 国家/地区简称，留空则不显示 | `UK` |
| `City` | 城市，留空则只显示国家 | `LONDON` |

常用时区：`China Standard Time`（北京）、`Tokyo Standard Time`（东京）、`Eastern Standard Time`（纽约）、`UTC`（世界标准时）。

开机自启：双击 `WorldClock_AutoStart_Enable.reg`；取消自启双击 `WorldClock_AutoStart_Disable.reg`。

> 自启脚本按固定路径 `C:\Tools\WorldClock\WorldClockWallpaper.exe` 注册。如果程序放在其他目录，请先修改 reg 文件里的路径。

## 构建

```powershell
.\build.ps1
# 输出：dist\WorldClockWallpaper.exe
```

只依赖 Windows 自带的 .NET Framework 编译器（`csc.exe`），无需安装 Visual Studio。

## 说明

- exe 未做代码签名，首次运行可能触发 SmartScreen 提示
- 版本号见 exe 文件属性，Release 以标签形式标记（如 `v1.5`）
