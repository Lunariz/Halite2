using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Halite2.hlt;
using Halite2.Strategies;

namespace Halite2
{
	class StrategyManager
	{
		//TODO: Use reflection & statics instead of instancing every strategy
		//Alternatively, use a singleton for every class
		public static Strategy CreateStrategy(StrategyType type)
		{
			switch (type)
			{
				case StrategyType.Dock:
					return new DockStrategy();
				case StrategyType.DockUnowned:
					return new DockUnownedStrategy();
				case StrategyType.AttackDockedShips:
					return new AttackDockedShipsStrategy();
				case StrategyType.AttackUndockedShips:
					return new AttackUndockedShipsStrategy();
				case StrategyType.DefendDockedShips:
					return new DefendDockedShipsStrategy();
				case StrategyType.CrashIntoPlanet:
					return new CrashIntoPlanetStrategy();
				case StrategyType.CrashIntoShip:
					return new CrashIntoShipStrategy();
				case StrategyType.Grouping:
					return new GroupingStrategy();
				case StrategyType.Kite:
					return new KiteStrategy();
				case StrategyType.Attack:
					return new AttackStrategy();
				case StrategyType.Defend:
					return new DefendStrategy();
				case StrategyType.Desert:
					return new DesertStrategy();
			}

			return null;
		}

		public static int StrategyCount
		{
			get
			{
				return 2;

				//Previously this would allow us to iterate over all strategies, but by reducing the count we reduce the amount of strategies our algorithm considers
				//return Enum.GetValues(typeof(StrategyType)).Length;
			}
		}
	}

	public enum StrategyType
	{
		Defend,
		Attack,

		Dock,
		DockUnowned,
		AttackDockedShips,
		AttackUndockedShips,
		DefendDockedShips,
		CrashIntoPlanet,
		CrashIntoShip,
		Grouping,
		Kite,
		Desert
	}
}