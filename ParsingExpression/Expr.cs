using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression
{
    public abstract class Expr
    {
        public abstract override string ToString();
        public abstract bool Match(string text, ref int pos);

        public static Expr AnyChar() { return new CharClassExpr(c => true, "."); }
        public static Expr CharsRange(char from, char to) { return new CharClassExpr(c => c >= from && c <= to, $"[{from}-{to}]"); }
        public static Expr ControlChar() { return new CharClassExpr(c => char.IsControl(c)); }
        public static Expr WhitespaceChar() { return new CharClassExpr(c => char.IsWhiteSpace(c), @"\s"); }
        public static Expr LetterChar() { return new CharClassExpr(c => char.IsLetter(c)); }
        public static Expr LetterOrDigitChar() { return new CharClassExpr(c => char.IsLetterOrDigit(c)); }
        public static Expr NumberChar() { return new CharClassExpr(c => char.IsNumber(c)); }
        public static Expr DigitChar() { return new CharClassExpr(c => char.IsDigit(c), @"\d"); }
        public static Expr UnicodeCatChar(UnicodeCategory category) { return new CharClassExpr(c => char.GetUnicodeCategory(c) == category); }
        public static Expr Number(Expr child, int min, int max) { return new NumberExpr(child, min, max); }
        public static Expr Characters(string chars) { return new CharsExpr(chars); }
        public static Expr Sequence(params Expr[] args) { return new SequenceExpr(args); }
        public static Expr Alternatives(params Expr[] args) { return new AlternativesExpr(args); }
        public static Expr Check(Expr child) { return new Check(child); }
        public static Expr CheckNot(Expr child) { return new CheckNot(child); }
    }

    // quantor
    ////@"(?[\w\-]+\.)?(?<name>[\w\-]+)
    ////(\;((?<param>[\w\-]+)
    ////(\=(?<pvalue>(\"[\"]*\")|([\"\;\:\,]*))
    ////(\,(?<pvalue>(\"[\"]*\")|([\"\;\:\,]*)))?)?))*
    ////:(?<value>.*)";

    public class CharClassExpr : Expr
    {
        public Func<char, bool> ClassTest { get; private set; }

        readonly string _str;

        public CharClassExpr(Func<char, bool> classTest, string str = null)
        {
            this.ClassTest = classTest;

            _str = str;
        }

        public override string ToString()
        {
            return _str ?? "[" + this.ClassTest + "]";
        }   

        public override bool Match(string text, ref int pos)
        {
            if (text[pos].GetHashCode() >= this._str[0] && text[pos].GetHashCode() <= this._str[3].GetHashCode())
            {
                ++pos;
                return true;
            }
            return false;
        }
    }


    public class CharsExpr : Expr
    {
        public string Chars { get; private set; }

        public CharsExpr(string chars)
        {
            this.Chars = chars;
        }

        public override string ToString()
        {
            return this.Chars;
        }

        public override bool Match(string text, ref int pos)
        {
            int j = pos;
            if (this.Chars.First() == ':' && this.Chars.Last() == ':')
            {
                switch (this.Chars)
                {
                    case (":digit:"):
                        if (!Char.IsDigit(text[j]))
                            return false;
                        break;
                    case (":notdigit:"):
                        if (Char.IsDigit(text[j]))
                            return false;
                        break;
                    case (":word:"):
                        if (!Char.IsLetterOrDigit(text[j]))
                            return false;
                        break;
                    case (":notword:"):
                        if (Char.IsLetterOrDigit(text[j]))
                            return false;
                        break;
                    case (":space:"):
                        if (!Char.IsWhiteSpace(text[j]))
                            return false;
                        break;
                    case (":notspace:"):
                        if (Char.IsWhiteSpace(text[j]))
                            return false;
                        break;
                    default:
                        throw new NotImplementedException("");
                }
                ++pos;
            }
            else
            {
                for (int i = 0; i < this.Chars.Length; i++)
                {
                    if (text[j] != this.Chars[i])
                        return false;
                    ++j;
                }
                pos = j;
            }
            
            return true;
        }
    }

    public abstract class ItemExpr : Expr
    {
        public Expr Child { get; private set; }

        public ItemExpr(Expr child)
        {
            this.Child = child;
        }

        protected string GetChildString()
        {
            return (!(this.Child is CharClassExpr)) ? $"({this.Child })" : this.Child.ToString();
        }
    }

    public abstract class ItemsExpr : Expr
    {
        public ReadOnlyCollection<Expr> Items { get; private set; }

        public ItemsExpr(params Expr[] items)
        {
            this.Items = new ReadOnlyCollection<Expr>(items);
        }
    }

    public class SequenceExpr : ItemsExpr
    {
        public SequenceExpr(params Expr[] items)
            : base(items) { }

        public override string ToString()
        {
            return string.Join(string.Empty, this.Items.Select(t => t is AlternativesExpr ? $"({t})" : t.ToString()));
        }

        public override bool Match(string text, ref int pos)
        {
            bool LabelCheck = false;
            bool LabelCheckNot = false;
            foreach (var item in this.Items)
            {
                if (LabelCheck || LabelCheckNot)
                {
                    LabelCheck = false;
                    continue;
                }

                if (!item.Match(text, ref pos))
                {
                    if (item.GetType().Name.Contains("Check"))
                    {
                        LabelCheck = true;
                        continue;
                    }
                    else
                        return false;
                }
            }
            return true;
        }
    }

    public class AlternativesExpr : ItemsExpr
    {
        public AlternativesExpr(params Expr[] items)
            : base(items) { }

        public override string ToString()
        {
            return string.Join("|", this.Items.Select(t => t is CharClassExpr ? $"({t})" : t.ToString()));
        }

        public override bool Match(string text, ref int pos)
        {
            foreach(var item in this.Items)
            {
                if (item.Match(text, ref pos))
                    return true;
            }
            return false;
        }
    }

    public class NumberExpr : ItemExpr
    {
        public int Min { get; private set; }
        public int Max { get; private set; }

        public NumberExpr(Expr child, int min, int max)
            : base(child)
        {
            this.Min = min;
            this.Max = max;
        }

        public override string ToString()
        {
            string q;

            if (this.Min == 0 && this.Max == int.MaxValue) q = "*";
            else if (this.Min == 1 && this.Max == int.MaxValue) q = "+";
            else if (this.Min == 0 && this.Max == 1) q = "?";
            else
            {
                string nums;

                if (this.Min == this.Max) nums = this.Min.ToString();
                else if (this.Min == 0 && this.Max > 0) nums = "," + this.Max;
                else if (this.Min > 0 && this.Max == int.MaxValue) nums = this.Min + ",";
                else nums = this.Min + "," + this.Max;

                q = "{" + nums + "}";
            }

            return this.GetChildString() + q;
        }

        public override bool Match(string text, ref int pos)
        {
            int count = 0;
            while (this.Child.Match(text, ref pos))
                ++count;
            if (count >= this.Min && count <= this.Max)
                return true;
            else
                return false;
        }
    }

    public class Check : ItemExpr
    {
        public Check(Expr child)
            : base(child) { }

        public override string ToString()
        {
            return "&" + this.GetChildString();
        }

        public override bool Match(string text, ref int pos)
        {
            int j = pos;
            if (!this.Child.Match(text, ref j))
            {
               return false;
            }
            pos = j;
            return true;
        }
    }

    public class CheckNot : ItemExpr
    {
        public CheckNot(Expr child)
            : base(child) { }

        public override string ToString()
        {
            return "!" + this.GetChildString();
        }

        public override bool Match(string text, ref int pos)
        {
            int j = pos;
            if (this.Child.Match(text, ref j))
            {
                pos = j;
                return false;
            }
            return true;
        }
    }
}
