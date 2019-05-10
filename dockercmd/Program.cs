#region header

// dockercmd - Program.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2019.  All rights reserved.
// 
// Created: 2019-05-09 6:19 PM

#endregion

#region using

using System ;
using System.Collections.Generic ;
using System.Diagnostics ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Runtime.InteropServices ;
using System.Text ;

using JetBrains.Annotations ;

using Newtonsoft.Json ;

#endregion

namespace ArkaneSystems.DockerCmd
{
    internal static class Program
    {
        /// <summary>
        ///     Platform-specific name of the Docker executable.
        /// </summary>
        private static string DockerExecutable { get ; set ; }

        /// <summary>
        ///     Entry point.
        /// </summary>
        /// <param name="args">Command to execute.</param>
        /// <returns>Return code of the container; alternatively, error code from dockercmd (> 128).</returns>
        public static int Main ([NotNull] string[] args)
        {
            try
            {
                // Set Docker executable name by platform.
                Program.DockerExecutable = RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? "docker.exe" : "docker" ;

                // Check we have enough parameters.
                if (args.Length < 1)
                {
                    Console.WriteLine (@"error: insufficient arguments specified") ;
                    return 129 ;
                }

                // Display help if requested.
                if ((args.Length == 1) && (args[0] == "-h"))
                {
                    Console.WriteLine (@"usage: dockercmd <command> <arguments> : run a command based upon a configuration in ~/.dockercmd
       dockercmd -h                    : display this help message
       dockercmd -v                    : display the version") ;
                    return 0 ;
                }

                // Display version if requested.
                if ((args.Length == 1) && (args[0] == "-v"))
                {
                    Console.WriteLine ($"dockercmd {Assembly.GetEntryAssembly ().GetName ().Version}") ;
                    return 0 ;
                }

                // Split command line.
                string   cdFile    = Program.GetCommandDefinitionFile (args[0]) ;
                string[] arguments = args.Skip (1).ToArray () ;

                // Look for config file.
                if (!File.Exists (cdFile))
                {
                    Console.WriteLine ("error: could not find command definition file") ;
                    return 130 ;
                }

                // Read command definition.
                CommandDefinition cmd ;
                try
                {
                    var serializerSettings = new JsonSerializerSettings {MissingMemberHandling = MissingMemberHandling.Error} ;
                    cmd = JsonConvert.DeserializeObject <CommandDefinition> (File.ReadAllText (cdFile), serializerSettings) ;
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine ($"error: malformed command definition file\r\ndetail: {ex.Message}") ;
                    return 131 ;
                }


                // Check that we have an image.
                if (string.IsNullOrWhiteSpace (cmd.Image))
                {
                    Console.WriteLine ("error: no image specified in command definition file") ;
                    return 131 ;
                }

                // Adjust command definition as required.
                Program.PatchCommandDefinition (cmd) ;

                // Check if the requested image is present.
                if (!Program.CheckForImage (cmd.Image))
                {
                    Console.WriteLine ("command image is not present, attempting pull...") ;

                    int exitCode = Program.PullImage (cmd.Image) ;
                    if (exitCode != 0)
                    {
                        Console.WriteLine ("error: unable to pull command image") ;
                        return 132 ;
                    }
                }

                // Assemble docker run command parts.
                string cmdargs = Program.GetDockerRunParameters (cmd, arguments) ;


                int runExitCode = Program.RunContainer (cmdargs) ;
                if (runExitCode != 0)
                    return runExitCode ;

                // Declare success.
                return 0 ;
            }

            // Unanticipated error handler.
            catch (Exception e)
            {
                Console.WriteLine ($"unanticipated error: {e.Message}") ;
                return 255 ;
            }
        }

        /// <summary>
        ///     Locate the command definition file corresponding to a given command.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <returns>Path to the command definition file, whether or not it exists.</returns>
        private static string GetCommandDefinitionFile (string command)
        {
            // Get docker command config file directory.
            string commandPath =
                Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".dockercmd") ;

            string cdFile = Path.Combine (commandPath, $"{command}.json") ;
            return cdFile ;
        }

