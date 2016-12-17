
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.IO;
using SethCS.Extensions;
using PiPictureFrame.Core;
using System.Collections.Generic;

namespace PiPictureFrame.Cli
{
    public class Program
    {
        static int Main( string[] args )
        {
            List<string> argList = new List<string>( args );

            bool enableLogging = false;
            if( args.Length >= 1 )
            {
                if( ( argList.Contains( "--help" ) || argList.Contains( "/?" ) ) )
                {
                    PrintHelp();
                    return 0;
                }
                else if( argList.Contains( "--enable-logging" ) )
                {
                    enableLogging = true;
                }
            }

            if( enableLogging )
            {
                using(
                    FileStream outFile = new FileStream(
                        "PiFrame_" + DateTime.Now.ToFileNameString() + ".log",
                        FileMode.Create,
                        FileAccess.Write
                    )
                )
                {
                    using( StreamWriter writer = new StreamWriter( outFile ) )
                    {
                        Action<string> loggingAction =
                            delegate ( string s )
                            {
                                writer.WriteLine( DateTime.Now.ToTimeStampString() + "  " + s );
                                writer.Flush();
                                Console.WriteLine( s );
                                Console.Out.Flush();
                            };

                        RunFrame( loggingAction );
                    }
                }
            }
            else
            {
                Action<string> loggingAction =
                    delegate ( string s )
                    {
                        Console.WriteLine( s );
                        Console.Out.Flush();
                    };

                RunFrame( loggingAction );
            }

            return 0;
        }

        private static void RunFrame( Action<string> loggingAction )
        {
            using( PictureFrame frame = new PictureFrame() )
            {
                frame.Init( loggingAction );
                frame.Run();
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine( "Pi Picture Frame Control Command Line" );
            Console.WriteLine( "Usage: PiPictureFrame.Cli.exe [--help|/?]" );
            Console.WriteLine( "--help, /?\tPrint this message." );
        }
    }
}
