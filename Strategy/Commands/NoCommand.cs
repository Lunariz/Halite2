using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	class NoCommand : Command
	{
		public NoCommand(Ship ship) : base(ship, CommandType.NoCommand)
		{
		}
	}
}