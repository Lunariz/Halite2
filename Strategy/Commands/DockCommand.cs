using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	class DockCommand : Command
	{
		public Planet TargetPlanet;

		public DockCommand(Ship ship, Planet targetPlanet) : base(ship, CommandType.DockCommand)
		{
			this.TargetPlanet = targetPlanet;
		}

		public Planet GetDestination()
		{
			return TargetPlanet;
		}
	}
}