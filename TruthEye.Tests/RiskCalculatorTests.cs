using TruthEye.Security;

namespace TruthEye.Tests;

public class RiskCalculatorTests
{
    [Fact]
    public void Score_uses_weights_and_unknown_events_count_one()
    {
        var score = RiskCalculator.Score(new (string?, int)[] { ("NoFace", 2), ("TabSwitch", 1), ("Weird", 3) });
        Assert.Equal(2 * 3 + 2 + 3, score);
    }

    [Theory]
    [InlineData(0, "None")]
    [InlineData(1, "Low")]
    [InlineData(4, "Low")]
    [InlineData(5, "Medium")]
    [InlineData(9, "Medium")]
    [InlineData(10, "High")]
    public void Level_thresholds(int score, string level) => Assert.Equal(level, RiskCalculator.Level(score));
}