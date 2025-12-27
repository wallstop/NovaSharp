// Disable warnings about XML documentation
namespace WallstopStudios.NovaSharp.Interpreter.LuaPort
{
#pragma warning disable 1591
#pragma warning disable IDE1006 // Mirrors upstream KopiLua naming (snake_case kept intentionally).

    // NOTE: This file mirrors the upstream KopiLua string library so we can diff and port fixes
    // without translating every identifier to PascalCase. Keep the intentional snake_case members.

    //
    // This part taken from KopiLua - https://github.com/NLua/KopiLua
    //
    // =========================================================================================================
    //
    // Kopi Lua License
    // ----------------
    // MIT License for KopiLua
    // Copyright (c) 2012 LoDC
    // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
    // associated documentation files (the "Software"), to deal in the Software without restriction,
    // including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
    // and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
    // subject to the following conditions:
    // The above copyright notice and this permission notice shall be included in all copies or substantial
    // portions of the Software.
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
    // LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    // IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    // WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    // SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    // ===============================================================================
    // Lua License
    // -----------
    // Lua is licensed under the terms of the MIT license reproduced below.
    // This means that Lua is free software and can be used for both academic
    // and commercial purposes at absolutely no cost.
    // For details and rationale, see http://www.lua.org/license.html .
    // ===============================================================================
    // Copyright (C) 1994-2008 Lua.org, PUC-Rio.
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.

    using System;
    using LuaPort.LuaStateInterop;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;
    using static NovaSharp.Interpreter.LuaPort.LuaStateInterop.LuaBase;
    using lua_Integer = System.Int32;
    using LUA_INTFRM_T = System.Int64;
    using ptrdiff_t = System.Int32;
    using UNSIGNED_LUA_INTFRM_T = System.UInt64;

    internal static class KopiLuaStringLib
    {
        public const int LuaPatternMaxCaptures = 32;

        private static ptrdiff_t Posrelat(ptrdiff_t pos, uint len)
        {
            /* relative string position: negative means back from end */
            if (pos < 0)
            {
                pos += (ptrdiff_t)len + 1;
            }

            return (pos >= 0) ? pos : 0;
        }

        /*
        ** {======================================================
        ** PATTERN MATCHING
        ** =======================================================
        */

        public const int CAP_UNFINISHED = (-1);
        public const int CAP_POSITION = (-2);

        /// <summary>
        /// Capture data for pattern matching. Converted from class to struct to eliminate
        /// 32 heap allocations per MatchState instance.
        /// </summary>
        public struct Capture
        {
            public CharPtr init;
            public ptrdiff_t len;
        }

        /// <summary>
        /// State for pattern matching operations. Uses thread-local pooling to avoid
        /// repeated allocations during string.find, string.match, string.gmatch, and string.gsub.
        /// </summary>
        public sealed class MatchState
        {
            public int matchdepth; /* control for recursive depth (to avoid C stack overflow) */
            public CharPtr srcInit; /* init of source string */
            public CharPtr srcEnd; /* end (`\0') of source string */
            public LuaState l;
            public int level; /* total number of captures (finished or unfinished) */

            // Struct array - no per-element heap allocation needed
            public Capture[] capture = new Capture[LuaPatternMaxCaptures];

            /// <summary>
            /// Resets the MatchState for reuse. Called when renting from the pool.
            /// </summary>
            public void Reset()
            {
                matchdepth = 0;
                srcInit = CharPtr.Null;
                srcEnd = CharPtr.Null;
                l = null!;
                level = 0;
                // Clear capture array to prevent holding references to old CharPtr data
                Array.Clear(capture, 0, capture.Length);
            }
        }

        // Thread-local pool for MatchState instances to avoid allocations in pattern matching
        [ThreadStatic]
        private static MatchState t_cachedMatchState;

        /// <summary>
        /// Gets a MatchState instance, either from thread-local cache or newly allocated.
        /// </summary>
        private static MatchState RentMatchState()
        {
            MatchState ms = t_cachedMatchState;
            if (ms != null)
            {
                t_cachedMatchState = null!;
                ms.Reset();
                return ms;
            }
            return new MatchState();
        }

        /// <summary>
        /// Returns a MatchState instance to the thread-local cache for reuse.
        /// </summary>
        private static void ReturnMatchState(MatchState ms)
        {
            // Clear references to allow GC of the LuaState and source string
            ms.Reset();
            t_cachedMatchState = ms;
        }

        // Thread-local cached buffers for str_format to avoid allocations
        // These are fixed-size arrays reused across format calls on the same thread
        [ThreadStatic]
        private static char[] t_formatFormBuffer; // MaxFormat size

        [ThreadStatic]
        private static char[] t_formatBuffBuffer; // MAX_ITEM size

        /// <summary>
        /// Gets cached format buffers for str_format, allocating only on first use per thread.
        /// </summary>
        private static void GetFormatBuffers(out char[] formBuffer, out char[] buffBuffer)
        {
            formBuffer = t_formatFormBuffer ??= new char[MaxFormat];
            buffBuffer = t_formatBuffBuffer ??= new char[MAX_ITEM];
        }

        public const int MAXCCALLS = 200;
        public const char L_ESC = '%';
        public const string SPECIALS = "^$*+?.([%-";

        private static int check_capture(MatchState ms, int l)
        {
            l -= '1';
            if (l < 0 || l >= ms.level || ms.capture[l].len == CAP_UNFINISHED)
            {
                return LuaLError(ms.l, "invalid capture index {0}", l + 1);
            }

            return l;
        }

        private static int capture_to_close(MatchState ms)
        {
            int level = ms.level;
            for (level--; level >= 0; level--)
            {
                if (ms.capture[level].len == CAP_UNFINISHED)
                {
                    return level;
                }
            }

            return LuaLError(ms.l, "invalid pattern capture");
        }

        private static CharPtr Classend(MatchState ms, CharPtr p)
        {
            p = new CharPtr(p);
            char c = p[0];
            p = p.Next();
            switch (c)
            {
                case L_ESC:
                {
                    if (p[0] == '\0')
                    {
                        LuaLError(
                            ms.l,
                            "malformed pattern (ends with " + LuaQuoteLiteral("%") + ")"
                        );
                    }

                    return p + 1;
                }
                case '[':
                {
                    if (p[0] == '^')
                    {
                        p = p.Next();
                    }

                    do
                    { /* look for a `]' */
                        if (p[0] == '\0')
                        {
                            LuaLError(
                                ms.l,
                                "malformed pattern (missing " + LuaQuoteLiteral("]") + ")"
                            );
                        }

                        c = p[0];
                        p = p.Next();
                        if (c == L_ESC && p[0] != '\0')
                        {
                            p = p.Next(); /* skip escapes (e.g. `%]') */
                        }
                    } while (p[0] != ']');
                    return p + 1;
                }
                default:
                {
                    return p;
                }
            }
        }

