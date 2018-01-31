using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Specifically dock on the nearest planet that no player owns yet
    class DockUnownedStrategy : Strategy
    {
        public override Command Execute(Ship ship, GameMap gameMap)
        {
            if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
            {
                return new Command(ship, Command.CommandType.NoCommand);
            }

            var planets = PlanetsByDistance(ship, gameMap);

            foreach (Planet planet in planets)
            {
	            if (!planet.IsOwned() && !planet.IsFull())
	            {
					//We only go to planets that nobody else is going to (no group has been created, and thus upon creation membercount = 0)
					TargetGroup group = TargetUtility.GetOrCreateGroup(planet, 1);
		            if (group.MemberCount == 0)
		            {
			            group.AddMember(ship);
			            if (ship.CanDock(planet))
			            {
				            return new DockCommand(ship, planet);
			            }

			            return new MoveCommand(ship, new PathingTarget(planet, Constants.DOCK_RADIUS));
		            }
	            }
            }

            return Fallback().Execute(ship, gameMap);
        }

	    public override StrategyType Type
	    {
		    get { return StrategyType.DockUnowned; }
	    }

	    public override double DistanceToTarget(Ship ship, GameMap gameMap)
	    {
			var planets = PlanetsByDistance(ship, gameMap).Where(planet => !planet.IsOwned() && !planet.IsFull()).ToList();
		    if (planets.Count > 0)
		    {
			    return ship.GetDistanceTo(planets[0]);
		    }
		    return Fallback().DistanceToTarget(ship, gameMap);
	    }
    }
}
