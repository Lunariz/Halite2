using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
	public class Command
	{
		public enum CommandType
		{
			NoCommand,
			DockCommand,
			UndockCommand,
			MoveCommand
		}

		public Ship Ship;
		public CommandType Type;

		public Command(Ship ship, CommandType type)
		{
			this.Ship = ship;
			this.Type = type;
		}
	}
}