        private static int match_class(char c, char cl)
        {
            bool res;
            switch (ToLower(cl))
            {
                case 'a':
                    res = IsAlpha(c);
                    break;
                case 'c':
                    res = IsControl(c);
                    break;
                case 'd':
                    res = IsDigit(c);
                    break;
                case 'l':
                    res = IsLower(c);
                    break;
                case 'p':
                    res = IsPunctuation(c);
                    break;
                case 's':
                    res = IsSpace(c);
                    break;
                case 'g':
                    res = IsGraphical(c);
                    break;
                case 'u':
                    res = IsUpper(c);
                    break;
                case 'w':
                    res = IsAlphanumeric(c);
                    break;
                case 'x':
                    res = IsHexDigit((char)c);
                    break;
                case 'z':
                    res = (c == 0);
                    break;
                default:
                    return (cl == c) ? 1 : 0;
            }
            return (IsLower(cl) ? (res ? 1 : 0) : ((!res) ? 1 : 0));
        }

        private static int Matchbracketclass(int c, CharPtr p, CharPtr ec)
        {
            int sig = 1;
            if (p[1] == '^')
            {
                sig = 0;
                p = p.Next(); /* skip the `^' */
            }
            while ((p = p.Next()) < ec)
            {
                if (p == L_ESC)
                {
                    p = p.Next();
                    if (match_class((char)c, (char)(p[0])) != 0)
                    {
                        return sig;
                    }
                }
                else if ((p[1] == '-') && (p + 2 < ec))
                {
                    p += 2;
                    if ((byte)((p[-2])) <= c && (c <= (byte)p[0]))
                    {
                        return sig;
                    }
                }
                else if ((byte)(p[0]) == c)
                {
                    return sig;
                }
            }
            return (sig == 0) ? 1 : 0;
        }

        private static int Singlematch(int c, CharPtr p, CharPtr ep)
        {
            switch (p[0])
            {
                case '.':
                    return 1; /* matches any char */
                case L_ESC:
                    return match_class((char)c, (char)(p[1]));
                case '[':
                    return Matchbracketclass(c, p, ep - 1);
                default:
                    return ((byte)(p[0]) == c) ? 1 : 0;
            }
        }

        private static CharPtr Matchbalance(MatchState ms, CharPtr s, CharPtr p)
        {
            if ((p[0] == 0) || (p[1] == 0))
            {
                LuaLError(ms.l, "unbalanced pattern");
            }

            if (s[0] != p[0])
            {
                return CharPtr.Null;
            }
            else
            {
                int b = p[0];
                int e = p[1];
                int cont = 1;
                while ((s = s.Next()) < ms.srcEnd)
                {
                    if (s[0] == e)
                    {
                        if (--cont == 0)
                        {
                            return s + 1;
                        }
                    }
                    else if (s[0] == b)
                    {
                        cont++;
                    }
                }
            }
            return CharPtr.Null; /* string ends out of balance */
        }

        private static CharPtr max_expand(MatchState ms, CharPtr s, CharPtr p, CharPtr ep)
        {
            ptrdiff_t i = 0; /* counts maximum expand for item */
            while ((s + i < ms.srcEnd) && (Singlematch((byte)(s[i]), p, ep) != 0))
            {
                i++;
            }

            /* keeps trying to match with the maximum repetitions */
            while (i >= 0)
            {
                CharPtr res = Match(ms, (s + i), ep + 1);
                if (!res.IsNull)
                {
                    return res;
                }

                i--; /* else didn't match; reduce 1 repetition to try again */
            }
            return CharPtr.Null;
        }

        private static CharPtr min_expand(MatchState ms, CharPtr s, CharPtr p, CharPtr ep)
        {
            for (; ; )
            {
                CharPtr res = Match(ms, s, ep + 1);
                if (!res.IsNull)
                {
                    return res;
                }
                else if ((s < ms.srcEnd) && (Singlematch((byte)(s[0]), p, ep) != 0))
                {
                    s = s.Next(); /* try with one more repetition */
                }
                else
                {
                    return CharPtr.Null;
                }
            }
        }

        private static CharPtr start_capture(MatchState ms, CharPtr s, CharPtr p, int what)
        {
            CharPtr res;
            int level = ms.level;
            if (level >= LuaPatternMaxCaptures)
            {
                LuaLError(ms.l, "too many captures");
            }

            ms.capture[level].init = s;
            ms.capture[level].len = what;
            ms.level = level + 1;
            if ((res = Match(ms, s, p)).IsNull) /* match failed? */
            {
                ms.level--; /* undo capture */
            }

            return res;
        }

        private static CharPtr end_capture(MatchState ms, CharPtr s, CharPtr p)
        {
            int l = capture_to_close(ms);
            CharPtr res;
            ms.capture[l].len = s - ms.capture[l].init; /* close capture */
            if ((res = Match(ms, s, p)).IsNull) /* match failed? */
            {
                ms.capture[l].len = CAP_UNFINISHED; /* undo capture */
            }

            return res;
        }

        private static CharPtr match_capture(MatchState ms, CharPtr s, int l)
        {
            uint len;
            l = check_capture(ms, l);
            len = (uint)ms.capture[l].len;
            if ((uint)(ms.srcEnd - s) >= len && MemoryCompare(ms.capture[l].init, s, len) == 0)
            {
                return s + len;
            }
            else
            {
                return CharPtr.Null;
            }
        }

