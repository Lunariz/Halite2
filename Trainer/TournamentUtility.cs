using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class TournamentUtility
	{
		//These values should be changed manually to parallelize the most efficiently for a given amount of cores.
		//These values are not easy to calculate, as a game simulation can take between 2 and 5 cores at any time (amount of players calculating their turn + referee)
		//On a 24 core machine, the most efficient values were 7 and 8.
		private static int s_duelDegreeOfParallelism = 1;
		private static int s_groupDegreeOfParallelism = 1;

		private static double[] s_duelPoints = {1d, 0d};
		private static double[] s_groupPoints = {1d, 0.66d, 0.33d, 0d};

		public static List<StrategyChooser> RunSelectionTournament(List<StrategyChooser> choosers, int winners,
			int duelRounds = 1, int groupRounds = 1)
		{
			TournamentResults results = RunTournament(choosers, duelRounds, groupRounds);

			List<StrategyChooser> sortedChoosers = choosers.GetRange(0, choosers.Count);

			//Sort by points per game from high to low
			sortedChoosers.Sort((a, b) => -1 * results.AveragePointsPerGame[a].CompareTo(results.AveragePointsPerGame[b]));

			return sortedChoosers.GetRange(0, winners);
		}

		public static TournamentResults RunTournament(List<StrategyChooser> choosers, int duelRounds = 1, int groupRounds = 1)
		{
			ConcurrentDictionary<StrategyChooser, double> gamePoints = new ConcurrentDictionary<StrategyChooser, double>();
			ConcurrentDictionary<StrategyChooser, int> gameCounts = new ConcurrentDictionary<StrategyChooser, int>();

			foreach (StrategyChooser chooser in choosers)
			{
				gamePoints[chooser] = 0;
				gameCounts[chooser] = 0;
			}

			PlayGames(choosers, duelRounds, 2, s_duelDegreeOfParallelism, s_duelPoints, gamePoints, gameCounts);
			PlayGames(choosers, groupRounds, 4, s_groupDegreeOfParallelism, s_groupPoints, gamePoints, gameCounts);

			TournamentResults tournamentResults = new TournamentResults(gamePoints, gameCounts);

			Trainer.ClearLogs();

			return tournamentResults;
		}

		private static void PlayGames(List<StrategyChooser> choosers, int rounds, int playerCount, int degreesOfParallelism, double[] pointAllocation, ConcurrentDictionary<StrategyChooser, double> gamePoints, ConcurrentDictionary<StrategyChooser, int> gameCounts)
		{
			List<StrategyChooser[]> plannedMatches = new List<StrategyChooser[]>();

			for (int i = 0; i < rounds; i++)
			{
				List<StrategyChooser> randomizedChoosers = Util.RandomizeList(choosers);

				for (int roundIndex = 0; roundIndex < randomizedChoosers.Count / playerCount; roundIndex++)
				{
					int index = roundIndex * playerCount;

					StrategyChooser[] players = new StrategyChooser[playerCount];
					for (int j = 0; j < playerCount; j++)
					{
						players[j] = randomizedChoosers[index + j];
					}

					plannedMatches.Add(players);
				}
			}

			Parallel.For(0, plannedMatches.Count, new ParallelOptions {MaxDegreeOfParallelism = degreesOfParallelism}, i =>
			{
				StrategyChooser[] players = plannedMatches[i];
				GameResults gameResults = Trainer.PlayMatch(players);

				foreach (var kvp in gameResults.PlayerRanks)
				{
					StrategyChooser player = players[kvp.Key];
					int rank = kvp.Value;

					double points = pointAllocation[rank - 1];
					gamePoints[player] += points;
					gameCounts[player]++;
				}
			});
		}
	}

	public class TournamentResults
	{
		public ConcurrentDictionary<StrategyChooser, double> AveragePointsPerGame = new ConcurrentDictionary<StrategyChooser, double>();

		public TournamentResults(ConcurrentDictionary<StrategyChooser, double> gamePoints,
			ConcurrentDictionary<StrategyChooser, int> gameCounts)
		{
			foreach (var kvp in gamePoints)
			{
				AveragePointsPerGame[kvp.Key] = kvp.Value / Math.Max(1, gameCounts[kvp.Key]);
			}
		}
	}
}