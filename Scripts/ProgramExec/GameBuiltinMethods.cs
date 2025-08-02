// LABELED DIFF FOR GameBuiltinMethods.cs
// Replace the entire file with this updated version

using System;
using System.Collections;
using UnityEngine;

namespace GptDeepResearch
{
	/*
        New system: Each scene provides a GameControllerBase implementation
        that registers scene-specific commands with this system
    */

	// REMOVE: Old IGameController interface - replaced with GameControllerBase

	// Add this class to handle built-in game functions
	public static class GameBuiltinMethods
	{
		// MODIFY: Replace gameController with scene-specific controller
		private static GameControllerBase sceneController;

		// ADD: Registration method for scene controllers
		public static void RegisterGameController(GameControllerBase controller)
		{
			sceneController = controller;
		}

		// ADD: Unregistration method
		public static void UnregisterGameController()
		{
			sceneController = null;
		}

		// ADD: Get current scene controller
		public static GameControllerBase GetCurrentController()
		{
			return sceneController;
		}

		public static IEnumerator ExecuteBuiltinFunction(string functionName, object[] args, Action<object> setValue)
		{
			// MODIFY: Update function handling for new system
			switch (functionName.ToLower())
			{
				// These are still handled by the old system for backward compatibility
				case "move":
				case "collect":
				case "plant":
				case "can_move":
				case "inventory_count":
					// Check if we have the old-style controller
					if (sceneController != null)
					{
						// NEW: Route to scene controller
						yield return HandleSceneCommand(functionName, args, setValue);
					}
					else
					{
						throw new Exception($"No scene controller registered for function '{functionName}'");
					}
					break;

				default:
					// CHECK: If it's a scene-specific command
					if (sceneController != null && sceneController.HasCommand(functionName))
					{
						yield return HandleSceneCommand(functionName, args, setValue);
					}
					else
					{
						throw new Exception($"Unknown built-in function '{functionName}'");
					}
					break;
			}
		}

		// FIXED: Updated method to handle scene commands with new predicate signature
		private static IEnumerator HandleSceneCommand(string functionName, object[] args, Action<object> setValue)
		{
			if (sceneController == null)
			{
				throw new Exception($"No scene controller registered for function '{functionName}'");
			}

			// Determine if this is an action or predicate command
			if (sceneController.actionCommands.ContainsKey(functionName))
			{
				// Action command (no return value)
				yield return sceneController.ExecuteActionCommand(functionName, args);
				setValue(null);
			}
			else if (sceneController.predicateCommands.ContainsKey(functionName))
			{
				// Predicate command (returns bool)
				bool result = false;
				bool resultReceived = false;

				// Execute the predicate command with callback
				yield return sceneController.ExecutePredicateCommand(functionName, args, (bool predicateResult) =>
				{
					result = predicateResult;
					resultReceived = true;
				});

				// Wait for result if needed
				while (!resultReceived)
				{
					yield return null;
				}

				setValue(result);
			}
			else
			{
				throw new Exception($"Function '{functionName}' not found in scene controller");
			}
		}

		public static bool IsBuiltinFunction(string functionName)
		{
			// MODIFY: Check both built-in and scene-specific functions
			switch (functionName.ToLower())
			{
				// Legacy built-ins
				case "move":
				case "collect":
				case "plant":
				case "can_move":
				case "inventory_count":
					return true;
				default:
					// Check scene controller
					return sceneController != null && sceneController.HasCommand(functionName);
			}
		}

		// ADD: Get all available commands for syntax highlighting
		public static System.Collections.Generic.List<string> GetAllAvailableCommands()
		{
			var commands = new System.Collections.Generic.List<string>();

			// Add legacy commands
			commands.AddRange(new[] { "move", "collect", "plant", "can_move", "inventory_count" });

			// Add scene-specific commands
			if (sceneController != null)
			{
				commands.AddRange(sceneController.GetAllCommandNames());
			}

			return commands;
		}

		// ADD: Scene reset functionality
		public static IEnumerator ResetScene()
		{
			if (sceneController != null)
			{
				yield return sceneController.SceneReset();
			}
		}
	}
}