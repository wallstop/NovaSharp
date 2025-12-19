// Copyright (c) NovaSharp, released under the MIT license.
// See LICENSE file in the repository root for full license information.

namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.PatternMatching;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using global::TUnit.Core;
using WallstopStudios.NovaSharp.Interpreter.Compatibility;
using WallstopStudios.NovaSharp.Interpreter.DataTypes;
using WallstopStudios.NovaSharp.Interpreter.Modules;

/// <summary>
/// Tests that NovaSharp's character class implementations in pattern matching
/// match the behavior of reference Lua interpreters (which use C's ctype.h functions).
/// </summary>
/// <remarks>
/// <para>
/// Reference: Lua 5.4 §6.4.1 - Patterns
/// </para>
/// <para>
/// Character classes tested:
/// <list type="bullet">
///   <item><description>%a - letters (alphabetic) - C isalpha()</description></item>
///   <item><description>%c - control characters - C iscntrl()</description></item>
///   <item><description>%d - digits - C isdigit()</description></item>
///   <item><description>%g - printable except space (Lua 5.2+) - C isgraph()</description></item>
///   <item><description>%l - lowercase letters - C islower()</description></item>
///   <item><description>%p - punctuation - C ispunct()</description></item>
///   <item><description>%s - space characters - C isspace()</description></item>
///   <item><description>%u - uppercase letters - C isupper()</description></item>
///   <item><description>%w - alphanumeric - C isalnum()</description></item>
///   <item><description>%x - hexadecimal digits - C isxdigit()</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class CharacterClassParityTUnitTests
{
    // Expected ASCII codes for each character class, derived from reference Lua 5.4

    // %a (alpha): A-Z (65-90), a-z (97-122)
    private static readonly int[] ExpectedAlpha =
    [
        65,
        66,
        67,
        68,
        69,
        70,
        71,
        72,
        73,
        74,
        75,
        76,
        77,
        78,
        79,
        80,
        81,
        82,
        83,
        84,
        85,
        86,
        87,
        88,
        89,
        90,
        97,
        98,
        99,
        100,
        101,
        102,
        103,
        104,
        105,
        106,
        107,
        108,
        109,
        110,
        111,
        112,
        113,
        114,
        115,
        116,
        117,
        118,
        119,
        120,
        121,
        122,
    ];

    // %c (control): 0-31, 127
    private static readonly int[] ExpectedControl =
    [
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        19,
        20,
        21,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31,
        127,
    ];

    // %d (digit): 0-9 (48-57)
    private static readonly int[] ExpectedDigit = [48, 49, 50, 51, 52, 53, 54, 55, 56, 57];

    // %l (lower): a-z (97-122)
    private static readonly int[] ExpectedLower =
    [
        97,
        98,
        99,
        100,
        101,
        102,
        103,
        104,
        105,
        106,
        107,
        108,
        109,
        110,
        111,
        112,
        113,
        114,
        115,
        116,
        117,
        118,
        119,
        120,
        121,
        122,
    ];

    // %p (punct): All printable non-alphanumeric, non-space characters
    // ! " # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \ ] ^ _ ` { | } ~
    private static readonly int[] ExpectedPunct =
    [
        33,
        34,
        35,
        36,
        37,
        38,
        39,
        40,
        41,
        42,
        43,
        44,
        45,
        46,
        47,
        58,
        59,
        60,
        61,
        62,
        63,
        64,
        91,
        92,
        93,
        94,
        95,
        96,
        123,
        124,
        125,
        126,
    ];

    // %s (space): tab (9), newline (10), vertical tab (11), form feed (12), carriage return (13), space (32)
    private static readonly int[] ExpectedSpace = [9, 10, 11, 12, 13, 32];

    // %u (upper): A-Z (65-90)
    private static readonly int[] ExpectedUpper =
    [
        65,
        66,
        67,
        68,
        69,
        70,
        71,
        72,
        73,
        74,
        75,
        76,
        77,
        78,
        79,
        80,
        81,
        82,
        83,
        84,
        85,
        86,
        87,
        88,
        89,
        90,
    ];

    // %w (alnum): 0-9 (48-57), A-Z (65-90), a-z (97-122)
    private static readonly int[] ExpectedAlnum =
    [
        48,
        49,
        50,
        51,
        52,
        53,
        54,
        55,
        56,
        57,
        65,
        66,
        67,
        68,
        69,
        70,
        71,
        72,
        73,
        74,
        75,
        76,
        77,
        78,
        79,
        80,
        81,
        82,
        83,
        84,
        85,
        86,
        87,
        88,
        89,
        90,
        97,
        98,
        99,
        100,
        101,
        102,
        103,
        104,
        105,
        106,
        107,
        108,
        109,
        110,
        111,
        112,
        113,
        114,
        115,
        116,
        117,
        118,
        119,
        120,
        121,
        122,
    ];

    // %x (xdigit): 0-9 (48-57), A-F (65-70), a-f (97-102)
    private static readonly int[] ExpectedXdigit =
    [
        48,
        49,
        50,
        51,
        52,
        53,
        54,
        55,
        56,
        57,
        65,
        66,
        67,
        68,
        69,
        70,
        97,
        98,
        99,
        100,
        101,
        102,
    ];

    // %g (graph): All printable except space (33-126) - Lua 5.2+
    private static readonly int[] ExpectedGraph =
    [
        33,
        34,
        35,
        36,
        37,
        38,
        39,
        40,
        41,
        42,
        43,
        44,
        45,
        46,
        47,
        48,
        49,
        50,
        51,
        52,
        53,
        54,
        55,
        56,
        57,
        58,
        59,
        60,
        61,
        62,
        63,
        64,
        65,
        66,
        67,
        68,
        69,
        70,
        71,
        72,
        73,
        74,
        75,
        76,
        77,
        78,
        79,
        80,
        81,
        82,
        83,
        84,
        85,
        86,
        87,
        88,
        89,
        90,
        91,
        92,
        93,
        94,
        95,
        96,
        97,
        98,
        99,
        100,
        101,
        102,
        103,
        104,
        105,
        106,
        107,
        108,
        109,
        110,
        111,
        112,
        113,
        114,
        115,
        116,
        117,
        118,
        119,
        120,
        121,
        122,
        123,
        124,
        125,
        126,
    ];

    private static int[] GetMatchingCharCodes(Script script, string pattern)
    {
        List<int> codes = [];
        for (int i = 0; i <= 127; i++)
        {
            DynValue result = script.DoString(
                $"return string.match(string.char({i}), '{pattern}')"
            );
            if (result.Type != DataType.Nil)
            {
                codes.Add(i);
            }
        }
        return [.. codes];
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task AlphaClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%a");
        await Assert.That(actual).IsEquivalentTo(ExpectedAlpha).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task ControlClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%c");
        await Assert.That(actual).IsEquivalentTo(ExpectedControl).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task DigitClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%d");
        await Assert.That(actual).IsEquivalentTo(ExpectedDigit).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task LowerClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%l");
        await Assert.That(actual).IsEquivalentTo(ExpectedLower).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task PunctClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%p");
        await Assert.That(actual).IsEquivalentTo(ExpectedPunct).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task SpaceClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%s");
        await Assert.That(actual).IsEquivalentTo(ExpectedSpace).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task UpperClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%u");
        await Assert.That(actual).IsEquivalentTo(ExpectedUpper).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task AlnumClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%w");
        await Assert.That(actual).IsEquivalentTo(ExpectedAlnum).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task XdigitClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%x");
        await Assert.That(actual).IsEquivalentTo(ExpectedXdigit).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests %g (graph) character class - available in Lua 5.2+ only.
    /// </summary>
    [Test]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task GraphClassMatchesLuaReference(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%g");
        await Assert.That(actual).IsEquivalentTo(ExpectedGraph).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that negated character classes (%A, %D, etc.) work correctly.
    /// The uppercase version of a character class matches the complement of the lowercase.
    /// </summary>
    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task NegatedAlphaClassMatchesComplement(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%A");
        int[] expected = Enumerable.Range(0, 128).Except(ExpectedAlpha).ToArray();
        await Assert.That(actual).IsEquivalentTo(expected).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task NegatedDigitClassMatchesComplement(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%D");
        int[] expected = Enumerable.Range(0, 128).Except(ExpectedDigit).ToArray();
        await Assert.That(actual).IsEquivalentTo(expected).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task NegatedLowerClassMatchesComplement(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%L");
        int[] expected = Enumerable.Range(0, 128).Except(ExpectedLower).ToArray();
        await Assert.That(actual).IsEquivalentTo(expected).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task NegatedSpaceClassMatchesComplement(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%S");
        int[] expected = Enumerable.Range(0, 128).Except(ExpectedSpace).ToArray();
        await Assert.That(actual).IsEquivalentTo(expected).ConfigureAwait(false);
    }

    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51)]
    [Arguments(LuaCompatibilityVersion.Lua52)]
    [Arguments(LuaCompatibilityVersion.Lua53)]
    [Arguments(LuaCompatibilityVersion.Lua54)]
    [Arguments(LuaCompatibilityVersion.Lua55)]
    public async Task NegatedUpperClassMatchesComplement(LuaCompatibilityVersion version)
    {
        Script script = new Script(version);
        int[] actual = GetMatchingCharCodes(script, "%U");
        int[] expected = Enumerable.Range(0, 128).Except(ExpectedUpper).ToArray();
        await Assert.That(actual).IsEquivalentTo(expected).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests specific characters that were previously misclassified by using .NET's
    /// Char.IsPunctuation() instead of C's ispunct().
    /// </summary>
    /// <remarks>
    /// These characters are punctuation in C (ispunct) but were NOT punctuation
    /// in .NET (Char.IsPunctuation): $ + &lt; = &gt; ^ ` | ~
    /// </remarks>
    [Test]
    [Arguments(LuaCompatibilityVersion.Lua51, "$", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "+", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "<", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "=", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, ">", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "^", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "`", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "|", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "~", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "!", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "@", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "#", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "%", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "&", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "*", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "(", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, ")", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "-", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "_", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "[", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "]", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "{", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "}", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "\\", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "/", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, ":", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, ";", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "\"", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "'", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, ",", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, ".", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "?", true)]
    [Arguments(LuaCompatibilityVersion.Lua51, "a", false)]
    [Arguments(LuaCompatibilityVersion.Lua51, "Z", false)]
    [Arguments(LuaCompatibilityVersion.Lua51, "0", false)]
    [Arguments(LuaCompatibilityVersion.Lua51, " ", false)]
    [Arguments(LuaCompatibilityVersion.Lua52, "$", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "+", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "<", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "=", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, ">", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "^", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "`", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "|", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "~", true)]
    [Arguments(LuaCompatibilityVersion.Lua52, "a", false)]
    [Arguments(LuaCompatibilityVersion.Lua52, "0", false)]
    [Arguments(LuaCompatibilityVersion.Lua52, " ", false)]
    [Arguments(LuaCompatibilityVersion.Lua53, "$", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "+", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "<", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "=", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, ">", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "^", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "`", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "|", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "~", true)]
    [Arguments(LuaCompatibilityVersion.Lua53, "a", false)]
    [Arguments(LuaCompatibilityVersion.Lua53, "0", false)]
    [Arguments(LuaCompatibilityVersion.Lua53, " ", false)]
    [Arguments(LuaCompatibilityVersion.Lua54, "$", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "+", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "<", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "=", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, ">", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "^", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "`", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "|", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "~", true)]
    [Arguments(LuaCompatibilityVersion.Lua54, "a", false)]
    [Arguments(LuaCompatibilityVersion.Lua54, "0", false)]
    [Arguments(LuaCompatibilityVersion.Lua54, " ", false)]
    [Arguments(LuaCompatibilityVersion.Lua55, "$", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "+", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "<", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "=", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, ">", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "^", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "`", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "|", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "~", true)]
    [Arguments(LuaCompatibilityVersion.Lua55, "a", false)]
    [Arguments(LuaCompatibilityVersion.Lua55, "0", false)]
    [Arguments(LuaCompatibilityVersion.Lua55, " ", false)]
    public async Task PunctMatchesSpecificCharacters(
        LuaCompatibilityVersion version,
        string character,
        bool shouldMatch
    )
    {
        Script script = new Script(version);
        DynValue result = script.DoString(
            $"return string.match({EscapeString(character)}, '%p') ~= nil"
        );
        await Assert.That(result.Boolean).IsEqualTo(shouldMatch).ConfigureAwait(false);
    }

    private static string EscapeString(string s)
    {
        return s switch
        {
            "\\" => "'\\\\'",
            "'" => "\"'\"",
            "\"" => "'\"'",
            _ => $"'{s}'",
        };
    }
}
