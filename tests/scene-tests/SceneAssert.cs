using System;

namespace FreeRehabHub.SceneTests;

public sealed class SceneAssertionException : Exception
{
    public SceneAssertionException(string message) : base(message)
    {
    }
}

public static class SceneAssert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new SceneAssertionException($"Beklenen: true, Gerçek: false — {message}");
        }
    }

    public static void False(bool condition, string message)
    {
        True(!condition, message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new SceneAssertionException($"Beklenen: {expected}, Gerçek: {actual} — {message}");
        }
    }

    public static void NotNull(object? value, string message)
    {
        if (value is null)
        {
            throw new SceneAssertionException($"Null olmamalıydı — {message}");
        }
    }
}
