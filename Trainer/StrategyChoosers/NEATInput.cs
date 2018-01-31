using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;
using SharpNeat.Phenomes;

namespace Halite2
{
	public class NEATInput
	{
		public static Dictionary<int, StrategyType> s_lastKnownStrategies = new Dictionary<int, StrategyType>();

		private Ship m_ship;
		private GameMap m_gameMap;

		private static int s_rotationDivisions = 1;
		private static double[] s_bandDistances = new double[] {5, 25, 100, 1000};
		private static double[] s_shipDivisors = new double[] {20, 50, 200, 200}; //Approximately the maximum number of ships we expect in each band, so that we can normalize the amount of ships to 0..1

		public static int InputCount
		{
			get { return GenericInputs + AreaInputs; }
		}

		public static int OutputCount
		{
			get
			{
				return 1;

				//Previously, we would assign an output for every strategy and find the highest value. But for choosing between two strategies, you only need one output.
				//StrategyManager.StrategyCount;
			}
		}

		public NEATInput(Ship ship, GameMap gameMap)
		{
			m_ship = ship;
			m_gameMap = gameMap;
		}

		public void Fill(ISignalArray array)
		{
			int i = 0;
			foreach (double input in GetGenericInputs())
			{
				array[i] = input;
				i++;
			}
			foreach (double input in GetAreaInputs())
			{
				array[i] = input;
				i++;
			}
		}

		private static int GenericInputs
		{
			get { return 13; }
		}

		private List<double> GetGenericInputs()
		{
			List<double> inputs = new List<double>();

			inputs.Add(MinusNormalize((double) m_ship.GetHealth() / Constants.MAX_SHIP_HEALTH)); //Remaining health
			inputs.Add(m_ship.GetDockingStatus() == Ship.DockingStatus.Undocked ? -1 : 1); //Is this ship docked
			inputs.Add(MinusNormalize((m_gameMap.GetAllPlayers().Count(p => p.GetShips().Count > 0) - 2) / 2)); //Remaining playercount, -1 for 2, 0 for 3, 1 for 4
			inputs.Add(MinusNormalize(GameMap.Turn / 300d)); //Game progress
			inputs.Add(MinusNormalize((m_gameMap.GetHeight() - 160d) / (256d - 160d))); //Mapsize
			inputs.Add(MinusNormalize(m_gameMap.GetMyPlayer.GetShips().Count / m_gameMap.GetAllShips().Count)); //Generic turn-wide info about % of ships we own
			inputs.Add(MinusNormalize(m_gameMap.GetAllPlanets().Count(p => p.GetOwner() == m_gameMap.GetMyPlayerId()) / m_gameMap.GetAllPlanets().Count)); //Generic turn-wide info about % of planets we own
			inputs.Add(MinusNormalize(m_ship.GetDistanceTo(m_gameMap.ShipsByDistance(m_ship, false)[0]) / 100d)); //Approximate normalized distance to closest enemy ship
			inputs.Add(MinusNormalize(m_ship.GetDistanceTo(m_gameMap.NearbyPlanetsByDistance(m_ship)[0]) / 100d)); //Approximate normalized distance to closest planet
			inputs.Add(m_ship.GetId() == m_gameMap.GetMyPlayerId() * 3 ? 1 : 0); //Is player ship ID 0
			inputs.Add(m_ship.GetId() == m_gameMap.GetMyPlayerId() * 3 + 1 ? 1 : 0); //Is player ship ID 1
			inputs.Add(m_ship.GetId() == m_gameMap.GetMyPlayerId() * 3 + 2 ? 1 : 0); //Is player ship ID 2
			inputs.Add(s_lastKnownStrategies.ContainsKey(m_ship.GetId()) ? (InputArea.aggressiveStrategies.Contains(s_lastKnownStrategies[m_ship.GetId()]) ? 1 : -1) : 0); //Was this ship aggressive last turn, 1 for yes, -1 for no, 0 for new ships

			return inputs;
		}

		private static int AreaInputs
		{
			get { return s_rotationDivisions * s_bandDistances.Length * InputArea.InputCount; }
		}

