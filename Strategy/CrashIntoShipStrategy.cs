using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Crash into the nearest enemy ship
	class CrashIntoShipStrategy : Strategy
	{
		public override Command Execute(Ship ship, GameMap gameMap)
		{
			if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
			{
				return new UndockCommand(ship);
			}

			var enemyShips = gameMap.ShipsByDistance(ship, false);

			foreach (Ship enemy in enemyShips)
			{
				if (TargetUtility.CreateOrJoinGroup(ship, enemy, 1))
				{
					return new MoveCommand(ship, new PathingTarget(enemy, 0, true));
				}
			}

			return Fallback().Execute(ship, gameMap);
		}

		public override StrategyType Type
		{
			get { return StrategyType.CrashIntoShip; }
		}

		public override double DistanceToTarget(Ship ship, GameMap gameMap)
		{
			var enemyShips = gameMap.ShipsByDistance(ship, false);
			if (enemyShips.Count > 0)
			{
				return ship.GetDistanceTo(enemyShips[0]);
			}
			return Fallback().DistanceToTarget(ship, gameMap);
		}
	}
}