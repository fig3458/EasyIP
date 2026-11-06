using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyIP
{
    public sealed class AdapterInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Display
        {
            get
            {
                if (string.IsNullOrEmpty(Description) || Description == Name)
                    return Name;
                return Name + "  (" + Description + ")";
            }
        }
    }

    public static class NetworkHelper
    {
        public static List<AdapterInfo> GetAdapters()
        {
            List<AdapterInfo> list = new List<AdapterInfo>();
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    if (nic.OperationalStatus != OperationalStatus.Up &&
                        nic.OperationalStatus != OperationalStatus.Down &&
                        nic.OperationalStatus != OperationalStatus.Dormant)
                        continue;

                    // Prefer Ethernet / Wireless / Unknown (USB adapters etc.)
                    // Include common LAN adapters; also keep others that already have IPv4.
                    bool commonLan =
                        nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Unknown;

                    if (!commonLan)
                    {
                        bool hasIpv4 = false;
                        foreach (UnicastIPAddressInformation addr in nic.GetIPProperties().UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                hasIpv4 = true;
                                break;
                            }
                        }
                        if (!hasIpv4) continue;
                    }

                    list.Add(new AdapterInfo
                    {
                        Name = nic.Name,
                        Description = nic.Description
                    });
                }
            }
            catch
            {
                // Fallback via netsh
            }

            if (list.Count == 0)
            {
                string output = RunNetsh("interface show interface");
                MatchCollection matches = Regex.Matches(output, @"^\s*\S+\s+\S+\s+\S+\s+(.+)$", RegexOptions.Multiline);
                foreach (Match m in matches)
                {
                    string name = m.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(name) || name.IndexOf("Interface Name", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    list.Add(new AdapterInfo { Name = name, Description = name });
                }
            }

            return list;
        }

        public static bool TryParseIpv4(string text, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            IPAddress ip;
            if (!IPAddress.TryParse(text.Trim(), out ip)) return false;
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
            normalized = ip.ToString();
            return true;
        }

        public static string SuggestGateway(string ip)
        {
            string normalized;
            if (!TryParseIpv4(ip, out normalized)) return "";
            string[] parts = normalized.Split('.');
            if (parts.Length != 4) return "";
            return parts[0] + "." + parts[1] + "." + parts[2] + ".1";
        }

        public static string SetStaticIp(string adapterName, string ip, string mask, string gateway, string dns1, string dns2)
        {
            if (string.IsNullOrWhiteSpace(adapterName))
                return "请选择网卡。";

            string nip, nmask, ngw, nd1, nd2;
            if (!TryParseIpv4(ip, out nip)) return "IP 地址格式不正确。";
            if (!TryParseIpv4(mask, out nmask)) return "子网掩码格式不正确。";
            if (!TryParseIpv4(gateway, out ngw)) return "网关格式不正确。";
            if (!TryParseIpv4(dns1, out nd1)) return "首选 DNS 格式不正确。";
            if (!string.IsNullOrWhiteSpace(dns2) && !TryParseIpv4(dns2, out nd2))
                return "备用 DNS 格式不正确。";
            else if (string.IsNullOrWhiteSpace(dns2))
                nd2 = null;
            else
                TryParseIpv4(dns2, out nd2);

            string nameArg = Quote(adapterName);

            // address
            string r1 = RunNetsh("interface ip set address name=" + nameArg + " static " + nip + " " + nmask + " " + ngw + " 1");
            if (IsNetshError(r1))
            {
                // Older netsh style without metric
                r1 = RunNetsh("interface ip set address name=" + nameArg + " static " + nip + " " + nmask + " " + ngw);
                if (IsNetshError(r1)) return "设置 IP 失败：\r\n" + r1;
            }

            // DNS primary
            string r2 = RunNetsh("interface ip set dns name=" + nameArg + " static " + nd1 + " primary");
            if (IsNetshError(r2))
            {
                r2 = RunNetsh("interface ip set dns name=" + nameArg + " static " + nd1);
                if (IsNetshError(r2)) return "设置首选 DNS 失败：\r\n" + r2;
            }

            // DNS secondary
            if (!string.IsNullOrEmpty(nd2))
            {
                RunNetsh("interface ip delete dns name=" + nameArg + " all");
                RunNetsh("interface ip set dns name=" + nameArg + " static " + nd1 + " primary");
                string r3 = RunNetsh("interface ip add dns name=" + nameArg + " " + nd2 + " index=2");
                if (IsNetshError(r3))
                {
                    // Non-fatal on some systems
                }
            }

            return null;
        }

        public static string SetDhcp(string adapterName)
        {
            if (string.IsNullOrWhiteSpace(adapterName))
                return "请选择网卡。";

            string nameArg = Quote(adapterName);
            string r1 = RunNetsh("interface ip set address name=" + nameArg + " dhcp");
            if (IsNetshError(r1)) return "切换 DHCP 失败：\r\n" + r1;

            string r2 = RunNetsh("interface ip set dns name=" + nameArg + " dhcp");
            if (IsNetshError(r2))
            {
                // Some systems use: set dnsservers ... source=dhcp
                RunNetsh("interface ip set dnsservers name=" + nameArg + " source=dhcp");
            }

            return null;
        }

        public static string GetCurrentIpv4(string adapterName)
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (!string.Equals(nic.Name, adapterName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (UnicastIPAddressInformation addr in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            return addr.Address.ToString();
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// Split 192.168.16.25 into prefix "192.168." and tail "16.25".
        /// </summary>
        public static bool TrySplitPrefixTail(string ipv4, out string prefix, out string tail)
        {
            prefix = null;
            tail = null;
            string normalized;
            if (!TryParseIpv4(ipv4, out normalized)) return false;
            string[] p = normalized.Split('.');
            if (p.Length != 4) return false;
            prefix = p[0] + "." + p[1] + ".";
            tail = p[2] + "." + p[3];
            return true;
        }

        public static bool TryJoinPrefixTail(string prefix, string tail, out string ipv4)
        {
            ipv4 = null;
            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(tail))
                return false;
            string p = prefix.Trim();
            if (!p.EndsWith(".")) p = p + ".";
            string t = tail.Trim().TrimStart('.');
            return TryParseIpv4(p + t, out ipv4);
        }

        /// <summary>
        /// Pick a usable LAN adapter + its IPv4. Prefer Up interfaces with a real address.
        /// </summary>
        public static bool TryGetPreferredIpv4(out string adapterName, out string ipv4, out string description)
        {
            adapterName = null;
            ipv4 = null;
            description = null;

            string bestName = null;
            string bestIp = null;
            string bestDesc = null;
            int bestScore = -1;

            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    string ip = null;
                    foreach (UnicastIPAddressInformation addr in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                            continue;
                        string s = addr.Address.ToString();
                        if (s.StartsWith("169.254.", StringComparison.Ordinal)) // APIPA
                            continue;
                        ip = s;
                        break;
                    }
                    if (string.IsNullOrEmpty(ip))
                        continue;

                    int score = 0;
                    if (nic.OperationalStatus == OperationalStatus.Up) score += 100;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx)
                        score += 20;
                    else if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        score += 10;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestName = nic.Name;
                        bestIp = ip;
                        bestDesc = nic.Description;
                    }
                }
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrEmpty(bestName) || string.IsNullOrEmpty(bestIp))
                return false;

            adapterName = bestName;
            ipv4 = bestIp;
            description = bestDesc;
            return true;
        }

        private static string Quote(string s)
        {
            return "\"" + (s ?? "").Replace("\"", "") + "\"";
        }

        private static bool IsNetshError(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            string lower = output.ToLowerInvariant();
            return lower.Contains("error") || lower.Contains("失败") || lower.Contains("不正确") || lower.Contains("not found") || lower.Contains("element not found");
        }

        private static string RunNetsh(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Default,
                    StandardErrorEncoding = Encoding.Default
                };
                using (Process p = Process.Start(psi))
                {
                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit(30000);
                    return (stdout + "\r\n" + stderr).Trim();
                }
            }
            catch (Exception ex)
            {
                return "无法执行 netsh：" + ex.Message;
            }
        }
    }
}
