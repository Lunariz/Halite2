using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Dock on the nearest planet, if it is safe
    class DockStrategy : Strategy
    {
		private static double s_nearbyEnemyDistance = 25d;
		private static float s_preferredDefendRatio = 1.1f;

        public override Command Execute(Ship ship, GameMap gameMap)
        {
            if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return new Command(ship, Command.CommandType.NoCommand);
            }

            var ID = gameMap.GetMyPlayerId();
			
			List<Planet> unownedOrAlliedPlanets = gameMap.NearbyPlanetsByDistance(ship).Where(p => !p.IsOwned() || (p.IsOwned() && p.GetOwner() == ID)).ToList();

			Dictionary<Ship, Planet> closestUnownedPlanet = new Dictionary<Ship, Planet>();
			foreach (Ship otherShip in gameMap.GetAllShips())
			{
				Planet closestPlanet = null;
				double closestPlanetDistance = double.MaxValue;
				foreach (Planet unownedPlanet in unownedOrAlliedPlanets)
				{
					double distance = unownedPlanet.GetDistanceTo(otherShip) - unownedPlanet.GetRadius();
					if (distance < closestPlanetDistance)
					{
						closestPlanetDistance = distance;
						closestPlanet = unownedPlanet;
					}
				}

				closestUnownedPlanet[otherShip] = closestPlanet;
			}

            foreach (Planet planet in unownedOrAlliedPlanets)
            {
		        bool dontDock = false;

		        List<Ship> nearbyEnemyShips = gameMap.GetAllShips(false).Where(s => s.GetDockingStatus() == Ship.DockingStatus.Undocked && closestUnownedPlanet[s] == planet && s.GetDistanceTo(planet) <= s_nearbyEnemyDistance).ToList();

		        int preferredGroupSize = (int) (nearbyEnemyShips.Count * s_preferredDefendRatio + 1);

		        //Defend against potential attackers by not docking ships
				//nearbyEnemyShips[0] is pretty hacky, but we can't make different targetgroups for the same planet
	            if (nearbyEnemyShips.Count > 0)
	            {
		            if (TargetUtility.CreateOrJoinGroup(ship, nearbyEnemyShips[0], preferredGroupSize))
		            {
			            dontDock = true;
		            }
	            }

	            if (TargetUtility.CreateOrJoinGroup(ship, planet, planet.GetDockingSpots() - planet.GetDockedShips().Count))
		        {
			        if (dontDock)
			        {
				        return new KiteStrategy().Calculate(ship, gameMap, true, 7.5d);
			        }

			        if (ship.CanDock(planet))
			        {
				        return new DockCommand(ship, planet);
			        }

			        return new MoveCommand(ship, new PathingTarget(planet, Constants.DOCK_RADIUS));
		        }
            }

            return Fallback().Execute(ship, gameMap);
        }

	    public override StrategyType Type
	    {
		    get { return StrategyType.Dock; }
	    }

	    public override double DistanceToTarget(Ship ship, GameMap gameMap)
	    {
			var planets = PlanetsByDistance(ship, gameMap).Where(planet => planet.GetOwner() == gameMap.GetMyPlayerId() && !planet.IsFull()).ToList();
		    if (planets.Count > 0)
		    {
			    return ship.GetDistanceTo(planets[0]);
		    }
		    return Fallback().DistanceToTarget(ship, gameMap);
	    }
    }
}
