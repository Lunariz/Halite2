using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;
using Halite2.Strategies;

namespace Halite2
{
	public class StrategyChooser
	{
		public string TemporaryName;

		public virtual Strategy ChooseStrategy(Ship ship, GameMap gameMap)
		{
			return null;
		}

		public virtual StrategyChooser Crossover(StrategyChooser other)
		{
			return null;
		}

		public virtual void Mutate(float mutateChance = 0.05f, float insertionChance = 0.05f, float removalChance = 0.05f)
		{
		}

		public static StrategyChooser CreateRandom(int size)
		{
			return null;
		}

		public static StrategyChooser CreateRandom(int size, StrategyChooserType type)
		{
			switch (type)
			{
				case StrategyChooserType.String:
					return StringStrategyChooser.CreateRandom(size);
			}

			return null;
		}

		public static StrategyChooser CreateFromFile(string filePath)
		{
			string[] fileLines = File.ReadAllLines(filePath);
			string identifierLine = fileLines[0];

			if (identifierLine == "String")
			{
				return StringStrategyChooser.CreateFromFile(filePath);
			}
			if (identifierLine == "NEAT")
			{
				return NEATStrategyChooser.CreateFromFile(filePath);
			}

			return null;
		}
	}

	public enum StrategyChooserType
	{
		None,
		String,
		NEAT
	}
}