        private static CharPtr Match(MatchState ms, CharPtr s, CharPtr p)
        {
            s = new CharPtr(s);
            p = new CharPtr(p);
            if (ms.matchdepth-- == 0)
            {
                LuaLError(ms.l, "pattern too complex");
            }

            init: /* using goto's to optimize tail recursion */
            switch (p[0])
            {
                case '(':
                { /* start capture */
                    if (p[1] == ')') /* position capture? */
                    {
                        return start_capture(ms, s, p + 2, CAP_POSITION);
                    }
                    else
                    {
                        return start_capture(ms, s, p + 1, CAP_UNFINISHED);
                    }
                }
                case ')':
                { /* end capture */
                    return end_capture(ms, s, p + 1);
                }
                case L_ESC:
                {
                    switch (p[1])
                    {
                        case 'b':
                        { /* balanced string? */
                            s = Matchbalance(ms, s, p + 2);
                            if (s.IsNull)
                            {
                                return CharPtr.Null;
                            }

                            p += 4;
                            goto init; /* else return match(ms, s, p+4); */
                        }
                        case 'f':
                        { /* frontier? */
                            CharPtr ep;
                            char previous;
                            p += 2;
                            if (p[0] != '[')
                            {
                                LuaLError(
                                    ms.l,
                                    "missing "
                                        + LuaQuoteLiteral("[")
                                        + " after "
                                        + LuaQuoteLiteral("%f")
                                        + " in pattern"
                                );
                            }

                            ep = Classend(ms, p); /* points to what is next */
                            previous = (s == ms.srcInit) ? '\0' : s[-1];
                            if (
                                (Matchbracketclass((byte)(previous), p, ep - 1) != 0)
                                || (Matchbracketclass((byte)(s[0]), p, ep - 1) == 0)
                            )
                            {
                                return CharPtr.Null;
                            }

                            p = ep;
                            goto init; /* else return match(ms, s, ep); */
                        }
                        default:
                        {
                            if (IsDigit((char)(p[1])))
                            { /* capture results (%0-%9)? */
                                s = match_capture(ms, s, (byte)(p[1]));
                                if (s.IsNull)
                                {
                                    return CharPtr.Null;
                                }

                                p += 2;
                                goto init; /* else return match(ms, s, p+2) */
                            }
                            //ismeretlen hiba miatt lett ide átmásolva
                            { /* it is a pattern item */
                                CharPtr ep = Classend(ms, p); /* points to what is next */
                                int m =
                                    (s < ms.srcEnd) && (Singlematch((byte)(s[0]), p, ep) != 0)
                                        ? 1
                                        : 0;
                                switch (ep[0])
                                {
                                    case '?':
                                    { /* optional */
                                        CharPtr res;
                                        if ((m != 0) && (!(res = Match(ms, s + 1, ep + 1)).IsNull))
                                        {
                                            return res;
                                        }

                                        p = ep + 1;
                                        goto init; /* else return match(ms, s, ep+1); */
                                    }
                                    case '*':
                                    { /* 0 or more repetitions */
                                        return max_expand(ms, s, p, ep);
                                    }
                                    case '+':
                                    { /* 1 or more repetitions */
                                        return ((m != 0) ? max_expand(ms, s + 1, p, ep) : CharPtr.Null);
                                    }
                                    case '-':
                                    { /* 0 or more repetitions (minimum) */
                                        return min_expand(ms, s, p, ep);
                                    }
                                    default:
                                    {
                                        if (m == 0)
                                        {
                                            return CharPtr.Null;
                                        }

                                        s = s.Next();
                                        p = ep;
                                        goto init; /* else return match(ms, s+1, ep); */
                                    }
                                }
                            }
                            //goto dflt;  /* case default */
                        }
                    }
                }
                case '\0':
                { /* end of pattern */
                    return s; /* match succeeded */
                }
                case '$':
                {
                    if (p[1] == '\0') /* is the `$' the last char in pattern? */
                    {
                        return (s == ms.srcEnd) ? s : CharPtr.Null; /* check end of string */
                    }
                    else
                    {
                        goto dflt;
                    }
                }
                default:
                    dflt:
                    { /* it is a pattern item */
                        CharPtr ep = Classend(ms, p); /* points to what is next */
                        int m = (s < ms.srcEnd) && (Singlematch((byte)(s[0]), p, ep) != 0) ? 1 : 0;
                        switch (ep[0])
                        {
                            case '?':
                            { /* optional */
                                CharPtr res;
                                if ((m != 0) && (!(res = Match(ms, s + 1, ep + 1)).IsNull))
                                {
                                    return res;
                                }

                                p = ep + 1;
                                goto init; /* else return match(ms, s, ep+1); */
                            }
                            case '*':
                            { /* 0 or more repetitions */
                                return max_expand(ms, s, p, ep);
                            }
                            case '+':
                            { /* 1 or more repetitions */
                                return ((m != 0) ? max_expand(ms, s + 1, p, ep) : CharPtr.Null);
                            }
                            case '-':
                            { /* 0 or more repetitions (minimum) */
                                return min_expand(ms, s, p, ep);
                            }
                            default:
                            {
                                if (m == 0)
                                {
                                    return CharPtr.Null;
                                }

                                s = s.Next();
                                p = ep;
                                goto init; /* else return match(ms, s+1, ep); */
                            }
                        }
                    }
            }
        }

        private static CharPtr Lmemfind(CharPtr s1, uint l1, CharPtr s2, uint l2)
        {
            if (l2 == 0)
            {
                return s1; /* empty strings are everywhere */
            }
            else if (l2 > l1)
            {
                return CharPtr.Null; /* avoids a negative `l1' */
            }
            else
            {
                CharPtr init; /* to search for a `*s2' inside `s1' */
                l2--; /* 1st char will be checked by `memchr' */
                l1 = l1 - l2; /* `s2' cannot be found after that */
                while (l1 > 0 && !(init = MemoryFindCharacter(s1, s2[0], l1)).IsNull)
                {
                    init = init.Next(); /* 1st char is already checked */
                    if (MemoryCompare(init, s2 + 1, l2) == 0)
                    {
                        return init - 1;
                    }
                    else
                    { /* correct `l1' and `s1' to try again */
                        l1 -= (uint)(init - s1);
                        s1 = init;
                    }
                }
                return CharPtr.Null; /* not found */
            }
        }

        private static void push_onecapture(MatchState ms, int i, CharPtr s, CharPtr e)
        {
            if (i >= ms.level)
            {
                if (i == 0) /* ms.level == 0, too */
                {
                    LuaPushLString(ms.l, s, (uint)(e - s)); /* add whole match */
                }
                else
                {
                    LuaLError(ms.l, "invalid capture index");
                }
            }
            else
            {
                ptrdiff_t l = ms.capture[i].len;
                if (l == CAP_UNFINISHED)
                {
                    LuaLError(ms.l, "unfinished capture");
                }

                if (l == CAP_POSITION)
                {
                    LuaPushInteger(ms.l, ms.capture[i].init - ms.srcInit + 1);
                }
                else
                {
                    LuaPushLString(ms.l, ms.capture[i].init, (uint)l);
                }
            }
        }

