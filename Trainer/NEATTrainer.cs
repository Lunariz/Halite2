using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using SharpNeatGUI;

namespace Halite2
{
	public class NEATTrainer : Trainer
	{
		private static int s_poolSize = 4;
		private static int s_speciesCount = 1;
		public static int s_tournamentDuelRounds = 1;
		public static int s_tournamentGroupRounds = 1;
		private static bool s_saveProgress = true;
		private static bool s_showBestGenome = true;

		private List<NeatGenome> m_neatGenomePool = new List<NeatGenome>();
		private GenomeForm m_bestGenomeForm;
		private NeatEvolutionAlgorithm<NeatGenome> m_neatEvolutionAlgorithm;
		private NeatGenomeFactory m_neatGenomeFactory;

		private int m_currentGen = 0;

		public NEATTrainer(bool loadExisting = false, int loadedGen = 0)
		{
			NeatGenomeParameters neatGenomeParams = CreateCustomNeatGenomeParameters();
			neatGenomeParams.ConnectionMutationInfoList = CreateCustomMutationInfoList();
			m_neatGenomeFactory = new NeatGenomeFactory(NEATInput.InputCount, NEATInput.OutputCount, neatGenomeParams);

			if (loadExisting)
			{
				LoadAll();
				m_currentGen = loadedGen + 1;
			}
			else
			{
				Console.WriteLine("initializing at " + DateTime.Now);
				m_neatGenomePool = m_neatGenomeFactory.CreateGenomeList(s_poolSize, 0);
			}
		}

		public override void Train(int generations)
		{
			m_neatEvolutionAlgorithm = CreateEvolutionAlgorithm();

			Thread showGenomeFormThread = null;

			if (s_showBestGenome)
			{
				showGenomeFormThread = new Thread(ShowGenomeForm);
				showGenomeFormThread.Start();
			}

			for (; m_currentGen < generations; m_currentGen++)
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				Console.WriteLine("starting generation " + m_currentGen + " at " + DateTime.Now);

				m_neatEvolutionAlgorithm.PerformOneGeneration();

				stopwatch.Stop();

				Console.WriteLine("generation took: " + stopwatch.Elapsed);

				if (s_showBestGenome)
				{
					m_bestGenomeForm.Invoke(new Action(() => m_bestGenomeForm.RefreshView()));
				}

				if (s_saveProgress)
				{
					//Sample this gen
					NEATStrategyChooser player1 = new NEATStrategyChooser(m_neatEvolutionAlgorithm.CurrentChampGenome);
					NEATStrategyChooser player2 = new NEATStrategyChooser(m_neatGenomePool[StaticRandom.Rand(m_neatGenomePool.Count)]);
					PlayMatch(true, player1, player2);

					//Save the winner of this gen
					NEATStrategyChooser sample = new NEATStrategyChooser(m_neatEvolutionAlgorithm.CurrentChampGenome);
					CreateFile(sample, "gen" + m_currentGen);
				}

				SaveAll();
			}

			if (s_showBestGenome)
			{
				showGenomeFormThread.Abort();
			}
		}

		public void SaveAll()
		{
			for (int i = 0; i < m_neatGenomePool.Count; i++)
			{
				StrategyChooser chooser = new NEATStrategyChooser(m_neatGenomePool[i]);
				CreateFile(chooser, "currentGen" + i);
			}
		}

		public void LoadAll()
		{
			m_neatGenomePool.Clear();
			for (int i = 0; i < s_poolSize; i++)
			{
				m_neatGenomePool.Add((StrategyChooser.CreateFromFile(RunUtility.BotDir + "currentGen" + i + ".bot") as NEATStrategyChooser).Genome);
			}
		}

		private void ShowGenomeForm()
		{
			Thread.Sleep(1000);
			m_bestGenomeForm = new GenomeForm("Best Genome", m_neatEvolutionAlgorithm);
			m_bestGenomeForm.Show();
			m_bestGenomeForm.RefreshView();

			System.Windows.Forms.Application.Run();
		}