		private List<double> GetAreaInputs()
		{
			List<InputArea> areas = new List<InputArea>();

			for (int i = 0; i < s_bandDistances.Length; i++)
			{
				for (int j = 0; j < s_rotationDivisions; j++)
				{
					areas.Add(new InputArea(s_shipDivisors[i]));
				}
			}

			foreach (Ship otherShip in m_gameMap.GetAllShips())
			{
				double angle = m_ship.OrientTowardsInDeg(otherShip);
				double distance = m_ship.GetDistanceTo(otherShip);

				InputArea area = GetArea(areas, angle, distance);

				area.AddShip(otherShip, m_gameMap);
			}

			foreach (Planet planet in m_gameMap.GetAllPlanets())
			{
				double angle = m_ship.OrientTowardsInDeg(planet);
				double distance = m_ship.GetDistanceTo(planet);

				InputArea area = GetArea(areas, angle, distance);

				area.AddPlanet(planet, m_gameMap);
			}

			List<double> inputs = new List<double>();

			//Done generating data, write it to one big list
			foreach (InputArea area in areas)
			{
				inputs.AddRange(area.GetData());
			}

			return inputs;
		}

		private InputArea GetArea(List<InputArea> areas, double angle, double distance)
		{
			for (int i = 0; i < s_bandDistances.Length; i++)
			{
				//When the next distance is too far, we have found the farthest area we can reach
				if (distance <= s_bandDistances[i])
				{
					int index = i * s_rotationDivisions;
					index += (int) Math.Min(angle / (360d / s_rotationDivisions), s_rotationDivisions - 1);

					return areas[index];
				}
			}

			return null;
		}

		private double MinusNormalize(double d)
		{
			//Turns a range of [0,1] into [-1,1]
			return (d * 2) - 1;
		}

		private class InputArea
		{
			public static StrategyType[] aggressiveStrategies = new[]
			{
				StrategyType.AttackDockedShips, StrategyType.AttackUndockedShips, StrategyType.CrashIntoPlanet,
				StrategyType.CrashIntoShip, StrategyType.Attack
			};

			public float DockedAllies,
				UndockedAllies,
				AggressiveAllies,
				DefensiveAllies,
				DockedEnemies,
				UndockedEnemies,
				AllyPlanets,
				EnemyPlanets,
				UnownedPlanets;

			public double ShipDivisor;

			public static int InputCount = 8;

			public InputArea(double shipDivisor)
			{
				this.ShipDivisor = shipDivisor;
			}

			public void AddShip(Ship ship, GameMap gameMap)
			{
				if (ship.GetOwner() == gameMap.GetMyPlayerId())
				{
					if (ship.GetDockingStatus() == Ship.DockingStatus.Undocked)
					{
						UndockedAllies++;
					}
					else
					{
						DockedAllies++;
					}

					if (s_lastKnownStrategies.ContainsKey(ship.GetId()))
					{
						if (aggressiveStrategies.Contains(s_lastKnownStrategies[ship.GetId()]))
						{
							AggressiveAllies++;
						}
						else
						{
							DefensiveAllies++;
						}
					}
				}
				else
				{
					if (ship.GetDockingStatus() == Ship.DockingStatus.Undocked)
					{
						UndockedEnemies++;
					}
					else
					{
						DockedEnemies++;
					}
				}
			}

			public void AddPlanet(Planet planet, GameMap gameMap)
			{
				if (!planet.IsOwned())
				{
					UnownedPlanets++;
				}
				else if (planet.GetOwner() == gameMap.GetMyPlayerId())
				{
					AllyPlanets++;
				}
				else
				{
					EnemyPlanets++;
				}
			}

			public List<double> GetData()
			{
				List<double> data = new List<double>();

				float totalShips = DockedAllies + UndockedAllies + DockedEnemies + UndockedEnemies;

				//For ships, we normalize the data by reporting percentages (report 0 in case of divide by zero)
				data.Add(totalShips / ShipDivisor); //Total amount of ships divided by maximum total amount (expected maximum)

				float alliedShips = (DockedAllies + UndockedAllies) / totalShips;
				data.Add(totalShips > 0 ? alliedShips : 0); //% of ships that is allied

				float dockedAllies = DockedAllies / (DockedAllies + UndockedAllies);
				data.Add((DockedAllies + UndockedAllies) > 0 ? dockedAllies : 0); //% of allies that is docked

				float dockedEnemies = DockedEnemies / (DockedEnemies + UndockedEnemies);
				data.Add((DockedEnemies + UndockedEnemies) > 0 ? dockedEnemies : 0); //% of enemies that is docked

				float aggessiveAllies = AggressiveAllies / (AggressiveAllies + DefensiveAllies);
				data.Add((AggressiveAllies + DefensiveAllies) > 0 ? aggessiveAllies : 0); //% of allies that is aggressive

				//For planets, we clamp the value at 1 - reporting multiple planets is not important
				data.Add(Math.Min(1, AllyPlanets));
				data.Add(Math.Min(1, EnemyPlanets));
				data.Add(Math.Min(1, UnownedPlanets));

				return data;
			}
		}
	}
}