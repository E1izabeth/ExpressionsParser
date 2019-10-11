using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ParsingExpression.RulesTree;

namespace ParsingExpression
{
    public interface IExprVisitor<T>
    {
        T VisitCharClass(CharClassExpr charClassExpr);
        T VisitChars(CharsExpr charsExpr);
        T VisitSequence(SequenceExpr sequenceExpr);
        T VisitAlternative(AlternativesExpr alternativesExpr);
        T VisitNum(NumberExpr numberExpr);
        T VisitCheck(Check check);
        T VisitCheckNot(CheckNot checkNot);
        T VisitRuleCall(RuleCallExpr ruleCallExpr);
    }

    public abstract class Expr
    {
        public abstract override string ToString();

        public ParsingState Match(ParsingState st)
        {
            // Console.WriteLine("Trying to match {0} at {1} for {2}", this, st.Pos, st.Pos < st.Text.Length ? st.Text[st.Pos].ToString() : "<EOT>");
            var resultState = this.MatchImpl(st);
            // Console.WriteLine("{0} {1} at {2} ", resultState.LastMatchSuccessed ? "OK" : "FAIL", this, resultState.Pos);
            return resultState;
        }

        public bool Match(string text, ref int pos)
        {
            //Console.WriteLine("Trying to match {0} at {1} for {2}", this, pos, pos < text.Length ? text[pos].ToString() : "<EOT>");
            var result = this.MatchImpl(text, ref pos);
            //Console.WriteLine("{0} {1} at {2} ", result ? "OK" : "FAIL", this, pos);
            return result;
        }

        protected abstract bool MatchImpl(string text, ref int pos);
        protected abstract ParsingState MatchImpl(ParsingState st);

        public T Apply<T>(IExprVisitor<T> visitor)
        {
            return this.ApplyImpl(visitor);
        }

        protected abstract T ApplyImpl<T>(IExprVisitor<T> visitor);

        public static Expr AnyChar() { return new CharClassExpr(c => true, "."); }
        public static Expr CharsRange(char from, char to) { return new CharClassExpr(c => c >= from && c <= to, $"[{from}-{to}]"); }
        public static Expr ControlChar() { return new CharClassExpr(c => char.IsControl(c)); }
        public static Expr WhitespaceChar() { return new CharClassExpr(c => char.IsWhiteSpace(c), @"\s"); }
        public static Expr NotWhitespaceChar() { return new CharClassExpr(c => !char.IsWhiteSpace(c), @"\S"); }
        public static Expr LetterChar() { return new CharClassExpr(c => char.IsLetter(c)); }
        public static Expr LetterOrDigitChar() { return new CharClassExpr(c => char.IsLetterOrDigit(c), @"\w"); }
        public static Expr NotLetterOrDigitChar() { return new CharClassExpr(c => !char.IsLetterOrDigit(c), @"\W"); }
        public static Expr NumberChar() { return new CharClassExpr(c => char.IsNumber(c)); }
        public static Expr DigitChar() { return new CharClassExpr(c => char.IsDigit(c), @"\d"); }
        public static Expr NotDigitChar() { return new CharClassExpr(c => char.IsDigit(c), @"\D"); }
        public static Expr UnicodeCatChar(UnicodeCategory category) { return new CharClassExpr(c => char.GetUnicodeCategory(c) == category); }
        public static Expr Number(Expr child, int min, int max) { return new NumberExpr(child, min, max); }
        public static Expr Characters(string chars) { return new CharsExpr(chars); }
        public static Expr Sequence(params Expr[] args) { return new SequenceExpr(args); }
        public static Expr Alternatives(params Expr[] args) { return new AlternativesExpr(args); }
        public static Expr Check(Expr child) { return new Check(child); }
        public static Expr CheckNot(Expr child) { return new CheckNot(child); }
        public static Expr RuleCall(string ruleName) { return new RuleCallExpr(ruleName); }
    }

    public class RuleCallExpr : Expr
    {
        public string RuleName { get; private set; }

        public RuleCallExpr(string ruleName)
        {
            this.RuleName = ruleName;
        }

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitRuleCall(this);
        }

        public override string ToString()
        {
            return "$" + this.RuleName;
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            throw new NotImplementedException();
        }

        protected override bool MatchImpl(string text, ref int pos)
        {
            throw new NotImplementedException();
        }
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

        protected override bool MatchImpl(string text, ref int pos)
        {
            if (pos >= text.Length)
                return false;

            if (this.ClassTest(text[pos]))
            {
                ++pos;
                return true;
            }
            return false;
        }

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitCharClass(this);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            if (st.Pos >= st.Text.Length)
                return st.ExitChild(false);

