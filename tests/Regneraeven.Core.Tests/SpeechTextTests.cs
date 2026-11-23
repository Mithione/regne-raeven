using Regneraeven.Core.Help;

namespace Regneraeven.Core.Tests;

public class SpeechTextTests
{
    [Theory]
    [InlineData("3/4 + 1/6", "tre fjerdedele plus en sjettedel")]
    [InlineData("4 + 3 · 5", "4 plus 3 gange 5")]
    [InlineData("(4 + 3) · 5", "parentes 4 plus 3 slut parentes gange 5")]
    [InlineData("= 4 + 15", "er lig med 4 plus 15")]
    [InlineData("432 : 6 = 72", "432 divideret med 6 er lig med 72")]
    [InlineData("1/2 + 1/4", "en halv plus en fjerdedel")]
    [InlineData("12 - 7 = 5", "12 minus 7 er lig med 5")]
    [InlineData("2 · x + 5 = 21", "2 gange x plus 5 er lig med 21")]
    [InlineData("2 · x = 16     (træk 5 fra på begge sider)", "2 gange x er lig med 16, træk 5 fra på begge sider")]
    [InlineData("7/13", "syv over 13")]
    public void Reads_formulas(string formula, string spoken) => Assert.Equal(spoken, SpeechText.Formula(formula));

    [Theory]
    [InlineData("I 3/4 er 3 tælleren.", "I tre fjerdedele er 3 tælleren.")]
    [InlineData("7/4 = 1 3/4.", "syv fjerdedele er lig med 1 og tre fjerdedele.")]
    [InlineData("Areal = ½ · grundlinje · højde.", "Areal er lig med en halv gange grundlinje gange højde.")]
    [InlineData("Svaret er 36 cm².", "Svaret er 36 kvadratcentimeter.")]
    [InlineData("En ret vinkel er 90°.", "En ret vinkel er 90 grader.")]
    [InlineData("25 % af 80", "25 procent af 80")]
    [InlineData("3 − 7 = −4 (start ved 3, gå 7 til venstre).", "3 minus 7 er lig med minus 4, start ved 3, gå 7 til venstre.")]
    [InlineData("Huskeregel: x før y.", "Huskeregel: x før y.")]
    [InlineData("x-aksen og 2-tallet", "x-aksen og 2-tallet")]
    public void Reads_prose(string text, string spoken) => Assert.Equal(spoken, SpeechText.Prose(text));

    [Fact]
    public void Column_layouts_are_not_read_aloud()
    {
        Assert.True(SpeechText.IsLayout(["  4 6 8", "+ 2 5 7", "-------", "  7 2 5"]));
        Assert.True(SpeechText.IsLayout(["  7 ¹2 ¹4", "− 2  5  7"]));
        Assert.False(SpeechText.IsLayout(["4 + 3 · 5", "= 4 + 15", "= 19"]));
    }

    [Fact]
    public void Every_rule_can_be_read_without_symbols()
    {
        foreach (var rule in RuleBook.All)
        {
            var chunks = SpeechText.ForRule(rule);
            Assert.Equal(rule.Sections.Count + 2, chunks.Count);
            foreach (var c in chunks)
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Text));
                foreach (var symbol in new[] { "·", "×", "²", "°", "½", "---", "←", "¹" })
                    Assert.DoesNotContain(symbol, c.Text);
            }
        }
    }

    [Fact]
    public void Plus_section_points_to_the_screen_for_the_layout()
    {
        var plus = RuleBook.For(Topics.Topic.Plus)!;
        Assert.Contains("Se opstillingen på skærmen", SpeechText.ForSection(plus.Sections[0]));
    }
}
