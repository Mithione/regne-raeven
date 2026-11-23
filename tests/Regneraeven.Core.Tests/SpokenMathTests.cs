namespace Regneraeven.Core.Tests;

public class SpokenMathTests
{
    [Theory]
    // Words
    [InlineData("tre fjerdedele plus en sjettedel", "3/4 + 1/6")]
    [InlineData("to komma fem gange fire", "2,5 · 4")]
    [InlineData("hundrede og femogtyve divideret med fem", "125 : 5")]
    [InlineData("fem og tyve minus syv", "25 - 7")]
    [InlineData("start parentes tolv minus fire slut parentes gange tre", "(12 - 4) · 3")]
    [InlineData("parentes tolv minus fire parentes gange tre", "(12 - 4) · 3")]
    [InlineData("hvad er syv gange otte", "7 · 8")]
    [InlineData("to tusind tre hundrede plus fire hundrede og to", "2300 + 402")]
    [InlineData("en halv plus en kvart", "1/2 + 1/4")]
    [InlineData("to tredjedele ganget med tre fjerdedele", "2/3 · 3/4")]
    [InlineData("nul komma nul fem plus en", "0,05 + 1")]
    [InlineData("minus tre plus otte", "-3 + 8")]
    [InlineData("tre over fire minus en over otte", "3/4 - 1/8")]
    [InlineData("to og en halv gange fire", "(2 + 1/2) · 4")]
    [InlineData("fireogfyrre delt med fire", "44 : 4")]
    [InlineData("seks gange parentes to plus tre", "6 · (2 + 3)")]
    // Digits written by Windows voice typing
    [InlineData("3/4 + 1/6", "3/4 + 1/6")]
    [InlineData("3 fjerdedele plus 1 sjettedel", "3/4 + 1/6")]
    [InlineData("12 x 4", "12 · 4")]
    [InlineData("2,5 gange 4", "2,5 · 4")]
    [InlineData("Hvad er 48 divideret med 6?", "48 : 6")]
    [InlineData("1.500 minus 250", "1500 - 250")]
    // Equations
    [InlineData("to x plus fem er lig med enogtyve", "2 · x + 5 = 21")]
    [InlineData("x minus 4 lig med 9", "x - 4 = 9")]
    public void Converts_dictation(string spoken, string expected) =>
        Assert.Equal(expected, SpokenMath.ToExpression(spoken));

    [Theory]
    [InlineData("Ida har 24 kr. og køber en is til 12 kr. Hvor mange penge har hun tilbage?")]
    [InlineData("Hvad er arealet af et rektangel")]
    [InlineData("syv")]
    [InlineData("to tre fire")]
    [InlineData("")]
    public void Leaves_word_problems_alone(string spoken) => Assert.Null(SpokenMath.ToExpression(spoken));

    [Theory]
    [InlineData("tre fjerdedele plus en sjettedel", "11/12")]
    [InlineData("start parentes tolv minus fire slut parentes gange tre", "24")]
    [InlineData("to komma fem gange fire", "10")]
    public void Converted_text_can_be_solved(string spoken, string result) =>
        Assert.Equal(result, Expression.Solve(SpokenMath.ToExpression(spoken)!).ResultText);
}