        /// <summary>
        ///     Patch the command definition before running it.
        /// </summary>
        /// <param name="cmd">The command definition to patch.</param>
        /// <remarks>
        ///     Fixes up image name with the default, if not explicitly specified and default set.
        ///     Sets name of container to default if not set; whether set or not, applies pid as suffix.
        /// </remarks>
        private static void PatchCommandDefinition (CommandDefinition cmd)
        {
            // Fix default repo if environment variable set.
            if (!cmd.Image.Contains ('/'))
            {
                // Get default docker image prefix.
                string imagePrefix = Environment.GetEnvironmentVariable ("DOCKER_REPO_PREFIX") ;

                if (!string.IsNullOrWhiteSpace (imagePrefix))
                    cmd.Image = string.Concat (imagePrefix, "/", cmd.Image) ;
            }

            if (string.IsNullOrWhiteSpace (cmd.Name))
            {
                string imageBase = cmd.Image.Split ('/').Last () ;
                cmd.Name = string.Concat ("command_", imageBase, "_", Process.GetCurrentProcess ().Id.ToString ()) ;
            }
            else
            {
                cmd.Name = string.Concat (cmd.Name, "_", Process.GetCurrentProcess ().Id.ToString ()) ;
            }
        }

        /// <summary>
        ///     Check if the given Docker image exists locally.
        /// </summary>
        /// <param name="image">The image to check for.</param>
        /// <returns>True if the image exists; false otherwise.</returns>
        private static bool CheckForImage (string image)
        {
            Process dp = Process.Start (new ProcessStartInfo
                                        {
                                            FileName               = "docker.exe",
                                            Arguments              = string.Concat ("images --format {{.Repository}} ", image),
                                            RedirectStandardOutput = true,
                                            CreateNoWindow         = true
                                        }) ;

            string[] output = dp.StandardOutput.ReadToEnd ().Split ("\n") ;

            dp.WaitForExit (10000) ;
            dp.Dispose () ;

            return output.Contains (image) ;
        }

        /// <summary>
        ///     Attempt to pull the specified Docker image.
        /// </summary>
        /// <param name="image">The image to pull.</param>
        /// <returns>Exit code from the "docker pull" command.</returns>
        /// <remarks>Will use existing credentials, if any.</remarks>
        private static int PullImage (string image)
        {
            Process dp = Process.Start (new ProcessStartInfo
                                        {
                                            FileName = Program.DockerExecutable, Arguments = string.Concat ("pull ", image)
                                        }) ;

            dp.WaitForExit () ;
            int exitCode = dp.ExitCode ;
            dp.Dispose () ;

            return exitCode ;
        }

        /// <summary>
        ///     Construct the appropriate arguments to "docker run", based on the command definition.
        /// </summary>
        /// <param name="cmd">The command definition to use.</param>
        /// <param name="arguments">Additional arguments supplied to the command.</param>
        /// <returns>A parameter string to be supplied to "docker run".</returns>
        private static string GetDockerRunParameters ([NotNull] CommandDefinition cmd, [NotNull] string[] arguments)
        {
            var parts = new List <string> (8) ;

            // name
            parts.Add ($"--name {cmd.Name}") ;

            // interactivity
            parts.Add (cmd.Interactive ? "-it" : "-d --init") ;

            // persistence
            if (!cmd.PersistContainer)
                parts.Add ("--rm") ;

            // port mapping
            if (cmd.PublishTcpPorts.Length > 0)
                foreach (int p in cmd.PublishTcpPorts)
                    parts.Add ($"-p {p}:{p}") ;

            // volumes
            if (!string.IsNullOrWhiteSpace (cmd.MountCwd))
            {
                string cwd = Environment.CurrentDirectory ;

                if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
                    cwd = cwd.Replace ('\\', '/') ;

                parts.Add ($"-v \"{cwd}:{cmd.MountCwd}\"") ;
            }

            // pids
            if (cmd.ShareHostPids)
                parts.Add ("--pid host") ;

            // Now build it.
            var buffer = new StringBuilder (256) ;

            foreach (string p in parts)
            {
                buffer.Append (p) ;
                buffer.Append (" ") ;
            }

            buffer.Append (cmd.Image) ;
            buffer.Append (" ") ;

            foreach (string a in arguments)
            {
                buffer.Append (a) ;
                buffer.Append (" ") ;
            }

            return buffer.ToString ().Trim () ;
        }

        /// <summary>
        ///     Runs a container given a "docker run" argument string.
        /// </summary>
        /// <param name="cmdstring">An argument string, as returned from <see cref="GetDockerRunParameters"/>.</param>
        /// <returns>Return code of the container.</returns>
        private static int RunContainer (string cmdstring)
        {
            Process dp = Process.Start (new ProcessStartInfo
                                        {
                                            FileName = Program.DockerExecutable, Arguments = string.Concat ("run ", cmdstring)
                                        }) ;

            dp.WaitForExit () ;
            int exitCode = dp.ExitCode ;
            dp.Dispose () ;

            return exitCode ;
        }
    }
}