            if (this.ClassTest(st.Text[st.Pos]))
            {
                return st.ExitChild(true, 1);
            }
            return st.ExitChild(false);
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

        protected override bool MatchImpl(string text, ref int pos)
        {
            if (pos + this.Chars.Length > text.Length)
                return false;

            int j = pos;

            for (int i = 0; i < this.Chars.Length; i++)
            {
                if (text[j] != this.Chars[i])
                    return false;

                ++j;
            }
            pos = j;

            return true;
        }

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitChars(this);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            int startPos = st.Pos;
            if (st.Pos + this.Chars.Length > st.Text.Length)
                return st.ExitChild(false);

            int j = st.Pos;

            for (int i = 0; i < this.Chars.Length; i++)
            {
                if (st.Text[j] != this.Chars[i])
                    return st.ExitChild(false);

                ++j;
            }

            return st.ExitChild(true, j - startPos);
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
            if (items.Length < 1)
                throw new ArgumentException();

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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitSequence(this);
        }

        protected override bool MatchImpl(string text, ref int pos)
        {
            foreach (var item in this.Items)
            {
                if (!item.Match(text, ref pos))
                    return false;
            }

            return true;
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            //int startPos = st.Pos;
            foreach (var item in this.Items)
            {
                st = item.Match(st.EnterChild(item));
                if (st.LastMatchSuccessed == false)
                    return st.ExitChild(false);
            }
            return st.ExitChild(true, 0);
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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitAlternative(this);
        }

        protected override bool MatchImpl(string text, ref int pos)
        {
            foreach (var item in this.Items)
            {
                var curr = pos;
                if (item.Match(text, ref curr))
                {
                    pos = curr;
                    return true;
                }
            }

            return false;
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            int startPos = st.Pos;
            foreach (var item in this.Items)
            {
                st = item.Match(st.EnterChild(item));
                if (st.LastMatchSuccessed == true)
                    return st.ExitChild(true);
            }

            return st.ExitChild(false, st.Pos - startPos);
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

        protected override bool MatchImpl(string text, ref int pos)
        {
            int count = 0;

            //for (; count < this.Min; ++count)
            //    if (!this.Child.Match(text, ref pos))
            //        return false;

            //for (; count < this.Max; ++count)
            //    if (!this.Child.Match(text, ref pos))
            //        break;

            //return tr;

            for (; count < this.Max; ++count)
                if (!this.Child.Match(text, ref pos))
                    break;

            if (count >= this.Min && count <= this.Max)
                return true;
            else
                return false;
        }

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitNum(this);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            int count = 0;
            int startPos = st.Pos;
            for (; count < this.Max; ++count)
            {
                st = this.Child.Match(st.EnterChild(this.Child));
                if (st.LastMatchSuccessed == false)
                    break;
            }

            if (count >= this.Min && count <= this.Max)
                return st.ExitChild(true);
            else
                return st.ExitChild(false);
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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitCheck(this);
        }

        protected override bool MatchImpl(string text, ref int pos)
        {
            var curr = pos;
            return this.Child.Match(text, ref curr);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            //    int pos = st.Pos;
            //    if (this.Child.Match(st.EnterChild(this.Child)).LastMatchSuccessed == true)
            //        return st.ExitChild(true, pos - st.Pos);
            //    else
            //        return st.ExitChild(false, st.Pos - pos);

            var newState = this.Child.Match(st.EnterChild(this.Child));
            return newState.ExitChild(newState.LastMatchSuccessed.Value, -(newState.Pos - st.Pos));
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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitCheckNot(this);
        }

        protected override bool MatchImpl(string text, ref int pos)
        {
            var curr = pos;
            return !this.Child.Match(text, ref curr);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            //int pos = st.Pos;
            //if (this.Child.Match(st).LastMatchSuccessed == true)
            //    return st.ExitChild(false, pos - st.Pos);
            //else
            //    return st.ExitChild(true, st.Pos - pos);

            var newState = this.Child.Match(st.EnterChild(this.Child));
            return newState.ExitChild(!newState.LastMatchSuccessed.Value, -(newState.Pos - st.Pos));
        }
    }

    public static class ExprExtensions
    {
        public static IEnumerable<Expr> GetItems(this Expr expr)
        {
            var itemsExpr = expr as ItemsExpr;
            var itemExpr = expr as ItemExpr;

            if (itemsExpr != null)
                return itemsExpr.Items;
            if (itemExpr != null)
                return new[] { itemExpr.Child };

            return new Expr[0];
        }
    }

}
