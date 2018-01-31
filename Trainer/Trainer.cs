using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class Trainer
	{
		private static ConcurrentQueue<string> MatchStringPool = new ConcurrentQueue<string>();

		public virtual void Train(int generations)
		{
			throw new Exception("Base class Trainer is not a valid trainer, use one of the implemented Trainers instead");
		}

		public static string CreateFile(StrategyChooser strategyChooser, string name)
		{
			strategyChooser.TemporaryName = name;
			string fileContents = strategyChooser.ToString();
			string path = RunUtility.BotDir + name + ".bot";

			if (File.Exists(path))
			{
				File.Delete(path);
			}

			File.WriteAllText(path, fileContents);

			return name + ".bot";
		}

		public static GameResults PlayMatch(params StrategyChooser[] strategyChoosers)
		{
			return PlayMatch(false, strategyChoosers);
		}

		public static GameResults PlayMatch(bool generateReplay, params StrategyChooser[] strategyChoosers)
		{
			string matchString = TakeMatchString();

			List<string> playerFiles = new List<string>();
			for (int i = 0; i < strategyChoosers.Length; i++)
			{
				string playerFile = CreateFile(strategyChoosers[i], "Player" + (i + 1) + matchString);
				playerFiles.Add(playerFile);
			}

			GameResults results = RunUtility.RunMatch(generateReplay, playerFiles.ToArray());

			foreach (string playerFile in playerFiles)
			{
				string filePath = RunUtility.BotDir + playerFile;
				File.Delete(filePath);
			}

			ReturnMatchString(matchString);

			return results;
		}

		public static void ClearLogs()
		{
			string path = RunUtility.AppDir;
			string[] files = System.IO.Directory.GetFiles(path, "*.log");
			foreach (string file in files)
			{
				File.Delete(file);
			}
		}

		private static string CreateRandomString(int length = 10)
		{
			string randomString = "";
			for (int i = 0; i < length; i++)
			{
				randomString += (char) StaticRandom.Rand(97, 122);
			}
			return randomString;
		}

		private static string TakeMatchString()
		{
			string matchString;
			if (MatchStringPool.TryDequeue(out matchString))
			{
				return matchString;
			}
			else
			{
				return CreateRandomString();
			}
		}

		private static void ReturnMatchString(string matchString)
		{
			MatchStringPool.Enqueue(matchString);
		}

		public static int GetRandomBoardHeight()
		{
			//Matches distribution of sizes on the server
			//https://github.com/HaliteChallenge/Halite-II/blob/master/apiserver/apiserver/coordinator/matchmaking.py#L9
			int[] possibleHeights = new[] {80, 80, 88, 88, 96, 96, 96, 104, 104, 104, 104, 112, 112, 112, 120, 120, 128, 128};

			return possibleHeights[StaticRandom.Rand(possibleHeights.Length)]*2;
		}
	}
}
