using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Crash into the nearest planet owned by an enemy
    class CrashIntoPlanetStrategy : Strategy
    {
        public override Command Execute(Ship ship, GameMap gameMap)
        {
            if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return new UndockCommand(ship);
            }

            var planets = PlanetsByDistance(ship, gameMap);
            var ID = gameMap.GetMyPlayerId();

            foreach (Planet planet in planets)
            {
                if (planet.IsOwned() && planet.GetOwner() != ID && TargetUtility.CreateOrJoinGroup(ship, planet, (int) Math.Ceiling((float) planet.GetHealth() / 255)))
                {
                    return new MoveCommand(ship, new PathingTarget(planet, 0, true));
                }
            }

            return Fallback().Execute(ship, gameMap);
        }

	    public override StrategyType Type
	    {
		    get { return StrategyType.CrashIntoPlanet; }
	    }

	    public override double DistanceToTarget(Ship ship, GameMap gameMap)
	    {
			var planets = PlanetsByDistance(ship, gameMap).Where(planet => planet.IsOwned() && planet.GetOwner() != gameMap.GetMyPlayerId()).ToList();
		    if (planets.Count > 0)
		    {
			    return ship.GetDistanceTo(planets[0]);
		    }
		    return Fallback().DistanceToTarget(ship, gameMap);
	    }
    }
}
