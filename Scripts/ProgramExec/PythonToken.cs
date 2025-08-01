using System;
using System.Collections.Generic;

namespace GptDeepResearch
{
	// Token types for the Python mini-language
	public enum TokenType
	{
		EOF,
		INDENT,
		DEDENT,
		NEWLINE,
		// Literals
		NUMBER,
		STRING,
		BOOLEAN,
		// Identifiers and Keywords
		NAME,
		// Operators
		PLUS,       // +
		MINUS,      // -
		STAR,       // *
		SLASH,      // /
		PERCENT,    // %
		EQ,         // ==
		NEQ,        // !=
		LT,         // <
		GT,         // >
		LTE,        // <=
		GTE,        // >=
		ASSIGN,     // =
		LPAREN,     // (
		RPAREN,     // )
		LBRACKET,   // [
		RBRACKET,   // ]
		COLON,      // :
		COMMA,      // ,
		DOT,        // .
					// Keywords
		IF,
		ELSE,
		WHILE,
		FOR, // Add this line to the enum
		IN, // Add this to TokenType enum

		DEF,
		RETURN,
		PASS,
		NOT,
		AND,
		OR,

		GLOBAL,     // Add this line
	}

	public class Token
	{
		public TokenType Type;
		public string Text;
		public int Line;

		public Token(TokenType type, string text, int line)
		{
			Type = type;
			Text = text;
			Line = line;
		}

		public override string ToString() => $"{Type}({Text}) on line {Line}";
	}
}
