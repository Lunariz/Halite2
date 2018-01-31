using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Desert all planets and run towards the nearest corner on the map for survival
	class DesertStrategy : Strategy
	{
		public static bool s_shouldDesert = false;
		private static bool s_shouldNeverDesert = false;
		private static int s_lastTurn = 0;
		private static int s_desertTurn;

		private static int s_cornerDistance = 1;
		private static List<Position> s_corners;
		private static Position s_currentCorner;
		private static int s_currentCornerIndex = 0;
		private static double s_closeToCornerDistance = 8;

		private static Dictionary<Position, int> s_stayAtCornerShips = new Dictionary<Position, int>();
		private static Dictionary<int, Position> s_stayAtCornerShipsReversed = new Dictionary<int, Position>();

		private static Dictionary<Player, float> s_shipPercents = new Dictionary<Player, float>();
		private static int s_shipPercentTurn = 0;
		private static double s_preferredKillDesertingRatio = 1.2d;
		private static Player s_killDesertingPlayer = null;
		private static int s_killDesertingPlayerId = -1;
		private static bool s_reachedCornerOnce = false;

		public static bool CheckDesert(GameMap gameMap)
		{
			//We terminate early when deserting in training mode to speedup training, even in duels
			if (GameMap.IsTraining && s_shouldDesert && GameMap.Turn > s_desertTurn + 5)
			{
				throw new Exception("Terminated due to desertion in training mode");
			}

			if (s_shouldNeverDesert)
			{
				return false;
			}
			//Do not desert / stop deserting if there are 2 players remaining
			if (gameMap.GetAllPlayers().Count(p => p.GetShips().Count > 0) <= 2 && !GameMap.IsTraining)
			{
				s_shouldDesert = false;
				s_shouldNeverDesert = true;
				return false;
			}
			if (s_shouldDesert)
			{
				return true;
			}
			if (s_lastTurn == GameMap.Turn)
			{
				return false;
			}
			s_lastTurn = GameMap.Turn;

			if (RecalculateShouldDesert(gameMap))
			{
				s_shouldDesert = true;
				s_desertTurn = GameMap.Turn;
			}

			return s_shouldDesert;
		}

		private static bool RecalculateShouldDesert(GameMap gameMap)
		{
			double myShipPercent = gameMap.GetMyPlayer.GetShips().Count / (double) gameMap.GetAllShips().Count;
			foreach (Player player in gameMap.GetAllPlayers())
			{
				double enemyShipPercent = player.GetShips().Count / (double) gameMap.GetAllShips().Count;
				if (player != gameMap.GetMyPlayer && enemyShipPercent > 0.5f && myShipPercent < 0.25f ||
				    player == gameMap.GetMyPlayer && myShipPercent < 0.1f)
				{
					return true;
				}
			}
			return false;
		}

		private static void RecalculateShipPercents(GameMap gameMap)
		{
			foreach (Player player in gameMap.GetAllPlayers())
			{
				float shipPercent = player.GetShips().Count / (float) gameMap.GetAllShips().Count;
				s_shipPercents[player] = shipPercent;
			}
		}

		private Position GetClosestCorner(List<Ship> ships, GameMap gameMap)
		{
			var corners = GetCorners(gameMap);

			Dictionary<Position, double> distanceSums = new Dictionary<Position, double>()
			{
				{corners[0], 0},
				{corners[1], 0},
				{corners[2], 0},
				{corners[3], 0},
			};

			foreach (Ship distanceShip in ships)
			{
				var keys = distanceSums.Keys.ToList();
				foreach (var key in keys)
				{
					distanceSums[key] += key.GetDistanceTo(distanceShip);
				}
			}

			Position bestCorner = null;
			double bestDistanceSum = double.MaxValue;

			foreach (var kvp in distanceSums)
			{
				if (kvp.Value < bestDistanceSum)
				{
					bestDistanceSum = kvp.Value;
					bestCorner = kvp.Key;
				}
			}

			return bestCorner;
		}

		public override Command Execute(Ship ship, GameMap gameMap)
		{
			if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
			{
				return new UndockCommand(ship);
			}

			if (s_currentCorner == null)
			{
				s_currentCorner = GetClosestCorner(gameMap.GetAllShips(true), gameMap);
				s_currentCornerIndex = s_corners.IndexOf(s_currentCorner);
			}

			int closeToCorner = 0;
			Ship closestToCorner = null;
			double closestToCornerDistance = double.MaxValue;
			foreach (Ship allyShip in gameMap.GetAllShips(true))
			{
				double distance = allyShip.GetDistanceTo(s_currentCorner);
				if (distance < s_closeToCornerDistance)
				{
					closeToCorner++;
				}
				if (distance < closestToCornerDistance && !s_stayAtCornerShips.ContainsValue(allyShip.GetId()))
				{
					closestToCornerDistance = distance;
					closestToCorner = allyShip;
				}
			}

			if (closestToCorner != null && closeToCorner > 1)
			{
				s_stayAtCornerShips[s_currentCorner] = closestToCorner.GetId();
				s_stayAtCornerShipsReversed[closestToCorner.GetId()] = s_currentCorner;
			}

			//If you are the only one remaining and enemies are coming, don't stay
			if (gameMap.GetAllShips(true).Count == 1)
			{
				bool enemiesAreComing = false;
				foreach (Ship enemyShip in gameMap.GetAllShips(false))
				{
					if (enemyShip.GetDistanceTo(ship) <= 20)
					{
						if (s_stayAtCornerShips.ContainsValue(ship.GetId()) && s_stayAtCornerShipsReversed.ContainsKey(ship.GetId()))
						{
							s_stayAtCornerShips.Remove(s_stayAtCornerShipsReversed[ship.GetId()]);
							s_stayAtCornerShipsReversed.Remove(ship.GetId());
						}

						enemiesAreComing = true;
						break;
					}
				}

				if (!enemiesAreComing)
				{
					s_currentCorner = GetClosestCorner(gameMap.GetAllShips(true), gameMap);
					s_stayAtCornerShips[s_currentCorner] = ship.GetId();
					s_stayAtCornerShipsReversed[ship.GetId()] = s_currentCorner;
				}
			}

			//If you are told to stay, stay!
			if (s_stayAtCornerShips.ContainsValue(ship.GetId()))
			{
				return new MoveCommand(ship, new PathingTarget(s_stayAtCornerShipsReversed[ship.GetId()], 0));
			}

			if (GameMap.Turn != s_shipPercentTurn)
			{
				s_shipPercentTurn = GameMap.Turn;
				RecalculateShipPercents(gameMap);

				if (s_killDesertingPlayerId != -1 && s_shipPercents[gameMap.GetAllPlayers()[s_killDesertingPlayerId]] == 0)
				{
					s_reachedCornerOnce = false;
				}

				double myShipPercent = s_shipPercents[gameMap.GetMyPlayer];
				s_killDesertingPlayer = null;
				s_killDesertingPlayerId = -1;
				double lowestShipPercent = double.MaxValue;
				foreach (Player player in gameMap.GetAllPlayers())
				{
					if (player == gameMap.GetMyPlayer)
					{
						continue;
					}

					double enemyShipPercent = s_shipPercents[player];
					if (enemyShipPercent != 0 && myShipPercent > enemyShipPercent * s_preferredKillDesertingRatio + 0.0001d && enemyShipPercent < lowestShipPercent)
					{
						s_killDesertingPlayer = player;
						s_killDesertingPlayerId = player.GetId();
						lowestShipPercent = enemyShipPercent;
					}
				}
			}

			bool reachedCurrentCorner = closeToCorner / (double) (gameMap.GetAllShips(true).Count - s_stayAtCornerShips.Count) > 0.9f;
			s_reachedCornerOnce = s_reachedCornerOnce || reachedCurrentCorner;

			if (s_killDesertingPlayer != null && s_reachedCornerOnce)
			{
				Position enemyCorner = GetClosestCorner(s_killDesertingPlayer.GetShips().Values.ToList(), gameMap);

				//We are ready to attack
				if (enemyCorner == s_currentCorner && reachedCurrentCorner)
				{
					//Todo: attack the deserting ships specifically
					return StrategyManager.CreateStrategy(StrategyType.AttackUndockedShips).Execute(ship, gameMap);
				}
				//We needed to go to this corner first, now we'll go to the next one
				if (reachedCurrentCorner)
				{
					if (s_corners[(s_currentCornerIndex + 1) % 4] == enemyCorner)
					{
						s_currentCornerIndex = (s_currentCornerIndex + 1) % s_corners.Count;
						s_currentCorner = s_corners[s_currentCornerIndex];
					}
					else
					{
						s_currentCornerIndex = (s_currentCornerIndex - 1 + s_corners.Count) % s_corners.Count;
						s_currentCorner = s_corners[s_currentCornerIndex];
					}
				}
			}
			else if (reachedCurrentCorner)
			{
				s_currentCornerIndex = (s_currentCornerIndex - 1 + s_corners.Count) % s_corners.Count;
				s_currentCorner = s_corners[s_currentCornerIndex];
			}

			return new MoveCommand(ship, new PathingTarget(s_currentCorner, 0));
		}

		private List<Position> GetCorners(GameMap gameMap)
		{
			if (s_corners != null)
			{
				return s_corners;
			}

			Position topLeftCorner = new Position(s_cornerDistance, s_cornerDistance);
			Position topRightCorner = new Position(gameMap.GetWidth() - s_cornerDistance, s_cornerDistance);
			Position bottomLeftCorner = new Position(s_cornerDistance, gameMap.GetHeight() - s_cornerDistance);
			Position bottomRightCorner = new Position(gameMap.GetWidth() - s_cornerDistance, gameMap.GetHeight() - s_cornerDistance);

			s_corners = new List<Position>() {topLeftCorner, topRightCorner, bottomRightCorner, bottomLeftCorner};

			return s_corners;
		}
	}
}