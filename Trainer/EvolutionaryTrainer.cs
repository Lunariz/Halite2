using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class EvolutionaryTrainer : Trainer
	{
		private static int s_poolSize = 64;
		private static bool s_saveProgress = true;

		private List<StrategyChooser> m_strategyChooserPool = new List<StrategyChooser>();

		private int m_currentGen = 0;

		public EvolutionaryTrainer(int chooserSize, StrategyChooserType type, bool loadExisting = false, int loadedGen = 0)
		{
			if (loadExisting)
			{
				LoadAll();
				m_currentGen = loadedGen + 1;
			}
			else
			{
				InitializeRandom(chooserSize, type);
			}
		}

		public override void Train(int generations)
		{
			for (; m_currentGen < generations; m_currentGen++)
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				Console.WriteLine("starting generation " + m_currentGen + " at " + DateTime.Now);
				BreedPool(m_currentGen);

				stopwatch.Stop();

				SaveAll();

				Console.WriteLine("generation took: " + stopwatch.Elapsed);
			}
		}

		private void BreedPool(int generation)
		{
			List<StrategyChooser> winners = TournamentUtility.RunSelectionTournament(m_strategyChooserPool, m_strategyChooserPool.Count / 2, 7, 14);

			if (s_saveProgress)
			{
				//Sample this gen;
				PlayMatch(true, winners[0], winners[1]);

				//Save the winner in this gen;
				CreateFile(winners[0], "gen" + generation);
			}

			List<StrategyChooser> newPool = new List<StrategyChooser>();
			for (int i = 0; i < s_poolSize; i++)
			{
				StrategyChooser randomChooser1 = winners[StaticRandom.Rand(winners.Count)];
				StrategyChooser randomChooser2 = winners[StaticRandom.Rand(winners.Count)];

				StrategyChooser newChooser = randomChooser1.Crossover(randomChooser2);
				newChooser.Mutate();

				newPool.Add(newChooser);
			}

			m_strategyChooserPool = newPool;
		}

		public void SaveAll()
		{
			for (int i = 0; i < s_poolSize; i++)
			{
				CreateFile(m_strategyChooserPool[i], "currentGen" + i);
			}
		}

		public void InitializeRandom(int chooserSize, StrategyChooserType type)
		{
			for (int i = 0; i < s_poolSize; i++)
			{
				m_strategyChooserPool.Add(StrategyChooser.CreateRandom(chooserSize, type));
			}
		}

		public void LoadAll()
		{
			m_strategyChooserPool.Clear();
			for (int i = 0; i < s_poolSize; i++)
			{
				m_strategyChooserPool.Add(StrategyChooser.CreateFromFile(RunUtility.BotDir + "currentGen" + i + ".bot"));
			}
		}
	}
}
