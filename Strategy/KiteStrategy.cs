using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Distract enemy ships and attempt to attack docked enemy ships through bruteforced micro play
    class KiteStrategy : Strategy
    {
	    public static double s_minDistance = 20;
	    public static double s_nearbyDistance = 20;

	    public override Command Execute(Ship ship, GameMap gameMap)
	    {
			return Calculate(ship, gameMap);
	    }

	    public Command Calculate(Ship ship, GameMap gameMap, bool moveTowardsAllies = false, double preferredMinDistance = 6)
        {
            if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return new UndockCommand(ship);
            }

            var enemyShips = gameMap.ShipsByDistance(ship, false);
			var undockedEnemyShips =  enemyShips.Where(enemyShip => enemyShip.GetDockingStatus() == Ship.DockingStatus.Undocked).ToList(); 

	        if (undockedEnemyShips.Count == 0)
	        {
				return Fallback().Execute(ship, gameMap);
	        }

	        Ship closestEnemyShip = undockedEnemyShips[0];

	        if (ship.GetDistanceTo(closestEnemyShip) > s_minDistance && !DesertStrategy.s_shouldDesert)
	        {
		        //If there are no ships to kite, just attack a docked ship - this brings us deep into their base where we're bound to attract attention
		        return StrategyManager.CreateStrategy(StrategyType.AttackDockedShips).Execute(ship, gameMap);
	        }

			Profiler.Start("Kite");

	        Ship closestDockedEnemy = enemyShips.FirstOrDefault(s => s.GetDockingStatus() != Ship.DockingStatus.Undocked);

			//We are being chased by one or more ships - find the direction to run to that will (in order of priority):
			//1. Won't make us collide into planets
			//2. Will make the ship take the least amount of attacks, if all enemy ships were to move directly to us
			//3. Gets us into attack range of 1 or more docked enemy ships
			//4. Gets as close as possible to ships that are outside our chase radius
			//5. Leaves us the most amount of room to maneuver on the map

			//This list will keep track of all locations we are still considering
			List<LocationInfo> remainingLocations = new List<LocationInfo>();

			List<Planet> possibleCollidingPlanets = gameMap.GetAllPlanets().Where(p => p.GetDistanceTo(ship) - ship.GetRadius() - p.GetRadius() <= Constants.MAX_SPEED).ToList();
            possibleCollidingPlanets.Sort((a,b)=>ship.GetDistanceTo(a).CompareTo(ship.GetDistanceTo(b)));

			remainingLocations.Add(new LocationInfo(0, 0, ship, NavigationPath.CreateStatic(ship)));



			//Step 1: create all locationInfos that won't make us collide into planets
	        for (int angle = 0; angle < 360; angle++)
	        {
	            FillRemainingLocations(ship, remainingLocations, possibleCollidingPlanets, angle);
            }
			
            //Step 2: take the least amount of attacks
			List<NavigationPath> enemyShipPaths = new List<NavigationPath>();
	        double attackDistance = preferredMinDistance;

			List<Ship> possibleAttackingEnemyShips = undockedEnemyShips.Where(s => s.GetDistanceTo(ship) - ship.GetRadius() - s.GetRadius() - attackDistance - Constants.MAX_SPEED <= Constants.MAX_SPEED).ToList();

	        foreach (Ship enemyShip in possibleAttackingEnemyShips)
	        {
		        //Assume the ship travels with max speed towards the current location of our ship
		        double lerp = Constants.MAX_SPEED / ship.GetDistanceTo(enemyShip);
				Position newPosition = Position.Lerp(enemyShip, ship, lerp);
				enemyShipPaths.Add(NavigationPath.CreateSimple(enemyShip, newPosition));
	        }

	        List<Planet> spawningEnemyPlanets = gameMap.GetAllPlanets().Where(p => p.IsOwned() && p.GetOwner() != gameMap.GetMyPlayerId() &&
	                                                                               p.GetCurrentProduction() + (p.GetDockedShips().Count * Constants.BASE_PRODUCTION)*2 >= Constants.PRODUCTION_PER_SHIP).ToList();

	        remainingLocations = ReduceAttacks(gameMap, remainingLocations, enemyShipPaths, spawningEnemyPlanets, attackDistance);

			//Step 3: Get in attack range of preferably 1 or more enemy docked ships
	        remainingLocations = AttackDocked(ship, remainingLocations, enemyShips);
			
			//Step 4: get close to as many ships as possible
			//Special rule: if we are kiting because of desertion, get as far away as possible
	        //remainingLocations = MinimizeDistance(remainingLocations, enemyShipPaths);
			
	        //Step 5: leave room for more maneuvering in the future - choose the location that maximizes the distance to the walls, closest planet and closest enemy ship
	        Ship closestUndockedAlly = null;
	        double closestUndockedAllyDistance = double.MaxValue;
	        Ship closestDockedAlly = null;
	        double closestDockedAllyDistance = double.MaxValue;
	        foreach (Ship ally in gameMap.GetAllShips(false))
	        {
		        if (ally == ship)
		        {
			        continue;
		        }
		        double distance = ally.GetDistanceTo(ship);
		        if (distance < closestUndockedAllyDistance && ally.GetDockingStatus() == Ship.DockingStatus.Undocked)
		        {
			        closestUndockedAllyDistance = distance;
			        closestUndockedAlly = ally;
		        }
		        if (distance < closestDockedAllyDistance && ally.GetDockingStatus() != Ship.DockingStatus.Undocked)
		        {
			        closestDockedAllyDistance = distance;
			        closestDockedAlly = ally;
		        }
	        }

	        LocationInfo bestLocation = FindWeightedBestLocation(ship, gameMap, remainingLocations, closestUndockedAlly, closestDockedAlly, closestEnemyShip, closestDockedEnemy, moveTowardsAllies);

			Profiler.Stop("Kite");

	        if (bestLocation != null)
	        {
		        return new MoveCommand(ship, new PathingTarget(bestLocation.ResultingPosition, 0));
	        }

            return Fallback().Execute(ship, gameMap);
        }

        private static void FillRemainingLocations(Ship ship, List<LocationInfo> remainingLocations, List<Planet> possibleCollidingPlanets, int angle)
        {
            for (int distance = 1; distance < Constants.MAX_SPEED + 1; distance++)
            {
                double angleRad = (double)angle / 180 * Math.PI;

                double newX = ship.GetXPos() + Math.Cos(angleRad) * distance;
                double newY = ship.GetYPos() + Math.Sin(angleRad) * distance;
                Position resultingPosition = new Position(newX, newY);

                LocationInfo location = new LocationInfo(angle, distance, resultingPosition,
                    NavigationPath.CreateSimple(ship, resultingPosition));

                foreach (Planet planet in possibleCollidingPlanets)
                {
                    double hitboxDistance = ship.GetRadius() + planet.GetRadius();
                    double t = NavigationCollisionUtility.CalculateCollisionTimeBetweenMovements(
                        location.Path.GetMovementAtFrame(0), new NavigationMovement(planet, planet, planet),
                        hitboxDistance);
                    if (t >= 0 && t <= 1)
                    {
                        return;
                    }
                }

				remainingLocations.Add(new LocationInfo(angle, distance, resultingPosition, NavigationPath.CreateSimple(ship, resultingPosition)));
            }
        }

	    private List<LocationInfo> ReduceAttacks(GameMap gameMap, List<LocationInfo> remainingLocations, List<NavigationPath> enemyShipPaths, List<Planet> spawningEnemyPlanets, double attackDistance)
	    {
		    List<LocationInfo> smallestAttackAmountLocations = new List<LocationInfo>();
	        int smallestAttackAmount = int.MaxValue;
			
	        foreach (LocationInfo location in remainingLocations)
	        {
		        int attacks = 0;
		        foreach (NavigationPath enemyShipPath in enemyShipPaths)
		        {
			        double t = NavigationCollisionUtility.CalculateCollisionTimeBetweenMovements(location.Path.GetMovementAtFrame(0), enemyShipPath.GetMovementAtFrame(0), attackDistance);
			        if (t >= 0 && t <= 1)
			        {
				        attacks++;
			        }
			        if (location.ResultingPosition.GetDistanceTo(enemyShipPath.End) <= attackDistance)
			        {
				        attacks++;
			        }
		        }
		        foreach (Planet spawningEnemyPlanet in spawningEnemyPlanets)
		        {
			        Position spawnPosition = spawningEnemyPlanet.GetShipSpawnPosition(gameMap);
			        if (location.ResultingPosition.GetDistanceTo(spawnPosition) <= attackDistance)
			        {
				        attacks++;
			        }
		        }

		        if (attacks < smallestAttackAmount)
		        {
			        smallestAttackAmount = attacks;
					smallestAttackAmountLocations.Clear();
		        }
		        if (attacks == smallestAttackAmount)
		        {
			        smallestAttackAmountLocations.Add(location);
		        }
	        }

			DebugLog.AddLog("ship " + remainingLocations[0].Path.Entity.GetId() + " will take " + smallestAttackAmount + " attacks by kiting after evaluating " + remainingLocations.Count + " possibilities");

			//Only save the locations that make us endure the least amount of attacks
	        return smallestAttackAmountLocations;
	    }

	    private List<LocationInfo> AttackDocked(Ship ship, List<LocationInfo> remainingLocations, List<Ship> enemyShips)
	    {
		    List<LocationInfo> bestDockAttackLocations = new List<LocationInfo>();
	        int bestAttackAmount = 0;
			List<Ship> possibleAttackableDockedEnemyShips = enemyShips.Where(enemyShip => enemyShip.GetDockingStatus() != Ship.DockingStatus.Undocked && enemyShip.GetDistanceTo(ship) - ship.GetRadius() - enemyShip.GetRadius() - Constants.WEAPON_RADIUS <= Constants.MAX_SPEED).ToList();

	        foreach (LocationInfo location in remainingLocations)
	        {
		        int attacks = 0;
		        foreach (Ship enemyShip in possibleAttackableDockedEnemyShips)
		        {
			        if (location.ResultingPosition.GetDistanceTo(enemyShip) <= ship.GetRadius() + enemyShip.GetRadius() + Constants.WEAPON_RADIUS)
			        {
				        attacks++;
			        }
		        }

		        if (attacks != 0 && attacks < bestAttackAmount)
		        {
			        bestAttackAmount = attacks;
					bestDockAttackLocations.Clear();
		        }
		        if (attacks == bestAttackAmount)
		        {
			        bestDockAttackLocations.Add(location);
		        }
	        }

	        return bestDockAttackLocations;
	    }

	    private List<LocationInfo> MinimizeDistance(List<LocationInfo> remainingLocations, List<NavigationPath> enemyShipPaths)
	    {
		    List<LocationInfo> nearbyEnemyLocations = new List<LocationInfo>();
		    int bestNearbyAmount = int.MaxValue;

	        foreach (LocationInfo location in remainingLocations)
	        {
		        int nearbyAmount = 0;

		        foreach (NavigationPath enemyShipPath in enemyShipPaths)
		        {
			        double distance = location.Path.End.GetDistanceTo(enemyShipPath.End);
			        if (distance <= s_nearbyDistance)
			        {
				        nearbyAmount++;
			        }
		        }
				
		        if (nearbyAmount < bestNearbyAmount)
		        {
			        nearbyEnemyLocations.Clear();
			        bestNearbyAmount = nearbyAmount;
		        }
				if (nearbyAmount == bestNearbyAmount)
		        {
			        nearbyEnemyLocations.Add(location);
		        }
	        }

			//Only save the locations that get us far away, disqualify the rest
	        return nearbyEnemyLocations;
	    }

	    private LocationInfo FindWeightedBestLocation(Ship ship, GameMap gameMap, List<LocationInfo> remainingLocations, Ship closestUndockedAlly, Ship closestDockedAlly, Ship closestUndockedEnemy, Ship closestDockedEnemy, bool moveTowardsAllies)
	    {
		    double maximumDistance = double.MinValue;
	        LocationInfo bestLocation = null;
	        Planet closestPlanet = PlanetsByDistance(ship, gameMap)[0];

	        foreach (LocationInfo location in remainingLocations)
	        {
		        if (location.ResultingPosition.GetXPos() < 0 || location.ResultingPosition.GetYPos() < 0)
		        {
			        continue;
		        }

		        double sqrtDistanceSum = 0;
				//Incentivize staying a high distance away from walls and planets
		        sqrtDistanceSum += Math.Sqrt(location.ResultingPosition.GetXPos()) / 2;
				sqrtDistanceSum += Math.Sqrt(location.ResultingPosition.GetYPos()) / 2;
				sqrtDistanceSum += Math.Sqrt(gameMap.GetWidth() - location.ResultingPosition.GetXPos()) / 2;
				sqrtDistanceSum += Math.Sqrt(gameMap.GetHeight() - location.ResultingPosition.GetYPos()) / 2;
		        sqrtDistanceSum += Math.Sqrt(location.ResultingPosition.GetDistanceTo(closestPlanet)) / 2;

				//Incentivize staying a preferred distance away from allied undocked ships
		        if (closestUndockedAlly != null)
		        {
			        sqrtDistanceSum += location.ResultingPosition.GetDistanceTo(closestUndockedAlly) * (moveTowardsAllies ? -1 : 1);
		        }

				//Incentivize staying a high distance away from allied docked ships
		        if (closestDockedAlly != null)
		        {
			        sqrtDistanceSum += location.ResultingPosition.GetDistanceTo(closestDockedAlly) * 2;
		        }

		        //Incentivize staying a high distance away from enemy undocked ships
		        if (closestUndockedEnemy != null)
		        {
			        sqrtDistanceSum += location.ResultingPosition.GetDistanceTo(closestUndockedEnemy);
		        }

		        //Incentivize staying a low distance away from enemy docked ships
		        if (closestDockedEnemy != null)
		        {
			        sqrtDistanceSum += -1 * location.ResultingPosition.GetDistanceTo(closestDockedEnemy) * 3;
		        }

		        if (sqrtDistanceSum > maximumDistance)
		        {
			        bestLocation = location;
			        maximumDistance = sqrtDistanceSum;
		        }
	        }

			return bestLocation;
	    }

        private class LocationInfo
	    {
		    public int Angle;
		    public int Distance;
		    public Position ResultingPosition;
		    public NavigationPath Path;

		    public LocationInfo(int angle, int distance, Position resultingPosition, NavigationPath path)
		    {
			    this.Angle = angle;
			    this.Distance = distance;
			    this.ResultingPosition = resultingPosition;
			    this.Path = path;
		    }
	    }

	    public override StrategyType Type
	    {
		    get { return StrategyType.Kite; }
	    }

	    public override double DistanceToTarget(Ship ship, GameMap gameMap)
	    {
			var undockedEnemyShips = gameMap.ShipsByDistance(ship, false).Where(enemyShip => enemyShip.GetDockingStatus() == Ship.DockingStatus.Undocked).ToList();
		    if (undockedEnemyShips.Count > 0)
		    {
			    return ship.GetDistanceTo(undockedEnemyShips[0]);
		    }
		    return Fallback().DistanceToTarget(ship, gameMap);
	    }
    }
}
