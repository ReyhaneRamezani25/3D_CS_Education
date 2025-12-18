using System.Linq;
using ArabicSupport;

public static class RtlHelpers
{
    // --- Method 1: Use RLE (U+202B ... U+202C) for each line ---
    // Forces full RTL direction per line and keeps Arabic shaping.
    public static string FixMultilineRLE(string input, bool showTashkeel = false, bool useHinduNumbers = false)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Normalize newlines
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        // Apply ArabicFixer to fix shaping and numbers
        var fixedText = ArabicFixer.Fix(input, showTashkeel, useHinduNumbers);

        // Wrap each line with Right-To-Left Embedding (RLE) and Pop Directional Format (PDF)
        var lines = fixedText.Split('\n').Select(line => "\u202B" + line + "\u202C");
        return string.Join("\n", lines);
    }

    // --- Method 2: Use RLM (U+200F) for line starts ---
    // Adds a Right-To-Left Mark at each line start.
    public static string FixMultilineRLM(string input, bool showTashkeel = false, bool useHinduNumbers = false)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var fixedText = ArabicFixer.Fix(input, showTashkeel, useHinduNumbers);
        fixedText = fixedText.Replace("\r\n", "\n").Replace("\r", "\n");

        // Add RLM at start and before each newline
        return "\u200F" + fixedText.Replace("\n", "\u200F\n");
    }

    // --- Method 3: Single-line helper ---
    // Just fix shaping, no RTL control characters.
    public static string FixSingleLine(string input, bool showTashkeel = false, bool useHinduNumbers = false)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return ArabicFixer.Fix(input, showTashkeel, useHinduNumbers);
    }
}
