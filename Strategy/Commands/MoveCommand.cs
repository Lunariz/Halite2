using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    class MoveCommand : Command
    {
        //TODO: Overlap avoidance to increase target choice efficiency
        public PathingTarget Target;
	    public bool AvoidObstacles;

        public MoveCommand(Ship ship, PathingTarget target, bool avoidObstacles = true) : base(ship, CommandType.MoveCommand)
        {
            this.Target = target;
	        this.AvoidObstacles = avoidObstacles;
        }
    }
}
