﻿
using System;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSharpVerbalExpressions
{
	public class VerbalExpressions
	{
		public static VerbalExpressions DefaultExpression
		{
			get { return new VerbalExpressions(); }
		}

		private string _prefixes = "";
		private string _source = "";
		private string _suffixes = "";
		private RegexOptions _modifiers = RegexOptions.Multiline;


		private string RegexString
		{
			get { return _prefixes + _source + _suffixes; }
		}

		private Regex PatternRegex
		{
			get { return new Regex(this.RegexString, _modifiers); }
		}


		public VerbalExpressions()
		{
		}

		static VerbalExpressions()
		{
		}


		public string Sanitize(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}

			return Regex.Escape(value);
		}

		public bool Test(string toTest)
		{
			return IsMatch(toTest);
		}

		public bool IsMatch(string toTest)
		{
			return PatternRegex.IsMatch(toTest);
		}

		public Regex ToRegex()
		{
			return PatternRegex;
		}

		public override string ToString()
		{
			return PatternRegex.ToString();
		}

		public string Capture(string toTest, string groupName)
		{
			if (!Test(toTest))
				return null;

			var match=PatternRegex.Match(toTest);
			return match.Groups[groupName].Value;
		}

		public VerbalExpressions Add(string value)
		{
			if (object.ReferenceEquals(value, null))
			{
				throw new ArgumentNullException("value");
			}

			return Add(value, true);
		}

		public VerbalExpressions Add(CommonRegex commonRegex)
		{
			return Add(commonRegex.Name, false);
		}

		public VerbalExpressions Add(string value, bool sanitize = true)
		{
			if (value == null)
				throw new ArgumentNullException("value must be provided");

			value = sanitize ? Sanitize(value) : value;
			_source += value;
			return this;
		}

		public VerbalExpressions StartOfLine(bool enable = true)
		{
			_prefixes = enable ? "^" : string.Empty;
			return this;
		}

		public VerbalExpressions EndOfLine(bool enable = true)
		{
			_suffixes = enable ? "$" : string.Empty;
			return this;
		}

		public VerbalExpressions Then(string value, bool sanitize = true)
		{
			var sanitizedValue = sanitize ? Sanitize(value) : value;
			value = string.Format("({0})", sanitizedValue);
			return Add(value, false);
		}

		public VerbalExpressions Then(CommonRegex commonRegex)
		{
			return Then(commonRegex.Name, false);
		}

		public VerbalExpressions Find(string value)
		{
			return Then(value);
		}

		public VerbalExpressions Maybe(string value, bool sanitize = true)
		{
			value = sanitize ? Sanitize(value) : value;
			value = string.Format("({0})?", value);
			return Add(value, false);
		}

		public VerbalExpressions Maybe(CommonRegex commonRegex)
		{
			return Maybe(commonRegex.Name, sanitize: false);
		}

		public VerbalExpressions Anything()
		{
			return Add("(.*)", false);
		}

		public VerbalExpressions AnythingBut(string value, bool sanitize = true)
		{
			value = sanitize ? Sanitize(value) : value;
			value = string.Format("([^{0}]*)", value);
			return Add(value, false);
		}

		public VerbalExpressions Something()
		{
			return Add("(.+)", false);
		}

		public VerbalExpressions SomethingBut(string value, bool sanitize = true)
		{
			value = sanitize ? Sanitize(value) : value;
			value = string.Format("([^" + value + "]+)");
			return Add(value, false);
		}

		public VerbalExpressions Replace(string value)
		{
			string whereToReplace = PatternRegex.ToString();

			if (whereToReplace.Length != 0)
			{
				_source.Replace(whereToReplace, value);
			}

			return this;
		}

		public VerbalExpressions LineBreak()
		{
			return Add(@"(\n|(\r\n))", false);
		}

		public VerbalExpressions Br()
		{
			return LineBreak();
		}

		public VerbalExpressions Tab()
		{
			return Add(@"\t");
		}

		public VerbalExpressions Word()
		{
			return Add(@"\w+", false);
		}

		public VerbalExpressions AnyOf(string value, bool sanitize = true)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}

			value = sanitize ? Sanitize(value) : value;
			value = string.Format("[{0}]", Sanitize(value));
			return Add(value, false);
		}

		public VerbalExpressions Any(string value)
		{
			return AnyOf(value);
		}

		public VerbalExpressions Range(params object[] arguments)
		{
			if (object.ReferenceEquals(arguments, null))
			{
				throw new ArgumentNullException("arguments");
			}

			if (arguments.Length == 1)
			{
				throw new ArgumentOutOfRangeException("arguments");
			}

			string[] sanitizedStrings = arguments.Select(argument =>
				{
					if (object.ReferenceEquals(argument, null))
					{
						return string.Empty;
					}

					string casted = argument.ToString();
					if (string.IsNullOrEmpty(casted))
					{
						return string.Empty;
					}
					else
					{
						return Sanitize(casted);
					}
				})
				.Where(sanitizedString =>
					!string.IsNullOrEmpty(sanitizedString))
				.OrderBy(s => s)
				.ToArray();

			if (sanitizedStrings.Length > 3)
			{
				throw new ArgumentOutOfRangeException("arguments");
			}

			if (!sanitizedStrings.Any())
			{
				return this;
			}

			bool hasOddNumberOfParams = (sanitizedStrings.Length % 2) > 0;

			StringBuilder sb = new StringBuilder("[");
			for (int _from = 0; _from < sanitizedStrings.Length; _from += 2)
			{
				int _to = _from + 1;
				if (sanitizedStrings.Length <= _to)
				{
					break;
				}
				sb.AppendFormat("{0}-{1}", sanitizedStrings[_from], sanitizedStrings[_to]);
			}
			sb.Append("]");

			if (hasOddNumberOfParams)
			{
				sb.AppendFormat("|{0}", sanitizedStrings.Last());
			}

			return Add(sb.ToString(), false);
		}

		public VerbalExpressions Multiple(string value, bool sanitize = true)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}

			value = sanitize ? this.Sanitize(value) : value;
			value = string.Format(@"({0})+", value);

			return Add(value, false);
		}

		public VerbalExpressions Or(CommonRegex commonRegex)
		{
			return Or(commonRegex.Name, false);
		}

		public VerbalExpressions Or(string value, bool sanitize = true)
		{
			_prefixes += "(";
			_suffixes = ")" + _suffixes;

			_source += ")|(";

			return Add(value, sanitize);
		}

		public VerbalExpressions BeginCapture()
		{
			return Add("(", false);
		}

		public VerbalExpressions BeginCapture(string groupName)
		{
			return Add("(?<", false).Add(groupName,true).Add(">",false);
		}

		public VerbalExpressions EndCapture()
		{
			return Add(")", false);
		}

		public VerbalExpressions RepeatPrevious(int n)
		{
			return Add("{" + n + "}", false);
		}

		public VerbalExpressions RepeatPrevious(int n, int m)
		{
			return Add("{" + n + "," + m + "}", false);
		}


		public VerbalExpressions AddModifier(char modifier)
		{
			switch (modifier)
			{
			case 'i':
				_modifiers |= RegexOptions.IgnoreCase;
				break;
			case 'x':
				_modifiers |= RegexOptions.IgnorePatternWhitespace;
				break;
			case 'm':
				_modifiers |= RegexOptions.Multiline;
				break;
			case 's':
				_modifiers |= RegexOptions.Singleline;
				break;
			}

			return this;
		}

		public VerbalExpressions RemoveModifier(char modifier)
		{
			switch (modifier)
			{
			case 'i':
				_modifiers &= ~RegexOptions.IgnoreCase;
				break;
			case 'x':
				_modifiers &= ~RegexOptions.IgnorePatternWhitespace;
				break;
			case 'm':
				_modifiers &= ~RegexOptions.Multiline;
				break;
			case 's':
				_modifiers &= ~RegexOptions.Singleline;
				break;
			}

			return this;
		}

		public VerbalExpressions WithAnyCase(bool enable = true)
		{
			if (enable)
			{
				AddModifier('i');
			}
			else
			{
				RemoveModifier('i');
			}
			return this;
		}

		public VerbalExpressions UseOneLineSearchOption(bool enable)
		{
			if (enable)
			{
				RemoveModifier('m');
			}
			else
			{
				AddModifier('m');
			}

			return this;
		}

		public VerbalExpressions WithOptions(RegexOptions options)
		{
			this._modifiers = options;
			return this;
		}

	}
}