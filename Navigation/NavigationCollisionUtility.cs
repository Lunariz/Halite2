using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    public class NavigationCollisionUtility
    {
		//Find a collision between two paths by splitting them into movements and then checking for collisions step by step
		public static CollisionInfo FindCollisionBetweenPaths(NavigationPath navigationPath, NavigationPath colliderPath, Position ignoreCollider, double maxDistance)
        {
            if (colliderPath.Entity.Equals(navigationPath.Entity) || colliderPath.Entity.Equals(ignoreCollider))
            {
                return CollisionInfo.CreateNoCollision();
            }

            List<Position> navigationPositions = new List<Position>();
            List<Position> colliderPositions = new List<Position>();
            double navigationDistance = 0;
	        double colliderDistance = navigationPath.DistanceOffset;

            int navigationFrame = 0;
            int colliderFrame = colliderPath.GetMovementFrameAtDistance(colliderDistance);

            double navigationFrameDistance = 0;
            double colliderFrameDistance = colliderPath.GetDistanceAtFrame(colliderFrame);

            navigationPositions.Add(navigationPath.GetPositionAtFrame(0));
            colliderPositions.Add(colliderPath.GetPositionAtDistance(colliderDistance));

            while (navigationFrame < navigationPath.Frames && navigationDistance < maxDistance)
            {
                NavigationMovement navigationFrameMovement = navigationPath.GetMovementAtFrame(navigationFrame);
                NavigationMovement colliderFrameMovement = colliderPath.GetMovementAtFrame(colliderFrame);

				double navigationDistanceRemaining =
                    navigationFrameMovement.Distance - (navigationDistance - navigationFrameDistance);
                double colliderDistanceRemaining =
                    colliderFrameMovement.Distance - (colliderDistance - colliderFrameDistance);

				//If both are above/at maxspeed, reduce both to a multiple of maxspeed, and take the lowest (e.g. a distance of 15 and 8 becomes a distance of 7 for both)
				//If both are below maxspeed, simply keep the values the same (e.g. a distance of 5 and 6 stay as 5 and 6)
				//If one is below maxspeed, reduce the other to maxspeed (e.g. a distance of 6 and 8 become a distance of 6 and 7)
	            double navigationMovementDistance = navigationDistanceRemaining >= Constants.MAX_SPEED && colliderDistanceRemaining >= Constants.MAX_SPEED
		            ? Math.Min(navigationDistanceRemaining - navigationDistanceRemaining % Constants.MAX_SPEED,
			            colliderDistanceRemaining - colliderDistanceRemaining % Constants.MAX_SPEED)
		            : Math.Min(navigationDistanceRemaining, Constants.MAX_SPEED);

				double colliderMovementDistance = navigationDistanceRemaining >= Constants.MAX_SPEED && colliderDistanceRemaining >= Constants.MAX_SPEED
		            ? Math.Min(navigationDistanceRemaining - navigationDistanceRemaining % Constants.MAX_SPEED,
			            colliderDistanceRemaining - colliderDistanceRemaining % Constants.MAX_SPEED)
		            : Math.Min(colliderDistanceRemaining, Constants.MAX_SPEED);

				//However when they are exactly equal, we can always go the whole way on both
	            if (Math.Abs(navigationDistanceRemaining - colliderDistanceRemaining) < 0.001d)
	            {
		            navigationMovementDistance = navigationDistanceRemaining;
		            colliderMovementDistance = colliderDistanceRemaining;
	            }
	            if (Math.Abs(colliderDistanceRemaining) < 0.001d)
	            {
		            navigationMovementDistance = navigationDistanceRemaining;
	            }

	            {
					double lerp = (navigationDistance - navigationFrameDistance + navigationMovementDistance) /
                                  navigationFrameMovement.Distance;
                    Position lerpedPosition = (navigationFrameMovement.StandsStill) ? navigationFrameMovement.Start : Position.Lerp(navigationFrameMovement.Start, navigationFrameMovement.End, lerp);
					
                    navigationPositions.Add(lerpedPosition);
					navigationDistance += navigationMovementDistance;

					if (Math.Abs(navigationMovementDistance - navigationDistanceRemaining) < 0.0001d)
					{
						navigationFrame++;
						navigationFrameDistance += navigationFrameMovement.Distance;
					}
                }
				
                {
					double lerp = (colliderDistance - colliderFrameDistance + colliderMovementDistance) /
                                  colliderFrameMovement.Distance;
                    Position lerpedPosition = (colliderFrameMovement.StandsStill) ? colliderFrameMovement.Start : Position.Lerp(colliderFrameMovement.Start, colliderFrameMovement.End, lerp);

                    colliderPositions.Add(lerpedPosition);
	                colliderDistance += colliderMovementDistance;

	                if (Math.Abs(colliderMovementDistance - colliderDistanceRemaining) < 0.0001d)
	                {
		                colliderFrame++;
		                colliderFrameDistance += colliderFrameMovement.Distance;
	                }
                }
            }

            double collisionDistance = navigationPath.Entity.GetRadius() + colliderPath.Entity.GetRadius();
            navigationDistance = 0;

            for (int i = 0; i < navigationPositions.Count - 1; i++)
            {
                NavigationMovement navigationMovement = new NavigationMovement(navigationPath.Entity,
                    navigationPositions[i], navigationPositions[i + 1]);
                NavigationMovement colliderMovement = new NavigationMovement(colliderPath.Entity, colliderPositions[i],
                    colliderPositions[i + 1]);

                double t = CalculateCollisionTimeBetweenMovements(navigationMovement, colliderMovement,
                    collisionDistance);

				if (t >= 0 && t <= 1)
                {
                    return new CollisionInfo(true, colliderPath.Entity,
                        Position.Lerp(colliderMovement.Start, colliderMovement.End, t),
                        navigationDistance + navigationMovement.Distance * t);
                }

                navigationDistance += navigationMovement.Distance;
            }

            return CollisionInfo.CreateNoCollision();
        }

	    public static CollisionInfo CreateCollision(NavigationMovement navigationMovement, NavigationMovement colliderMovement, double collisionDistance, double navigationDistance)
	    {
		    double t = CalculateCollisionTimeBetweenMovements(navigationMovement, colliderMovement, collisionDistance);

			if (t >= 0 && t <= 1)
            {
                return new CollisionInfo(true, colliderMovement.Entity,
                    Position.Lerp(colliderMovement.Start, colliderMovement.End, t),
                    navigationDistance + navigationMovement.Distance * t);
            }

			return CollisionInfo.CreateNoCollision();
	    }

	    public static List<NavigationMovement> SubdividePath(NavigationPath path)
	    {
		    List<NavigationMovement> subdividedPath = new List<NavigationMovement>();

			List<Position> navigationPositions = new List<Position>();
            double navigationDistance = 0;
            int navigationFrame = 0;
            double navigationFrameDistance = 0;

            navigationPositions.Add(path.GetPositionAtFrame(0));

            while (navigationFrame < path.Frames)
            {
                NavigationMovement navigationFrameMovement = path.GetMovementAtFrame(navigationFrame);

				//We skip segments with length 0 within the path
	            while (navigationFrameMovement.StandsStill && navigationFrame < path.Frames + 1)
	            {
		            navigationFrame++;
					navigationFrameMovement = path.GetMovementAtFrame(navigationFrame);
	            }
	            if (navigationFrame >= path.Frames + 1)
	            {
		            break;
	            }

                double navigationDistanceRemaining = navigationFrameMovement.Distance - (navigationDistance - navigationFrameDistance);
	            double navigationMovementDistance = Math.Min(navigationDistanceRemaining, Constants.MAX_SPEED);

				double lerp = (navigationDistance - navigationFrameDistance + navigationMovementDistance) /
                                navigationFrameMovement.Distance;
                Position lerpedPosition = (navigationFrameMovement.StandsStill) ? navigationFrameMovement.Start : Position.Lerp(navigationFrameMovement.Start, navigationFrameMovement.End, lerp);
					
                navigationPositions.Add(lerpedPosition);
				navigationDistance += navigationMovementDistance;

				if (Math.Abs(navigationMovementDistance - navigationDistanceRemaining) < 0.0001d)
				{
					navigationFrame++;
					navigationFrameDistance += navigationFrameMovement.Distance;
				}
            }

		    for (int i = 0; i < navigationPositions.Count - 1; i++)
		    {
			    NavigationMovement navigationMovement = new NavigationMovement(path.Entity,
				    navigationPositions[i], navigationPositions[i + 1]);
				subdividedPath.Add(navigationMovement);
		    }

		    return subdividedPath;
	    }

        public static double CalculateCollisionTimeBetweenMovements(NavigationMovement movement1, NavigationMovement movement2, double distance)
        {
			//Subtract vector2 from vector1
			NavigationMovement movement = new NavigationMovement(movement1.Entity, movement1.Start, movement1.End - movement2.Delta);

			//return t for point-to-movement version
			return CalculateCollisionTime(movement2.Start, movement, distance);
        }

		//https://stackoverflow.com/questions/1073336/circle-line-segment-collision-detection-algorithm
	    public static double CalculateCollisionTime(Position p, NavigationMovement movement, double distance)
	    {
		    Position d = movement.Delta;
			Position f = new Position(movement.Start.GetXPos() - p.GetXPos(), movement.Start.GetYPos() - p.GetYPos());

		    double a = d.GetXPos() * d.GetXPos() + d.GetYPos() * d.GetYPos(); //d dot d
		    double b = 2 * (f.GetXPos() * d.GetXPos() + f.GetYPos() * d.GetYPos()); //2*(f dot d)
		    double c = (f.GetXPos() * f.GetXPos() + f.GetYPos() * f.GetYPos()) - (distance * distance); //(f dot f) - r * r

			double discriminant = b*b-4*a*c;
			if(discriminant < 0)
			{
				// no intersection
				return -1;
			}

			discriminant = Math.Sqrt(discriminant);

			double t1 = (-b - discriminant)/(2*a);
			//double t2 = (-b + discriminant)/(2*a);

			// 3x HIT cases:
			//          -o->             --|-->  |            |  --|->
			// Impale(t1 hit,t2 hit), Poke(t1 hit,t2>1), ExitWound(t1<0, t2 hit), 

			// 3x MISS cases:
			//       ->  o                     o ->              | -> |
			// FallShort (t1>1,t2>1), Past (t1<0,t2<0), CompletelyInside(t1<0, t2>1)

			if (t1 >= 0 && t1 <= 1)
			{
				// t1 is the intersection, and it's closer than t2
				// (since t1 uses -b - discriminant)
				// Impale, Poke
				return t1;
			}

			//We want to catch situations where the segment starts out colliding, rather than returning the t where they would then stop colliding
			if (p.GetDistanceTo(movement.Start) <= distance)
			{
				//ExitWound, CompletelyInside
				return 0;
			}

			//No collision: FallShort or Past
		    return -1;
	    }

	    public static double PointDistanceToLineSegment(Position position, NavigationMovement movement, out double t)
        {
            Position pt = position;
            Position p1 = movement.Start;
            Position p2 = movement.End;

            double dx = p2.GetXPos() - p1.GetXPos();
            double dy = p2.GetYPos() - p1.GetYPos();
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                dx = pt.GetXPos() - p1.GetXPos();
                dy = pt.GetYPos() - p1.GetYPos();

                t = 0;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            t = ((pt.GetXPos() - p1.GetXPos()) * dx + (pt.GetYPos() - p1.GetYPos()) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                t = 0;
                dx = pt.GetXPos() - p1.GetXPos();
                dy = pt.GetYPos() - p1.GetYPos();
            }
            else if (t > 1)
            {
                t = 1;
                dx = pt.GetXPos() - p2.GetXPos();
                dy = pt.GetYPos() - p2.GetYPos();
            }
            else
            {
                Position closest = new Position(p1.GetXPos() + t * dx, p1.GetYPos() + t * dy);
                dx = pt.GetXPos() - closest.GetXPos();
                dy = pt.GetYPos() - closest.GetYPos();
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }
		
	    public static bool IntersectsAABB(NavigationPath path, BoundingBox box)
	    {
		    if (path.Positions.Count == 1)
            {
                BoundingBox pointBox = new BoundingBox(path.Positions[0], path.Positions[0], path.Entity.GetRadius());
	            if (pointBox.Overlap(box))
	            {
		            return true;
	            }
            }

			for (int i = 0; i < path.Positions.Count - 1; i++)
            {
                BoundingBox pathSegmentBox = new BoundingBox(path.Positions[i], path.Positions[i + 1], path.Entity.GetRadius());
	            if (pathSegmentBox.Overlap(box))
	            {
		            return true;
	            }
            }

		    return false;
	    }
    }

	public class BoundingBox
    {
        private double m_minX, m_maxX, m_minY, m_maxY;

        public BoundingBox(Position posA, Position posB, double radius)
        {
            m_minX = Math.Min(posA.GetXPos(), posB.GetXPos()) - radius;
            m_maxX = Math.Max(posA.GetXPos(), posB.GetXPos()) + radius;
            m_minY = Math.Min(posA.GetYPos(), posB.GetYPos()) - radius;
            m_maxY = Math.Max(posA.GetYPos(), posB.GetYPos()) + radius;
        }

	    public BoundingBox(NavigationPath path)
	    {
		    double minX = double.MaxValue;
		    double maxX = double.MinValue;
		    double minY = double.MaxValue;
		    double maxY = double.MinValue;

		    foreach (Position position in path.Positions)
		    {
			    double posMinX = position.GetXPos() - path.Entity.GetRadius();
			    double posMaxX = position.GetXPos() + path.Entity.GetRadius();
			    double posMinY = position.GetYPos() - path.Entity.GetRadius();
			    double posMaxY = position.GetYPos() + path.Entity.GetRadius();

			    if (posMinX < minX)
			    {
				    minX = posMinX;
			    }
				if (posMaxX > maxX)
			    {
				    maxX = posMaxX;
			    }
				if (posMinY < minY)
			    {
				    minY = posMinY;
			    }
				if (posMaxY > maxY)
			    {
				    maxY = posMaxY;
			    }
		    }

		    m_minX = minX;
		    m_maxX = maxX;
		    m_minY = minY;
		    m_maxY = maxY;
	    }

        public bool Overlap(BoundingBox other)
        {
            return (other.m_maxX >= m_minX && other.m_minX <= m_maxX) && (other.m_maxY >= m_minY && other.m_minY <= m_maxY);
        }

        public bool NavPathIntersect(NavigationPath navPath, double maxDistance)
        {
            double minX = m_minX - navPath.Entity.GetRadius();
            double minY = m_minY - navPath.Entity.GetRadius();
            double maxX = m_maxX + navPath.Entity.GetRadius();
            double maxY = m_maxY + navPath.Entity.GetRadius();

	        double distance = 0;

            for (int i = 0; i < navPath.Positions.Count - 1; i++)
            {
	            double t;
                if (Intersect(navPath.Positions[i], navPath.Positions[i + 1], minX, minY, maxX, maxY, out t))
                {
	                distance += navPath.GetDistanceAtFrame(i) * t;
                    return distance <= maxDistance;
                }

	            distance += navPath.GetDistanceAtFrame(i);

	            if (distance > maxDistance)
	            {
		            return false;
	            }
            }

            return false;
        }

        private bool Intersect(Position start, Position end, double minX, double minY, double maxX, double maxY, out double t)
        {
	        t = -1;

            double vecX = end.GetXPos() - start.GetXPos();
            double vecY = end.GetYPos() - start.GetYPos();
            double tx1 = (minX - start.GetXPos()) / vecX;
            double tx2 = (maxX - start.GetXPos()) / vecX;

            double tmin = Math.Min(tx1, tx2);
            double tmax = Math.Max(tx1, tx2);

            double ty1 = (minY - start.GetYPos()) / vecY;
            double ty2 = (maxY - start.GetYPos()) / vecY;

            tmin = Math.Max(tmin, Math.Min(ty1, ty2));
            tmax = Math.Min(tmax, Math.Max(ty1, ty2));

            //Either behind origin or beyond reach of path
            if (tmax > 1 && tmin > 1)
                return false;
            if (tmax < 0 && tmin < 0)
                return false;

	        t = tmin;

            return tmax >= tmin;
        }
    }
}
