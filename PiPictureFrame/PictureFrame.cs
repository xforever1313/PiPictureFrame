﻿
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace PiPictureFrame.Core
{
    public class PictureFrame : IDisposable
    {
        // ---------------- Fields ----------------

        private bool isDisposed;

        private HttpServer server;

        private Action<string> loggingAction;

        private PictureFrameConfig config;

        private static readonly string rootFolder;

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.
        /// </summary>
        public PictureFrame()
        {
            this.isDisposed = false;
        }

        /// <summary>
        /// Static Constructor.
        /// </summary>
        static PictureFrame()
        {
            rootFolder = Path.GetDirectoryName( UserConfigLocation );
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// The location of the XML user configuration for the pi picture frame.
        /// </summary>
        public static string UserConfigLocation
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ),
                    "PiPicFrame",
                    "UserConfig.xml"
                );
            }
        }

        // ---------------- Functions ----------------

        /// <summary>
        /// Inits this class.
        /// </summary>
        /// <param name="loggingAction">
        /// Action that is taken when the server wants to print something to some
        /// text-based console.  The string argument is the string we want to print.
        /// </param>
        public void Init( Action<string> loggingAction = null )
        {
            this.loggingAction = loggingAction;

            if( File.Exists( UserConfigLocation ) )
            {
                XmlDocument doc = new XmlDocument();
                doc.Load( UserConfigLocation );
                this.config = PictureFrameConfig.FromXml( doc.DocumentElement );
            }
            else
            {
                this.config = new PictureFrameConfig();
                this.SaveConfig();
            }

            this.server = new HttpServer( config.Port );
            this.server.LoggingAction += this.loggingAction;
        }

        /// <summary>
        /// Saves the configuration to the UserXml file.
        /// </summary>
        public void SaveConfig()
        {
            if( this.config == null )
            {
                throw new InvalidOperationException( "Config is null, call " + nameof( this.Init ) + " first" );
            }

            XmlDocument doc = new XmlDocument();

            // Create declaration.
            XmlDeclaration dec = doc.CreateXmlDeclaration( "1.0", "UTF-8", null );

            // Add declaration to document.
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore( dec, root );

            XmlElement configElement = doc.CreateElement( "pictureframeconfig" );
            doc.AppendChild( configElement );

            this.config.ToXml( configElement, doc );

            if( Directory.Exists( rootFolder ) == false )
            {
                Directory.CreateDirectory( rootFolder );
            }

            doc.Save( UserConfigLocation );
        }

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
