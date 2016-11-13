
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PiPictureFrame.Core
{
    public class HttpServer : IDisposable
    {
        // ---------------- Fields ----------------

        /// <summary>
        /// Default port to listen on.
        /// </summary>
        public const short DefaultPort = 80;

        /// <summary>
        /// Reference to http listener.
        /// </summary>
        private readonly HttpListener listener;

        /// <summary>
        /// Thread that does the listening.
        /// </summary>
        private Thread listeningThread;

        private bool isListening;

        private object isListeningLock;

        private readonly ManualResetEvent quitEvent;

        // ---------------- Events ----------------

        /// <summary>
        /// Action that is taken when the server wants to print something to some
        /// text-based console.  The string argument is the string we want to print.
        /// </summary>
        public Action<string> LoggingAction;

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        public HttpServer( short port = DefaultPort )
        {
            if( HttpListener.IsSupported == false )
            {
                throw new PlatformNotSupportedException(
                    "This platform does not support HTTP Listeners..."
                );
            }

            this.quitEvent = new ManualResetEvent( false );

            this.isListeningLock = new object();
            this.IsListening = false;

            this.listener = new HttpListener();

            this.listener.Prefixes.Add( "http://*:" + port + "/" );

            this.listeningThread = new Thread( () => this.HandleRequest() );
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// Whether or not we are listening.
        /// </summary>
        public bool IsListening
        {
            get
            {
                lock( this.isListeningLock )
                {
                    return this.isListening;
                }
            }
            private set
            {
                lock( this.isListeningLock )
                {
                    this.isListening = value;
                }
            }
        }

        // ---------------- Functions ----------------

        /// <summary>
        /// Opens the websocket and listens for requests.
        /// No-op if already listening.
        /// </summary>
        public void Start()
        {
            // No-op if we are not listening.
            if( this.IsListening == false )
            {
                this.listener.Start();
                this.listeningThread.Start();
                this.IsListening = true;
                this.LoggingAction?.Invoke( "Server Running!" );
            }
        }

        /// <summary>
        /// Closes the web socket and disposes this class.
        /// No-op if not already listening.
        /// </summary>
        public void Dispose()
        {
            // No-op if we are not listening.
            if( this.IsListening )
            {
                this.LoggingAction?.Invoke( "Terminating server..." );
                this.IsListening = false;
                this.listener.Stop();
                this.listeningThread.Join();

                this.listener.Close();

                this.LoggingAction?.Invoke( "Terminating server...Done!" );

                this.quitEvent.Set();
            }
        }

        /// <summary>
        /// Blocks the calling thread(s) until the quit event is triggered.
        /// </summary>
        public void WaitForQuitEvent()
        {
            this.quitEvent.WaitOne();
        }

        private void HandleRequest()
        {
            while( this.IsListening )
            {
                HttpListenerContext context = null;
                try
                {
                    context = listener.GetContext();
                }
                catch( HttpListenerException err )
                {
                    // Error code 995 means GetContext got aborted (E.g. when shutting down).
                    // If that's the case, just start over.  The while loop will break out and
                    // the thread will exit cleanly.
                    if( err.ErrorCode == 995 )
                    {
                        this.LoggingAction?.Invoke( "Server got terminated, shutting down..." );
                        continue;
                    }
                    else
                    {
                        this.LoggingAction?.Invoke( "FATAL ERROR:" + err.ToString() );
                        throw;
                    }
                }

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = string.Empty;
                try
                {
                    // Construct Response.
                    // Taken from https://msdn.microsoft.com/en-us/library/system.net.httplistener.begingetcontext%28v=vs.110%29.aspx
                    string url = request.RawUrl.ToLower();
                    if( ( url == "/" ) || ( url == "/index.html" ) )
                    {
                        //TODO: add index.html logic.
                    }
                    else
                    {
                        responseString = Get404Html();
                        response.StatusCode = Convert.ToInt32( HttpStatusCode.NotFound );
                    }
                }
                catch( Exception e )
                {
                    responseString = GetErrorHtml( e );
                    response.StatusCode = Convert.ToInt32( HttpStatusCode.InternalServerError );

                    this.LoggingAction?.Invoke( "**********" );
                    this.LoggingAction?.Invoke( "Caught Exception when determining response: " + e.Message );
                    this.LoggingAction?.Invoke( e.StackTrace );
                    this.LoggingAction?.Invoke( "**********" );
                }
                finally
                {
                    try
                    {
                        // Only send response if our string is not empty
                        // (Possible for an image, ExportToXml or ExportJson got called and didn't
                        // add the response string).
                        if( responseString.Length > 0 )
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes( responseString );
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write( buffer, 0, buffer.Length );
                        }
                    }
                    catch( Exception e )
                    {
                        this.LoggingAction?.Invoke( "**********" );
                        this.LoggingAction?.Invoke( "Caught Exception when writing response: " + e.Message );
                        this.LoggingAction?.Invoke( e.StackTrace );
                        this.LoggingAction?.Invoke( "**********" );
                    }
                    response.OutputStream.Close();
                }

                this.LoggingAction?.Invoke(
                    request.HttpMethod + " from: " + request.UserHostName + " " + request.UserHostAddress + " " + request.RawUrl + " (" + response.StatusCode + ")"
                );
            }
        }

        // ---- HTML Functions ----

        /// <summary>
        /// Gets the internal server error html.
        /// </summary>
        /// <param name="e">Exception caught.</param>
        /// <returns>The internal server error html.</returns>
        private static string GetErrorHtml( Exception e )
        {
            string html =
 @"<!DOCTYPE html>
<html>
<head>
    <title>Pi Picture Frame Control</title>
    <meta http-equiv=""content-type"" content=""text/html; charset = utf-8"" />
</head>
<body>
    <h1>500: Internal System Error</h1>
    <h2>Error:</h2>
";
            using( StringReader reader = new StringReader( e.Message ) )
            {
                string line;
                while( ( line = reader.ReadLine() ) != null )
                {
                    html += "<p>" + line + "</p>" + Environment.NewLine;
                }
            }

            html += "<h2>Stack Trace:</h2>" + Environment.NewLine;

            using( StringReader reader = new StringReader( e.StackTrace ) )
            {
                string line;
                while( ( line = reader.ReadLine() ) != null )
                {
                    html += "<p>" + line + "</p>" + Environment.NewLine;
                }
            }

            html += @"
</body>
</html>
";
            return html;
        }

        /// <summary>
        /// Gets the 404 html.
        /// </summary>
        /// <returns>HTML for the 404 page.</returns>
        private static string Get404Html()
        {
            return
@"<!doctype html>
<head>
    <meta http-equiv=""content-type"" content=""text/html; charset=utf-8"" />
    <title>Pi Picture Frame Control.  Not Found.</title>
    <style>
        body
        {
            background-color:ffffff
        }
    </style>
</head>
<body>

        <h1>404 Not Found</h1>

</body>
</html>
";
        }
    }
}
