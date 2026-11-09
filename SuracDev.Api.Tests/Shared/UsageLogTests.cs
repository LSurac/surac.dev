using System.Net;
using SuracDev.Api.Shared.Infrastructure.Telemetry;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// The pure parts of the usage log: how much of a client address a line keeps, which area a page
/// belongs to, and that a page path never carries an application-link token.
/// </summary>
public sealed class UsageLogTests
{
  [Theory]
  [InlineData("203.0.113.57", false, "203.0.113.0")]
  [InlineData("203.0.113.57", true, "203.0.113.57")]
  [InlineData("2001:db8:1234:5678:9abc::1", false, "2001:db8:1234::")]
  [InlineData("2001:db8:1234:5678:9abc::1", true, "2001:db8:1234:5678:9abc::1")]
  [InlineData("::ffff:203.0.113.57", false, "203.0.113.0")]
  public void An_anonymous_visitor_is_logged_with_the_network_part_only(
    string address,
    bool signedIn,
    string expected)
  {
    Assert.Equal(expected, ClientAddress.ForLog(IPAddress.Parse(address), keepFullAddress: signedIn));
  }

  [Fact]
  public void A_missing_address_is_logged_as_unknown()
  {
    Assert.Equal("unknown", ClientAddress.ForLog(null, keepFullAddress: false));
  }

  [Theory]
  [InlineData("/", "public")]
  [InlineData("/en/projects/kassensturz", "public")]
  [InlineData("/administration", "public")]
  [InlineData("/admin", "admin")]
  [InlineData("/admin/bewerbungen", "admin")]
  [InlineData("/b/{token}", "link")]
  public void Every_page_belongs_to_one_area(string path, string area)
  {
    Assert.Equal(area, SiteMetrics.AreaOf(path));
  }

  [Theory]
  [InlineData("/b/abc_DEF-123", "/b/{token}")]
  [InlineData("/b/abc_DEF-123/", "/b/{token}/")]
  [InlineData("/b/{token}", "/b/{token}")]
  [InlineData("/api/links/abc_DEF-123/cv.pdf", "/api/links/{token}/cv.pdf")]
  [InlineData("/en/about", "/en/about")]
  [InlineData("/blog/b/x", "/blog/b/x")]
  public void A_page_path_never_carries_a_link_token(string path, string expected)
  {
    Assert.Equal(expected, TelemetryRedaction.Path(path));
  }
}
