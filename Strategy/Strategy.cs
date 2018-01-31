using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.Strategies
{
	public class Strategy
	{
		public static Dictionary<StrategyType, StrategyType> s_fallbackGraph = new Dictionary<StrategyType, StrategyType>()
		{
			//These are allowed to be cyclical because always at least one will work
			{StrategyType.AttackUndockedShips, StrategyType.AttackDockedShips},
			{StrategyType.AttackDockedShips, StrategyType.AttackUndockedShips},
			{StrategyType.CrashIntoPlanet, StrategyType.AttackUndockedShips},
			{StrategyType.CrashIntoShip, StrategyType.CrashIntoPlanet},
			{StrategyType.DockUnowned, StrategyType.AttackUndockedShips},
			{StrategyType.Dock, StrategyType.DockUnowned},
			{StrategyType.DefendDockedShips, StrategyType.Dock},
			{StrategyType.Grouping, StrategyType.AttackUndockedShips},
			{StrategyType.Kite, StrategyType.AttackUndockedShips}
		};

		public virtual Command Execute(Ship ship, GameMap gameMap)
		{
			return null;
		}

		public virtual Strategy Fallback()
		{
			//DebugLog.AddLog("ship " + ship.GetId() + " performing fallback from strategy " + Type + " to strategy " + s_fallbackGraph[Type]);

			return StrategyManager.CreateStrategy(s_fallbackGraph[Type]);
		}

		protected List<Planet> PlanetsByDistance(Ship ship, GameMap gameMap)
		{
			List<Planet> planets = gameMap.GetAllPlanets();
			planets.Sort((a, b) => ship.GetDistanceTo(ship.GetClosestPoint(a)).CompareTo(ship.GetDistanceTo(ship.GetClosestPoint(b))));
			return planets;
		}

		public virtual StrategyType Type
		{
			get { return StrategyType.DockUnowned; }
		}

		public virtual double DistanceToTarget(Ship ship, GameMap gameMap)
		{
			return Double.MaxValue;
		}
	}
}