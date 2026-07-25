using FreeRehabHub.Modules.ArmRaise.Scoring;
using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.ArmRaise.Tests;

public sealed class ShoulderFlexionCalculatorTests
{
    private static readonly PosePoint Shoulder = new() { X = 0, Y = 0, Z = 0 };

    [Fact]
    public void CalculateFlexionAngleDegrees_ArmAtSide_ReturnsZero()
    {
        var hip = new PosePoint { X = 0, Y = 1, Z = 0 };
        var elbow = new PosePoint { X = 0, Y = 1, Z = 0 };

        var angle = ShoulderFlexionCalculator.CalculateFlexionAngleDegrees(hip, Shoulder, elbow);

        Assert.Equal(0.0, angle, 3);
    }

    [Fact]
    public void CalculateFlexionAngleDegrees_ArmHorizontal_ReturnsNinety()
    {
        var hip = new PosePoint { X = 0, Y = 1, Z = 0 };
        var elbow = new PosePoint { X = 1, Y = 0, Z = 0 };

        var angle = ShoulderFlexionCalculator.CalculateFlexionAngleDegrees(hip, Shoulder, elbow);

        Assert.Equal(90.0, angle, 3);
    }

    [Fact]
    public void CalculateFlexionAngleDegrees_ArmRaisedOverhead_ReturnsOneEighty()
    {
        var hip = new PosePoint { X = 0, Y = 1, Z = 0 };
        var elbow = new PosePoint { X = 0, Y = -1, Z = 0 };

        var angle = ShoulderFlexionCalculator.CalculateFlexionAngleDegrees(hip, Shoulder, elbow);

        Assert.Equal(180.0, angle, 3);
    }

    [Fact]
    public void CalculateFlexionAngleDegrees_HipAndShoulderCoincide_ReturnsZeroWithoutThrowing()
    {
        var hip = new PosePoint { X = 0, Y = 0, Z = 0 };
        var elbow = new PosePoint { X = 1, Y = 0, Z = 0 };

        var angle = ShoulderFlexionCalculator.CalculateFlexionAngleDegrees(hip, Shoulder, elbow);

        Assert.Equal(0.0, angle, 3);
    }
}
