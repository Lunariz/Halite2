using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class NavigationPath
	{
		public Entity Entity;
		public PathingTarget Target;
		public List<Position> Positions = new List<Position>();
		public double DistanceOffset = 0;
		public int TimeOffset;

		public int Frames { get { return Positions.Count; } }

		public Position Start
		{
			get { return GetPositionAtFrame(0); }
			set { Positions[0] = value; }
		}

		public Position End
		{
			get { return GetPositionAtFrame(Frames - 1); }
			set { Positions[Frames - 1] = value; }
		}

		public NavigationPath(Entity entity)
		{
			this.Entity = entity;
		}

		public static HashSet<Entity> VolatilePaths = new HashSet<Entity>();

		public ThrustMove GetThrustMove()
		{
			Ship ship = Entity as Ship;

			int navigationFrame = 0;
			NavigationMovement navigationMovement = GetMovementAtFrame(navigationFrame);

			double angleRad = Math.Atan2(navigationMovement.Delta.GetYPos(), navigationMovement.Delta.GetXPos());
			int angle = Util.AngleRadToDegClipped(angleRad);

			double decimalThrust = navigationMovement.Distance;
			
			//This floating point magic usually won't matter, but it avoids ceiling too far because of floating point errors and overshooting
			int thrust = (int) (decimalThrust > 0.01d ? Math.Ceiling(decimalThrust - 0.0001d) : decimalThrust);
			thrust = Math.Min(thrust, Constants.MAX_SPEED);
			
			return new ThrustMove(ship, angle, thrust);
		}

		public static NavigationPath CreateStatic(Entity entity)
		{
			NavigationPath navigationPath = new NavigationPath(entity);
			navigationPath.Positions.Add(entity);

			return navigationPath;
		}

		public static NavigationPath CreateSimple(Entity entity, Position targetPosition)
		{
			NavigationPath navigationPath = new NavigationPath(entity);
			navigationPath.Positions.Add(entity);
			navigationPath.Positions.Add(targetPosition);

			return navigationPath;
		}

		public static NavigationPath CreateExtrapolated(Entity entity, Position previousPosition, int maxMoves = 3)
		{
			NavigationPath navigationPath = new NavigationPath(entity);
			double deltaX = entity.GetXPos() - previousPosition.GetXPos();
			double deltaY = entity.GetYPos() - previousPosition.GetYPos();

			navigationPath.Positions.Add(entity);
			navigationPath.Positions.Add(new Position(entity.GetXPos() + deltaX * maxMoves, entity.GetYPos() + deltaY * maxMoves));

			return navigationPath;
		}

		private static int MaxIterations
		{
			get { return GameMap.IsLive ? 7 : 2; }
		}
		
		public static NavigationPath CreateModified(Entity entity, Position startingPosition, PathingTarget target, NavigationWorld world, double distanceOffset = 0, int timeOffset = 0, int maxIterations = -1, int iteration = 0)
		{
			if (maxIterations == -1)
			{
				maxIterations = MaxIterations;
			}

			NavigationPath navigationPath = new NavigationPath(entity);
			navigationPath.Target = target;
			navigationPath.TimeOffset = timeOffset;
			navigationPath.Positions.Add(startingPosition);
			
			Position targetPosition = world.FindInterceptPosition(startingPosition, distanceOffset, target);
			targetPosition = startingPosition.GetOvershootPoint(targetPosition);

			navigationPath.Positions.Add(targetPosition);

			if (navigationPath.GetMovementAtFrame(0).Distance < 0.01d)
			{
				return navigationPath;
			}

			navigationPath.Modify(world, target, maxIterations, iteration);

			if (iteration >= maxIterations)
			{
				VolatilePaths.Add(entity);
			}

			return navigationPath;
		}
		
		public void Modify(NavigationWorld world, PathingTarget target, int maxIterations, int iteration = 0, bool onlyFirstHalf = false)
		{
			if (iteration >= maxIterations)
			{
				if (!onlyFirstHalf)
				{
					//We are out of iterations - any attempt we make at moving results in a collision, so let's stop early instead
					End = GetPositionAtFrame(Frames - 1);
				}

				return;
			}

			Position ignoreTargetCollision = GetIgnoreTarget();
			double maxDistance = onlyFirstHalf ? GetMovementAtFrame(0).Distance : Double.MaxValue;
			CollisionInfo collision = world.FindCollision(this, ignoreTargetCollision, maxDistance);

			if (!collision.CollisionHappened)
			{
				return;
			}
			
			//We try to avoid the collision if we have more iterations ahead of us, otherwise we simply stop right before the collision
			Position modifiedTarget = iteration < maxIterations - 1 ? CreateCollisionAvoidance(collision, target) : CreateCollisionStop(collision);
			modifiedTarget = Positions[0].GetOvershootPoint(modifiedTarget);
			
			Positions[1] = modifiedTarget;

			//Now there are 2 options, either the avoidance point is reachable immediately or it is not.
			//If it is reachable, this modify does nothing - we have our path from start to avoidance point, and will continue creating the path from the avoidance point to the target
			//If it is not reachable, we need to find another avoidance point that would get us closer to the original avoidance point, choose that as the new avoidance point, and discard the old one.
			//We repeat this recursively to find the closest (hopefully) reachable avoidance point, and then calculate the rest of the path from there
			Modify(world, target, maxIterations, iteration+1, true);

			double newDistanceOffset = DistanceOffset + GetMovementAtFrame(0).Distance;
			int newTimeOffset = TimeOffset + (int) (GetMovementAtFrame(0).Distance / Constants.MAX_SPEED) + ((GetMovementAtFrame(0).Distance % Constants.MAX_SPEED) < 0.001d ? 0 : 1);

			if (!onlyFirstHalf)
			{
				//We find a variable length path from the avoidance point to the end by repeatedly splitting until all subpaths have no collisions
				NavigationPath newContinuedNavigationPath = CreateModified(Entity, Positions[1], target, world, newDistanceOffset, newTimeOffset,
					maxIterations, iteration + 1);

				//We combine the two variable length paths. They have only one shared point, the avoidance point, which we only copy from the second half
				CombineWith(newContinuedNavigationPath, Frames - 1);
			}
		}
		
		public Position GetIgnoreTarget()
		{
			return Target != null && Target.IgnoreTargetCollision ? Target.Center : null;
		}

		public Position CreateCollisionAvoidance(CollisionInfo info, PathingTarget target)
		{
			Position p = Start;
			Position c = info.ColliderPosition;

			double cx = c.GetXPos();
			double cy = c.GetYPos();

			// find tangents
			double radius = info.Collider.GetRadius() + Entity.GetRadius();
			double dx = cx - p.GetXPos();
			double dy = cy - p.GetYPos();
			double dd = Math.Sqrt(dx * dx + dy * dy);
			double a = Math.Asin(radius / dd);
			double b = Math.Atan2(dy, dx);

			if (dd < radius)
			{
				//There are no tangents if p is inside the circle!
				//There is no valid path from inside another collider, so we cut the algorithm short by no longer travelling further than this point

				return p;
			}

			double t1 = b - a;
			Position tangent1 = new Position(cx + radius * Math.Sin(t1), cy + radius * -Math.Cos(t1));

			double t2 = b + a;
			Position tangent2 = new Position(cx + radius * -Math.Sin(t2), cy + radius * Math.Cos(t2));

			Position closestToTarget = target.Center.GetDistanceTo(tangent2) > target.Center.GetDistanceTo(tangent1) ? tangent1 : tangent2;

			NavigationMovement colliderCenterToNewTargetVector = new NavigationMovement(Entity, info.ColliderPosition, closestToTarget);

			double safetyMargin = 1d;
			double moveRatio = (Entity.GetRadius() + info.Collider.GetRadius() + safetyMargin) / colliderCenterToNewTargetVector.Distance;
			Position newPosition = new Position(info.ColliderPosition.GetXPos() + colliderCenterToNewTargetVector.Delta.GetXPos() * moveRatio, info.ColliderPosition.GetYPos() + colliderCenterToNewTargetVector.Delta.GetYPos() * moveRatio);

			return newPosition;
		}

		public Position CreateCollisionStop(CollisionInfo info)
		{
			//We simply stop right before the collision
			double safetyMargin = 1d;
			return GetPositionAtDistance(Math.Floor(Math.Max(0, info.Distance - safetyMargin)));
		}

		public void ModifyCollisionStop(CollisionInfo info)
		{
			double distance = info.Distance;
			int frame = Math.Min(GetMovementFrameAtDistance(distance), Positions.Count-2); // If the collision is exactly at the end of our path, we want to modify the end of our path, not the non-existant point afterwards
			Position safePosition = CreateCollisionStop(info);

			//Modify the movement that would make us collide such that we stop right before the collision
			Positions[frame + 1] = safePosition;

			//Remove any positions thereafter so we stop at that point
			if (Positions.Count > frame + 2)
			{
				Positions.RemoveRange(frame + 2, Positions.Count - (frame + 2));
			}
		}

		public Position GetPositionAtFrame(int frame)
		{
			int maxFrame = Math.Min(frame, Frames - 1);
			return Positions[maxFrame];
		}

		public Position GetPositionAtDistance(double distance)
		{
			double totalDistance = 0;
			for (int i = 0; i < Frames; i++)
			{
				NavigationMovement navigationMovement = GetMovementAtFrame(i);
				if (totalDistance + navigationMovement.Distance > distance)
				{
					return navigationMovement.GetPartwayPosition(distance - totalDistance);
				}

				totalDistance += navigationMovement.Distance;
			}

			return Positions[Frames - 1];
		}

		public double GetDistanceAtFrame(int frame)
		{
			double totalDistance = 0;

			for (int i = 0; i < frame; i++)
			{
				NavigationMovement navigationMovement = GetMovementAtFrame(i);
				totalDistance += navigationMovement.Distance;
			}

			return totalDistance;
		}

		public int GetMovementFrameAtDistance(double distance)
		{
			double totalDistance = 0;
			for (int i = 0; i < Frames; i++)
			{
				NavigationMovement navigationMovement = GetMovementAtFrame(i);
				if (totalDistance + navigationMovement.Distance >= distance)
				{
					//i is the frame where we are about to make a move that will get us to the required distance
					return i;
				}

				totalDistance += navigationMovement.Distance;
			}

			return Frames - 1;
		}

		public List<Position> GetIncrementalPositions(double increment)
		{
			List<Position> positions = new List<Position>();

			//We make sure to only return one position if this entity is standing still
			if (Frames == 1 || (Frames == 2 && GetMovementAtFrame(0).StandsStill))
			{
				positions.Add(Start);
				return positions;
			}
			
			double totalMovementDistance = 0;
			double totalDistance = 0;
			
			for (int i = 0; i < Frames; i++)
			{
				NavigationMovement navigationMovement = GetMovementAtFrame(i);
				double newTotalMovementDistance = totalMovementDistance + navigationMovement.Distance;
				while (totalDistance <= newTotalMovementDistance)
				{
					double partwayDistance = totalDistance - totalMovementDistance;
					Position incrementalPosition = navigationMovement.GetPartwayPosition(partwayDistance);

					positions.Add(incrementalPosition);

					totalDistance += increment;
				}

				totalMovementDistance = newTotalMovementDistance;
			}

			positions.Add(Positions[Frames - 1]);

			return positions;
		}

		public NavigationMovement GetMovementAtFrame(int frame)
		{
			return new NavigationMovement(Entity, GetPositionAtFrame(frame), GetPositionAtFrame(frame+1));
		}

		public void CombineWith(NavigationPath addition, int additionIndex)
		{
			List<Position> newPositions = new List<Position>();
			for (int i = 0; i < additionIndex; i++)
			{
				newPositions.Add(Positions[i]);
			}
			foreach (Position position in addition.Positions)
			{
				newPositions.Add(position);
			}

			Positions = newPositions;
		}

		public override string ToString()
		{
			string pretty = "";
			for (int i=0; i<Positions.Count; i++)
			{
				var pos = Positions[i];
				pretty += pos + (i == Positions.Count-1 ? "" : "\n");
			}
			return pretty;
		}
	}
}
