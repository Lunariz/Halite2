using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2;

class Program
{
	public static void Main(string[] args)
	{
		RunUtility.CompileBot();

		Console.WriteLine("Welcome to Lunariz' Bot trainer");
		Console.WriteLine("For demo purposes, the trainer will now train a NEAT-based bot for 5 generations. This can take several minutes.");
		Console.WriteLine("-----------------");

		Trainer trainer = new NEATTrainer();
		trainer.Train(5);

		Console.WriteLine("Done!");

		Console.ReadLine();
	}
}