using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Halite2
{
	class RunUtility
	{
		private static bool s_showFullLog = false;
		private static bool s_showTimeLog = false;
		
		public static string AppDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		private static string rootDir = "";
		public static string RootDir
		{
			get
			{
				if (rootDir == "")
				{
					//AppDir will be RootDir/Bin/Debug, so we simply take the parent directory twice
					rootDir = Path.GetDirectoryName(Path.GetDirectoryName(AppDir)) + "\\";
				}
				return rootDir;
			}
		}

		public static string BotDir
		{
			get { return RootDir + "bots\\"; }
		}

		public static void CompileBot()
		{
			Process process = new Process();

			ProcessStartInfo startInfo = new ProcessStartInfo(RootDir + "compile.bat");
			startInfo.CreateNoWindow = false;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;

			process.StartInfo = startInfo;
			
			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();

			if (!Directory.Exists(BotDir))
			{
				Directory.CreateDirectory(BotDir);
			}
		}

		public static GameResults RunMatch(bool generateReplay, params string[] botFiles)
		{
			return RunMatch(generateReplay, "", Trainer.GetRandomBoardHeight(), botFiles);
		}

		public static GameResults RunMatch(bool generateReplay, string seed, int height, params string[] botFiles)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			string arguments = "-d \"" + (int)(height*1.5f) + " " + height + "\"";

			if (seed != "")
			{
				arguments += " -s " + seed;
			}

			if (!generateReplay)
			{
				arguments += " --noreplay";
			}

			foreach (string botFile in botFiles)
			{
				arguments += " \"" + RootDir + "MyBot.exe " + botFile + "\"";
			}
			
            Process process = new Process();

			ProcessStartInfo startInfo = new ProcessStartInfo(RootDir + "halite.exe");
            startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.Arguments = arguments;

	        process.StartInfo = startInfo;
			
			GameResults results = new GameResults();
			
			process.OutputDataReceived += (sender, arg) => ProcessOutput(arg.Data, ref results);
			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();
			process.CancelOutputRead();

			if (generateReplay)
			{
				string replayPath = AppDir + "\\" + results.FileName;
				if (File.Exists(replayPath))
				{
					File.Move(replayPath, RootDir + results.FileName);
				}
			}

			if (!process.HasExited)
			{
				process.Kill();
			}
			
			process.Close();

			process.Dispose();

			if (s_showTimeLog)
			{
				Console.WriteLine("Total time for game: " + sw.Elapsed);
				Console.WriteLine("TURNS: " + results.Turns);
				Console.WriteLine("--------");
			}

			sw.Stop();

			return results;
		}

		private static void ProcessOutput(string output, ref GameResults results)
		{
			if (output == null)
			{
				return;
			}

			if (s_showFullLog)
			{
				Console.WriteLine(output);
			}
			
			Regex fileRegex = new Regex("Opening a file at \\.\\\\(?<file>.*)");
			Match fileMatch = fileRegex.Match(output);
			if (fileMatch.Success)
			{
				results.FileName = fileMatch.Groups["file"].Value;
			}

			Regex playerResultRegex = new Regex("Player #(?<playerIndex>[0-9]*), (?<playerName>[a-zA-Z0-9 \\.]*), came in rank #(?<rank>[0-9]*) and was last alive on frame #(?<lastFrame>[0-9]*), producing (?<ships>[0-9]*) ships and dealing (?<damage>[0-9]*) damage!");
			Match playerResultMatch = playerResultRegex.Match(output);
			if (playerResultMatch.Success)
			{
				int playerIndex = Convert.ToInt32(playerResultMatch.Groups["playerIndex"].Value);
				int playerRank = Convert.ToInt32(playerResultMatch.Groups["rank"].Value);
				results.PlayerRanks[playerIndex] = playerRank;
				results.RankedPlayers[playerRank] = playerIndex;
			}

			Regex turnRegex = new Regex("Turn (?<turn>[0-9]*)");
			Match turnMatch = turnRegex.Match(output);
			if (turnMatch.Success)
			{
				int turn = Convert.ToInt32(turnMatch.Groups["turn"].Value);
				results.Turns = turn;
			}
		}

		private static string FindBetterBot(string bot1, string bot2, int games = 100, bool generateReplays = false)
		{
			int win1 = 0;
			int win2 = 0;
			for (int i = 0; i < games; i++)
			{
				string leftBot = i % 2 == 0 ? bot1 : bot2;
				string rightBot = i % 2 == 0 ? bot2 : bot1;
				GameResults results = RunMatch(generateReplays, leftBot, rightBot);

				//XOR: either p1 wins or p2 wins and they are swapped, but not both
				win1 += results.IsPlayer1Winner() ^ i%2 == 0 ? 0 : 1;
				win2 += results.IsPlayer1Winner() ^ i%2 == 0 ? 1 : 0;
				
				double currentWinrate = (float) Math.Max(win1, win2) / (win1 + win2);
				Console.WriteLine((results.IsPlayer1Winner() ? leftBot : rightBot) + " wins");
				Console.WriteLine((win1 >= win2 ? bot1 : bot2) + " wins with winrate " + currentWinrate);
			}

			double winRate = (float) Math.Max(win1, win2) / (win1 + win2);

			return win1 >= win2 ? bot1 : bot2;
		}
	}

	public class GameResults
	{
		public string FileName;
		public Dictionary<int, int> PlayerRanks = new Dictionary<int, int>();
		public Dictionary<int, int> RankedPlayers = new Dictionary<int, int>();
		public int Turns;

		public bool IsWinner(int playerIndex)
		{
			return RankedPlayers[1] == playerIndex;
		}

		public bool IsPlayer1Winner()
		{
			return IsWinner(0);
		}
	}
}
