using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Halite2
{
	public class Profiler
	{
		private static bool s_disabled;

		private static ProfilingInstance s_topLevelInstance;
		public static ProfilingInstance GetTopLevelInstance
		{
			get
			{
				return s_topLevelInstance;
			}
		}

		private static ProfilingInstance s_flatProfilingInstance = new FlatProfilingInstance("Flat").Start("Flat");
		public static ProfilingInstance GetFlatInstance
		{
			get
			{
				return s_flatProfilingInstance;
			}
		}

		public static void Start(string name, bool flatOnly = false)
		{
			if (s_disabled)
			{
				return;
			}

			//var req = new ProfilingRequest(name, flatOnly, true);
			//s_profilingRequests.Enqueue(req);

			StartProfiling(name, flatOnly);
		}

		public static void Stop(string name, bool flatOnly = false)
		{
			if (s_disabled)
			{
				return;
			}

			//var req = new ProfilingRequest(name, flatOnly, false);
			//s_profilingRequests.Enqueue(req);

			StopProfiling(name, flatOnly);
		}

		public static void PauseAll()
		{
			if (s_disabled)
			{
				return;
			}

			s_topLevelInstance.Pause();
			s_flatProfilingInstance.Pause();
		}

		public static void UnpauseAll()
		{
			if (s_disabled)
			{
				return;
			}

			s_topLevelInstance.Unpause();
			s_flatProfilingInstance.Unpause();
		}

		public static void DisableProfiler()
		{
			s_disabled = true;
		}

		private static void StartProfiling(string name, bool flatOnly = false)
		{
			s_flatProfilingInstance.Start(name);

			if (!flatOnly)
			{
				if (s_topLevelInstance != null)
				{
					s_topLevelInstance.Start(name);
				}
				else
				{
					s_topLevelInstance = new ProfilingInstance(name);
					s_topLevelInstance.Start(name);
				}
			}
		}

		private static void StopProfiling(string name, bool flatOnly = false)
		{
			s_flatProfilingInstance.Stop(name);

			if (!flatOnly)
			{
				s_topLevelInstance.Stop(name);
			}
		}

		private struct ProfilingRequest
		{
			public string Name;
			public bool FlatOnly;
			public bool Start;

			public ProfilingRequest(string name, bool flatOnly, bool start)
			{
				this.Name = name;
				this.FlatOnly = flatOnly;
				this.Start = start;
			}
		}
	}

	public class ProfilingInstance
	{
		public string Name;
		public Dictionary<string, ProfilingInstance> SubInstances = new Dictionary<string, ProfilingInstance>();
		public ProfilingInstance Parent;
		public ProfilingInstance CurrentActiveSubInstance;
		public int TimesStarted = 0;

		private TimeSpan m_profilingTime = new TimeSpan(0);

		public virtual TimeSpan ProfilingTime
		{
			get { return m_profilingTime; } 
			set { m_profilingTime = value; }
		}

		public Stopwatch Stopwatch;
		public bool CurrentlyProfiling;

		public ProfilingInstance(string name, ProfilingInstance parent = null)
		{
			this.Name = name;
			this.Parent = parent;
		}

		public ProfilingInstance Start(string name)
		{
			ProfilingInstance instance = FindOrCreateInstance(name);

			instance.Start();

			return instance;
		}

		private void Start()
		{
			if (CurrentlyProfiling)
			{
				throw new Exception("Starting profiling twice in a row without stopping on profiler: " + Name);
			}

			Stopwatch = new Stopwatch();
			Stopwatch.Start();

			TimesStarted++;

			if (Parent != null)
			{
				Parent.CurrentActiveSubInstance = this;
			}

			CurrentlyProfiling = true;
		}

		public void Stop(string name)
		{
			ProfilingInstance instance = FindOrCreateInstance(name);
			
			instance.Stop();
		}

		private void Stop()
		{
			if (!CurrentlyProfiling)
			{
				throw new Exception("Stopping while not even started on profiler: " + Name);
			}

			Stopwatch.Stop();
			ProfilingTime += Stopwatch.Elapsed;

			if (Parent != null)
			{
				Parent.CurrentActiveSubInstance = null;
			}

			CurrentlyProfiling = false;
		}

		public void Pause()
		{
			if (CurrentlyProfiling)
			{
				Stopwatch.Stop();
				foreach (ProfilingInstance subinstance in SubInstances.Values)
				{
					subinstance.Pause();
				}
			}
		}

		public void Unpause()
		{
			if (CurrentlyProfiling)
			{
				Stopwatch.Start();
				foreach (ProfilingInstance subinstance in SubInstances.Values)
				{
					subinstance.Unpause();
				}
			}
		}

		protected virtual ProfilingInstance FindOrCreateInstance(string name)
		{
			if (name == Name)
			{
				return this;
			}
			if (CurrentActiveSubInstance != null)
			{
				return CurrentActiveSubInstance.FindOrCreateInstance(name);
			}
			if (SubInstances.ContainsKey(name))
			{
				return SubInstances[name];
			}

			SubInstances[name] = new ProfilingInstance(name, this);
			return SubInstances[name];
		}

		public string CreatePrettyPrint(int depth = 0, double totalTime = 0d)
		{
			string result = "";
			if (depth == 0)
			{
				result = "Showing results for profiler " + Name + " (" + TimesStarted + ")\n";
				result += "Total time spent: " + ProfilingTime + " aka " + ProfilingTime.TotalMilliseconds + " ms\n";
			}
			else
			{
				string depthIndent = "";
				for (int i = 0; i < depth; i++)
				{
					depthIndent += "\t";
				}

				result = depthIndent + Name + "(" + (ProfilingTime.TotalMilliseconds / totalTime * 100).ToString("G3") + "%): " + ProfilingTime + " aka " + ProfilingTime.TotalMilliseconds + " ms\n";
			}

			List<ProfilingInstance> instances = SubInstances.Values.ToList();
			instances.Sort((a, b) => -1 * a.ProfilingTime.CompareTo(b.ProfilingTime));

			foreach (ProfilingInstance instance in instances)
			{
				double actualTotaltime = Math.Max(totalTime, ProfilingTime.TotalMilliseconds);
				result += instance.CreatePrettyPrint(depth + 1, actualTotaltime);
			}

			return result;
		}

		public override string ToString()
		{
			return CreatePrettyPrint();
		}
	}

	public class FlatProfilingInstance : ProfilingInstance
	{
		public FlatProfilingInstance(string name, ProfilingInstance parent = null) : base(name, parent) { }

		public override TimeSpan ProfilingTime { get { return Stopwatch.Elapsed; } }

		protected override ProfilingInstance FindOrCreateInstance(string name)
		{
			if (name == Name)
			{
				return this;
			}
			if (SubInstances.ContainsKey(name))
			{
				return SubInstances[name];
			}

			SubInstances[name] = new ProfilingInstance(name, this);
			return SubInstances[name];
		}
	}
}
