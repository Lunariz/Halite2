cd %~dp0
IF EXIST MyBot.exe DEL /F MyBot.exe
csc -out:MyBot.exe MyBot.cs hlt\*.cs Scripts\*.cs Trainer\StrategyChoosers\*.cs Strategy\*.cs Strategy\Commands\*.cs Navigation\*.cs /reference:Redzen.dll /recurse:Trainer\NEAT\*.cs
IF EXIST Bot.dll DEL /F Bot.dll
csc /t:library /out:Bot.dll hlt\*.cs Scripts\*.cs Trainer\StrategyChoosers\*.cs Strategy\*.cs Strategy\Commands\*.cs Navigation\*.cs /reference:Redzen.dll /recurse:Trainer\NEAT\*.cs