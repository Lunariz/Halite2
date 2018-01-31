﻿
namespace SharpNeat.Phenomes
{
    /// <summary>
    /// A MappingSignalArray that applies the bounds interval [0,1] to returned values.
    /// </summary>
    public class OutputMappingSignalArray : MappingSignalArray
    {
        #region Constructor

        /// <summary>
        /// Construct an OutputMappingSignalArray that wraps the provided wrappedArray.
        /// </summary>
        public OutputMappingSignalArray(double[] wrappedArray, int[] map) : base(wrappedArray, map)
        {
        }

        #endregion

        #region Indexer

        /// <summary>
        /// Gets the value at the specified index, applying the bounds interval [0,1] to the return value.
        /// </summary>
        /// <param name="index">The index of the value to retrieve.</param>
        /// <returns>A double.</returns>
        public override double this[int index] 
        { 
            get
            {
                // Apply bounds of [0,1].
                double y = base[index]; 
                if(y < 0.0) y = 0.0;
                else if(y > 1.0) y = 1.0;
                return y;
            }
        }

        #endregion
    }
}
