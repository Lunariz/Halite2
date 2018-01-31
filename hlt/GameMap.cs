using System;
using System.Collections.Generic;
using System.Linq;

namespace Halite2.hlt
{
	public class GameMap
	{
		private int width, height;
		private int playerId;
		private List<Player> players;
		private IList<Player> playersUnmodifiable;
		private Dictionary<int, Planet> planets;
		private List<Ship> allShips;
		private IList<Ship> allShipsUnmodifiable;

		// used only during parsing to reduce memory allocations
		private List<Ship> currentShips = new List<Ship>();

		public static int Turn;
		public static bool IsTraining;
		public static bool IsTesting;
		public static bool IsLive;

		public GameMap(int width, int height, int playerId)
		{
			this.width = width;
			this.height = height;
			this.playerId = playerId;
			players = new List<Player>(Constants.MAX_PLAYERS);
			playersUnmodifiable = players.AsReadOnly();
			planets = new Dictionary<int, Planet>();
			allShips = new List<Ship>();
			allShipsUnmodifiable = allShips.AsReadOnly();
		}

		public int GetHeight()
		{
			return height;
		}

		public int GetWidth()
		{
			return width;
		}

		public Position GetCenter()
		{
			return new Position(width / 2, height / 2);
		}

		public int GetMyPlayerId()
		{
			return playerId;
		}

		public IList<Player> GetAllPlayers()
		{
			return playersUnmodifiable;
		}

		public Player GetMyPlayer
		{
			get { return playersUnmodifiable[GetMyPlayerId()]; }
		}

		public Ship GetShip(int playerId, int entityId)
		{
			return players[playerId].GetShip(entityId);
		}

		public Planet GetPlanet(int entityId)
		{
			return planets[entityId];
		}

		public List<Planet> GetAllPlanets()
		{
			return planets.Values.ToList();
		}

		public List<Ship> GetAllShips()
		{
			return allShipsUnmodifiable.ToList();
		}

		public List<Ship> GetAllShips(bool allied)
		{
			var ships = GetAllShips();
			var ID = GetMyPlayerId();

			ships = ships.Where(s => ((allied && s.GetOwner() == ID) || (!allied && s.GetOwner() != ID))).ToList();

			return ships;
		}

		public List<Ship> ShipsByDistance(Ship ship)
		{
			var ships = GetAllShips();
			ships.Remove(ship);
			ships.Sort((a, b) => ship.GetDistanceTo(a).CompareTo(ship.GetDistanceTo(b)));
			return ships;
		}

		public List<Ship> ShipsByDistance(Ship ship, bool allied)
		{
			var ships = GetAllShips(allied);
			ships.Remove(ship);
			ships.Sort((a, b) => ship.GetDistanceTo(a).CompareTo(ship.GetDistanceTo(b)));

			return ships;
		}

		public List<Planet> NearbyPlanetsByDistance(Entity entity)
		{
			var planets = GetAllPlanets();
			planets.Sort((a, b) => entity.GetDistanceTo(a).CompareTo(entity.GetDistanceTo(b)));

			return planets;
		}

		public List<Entity> ObjectsBetween(Position start, Position target)
		{
			List<Entity> entitiesFound = new List<Entity>();

			AddEntitiesBetween(entitiesFound, start, target, planets.Values.ToList<Entity>());
			AddEntitiesBetween(entitiesFound, start, target, allShips.ToList<Entity>());

			return entitiesFound;
		}

		private static void AddEntitiesBetween(List<Entity> entitiesFound,
			Position start, Position target,
			ICollection<Entity> entitiesToCheck)
		{
			foreach (Entity entity in entitiesToCheck)
			{
				if (entity.Equals(start) || entity.Equals(target))
				{
					continue;
				}
				if (Collision.segmentCircleIntersect(start, target, entity, Constants.FORECAST_FUDGE_FACTOR))
				{
					entitiesFound.Add(entity);
				}
			}
		}

		public List<Entity> NearbyEntitiesByDistance(Entity entity)
		{
			var entityByDistance = new List<Entity>();

			//Add all ships and planets
			entityByDistance.AddRange(planets.Values);
			entityByDistance.AddRange(allShips);
			entityByDistance.Remove(entity);

			//Remove entity itself
			entityByDistance.Sort((a, b) => entity.GetDistanceTo(a).CompareTo(entity.GetDistanceTo(b)));

			return entityByDistance;
		}

		public GameMap UpdateMap(Metadata mapMetadata)
		{
			int numberOfPlayers = MetadataParser.ParsePlayerNum(mapMetadata);

			players.Clear();
			planets.Clear();
			allShips.Clear();

			// update players info
			for (int i = 0; i < numberOfPlayers; ++i)
			{
				currentShips.Clear();
				Dictionary<int, Ship> currentPlayerShips = new Dictionary<int, Ship>();
				int playerId = MetadataParser.ParsePlayerId(mapMetadata);

				Player currentPlayer = new Player(playerId, currentPlayerShips);
				MetadataParser.PopulateShipList(currentShips, playerId, mapMetadata);
				allShips.AddRange(currentShips);

				foreach (Ship ship in currentShips)
				{
					currentPlayerShips[ship.GetId()] = ship;
				}
				players.Add(currentPlayer);
			}

			int numberOfPlanets = int.Parse(mapMetadata.Pop());

			for (int i = 0; i < numberOfPlanets; ++i)
			{
				List<int> dockedShips = new List<int>();
				Planet planet = MetadataParser.NewPlanetFromMetadata(dockedShips, mapMetadata);
				planets[planet.GetId()] = planet;
			}

			if (!mapMetadata.IsEmpty())
			{
				throw new InvalidOperationException("Failed to parse data from Halite game engine. Please contact maintainers.");
			}

			return this;
		}
	}
}