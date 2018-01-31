using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite2
{
	public class RandomTrainer : Trainer
	{
		public static bool SaveStrongChampions = false;
		public static int s_minimumChampionStrength = 5;

		public StrategyChooser Champion;

		private int m_chooserSize;
		private StrategyChooserType m_type;

		public RandomTrainer(int chooserSize, StrategyChooserType type)
		{
			m_chooserSize = chooserSize;
			m_type = type;
			Champion = StrategyChooser.CreateRandom(chooserSize, type);
		}

		public override void Train(int generations)
		{
			int defeated = 0;

			for (int i = 0; i < generations; i++)
			{
				StrategyChooser contender = StrategyChooser.CreateRandom(m_chooserSize, m_type);

				string championFile = CreateFile(Champion, "champion");
				string contenderFile = CreateFile(contender, "contender");

				GameResults results = RunUtility.RunMatch(false, championFile, contenderFile);
				if (results.IsWinner(1))
				{
					if (SaveStrongChampions && defeated >= s_minimumChampionStrength)
					{
						CreateFile(Champion, "gen" + (i - defeated - 1) + "_defeated_" + defeated);
					}

					Champion = contender;

					Console.WriteLine("A new champion is born! Gen " + i + " is now the best that ever was");
					defeated = 0;
				}
				else
				{
					defeated++;
				}
			}

			if (SaveStrongChampions)
			{
				CreateFile(Champion, "finalChampion");
			}
		}
	}
}
