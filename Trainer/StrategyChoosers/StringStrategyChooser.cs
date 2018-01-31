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
	public class StringStrategyChooser : StrategyChooser
	{
		public List<StrategyType> Strategies = new List<StrategyType>();

		private Dictionary<int, Strategy> m_shipStrategies = new Dictionary<int, Strategy>();
		private int m_strategyIndex = 0;

		public StringStrategyChooser(string input = "")
		{
			string[] lines = input.Split(Environment.NewLine.ToCharArray());
			if (lines.Length <= 1)
			{
				return;
			}

			string strategyLine = lines[lines.Length - 1];
			string[] strategies = strategyLine.Split(',');

			foreach (string strategy in strategies)
			{
				int strategyIndex = Convert.ToInt32(strategy);
				StrategyType type = (StrategyType) strategyIndex;
				Strategies.Add(type);
			}
		}

		public StringStrategyChooser(List<StrategyType> input)
		{
			Strategies = input;
		}

		public override Strategy ChooseStrategy(Ship ship, GameMap gameMap)
		{
			if (!m_shipStrategies.ContainsKey(ship.GetId()))
			{
				Strategy newStrategy = StrategyManager.CreateStrategy(GetNextStrategyType());

				m_shipStrategies[ship.GetId()] = newStrategy;
			}

			return m_shipStrategies[ship.GetId()];
		}

		private StrategyType GetNextStrategyType()
		{
			StrategyType type = Strategies[m_strategyIndex % Strategies.Count];
			m_strategyIndex++;
			return type;
		}

		public override StrategyChooser Crossover(StrategyChooser other)
		{
			if (!(other is StringStrategyChooser))
			{
				return null;
			}

			StringStrategyChooser otherString = other as StringStrategyChooser;

			int smallestSize = Math.Min(this.Strategies.Count, otherString.Strategies.Count);
			StringStrategyChooser biggestChooser = (this.Strategies.Count > otherString.Strategies.Count) ? this : otherString;

			List<StrategyType> crossoverStrats = new List<StrategyType>();

			for (int i = 0; i < smallestSize; i++)
			{
				StrategyType randomType = (StaticRandom.RandDouble() < 0.5f) ? Strategies[i] : otherString.Strategies[i];
				crossoverStrats.Add(randomType);
			}

			for (int i = smallestSize; i < biggestChooser.Strategies.Count; i++)
			{
				crossoverStrats.Add(biggestChooser.Strategies[i]);
			}

			return new StringStrategyChooser(crossoverStrats);
		}

		public override void Mutate(float mutateChance = 0.05f, float insertionChance = 0.05f, float removalChance = 0.05f)
		{
			List<StrategyType> newStrategies = new List<StrategyType>();
			foreach (StrategyType type in Strategies)
			{
				if (StaticRandom.RandDouble() < removalChance)
				{
					continue;
				}
				if (StaticRandom.RandDouble() < insertionChance)
				{
					newStrategies.Add(ChooseRandomStrategyType());
				}
				if (StaticRandom.RandDouble() < mutateChance)
				{
					newStrategies.Add(ChooseRandomStrategyType());
				}
				else
				{
					newStrategies.Add(type);
				}
			}

			Strategies = newStrategies;
		}

		public override string ToString()
		{
			string output = "String\n";
			for (int i = 0; i < Strategies.Count; i++)
			{
				StrategyType type = Strategies[i];
				int strategyIndex = (int) type;

				output += strategyIndex;

				if (i < Strategies.Count - 1)
				{
					output += ",";
				}
			}

			return output;
		}

		public static StrategyChooser CreateRandom(int size)
		{
			StringStrategyChooser strategyString = new StringStrategyChooser();
			for (int i = 0; i < size; i++)
			{
				strategyString.Strategies.Add(ChooseRandomStrategyType());
			}

			return strategyString;
		}

		public static StringStrategyChooser CreateFromFile(string filePath)
		{
			string fileContents = File.ReadAllText(filePath);
			return new StringStrategyChooser(fileContents);
		}

		private static StrategyType ChooseRandomStrategyType()
		{
			int count = StrategyManager.StrategyCount;

			int random = StaticRandom.Rand(count);
			StrategyType type = (StrategyType) Enum.GetValues(typeof(StrategyType)).GetValue(random);

			return type;
		}
	}
}