		private NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
		{
			// Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weight difference.
			IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
			ISpeciationStrategy<NeatGenome> speciationStrategy = new KMeansClusteringStrategy<NeatGenome>(distanceMetric);

			// Create complexity regulation strategy. 
			IComplexityRegulationStrategy complexityRegulationStrategy = new DefaultComplexityRegulationStrategy(ComplexityCeilingType.Absolute, 100);

			NeatEvolutionAlgorithmParameters _eaParams = CreateCustomEAParameters();

			// Create the evolution algorithm.
			NeatEvolutionAlgorithm<NeatGenome> ea = new NeatEvolutionAlgorithm<NeatGenome>(_eaParams, speciationStrategy, complexityRegulationStrategy);

			// Create a genome list evaluator. This will evaluate the entire list using the decoder
			IGenomeListEvaluator<NeatGenome> evaluator = new TournamentGenomeListEvaluator<NeatGenome, IBlackBox>();

			// Initialize the evolution algorithm.
			ea.Initialize(evaluator, m_neatGenomeFactory, m_neatGenomePool);

			// Finished. Return the evolution algorithm
			return ea;
		}

		private ConnectionMutationInfoList CreateCustomMutationInfoList()
		{
			ConnectionMutationInfoList list = new ConnectionMutationInfoList(6);

			//Jiggle values
			//large chance (60%) to jiggle a large amount of connections (30%) by a little amount (0.5)
			//medium chance (30%) to jiggle a medium amount of connections (10%) by a medium amount (1.5)
			//small chance (10%) to jiggle a small amount of connections (3.3%) by a large amount (3)
			list.Add(new ConnectionMutationInfo(0.5985, ConnectionPerturbanceType.JiggleUniform,
				ConnectionSelectionType.Proportional, 0.3d, 0, 0.5, 0.5));

			list.Add(new ConnectionMutationInfo(0.2985, ConnectionPerturbanceType.JiggleUniform,
				ConnectionSelectionType.Proportional, 0.1d, 0, 1.5, 1.5));

			list.Add(new ConnectionMutationInfo(0.0985, ConnectionPerturbanceType.JiggleUniform,
				ConnectionSelectionType.Proportional, 0.033d, 0, 3, 3));

			// Small chance to reset mutations. 1/5th, 1/4th and 1/3rd connections respectively.
			list.Add(new ConnectionMutationInfo(0.015, ConnectionPerturbanceType.Reset,
				ConnectionSelectionType.Proportional, 0.2d, 0, 0.0, 0));

			list.Add(new ConnectionMutationInfo(0.015, ConnectionPerturbanceType.Reset,
				ConnectionSelectionType.Proportional, 0.25d, 0, 0.0, 0));

			list.Add(new ConnectionMutationInfo(0.015, ConnectionPerturbanceType.Reset,
				ConnectionSelectionType.Proportional, 0.33d, 0, 0.0, 0));

			list.Initialize();
			return list;
		}

		private NeatGenomeParameters CreateCustomNeatGenomeParameters()
		{
			NeatGenomeParameters parameters = new NeatGenomeParameters();
			parameters.ConnectionWeightMutationProbability = 0.5d;
			parameters.AddNodeMutationProbability = 0.19d;
			parameters.AddConnectionMutationProbability = 0.3d;
			parameters.InitialInterconnectionsProportion = 0.1d;

			return parameters;
		}

		private NeatEvolutionAlgorithmParameters CreateCustomEAParameters()
		{
			NeatEvolutionAlgorithmParameters parameters = new NeatEvolutionAlgorithmParameters();
			parameters.SpecieCount = s_speciesCount;
			parameters.SelectionProportion = 0.5d;

			return parameters;
		}
	}

	public class TournamentGenomeListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
		where TGenome : class, IGenome<TGenome>
		where TPhenome : class
	{
		readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
		ulong _evalCount;

		public ulong EvaluationCount
		{
			get { return _evalCount; }
		}

		public bool StopConditionSatisfied
		{
			get { return false; }
		}

		public void Evaluate(IList<TGenome> genomeList)
		{
			Dictionary<TGenome, StrategyChooser> choosers = new Dictionary<TGenome, StrategyChooser>();
			foreach (TGenome genome in genomeList)
			{
				choosers[genome] = new NEATStrategyChooser(genome as NeatGenome);

				genome.EvaluationInfo.SetFitness(1);
			}

			TournamentResults results = TournamentUtility.RunTournament(choosers.Values.ToList(), NEATTrainer.s_tournamentDuelRounds, NEATTrainer.s_tournamentGroupRounds);

			foreach (var kvp in choosers)
			{
				kvp.Key.EvaluationInfo.SetFitness(results.AveragePointsPerGame[kvp.Value]);
			}
		}

		public void Reset()
		{
		}
	}
}