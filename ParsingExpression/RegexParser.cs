using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParsingExpression
{
    class RegexParser
    {
        enum TokenKind
        {
            checkOp,
            checkNotOp,
            openGroup,
            closeGroup,
            orOp,
            escapedChar,
            quantor,
            charClass,
            rangeOp,
            anyChar,
            ch,
            notClass
        }

        class Token
        {
            public readonly string str;
            public readonly TokenKind kind;

            public Token(string str, TokenKind kind)
            {
                this.str = str;
                this.kind = kind;
            }

            public bool Check(TokenKind required)
            {
                return this.kind == required;
            }

            public override string ToString()
            {
                return $"{kind}: {str}";
            }
        }

        const string _tokensPattern = @"
        ^(
        (?<checkOp>         &                               )|
        (?<checkNotOp>      !                               )|
        (?<openGroup>       \(                              )|
        (?<closeGroup>      \)                              )|
        (?<orOp>            \|                              )|
        (?<escapedChar>     (\\.)                           )|
        (?<quantor>         [\*\?\+]|(\{((\d+)|(\d+,)|(,\d+)|(\d+,\d+))\})       )|
        (?<charClass>       (\[
            (?<notClass>     (\^)                   )*
                                (
            (?<escapedChar>     (\\.)               )|
            (?<rangeOp>         (\-)                )|
            (?<ch>              .                   )
                                        )*\])               )|
        (?<anyChar>            \.                           )|
        (?<ch>              [^\*\+\{\?\&\!\(\)\[\|\.\\]+              )
        )*$
        ";

        //static readonly Dictionary<TokenKind, string> _patternsByTokenKind = new Dictionary<TokenKind, string>() {
        //    { TokenKind.checkOp, @"&" },
        //    { TokenKind.checkNotOp, @"!" },
        //    { TokenKind.openGroup, @"\(" },
        //    { TokenKind.closeGroup, @"\)" },
        //    { TokenKind.orOp, @"\|" },
        //    { TokenKind.escapedChar, @"(\\.)" },
        //    { TokenKind.quantor, @"[\*\?\+]|(\{\d*(,\d*)?\})" },
        //    { TokenKind.charClass, @"(\[(" },
        //    { TokenKind.escapedChar, @"(\\.)" },
        //    { TokenKind.rangeOp, @"\-)" },
        //    { TokenKind.ch, @"." },
        //                                   // )*\])               )|
        //    { TokenKind.anyChar, @"\." },
        //    { TokenKind.ch, @"[^\&\!\(\)\[\|\.]+" },
        //};

        //static readonly string _tokensPattern2 = string.Format("^({0})*$" + string.Join(" |", _patternsByTokenKind.Select(
        //    kv => $"(?<{kv.Key}>{kv.Value})" 
        //)));

        static readonly Regex _tokensRegex = new Regex(_tokensPattern, RegexOptions.IgnorePatternWhitespace);

        public RegexParser()
        {
        }

        bool TryTokenize(string line, out Token[] tokens)
        {
            var match = _tokensRegex.Match(line);
            if (match.Success)
            {
                tokens = Enum.GetValues(typeof(TokenKind)).OfType<TokenKind>().SelectMany(
                    k => match.Groups[k.ToString()].Captures.OfType<Capture>()
                              .Select(c => new { c, k })
                ).OrderBy(e => e.c.Index).Select(e => new Token(e.c.Value, e.k)).ToArray();
            }
            else
            {
                tokens = null;
            }

            return tokens != null;
        }

        public bool TryParse(string pattern, out Expr expr)
        {
            Token[] tokens;

            if (this.TryTokenize(pattern, out tokens))
             {
                expr = this.ParseImpl(tokens, 0, tokens.Length - 1);
            }
            else
            {
                expr = null;
            }

            return expr != null;
        }

        void ParseQuantifier(string str, ref int min, ref int max)
        {
            if (str.Length == 1)
            {
                switch (str.First())
                {
                    case '?': min = 0; max = 1; break;
                    case '*': min = 0; max = int.MaxValue; break;
                    case '+': min = 1; max = int.MaxValue; break;
                    default:
                        throw new NotImplementedException("");
                }
            }
            else
            {
                var parts = str.Substring(1, str.Length - 2).Split(new[] { ',' }, StringSplitOptions.None).Select(p => p.Trim()).ToArray();

                if (parts.Length == 1)
                {
                    min = max = int.Parse(parts[0]);
                }
                else if (parts[0].Length == 0)
                {
                    min = 0;
                    max = int.Parse(parts[1]);
                }
                else if (parts[1].Length == 0)
                {
                    min = int.Parse(parts[0]);
                    max = int.MaxValue;
                }
                else
                {
                    min = int.Parse(parts[0]);
                    max = int.Parse(parts[1]);
                }
            }
        }

        int SkipBraces(Token[] tokens, int from, int to)
        {
            int depth = 1;

            for (int i = from; i <= to; i++)
            {
                var t = tokens[i];
                if (t.Check(TokenKind.openGroup))
                {
                    depth++;
                }
                else if (t.Check(TokenKind.closeGroup))
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            throw new NotImplementedException("");
        }

        Expr GiveEscape(Token t)
        {
            Expr escape;
            if (t.str[1] == 'w')
                escape = Expr.LetterOrDigitChar();
            else if (t.str[1] == 'W')
                escape = Expr.NotLetterOrDigitChar();
            else if (t.str[1] == 'd')
                escape = Expr.DigitChar();
            else if (t.str[1] == 'D')
                escape = Expr.NotDigitChar();
            else if (t.str[1] == 's')
                escape = Expr.WhitespaceChar();
            else if (t.str[1] == 'S')
                escape = Expr.NotWhitespaceChar();
            else if (t.str[1] == 'n')
                escape = Expr.Characters("\n");
            else if (t.str[1] == 'r')
                escape = Expr.Characters("\r");
            else if (t.str[1] == 't')
                escape = Expr.Characters("\t");
            else
                escape = Expr.Characters(t.str[1].ToString());
            return escape;
        }

        Expr ParseImpl(Token[] tokens, int from, int to)
        {
            var items = new LinkedList<Expr>();
            var makeOr = false;
            var makeCheck = false;
            var makeCheckNot = false;

            for (int i = from; i <= to; i++)
            {
                var t = tokens[i];

                switch (t.kind)
                {
                    case TokenKind.openGroup:
                        var nextPos = this.SkipBraces(tokens, i + 1, to);
                        var groupNode = this.ParseImpl(tokens, i + 1, nextPos - 1);
                        if (groupNode == null)
                            return null;
                        i = nextPos;
                        items.AddLast(groupNode);
                        break;
                    case TokenKind.closeGroup:
                        return null;
                    case TokenKind.checkOp:
                        makeCheck = true;
                        continue;
                    case TokenKind.checkNotOp:
                        makeCheckNot = true;
                        continue;
                    case TokenKind.orOp:
                        makeOr = true;
                        continue;
                    case TokenKind.quantor:
                        int minNum = -1, maxNum = -1;
                        this.ParseQuantifier(t.str, ref minNum, ref maxNum);

                        var lastExprPart = items.Last.Value;
                        items.RemoveLast();
                        items.AddLast(Expr.Number(lastExprPart, minNum, maxNum));
                        break;
                    case TokenKind.charClass:
                        var classAlts = this.ParseCharClass(tokens, ref i);
                        items.AddLast(classAlts);
                        break;
                    case TokenKind.anyChar:
                        items.AddLast(Expr.AnyChar());
                        break;
                    case TokenKind.ch:
                        items.AddLast(Expr.Characters(t.str));
                        break;
                    case TokenKind.escapedChar:
                        items.AddLast(GiveEscape(t));
                        break;
                    case TokenKind.rangeOp:
                        return null;
                    default:
                        throw new NotImplementedException("");
                }

                if (makeOr)
                {
                    if (makeCheck || makeCheckNot)
                        return null;

                    if (items.Count < 2)
                        return null;

                    var nextPos = i + 1;
                    if (nextPos <= to)
                        if (tokens[nextPos].Check(TokenKind.quantor))
                            continue;

                    var lastItemIfAlts = items.Last.Previous.Value as AlternativesExpr;
                    var newAltesChildren = lastItemIfAlts == null ? new[] { items.Last.Previous.Value, items.Last.Value }
                                            : lastItemIfAlts.Items.Concat(new[] { items.Last.Value }).ToArray();

                    var newAltsNode = Expr.Alternatives(newAltesChildren);
                    items.RemoveLast();
                    items.RemoveLast();
                    items.AddLast(newAltsNode);
                    makeOr = false;
                }
                else if (makeCheck)
                {
                    if (makeOr || makeCheckNot)
                        return null;

                    var lastExprPart = items.Last.Value;
                    items.RemoveLast();
                    items.AddLast(Expr.Check(lastExprPart));
                    makeCheck = false;
                }
                else if (makeCheckNot)
                {
                    if (makeCheck || makeOr)
                        return null;

                    var lastExprPart = items.Last.Value;
                    items.RemoveLast();
                    items.AddLast(Expr.CheckNot(lastExprPart));
                    makeCheckNot = false;
                }
            }

            return Expr.Sequence(items.ToArray());
        }

        private Expr ParseCharClass(Token[] tokens, ref int i)
        {
            var alts = new LinkedList<Expr>();

            var invertClass = false;

            var classToken = tokens[i];
            for (int l = 2; l < classToken.str.Length;)
            {
                i++;
                var ct = tokens[i];
                l += ct.str.Length;

                switch (ct.kind)
                {
                    case TokenKind.notClass:
                        invertClass = true;
                        break;
                    case TokenKind.ch:
                        ct.str.ForEach(c => alts.AddLast(Expr.Characters(c.ToString())));
                        break;
                    case TokenKind.escapedChar:
                        alts.AddLast(GiveEscape(ct));
                        break;
                    case TokenKind.rangeOp:
                        var lastChar = alts.Last.Value as CharsExpr;
                        if (lastChar == null)
                            return null;

                        alts.RemoveLast();
                        var nextChar = tokens[i + 1];
                        if (nextChar.kind != TokenKind.ch)
                            return null;
                        l++;
                        i++;
                        alts.AddLast(Expr.CharsRange(lastChar.Chars.First(), nextChar.str.First()));
                        break;
                    default:
                        throw new NotImplementedException("");
                }
            }

            var classExpr = Expr.Alternatives(alts.ToArray());

            if (invertClass)
            {
                classExpr = Expr.Sequence(
                    Expr.CheckNot(classExpr),
                    Expr.AnyChar()
                );
            }

            return classExpr;
        }
    }
}
