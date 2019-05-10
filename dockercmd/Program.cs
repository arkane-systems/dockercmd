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
using System.Text ;

using Newtonsoft.Json ;

#endregion

namespace ArkaneSystems.DockerCmd
{
    internal static class Program
    {
        public static int Main (string[] args)
        {
            try
            {
                // Check we have enough parameters.
                if (args.Length < 1)
                {
                    Console.WriteLine (@"error: insufficient arguments specified") ;
                    return 1 ;
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
                string   command   = args[0] ;
                string[] arguments = args.Skip (1).ToArray () ;

#if DEBUG
                Console.WriteLine ($"command: {command}; args: {arguments.Length}") ;
#endif

                // Get docker command config file directory.
                string commandPath =
                    Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".dockercmd") ;

#if DEBUG
                Console.WriteLine ($"Found command path: {commandPath}") ;
#endif

                string cdFile = Path.Combine (commandPath, $"{command}.json") ;

                // Look for config file.
                if (!File.Exists (cdFile))
                {
                    Console.WriteLine ("error: could not find command definition file") ;
                    return 2 ;
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
                    return 3 ;
                }

                // Adjust command definition as required.
                if (string.IsNullOrWhiteSpace (cmd.Image))
                {
                    Console.WriteLine ("error: no image specified in command definition file") ;
                    return 3 ;
                }

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
                    cmd.Name = string.Concat (Environment.UserName, "_command_", imageBase) ;
                }
                else
                {
                    cmd.Name = string.Concat (Environment.UserName, "_", cmd.Name) ;
                }

#if DEBUG
                Console.WriteLine ($"selected image: {cmd.Image}") ;
                Console.WriteLine ($"container name: {cmd.Name}") ;
                Console.WriteLine ($"interactive: {cmd.Interactive}") ;
                Console.WriteLine ($"persistContainer: {cmd.PersistContainer}") ;
#endif

                // Check if the requested image is present.
                if (!Program.CheckForImage (cmd.Image))
                {
                    Console.WriteLine ("command image is not present, attempting pull...") ;

                    var exitCode = Program.PullImage (cmd.Image) ;
                    if (exitCode != 0)
                    {
                        Console.WriteLine ("error: unable to pull command image") ;
                        return exitCode ;
                    }
                }

                // Assemble docker run command parts.
                var parts = new List <String> (8) ;

                parts.Add ($"--name {cmd.Name}");
                parts.Add (cmd.Interactive ? "-it" : "-d --init") ;

                if (!cmd.PersistContainer)
                    parts.Add ("--rm") ;

                StringBuilder cmdargsb = new StringBuilder (256) ;

                foreach (var p in parts)
                {
                    cmdargsb.Append (p) ;
                    cmdargsb.Append (" ") ;
                }

                cmdargsb.Append (cmd.Image) ;
                cmdargsb.Append (" ") ;

                foreach (var a in arguments)
                {
                    cmdargsb.Append (a) ;
                    cmdargsb.Append (" ") ;
                }

                var cmdargs = cmdargsb.ToString ().Trim () ;

#if DEBUG
                Console.WriteLine ($"docker run arguments: {cmdargs}");
#endif

                var runExitCode = Program.RunContainer (cmdargs) ;
                if (runExitCode != 0)
                {
                    return runExitCode ;
                }

                // Declare success.
                return 0 ;
            }
            catch (Exception e)
            {
                Console.WriteLine ($"unanticipated error: {e.Message}") ;
                return 128 ;
            }
        }

        private static bool CheckForImage (string image)
        {
            Process dp = Process.Start (new ProcessStartInfo
                                        {
                                            FileName = "docker.exe",
                                            Arguments = string.Concat ("images --format {{.Repository}} ", image),
                                            RedirectStandardOutput = true,
                                            CreateNoWindow = true
                                        }) ;

            string[] output = dp.StandardOutput.ReadToEnd ().Split ("\n") ;

            dp.WaitForExit (10000) ;
            dp.Dispose () ;

            return output.Contains (image) ;
        }

        private static int PullImage (string image)
        {
            Process dp = Process.Start (new ProcessStartInfo
                                        {
                                            FileName = "docker.exe",
                                            Arguments = string.Concat ("pull ", image)
                                        }) ;

            dp.WaitForExit ();
            var exitCode = dp.ExitCode ;
            dp.Dispose ();

            return exitCode ;
        }

        private static int RunContainer (string cmdstring)
        {
            Process dp = Process.Start (new ProcessStartInfo
                                        {
                                            FileName = "docker.exe",
                                            Arguments = string.Concat ("run ", cmdstring)
                                        }) ;

            dp.WaitForExit () ;
            var exitCode = dp.ExitCode ;
            dp.Dispose () ;

            return exitCode ;
        }
    }
}
