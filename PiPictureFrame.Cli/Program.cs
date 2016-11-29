
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using PiPictureFrame.Core;

namespace PiPictureFrame.Cli
{
    public class Program
    {
        static int Main( string[] args )
        {
            if( args.Length >= 1 )
            {
                if( ( args[0] == "--help" ) || ( args[0] == "/?" ) )
                {
                    PrintHelp();
                    return 0;
                }
            }

            Action<string> loggingAction = ( s => Console.WriteLine( s ) );
            using( PictureFrame frame = new PictureFrame() )
            {
                frame.Init( loggingAction );
                frame.Run();
            }

            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine( "Pi Picture Frame Control Command Line" );
            Console.WriteLine( "Usage: PiPictureFrame.Cli.exe [--help|/?]" );
            Console.WriteLine( "--help, /?\tPrint this message." );
        }
    }
}
