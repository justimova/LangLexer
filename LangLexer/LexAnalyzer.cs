using System;
using System.Collections.Generic;

namespace LangLexer
{
	public enum States
	{
		STATE0 = 0,
		STATE1 = 1,
		STATE2 = 2,
		STATE4 = 4,
		STATE5 = 5,
		STATE6 = 6,
		STATE9 = 9,
		STATE12 = 12,
		STATE11 = 11,
		STATE13 = 13,
		STATE14 = 14,
		STATE101 = 101,
		STATE102 = 102
	}

	public static class CharClass
	{
		public static string Letter => "Letter";
		public static string Digit => "Digit";
		public static string Other => "other";
		public static string Dot => "dot";
		public static string Ws => "ws";
		public static string Nl => "nl";
		public static string Сolon => ":";
		public static string Equal => "=";
		public static string Plus => "+";
		public static string Minus => "-";
		public static string Mult => "*";
		public static string Pow => "^";
		public static string More => ">";
		public static string Less => "<";
		public static string Div => "/";
		public static string OpenBracket => "(";
		public static string CloseBracket => ")";
	}

	public static class Tokens
	{
		public static string Keyword => "keyword";
		public static string AssignOp => "assign_op";
		public static string Dot => "dot";
		public static string Ws => "ws";
		public static string Nl => "nl";
		public static string AddOp => "add_op";
		public static string MultOp => "mult_op";
		public static string CompareOp => "comp_op";
		public static string PowOp => "pow_op";
		public static string ParOp => "par_op";
	}

	public class LexAnalyzerException : Exception
	{
		public int NumLine { get; private set; }
		public char ErrorChar { get; private set; }
		public int ErrorCode { get; private set; }

		public LexAnalyzerException(string message, int numLine, char errorChar, int errorCode)
			: base(message)
		{
			NumLine = numLine;
			ErrorChar = errorChar;
			ErrorCode = errorCode;
		}
	}

	public class Lex
	{
		public int NumLine { get; private set; }
		public string Lexema { get; private set; }
		public string Token { get; private set; }
		public int? IdxIdConst { get; private set; }

		public Lex(int numLine, string lexema, string token, int? idxIdConst = null)
		{
			NumLine = numLine;
			Lexema = lexema;
			Token = token;
			IdxIdConst = idxIdConst;
		}
	}

	public class LexAnalyzer
	{
		#region Consts
		private const char EOF = '\0';
		#endregion

