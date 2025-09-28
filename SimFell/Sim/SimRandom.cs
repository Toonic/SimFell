namespace SimFell;

public static class SimRandom
{
    private static Random _random;
    public static bool Deterministic { get; private set; } = false;
    public static bool CanCrit { get; private set; } = true;

    // Default constructor with a seed for deterministic behavior
    static SimRandom()
    {
        _random = new Random(); // Use a constant seed for deterministic mode
    }

    // Enable deterministic mode with an optional seed (defaults to 42)
    public static void EnableDeterminism(bool canCrit = true, int seed = 42)
    {
        _random = new Random(seed);
        Deterministic = true;
        CanCrit = canCrit;
    }

    // Disable deterministic mode (uses system time-based seed)
    public static void DisableDeterminism()
    {
        _random = new Random(); // Non-deterministic, random seed based on system time
        Deterministic = false;
        CanCrit = true;
    }

    /// <summary>
    /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    /// <returns>
    /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
    /// </returns>
    public static double NextDouble() => _random.NextDouble();
    /// <summary>
    /// Returns a random integer that is within a specified range.
    /// </summary>
    /// <param name="min">The inclusive lower bound of the random number returned</param>
    /// <param name="max">The exclusive upper bound of the random number returned. maxValue must be greater than or
    /// equal to minValue</param>
    /// <returns>
    /// A 32-bit signed integer greater than or equal to minValue and less than maxValue; that is, the range of return
    /// values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned
    /// </returns>
    public static int Next(int min, int max) => _random.Next(min, max);
    /// <summary>
    /// Returns a bool based on a given chance against NextDouble().
    /// </summary>
    /// <param name="chance">Chance as a Percentage.</param>
    /// <returns>True if the NextDouble is under or equal to the given chance.</returns>
    public static bool Roll(double percent)
    {
        percent = Math.Clamp(percent, 0.0, 100.0);
        return _random.NextDouble() < percent / 100.0;
    }
}