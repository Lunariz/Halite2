using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Halite2
{
	public static class StaticRandom
	{
		private static Random _global = new Random();

		static readonly ThreadLocal<Random> random =
			new ThreadLocal<Random>(() =>
			{
				int seed;
				lock (_global) seed = _global.Next();
				return new Random(seed);
			});

		public static int Rand()
		{
			return random.Value.Next();
		}

		public static int Rand(int max)
		{
			return random.Value.Next(max);
		}

		public static int Rand(int min, int max)
		{
			return random.Value.Next(min, max);
		}

		public static double RandDouble()
		{
			return random.Value.NextDouble();
		}
	}
}