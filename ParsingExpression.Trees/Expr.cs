﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ParsingExpression.Trees
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
            var resultState = this.MatchImpl(st);
            return resultState;
        }

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
            return st.Grammar.GetRule(this.RuleName).TryParse(st);
        }
    }

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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitCharClass(this);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            if (st.Pos >= st.Text.Length)
                return null;
           
            if (this.ClassTest(st.Text[st.Pos]))
            {
                return st.Advance(1);
            }

            return null;
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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitChars(this);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            int startPos = st.Pos;
            if (st.Pos + this.Chars.Length > st.Text.Length)
                return null;

            int j = st.Pos;

            for (int i = 0; i < this.Chars.Length; i++)
            {
                if (st.Text[j] != this.Chars[i])
                    return null;

                ++j;
            }

            return st.Advance(j - startPos);
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

        protected override ParsingState MatchImpl(ParsingState st)
        {
            foreach (var item in this.Items)
            {
                st = item.Match(st);
                if (st == null)
                    return null;
            }
            return st;
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

        protected override ParsingState MatchImpl(ParsingState st)
        {
            var prevState = st;
            int startPos = st.Pos;
            foreach (var item in this.Items)
            {
                prevState = item.Match(st);
                if (prevState!=null)
                    return prevState;
            }

            return null;
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

        protected override T ApplyImpl<T>(IExprVisitor<T> visitor)
        {
            return visitor.VisitNum(this);
        }

        protected override ParsingState MatchImpl(ParsingState st)
        {
            int count = 0;
            int startPos = st.Pos;
            var prevState = st;
            for (; count < this.Max; ++count)
            {
                st = this.Child.Match(st);
                if (st == null)
                {
                    st = prevState;
                    break;
                }
                prevState = st;
            }

            if (count >= this.Min && count <= this.Max)
                return st;
            else
                return null;
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

        protected override ParsingState MatchImpl(ParsingState st)
        {
            var newState = this.Child.Match(st);
            if (newState != null)
                return st;
            else
                return null;
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

        protected override ParsingState MatchImpl(ParsingState st)
        {
            var newState = this.Child.Match(st);
            if (newState == null)
                return st;
            else
                return null;
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
