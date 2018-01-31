using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Position in between the nearest docked allied ship, and undocked enemy ship, in order to defend the docked ship from attacks
	class DefendDockedShipsStrategy : Strategy
	{
		private static float s_defendDistance = 5f;
		private static float s_defendLeeway = 3f;
		private static int s_defendLimit = 3;

		public override Command Execute(Ship ship, GameMap gameMap)
		{
			var enemies = gameMap.ShipsByDistance(ship, false);
			Ship enemyShip = enemies[0];
			var allies = gameMap.ShipsByDistance(enemyShip, true);
			var dockedAllies = allies.Where(a => a.GetDockingStatus() != Ship.DockingStatus.Undocked).ToList();
			dockedAllies.Remove(ship);

			if (dockedAllies.Count == 0)
			{
				return Fallback().Execute(ship, gameMap);
			}

			if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
			{
				return new UndockCommand(ship);
			}

			foreach (Ship allyShip in dockedAllies)
			{
				if (TargetUtility.CreateOrJoinGroup(ship, allyShip, s_defendLimit))
				{
					Position targetPosition = enemyShip.GetClosestPoint(allyShip, s_defendDistance);
					return new MoveCommand(ship, new PathingTarget(targetPosition, s_defendLeeway));
				}
			}

			return Fallback().Execute(ship, gameMap);
		}

		public override StrategyType Type
		{
			get { return StrategyType.DefendDockedShips; }
		}

		public override double DistanceToTarget(Ship ship, GameMap gameMap)
		{
			var enemies = gameMap.ShipsByDistance(ship, false);
			Ship enemyShip = enemies[0];
			var allies = gameMap.ShipsByDistance(enemyShip, true);
			var dockedAllies = allies.Where(a => a.GetDockingStatus() != Ship.DockingStatus.Undocked).ToList();
			dockedAllies.Remove(ship);

			if (dockedAllies.Count > 0)
			{
				return ship.GetDistanceTo(enemyShip.GetClosestPoint(dockedAllies[0], s_defendDistance));
			}
			return Fallback().DistanceToTarget(ship, gameMap);
		}
	}
}