# EasyIP

Windows 便携 IP 配置小工具：快速改网卡静态 IP / 掩码 / 网关 / DNS，并维护常用联系人 IP 本。

兼容 **Windows 7 / 10 / 11**，依赖系统自带 .NET Framework 4.x，无需额外运行库。

## 功能

- 选择网卡，一键写入静态 IP、掩码、网关、DNS
- 可自动按 IP 推算网关
- 本地联系人库（`contacts.db`）保存常用名称 / IP / 备注
- 默认值可按现场网段调整（源码内 `DefaultIpPrefix` 等）

## 下载与使用

1. 到 [Releases](../../releases) 下载 `EasyIP.exe`，或自行编译
2. 建议**以管理员身份运行**（修改网卡配置需要）
3. 选网卡 → 填 IP → 应用

## 自行编译

```bat
build.bat
```

产物：`bin\EasyIP.exe`  
精简构建也可用 `build-lite.bat`。

## 说明

运行目录下的 `contacts.db` 为本地数据，请勿提交到仓库。
