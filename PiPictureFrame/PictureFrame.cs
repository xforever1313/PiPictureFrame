
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

namespace PiPictureFrame.Core
{
    public class PictureFrame : IDisposable
    {
        // ---------------- Fields ----------------

        private bool isDisposed;

        private HttpServer server;

        private Action<string> loggingAction;

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpPort">The port to have the web-interface run on.</param>
        /// <param name="loggingAction">
        /// Action that is taken when the server wants to print something to some
        /// text-based console.  The string argument is the string we want to print.
        /// </param>
        public PictureFrame( short httpPort = 80, Action<string> loggingAction = null )
        {
            this.isDisposed = false;
            this.loggingAction = loggingAction;
            this.server = new HttpServer( httpPort );
            this.server.LoggingAction += this.loggingAction;
        }

        // ---------------- Properties ----------------

        // ---------------- Functions ----------------

        /// <summary>
        /// Runs the PictureFrame service.
        /// </summary>
        public void Run()
        {
            if( this.isDisposed )
            {
                throw new ObjectDisposedException( this.GetType().FullName );
            }

            this.server.Start();
            HttpServer.QuitReason quitReason = this.server.WaitForQuitEvent();

            // If disposed was called, quit RIGHT AWAY, the system may be shutting down,
            // and we don't want to delay that.
            if( isDisposed == false )
            {
                this.loggingAction?.Invoke( "Waiting 5 seconds before handling quit event..." );
                Thread.Sleep( 5 * 1000 );
                this.loggingAction?.Invoke( "Waiting 5 seconds before handling quit event...Done!" );
                this.HandleQuitReason( quitReason );
            }
        }

        /// <summary>
        /// Runs the PictureFrame service in a background thread.
        /// </summary>
        /// <returns>The task to run.</returns>
        public Task RunAsync()
        {
            return Task.Run( () => this.Run() );
        }

        /// <summary>
        /// Stops and disposes the PictureFrame service.
        /// </summary>
        public void Dispose()
        {
            this.isDisposed = true;
            this.server.LoggingAction -= this.loggingAction;
            this.server.Dispose();
        }

        /// <summary>
        /// Handles the quit event.
        /// </summary>
        /// <param name="reason">The reason why we are quitting.</param>
        private void HandleQuitReason( HttpServer.QuitReason reason )
        {
            this.loggingAction?.Invoke( "Reason for Quitting: " + reason );

            switch( reason )
            {
                case HttpServer.QuitReason.Disposed:
                    this.loggingAction?.Invoke( "Stop Service Called, no additional action." );
                    break;

                case HttpServer.QuitReason.ExitToDesktop:
                    break;

                case HttpServer.QuitReason.None:
                    this.loggingAction?.Invoke( "This should never happen..." );
                    break;

                case HttpServer.QuitReason.Restarting:
                    break;

                case HttpServer.QuitReason.ShuttingDown:
                    break;
            }
        }
    }
}
