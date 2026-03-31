#if !NET472
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Regression tests for CLI value parsing helpers.
/// </summary>
public class DesktopValueParserTests {
    [TestMethod]
    /// <summary>
    /// Ensures plain decimal colors stay decimal instead of being parsed as hexadecimal.
    /// </summary>
    public void DesktopValueParser_ParseRequiredColor_DecimalValue_ReturnsDecimalColor() {
        uint color = DesktopManager.Cli.DesktopValueParser.ParseRequiredColor("255", "color");

        Assert.AreEqual((uint)255, color);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hexadecimal colors still parse correctly when prefixed with a hash.
    /// </summary>
    public void DesktopValueParser_ParseRequiredColor_HashHexValue_ReturnsHexColor() {
        uint color = DesktopManager.Cli.DesktopValueParser.ParseRequiredColor("#00FF10", "color");

        Assert.AreEqual(0x00FF10u, color);
    }
}
#endif
