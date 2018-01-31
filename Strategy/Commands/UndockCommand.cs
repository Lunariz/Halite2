using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	class UndockCommand : Command
	{
		public UndockCommand(Ship ship) : base(ship, CommandType.UndockCommand)
		{
		}
	}
}