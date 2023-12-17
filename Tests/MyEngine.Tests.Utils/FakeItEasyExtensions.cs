using FakeItEasy;
using FluentAssertions;
using Xunit.Abstractions;

namespace MyEngine.Tests.Utils;

public static class FakeItEasyExtensions
{
    public static T IsEquivalentTo<T>(this IArgumentConstraintManager<T> constraintManager, object? other)
    {
        return constraintManager.Matches(x => EquivalentCheck(x, other));
    }

    private static bool EquivalentCheck(object? actual, object? expected)
    {
        try
        {
            actual.Should().BeEquivalentTo(expected);
            return true;
        }
        catch (ExpectationException _)
        {
            return false;
        }
    }
}
