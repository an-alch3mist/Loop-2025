using System;
using System.Collections.Generic;

namespace GptDeepResearch
{
	// Parser that builds AST from tokens
	public class PythonParser
	{
		private List<Token> _tokens;
		private int _pos = 0;

		public PythonParser(List<Token> tokens)
		{
			_tokens = tokens;
		}

		private Token Current => _pos < _tokens.Count ? _tokens[_pos] : null;
		private Token Next()
		{
			_pos += 1;
			return Current;
		}
		private bool Match(TokenType type)
		{
			if (Current != null && Current.Type == type)
			{
				Next();
				return true;
			}
			return false;
		}
		private Token Expect(TokenType type, string msg)
		{
			if (Current != null && Current.Type == type)
			{
				Token t = Current;
				Next();
				return t;
			}
			throw new Exception($"Expected {msg} at line {Current?.Line}");
		}

		public List<Stmt> Parse()
		{
			List<Stmt> statements = new List<Stmt>();
			while (Current != null && Current.Type != TokenType.EOF)
			{
				if (Current.Type == TokenType.NEWLINE)
				{
					Next();
					continue;
				}
				if (Current.Type == TokenType.DEDENT)
				{
					// Skip stray DEDENT
					Next();
					continue;
				}
				statements.Add(ParseStatement());
			}
			return statements;
		}

		private Stmt ParseStatement()
		{
			Token token = Current;
			switch (token.Type)
			{
				case TokenType.IF:
					return ParseIf();
				case TokenType.WHILE:
					return ParseWhile();
				// 6. Add to PythonParser.cs in ParseStatement method:
				case TokenType.FOR:
					return ParseFor();
				case TokenType.DEF:
					return ParseFunctionDef();
				case TokenType.RETURN:
					return ParseReturn();
				case TokenType.PASS:
					return ParsePass();
				case TokenType.GLOBAL:  // Add this case
					return ParseGlobal();
				default:
					// Assignment or expression
					if (token.Type == TokenType.NAME && PeekNextType() == TokenType.ASSIGN)
					{
						return ParseAssignment();
					}
					else
					{
						Expr expr = ParseExpression();
						Expect(TokenType.NEWLINE, "newline");
						return new ExpressionStmt(expr, token.Line);
					}
			}
		}

		// Add ParseGlobal method:
		private Stmt ParseGlobal()
		{
			Token globalToken = Expect(TokenType.GLOBAL, "global");
			List<string> names = new List<string>();
			do
			{
				Token nameToken = Expect(TokenType.NAME, "variable name");
				names.Add(nameToken.Text);
			} while (Match(TokenType.COMMA));
			Expect(TokenType.NEWLINE, "newline");
			return new GlobalStmt(names, globalToken.Line);
		}

		private TokenType PeekNextType()
		{
			if (_pos + 1 < _tokens.Count)
				return _tokens[_pos + 1].Type;
			return TokenType.EOF;
		}

		private Stmt ParseAssignment()
		{
			Token nameToken = Expect(TokenType.NAME, "identifier");
			string name = nameToken.Text;
			Expect(TokenType.ASSIGN, "'='");
			Expr value = ParseExpression();
			Expect(TokenType.NEWLINE, "newline");
			return new AssignStmt(name, value, nameToken.Line);
		}

		private Stmt ParseIf()
		{
			Token ifToken = Expect(TokenType.IF, "if");
			Expr condition = ParseExpression();
			Expect(TokenType.COLON, "':'");
			Expect(TokenType.NEWLINE, "newline");
			Expect(TokenType.INDENT, "indent");
			List<Stmt> thenBranch = new List<Stmt>();
			while (Current.Type != TokenType.DEDENT && Current.Type != TokenType.EOF)
			{
				thenBranch.Add(ParseStatement());
			}
			Expect(TokenType.DEDENT, "dedent");
			List<Stmt> elseBranch = null;
			if (Current.Type == TokenType.ELSE)
			{
				Expect(TokenType.ELSE, "else");
				Expect(TokenType.COLON, "':'");
				Expect(TokenType.NEWLINE, "newline");
				Expect(TokenType.INDENT, "indent");
				elseBranch = new List<Stmt>();
				while (Current.Type != TokenType.DEDENT && Current.Type != TokenType.EOF)
				{
					elseBranch.Add(ParseStatement());
				}
				Expect(TokenType.DEDENT, "dedent");
			}
			return new IfStmt(condition, thenBranch, elseBranch, ifToken.Line);
		}

