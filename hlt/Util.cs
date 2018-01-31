using System;
using System.Collections.Generic;
using System.Linq;

namespace Halite2.hlt
{
    public class Util
    {
        public static int AngleRadToDegClipped(double angleRad)
        {
            long degUnclipped = (long)Math.Round(angleRad / Math.PI * 180);
            // Make sure return value is in [0, 360) as required by game engine.
            return (int)(((degUnclipped % 360L) + 360L) % 360L);
        }

	    public static List<T> RandomizeList<T>(List<T> list)
	    {
		    List<int> remainingRandomIndices = Enumerable.Range(0, list.Count).ToList();
			List<T> newList = new List<T>();

			for (int i = 0; i < list.Count; i++)
			{
				int randomIndexIndex = StaticRandom.Rand(remainingRandomIndices.Count);
				int randomIndex = remainingRandomIndices[randomIndexIndex];
				remainingRandomIndices.RemoveAt(randomIndexIndex);

				T randomItem = list[randomIndex];
				newList.Add(randomItem);
			}

			return newList;
	    }
    }
}