        private static int push_captures(MatchState ms, CharPtr s, CharPtr e)
        {
            int i;
            int nlevels = ((ms.level == 0) && (!s.IsNull)) ? 1 : ms.level;
            LuaLCheckStack(ms.l, nlevels, "too many captures");
            for (i = 0; i < nlevels; i++)
            {
                push_onecapture(ms, i, s, e);
            }

            return nlevels; /* number of strings pushed */
        }

        private static int str_find_aux(LuaState l, int find)
        {
            CharPtr s = LuaLCheckLString(l, 1, out uint l1);
            CharPtr p = PatchPattern(LuaLCheckLString(l, 2, out uint l2));

            ptrdiff_t init = Posrelat(LuaLOptInteger(l, 3, 1), l1) - 1;
            if (init < 0)
            {
                init = 0;
            }
            else if ((uint)(init) > l1)
            {
                init = (ptrdiff_t)l1;
            }

            if (
                (find != 0)
                && (
                    (LuaToBoolean(l, 4) != 0)
                    || /* explicit request? */
                    StringFindAny(p, SPECIALS).IsNull
                )
            )
            { /* or no special characters? */
                /* do a plain search */
                CharPtr s2 = Lmemfind(s + init, (uint)(l1 - init), p, (uint)(l2));
                if (!s2.IsNull)
                {
                    LuaPushInteger(l, s2 - s + 1);
                    LuaPushInteger(l, (int)(s2 - s + l2));
                    return 2;
                }
            }
            else
            {
                MatchState ms = RentMatchState();
                try
                {
                    int anchor = 0;
                    if (p[0] == '^')
                    {
                        p = p.Next();
                        anchor = 1;
                    }
                    CharPtr s1 = s + init;
                    ms.l = l;
                    ms.matchdepth = MAXCCALLS;
                    ms.srcInit = s;
                    ms.srcEnd = s + l1;
                    do
                    {
                        CharPtr res;
                        ms.level = 0;
                        // LuaAssert(ms.matchdepth == MAXCCALLS);
                        ms.matchdepth = MAXCCALLS;
                        if (!(res = Match(ms, s1, p)).IsNull)
                        {
                            if (find != 0)
                            {
                                LuaPushInteger(l, s1 - s + 1); /* start */
                                LuaPushInteger(l, res - s); /* end */
                                return push_captures(ms, CharPtr.Null, CharPtr.Null) + 2;
                            }
                            else
                            {
                                return push_captures(ms, s1, res);
                            }
                        }
                    } while (((s1 = s1.Next()) <= ms.srcEnd) && (anchor == 0));
                }
                finally
                {
                    ReturnMatchState(ms);
                }
            }
            LuaPushNil(l); /* not found */
            return 1;
        }

        public static int str_find(LuaState l)
        {
            return str_find_aux(l, 1);
        }

        public static int str_match(LuaState l)
        {
            return str_find_aux(l, 0);
        }

        private class GMatchAuxData
        {
            public CharPtr s;
            public CharPtr p;
            public uint ls;
            public uint pos;
        }

        private static int gmatch_aux(LuaState l, GMatchAuxData auxdata)
        {
            MatchState ms = RentMatchState();
            try
            {
                uint ls = auxdata.ls;
                CharPtr s = auxdata.s;
                CharPtr p = auxdata.p;
                CharPtr src;
                ms.l = l;
                ms.matchdepth = MAXCCALLS;
                ms.srcInit = s;
                ms.srcEnd = s + ls;
                for (src = s + auxdata.pos; src <= ms.srcEnd; src = src.Next())
                {
                    CharPtr e;
                    ms.level = 0;
                    //LuaAssert(ms.matchdepth == MAXCCALLS);
                    ms.matchdepth = MAXCCALLS;

                    if (!(e = Match(ms, src, p)).IsNull)
                    {
                        lua_Integer newstart = e - s;
                        if (e == src)
                        {
                            newstart++; /* empty match? go at least one position */
                        }

                        auxdata.pos = (uint)newstart;
                        return push_captures(ms, src, e);
                    }
                }
                return 0; /* not found */
            }
            finally
            {
                ReturnMatchState(ms);
            }
        }

        private static DynValue gmatch_aux_2(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return executionContext.EmulateClassicCall(
                args,
                "gmatch",
                l => gmatch_aux(l, (GMatchAuxData)executionContext.AdditionalData)
            );
        }

        public static int str_gmatch(LuaState l)
        {
            CallbackFunction c = new(gmatch_aux_2, "gmatch");
            string s = ArgAsType(l, 1, DataType.String, false).String;
            string p = PatchPattern(ArgAsType(l, 2, DataType.String, false).String);

            // Lua 5.4 added optional 'init' parameter (§6.4.1)
            uint startPos = 0;
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                l.ExecutionContext.Script.CompatibilityVersion
            );
            if (version >= LuaCompatibilityVersion.Lua54)
            {
                // Get init parameter (default 1 in Lua terms = position 0 in C# terms)
                ptrdiff_t init = Posrelat(LuaLOptInteger(l, 3, 1), (uint)s.Length) - 1;
                if (init < 0)
                {
                    init = 0;
                }
                else if (init > s.Length)
                {
                    init = s.Length;
                }
                startPos = (uint)init;
            }
            // For Lua 5.1-5.3, the third argument is ignored (always starts at position 0)

            c.AdditionalData = new GMatchAuxData()
            {
                s = new CharPtr(s),
                p = new CharPtr(p),
                ls = (uint)s.Length,
                pos = startPos,
            };

            l.Push(DynValue.NewCallback(c));

            return 1;
        }

        private static int gfind_nodef(LuaState l)
        {
            return LuaLError(
                l,
                LuaQuoteLiteral("string.gfind")
                    + " was renamed to "
                    + LuaQuoteLiteral("string.gmatch")
            );
        }

