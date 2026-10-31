# DeepSeek 池余额桌宠 · DeepSeekPet

一个 Windows 桌面悬浮宠物：用 [mdd111/deepseek-dafeiyu-pet](https://github.com/mdd111/deepseek-dafeiyu-pet) 的 sprites 形象，实时盯着 DeepSeek 池里 40 个账号的积分余额，**余额一掉就提醒你**。

单文件 exe，.NET Framework 4.8，无需 Python / Electron / 运行库。

<p align="center">
  <img src="docs/preview.png" width="380" alt="桌宠预览">
</p>

## 三套皮肤

右键 → 外观 一键切换，配置写进 `config.json` 的 `theme`。

<p align="center">
  <img src="docs/skins.png" width="100%" alt="奶白手账 / 深蓝夜幕 / 极简无框">
</p>

| 皮肤 | 说明 |
| --- | --- |
| `soft` 奶白手账（默认） | 半透明白卡 + 顶部蕾丝圆点边 + 深蓝数字 + 矢量小鲸鱼 |
| `night` 深蓝夜幕 | 深蓝玻璃卡 + 星光点缀 + 蓝紫发光字 |
| `minimal` 极简无框 | 去掉卡片，白描边深蓝字直接浮在桌面 |

## 功能

- **实时余额**：每 30 秒拉一次 `/status`，把 `accounts[].credits` 求和显示
- **消耗提示**：与上一轮比较，掉 N 就弹 `-N` 粉字 + 宠物抖动 + 换"吃饱了"表情 + 漫画气泡台词 + 提示音 + 记日志
- **补充提示**：余额涨了显示 `+N` + 唱歌表情
- **到期提醒**：`credits_expiry_buckets` 里 N 天内到期的额度合计，超阈值提醒
- **池健康**：`healthy / total` 文字显示，冷却 / 禁用号也会提醒
- **低余额告警**：低于 `lowBalanceWarn` 换警告表情（10 分钟不重复）
- **账号明细**：双击打开可排序表格，空号标红
- **CSV 日志**：`logs/balance-yyyyMMdd.csv`，每轮一行
- **透明置顶**：分层窗口逐像素 alpha，透明区域点击直接穿透，不挡桌面
- **拖动记忆位置**、托盘图标、开机自启脚本
- **顺滑动作**：时间驱动的呼吸 / 摇摆、状态切换 0.16 秒交叉淡化、拖动倾斜与惯性、静止自动降帧（CPU 约 0.6%）
- **主动搭话**：每 38 分钟左右随机说一句（余额、喝水、别盯屏幕），累计消耗每满 1000 点额外"心疼"提醒

## 快速开始

1. 下载本仓库（或 Releases 里的 zip）解压到任意目录
2. 复制 `config.example.json` 为 `config.json`，填入自己的 `apiUrl` 和 `token`
3. 双击 `启动桌宠.vbs`

> 也可以用 `调试启动.bat` 看控制台输出；`调试启动.bat --demo` 会在启动 4 秒后模拟一次 `-1234` 消耗，方便看提示效果。
>
> 三个 `.vbs` 脚本里的提示文字是英文的：VBScript 按系统 ANSI 代码页读取脚本，带中文注释的 `.vbs` 在部分机器上会抛 `800A0409 未结束的字符串常量`。脚本文件名仍是中文，内容保持纯 ASCII 以兼容所有代码页。

>`安装开机自启.vbs` / `卸载开机自启.vbs` 会在当前用户的启动文件夹里增删快捷方式。

## 配置 `config.json`

```json
{
  "apiUrl": "http://your-host/status",
  "token": "YOUR_TOKEN",
  "pollSeconds": 30,
  "zoom": 1.2,
  "topMost": true,
  "showHud": true,
  "sound": true,
  "clickBanter": true,
  "lowBalanceWarn": 20000,
  "expiryWarnDays": 7,
  "timeoutSeconds": 10,
  "theme": "soft"
}
```

| 字段 | 说明 |
| --- | --- |
| `apiUrl` / `token` | 状态接口和 Bearer Token（必填） |
| `pollSeconds` | 轮询间隔，5–3600 秒 |
| `timeoutSeconds` | 单次请求超时 |
| `zoom` | 形象缩放 0.5–2.0，也可右键「大小」 |
| `topMost` | 始终置顶 |
| `showHud` | 是否显示余额面板 |
| `sound` / `clickBanter` | 提示音 / 单击是否说话 |
| `lowBalanceWarn` | 低余额告警阈值 |
| `expiryWarnDays` | 临期提醒天数 |
| `theme` | `soft` / `night` / `minimal` |
| `x` / `y` | 窗口位置，删掉两行即回右下角 |

接口返回结构需要包含：

```json
{
  "total": 40,
  "healthy": 40,
  "cooling": 0,
  "disabled": 0,
  "credit_floor": 100,
  "accounts": [
    {
      "uid": "…", "nickname": "…",
      "credits": 2708, "credits_total": 2709,
      "credits_earliest_expiry": "2026-11-25T13:26:07+08:00",
      "credits_earliest_remaining": 1600,
      "credits_expiry_buckets": [{ "date": "2026-11-25", "expires_at": "…", "remain": 1600 }],
      "cooling": false, "disabled": false,
      "token_usage": { "total_tokens": 18986, "request_count": 6 }
    }
  ]
}
```

## 交互

| 操作 | 效果 |
| --- | --- |
| 左键拖动 | 移动，带倾斜物理、松手惯性与边缘吸附 |
| 单击 | 撒娇：换表情 + 随机台词 |
| 双击 | 账号明细表格 |
| **按住不动 0.8 秒** | 被捏脸：哭脸 + "会变形的啦" |
| **鼠标停在头顶 0.7 秒** | 摸头：开心表情 + "再摸一下~" |
| 鼠标划过身体 | 轻微放大 1.035 倍 |
| 右键 | 刷新 / 明细 / 面板开关 / 置顶 / 提示音 / 大小 / 外观 / 打开目录 / 退出 |
| 托盘图标 | 双击显示隐藏，右键同菜单 |
| 透明区域 | 点击穿透到下层窗口 |

## 构建

```powershell
# 需要 .NET Framework 4.x（Windows 自带），无需 VS
powershell -ExecutionPolicy Bypass -File build.ps1
```

`build.ps1` 会调用 `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` 编译 `src/*.cs`，输出 `DeepSeekPet.exe`（带 `assets/icon.ico`）。

## 目录

```
.
├─ DeepSeekPet.exe          编译产物（winexe，无控制台）
├─ config.example.json      配置模板
├─ src/
│  ├─ Program.cs            入口、单实例、崩溃日志
│  ├─ PetWindow.cs          分层窗口、绘制、动画、状态机、菜单
│  ├─ LayeredSurface.cs     DIB + UpdateLayeredWindow 逐像素 alpha
│  ├─ StatusClient.cs       HTTP 拉取 + 快照解析
│  ├─ Json.cs               零依赖迷你 JSON 解析器
│  ├─ Config.cs             配置读写
│  ├─ DetailsForm.cs        账号明细表
│  ├─ Native.cs             P/Invoke 声明
│  └─ Dbg.cs                调试日志
├─ assets/
│  ├─ dsh/                  动作帧：idle working done error sing approval start_1..3 tool_*
│  ├─ whale/                正/侧/背面大图
│  ├─ sfx/                  提示音
│  └─ icon.ico, icon.png
├─ build.ps1
├─ 启动桌宠.vbs  调试启动.bat
├─ 安装开机自启.vbs  卸载开机自启.vbs
└─ docs/                    截图
```

## 实现要点

- **透明**：`WS_EX_LAYERED` + `UpdateLayeredWindow`，先合成到 `Format32bppPArgb` 位图，再拷进 DIB Section，PNG 边缘是真透明（不是色键）
- **点击穿透**：`WM_NCHITTEST` 查 4×4 采样的 alpha 掩码，透明处返回 `HTTRANSPARENT`
- **拖动**：定时器读 `GetAsyncKeyState(VK_LBUTTON)` 跟踪，避免丢失鼠标捕获；松手按位移区分拖动与点击
- **零依赖**：HTTP 用 `HttpClient`（显式 `UseProxy = false`，避免首次请求代理探测卡 30 秒），JSON 自己解析
- **绘制**：GDI+ 合成 → 20 fps 重绘；文字 `AntiAlias`，气泡/飘字带描边

## 素材与许可

- 桌宠形象、动作帧来自 [mdd111/deepseek-dafeiyu-pet](https://github.com/mdd111/deepseek-dafeiyu-pet) 的 `sprites/` 目录，按原仓库许可使用
- 代码：MIT，见 [LICENSE](LICENSE)
