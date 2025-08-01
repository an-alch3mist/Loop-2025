using System;
using System.Collections;
using UnityEngine;

namespace GptDeepResearch
{
	/*
		Implement IGameController interface in your game's player controller
		Register it with GameBuiltins.SetGameController(yourController)
	*/


	// Interface for game actions that can be called from Python
	public interface IGameController
	{
		IEnumerator MovePlayer(string direction);
		IEnumerator CollectItem();
		IEnumerator PlantSeed();
		bool CanMoveInDirection(string direction);
		int GetInventoryCount(string itemType);
	}

	// Add this class to handle built-in game functions
	public static class GameBuiltins
	{
		private static IGameController gameController;

		public static void SetGameController(IGameController controller)
		{
			gameController = controller;
		}

		public static IEnumerator ExecuteBuiltinFunction(string functionName, object[] args, Action<object> setValue)
		{
			switch (functionName.ToLower())
			{
				case "move":
					if (args.Length != 1)
						throw new Exception($"move() takes exactly 1 argument ({args.Length} given)");

					string direction = args[0]?.ToString().ToLower();
					if (gameController != null)
					{
						var moveCoroutine = gameController.MovePlayer(direction);
						while (moveCoroutine.MoveNext())
							yield return moveCoroutine.Current;
					}
					else
						throw new Exception($"move() function is not defined in gameController");
					setValue(null);
					break;

				case "collect":
					if (gameController != null)
					{
						var collectCoroutine = gameController.CollectItem();
						while (collectCoroutine.MoveNext())
							yield return collectCoroutine.Current;
					}
					else
						throw new Exception($"collect() function is not defined in gameController");
					setValue(null);
					break;

				case "plant":
					if (gameController != null)
					{
						var plantCoroutine = gameController.PlantSeed();
						while (plantCoroutine.MoveNext())
							yield return plantCoroutine.Current;
					}
					else
						throw new Exception($"plant() function is not defined in gameController");
					setValue(null);
					break;

				case "can_move":
					if (args.Length != 1)
						throw new Exception($"can_move() takes exactly 1 argument ({args.Length} given)");

					string checkDirection = args[0]?.ToString().ToLower();

					if (gameController != null)
					{
						bool canMove = gameController?.CanMoveInDirection(checkDirection) ?? false;
						setValue(canMove);
					}
					else
					{
						setValue(null);
						throw new Exception($"can_move() function is not defined in gameController");
					}
					break;

				case "inventory_count":
					if (args.Length != 1)
						throw new Exception($"inventory_count() takes exactly 1 argument ({args.Length} given)");

					string itemType = args[0]?.ToString();
					if (gameController != null)
					{

						int count = gameController?.GetInventoryCount(itemType) ?? 0;
						setValue(count);
					}
					else
					{
						setValue(null);
						throw new Exception($"inventory_count() function is not defined in gameController");
					}
					break;

				default:
					throw new Exception($"Unknown built-in function '{functionName}'");
			}
		}

		public static bool IsBuiltinFunction(string functionName)
		{
			switch (functionName.ToLower())
			{
				case "move":
				case "collect":
				case "plant":
				case "can_move":
				case "inventory_count":
					return true;
				default:
					return false;
			}
		}
	}
}