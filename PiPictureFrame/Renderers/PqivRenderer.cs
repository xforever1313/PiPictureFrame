
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Diagnostics;

namespace PiPictureFrame.Core.Renderers
{
    /// <summary>
    /// This uses renders on PQIV on a linux machine.
    /// 
    /// This class does a giant No-Op if on windows.
    /// 
    /// Technically pqiv CAN run on windows, but I don't really want to compile
    /// that right now...
    /// </summary>
    public class PqivRenderer : IRenderer
    {
        // -------- Fields --------

        private readonly bool isLinux;

        private const string pqivExeLocation = "/usr/bin/pqiv";

        private Action<string> loggingAction;

        private Process pqivProcess;

        // -------- Constructor --------

        /// <summary>
        /// Constructor.
        /// </summary>
        public PqivRenderer( Action<string> loggingAction )
        {
            this.isLinux = ( Environment.OSVersion.Platform == PlatformID.Unix );
            this.loggingAction = loggingAction;
            this.pqivProcess = null;
        }

        /// <summary>
        /// Inits the fbi renderer...
        /// It just makes sure it exists.
        /// </summary>
        public void Init()
        {
            if( this.isLinux )
            {
                this.FindExecutable( pqivExeLocation );
            }
            else
            {
                this.loggingAction.Invoke( "Windows Machine, can not run PQIV." );
            }
        }

        /// <summary>
        /// Disposes this class.
        /// Makes sure all child processes are killed.
        /// </summary>
        public void Dispose()
        {
            this.pqivProcess?.StandardInput.WriteLine( "quit()" );
            this.pqivProcess?.WaitForExit();
        }

        /// <summary>
        /// Adds the picture to PQIV.
        /// </summary>
        /// <param name="lastPicturePath">The path of the previous picture (in case you need to clean things up).</param>
        /// <param name="currentPicturePath">The path of the picture to show.</param>
        public void ShowPicture( string lastPicturePath, string currentPicturePath )
        {
            if( this.isLinux )
            {
                if( pqivProcess == null )
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.Arguments = "--fullscreen --hide-info-box --fade --scale-images-up --end-of-files-action=wait --actions-from-stdin \"" + currentPicturePath + "\"";
                    info.RedirectStandardInput = true;
                    info.UseShellExecute = false;
                    info.FileName = pqivExeLocation;

                    this.pqivProcess = new Process();
                    this.pqivProcess.StartInfo = info;
                    this.pqivProcess.Start();
                }
                else
                {
                    // Send commands to pqiv.

                    // First, add new picture to pqiv:
                    currentPicturePath.Replace( ")", @"\)" ); // Per pqiv's man page.  All ) must be escaped.
                    this.pqivProcess.StandardInput.WriteLine( "add_file(" + currentPicturePath + ")" );

                    // Next, go to the next picture.
                    this.pqivProcess.StandardInput.WriteLine( "goto_file_relative(1)" );

                    // Lastly, remove the previous photo.
                    lastPicturePath.Replace( ")", @"\)" );
                    this.pqivProcess.StandardInput.WriteLine( "remove_file_byname(" + lastPicturePath + ")" );
                }
            }
            else
            {
                this.loggingAction.Invoke( "Windows Machine, can not run PQIV." );
            }
        }

        private void FindExecutable( string path )
        {
            ProcessStartInfo info = new ProcessStartInfo( path, "--help" );

            using( Process process = new Process() )
            {
                process.StartInfo = info;
                if( process.Start() == false )
                {
                    throw new ApplicationException( "Could not start " + path );
                }

                process.WaitForExit();
                int exitCode = process.ExitCode;
                if( exitCode != 0 )
                {
                    throw new ApplicationException( "Trying to execute '" + path + " --help' failed." );
                }
            }
        }
    }
}
