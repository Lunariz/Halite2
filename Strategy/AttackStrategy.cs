using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	class AttackStrategy : Strategy
	{
		private static int s_dockedAllyDistanceRatio = 2;

		private static double s_nearbyEnemyDistance = 50d;
		private static double s_abandonPlanetRatio = 0.3d;
		private static int s_minAbandonPlanetEnemySize = 10;
		private static float s_preferredAttackRatio = 1.1f;
		private static float s_preferredWaitDistance = 10f;
		private static int s_minWaitSize = 2;

		public override Command Execute(Ship ship, GameMap gameMap)
		{
			if (DesertStrategy.CheckDesert(gameMap))
			{
				return StrategyManager.CreateStrategy(StrategyType.Desert).Execute(ship, gameMap);
			}

			if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
			{
				return new NoCommand(ship);
			}

			List<Planet> enemyPlanets = gameMap.NearbyPlanetsByDistance(ship).Where(p => p.IsOwned() && p.GetOwner() != gameMap.GetMyPlayerId()).ToList();

			Dictionary<Ship, Planet> closestEnemyPlanet = new Dictionary<Ship, Planet>();
			foreach (Ship otherShip in gameMap.GetAllShips())
			{
				Planet closestPlanet = null;
				double closestPlanetDistance = double.MaxValue;
				foreach (Planet alliedPlanet in enemyPlanets)
				{
					double distance = alliedPlanet.GetDistanceTo(otherShip) - alliedPlanet.GetRadius();
					if (distance < closestPlanetDistance)
					{
						closestPlanetDistance = distance;
						closestPlanet = alliedPlanet;
					}
				}

				closestEnemyPlanet[otherShip] = closestPlanet;
			}

			//Attempt to attack nearby owned planets first
			foreach (Planet ownedPlanet in enemyPlanets)
			{
				List<Ship> nearbyEnemyShips = gameMap.GetAllShips(false).Where(s => s.GetDockingStatus() == Ship.DockingStatus.Undocked && closestEnemyPlanet[s] == ownedPlanet && s.GetDistanceTo(ownedPlanet) <= s_nearbyEnemyDistance).ToList();
				List<Ship> nearbyAlliedShips = gameMap.GetAllShips(true).Where(s => s.GetDockingStatus() == Ship.DockingStatus.Undocked && closestEnemyPlanet[s] == ownedPlanet && s.GetDistanceTo(ownedPlanet) <= s_nearbyEnemyDistance).ToList();
				//List<Ship> dockedEnemyShips = gameMap.GetAllShips(false).Where(s => s.GetDockingStatus() != Ship.DockingStatus.Undocked && s.GetDockedPlanet() == ownedPlanet.GetId()).ToList();

				if (nearbyEnemyShips.Count >= s_minAbandonPlanetEnemySize && nearbyAlliedShips.Count / (double) nearbyEnemyShips.Count < s_abandonPlanetRatio)
				{
					continue;
				}

				int preferredGroupSize = (int) (nearbyEnemyShips.Count * s_preferredAttackRatio + 1);

				if (TargetUtility.CreateOrJoinGroup(ship, ownedPlanet, preferredGroupSize))
				{
					if (nearbyAlliedShips.Count < preferredGroupSize)
					{
						if (nearbyEnemyShips.Count == 0 || nearbyAlliedShips.Count < s_minWaitSize)
						{
							return new KiteStrategy().Calculate(ship, gameMap, false);
						}

						return new KiteStrategy().Calculate(ship, gameMap, true);
					}
					else if (nearbyAlliedShips.Contains(ship))
					{
						//Our attacking group is large enough, attack the docked ships
						//Todo: Perform high density attack instead
						return StrategyManager.CreateStrategy(StrategyType.AttackDockedShips).Execute(ship, gameMap);
					}
				}
			}

			//Kite unless we've obviously won
			if (gameMap.GetMyPlayer.GetShips().Count / (double) gameMap.GetAllShips().Count <= 0.8f)
			{
				return new KiteStrategy().Calculate(ship, gameMap, false);
			}

			return StrategyManager.CreateStrategy(StrategyType.AttackUndockedShips).Execute(ship, gameMap);
		}
	}
}