		#region Fields
		private Dictionary<string, string> _tableOfLanguageTokens = new Dictionary<string, string>
			{
				{ "program", Tokens.Keyword }, { "end", Tokens.Keyword }, { "var", Tokens.Keyword }, { "do", Tokens.Keyword }, { "while", Tokens.Keyword }, { "begin", Tokens.Keyword },
				{ "enddo", Tokens.Keyword }, { "int", Tokens.Keyword }, { "float", Tokens.Keyword },
				{ ":=", Tokens.AssignOp }, { ".", Tokens.Dot }, { " ", Tokens.Ws }, { "\t", Tokens.Ws }, { "\n", Tokens.Nl },
				{ "-", Tokens.AddOp }, { "+", Tokens.AddOp }, { "*", Tokens.MultOp }, { "/", Tokens.MultOp }, { "^", Tokens.PowOp }, { ">", Tokens.CompareOp }, { "<", Tokens.CompareOp }, { "(", Tokens.ParOp }, { ")", Tokens.ParOp }
			};
		private Dictionary<States, string> _tableIdentFloatInt = new Dictionary<States, string> {
			{ States.STATE2, "ident" }, { States.STATE6, "float" }, { States.STATE9, "int" }
		};
		private Dictionary<(States prevState, string charClass), States> _stf = new Dictionary<(States prevState, string charClass), States>
		{
			{ (States.STATE0, CharClass.Letter), States.STATE1 }, { (States.STATE1, CharClass.Letter), States.STATE1 }, { (States.STATE1, CharClass.Digit), States.STATE1 }, { (States.STATE1, CharClass.Other), States.STATE2 },
			{ (States.STATE0, CharClass.Digit), States.STATE4 }, { (States.STATE4, CharClass.Digit), States.STATE4 }, { (States.STATE4, CharClass.Dot), States.STATE5 }, { (States.STATE4, CharClass.Other), States.STATE9 }, { (States.STATE5, CharClass.Digit), States.STATE5 }, { (States.STATE5, CharClass.Other), States.STATE6 },
			{ (States.STATE0, CharClass.Сolon), States.STATE11 }, { (States.STATE11, CharClass.Equal), States.STATE12 }, 
			{ (States.STATE11,CharClass.Other), States.STATE102 },
			{ (States.STATE0, CharClass.Ws), States.STATE0 },
			{ (States.STATE0, CharClass.Nl), States.STATE13 },
			{ (States.STATE0, CharClass.Plus), States.STATE14 }, { (States.STATE0, CharClass.Minus), States.STATE14 }, { (States.STATE0, CharClass.Mult), States.STATE14 }, { (States.STATE0, CharClass.Div), States.STATE14 }, { (States.STATE0, CharClass.Pow), States.STATE14 }, { (States.STATE0, CharClass.Less), States.STATE14 }, { (States.STATE0, CharClass.More), States.STATE14 }, { (States.STATE0, CharClass.OpenBracket), States.STATE14 }, { (States.STATE0, CharClass.CloseBracket), States.STATE14 },
			{ (States.STATE0, CharClass.Other), States.STATE101 }
		};
		private List<States> FinalStates = new List<States> { States.STATE2, States.STATE6, States.STATE9, States.STATE12,
			States.STATE14, States.STATE101, States.STATE102 };
		private List<States> FinalStarStates = new List<States> { States.STATE2, States.STATE6, States.STATE9 };
		private List<States> FinalErrorStates = new List<States> { States.STATE101, States.STATE102 };
		private List<States> FinalOperatorStates = new List<States> { States.STATE12, States.STATE14 };

		private States _currentState = States.STATE0;
		private int _currentNumChar = -1;
		private int _currentNumLine = 1;
		private string _currentLexeme = "";
		private char _currentChar = EOF;

		#endregion

		#region Properties
		public List<Lex> TableOfSymbol { get; private set; } = new List<Lex>();
		public List<string> TableOfId { get; set; }
		public List<string> TableOfConst { get; set; }
		#endregion

		#region Constructors
		public LexAnalyzer() => Initialize();
		#endregion