        private static void add_s(MatchState ms, LuaLBuffer b, CharPtr s, CharPtr e)
        {
            uint i;
            CharPtr news = LuaToLString(ms.l, 3, out uint l);
            for (i = 0; i < l; i++)
            {
                if (news[i] != L_ESC)
                {
                    LuaLAddChar(b, news[i]);
                }
                else
                {
                    i++; /* skip ESC */
                    if (!IsDigit((char)(news[i])))
                    {
                        if (news[i] != L_ESC)
                        {
                            // Lua 5.2+ rejects invalid % escapes; Lua 5.1 treats them as literal chars
                            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                                ms.l.ExecutionContext.Script.CompatibilityVersion
                            );
                            if (version >= LuaCompatibilityVersion.Lua52)
                            {
                                LuaLError(ms.l, "invalid use of '%' in replacement string");
                            }
                        }
                        LuaLAddChar(b, news[i]);
                    }
                    else if (news[i] == '0')
                    {
                        LuaLAddLString(b, s, (uint)(e - s));
                    }
                    else
                    {
                        push_onecapture(ms, news[i] - '1', s, e);
                        LuaLAddValue(b); /* add capture to accumulated result */
                    }
                }
            }
        }

        private static void add_value(MatchState ms, LuaLBuffer b, CharPtr s, CharPtr e)
        {
            LuaState l = ms.l;
            switch (LuaType(l, 3))
            {
                case LuaTypeNumber:
                case LuaTypeString:
                {
                    add_s(ms, b, s, e);
                    return;
                }
                // case LuaTypeUserData: /// +++ does this make sense ??
                case LuaTypeFunction:
                {
                    int n;
                    LuaPushValue(l, 3);
                    n = push_captures(ms, s, e);
                    LuaCall(l, n, 1);
                    break;
                }
                case LuaTypeTable:
                {
                    push_onecapture(ms, 0, s, e);
                    LuaGetTable(l, 3);
                    break;
                }
            }
            if (LuaToBoolean(l, -1) == 0)
            { /* nil or false? */
                LuaPop(l, 1);
                LuaPushLString(l, s, (uint)(e - s)); /* keep original text */
            }
            else if (LuaIsString(l, -1) == 0)
            {
                LuaLError(l, "invalid replacement value (a {0})", LuaLTypeName(l, -1));
            }

            LuaLAddValue(b); /* add result to accumulator */
        }

        public static int str_gsub(LuaState l)
        {
            CharPtr src = LuaLCheckLString(l, 1, out uint srcl);
            CharPtr p = PatchPattern(LuaLCheckStringStr(l, 2));
            int tr = LuaType(l, 3);
            int maxS = LuaLOptInt(l, 4, (int)(srcl + 1));
            int anchor = 0;
            if (p[0] == '^')
            {
                p = p.Next();
                anchor = 1;
            }
            int n = 0;
            MatchState ms = RentMatchState();
            try
            {
                LuaLBuffer b = new(l);
                LuaLArgCheck(
                    l,
                    tr == LuaTypeNumber
                        || tr == LuaTypeString
                        || tr == LuaTypeFunction
                        || tr == LuaTypeTable
                        || tr == LuaTypeUserData,
                    3,
                    "string/function/table expected"
                );
                LuaLBuffInit(l, b);
                ms.l = l;
                ms.matchdepth = MAXCCALLS;
                ms.srcInit = src;
                ms.srcEnd = src + srcl;
                while (n < maxS)
                {
                    CharPtr e;
                    ms.level = 0;
                    //LuaAssert(ms.matchdepth == MAXCCALLS);
                    ms.matchdepth = MAXCCALLS;
                    e = Match(ms, src, p);
                    if (!e.IsNull)
                    {
                        n++;
                        add_value(ms, b, src, e);
                    }
                    if ((!e.IsNull) && e > src) /* non empty match? */
                    {
                        src = e; /* skip it */
                    }
                    else if (src < ms.srcEnd)
                    {
                        char c = src[0];
                        src = src.Next();
                        LuaLAddChar(b, c);
                    }
                    else
                    {
                        break;
                    }

                    if (anchor != 0)
                    {
                        break;
                    }
                }
                LuaLAddLString(b, src, (uint)(ms.srcEnd - src));
                LuaLPushResult(b);
                LuaPushInteger(l, n); /* number of substitutions */
                return 2;
            }
            finally
            {
                ReturnMatchState(ms);
            }
        }

        /* }====================================================== */

        /* maximum size of each formatted item (> len(format('%99.99f', -1e308))) */
        public const int MAX_ITEM = 512;

        /* valid flags in a format specification */
        public const string FLAGS = "-+ #0";

        /// <summary>
        /// Formats a double as a hexadecimal floating-point string per Lua's %a/%A specifier.
        /// Format: [-]0xh.hhhhp±d where h.hhhh is the significand in hexadecimal
        /// and p±d is the exponent as a signed decimal.
        /// </summary>
        /// <param name="value">The double value to format.</param>
        /// <param name="precision">The number of hex digits after the decimal point (-1 for default).</param>
        /// <param name="uppercase">True for %A (uppercase hex), false for %a (lowercase).</param>
        /// <param name="flags">Format flags parsed from the format string.</param>
        /// <param name="width">Minimum field width (-1 if not specified).</param>
        /// <returns>The formatted hex float string.</returns>
        private static string FormatHexFloat(
            double value,
            int precision,
            bool uppercase,
            string flags,
            int width
        )
        {
            // Handle special cases first
            if (double.IsNaN(value))
            {
                string nanStr = uppercase ? "-NAN" : "-nan";
                return ApplyFieldWidth(nanStr, width, flags);
            }

            if (double.IsPositiveInfinity(value))
            {
                string infStr = uppercase ? "INF" : "inf";
                string prefix = GetSignPrefix(false, flags);
                return ApplyFieldWidth(prefix + infStr, width, flags);
            }

            if (double.IsNegativeInfinity(value))
            {
                string infStr = uppercase ? "-INF" : "-inf";
                return ApplyFieldWidth(infStr, width, flags);
            }

            bool negative = value < 0 || (value == 0 && double.IsNegativeInfinity(1.0 / value));
            double absValue = Math.Abs(value);

            // Handle zero specially
            if (absValue == 0)
            {
                string zeroResult = FormatHexFloatZero(negative, precision, uppercase, flags);
                return ApplyFieldWidth(zeroResult, width, flags);
            }

            // Extract mantissa and exponent using IEEE 754 representation
            long bits = BitConverter.DoubleToInt64Bits(absValue);
            int rawExponent = (int)((bits >> 52) & 0x7FF);
            long mantissa = bits & 0xFFFFFFFFFFFFF; // 52-bit mantissa

            int exponent;
            long significand;

            if (rawExponent == 0)
            {
                // Denormalized number: implicit leading bit is 0
                // Find the actual exponent by normalizing
                exponent = -1022;
                significand = mantissa;

                // Normalize the significand
                while ((significand & (1L << 52)) == 0 && significand != 0)
                {
                    significand <<= 1;
                    exponent--;
                }
            }
            else
            {
                // Normalized number: implicit leading bit is 1
                exponent = rawExponent - 1023;
                significand = mantissa | (1L << 52); // Add implicit leading 1
            }

            // Build the result
            string hexChars = uppercase ? "0123456789ABCDEF" : "0123456789abcdef";
            string expChar = uppercase ? "P" : "p";
            string hexPrefix = uppercase ? "0X" : "0x";

            // The significand has 53 bits (52 mantissa + 1 implicit)
            // We output it as 1.xxxxx where xxxxx is the fractional part in hex
            // The high nibble of the integral part is always 1 for normalized numbers

            // Get the integer part (always 1 for normalized, 0 for denormalized with leading zeros)
            int intPart;
            long fracBits;

            if (rawExponent == 0 && mantissa != 0)
            {
                // Denormalized: find the leading 1 bit
                intPart = 0;
                fracBits = mantissa;

                // Shift until we have a leading 1
                int shift = 0;
                while ((fracBits & (1L << 51)) == 0 && fracBits != 0)
                {
                    fracBits <<= 1;
                    shift++;
                }

                if (fracBits != 0)
                {
                    intPart = 1;
                    fracBits = (fracBits << 1) & 0xFFFFFFFFFFFFF; // Remove the leading 1
                    exponent = -1022 - shift - 1;
                }
            }
            else
            {
                intPart = 1;
                fracBits = mantissa;
            }

            // Build the fractional hex digits (13 hex digits = 52 bits)
            char[] fracDigits = new char[13];
            long tempFrac = fracBits;
            for (int i = 0; i < 13; i++)
            {
                int nibble = (int)((tempFrac >> (48 - i * 4)) & 0xF);
                fracDigits[i] = hexChars[nibble];
            }

            // Apply precision
            string fracPart;
            if (precision < 0)
            {
                // Default: strip trailing zeros
                int lastNonZero = 12;
                while (lastNonZero >= 0 && fracDigits[lastNonZero] == '0')
                {
                    lastNonZero--;
                }

                fracPart = lastNonZero >= 0 ? new string(fracDigits, 0, lastNonZero + 1) : "";
            }
            else if (precision == 0)
            {
                fracPart = "";
            }
            else
            {
                // Round and truncate to specified precision
                if (precision < 13)
                {
                    // Check if we need to round
                    int nextNibble = (int)((fracBits >> (48 - precision * 4)) & 0xF);
                    bool roundUp = nextNibble >= 8;

                    if (roundUp)
                    {
                        // Perform rounding by adding 1 to the last digit we keep
                        long roundMask = 1L << (52 - precision * 4);
                        fracBits += roundMask;

                        // Check for overflow into integer part
                        if ((fracBits & (1L << 52)) != 0)
                        {
                            intPart++;
                            fracBits &= 0xFFFFFFFFFFFFF;
                            if (intPart > 1)
                            {
                                intPart = 1;
                                exponent++;
                            }
                        }

                        // Recalculate fractional digits
                        tempFrac = fracBits;
                        for (int i = 0; i < precision; i++)
                        {
                            int nibble = (int)((tempFrac >> (48 - i * 4)) & 0xF);
                            fracDigits[i] = hexChars[nibble];
                        }
                    }

                    fracPart = new string(fracDigits, 0, precision);
                }
                else
                {
                    // Pad with zeros if precision > 13
                    fracPart = new string(fracDigits) + new string('0', precision - 13);
                }
            }

            // Check for '#' flag (alternate form) - always include decimal point
            bool alternateForm = flags.Contains('#', StringComparison.Ordinal);

            string signStr = negative ? "-" : GetSignPrefix(false, flags);
            string intStr = hexChars[intPart].ToString();
            string expSign = exponent >= 0 ? "+" : "";
            string expStr = exponent.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string result;
            if (string.IsNullOrEmpty(fracPart) && !alternateForm)
            {
                result = signStr + hexPrefix + intStr + expChar + expSign + expStr;
            }
            else
            {
                result = signStr + hexPrefix + intStr + "." + fracPart + expChar + expSign + expStr;
            }

            return ApplyFieldWidth(result, width, flags);
        }

        /// <summary>
        /// Formats a zero value in hex float format.
        /// </summary>
        private static string FormatHexFloatZero(
            bool negative,
            int precision,
            bool uppercase,
            string flags
        )
        {
            string hexPrefix = uppercase ? "0X" : "0x";
            string expChar = uppercase ? "P" : "p";
            string signStr = negative ? "-" : GetSignPrefix(false, flags);
            bool alternateForm = flags.Contains('#', StringComparison.Ordinal);

            string fracPart;
            if (precision < 0)
            {
                fracPart = "";
            }
            else if (precision == 0)
            {
                fracPart = "";
            }
            else
            {
                fracPart = new string('0', precision);
            }

            if (string.IsNullOrEmpty(fracPart) && !alternateForm)
            {
                return signStr + hexPrefix + "0" + expChar + "+0";
            }
            else
            {
                return signStr + hexPrefix + "0." + fracPart + expChar + "+0";
            }
        }

        /// <summary>
        /// Gets the sign prefix based on flags ('+' or ' ' for positive numbers).
        /// </summary>
        private static string GetSignPrefix(bool negative, string flags)
        {
            if (negative)
            {
                return "-";
            }

            if (flags.Contains('+', StringComparison.Ordinal))
            {
                return "+";
            }

            if (flags.Contains(' ', StringComparison.Ordinal))
            {
                return " ";
            }

            return "";
        }

        /// <summary>
        /// Applies field width to a formatted string with padding.
        /// </summary>
        private static string ApplyFieldWidth(string value, int width, string flags)
        {
            if (width <= 0 || value.Length >= width)
            {
                return value;
            }

            bool leftAlign = flags.Contains('-', StringComparison.Ordinal);
            bool zeroPad = flags.Contains('0', StringComparison.Ordinal) && !leftAlign;

            int padCount = width - value.Length;

            if (leftAlign)
            {
                return value + new string(' ', padCount);
            }

            if (zeroPad)
            {
                // For zero padding, we need to insert zeros after the sign and 0x prefix
                int insertPos = 0;

                // Skip sign
                if (value.Length > 0 && (value[0] == '-' || value[0] == '+' || value[0] == ' '))
                {
                    insertPos = 1;
                }

                // Skip 0x/0X prefix
                if (
                    value.Length > insertPos + 1
                    && value[insertPos] == '0'
                    && (value[insertPos + 1] == 'x' || value[insertPos + 1] == 'X')
                )
                {
                    insertPos += 2;
                }

                return value.Substring(0, insertPos)
                    + new string('0', padCount)
                    + value.Substring(insertPos);
            }

            return new string(' ', padCount) + value;
        }

        /*
        ** maximum size of each format specification (such as '%-099.99d')
        ** (+10 accounts for %99.99x plus margin of error)
        */
        public static readonly int MaxFormat =
            (FLAGS.Length + 1) + (LuaIntegerFormatLength.Length + 1) + 10;

        // Pre-computed escape sequences for characters 0-15 to avoid allocations in Addquoted
        // These are the 3-digit versions (e.g., "\000", "\001", ..., "\015") for when followed by a digit
        private static readonly string[] EscapeSequences3Digit =
        {
            "\\000",
            "\\001",
            "\\002",
            "\\003",
            "\\004",
            "\\005",
            "\\006",
            "\\007",
            "\\008",
            "\\009",
            "\\010",
            "\\011",
            "\\012",
            "\\013",
            "\\014",
            "\\015",
        };

        // These are the minimal versions (e.g., "\0", "\1", ..., "\15") for when not followed by a digit
        private static readonly string[] EscapeSequencesMinimal =
        {
            "\\0",
            "\\1",
            "\\2",
            "\\3",
            "\\4",
            "\\5",
            "\\6",
            "\\7",
            "\\8",
            "\\9",
            "\\10",
            "\\11",
            "\\12",
            "\\13",
            "\\14",
            "\\15",
        };

        private static void Addquoted(LuaState l, LuaLBuffer b, int arg)
        {
            CharPtr s = LuaLCheckLString(l, arg, out uint length);
            LuaLAddChar(b, '"');
            while ((length--) != 0)
            {
                switch (s[0])
                {
                    case '"':
                    case '\\':
                    case '\n':
                    {
                        LuaLAddChar(b, '\\');
                        LuaLAddChar(b, 'n');
                        break;
                    }
                    case '\r':
                    {
                        LuaLAddLString(b, "\\r", 2);
                        break;
                    }
                    default:
                    {
                        if (s[0] < (char)16)
                        {
                            int charValue = (int)s[0];
                            bool isfollowedbynum = length >= 1 && char.IsNumber(s[1]);

                            // Use pre-computed escape sequences to avoid string allocations
                            if (isfollowedbynum)
                            {
                                LuaLAddString(b, EscapeSequences3Digit[charValue]);
                            }
                            else
                            {
                                LuaLAddString(b, EscapeSequencesMinimal[charValue]);
                            }
                        }
                        else
                        {
                            LuaLAddChar(b, s[0]);
                        }
                        break;
                    }
                }
                s = s.Next();
            }
            LuaLAddChar(b, '"');
        }

        private static CharPtr Scanformat(LuaState l, CharPtr strfrmt, CharPtr form)
        {
            CharPtr p = strfrmt;
            while (p[0] != '\0' && !StringFindCharacter(FLAGS, p[0]).IsNull)
            {
                p = p.Next(); /* skip flags */
            }

            if ((uint)(p - strfrmt) >= (FLAGS.Length + 1))
            {
                LuaLError(l, "invalid format (repeated flags)");
            }

            if (IsDigit((byte)(p[0])))
            {
                p = p.Next(); /* skip width */
            }

            if (IsDigit((byte)(p[0])))
            {
                p = p.Next(); /* (2 digits at most) */
            }

            if (p[0] == '.')
            {
                p = p.Next();
                if (IsDigit((byte)(p[0])))
                {
                    p = p.Next(); /* skip precision */
                }

                if (IsDigit((byte)(p[0])))
                {
                    p = p.Next(); /* (2 digits at most) */
                }
            }
            if (IsDigit((byte)(p[0])))
            {
                LuaLError(l, "invalid format (width or precision too long)");
            }

            form[0] = '%';
            form = form.Next();
            StringCopyWithLength(form, strfrmt, p - strfrmt + 1);
            form += p - strfrmt + 1;
            form[0] = '\0';
            return p;
        }

        private static void Addintlen(CharPtr form)
        {
            uint l = (uint)StringLength(form);
            char spec = form[l - 1];
            StringCopy(form + l - 1, LuaIntegerFormatLength);
            form[l + (LuaIntegerFormatLength.Length + 1) - 2] = spec;
            form[l + (LuaIntegerFormatLength.Length + 1) - 1] = '\0';
        }

        public static int str_format(LuaState l)
        {
            int top = LuaGetTop(l);
            int arg = 1;
            CharPtr strfrmt = LuaLCheckLString(l, arg, out uint sfl);
            CharPtr strfrmtEnd = strfrmt + sfl;
            LuaLBuffer b = new(l);
            LuaLBuffInit(l, b);

            // Check Lua version for integer precision behavior (Lua 5.3+ supports integer subtype)
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                l.ExecutionContext.Script.CompatibilityVersion
            );
            bool supportsIntegerPrecision = version >= LuaCompatibilityVersion.Lua53;

            // Get cached thread-local buffers to avoid allocations
            // These are reused across all str_format calls on this thread
            GetFormatBuffers(out char[] formBuffer, out char[] buffBuffer);

            while (strfrmt < strfrmtEnd)
            {
                if (strfrmt[0] != L_ESC)
                {
                    LuaLAddChar(b, strfrmt[0]);
                    strfrmt = strfrmt.Next();
                }
                else if (strfrmt[1] == L_ESC)
                {
                    LuaLAddChar(b, strfrmt[0]); /* %% */
                    strfrmt = strfrmt + 2;
                }
                else
                { /* format item */
                    strfrmt = strfrmt.Next();
                    // Reuse pre-allocated buffers (wrap as CharPtr pointing to index 0)
                    CharPtr form = new CharPtr(formBuffer, 0);
                    CharPtr buff = new CharPtr(buffBuffer, 0);
                    if (++arg > top)
                    {
                        LuaLArgError(l, arg, "no value");
                    }

                    strfrmt = Scanformat(l, strfrmt, form);
                    char ch = strfrmt[0];
                    strfrmt = strfrmt.Next();
                    switch (ch)
                    {
                        case 'c':
                        {
                            StringFormat(buff, form, (int)LuaLCheckNumber(l, arg));
                            break;
                        }
                        case 'd':
                        case 'i':
                        {
                            Addintlen(form);
                            // Lua 5.3+: require integer representation and preserve precision
                            // Lua 5.1/5.2: use double conversion (no integer subtype existed)
                            if (supportsIntegerPrecision)
                            {
                                LuaNumber num = LuaLCheckLuaNumber(l, arg);
                                // In Lua 5.3+, %d and %i require the number to have an integer representation
                                LuaNumberHelpers.RequireIntegerRepresentation(num, "format", arg);
                                StringFormat(
                                    buff,
                                    form,
                                    num.IsInteger ? num.AsInteger : (LUA_INTFRM_T)num.ToDouble
                                );
                            }
                            else
                            {
                                StringFormat(buff, form, (LUA_INTFRM_T)LuaLCheckNumber(l, arg));
                            }
                            break;
                        }
                        case 'o':
                        case 'u':
                        case 'x':
                        case 'X':
                        {
                            Addintlen(form);
                            // Lua 5.3+: require integer representation and preserve precision
                            // Lua 5.1/5.2: use double conversion (no integer subtype existed)
                            if (supportsIntegerPrecision)
                            {
                                LuaNumber num = LuaLCheckLuaNumber(l, arg);
                                // In Lua 5.3+, %o, %u, %x, %X require the number to have an integer representation
                                LuaNumberHelpers.RequireIntegerRepresentation(num, "format", arg);
                                StringFormat(
                                    buff,
                                    form,
                                    num.IsInteger
                                        ? (UNSIGNED_LUA_INTFRM_T)(ulong)num.AsInteger
                                        : (UNSIGNED_LUA_INTFRM_T)(long)num.ToDouble
                                );
                            }
                            else
                            {
                                StringFormat(
                                    buff,
                                    form,
                                    (UNSIGNED_LUA_INTFRM_T)(LUA_INTFRM_T)LuaLCheckNumber(l, arg)
                                );
                            }
                            break;
                        }
                        case 'e':
                        case 'E':
                        case 'f':
                        case 'g':
                        case 'G':
                        {
                            StringFormat(buff, form, (double)LuaLCheckNumber(l, arg));
                            break;
                        }
                        case 'a':
                        case 'A':
                        {
                            // Hex float format specifier - only available in Lua 5.2+
                            if (version < LuaCompatibilityVersion.Lua52)
                            {
                                return LuaLError(
                                    l,
                                    "invalid option "
                                        + LuaQuoteLiteral("%" + ch)
                                        + " to "
                                        + LuaQuoteLiteral("format"),
                                    strfrmt[-1]
                                );
                            }

                            double value = (double)LuaLCheckNumber(l, arg);
                            bool uppercase = (ch == 'A');

                            // Parse flags and precision from the format string
                            string formStr = form.ToString();
                            string flags = "";
                            int width = -1;
                            int precision = -1;

                            // Parse the format string to extract flags, width, and precision
                            // Format is like: %[-+ #0][width][.precision]a
                            int pos = 1; // Skip the '%'
                            while (
                                pos < formStr.Length - 1
                                && FLAGS.Contains(formStr[pos], StringComparison.Ordinal)
                            )
                            {
                                flags += formStr[pos];
                                pos++;
                            }

                            // Parse width
                            int widthStart = pos;
                            while (pos < formStr.Length - 1 && char.IsDigit(formStr[pos]))
                            {
                                pos++;
                            }
                            if (pos > widthStart)
                            {
                                width = int.Parse(
                                    formStr.Substring(widthStart, pos - widthStart),
                                    System.Globalization.CultureInfo.InvariantCulture
                                );
                            }

                            // Parse precision
                            if (pos < formStr.Length - 1 && formStr[pos] == '.')
                            {
                                pos++;
                                int precStart = pos;
                                while (pos < formStr.Length - 1 && char.IsDigit(formStr[pos]))
                                {
                                    pos++;
                                }
                                if (pos > precStart)
                                {
                                    precision = int.Parse(
                                        formStr.Substring(precStart, pos - precStart),
                                        System.Globalization.CultureInfo.InvariantCulture
                                    );
                                }
                                else
                                {
                                    precision = 0; // "%.a" means precision of 0
                                }
                            }

                            string hexFloatStr = FormatHexFloat(
                                value,
                                precision,
                                uppercase,
                                flags,
                                width
                            );
                            // Copy result to buff
                            for (int i = 0; i < hexFloatStr.Length && i < MAX_ITEM - 1; i++)
                            {
                                buff[i] = hexFloatStr[i];
                            }
                            buff[Math.Min(hexFloatStr.Length, MAX_ITEM - 1)] = '\0';
                            break;
                        }
                        case 'q':
                        {
                            Addquoted(l, b, arg);
                            continue; /* skip the 'addsize' at the end */
                        }
                        case 's':
                        {
                            // Lua 5.2+ converts non-string values via tostring
                            // Lua 5.1 requires string type
                            DynValue argValue = GetArgument(l, arg);
                            string s;
                            if (argValue.Type == DataType.String)
                            {
                                s = argValue.String;
                            }
                            else if (version >= LuaCompatibilityVersion.Lua52)
                            {
                                // Lua 5.2+ coerces to string via tostring()
                                s = argValue.ToPrintString(version);
                            }
                            else
                            {
                                // Lua 5.1 coerces numbers to strings, but rejects other types
                                // ArgAsType returns a NEW DynValue with the converted string
                                // when AutoConvert is enabled (which is the default).
                                // For non-convertible types (table, function, etc.), it throws.
                                DynValue converted = ArgAsType(l, arg, DataType.String, false);
                                s = converted.String;
                            }

                            uint localLength = (uint)s.Length;
                            if ((StringFindCharacter(form, '.').IsNull) && localLength >= 100)
                            {
                                /* no precision and string is too long to be formatted;
                                   keep original string */
                                LuaPushLiteral(l, s);
                                LuaLAddValue(b);
                                continue; /* skip the `addsize' at the end */
                            }
                            else
                            {
                                StringFormat(buff, form, s);
                                break;
                            }
                        }
                        default:
                        { /* also treat cases `pnLlh' */
                            return LuaLError(
                                l,
                                "invalid option "
                                    + LuaQuoteLiteral("%" + ch)
                                    + " to "
                                    + LuaQuoteLiteral("format"),
                                strfrmt[-1]
                            );
                        }
                    }
                    LuaLAddLString(b, buff, (uint)StringLength(buff));
                }
            }
            LuaLPushResult(b);
            return 1;
        }

        private static string PatchPattern(string charPtr)
        {
            return charPtr.Replace("\0", "%z", StringComparison.Ordinal);
        }
    }
}
