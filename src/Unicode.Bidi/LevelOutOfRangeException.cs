namespace Unicode.Bidi;

public sealed class LevelOutOfRangeException : ArgumentOutOfRangeException
{
    public LevelOutOfRangeException(int number, int max)
        : base(nameof(number), number, $"Bidi embedding level must be between 0 and {max}.")
    {
    }
}
