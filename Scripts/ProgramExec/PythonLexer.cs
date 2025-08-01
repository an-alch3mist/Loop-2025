using System;
using System.Collections.Generic;
using System.Text;

namespace GptDeepResearch
{
	// The lexer tokenizes the input Python-like code, handling indentation and line tracking
	public class PythonLexer
	{
		private string _code;
		private List<Token> _tokens = new List<Token>();
		private int _pos = 0;
		private int _line = 1;
		private int _column = 0;

		private static HashSet<string> Keywords = new HashSet<string> {
			"if", "else", "while","for", "in", "def", "return", "pass", "not", "and", "or", "global",
		};

		public List<Token> Tokens => _tokens;

		public PythonLexer(string code)
		{
			_code = code.Replace("\r\n", "\n");
			Tokenize();
		}

		private void Tokenize()
		{
			Stack<int> indentStack = new Stack<int>();
			indentStack.Push(0);
			string[] lines = _code.Replace("\r\n", "\n").Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				int indentLevel = 0;
				int indentPos = 0;

				// Count indentation (spaces or tabs at start)
				while (indentPos < line.Length && (line[indentPos] == ' ' || line[indentPos] == '\t'))
				{
					// treat tab as 4 spaces for simplicity
					indentLevel += (line[indentPos] == ' ') ? 1 : 4;
					indentPos++;
				}

				string trimmed = line.Trim();
				// Skip empty lines or comments
				if (trimmed.Length == 0 || trimmed.StartsWith("#"))
				{
					_line++;
					continue;
				}

				// Indentation handling (emit INDENT/DEDENT tokens as needed)
				if (indentLevel > indentStack.Peek())
				{
					indentStack.Push(indentLevel);
					_tokens.Add(new Token(TokenType.INDENT, "", _line));
				}
				while (indentLevel < indentStack.Peek())
				{
					indentStack.Pop();
					_tokens.Add(new Token(TokenType.DEDENT, "", _line));
				}
				if (indentLevel != indentStack.Peek())
				{
					throw new Exception($"Indentation error at line {_line}");
				}

				// Get the content part of the line (after indentation)
				string contentLine = line.Substring(indentPos);

				// Tokenize the content part of this line
				_pos = 0;
				_column = indentPos;
				while (_pos < contentLine.Length)
				{
					char c = contentLine[_pos];

					// Skip whitespace inside line content
					if (c == ' ' || c == '\t')
					{
						_pos++;
						_column++;
						continue;
					}

					// Comment - skip rest of line
					if (c == '#') break;

					// Number literal (integer or float)
					if (char.IsDigit(c))
					{
						int start = _pos;
						while (_pos < contentLine.Length && (char.IsDigit(contentLine[_pos]) || contentLine[_pos] == '.'))
						{
							_pos++;
						}
						string num = contentLine.Substring(start, _pos - start);
						_tokens.Add(new Token(TokenType.NUMBER, num, _line));
						continue;
					}

					// String literal
					if (c == '\"' || c == '\'')
					{
						char quote = c;
						int start = _pos;
						_pos++;
						while (_pos < contentLine.Length && contentLine[_pos] != quote)
						{
							// Allow escape of the quote?
							if (contentLine[_pos] == '\\' && _pos + 1 < contentLine.Length)
							{
								_pos += 2;
							}
							else
							{
								_pos++;
							}
						}
						_pos++; // include closing quote
						string strVal = contentLine.Substring(start, _pos - start);
						_tokens.Add(new Token(TokenType.STRING, strVal, _line));
						continue;
					}

					// Identifier or keyword
					if (char.IsLetter(c) || c == '_')
					{
						int start = _pos;
						while (_pos < contentLine.Length && (char.IsLetterOrDigit(contentLine[_pos]) || contentLine[_pos] == '_'))
						{
							_pos++;
						}
						string name = contentLine.Substring(start, _pos - start);
						// after reading the name:
						TokenType type;
						if (name == "True" || name == "False")
							type = TokenType.BOOLEAN;
						else if (Keywords.Contains(name))
							type = GetKeywordType(name);
						else
							type = TokenType.NAME;

						_tokens.Add(new Token(type, name, _line));
						continue;
					}

					// Two-character operators
					if (_pos + 1 < contentLine.Length)
					{
						string two = contentLine.Substring(_pos, 2);
						if (two == "==" || two == "!=" || two == "<=" || two == ">=")
						{
							TokenType type;
							switch (two)
							{
								case "==": type = TokenType.EQ; break;
								case "!=": type = TokenType.NEQ; break;
								case "<=": type = TokenType.LTE; break;
								case ">=": type = TokenType.GTE; break;
								default: type = TokenType.NAME; break;
							}
							_tokens.Add(new Token(type, two, _line));
							_pos += 2;
							continue;
						}
					}

					// Single-character tokens
					switch (c)
					{
						case '+':
							_tokens.Add(new Token(TokenType.PLUS, "+", _line));
							break;
						case '-':
							_tokens.Add(new Token(TokenType.MINUS, "-", _line));
							break;
						case '*':
							_tokens.Add(new Token(TokenType.STAR, "*", _line));
							break;
						case '/':
							_tokens.Add(new Token(TokenType.SLASH, "/", _line));
							break;
						case '%':
							_tokens.Add(new Token(TokenType.PERCENT, "%", _line));
							break;
						case '<':
							_tokens.Add(new Token(TokenType.LT, "<", _line));
							break;
						case '>':
							_tokens.Add(new Token(TokenType.GT, ">", _line));
							break;
						case '=':
							_tokens.Add(new Token(TokenType.ASSIGN, "=", _line));
							break;
						case '(':
							_tokens.Add(new Token(TokenType.LPAREN, "(", _line));
							break;
						case ')':
							_tokens.Add(new Token(TokenType.RPAREN, ")", _line));
							break;
						case '[':
							_tokens.Add(new Token(TokenType.LBRACKET, "[", _line));
							break;
						case ']':
							_tokens.Add(new Token(TokenType.RBRACKET, "]", _line));
							break;
						case ':':
							_tokens.Add(new Token(TokenType.COLON, ":", _line));
							break;
						case ',':
							_tokens.Add(new Token(TokenType.COMMA, ",", _line));
							break;
						case '.':
							_tokens.Add(new Token(TokenType.DOT, ".", _line));
							break;
						default:
							throw new Exception($"Unknown token '{c}' at line {_line}");
					}
					_pos++;
					_column++;
				}

				// End of line
				_tokens.Add(new Token(TokenType.NEWLINE, "\\n", _line));
				_line++;
			}

			// At EOF, unwind remaining indents
			while (indentStack.Count > 1)
			{
				indentStack.Pop();
				_tokens.Add(new Token(TokenType.DEDENT, "", _line));
			}
			_tokens.Add(new Token(TokenType.EOF, "", _line));
		}

		private TokenType GetKeywordType(string name)
		{
			switch (name)
			{
				case "if": return TokenType.IF;
				case "else": return TokenType.ELSE;
				case "while": return TokenType.WHILE;
				// 3. Add to PythonLexer.cs in GetKeywordType method:
				case "for": return TokenType.FOR;
				case "in": return TokenType.IN;

				case "def": return TokenType.DEF;
				case "return": return TokenType.RETURN;
				case "pass": return TokenType.PASS;
				case "not": return TokenType.NOT;
				case "and": return TokenType.AND;
				case "or": return TokenType.OR;
				case "global": return TokenType.GLOBAL;
			}
			return TokenType.NAME;
		}
	}
}