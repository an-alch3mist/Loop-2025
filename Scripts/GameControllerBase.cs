using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GptDeepResearch
{
	/// <summary>
	/// Base class for scene-specific game controllers
	/// Inherit from this to implement your scene's game commands
	/// </summary>
	public abstract class GameControllerBase : MonoBehaviour
	{
		// Action commands (no return value) - e.g. move(), collect()
		public Dictionary<string, Func<object[], IEnumerator>> actionCommands =
			new Dictionary<string, Func<object[], IEnumerator>>();

		// Predicate commands (return bool) - e.g. is_block(), can_move()
		// FIXED: Changed from IEnumerator<bool> to IEnumerator with bool result parameter
		public Dictionary<string, Func<object[], IEnumerator>> predicateCommands =
			new Dictionary<string, Func<object[], IEnumerator>>();

		// Store results of predicate commands
		private Dictionary<string, bool> predicateResults = new Dictionary<string, bool>();

		protected virtual void Awake()
		{
			// Register commands during Awake
			RegisterCommands();
		}

		protected virtual void Start()
		{
			// Register with GameBuiltinMethods system
			GameBuiltinMethods.RegisterGameController(this);
		}

		protected virtual void OnDestroy()
		{
			// Unregister when destroyed
			GameBuiltinMethods.UnregisterGameController();
		}

		/// <summary>
		/// Override this method to register your scene-specific commands
		/// </summary>
		protected abstract void RegisterCommands();

		/// <summary>
		/// Override this method to reset your scene state
		/// Called before each script run and on reset button press
		/// </summary>
		public abstract IEnumerator SceneReset();

		/// <summary>
		/// Get all registered command names for syntax highlighting
		/// </summary>
		public List<string> GetAllCommandNames()
		{
			var commands = new List<string>();
			commands.AddRange(actionCommands.Keys);
			commands.AddRange(predicateCommands.Keys);
			return commands;
		}

		/// <summary>
		/// Execute an action command
		/// </summary>
		public IEnumerator ExecuteActionCommand(string commandName, object[] args)
		{
			if (actionCommands.TryGetValue(commandName, out var command))
			{
				yield return command(args);
			}
			else
			{
				throw new Exception($"Unknown action command: {commandName}");
			}
		}

		/// <summary>
		/// Execute a predicate command and return the result
		/// </summary>
		public IEnumerator ExecutePredicateCommand(string commandName, object[] args, System.Action<bool> onResult)
		{
			if (predicateCommands.TryGetValue(commandName, out var command))
			{
				// Clear any previous result
				predicateResults[commandName] = false;

				// Execute the command
				yield return command(args);

				// Return the result (Unity 2020.3 compatible way using TryGetValue)
				bool result;
				if (!predicateResults.TryGetValue(commandName, out result))
				{
					result = false; // Default value if key not found
				}
				onResult(result);
			}
			else
			{
				throw new Exception($"Unknown predicate command: {commandName}");
			}
		}

		/// <summary>
		/// Check if a command exists
		/// </summary>
		public bool HasCommand(string commandName)
		{
			return actionCommands.ContainsKey(commandName) || predicateCommands.ContainsKey(commandName);
		}

		/// <summary>
		/// Helper to register an action command
		/// </summary>
		protected void RegisterAction(string commandName, Func<object[], IEnumerator> action)
		{
			actionCommands[commandName] = action;
		}

		/// <summary>
		/// Helper to register a predicate command
		/// </summary>
		protected void RegisterPredicate(string commandName, Func<object[], IEnumerator> predicate)
		{
			predicateCommands[commandName] = predicate;
		}

		/// <summary>
		/// Helper method for predicate commands to set their result
		/// </summary>
		protected void SetPredicateResult(string commandName, bool result)
		{
			predicateResults[commandName] = result;
		}
	}

	/// <summary>
	/// Example implementation - copy this to create your scene controller
	/// </summary>
	public class ExampleSceneController : GameControllerBase
	{
		[Header("Player References")]
		public Transform playerTransform;
		public float moveSpeed = 2f;

		private Vector3 initialPlayerPosition;

		protected override void Awake()
		{
			// Store initial positions for reset
			if (playerTransform != null)
				initialPlayerPosition = playerTransform.position;

			base.Awake();
		}

		protected override void RegisterCommands()
		{
			// Register action commands (no return value)
			RegisterAction("move", MoveCommand);
			RegisterAction("collect", CollectCommand);

			// Register predicate commands (return bool)
			RegisterPredicate("is_block", IsBlockCommand);
			RegisterPredicate("can_move", CanMoveCommand);
		}

		public override IEnumerator SceneReset()
		{
			// Reset player position
			if (playerTransform != null)
			{
				playerTransform.position = initialPlayerPosition;
			}

			// Add any other scene reset logic here
			// e.g., reset inventory, clear collected items, etc.

			yield return null; // Yield at least once
		}

		// Example action command implementations
		private IEnumerator MoveCommand(object[] args)
		{
			if (args.Length != 1)
				throw new Exception("move() takes exactly 1 argument");

			string direction = args[0].ToString().ToLower();
			Vector3 moveVector = Vector3.zero;

			switch (direction)
			{
				case "up": moveVector = Vector3.up; break;
				case "down": moveVector = Vector3.down; break;
				case "left": moveVector = Vector3.left; break;
				case "right": moveVector = Vector3.right; break;
				default: throw new Exception($"Invalid direction: {direction}");
			}

			if (playerTransform != null)
			{
				Vector3 startPos = playerTransform.position;
				Vector3 endPos = startPos + moveVector;
				float elapsed = 0f;

				while (elapsed < 1f)
				{
					elapsed += Time.deltaTime * moveSpeed;
					playerTransform.position = Vector3.Lerp(startPos, endPos, elapsed);
					yield return null;
				}

				playerTransform.position = endPos;
			}
		}

		private IEnumerator CollectCommand(object[] args)
		{
			if (args.Length != 1)
				throw new Exception("collect() takes exactly 1 argument");

			string itemName = args[0].ToString();

			// Example: Find and collect item
			GameObject item = GameObject.Find(itemName);
			if (item != null)
			{
				// Animate collection
				Vector3 originalScale = item.transform.localScale;
				float elapsed = 0f;

				while (elapsed < 0.5f)
				{
					elapsed += Time.deltaTime;
					float scale = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
					item.transform.localScale = originalScale * scale;
					yield return null;
				}

				Destroy(item);
			}
		}

		// FIXED: Example predicate command implementations
		private IEnumerator IsBlockCommand(object[] args)
		{
			if (args.Length != 2)
				throw new Exception("is_block() takes exactly 2 arguments");

			float x = Convert.ToSingle(args[0]);
			float y = Convert.ToSingle(args[1]);

			// Example: Check if position is blocked
			// This could involve raycasting, collision detection, etc.
			Vector3 checkPos = new Vector3(x, y, 0);

			// Simulate some async work
			yield return new WaitForSeconds(0.1f);

			// Example logic: check if there's a collider at this position
			Collider2D collider = Physics2D.OverlapPoint(checkPos);
			bool result = collider != null && collider.CompareTag("Block");

			// Set the result using the helper method
			SetPredicateResult("is_block", result);
		}

		private IEnumerator CanMoveCommand(object[] args)
		{
			if (args.Length != 1)
				throw new Exception("can_move() takes exactly 1 argument");

			string direction = args[0].ToString().ToLower();

			if (playerTransform == null)
			{
				SetPredicateResult("can_move", false);
				yield break;
			}

			Vector3 checkDirection = Vector3.zero;
			switch (direction)
			{
				case "up": checkDirection = Vector3.up; break;
				case "down": checkDirection = Vector3.down; break;
				case "left": checkDirection = Vector3.left; break;
				case "right": checkDirection = Vector3.right; break;
				default:
					SetPredicateResult("can_move", false);
					yield break;
			}

			// Check if movement is possible
			Vector3 checkPos = playerTransform.position + checkDirection;

			// Simulate some async work
			yield return new WaitForSeconds(0.05f);

			// Example: check bounds or obstacles
			bool canMove = true; // Your logic here

			SetPredicateResult("can_move", canMove);
		}
	}
}