		#region Methods: Private
		private void Initialize()
		{
			TableOfSymbol = new List<Lex>();
			TableOfId = new List<string>();
			TableOfConst = new List<string>();
			ResetCurrentState();
			ResetCurrentLexeme();
			_currentNumChar = -1;
			_currentNumLine = 1;
			_currentChar = EOF;
		}
		private char NextChar(string source)
		{
			_currentNumChar++;
			if (_currentNumChar >= source.Length)
			{
				_currentChar = EOF;
				return _currentChar;
			}
			char codeChar = source[_currentNumChar];
			if (codeChar == EOF)
			{
				_currentChar = EOF;
				return _currentChar;
			}
			_currentChar = codeChar;
			return _currentChar;
		}
		private string GetCharClass()
		{
			return _currentChar switch
			{
				'.' => CharClass.Dot,
				char c when "abcdefghijklmnopqrstuvwxyz".Contains(c) => CharClass.Letter,
				char c when "0123456789".Contains(c) => CharClass.Digit,
				char c when " \t".Contains(c) => CharClass.Ws,
				'\n' => CharClass.Nl,
				char c when "+-:=*/()^<>".Contains(c) => _currentChar + "",
				_ => CharClass.Other,
			};
		}
		private States GetNextState(string charClass)
		{
			try
			{
				_currentState = _stf[(_currentState, charClass)];
			}
			catch
			{
				_currentState = _stf[(_currentState, CharClass.Other)];
			}
			return _currentState;
		}
		private bool IsFinalState(States state) => FinalStates.Contains(state);
		private bool IsInitState(States state) => state == States.STATE0;
		private bool IsIdentFloatIntState(States state) => FinalStarStates.Contains(state);
		private bool IsFinalOperatorState(States state) => FinalOperatorStates.Contains(state);
		private bool IsKeywordToken(string token) => token == Tokens.Keyword;
		private bool IsWhiteSpaceState(States state) => state == States.STATE13;
		private bool IsErrorState(States state) => FinalErrorStates.Contains(state);
		private void ResetCurrentState() => _currentState = States.STATE0;
		private string GetToken() =>
			_tableOfLanguageTokens.ContainsKey(_currentLexeme)
				? _tableOfLanguageTokens[_currentLexeme]
				: _tableIdentFloatInt[_currentState];
		private int GetIdConstIndex()
		{
			int indx = -1;
			switch(_currentState)
			{
				case States.STATE2:
					if (!TableOfId.Contains(_currentLexeme))
					{
						TableOfId.Add(_currentLexeme);
					}
					indx = TableOfId.IndexOf(_currentLexeme);
					break;
				case States.STATE6:
				case States.STATE9:
					if (!TableOfConst.Contains(_currentLexeme))
					{
						TableOfConst.Add(_currentLexeme);
					}
					indx = TableOfConst.IndexOf(_currentLexeme);
					break;
			}
			return indx;
		}
		private void Processing()
		{
			if (IsIdentFloatIntState(_currentState))
			{
				string token = GetToken();
				int? index = null;
				if (!IsKeywordToken(token))
					index = GetIdConstIndex();
				TableOfSymbol.Add(new Lex(_currentNumLine, _currentLexeme, token, index));
				ResetCurrentLexeme();
				ResetCurrentState();
			}
			if (IsFinalOperatorState(_currentState))
			{
				AddCurrentCharToLexeme();
				string token = GetToken();
				TableOfSymbol.Add(new Lex(_currentNumLine, _currentLexeme, token, null));
				ResetCurrentLexeme();
				ResetCurrentState();
			}
			if (IsErrorState(_currentState)) {
				Fail();
			}
		}
		private void AddCurrentCharToLexeme() => _currentLexeme += _currentChar;
		private string GetErrorMessage(string message) => $"{message}. Строка {_currentNumLine}";
		private void Fail()
		{
			string message = _currentState == States.STATE102 ? $"Ожидается символ =, а не {_currentChar}"
				: $"Неожиданный символ {_currentChar}";
			message = GetErrorMessage(message);
			throw CreateError(message);
		}
		private LexAnalyzerException CreateError(string message) =>
			new LexAnalyzerException(message, _currentNumLine, _currentChar, (int)_currentState);
		private void ResetCurrentLexeme() => _currentLexeme = "";
		#endregion

		#region Methods: Public
		public void Analysis(string source)
		{
			Initialize();
			try
			{
				while (NextChar(source) != EOF || _currentLexeme != "")
				{
					string currentCharClass = GetCharClass();
					GetNextState(currentCharClass);
					if (IsFinalState(_currentState))
					{
						Processing();
						if (currentCharClass == CharClass.Nl)
							_currentNumChar--;
					}
					else if (IsInitState(_currentState))
						ResetCurrentLexeme();
					else if (IsWhiteSpaceState(_currentState))
					{
						_currentNumLine++;
						ResetCurrentState();
					}
					else 
						AddCurrentCharToLexeme();
				}
			}
			catch (LexAnalyzerException)
			{
				throw;
			}
			catch
			{
				throw CreateError(GetErrorMessage("Лексическая ошибка"));
			}
		}
		#endregion
	}
}
