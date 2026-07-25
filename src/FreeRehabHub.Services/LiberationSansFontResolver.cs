using PdfSharp.Fonts;

namespace FreeRehabHub.Services;

// PdfSharp 6.x (.NET Core) sistemdeki fontları GDI+ olmadan otomatik çözemiyor — kullanılacak fontun
// bayt içeriğini elle sağlayan bir IFontResolver şart. Liberation Sans (OFL-1.1 lisanslı, bkz.
// assets/fonts/liberation-sans/LICENSE) repoya gömülü olduğu için Windows/Linux/macOS'ta hedef
// makinede kurulu olup olmamasından bağımsız, tutarlı şekilde render ediliyor.
public sealed class LiberationSansFontResolver : IFontResolver
{
    public const string FamilyName = "Liberation Sans";

    private const string RegularFaceName = "LiberationSans#Regular";
    private const string BoldFaceName = "LiberationSans#Bold";

    private readonly byte[] _regularBytes;
    private readonly byte[] _boldBytes;

    public LiberationSansFontResolver(string fontDirectory)
    {
        _regularBytes = File.ReadAllBytes(Path.Combine(fontDirectory, "LiberationSans-Regular.ttf"));
        _boldBytes = File.ReadAllBytes(Path.Combine(fontDirectory, "LiberationSans-Bold.ttf"));
    }

    public byte[] GetFont(string faceName)
    {
        return faceName == BoldFaceName ? _boldBytes : _regularBytes;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(isBold ? BoldFaceName : RegularFaceName);
    }
}
