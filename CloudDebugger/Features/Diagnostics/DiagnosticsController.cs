using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace CloudDebugger.Features.Diagnostics;

/// <summary>
/// Diagnostics
/// 
/// Tool that display various system and diagnostic information.
/// 
/// </summary>
public class DiagnosticsController : Controller
{
    public DiagnosticsController()
    {
    }

    /// <summary>
    /// Show all the environment variables
    /// </summary>
    /// <returns></returns>
    public IActionResult EnvironmentVariables()
    {
        var vars = Environment.GetEnvironmentVariables();
        var model = new List<EnvironmentVariableModel>();

        if (vars != null)
        {
            foreach (DictionaryEntry entry in vars)
            {
                string? key = entry.Key?.ToString();
                string? value = entry.Value?.ToString();

                if (key != null && value != null)
                {
                    model.Add(new EnvironmentVariableModel { Key = key, Value = value });
                }
            }
        }

        // Sort the model by key
        model = model.OrderBy(m => m.Key).ToList();

        return View(model);
    }


    /// <summary>
    /// Inspiration:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.networkinterface
    /// https://github.com/dotnet/dotnet-api-docs/blob/main/snippets/csharp/System.Net.NetworkInformation/IcmpV4Statistics/Overview/netinfo.cs
    /// </summary>
    /// <returns></returns>
    public IActionResult Network()
    {
        var model = new NetworkDetailsModel
        {
            HostName = IPGlobalProperties.GetIPGlobalProperties().HostName,
            DomainName = IPGlobalProperties.GetIPGlobalProperties().DomainName,
            NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Select(adapter => new NetworkInterfaceModel
            {
                Name = adapter.Name,
                Description = adapter.Description,
                NetworkInterfaceType = adapter.NetworkInterfaceType.ToString(),
                OperationalStatus = adapter.OperationalStatus.ToString(),
                IPVersions = GetSupportedIPVersions(adapter),
                DnsSuffix = adapter.GetIPProperties().DnsSuffix,
                DnsAddresses = adapter.GetIPProperties().DnsAddresses.Select(dns => dns.ToString()).ToList(),
                WinsServers = GetWinsServers(adapter),
                IPv4AddressInfos = GetIPv4AddressInfos(adapter),
                IPv6AddressInfos = GetIPv6AddressInfos(adapter),
                GatewayAddresses = GetGatewayAddresses(adapter),
            }).ToList()
        };

        return View(model);
    }

    private static List<IPAddressV4Info> GetIPv4AddressInfos(NetworkInterface adapter)
    {
        return adapter.GetIPProperties()?.UnicastAddresses
            .Where(uni => uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(uni => new IPAddressV4Info
            {
                IPAddress = uni.Address.ToString(),
                SubnetMask = uni.IPv4Mask?.ToString() ?? "N/A"
            })
            .ToList() ?? [];
    }

    private static List<IPAddressV6Info> GetIPv6AddressInfos(NetworkInterface adapter)
    {
        return adapter.GetIPProperties()?.UnicastAddresses
            .Where(uni => uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            .Select(uni => new IPAddressV6Info
            {
                IPAddress = uni.Address.ToString(),
                PrefixLength = $"/{uni.PrefixLength}"
            })
            .ToList() ?? [];
    }

    private static List<string> GetGatewayAddresses(NetworkInterface adapter)
    {
        return adapter.GetIPProperties()?.GatewayAddresses
            .Select(gw => gw.Address.ToString())
            .ToList() ?? [];
    }

    private static List<string> GetWinsServers(NetworkInterface adapter)
    {
        var winsServers = new List<string>();

        if (adapter.Supports(NetworkInterfaceComponent.IPv4))
        {
            var ipv4Properties = adapter.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null && (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()) && ipv4Properties.UsesWins)
            {
                var winsAddresses = adapter.GetIPProperties()?.WinsServersAddresses;
                if (winsAddresses != null)
                {
                    winsServers = winsAddresses.Select(wins => wins.ToString()).ToList();
                }
            }
        }

        return winsServers;
    }


    private static string GetSupportedIPVersions(NetworkInterface adapter)
    {
        var versions = new List<string>();
        if (adapter.Supports(NetworkInterfaceComponent.IPv4))
        {
            versions.Add("IPv4");
        }
        if (adapter.Supports(NetworkInterfaceComponent.IPv6))
        {
            versions.Add("IPv6");
        }
        return string.Join(" ", versions);
    }


    /// <summary>
    /// Display various runtime information
    /// 
    /// Resources:
    /// https://weblog.west-wind.com/posts/2024/Sep/03/Getting-the-ASPNET-Core-Server-Hosting-Urls-during-Server-Startup
    /// </summary>
    /// <param name="server"></param>
    /// <returns></returns>
    public IActionResult SystemRuntimeDetails([FromServices] IServer server)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var model = new SystemRuntimeDetailsModel
        {
            // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.runtimeinformation
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            OSDescription = RuntimeInformation.OSDescription,
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,

            //https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.runtimeenvironmen
            RuntimeDirectory = RuntimeEnvironment.GetRuntimeDirectory(),
            SystemVersion = RuntimeEnvironment.GetSystemVersion(),

            RunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "No",

            ServerAddresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses?.ToList() ?? []
        };

        return View(model);
    }
}
