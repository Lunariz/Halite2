using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;
using Halite2.Strategies;

namespace Halite2
{
	public class BatchStrategizer
	{
		public static List<Command> GenerateCommands(StrategyChooser strategyChooser, GameMap gameMap, List<Ship> ships)
		{
			Profiler.Start("Choose strategies");

			Dictionary<Entity, Strategy> strategies = new Dictionary<Entity, Strategy>();

			foreach (Ship ship in ships)
			{
				strategies[ship] = strategyChooser.ChooseStrategy(ship, gameMap);
			}

			Dictionary<Entity, Command> commands = new Dictionary<Entity, Command>();

			Profiler.Stop("Choose strategies");
			Profiler.Start("Execute strategies");

			foreach (Ship ship in ships)
			{
				Profiler.Start(strategies[ship].ToString());

				commands[ship] = strategies[ship].Execute(ship, gameMap);

				Profiler.Stop(strategies[ship].ToString());

				//If executing the strategy forced another ship out of his group, immediately reevaluate their strategy to join a new group
				if (TargetGroup.s_recentlyKickedEntity)
				{
					Entity kickedEntity = TargetGroup.s_mostRecentKickedEntity;

					Profiler.Start(strategies[kickedEntity].ToString());

					commands[kickedEntity] = strategies[kickedEntity].Execute(kickedEntity as Ship, gameMap);

					Profiler.Stop(strategies[kickedEntity].ToString());
				}
			}

			Profiler.Stop("Execute strategies");

			return commands.Values.ToList();
		}
	}
}