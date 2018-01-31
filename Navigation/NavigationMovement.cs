using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class NavigationMovement
	{
		public Entity Entity;
		public Position Start;
		public Position End;

		public NavigationMovement(Entity entity, Position start, Position end)
		{
			this.Entity = entity;
			this.Start = start;
			this.End = end;
		}

		public Position Delta
		{
			get { return new Position(End.GetXPos() - Start.GetXPos(), End.GetYPos() - Start.GetYPos()); }
		}

		public double Distance
		{
			get { return Math.Sqrt(Math.Pow(Delta.GetXPos(), 2) + Math.Pow(Delta.GetYPos(), 2)); }
		}

		public bool StandsStill
		{
			get { return Distance < 0.001f; }
		}

		public Position GetPartwayPosition(double distance)
		{
			if (distance == 0 || Distance == 0)
			{
				return Start;
			}

			double distanceRatio = distance / Distance;
			return new Position(Start.GetXPos() + Delta.GetXPos() * distanceRatio, Start.GetYPos() + Delta.GetYPos() * distanceRatio);
		}

		public override string ToString()
		{
			return "(" + Start.GetXPos() + ", " + Start.GetYPos() + ") to (" + End.GetXPos() + ", " + End.GetYPos() + ")";
		}
	}
}
