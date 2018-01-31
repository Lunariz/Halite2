using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Get in attacking range of the closest docked enemy ship
	class AttackDockedShipsStrategy : Strategy
	{
		public override Command Execute(Ship ship, GameMap gameMap)
		{
			if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
			{
				return new UndockCommand(ship);
			}

			var enemyShips = gameMap.ShipsByDistance(ship, false);

			foreach (Ship enemyShip in enemyShips)
			{
				if (enemyShip.GetDockingStatus() != Ship.DockingStatus.Undocked)
				{
					return new MoveCommand(ship, new PathingTarget(enemyShip, Constants.WEAPON_RADIUS + enemyShip.GetRadius()));
				}
			}

			return Fallback().Execute(ship, gameMap);
		}

		public override StrategyType Type
		{
			get { return StrategyType.AttackDockedShips; }
		}

		public override double DistanceToTarget(Ship ship, GameMap gameMap)
		{
			var enemyShips = gameMap.ShipsByDistance(ship, false).Where(enemyShip => enemyShip.GetDockingStatus() != Ship.DockingStatus.Undocked).ToList();
			if (enemyShips.Count > 0)
			{
				return ship.GetDistanceTo(enemyShips[0]);
			}

			return Fallback().DistanceToTarget(ship, gameMap);
		}
	}
}