# ButlerDroid

ButlerDroid 是一个只运行在 Android 手机上的本地定时提醒工具。它不依赖服务器、MQTT 或多用户系统，所有任务配置、调度和通知都保存在当前手机内。

## 主要用途

- 创建一次通知、公历定时、农历定时和循环任务。
- 在指定时间通过系统通知、锁屏提醒和应用前台弹窗提醒自己。
- 支持固定分钟或小时间隔，例如每 10 分钟提醒一次。
- 支持提前提醒，并且一个任务可以配置多个提前提醒时间。
- 任务内容支持预生成语音，到点时尽量直接播放提醒音频。
- 支持 JSON 导入导出，方便在不同手机之间迁移任务，重复导入时会按稳定标识覆盖更新。

## 调度类型

- 一次通知：只在指定日期和时间触发一次。
- 公历定时：每年按公历月、日和时间触发。
- 农历定时：每年按农历月、日和时间触发。
- 循环任务：支持每天、每周、每月，以及固定分钟或小时间隔。
- 提前提醒：在目标任务前按天、小时或分钟触发额外提醒。

## 通知方式

- Android 本地系统通知。
- 高优先级通知通道，支持锁屏和 heads-up 提示。
- 应用处于前台时弹出原生对话框，避免系统抑制前台 heads-up。
- 任务保存时预生成语音文件，到点时使用媒体播放器播放。

## 后台调度

- 使用 `AlarmManager.SetExactAndAllowWhileIdle` 进行精确定时。
- 使用前台服务和 `WakeLock` 保持调度服务运行。
- 重启手机后自动恢复调度。
- 提供精确定时闹钟、电池优化、通知权限的引导流程。

Android 后台能力受系统限制，小米、华为等设备可能还需要用户手动允许自启动、后台运行和锁屏通知。

## 数据存储

任务保存在本机 SQLite 数据库中，不经过云端。导出文件格式为 JSON，当前版本为 `2`，其中包含稳定的 `taskKey`，导入时会根据 `taskKey` 覆盖更新同名节点。

## 构建

项目基于 .NET MAUI，Android 目标框架为 `net10.0-android`。

```powershell
dotnet restore ButlerDroid.slnx
dotnet build ButlerDroid\ButlerDroid.csproj -f net10.0-android -p:RuntimeIdentifier=android-arm64
dotnet publish ButlerDroid\ButlerDroid.csproj -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm64 -p:AndroidPackageFormat=apk
```

发布后的 APK 位于：

```text
ButlerDroid/bin/Release/net10.0-android/android-arm64/com.companyname.butlerdroid-Signed.apk
```

## 测试

```powershell
dotnet test tests\ButlerDroid.Core.Tests\ButlerDroid.Core.Tests.csproj
```
