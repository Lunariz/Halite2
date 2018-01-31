using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class PathingTarget
	{
		public Position Center;
		public double Radius;
		public bool IgnoreTargetCollision;

		public PathingTarget(Position center, double radius, bool ignoreTargetCollision = false)
		{
			this.Center = center;
			this.Radius = radius;
			this.IgnoreTargetCollision = ignoreTargetCollision;
		}

		public Position GetTargetPosition(Position currentPosition)
		{
			return GetTargetPosition(currentPosition, Center);
		}

		public Position GetTargetPosition(Position currentPosition, Position targetPosition)
		{
			if (currentPosition.GetDistanceTo(targetPosition) < Radius + Center.GetRadius())
			{
				return currentPosition;
			}

			double distance = Math.Min(currentPosition.GetDistanceTo(targetPosition), Radius);
			if (targetPosition != Center)
			{
				distance += Center.GetRadius() - targetPosition.GetRadius();
			}
			return currentPosition.GetClosestPoint(targetPosition, distance);
		}
	}
}
