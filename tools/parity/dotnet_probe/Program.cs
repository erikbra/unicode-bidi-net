using System.Globalization;
using System.Text;
using Unicode.Bidi.char_data;

if (args.Length != 2 || args[0] != "bidi-class")
{
    Console.Error.WriteLine("usage: dotnet run --project tools/parity/dotnet_probe -- bidi-class <codepoint>");
    return 2;
}

int codePoint = ParseCodePoint(args[1]);
BidiClass bidiClass = CharData.BidiClass(new Rune(codePoint));
Console.WriteLine(bidiClass);
return 0;

static int ParseCodePoint(string value)
{
    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        return int.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    return int.Parse(value, CultureInfo.InvariantCulture);
}