		private Stmt ParseWhile()
		{
			Token whileToken = Expect(TokenType.WHILE, "while");
			Expr condition = ParseExpression();
			Expect(TokenType.COLON, "':'");
			Expect(TokenType.NEWLINE, "newline");
			Expect(TokenType.INDENT, "indent");
			List<Stmt> body = new List<Stmt>();
			while (Current.Type != TokenType.DEDENT && Current.Type != TokenType.EOF)
			{
				body.Add(ParseStatement());
			}
			Expect(TokenType.DEDENT, "dedent");
			return new WhileStmt(condition, body, whileToken.Line);
		}
		// 7. Add ParseFor method to PythonParser.cs:
		private Stmt ParseFor()
		{
			Token forToken = Expect(TokenType.FOR, "for");
			Token varToken = Expect(TokenType.NAME, "variable name");
			string variable = varToken.Text;
			Expect(TokenType.IN, "in");
			Expr iterable = ParseExpression();
			Expect(TokenType.COLON, "':'");
			Expect(TokenType.NEWLINE, "newline");
			Expect(TokenType.INDENT, "indent");

			List<Stmt> body = new List<Stmt>();
			while (Current.Type != TokenType.DEDENT && Current.Type != TokenType.EOF)
			{
				body.Add(ParseStatement());
			}
			Expect(TokenType.DEDENT, "dedent");

			return new ForStmt(variable, iterable, body, forToken.Line);
		}


		private Stmt ParseFunctionDef()
		{
			Token defToken = Expect(TokenType.DEF, "def");
			Token nameToken = Expect(TokenType.NAME, "function name");
			string name = nameToken.Text;
			Expect(TokenType.LPAREN, "'('");
			List<string> parameters = new List<string>();
			if (Current.Type != TokenType.RPAREN)
			{
				do
				{
					Token param = Expect(TokenType.NAME, "parameter name");
					parameters.Add(param.Text);
				} while (Match(TokenType.COMMA));
			}
			Expect(TokenType.RPAREN, "')'");
			Expect(TokenType.COLON, "':'");
			Expect(TokenType.NEWLINE, "newline");
			Expect(TokenType.INDENT, "indent");
			List<Stmt> body = new List<Stmt>();
			while (Current.Type != TokenType.DEDENT && Current.Type != TokenType.EOF)
			{
				body.Add(ParseStatement());
			}
			Expect(TokenType.DEDENT, "dedent");
			return new FunctionDefStmt(name, parameters, body, defToken.Line);
		}

		private Stmt ParseReturn()
		{
			Token retToken = Expect(TokenType.RETURN, "return");
			Expr value = null;
			if (Current.Type != TokenType.NEWLINE)
			{
				value = ParseExpression();
			}
			Expect(TokenType.NEWLINE, "newline");
			return new ReturnStmt(value, retToken.Line);
		}

		private Stmt ParsePass()
		{
			Token passToken = Expect(TokenType.PASS, "pass");
			Expect(TokenType.NEWLINE, "newline");
			return new PassStmt(passToken.Line);
		}

		private Expr ParseExpression()
		{
			return ParseOr();
		}

		private Expr ParseOr()
		{
			Expr expr = ParseAnd();
			while (Current.Type == TokenType.OR)
			{
				Token op = Current; Next();
				Expr right = ParseAnd();
				expr = new BinaryExpr(expr, TokenType.OR, right, op.Line);
			}
			return expr;
		}

		private Expr ParseAnd()
		{
			Expr expr = ParseNot();
			while (Current.Type == TokenType.AND)
			{
				Token op = Current; Next();
				Expr right = ParseNot();
				expr = new BinaryExpr(expr, TokenType.AND, right, op.Line);
			}
			return expr;
		}

		private Expr ParseNot()
		{
			if (Current.Type == TokenType.NOT)
			{
				Token op = Current; Next();
				Expr operand = ParseNot();
				return new UnaryExpr(TokenType.NOT, operand, op.Line);
			}
			return ParseCompare();
		}

