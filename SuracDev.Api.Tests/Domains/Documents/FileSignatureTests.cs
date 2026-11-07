using System.Text;
using SuracDev.Api.Domains.Documents.Domain;

namespace SuracDev.Api.Tests.Domains.Documents;

public sealed class FileSignatureTests
{
  [Fact]
  public void Recognises_pdf()
  {
    Assert.Equal("application/pdf", FileSignature.Detect(Encoding.ASCII.GetBytes("%PDF-1.7\n")));
  }

  [Fact]
  public void Recognises_png()
  {
    Assert.Equal("image/png", FileSignature.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]));
  }

  [Fact]
  public void Recognises_jpeg()
  {
    Assert.Equal("image/jpeg", FileSignature.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));
  }

  [Fact]
  public void Recognises_webp()
  {
    Assert.Equal("image/webp", FileSignature.Detect(Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 ")));
  }

  [Fact]
  public void Does_not_trust_anything_else_whatever_the_extension_says()
  {
    Assert.Null(FileSignature.Detect(Encoding.ASCII.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\">")));
    Assert.Null(FileSignature.Detect(Encoding.ASCII.GetBytes("RIFF\0\0\0\0WAVEfmt ")));
    Assert.Null(FileSignature.Detect([]));
  }
}
