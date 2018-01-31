using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	class DefendStrategy : Strategy
	{
		private static int s_dockedAllyDistanceRatio = 2;
		private static double s_maxEnemyClusterDistance = 50d;
		private static double s_maxAlliedClusterDistance = 25d;
		private static int s_maxEnemyClusterMembersToDefend = 5;

		private static double s_nearbyEnemyDistance = 50d;
		private static double s_abandonPlanetRatio = 0.5d;
		private static int s_minAbandonPlanetEnemySize = 3;
		private static float s_preferredDefendRatio = 1.1f;
	    private static float s_defendDistance = 5f;
	    private static float s_defendLeeway = 3f;

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

			//If we've obviously won, colonize all planets to win quickly
			if (gameMap.GetMyPlayer.GetShips().Count / (double) gameMap.GetAllShips().Count >= 0.9f)
			{
				return StrategyManager.CreateStrategy(StrategyType.DockUnowned).Execute(ship, gameMap);
			}

			List<Planet> alliedPlanets = gameMap.NearbyPlanetsByDistance(ship).Where(p => p.IsOwned() && p.GetOwner() == gameMap.GetMyPlayerId()).ToList();

			Dictionary<Ship, Planet> closestAllyPlanet = new Dictionary<Ship, Planet>();
			foreach (Ship otherShip in gameMap.GetAllShips())
			{
				Planet closestPlanet = null;
				double closestPlanetDistance = double.MaxValue;
				foreach (Planet alliedPlanet in alliedPlanets)
				{
					double distance = alliedPlanet.GetDistanceTo(otherShip) - alliedPlanet.GetRadius();
					if (distance < closestPlanetDistance)
					{
						closestPlanetDistance = distance;
						closestPlanet = alliedPlanet;
					}
				}

				closestAllyPlanet[otherShip] = closestPlanet;
			}

			//Attempt to defend nearby owned planets first
			foreach (Planet ownedPlanet in alliedPlanets)
			{
				List<Ship> nearbyEnemyShips = gameMap.GetAllShips(false).Where(s => s.GetDockingStatus() == Ship.DockingStatus.Undocked && closestAllyPlanet[s] == ownedPlanet && s.GetDistanceTo(ownedPlanet) < s_nearbyEnemyDistance).ToList();
				List<Ship> nearbyAlliedShips = gameMap.GetAllShips(true).Where(s => s.GetDockingStatus() == Ship.DockingStatus.Undocked && closestAllyPlanet[s] == ownedPlanet && s.GetDistanceTo(ownedPlanet) < s_nearbyEnemyDistance).ToList();
				List<Ship> dockedAlliedShips = gameMap.GetAllShips(true).Where(s => s.GetDockingStatus() != Ship.DockingStatus.Undocked && s.GetDockedPlanet() == ownedPlanet.GetId()).ToList();

				if (nearbyEnemyShips.Count == 0 ||
				    nearbyEnemyShips.Count >= s_minAbandonPlanetEnemySize && nearbyAlliedShips.Count / (double) nearbyEnemyShips.Count < s_abandonPlanetRatio)
				{
					//This planet is not worth defending
					continue;
				}

				int preferredGroupSize = (int) (nearbyEnemyShips.Count * s_preferredDefendRatio + 1);

				if (TargetUtility.CreateOrJoinGroup(ship, ownedPlanet, preferredGroupSize))
				{
					if (nearbyAlliedShips.Count < preferredGroupSize)
					{
						//Defend
						Ship closestDockedAlly = null;
						double closestDockedAllyDistance = double.MaxValue;
						foreach (Ship dockedAlly in dockedAlliedShips)
						{
							double distance = dockedAlly.GetDistanceTo(ship);
							if (distance < closestDockedAllyDistance)
							{
								closestDockedAllyDistance = distance;
								closestDockedAlly = dockedAlly;
							}
						}

						Ship closestEnemyShip = null;
						double closestEnemyDistance = double.MaxValue;
						foreach (Ship enemyShip in nearbyEnemyShips)
						{
							double distance = enemyShip.GetDistanceTo(closestDockedAlly);
							if (distance < closestEnemyDistance)
							{
								closestEnemyDistance = distance;
								closestEnemyShip = enemyShip;
							}
						}

						//Todo: keep the low health defenders in the back

						Position targetPosition = closestEnemyShip.GetClosestPoint(closestDockedAlly, s_defendDistance);
						return new MoveCommand(ship, new PathingTarget(targetPosition, s_defendLeeway));
					}
					else if (nearbyAlliedShips.Contains(ship))
					{
						//Our defending group is large enough, attack the nearby ships
						//Todo: Perform high density attack instead
						return StrategyManager.CreateStrategy(StrategyType.AttackUndockedShips).Execute(ship, gameMap);
					}
				}
			}

			return StrategyManager.CreateStrategy(StrategyType.Dock).Execute(ship, gameMap);
		}
	}
}
