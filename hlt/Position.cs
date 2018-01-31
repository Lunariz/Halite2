using System;

namespace Halite2.hlt
{

    public class Position
    {

        private double xPos;
        private double yPos;

        public Position(double xPos, double yPos)
        {
            this.xPos = xPos;
            this.yPos = yPos;
        }

        public double GetXPos()
        {
            return xPos;
        }

        public double GetYPos()
        {
            return yPos;
        }

        public double GetDistanceTo(Position target)
        {
            double dx = xPos - target.GetXPos();
            double dy = yPos - target.GetYPos();
            return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }

        public virtual double GetRadius()
        {
            return 0;
        }

        public int OrientTowardsInDeg(Position target)
        {
            return Util.AngleRadToDegClipped(OrientTowardsInRad(target));
        }

        public double OrientTowardsInRad(Position target)
        {
            double dx = target.GetXPos() - xPos;
            double dy = target.GetYPos() - yPos;

            return Math.Atan2(dy, dx) + 2 * Math.PI;
        }

        public Position GetClosestPoint(Position target)
        {
            return GetClosestPoint(target, Constants.MIN_DISTANCE_FOR_CLOSEST_POINT);
        }

	    public Position GetClosestPoint(Position target, double additionalDistanceFromRadius)
	    {
		    double radius = target.GetRadius() + additionalDistanceFromRadius;
            double angleRad = target.OrientTowardsInRad(this);

            double x = target.GetXPos() + radius * Math.Cos(angleRad);
            double y = target.GetYPos() + radius * Math.Sin(angleRad);

            return new Position(x, y);
	    }

	    public Position GetOvershootPoint(Position target)
	    {
		    double deltaX = target.GetXPos() - this.GetXPos();
		    double deltaY = target.GetYPos() - this.GetYPos();

		    double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

		    if (distance == 0)
		    {
			    return target;
		    }

		    double overshootDistance = Math.Ceiling(distance - 0.0001d);

			//We simply convert back and forth to angles saved as ints to mimic the loss of precision caused by moving in int angles
			double angleRad = Math.Atan2(deltaY, deltaX);
			int angle = Util.AngleRadToDegClipped(angleRad);
		    double newAngleRad = (double) angle / 180 * Math.PI;

		    double newDeltaX = Math.Cos(newAngleRad) * overshootDistance;
		    double newDeltaY = Math.Sin(newAngleRad) * overshootDistance;

			return new Position(this.GetXPos() + newDeltaX, this.GetYPos() + newDeltaY);
	    }

	    public static Position Lerp(Position start, Position end, double lerp)
	    {
		    double deltaX = end.GetXPos() - start.GetXPos();
		    double deltaY = end.GetYPos() - start.GetYPos();

		    double x = start.GetXPos() + deltaX * lerp;
		    double y = start.GetYPos() + deltaY * lerp;

			return new Position(x, y);
	    }

	    public bool IsOutsideBounds(GameMap gameMap)
	    {
		    if (xPos <= 0 || xPos >= gameMap.GetWidth() || yPos <= 0 || yPos >= gameMap.GetHeight())
		    {
			    return true;
		    }

		    return false;
	    }

        public override bool Equals(Object o)
        {
            if (this == o)
                return true;

            if (o == null || GetType() != o.GetType())
                return false;

            Position position = (Position)o;

            if (position == null)
                return false;

            return Equals(position.xPos, xPos) && Equals(position.yPos, yPos);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "Position(" + xPos + ", " + yPos + ")";
        }

		public static Position operator -(Position pos1, Position pos2)
		{
			return new Position(pos1.GetXPos() - pos2.GetXPos(), pos1.GetYPos() - pos2.GetYPos());
		}

		public static Position operator +(Position pos1, Position pos2)
		{
			return new Position(pos1.GetXPos() + pos2.GetXPos(), pos1.GetYPos() + pos2.GetYPos());
		}
    }
}