using System.Runtime.CompilerServices;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// Golden-file comparison for generated documents. The expected files live next to the test under
/// <c>Golden/</c> and are reviewed like code; <c>UPDATE_GOLDEN=1</c> rewrites them instead of
/// comparing, so a deliberate change shows up as a diff in the commit.
/// </summary>
public static class GoldenFile
{
  /// <summary>The text of a page in content order, the way an ATS parser reads it.</summary>
  public static string PageText(Page page) => ContentOrderTextExtractor.GetText(page);

  public static void Verify(string actual, string fileName, [CallerFilePath] string callerPath = "")
  {
    var path = Path.Combine(Path.GetDirectoryName(callerPath)!, "Golden", fileName);
    var normalized = Normalize(actual);

    if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, normalized);
      return;
    }

    Assert.True(File.Exists(path), $"Golden file {fileName} is missing. Create it with UPDATE_GOLDEN=1 and review it.");
    Assert.Equal(Normalize(File.ReadAllText(path)), normalized);
  }

  private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";
}
