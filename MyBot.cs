using System;
using Halite2.hlt;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Halite2.Strategies;

namespace Halite2
{
    public class MyBot
    {
        private static List<Move> moveList;
        private static GameMap gameMap;

        public static string AppDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

	    public static bool s_disableProfiler = true;

        public static void Main(string[] args)
        {
            if (File.Exists("default.bot") || s_disableProfiler || GameMap.IsLive)
            {
                //We are playing on the server!
                Profiler.DisableProfiler();
            }
			
            string name = args.Length > 0 ? args[0] : "LunarBot";

            Networking networking = new Networking();
            gameMap = networking.Initialize(name);
			
            Profiler.Start("Bot");

            DebugLog.AddLog("Starting match");

            moveList = new List<Move>();

            try
            {
                StrategyChooser strategyChooser = StringStrategyChooser.CreateRandom(250);

                if (args.Length > 0)
                {
                    strategyChooser = StrategyChooser.CreateFromFile(AppDir + "\\bots\\" + args[0]);
	                if (name.Contains("Player"))
	                {
		                GameMap.IsTraining = true;
	                }
	                else
	                {
		                GameMap.IsTesting = true;
	                }
                }
                else if (File.Exists(AppDir + "\\default.bot"))
                {
                    strategyChooser = StrategyChooser.CreateFromFile(AppDir + "\\default.bot");
	                if (name.Contains("Player"))
	                {
		                GameMap.IsTraining = true;
	                }
	                else
	                {
		                GameMap.IsTesting = true;
	                }
                }
                else if (File.Exists("default.bot"))
                {
                    strategyChooser = StrategyChooser.CreateFromFile("default.bot");
	                GameMap.IsLive = true;
                }



                while (true)
                {
                    Profiler.PauseAll();

					//This halts while we are waiting for other players to finish their turn
                    Metadata metadata = Networking.ReadLineIntoMetadata();

                    Profiler.UnpauseAll();

                    gameMap.UpdateMap(metadata);

                    DebugLog.AddLog("---------- turn #" + GameMap.Turn + "---------------");
					
					TargetUtility.ResetTargets();
                    moveList.Clear();

	                List<Ship> alliedShips = gameMap.GetAllShips(true);
					
                    Profiler.Start("Find command for each ship");

	                List<Command> commands = BatchStrategizer.GenerateCommands(strategyChooser, gameMap, alliedShips);

                    Profiler.Stop("Find command for each ship");
					Profiler.Start("Navigation");

                    List<Move> moves = BatchNavigation.GenerateMoves(gameMap, commands);

					Profiler.Stop("Navigation");

                    GameMap.Turn++;

                    Networking.SendMoves(moves);
                }
            }
            catch (Exception e)
            {
                DebugLog.AddLog(e.ToString());
            }

            try
            {
                Profiler.Stop("Bot");

                DebugLog.AddLog(Profiler.GetTopLevelInstance.ToString());
                DebugLog.AddLog("---");
                DebugLog.AddLog(Profiler.GetFlatInstance.ToString());
            }
            catch (Exception e)
            {
                DebugLog.AddLog(e.ToString());
            }
        }
    }
}
