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

namespace SharpNeat.Network
{
    /// <summary>
    /// Abstracted representation of a network definition.
    /// This interface and the related types INodeList, IConnectionList, INetworkNode,
    /// INetworkConnection, etc, allow networks to be described abstractly. 
    /// 
    /// One significant use of this class is in the decoding of genome classes into concrete 
    /// network instances; The decode methods can be written to operate on INetworkDefinition
    /// rather than specific genome types.
    /// </summary>
    public interface INetworkDefinition
    {
        /// <summary>
        /// Gets the number of input nodes. This does not include the bias node which is always present.
        /// </summary>
        int InputNodeCount { get; }
        /// <summary>
        /// Gets the number of output nodes.
        /// </summary>
        int OutputNodeCount { get; }
        /// <summary>
        /// Gets a bool flag that indicates if the network is acyclic.
        /// </summary>
        bool IsAcyclic { get; }
        /// <summary>
        /// Gets the network's activation function library. The activation function at each node is 
        /// represented by an integer ID, which refers to a function in this activation function library.
        /// </summary>
        IActivationFunctionLibrary ActivationFnLibrary { get; }
        /// <summary>
        /// Gets the list of network nodes.
        /// </summary>
        INodeList NodeList { get; }
        /// <summary>
        /// Gets the list of network connections.
        /// </summary>
        IConnectionList ConnectionList { get; }
        /// <summary>
        /// Gets NetworkConnectivityData for the network.
        /// </summary>
        NetworkConnectivityData GetConnectivityData();
    }
}
