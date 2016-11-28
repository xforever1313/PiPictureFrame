
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PiPictureFrame.Core;
using static PiPictureFrame.Core.HttpServer;

namespace PiPictureFrame.Cli
{
    public class Program
    {
        static int Main( string[] args )
        {
            short port = HttpServer.DefaultPort;
            if( args.Length >= 1 )
            {
                if( ( args[0] == "--help" ) || ( args[0] == "/?" ) )
                {
                    PrintHelp();
                    return 0;
                }
                else if( short.TryParse( args[0], out port ) == false )
                {
                    Console.WriteLine( "Invalid port number: " + args[0] );
                    return 1;
                }
            }

            Action<string> loggingAction = ( s => Console.WriteLine( s ) );
            using( PictureFrame frame = new PictureFrame( port, loggingAction ) )
            {
                frame.Run();
            }

            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine( "Pi Picture Frame Control Command Line" );
            Console.WriteLine( "Usage: PiPictureFrame.Cli.exe [--help|port|/?]" );
            Console.WriteLine( "--help, /?\tPrint this message." );
            Console.WriteLine( "port\t\tPort to listen on.  Defaulted to 80 if none are specified." );
        }
    }
}
