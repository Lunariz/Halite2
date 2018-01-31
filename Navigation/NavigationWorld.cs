using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;
using Entity = Halite2.hlt.Entity;

namespace Halite2
{
	public class NavigationWorld
	{
		public static double s_collisionIncrement = 1d;

		public GameMap Map;
		public ConcurrentDictionary<Entity, NavigationPath> Paths = new ConcurrentDictionary<Entity, NavigationPath>();
		public Dictionary<Entity, List<Position>> ProjectedPositions = new Dictionary<Entity, List<Position>>();
		public Dictionary<Entity, BoundingBox> BoundingBoxes = new Dictionary<Entity, BoundingBox>();
		public Dictionary<Entity, List<NavigationMovement>> SubdividedPaths = new Dictionary<Entity, List<NavigationMovement>>();

		public NavigationWorld(GameMap map, ConcurrentDictionary<Entity, NavigationPath> paths)
		{
			this.Map = map;
			this.Paths = new ConcurrentDictionary<Entity, NavigationPath>(paths);

			foreach (KeyValuePair<Entity, NavigationPath> kvp in Paths)
			{
				ProjectedPositions[kvp.Key] = kvp.Value.GetIncrementalPositions(s_collisionIncrement);
				BoundingBoxes[kvp.Key] = new BoundingBox(kvp.Value);
				SubdividedPaths[kvp.Key] = NavigationCollisionUtility.SubdividePath(kvp.Value);
			}
		}

		public void Update(ConcurrentDictionary<Entity, NavigationPath> newPaths)
		{
			foreach (KeyValuePair<Entity, NavigationPath> kvp in newPaths)
			{
				Update(kvp.Key, kvp.Value);
			}
		}

		public void Update(Entity entity, NavigationPath newPath)
		{
			Paths[entity] = newPath;
			ProjectedPositions[entity] = newPath.GetIncrementalPositions(s_collisionIncrement);
			BoundingBoxes[entity] = new BoundingBox(newPath);
			SubdividedPaths[entity] = NavigationCollisionUtility.SubdividePath(newPath);
		}

		public CollisionInfo FindCollision(NavigationPath navigationPath, Position ignoreCollider = null, double maxDistance = Double.MaxValue)
		{
			List<NavigationMovement> subdividedNavigationPath = NavigationCollisionUtility.SubdividePath(navigationPath);
			List<KeyValuePair<Entity, List<NavigationMovement>>> relevantSubdividedPaths = SubdividedPaths.Where(kvp => BoundingBoxes[kvp.Key].NavPathIntersect(navigationPath, maxDistance)).ToList();

			double navigationDistance = 0;

			for (int i = 0; i < subdividedNavigationPath.Count && navigationDistance <= maxDistance; i++)
			{
				NavigationMovement navigationMovement = subdividedNavigationPath[i];

				if (navigationMovement.End.IsOutsideBounds(Map))
				{
					//TODO: Find the proper collision location. Halfway is just an approximation
					return new CollisionInfo(true, navigationMovement.Entity, Position.Lerp(navigationMovement.Start, navigationMovement.End, 0.5d), navigationDistance);
				}

				int offsetIndex = i + navigationPath.TimeOffset;

				CollisionInfo closestCollision = CollisionInfo.CreateNoCollision();
				closestCollision.Distance = Double.MaxValue;

				foreach (var kvp in relevantSubdividedPaths)
				{
					if (kvp.Key.Equals(navigationMovement.Entity) || kvp.Key.Equals(ignoreCollider))
					{
						continue;
					}

					List<NavigationMovement> subdividedColliderPath = kvp.Value;
					NavigationMovement colliderMovement = subdividedColliderPath.Count > offsetIndex ? subdividedColliderPath[offsetIndex] : new NavigationMovement(kvp.Key, Paths[kvp.Key].End, Paths[kvp.Key].End);

					double collisionDistance = navigationPath.Entity.GetRadius() + colliderMovement.Entity.GetRadius();

					//Shortcut: if the two colliders start too far away, don't bother checking for collisions
					double maxStartDistance = collisionDistance + Constants.MAX_SPEED + (colliderMovement.Entity is Ship ? Constants.MAX_SPEED : 0);
					if (colliderMovement.Start.GetDistanceTo(navigationMovement.Start) > maxStartDistance)
					{
						continue;
					}

					CollisionInfo collision = NavigationCollisionUtility.CreateCollision(navigationMovement, colliderMovement, collisionDistance, navigationDistance);

					if (collision.CollisionHappened && collision.Distance < closestCollision.Distance)
					{
						closestCollision = collision;
					}
				}

				if (closestCollision.CollisionHappened)
				{
					return closestCollision;
				}

				navigationDistance += navigationMovement.Distance;
			}

			return CollisionInfo.CreateNoCollision();
		}

		public Position FindInterceptPosition(Position startingPosition, double distanceOffset, PathingTarget target)
		{
			if (!(target.Center is Entity) || !ProjectedPositions.ContainsKey(target.Center as Entity))
			{
				return target.GetTargetPosition(startingPosition);
			}

			List<Position> targetIncrementalPositions = ProjectedPositions[target.Center as Entity];

			//We have already traveled (distanceOffset), so we start our intercept at the (index) point where the target also already traveled that much
			for (double i = distanceOffset / s_collisionIncrement; i < targetIncrementalPositions.Count; i++)
			{
				//But the amount we are allowed to travel should be (distanceOffset) less than how far along they are now
				double allowedDistance = (i - distanceOffset / s_collisionIncrement) * s_collisionIncrement;

				double lerp = (distanceOffset / s_collisionIncrement) % 1d;
				int theirIndexFloor = Math.Min((int) (i), targetIncrementalPositions.Count - 1);
				int theirIndexCeil = Math.Min(theirIndexFloor + 1, targetIncrementalPositions.Count - 1);
				Position positionFloor = targetIncrementalPositions[theirIndexFloor];
				Position positionCeil = targetIncrementalPositions[theirIndexCeil];
				Position lerpedPosition = Position.Lerp(positionFloor, positionCeil, lerp);
				Position potentialInterceptPosition = target.GetTargetPosition(startingPosition, lerpedPosition);

				double distanceToIncrementalPosition = potentialInterceptPosition.GetDistanceTo(startingPosition);

				if (distanceToIncrementalPosition <= allowedDistance)
				{
					return potentialInterceptPosition;
				}
			}

			//It was not possible to intercept the path because either the target is only moving a small distance, or is moving away from us
			//So instead, we'll just travel to where their path ends

			Position theirEndpoint = targetIncrementalPositions[targetIncrementalPositions.Count - 1];

			return target.GetTargetPosition(startingPosition, theirEndpoint);
		}
	}

	public class CollisionInfo
	{
		public bool CollisionHappened;
		public Entity Collider;
		public Position ColliderPosition;
		public double Distance;

		public CollisionInfo(bool collisionHappened, Entity collider, Position colliderPosition, double distance)
		{
			this.CollisionHappened = collisionHappened;
			this.Collider = collider;
			this.ColliderPosition = colliderPosition;
			this.Distance = distance;
		}

		public static CollisionInfo CreateNoCollision()
		{
			return new CollisionInfo(false, null, null, 0);
		}
	}
}