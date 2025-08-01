using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GptDeepResearch
{
	// Exception used to handle return values from functions
	public class ReturnException : Exception
	{
		public object Value;
		public ReturnException(object value)
		{
			Value = value;
		}
	}

	// The interpreter evaluates the AST using coroutine-based execution
	public class PythonInterpreter
	{
		private Dictionary<string, object> Globals = new Dictionary<string, object>();
		private Dictionary<string, FunctionDefStmt> Functions = new Dictionary<string, FunctionDefStmt>();
		private Stack<Dictionary<string, object>> LocalsStack = new Stack<Dictionary<string, object>>();
		private Stack<HashSet<string>> GlobalDeclsStack = new Stack<HashSet<string>>(); // Add this line


		public PythonInterpreter()
		{
			// No special built-in initialization needed here
		}

		// Main entry: execute a list of statements
		public IEnumerator Execute(List<Stmt> statements)
		{
			foreach (Stmt stmt in statements)
			{
				IEnumerator stmtEnum = ExecStmt(stmt);
				while (stmtEnum.MoveNext())
				{
					yield return stmtEnum.Current;
				}
				// Step delay (yield null for CoroutineRunner to apply stepDelay)
				yield return null;
			}
		}

		// Update ExecStmt method to handle GlobalStmt:
		private IEnumerator ExecStmt(Stmt stmt)
		{
			if (stmt is ExpressionStmt es)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				// Evaluate expression and discard result (for side-effects)
				object value = null;
				IEnumerator exprEnum = ExecExpr(es.Expression, val => value = val);
				while (exprEnum.MoveNext())
				{
					yield return exprEnum.Current;
				}
			}
			else if (stmt is AssignStmt asg)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				object value = null;
				IEnumerator exprEnum = ExecExpr(asg.Value, val => value = val);
				while (exprEnum.MoveNext())
				{
					yield return exprEnum.Current;
				}
				SetVariable(asg.Target, value);
			}
			else if (stmt is GlobalStmt gs)  // Add this case
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				if (GlobalDeclsStack.Count > 0)
				{
					var globalDecls = GlobalDeclsStack.Peek();
					foreach (string name in gs.Names)
					{
						globalDecls.Add(name);
					}
				}
			}
			// ... rest of existing cases ...
			else if (stmt is IfStmt ifs)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				object cond = null;
				IEnumerator condEnum = ExecExpr(ifs.Condition, val => cond = val);
				while (condEnum.MoveNext())
				{
					yield return condEnum.Current;
				}
				if (IsTrue(cond))
				{
					if (ifs.ThenBranch != null)
					{
						foreach (Stmt inner in ifs.ThenBranch)
						{
							IEnumerator innerEnum = ExecStmt(inner);
							while (innerEnum.MoveNext())
							{
								yield return innerEnum.Current;
							}
						}
					}
				}
				else if (ifs.ElseBranch != null)
				{
					foreach (Stmt inner in ifs.ElseBranch)
					{
						IEnumerator innerEnum = ExecStmt(inner);
						while (innerEnum.MoveNext())
						{
							yield return innerEnum.Current;
						}
					}
				}
			}
			else if (stmt is WhileStmt ws)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				while (true)
				{
					object cond = null;
					var condEnum = ExecExpr(ws.Condition, v => cond = v);
					while (condEnum.MoveNext()) yield return condEnum.Current;
					if (!IsTrue(cond)) break;
					foreach (var s in ws.Body)
					{
						var innerEnum = ExecStmt(s);
						while (innerEnum.MoveNext()) yield return innerEnum.Current;
					}
					// Step delay each iteration
					yield return null;
				}
			}
			// 8. Add to PythonInterpreter.cs in ExecStmt method:
			else if (stmt is ForStmt fs)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				// Evaluate the iterable
				object iterableObj = null;
				IEnumerator iterEnum = ExecExpr(fs.Iterable, val => iterableObj = val);
				while (iterEnum.MoveNext())
					yield return iterEnum.Current;

				// Check if the object is iterable
				if (iterableObj is List<object> list)
				{
					// Iterate over each item in the list
					foreach (object item in list)
					{
						// Set the loop variable
						SetVariable(fs.Variable, item);

						// Execute the loop body
						foreach (Stmt bodyStmt in fs.Body)
						{
							IEnumerator bodyEnum = ExecStmt(bodyStmt);
							while (bodyEnum.MoveNext())
								yield return bodyEnum.Current;
						}

						// Step delay each iteration
						yield return null;
					}
				}
				else if (iterableObj is string str)
				{
					// Iterate over each character in the string
					foreach (char c in str)
					{
						SetVariable(fs.Variable, c.ToString());

						foreach (Stmt bodyStmt in fs.Body)
						{
							IEnumerator bodyEnum = ExecStmt(bodyStmt);
							while (bodyEnum.MoveNext())
								yield return bodyEnum.Current;
						}

						yield return null;
					}
				}
				else
				{
					throw new Exception($"Object of type '{iterableObj?.GetType().Name}' is not iterable at line {fs.Line}");
				}
			}
			else if (stmt is FunctionDefStmt fdef)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				// Store function definition for later calls
				Functions[fdef.Name] = fdef;
			}
			else if (stmt is ReturnStmt ret)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				object returnValue = null;
				if (ret.Value != null)
				{
					IEnumerator exprEnum = ExecExpr(ret.Value, val => returnValue = val);
					while (exprEnum.MoveNext())
					{
						yield return exprEnum.Current;
					}
				}
				throw new ReturnException(returnValue);
			}
			else if (stmt is PassStmt)
			{
				// At the beginning of ExecStmt method, add:
				ExecutionTracker.NotifyLineExecution(stmt.Line);

				// Do nothing
			}
			else
			{
				Debug.Log($"instead of throw new Exeption: Unknown statement type at line {stmt.Line}");
				yield break;
				// throw new Exception($"Unknown statement type at line {stmt.Line}");
			}
		}

		private IEnumerator ExecExpr(Expr expr, Action<object> setValue)
		{
			if (expr is NumberExpr ne)
			{
				setValue(ne.Value);
			}
			else if (expr is StringExpr se)
			{
				setValue(se.Value);
			}
			else if (expr is BooleanExpr booe)
			{
				setValue(booe.Value);
			}
			else if (expr is NameExpr nae)
			{
				string name = nae.Name;
				object val;
				if (GetVariable(name, out val))
				{
					setValue(val);
				}
				else
				{
					throw new Exception($"Name '{name}' is not defined at line {nae.Line}");
				}
			}
			else if (expr is ListExpr le)
			{
				List<object> list = new List<object>();
				foreach (Expr el in le.Elements)
				{
					object elemVal = null;
					IEnumerator elemEnum = ExecExpr(el, val => elemVal = val);
					while (elemEnum.MoveNext())
					{
						yield return elemEnum.Current;
					}
					list.Add(elemVal);
				}
				setValue(list);
			}
			else if (expr is BinaryExpr be)
			{
				object leftVal = null;
				IEnumerator leftEnum = ExecExpr(be.Left, val => leftVal = val);
				while (leftEnum.MoveNext())
				{
					yield return leftEnum.Current;
				}
				object rightVal = null;
				IEnumerator rightEnum = ExecExpr(be.Right, val => rightVal = val);
				while (rightEnum.MoveNext())
				{
					yield return rightEnum.Current;
				}
				object result = null;
				switch (be.Op)
				{
					case TokenType.PLUS:
						if (leftVal is string || rightVal is string)
						{
							result = leftVal.ToString() + rightVal.ToString();
						}
						else
						{
							double a = Convert.ToDouble(leftVal);
							double b = Convert.ToDouble(rightVal);
							result = a + b;
						}
						break;
					case TokenType.MINUS:
						result = Convert.ToDouble(leftVal) - Convert.ToDouble(rightVal);
						break;
					case TokenType.STAR:
						result = Convert.ToDouble(leftVal) * Convert.ToDouble(rightVal);
						break;
					case TokenType.SLASH:
						result = Convert.ToDouble(leftVal) / Convert.ToDouble(rightVal);
						break;
					case TokenType.PERCENT:
						result = Convert.ToDouble(leftVal) % Convert.ToDouble(rightVal);
						break;
					case TokenType.EQ:
						result = Equals(leftVal, rightVal);
						break;
					case TokenType.NEQ:
						result = !Equals(leftVal, rightVal);
						break;
					case TokenType.LT:
						result = CompareValues(leftVal, rightVal) < 0;
						break;
					case TokenType.GT:
						result = CompareValues(leftVal, rightVal) > 0;
						break;
					case TokenType.LTE:
						result = CompareValues(leftVal, rightVal) <= 0;
						break;
					case TokenType.GTE:
						result = CompareValues(leftVal, rightVal) >= 0;
						break;
					case TokenType.AND:
						result = IsTrue(leftVal) && IsTrue(rightVal);
						break;
					case TokenType.OR:
						result = IsTrue(leftVal) || IsTrue(rightVal);
						break;
					default:
						throw new Exception($"Unsupported binary operator {be.Op} at line {be.Line}");
				}
				setValue(result);
			}
			else if (expr is UnaryExpr ue)
			{
				object operandVal = null;
				IEnumerator operandEnum = ExecExpr(ue.Operand, val => operandVal = val);
				while (operandEnum.MoveNext())
				{
					yield return operandEnum.Current;
				}
				object result = null;
				if (ue.Op == TokenType.MINUS)
				{
					result = -Convert.ToDouble(operandVal);
				}
				else if (ue.Op == TokenType.NOT)
				{
					result = !IsTrue(operandVal);
				}
				else
				{
					throw new Exception($"Unsupported unary operator {ue.Op} at line {ue.Line}");
				}
				setValue(result);
			}
			else if (expr is CallExpr ce)
			{
				// Handle function and method calls
				if (ce.Callee is NameExpr)
				{
					string fname = ((NameExpr)ce.Callee).Name;
					// Evaluate arguments
					List<object> args = new List<object>();
					foreach (Expr arg in ce.Arguments)
					{
						object argVal = null;
						IEnumerator argEnum = ExecExpr(arg, val => argVal = val);
						while (argEnum.MoveNext())
						{
							yield return argEnum.Current;
						}
						args.Add(argVal);
					}
					yield return HandleBuiltinFunction(fname, args, setValue, ce);

				}
				else if (ce.Callee is AttributeExpr ae)
				{
					// Method call on object (only list methods are supported)
					object targetObj = null;
					IEnumerator targetEnum = ExecExpr(ae.Target, val => targetObj = val);
					while (targetEnum.MoveNext())
					{
						yield return targetEnum.Current;
					}
					string method = ae.Name;
					if (targetObj is List<object> listObj)
					{
						if (method == "append")
						{
							if (ce.Arguments.Count != 1)
								throw new Exception($"append() takes 1 argument at line {ce.Line}");
							object argVal = null;
							IEnumerator argEnum = ExecExpr(ce.Arguments[0], val => argVal = val);
							while (argEnum.MoveNext())
							{
								yield return argEnum.Current;
							}
							listObj.Add(argVal);
							setValue(null);
						}
						else if (method == "remove")
						{
							if (ce.Arguments.Count != 1)
								throw new Exception($"remove() takes 1 argument at line {ce.Line}");
							object argVal = null;
							IEnumerator argEnum = ExecExpr(ce.Arguments[0], val => argVal = val);
							while (argEnum.MoveNext())
							{
								yield return argEnum.Current;
							}
							if (!listObj.Remove(argVal))
							{
								throw new Exception($"Value not found in list at line {ce.Line}");
							}
							setValue(null);
						}
						else if (method == "pop")
						{
							int index = -1;
							if (ce.Arguments.Count == 0)
							{
								index = listObj.Count - 1;
							}
							else if (ce.Arguments.Count == 1)
							{
								object argVal = null;
								IEnumerator argEnum = ExecExpr(ce.Arguments[0], val => argVal = val);
								while (argEnum.MoveNext())
								{
									yield return argEnum.Current;
								}
								index = Convert.ToInt32(argVal);
							}
							else
							{
								throw new Exception($"pop() takes at most 1 argument at line {ce.Line}");
							}
							if (index < 0) index = listObj.Count + index;
							if (index < 0 || index >= listObj.Count)
								throw new Exception($"pop index out of range at line {ce.Line}");
							object popped = listObj[index];
							listObj.RemoveAt(index);
							setValue(popped);
						}
						else
						{
							throw new Exception($"Unknown list method '{method}' at line {ce.Line}");
						}
					}
					else
					{
						throw new Exception($"Object has no attribute '{method}' at line {ce.Line}");
					}
				}
				else
				{
					throw new Exception($"Invalid function call target at line {ce.Line}");
				}
			}
			else if (expr is IndexExpr ie)
			{
				object target = null;
				IEnumerator targetEnum = ExecExpr(ie.Target, val => target = val);
				while (targetEnum.MoveNext())
				{
					yield return targetEnum.Current;
				}
				object indexVal = null;
				IEnumerator idxEnum = ExecExpr(ie.Index, val => indexVal = val);
				while (idxEnum.MoveNext())
				{
					yield return idxEnum.Current;
				}
				if (target is List<object> listObj)
				{
					int idx = Convert.ToInt32(indexVal);
					if (idx < 0) idx = listObj.Count + idx;
					if (idx < 0 || idx >= listObj.Count)
						throw new Exception($"list index out of range at line {ie.Line}");
					setValue(listObj[idx]);
				}
				else
				{
					throw new Exception($"Type {target?.GetType()} is not subscriptable at line {ie.Line}");
				}
			}
			else if (expr is SliceExpr sle)
			{
				object target = null;
				IEnumerator targetEnum = ExecExpr(sle.Target, val => target = val);
				while (targetEnum.MoveNext())
				{
					yield return targetEnum.Current;
				}
				if (target is List<object> listObj)
				{
					int startIndex = 0;
					int endIndex = listObj.Count;
					if (sle.Start != null)
					{
						object startVal = null;
						IEnumerator startEnum = ExecExpr(sle.Start, val => startVal = val);
						while (startEnum.MoveNext())
						{
							yield return startEnum.Current;
						}
						startIndex = Convert.ToInt32(startVal);
						if (startIndex < 0) startIndex = listObj.Count + startIndex;
						if (startIndex < 0) startIndex = 0;
						if (startIndex > listObj.Count) startIndex = listObj.Count;
					}
					if (sle.End != null)
					{
						object endVal = null;
						IEnumerator endEnum = ExecExpr(sle.End, val => endVal = val);
						while (endEnum.MoveNext())
						{
							yield return endEnum.Current;
						}
						endIndex = Convert.ToInt32(endVal);
						if (endIndex < 0) endIndex = listObj.Count + endIndex;
						if (endIndex < 0) endIndex = 0;
						if (endIndex > listObj.Count) endIndex = listObj.Count;
					}
					List<object> sliceList = new List<object>();
					for (int i = startIndex; i < endIndex; i++)
					{
						sliceList.Add(listObj[i]);
					}
					setValue(sliceList);
				}
				else
				{
					throw new Exception($"Type {target?.GetType()} does not support slicing at line {sle.Line}");
				}
			}
			else
			{
				throw new Exception($"Unknown expression type at line {expr.Line}");
			}
		}




		// Add this method to your PythonInterpreter class
		private IEnumerator HandleBuiltinFunction(string fname, List<object> args, Action<object> setValue, CallExpr ce)
		{

			// old approach
			#region old approach
			/*
			if (fname == "print")
			{
				// Debug.Log("fname: is print");
				// Print all arguments separated by space
				string output = "";
				for (int i = 0; i < args.Count; i++)
				{
					if (i > 0) output += " ";
					output += (args[i] != null ? args[i].ToString() : "None");
				}
				Debug.Log(output);
				setValue(null);
			}
			else if (fname == "sleep")
			{
				double seconds = 0;
				if (args.Count > 0)
					seconds = Convert.ToDouble(args[0]);
				// Yield real-time wait
				yield return new WaitForSecondsRealtime((float)seconds);
				setValue(null);
			}
			else if (Functions.ContainsKey(fname))
			{
				// User-defined function call
				object funcResult = null;
				IEnumerator funcEnum = ExecFunction(Functions[fname], args, val => funcResult = val);
				while (funcEnum.MoveNext())
				{
					yield return funcEnum.Current;
				}
				setValue(funcResult);
			}
			else
			{
				throw new Exception($"Unknown function '{fname}' at line {ce.Line}");
			}
			*/
			#endregion

			// new apprach
			#region new approach
			switch (fname)
			{
				case "print":
					string output = "";
					for (int i = 0; i < args.Count; i++)
					{
						if (i > 0) output += " ";
						output += (args[i] != null ? args[i].ToString() : "None");
					}
					Debug.Log(output);
					setValue(null);
					break;

				case "len":
					if (args.Count != 1)
						throw new Exception($"len() takes exactly one argument ({args.Count} given)");

					object obj = args[0];
					if (obj is List<object> list)
						setValue(list.Count);
					else if (obj is string str)
						setValue(str.Length);
					else
						throw new Exception($"object of type '{obj?.GetType().Name}' has no len()");
					break;

				case "range":
					if (args.Count == 1)
					{
						// range(stop)
						int stop = Convert.ToInt32(args[0]);
						List<object> rangeList = new List<object>();
						for (int i = 0; i < stop; i++)
							rangeList.Add((double)i);
						setValue(rangeList);
					}
					else if (args.Count == 2)
					{
						// range(start, stop)
						int start = Convert.ToInt32(args[0]);
						int stop = Convert.ToInt32(args[1]);
						List<object> rangeList = new List<object>();
						for (int i = start; i < stop; i++)
							rangeList.Add((double)i);
						setValue(rangeList);
					}
					else if (args.Count == 3)
					{
						// range(start, stop, step)
						int start = Convert.ToInt32(args[0]);
						int stop = Convert.ToInt32(args[1]);
						int step = Convert.ToInt32(args[2]);
						if (step == 0)
							throw new Exception("range() arg 3 must not be zero");

						List<object> rangeList = new List<object>();
						if (step > 0)
						{
							for (int i = start; i < stop; i += step)
								rangeList.Add((double)i);
						}
						else
						{
							for (int i = start; i > stop; i += step)
								rangeList.Add((double)i);
						}
						setValue(rangeList);
					}
					else
					{
						throw new Exception($"range expected at most 3 arguments, got {args.Count}");
					}
					break;

				case "sleep":
					double seconds = 0;
					if (args.Count > 0)
						seconds = Convert.ToDouble(args[0]);
					yield return new WaitForSecondsRealtime((float)seconds);
					setValue(null);
					break;

				default:
					// Check if it's a game built-in function
					if (GameBuiltins.IsBuiltinFunction(fname))
					{
						var gameBuiltinEnum = GameBuiltins.ExecuteBuiltinFunction(fname, args.ToArray(), setValue);
						while (gameBuiltinEnum.MoveNext())
							yield return gameBuiltinEnum.Current;
					}
					else if (Functions.ContainsKey(fname))
					{
						// User-defined function call
						object funcResult = null;
						IEnumerator funcEnum = ExecFunction(Functions[fname], args, val => funcResult = val);
						while (funcEnum.MoveNext())
							yield return funcEnum.Current;
						setValue(funcResult);
					}
					else
					{
						throw new Exception($"Unknown function '{fname}'");
					}
					break;
			} 
			#endregion
		}



		// Executes a user-defined function, yielding a step delay after each statement
		// Update ExecFunction method to push/pop global declarations:
		private IEnumerator ExecFunction(FunctionDefStmt fdef, List<object> args, Action<object> setValue)
		{
			if (args.Count != fdef.Parameters.Count)
				throw new Exception($"Function '{fdef.Name}' expects {fdef.Parameters.Count} arguments, got {args.Count} at line {fdef.Line}");

			// Push new local scope and global declarations set
			var localVars = new Dictionary<string, object>();
			var globalDecls = new HashSet<string>();
			for (int i = 0; i < args.Count; i++)
				localVars[fdef.Parameters[i]] = args[i];
			LocalsStack.Push(localVars);
			GlobalDeclsStack.Push(globalDecls);  // Add this line

			try
			{
				// Drive each statement in the function body
				foreach (var stmt in fdef.Body)
				{
					var stmtEnum = ExecStmt(stmt);
					while (true)
					{
						object cur;
						try
						{
							if (!stmtEnum.MoveNext()) break;
							cur = stmtEnum.Current;
						}
						catch (ReturnException retEx)
						{
							// On 'return', capture value and exit
							setValue(retEx.Value);
							yield break;
						}

						// Propagate any nested WaitForSecondsRealtime from sleep()
						yield return cur;
					}

					// **THIS** ensures a frame (stepDelay) between consecutive statements
					yield return null;
				}

				// No return hit → produce null
				setValue(null);
			}
			finally
			{
				LocalsStack.Pop();
				GlobalDeclsStack.Pop();  // Add this line
			}
		}
		
		// Helper that simply iterates each statement in the function body
		private IEnumerable ExecuteFunctionBody(List<Stmt> body)
		{
			foreach (var stmt in body)
			{
				var stmtEnum = ExecStmt(stmt);
				while (stmtEnum.MoveNext())
					yield return stmtEnum.Current;
			}
		}

		// Helper: get variable from local or global scope
		private bool GetVariable(string name, out object value)
		{
			if (LocalsStack.Count > 0)
			{
				var localVars = LocalsStack.Peek();
				if (localVars.ContainsKey(name))
				{
					value = localVars[name];
					return true;
				}
			}
			if (Globals.ContainsKey(name))
			{
				value = Globals[name];
				return true;
			}
			value = null;
			return false;
		}

		// Update SetVariable method:
		private void SetVariable(string name, object value)
		{
			if (LocalsStack.Count > 0)
			{
				// Check if variable is declared global in current function
				if (GlobalDeclsStack.Count > 0 && GlobalDeclsStack.Peek().Contains(name))
				{
					Globals[name] = value;
				}
				else
				{
					var localVars = LocalsStack.Peek();
					// assign in local scope
					localVars[name] = value;
				}
			}
			else
			{
				Globals[name] = value;
			}
		}

		// Helper: truthiness (None/null false, false bool, 0 false, empty string/list false)
		private bool IsTrue(object obj)
		{
			if (obj == null) return false;
			if (obj is bool b) return b;
			if (obj is double d) return d != 0;
			if (obj is string s) return s.Length > 0;
			if (obj is List<object> list) return list.Count > 0;
			return true;
		}

		// Helper: compare values (numbers or strings)
		private int CompareValues(object a, object b)
		{
			if (a is double da && b is double db)
			{
				return da.CompareTo(db);
			}
			if (a is string sa && b is string sb)
			{
				return string.Compare(sa, sb);
			}
			throw new Exception($"Cannot compare values of types {a?.GetType()} and {b?.GetType()}");
		}
	}
}
