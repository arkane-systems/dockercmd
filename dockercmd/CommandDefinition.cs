#region header

// dockercmd - CommandDefinition.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2019.  All rights reserved.
// 
// Created: 2019-05-09 7:05 PM

#endregion

#region using

using JetBrains.Annotations ;

#endregion

namespace ArkaneSystems.DockerCmd
{
    /// <summary>
    ///     Definition of a Docker command.
    /// </summary>
    /// <remarks>
    ///     Used to deserialize the .json command definition file.
    /// </remarks>
    [UsedImplicitly]
    internal class CommandDefinition
    {
        /// <summary>
        ///     Name of the Docker image containing the command.
        /// </summary>
        public string Image { get ; set ; }

        /// <summary>
        ///     Name to use for the container executing the command.
        /// </summary>
        public string Name { get ; set ; }

        /// <summary>
        ///     Is the command/container interactive?
        /// </summary>
        public bool Interactive { get ; set ; } = true ;

        /// <summary>
        ///     Volume to mount current working directory on.
        /// </summary>
        public string MountCwd { get ; set ; }

        /// <summary>
        ///     Should the container persist after the command is complete?
        /// </summary>
        public bool PersistContainer { get ; set ; } = false ;

        /// <summary>
        ///     A list of TCP ports to publish on the host.
        /// </summary>
        public int[] PublishTcpPorts { get ; set ; } = new int[0] ;

        /// <summary>
        ///     Should the container use the same pid namespace as the host?
        /// </summary>
        public bool ShareHostPids { get ; set ; } = false ;
    }
}
