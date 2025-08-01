using System.Collections.Generic;

namespace GptDeepResearch
{
	// Abstract base classes for AST nodes
	public abstract class AstNode { public int Line; }
	public abstract class Expr : AstNode { }
	public abstract class Stmt : AstNode { }

	// Expressions
	public class NumberExpr : Expr
	{
		public double Value;
		public NumberExpr(double value, int line) { Value = value; Line = line; }
	}

	public class StringExpr : Expr
	{
		public string Value;
		public StringExpr(string value, int line) { Value = value; Line = line; }
	}

	public class BooleanExpr : Expr
	{
		public bool Value;
		public BooleanExpr(bool value, int line) { Value = value; Line = line; }
	}

	public class NameExpr : Expr
	{
		public string Name;
		public NameExpr(string name, int line) { Name = name; Line = line; }
	}

	public class ListExpr : Expr
	{
		public List<Expr> Elements;
		public ListExpr(List<Expr> elements, int line) { Elements = elements; Line = line; }
	}

	public class BinaryExpr : Expr
	{
		public Expr Left;
		public TokenType Op;
		public Expr Right;
		public BinaryExpr(Expr left, TokenType op, Expr right, int line)
		{
			Left = left; Op = op; Right = right; Line = line;
		}
	}

	public class UnaryExpr : Expr
	{
		public TokenType Op;
		public Expr Operand;
		public UnaryExpr(TokenType op, Expr operand, int line)
		{
			Op = op; Operand = operand; Line = line;
		}
	}

	public class CallExpr : Expr
	{
		public Expr Callee;
		public List<Expr> Arguments;
		public CallExpr(Expr callee, List<Expr> args, int line)
		{
			Callee = callee; Arguments = args; Line = line;
		}
	}

	public class AttributeExpr : Expr
	{
		public Expr Target;
		public string Name;
		public AttributeExpr(Expr target, string name, int line)
		{
			Target = target; Name = name; Line = line;
		}
	}

	public class IndexExpr : Expr
	{
		public Expr Target;
		public Expr Index;
		public IndexExpr(Expr target, Expr index, int line)
		{
			Target = target; Index = index; Line = line;
		}
	}

	public class SliceExpr : Expr
	{
		public Expr Target;
		public Expr Start;  // may be null
		public Expr End;    // may be null
		public SliceExpr(Expr target, Expr start, Expr end, int line)
		{
			Target = target; Start = start; End = end; Line = line;
		}
	}

	// Statements
	public class ExpressionStmt : Stmt
	{
		public Expr Expression;
		public ExpressionStmt(Expr expr, int line) { Expression = expr; Line = line; }
	}

	public class AssignStmt : Stmt
	{
		public string Target;
		public Expr Value;
		public AssignStmt(string target, Expr value, int line)
		{
			Target = target; Value = value; Line = line;
		}
	}

	public class IfStmt : Stmt
	{
		public Expr Condition;
		public List<Stmt> ThenBranch;
		public List<Stmt> ElseBranch;
		public IfStmt(Expr cond, List<Stmt> thenBranch, List<Stmt> elseBranch, int line)
		{
			Condition = cond; ThenBranch = thenBranch; ElseBranch = elseBranch; Line = line;
		}
	}

	public class WhileStmt : Stmt
	{
		public Expr Condition;
		public List<Stmt> Body;
		public WhileStmt(Expr cond, List<Stmt> body, int line)
		{
			Condition = cond; Body = body; Line = line;
		}
	}

	// 5. Add to PythonAST.cs - new ForStmt class:
	public class ForStmt : Stmt
	{
		public string Variable;      // Loop variable name
		public Expr Iterable;       // What to iterate over
		public List<Stmt> Body;     // Loop body

		public ForStmt(string variable, Expr iterable, List<Stmt> body, int line)
		{
			Variable = variable;
			Iterable = iterable;
			Body = body;
			Line = line;
		}
	}


	public class FunctionDefStmt : Stmt
	{
		public string Name;
		public List<string> Parameters;
		public List<Stmt> Body;
		public FunctionDefStmt(string name, List<string> parameters, List<Stmt> body, int line)
		{
			Name = name; Parameters = parameters; Body = body; Line = line;
		}
	}

	public class ReturnStmt : Stmt
	{
		public Expr Value; // may be null for "return"
		public ReturnStmt(Expr value, int line) { Value = value; Line = line; }
	}

	public class PassStmt : Stmt
	{
		public PassStmt(int line) { Line = line; }
	}

	public class GlobalStmt : Stmt
	{
		public List<string> Names;
		public GlobalStmt(List<string> names, int line) { Names = names; Line = line; }
	}
}
