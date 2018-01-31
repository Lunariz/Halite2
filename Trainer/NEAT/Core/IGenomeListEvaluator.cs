/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
using System.Collections.Generic;

namespace SharpNeat.Core
{
    /// <summary>
    /// Generic interface for evaluating a list of genomes. By operating on a list we allow concrete 
    /// implementations of this interface to choose between evaluating each genome independently of the others,
    /// perhaps across several execution threads, or in some collective evaluation scheme such as an artificial
    /// life/world scenario.
    /// </summary>
    public interface IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        /// Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        ulong EvaluationCount { get; }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the evolutionary algorithm search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        bool StopConditionSatisfied { get; }

        /// <summary>
        /// Evaluates a list of genomes.
        /// </summary>
        void Evaluate(IList<TGenome> genomeList);  

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        void Reset();
    }
}
