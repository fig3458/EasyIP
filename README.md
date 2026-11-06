# EasyIP

Windows **便携 IP 配置**小工具：快速改网卡静态 IP / 掩码 / 网关 / DNS，并维护常用联系人 IP 本。适合装机、换机、现场改地址。

兼容 **Windows 7 / 10 / 11**，依赖系统自带 .NET Framework 4.x，无需额外运行库。

## 功能

| 能力 | 说明 |
|------|------|
| 改网卡 | 选择网卡，写入静态 IP、掩码、网关、DNS |
| 自动网关 | 可按 IP 自动推算网关 |
| 联系人本 | 本地 `contacts.db` 保存名称 / IP / 备注，方便反复套用 |
| 默认网段 | 源码内 `DefaultIpPrefix` / DNS 等可按现场改 |

## 下载与使用

1. 到 [Releases](https://github.com/fig3458/EasyIP/releases) 下载 **Windows x64** 下的 `EasyIP.exe`
2. **以管理员身份运行**（改网卡配置需要）
3. 选网卡 → 填 IP / 掩码 / 网关 / DNS → 应用
4. 常用地址可先写入联系人，再一键填入

## 自行编译

```bat
build.bat
```

产物：`bin\EasyIP.exe`  
另有精简构建：`build-lite.bat`。

## 注意

- 运行目录下的 `contacts.db` 为本地数据，不要提交到 Git
- 改错 IP 可能导致本机暂时断网，请确认网段后再应用

## 许可证

[MIT](LICENSE)
