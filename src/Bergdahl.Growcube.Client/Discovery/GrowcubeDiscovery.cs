using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bergdahl.Growcube.Client.Discovery;

/// <summary>
/// Provides methods to discover Growcube devices on the network.
/// </summary>
public static class GrowcubeDiscovery
{
    /// <summary>
    /// Discovers Growcube devices within a given CIDR range.
    /// </summary>
    /// <param name="cidr">The CIDR range to scan (e.g., "192.168.1.0/24").</param>
    /// <param name="timeoutPerHost">The timeout for each host being scanned.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of IP addresses of discovered Growcube devices.</returns>
    public static async Task<IReadOnlyList<IPAddress>> DiscoverAsync(
        string cidr,
        TimeSpan timeoutPerHost,
        CancellationToken ct = default)
    {
        var ips = ExpandCidr(cidr).ToList();
        var found = new List<IPAddress>();
        using var sem = new SemaphoreSlim(64);

        var tasks = ips.Select(async ip =>
        {
            await sem.WaitAsync(ct);
            try
            {
                if (await LooksLikeGrowcubeAsync(ip, 8800, timeoutPerHost, ct))
                    lock (found)
                    {
                        found.Add(ip);
                    }
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tasks);
        return found;
    }

    private static async Task<bool> LooksLikeGrowcubeAsync(IPAddress ip, int port, TimeSpan timeout,
        CancellationToken ct)
    {
        using var tcp = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            await tcp.ConnectAsync(ip, port, cts.Token);

            using var stream = tcp.GetStream();
            var buf = new byte[64];
            var read = await stream.ReadAsync(buf, cts.Token);
            if (read <= 0) return true;

            var s = Encoding.ASCII.GetString(buf, 0, read);
            return s.Contains("elea", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<IPAddress> ExpandCidr(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2) throw new ArgumentException("CIDR must look like 192.168.1.0/24");
        var baseIp = IPAddress.Parse(parts[0]);
        var prefix = int.Parse(parts[1]);

        var b = baseIp.GetAddressBytes();
        if (b.Length != 4) throw new NotSupportedException("IPv4 only.");

        var ipU = (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var network = ipU & mask;
        var broadcast = network | ~mask;

        for (var cur = network + 1; cur < broadcast; cur++)
            yield return new IPAddress(new[]
            {
                (byte)(cur >> 24),
                (byte)(cur >> 16),
                (byte)(cur >> 8),
                (byte)cur
            });
    }
}