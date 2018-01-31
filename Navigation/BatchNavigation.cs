using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class BatchNavigation
	{
		public static bool s_randomizeMoveCommands = true;
		public static bool s_postProcessCollisions = true;
		public static int s_batchMoveIterations = 3;
		public static int s_maxMoveCommands = 90;
		public static int s_postProcessIterations = 20;

		private static Dictionary<int, Position> s_previousPositions = new Dictionary<int, Position>();

		//GenerateMoves performs three steps:
		//First, it creates a 'NavigationWorld' that describes where all entities will be moving. We use naïve extrapolation to predict the movement of enemy ships
		//Second, we iteratively modify the paths of our moving ships so that they efficiently path towards their target without colliding
		//Third, we iteratively cut the paths short of ships that could not find a path without colliding
		public static List<Move> GenerateMoves(GameMap gameMap, List<Command> commands)
		{
			List<Move> moves = new List<Move>();

			ConcurrentDictionary<Entity, NavigationPath> paths = new ConcurrentDictionary<Entity, NavigationPath>();
			Dictionary<int, Position> newPositions = new Dictionary<int, Position>();

			foreach (Planet planet in gameMap.GetAllPlanets())
			{
				paths[planet] = NavigationPath.CreateStatic(planet);
			}

			//We extrapolate paths if a previous position is known, otherwise we assume the ship is stationary
			foreach (Ship ship in gameMap.GetAllShips())
			{
				if (s_previousPositions.ContainsKey(ship.GetId()))
				{
					Position previousPosition = s_previousPositions[ship.GetId()];
					paths[ship] = NavigationPath.CreateExtrapolated(ship, previousPosition);
				}
				else
				{
					paths[ship] = NavigationPath.CreateStatic(ship);
				}

				newPositions[ship.GetId()] = ship;
			}

			s_previousPositions = newPositions;

			NavigationWorld world = new NavigationWorld(gameMap, paths);

			List<MoveCommand> moveCommands = new List<MoveCommand>();
			ConcurrentDictionary<Entity, NavigationPath> movePaths = new ConcurrentDictionary<Entity, NavigationPath>();

			Profiler.Start("Path iterations");

			if (s_randomizeMoveCommands)
			{
				commands = Util.RandomizeList(commands);
			}

			//We add all the basic moves to the list and create the first iteration of paths for our moving ships
			foreach (Command command in commands)
			{
				switch (command.Type)
				{
					case Command.CommandType.MoveCommand:
						MoveCommand moveCommand = command as MoveCommand;

						//We only allow the first 90 ships to find a detailed path, all other ships are forced to try in a single iteration (to save time)
						if (moveCommands.Count < s_maxMoveCommands)
						{
							paths[moveCommand.Ship] = NavigationPath.CreateModified(moveCommand.Ship, moveCommand.Ship, moveCommand.Target,
								world);
							moveCommands.Add(moveCommand);
						}
						else
						{
							paths[moveCommand.Ship] = NavigationPath.CreateModified(moveCommand.Ship, moveCommand.Ship, moveCommand.Target, world, 0, 0, 1);
						}
						break;

					//We know these ships won't move anymore, so we replace the extrapolated path with a static path
					case Command.CommandType.NoCommand:
						paths[command.Ship] = NavigationPath.CreateStatic(command.Ship);
						break;

					case Command.CommandType.DockCommand:
						DockCommand dockCommand = command as DockCommand;
						moves.Add(new DockMove(dockCommand.Ship, dockCommand.TargetPlanet));

						paths[command.Ship] = NavigationPath.CreateStatic(command.Ship);
						break;

					case Command.CommandType.UndockCommand:
						UndockCommand undockCommand = command as UndockCommand;
						moves.Add(new UndockMove(undockCommand.Ship));

						paths[command.Ship] = NavigationPath.CreateStatic(command.Ship);
						break;
				}
			}

			world.Update(paths);

			for (int i = 0; i < s_batchMoveIterations; i++)
			{
				DebugLog.AddLog("ITERATION START: " + i);

				NavigationPath.VolatilePaths.Clear();

				List<MoveCommand> randomizedMoveCommands = s_randomizeMoveCommands ? Util.RandomizeList(moveCommands) : moveCommands;
				foreach (MoveCommand moveCommand in randomizedMoveCommands)
				{
					NavigationPath path = NavigationPath.CreateModified(moveCommand.Ship, moveCommand.Ship, moveCommand.Target, world);
					movePaths[moveCommand.Ship] = path;

					world.Update(moveCommand.Ship, path);
				}
			}
			Profiler.Stop("Path iterations");

			if (s_postProcessCollisions)
			{
				Profiler.Start("Path postprocess");
				HashSet<Entity> newVolatilePaths = new HashSet<Entity>();
				Dictionary<Entity, CollisionInfo> volatileCollisions = new Dictionary<Entity, CollisionInfo>();
				//This is guaranteed to terminate, because the amount of moving ships is always descreasing
				int max = s_postProcessIterations;
				while (NavigationPath.VolatilePaths.Count > 0 && max > 0)
				{
					DebugLog.AddLog("volatile remaining: " + NavigationPath.VolatilePaths.Count + ", iters remaining: " + max);
					max--;

					foreach (Entity volatileEntity in NavigationPath.VolatilePaths)
					{
						NavigationPath volatilePath = movePaths[volatileEntity];

						foreach (NavigationPath path in movePaths.Values)
						{
							if (path == volatilePath)
							{
								continue;
							}

							CollisionInfo collision = NavigationCollisionUtility.FindCollisionBetweenPaths(path, volatilePath, path.GetIgnoreTarget(), Double.MaxValue);

							if (collision.CollisionHappened)
							{
								volatileCollisions[path.Entity] = collision;
								newVolatilePaths.Add(path.Entity);
							}
						}
					}

					foreach (Entity newVolatileEntity in newVolatilePaths)
					{
						NavigationPath path = movePaths[newVolatileEntity];
						path.ModifyCollisionStop(volatileCollisions[newVolatileEntity]);
					}

					NavigationPath.VolatilePaths = new HashSet<Entity>(newVolatilePaths);
					newVolatilePaths.Clear();
					volatileCollisions.Clear();
				}
				Profiler.Stop("Path postprocess");
			}

			//Add all the move commands as the result of our iterated paths
			foreach (KeyValuePair<Entity, NavigationPath> movePath in movePaths)
			{
				ThrustMove thrustMove = movePath.Value.GetThrustMove();
				moves.Add(thrustMove);
			}

			return moves;
		}
	}
}