using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Halite2.hlt;
using Halite2.Strategies;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace Halite2
{
	public class NEATStrategyChooser : StrategyChooser
	{
		public NeatGenome Genome;
		private IBlackBox m_box;
		private IGenomeDecoder<NeatGenome, IBlackBox> m_genomeDecoder =  new NeatGenomeDecoder(NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(1));
		
		public NEATStrategyChooser(NeatGenome genome)
		{
			Genome = genome;
			m_box = m_genomeDecoder.Decode(genome);
		}

		public override Strategy ChooseStrategy(Ship ship, GameMap gameMap)
		{
			Profiler.Start("NEAT Fill Input");

			ISignalArray inputArr = m_box.InputSignalArray;
			ISignalArray outputArr = m_box.OutputSignalArray;

			NEATInput input = new NEATInput(ship, gameMap);
			input.Fill(inputArr);
			
			Profiler.Stop("NEAT Fill Input");

			m_box.Activate();

			//Previously, we would assign an output for every strategy and find the highest value. But for choosing between two strategies, you only need one output.

			//int largestIndex = 0;
			//double largestIndexSize = double.MinValue;
			//for (int i = 0; i < NEATInput.OutputCount; i++)
			//{
			//	if (outputArr[i] > largestIndexSize)
			//	{
			//		largestIndex = i;
			//		largestIndexSize = outputArr[i];
			//	}
			//}

			//StrategyType strategy = (StrategyType) largestIndex;

			StrategyType strategy = outputArr[0] > 0.5f ? StrategyType.Attack : StrategyType.Defend;

			NEATInput.s_lastKnownStrategies[ship.GetId()] = strategy;

			return StrategyManager.CreateStrategy(strategy);
		}

		public override string ToString()
		{
			//This is somewhat hacky - ToString is used to create a bot file elsewhere, but the NEAT framework is only capable of writing it immediately
			//As a result, we first need to write the file, read it, then delete it, and return the contents so that it can be written again.

			string filename = TemporaryName + "tempNEAT.xml";

            XmlWriterSettings _xwSettings = new XmlWriterSettings();
            _xwSettings.Indent = true;

			using (XmlWriter xw = XmlWriter.Create(filename, _xwSettings))
			{
				NeatGenomeXmlIO.WriteComplete(xw, Genome, false);
			}

			string filecontents = File.ReadAllText(filename);
			filecontents = "NEAT\n" + filecontents;

			if (File.Exists(filename))
			{
				File.Delete(filename);
			}

			return filecontents;
		}

		public static NEATStrategyChooser CreateFromFile(string filePath)
		{
			//Remove the NEAT bot identifier on the first line before reading via the NEAT framework
			
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			string[] filecontents = File.ReadAllLines(filePath);
			for (int i = 1; i < filecontents.Length; i++)
			{
				writer.Write(filecontents[i] + "\n");
			}
			
			writer.Flush();
			stream.Position = 0;

			using(XmlReader xr = XmlReader.Create(stream)) 
            {
				NeatGenomeParameters neatGenomeParams = new NeatGenomeParameters();
				NeatGenomeFactory factory = new NeatGenomeFactory(NEATInput.InputCount, NEATInput.OutputCount, neatGenomeParams);
				NeatGenome genome = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, factory)[0];

				return new NEATStrategyChooser(genome);
            }
		}
	}

	
}
