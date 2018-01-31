using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	//Current functionality: Check if limit is reached for target, if yes, resort to fallback
	//Wanted functionality: Dynamically assign targets according to strategy based on distance heuristic
	public static class TargetUtility
	{
		private static Dictionary<Entity, TargetGroup> m_groups = new Dictionary<Entity, TargetGroup>();

		public static void ResetTargets()
		{
			m_groups.Clear();
			TargetGroup.s_recentlyKickedEntity = false;
		}

		public static TargetGroup GetOrCreateGroup(Entity target, int limit)
		{
			if (!m_groups.ContainsKey(target))
			{
				m_groups[target] = new TargetGroup(target, limit);
			}
			return m_groups[target];
		}

		public static bool CreateOrJoinGroup(Entity member, Entity target, int limit)
		{
			if (limit <= 0)
			{
				return false;
			}

			TargetGroup group = GetOrCreateGroup(target, limit);

			if (group.CanJoin(member))
			{
				group.AddMember(member);
				return true;
			}

			return false;
		}
	}

	public class TargetGroup
	{
		public int Limit;
		public Entity Target;

		private List<Entity> m_members = new List<Entity>();
		private Entity m_weakestMember;

		//TODO: refactor this
		public static Entity s_mostRecentKickedEntity;
		public static bool s_recentlyKickedEntity;

		public int MemberCount
		{
			get { return m_members.Count; }
		}

		public bool AtLimit
		{
			get { return MemberCount >= Limit; }
		}

		public bool IsEmpty
		{
			get { return MemberCount == 0; }
		}

		public TargetGroup(Entity target, int limit)
		{
			this.Limit = limit;
			this.Target = target;
		}

		public void AddMember(Entity member, bool kickWeakestIfNecessary = true)
		{
			s_recentlyKickedEntity = false;

			if (kickWeakestIfNecessary && AtLimit)
			{
				s_mostRecentKickedEntity = m_weakestMember;
				m_members.Remove(m_weakestMember);
				s_recentlyKickedEntity = true;
			}

			m_members.Add(member);

			RecalculateWeakest();
		}

		public bool CanJoin(Entity potentialMember)
		{
			if (IsEmpty && Limit > 0)
			{
				return true;
			}
			if (MemberCount < Limit)
			{
				return true;
			}

			double highestDistance = m_weakestMember.GetDistanceTo(Target);

			return potentialMember.GetDistanceTo(Target) < highestDistance;
		}

		public void Resize(int newLimit)
		{
			Limit = newLimit;
		}

		private void RecalculateWeakest()
		{
			double highestDistance = Double.MinValue;
			foreach (Entity member in m_members)
			{
				double distance = member.GetDistanceTo(Target);
				if (distance > highestDistance)
				{
					highestDistance = distance;
					m_weakestMember = member;
				}
			}
		}

		public override string ToString()
		{
			string prettyprint = "";
			prettyprint += "Targetgroup for targeting " + Target;
			prettyprint += "\nSlots: " + MemberCount + "/" + Limit;
			prettyprint += "\nMembers:";
			foreach (Entity member in m_members)
			{
				prettyprint += "\n" + member.ToString();
			}

			return prettyprint;
		}
	}
}