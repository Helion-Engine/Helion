using Helion.Util;

namespace Helion.Tests.Unit.GameLayer;

internal class MockClipboard : IClipboard
{
    public string GetText() => string.Empty;

    public void SetText(string text)
    {
    }
}
