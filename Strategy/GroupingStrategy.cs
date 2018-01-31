using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	//Form a group with nearby allied ships to be safer together
	class GroupingStrategy : Strategy
	{
		private static float s_groupingDistance = 5f;
		private static int s_groupLimit = 4;

		public override Command Execute(Ship ship, GameMap gameMap)
		{
			if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
			{
				return new UndockCommand(ship);
			}

			var ships = gameMap.ShipsByDistance(ship, true);

			foreach (Ship ally in ships)
			{
				if (ship.GetDistanceTo(ally) < s_groupingDistance)
				{
					return new NoCommand(ship);
				}

				if (TargetUtility.CreateOrJoinGroup(ship, ally, s_groupLimit))
				{
					return new MoveCommand(ship, new PathingTarget(ally, s_groupingDistance));
				}
			}

			return Fallback().Execute(ship, gameMap);
		}

		public override StrategyType Type
		{
			get { return StrategyType.Grouping; }
		}

		public override double DistanceToTarget(Ship ship, GameMap gameMap)
		{
			var ships = gameMap.ShipsByDistance(ship, true);
			if (ships.Count > 0)
			{
				return ship.GetDistanceTo(ships[0]);
			}
			return Fallback().DistanceToTarget(ship, gameMap);
		}
	}
}