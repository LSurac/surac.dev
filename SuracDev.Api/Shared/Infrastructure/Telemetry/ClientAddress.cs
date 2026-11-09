using System.Net;
using System.Net.Sockets;

namespace SuracDev.Api.Shared.Infrastructure.Telemetry;

/// <summary>
/// The client address as it appears in a request or page-view log line. The signed-in admin is logged
/// with the full address; an anonymous visitor only with the network part (IPv4 /24, IPv6 /48), enough
/// to tell visits and providers apart without storing who exactly it was. Login attempts keep the full
/// address regardless: that is the security case (docs/security-and-privacy.md).
/// </summary>
public static class ClientAddress
{
  private const int KeptIPv4Bytes = 3;
  private const int KeptIPv6Bytes = 6;

  public static string ForLog(IPAddress? address, bool keepFullAddress)
  {
    if (address is null)
    {
      return "unknown";
    }

    if (address.IsIPv4MappedToIPv6)
    {
      address = address.MapToIPv4();
    }

    return keepFullAddress ? address.ToString() : KeepNetworkPart(address);
  }

  private static string KeepNetworkPart(IPAddress address)
  {
    var bytes = address.GetAddressBytes();
    var kept = address.AddressFamily == AddressFamily.InterNetwork ? KeptIPv4Bytes : KeptIPv6Bytes;
    Array.Clear(bytes, kept, bytes.Length - kept);
    return new IPAddress(bytes).ToString();
  }
}
