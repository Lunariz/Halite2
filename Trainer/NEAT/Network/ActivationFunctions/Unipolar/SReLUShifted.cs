﻿/* ***************************************************************************
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
using Redzen.Numerics;

namespace SharpNeat.Network
{
    /// <summary>
    /// S-shaped rectified linear activation unit (SReLU). Shifted on the x-axis so that x=0 gives y=0.5, in keeping with the logistic sigmoid.
    /// From:
    ///    https://en.wikipedia.org/wiki/Activation_function
    ///    https://arxiv.org/abs/1512.07030 [Deep Learning with S-shaped Rectified Linear Activation Units]
    ///    
    /// </summary>
    public class SReLUShifted : IActivationFunction
    {
        /// <summary>
        /// Default instance provided as a public static field.
        /// </summary>
        public static readonly IActivationFunction __DefaultInstance = new SReLUShifted();

        public string FunctionId
	    {
		    get { return this.GetType().Name; }
	    }

	    public string FunctionString { get { return ""; } }

        public string FunctionDescription { get { return "Leaky Rectified Linear Unit (ReLU)"; } }

        public bool AcceptsAuxArgs { get { return false; } }

        public double Calculate(double x, double[] auxArgs)
        {
            const double tl = 0.001; // threshold (left).
            const double tr = 0.999; // threshold (right).
            const double a = 0.00001;

            x+=0.5;

            double y;
            if(x > tl && x < tr) {
                y = x;
            }
            else if(x <= tl) {
                y = tl + (x - tl) * a;
            }
            else {
                y = tr + (x - tr) * a;
            }

            return y;
        }

        public float Calculate(float x, float[] auxArgs)
        {
            float tl = 0.001f; // threshold (left).
            float tr = 0.999f; // threshold (right).
            float a = 0.00001f;

            x += 0.5f;

            float y;
            if(x > tl && x < tr) {
                y = x;
            }
            else if(x <= tl) {
                y = tl + (x - tl) * a;
            }
            else {
                y = tr + (x - tr) * a;
            }

            return y;
        }

        public double[] GetRandomAuxArgs(XorShiftRandom rng, double connectionWeightRange)
        {
            throw new SharpNeatException("GetRandomAuxArgs() called on activation function that does not use auxiliary arguments.");
        }

        public void MutateAuxArgs(double[] auxArgs, XorShiftRandom rng, ZigguratGaussianSampler gaussianSampler, double connectionWeightRange)
        {
            throw new SharpNeatException("MutateAuxArgs() called on activation function that does not use auxiliary arguments.");
        }
    }
}