		private Expr ParseCompare()
		{
			Expr expr = ParseAddSubtract();
			if (Current.Type == TokenType.EQ || Current.Type == TokenType.NEQ ||
				Current.Type == TokenType.LT || Current.Type == TokenType.GT ||
				Current.Type == TokenType.LTE || Current.Type == TokenType.GTE)
			{
				Token op = Current; Next();
				Expr right = ParseAddSubtract();
				expr = new BinaryExpr(expr, op.Type, right, op.Line);
			}
			return expr;
		}

		private Expr ParseAddSubtract()
		{
			Expr expr = ParseTerm();
			while (Current.Type == TokenType.PLUS || Current.Type == TokenType.MINUS)
			{
				Token op = Current; Next();
				Expr right = ParseTerm();
				expr = new BinaryExpr(expr, op.Type, right, op.Line);
			}
			return expr;
		}

		private Expr ParseTerm()
		{
			Expr expr = ParseFactor();
			while (Current.Type == TokenType.STAR || Current.Type == TokenType.SLASH || Current.Type == TokenType.PERCENT)
			{
				Token op = Current; Next();
				Expr right = ParseFactor();
				expr = new BinaryExpr(expr, op.Type, right, op.Line);
			}
			return expr;
		}

		private Expr ParseFactor()
		{
			Token token = Current;
			if (token.Type == TokenType.MINUS)
			{
				Next();
				Expr operand = ParseFactor();
				return new UnaryExpr(TokenType.MINUS, operand, token.Line);
			}
			if (token.Type == TokenType.NUMBER)
			{
				Next();
				double val;
				if (!double.TryParse(token.Text, out val))
				{
					throw new Exception($"Invalid number '{token.Text}' at line {token.Line}");
				}
				return new NumberExpr(val, token.Line);
			}
			if (token.Type == TokenType.STRING)
			{
				Next();
				string s = token.Text;
				if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
				{
					s = s.Substring(1, s.Length - 2);
				}
				return new StringExpr(s, token.Line);
			}
			if (token.Type == TokenType.BOOLEAN)
			{
				Next();
				bool val = token.Text == "True";
				return new BooleanExpr(val, token.Line);
			}
			if (token.Type == TokenType.NAME)
			{
				Next();
				Expr node = new NameExpr(token.Text, token.Line);
				return ParseCallIndexAttribute(node);
			}
			if (token.Type == TokenType.LPAREN)
			{
				Next();
				Expr expr = ParseExpression();
				Expect(TokenType.RPAREN, "')'");
				return ParseCallIndexAttribute(expr);
			}
			if (token.Type == TokenType.LBRACKET)
			{
				// list literal
				Next();
				List<Expr> elements = new List<Expr>();
				if (Current.Type != TokenType.RBRACKET)
				{
					do
					{
						Expr e = ParseExpression();
						elements.Add(e);
					} while (Match(TokenType.COMMA));
				}
				Expect(TokenType.RBRACKET, "']'");
				return new ListExpr(elements, token.Line);
			}
			throw new Exception($"Unexpected token '{token.Text}' at line {token.Line}");
		}

		private Expr ParseCallIndexAttribute(Expr node)
		{
			while (true)
			{
				if (Current.Type == TokenType.LPAREN)
				{
					// function or method call
					Next(); // consume '('
					List<Expr> args = new List<Expr>();
					if (Current.Type != TokenType.RPAREN)
					{
						do
						{
							args.Add(ParseExpression());
						} while (Match(TokenType.COMMA));
					}
					Expect(TokenType.RPAREN, "')'");
					node = new CallExpr(node, args, node.Line);
				}
				else if (Current.Type == TokenType.DOT)
				{
					Next(); // consume '.'
					Token nameToken = Expect(TokenType.NAME, "attribute name");
					node = new AttributeExpr(node, nameToken.Text, node.Line);
				}
				else if (Current.Type == TokenType.LBRACKET)
				{
					// indexing or slicing
					Next(); // consume '['
					Expr start = null;
					Expr end = null;
					if (Current.Type != TokenType.COLON)
					{
						start = ParseExpression();
					}
					if (Current.Type == TokenType.COLON)
					{
						Next(); // consume ':'
						if (Current.Type != TokenType.RBRACKET)
						{
							end = ParseExpression();
						}
						Expect(TokenType.RBRACKET, "']'");
						node = new SliceExpr(node, start, end, node.Line);
					}
					else
					{
						Expect(TokenType.RBRACKET, "']'");
						node = new IndexExpr(node, start, node.Line);
					}
				}
				else
				{
					break;
				}
			}
			return node;
		}
	}
}
