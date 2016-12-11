
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PiPictureFrame.Core
{
    /// <summary>
    /// Runs the HTTP Server class.
    /// Note, the user may request to shutdown the frame from the web-interface.
    /// This class does not handle that.  After calling Start() on this class,
    /// call WaitForQuitEvent().  This will return if the user wants to exit to the desktop,
    /// Restart the system, or shutdown the system.  It is then up to the caller to
    /// call Dispose on this class, and then handle what the user wishes for the system to do
    /// based on the returned QuitReason from WaitForQuitEvent.
    /// 
    /// Recommended to add a delay between calling WaitForQuitEvent and Dispose so CSS and JS will
    /// be retrieved before shutting down the server.
    /// </summary>
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
        /// The picture API.
        /// </summary>
        private readonly PictureFrame picFrame;

        /// <summary>
        /// Why the server quit.
        /// </summary>
        private QuitReason quitReason;

        private object quitReasonLock;

        /// <summary>
        /// Thread that does the listening.
        /// </summary>
        private Thread listeningThread;

        private bool isListening;

        private object isListeningLock;

        private readonly ManualResetEvent quitEvent;

        /// <summary>
        /// Options for hour drop-downs.
        /// </summary>
        private static List<int> hourOptions;

        /// <summary>
        /// Options for minute drop-downs.
        /// </summary>
        private static List<int> minuteOptions;

        /// <summary>
        /// Options for changing pictures in minutes.
        /// </summary>
        private static List<int> picChangeIntervalOptions;

        /// <summary>
        /// Options for changing pictures in hours.
        /// </summary>
        private static List<int> picRefreshIntervalOptions;

        // ---------------- Enums ----------------

        /// <summary>
        /// The reason why the server quit.
        /// </summary>
        public enum QuitReason
        {
            /// <summary>
            /// Nothing, the system is still running.
            /// </summary>
            None,

            /// <summary>
            /// Dispose method was called on the class.
            /// </summary>
            Disposed,

            /// <summary>
            /// The user wants the system to reboot.
            /// </summary>
            Restarting,

            /// <summary>
            /// The user wants the system to shutdown.
            /// </summary>
            ShuttingDown,

            /// <summary>
            /// The user wants the system to exit to the desktop.
            /// </summary>
            ExitToDesktop,

            /// <summary>
            /// The thread loop aborted fatally.
            /// Caller should not abort the picture frame.  User will need to kill the task
            /// to kill the frame.
            /// </summary>
            FatalError
        }

        // ---------------- Events ----------------

        /// <summary>
        /// Action that is taken when the server wants to print something to some
        /// text-based console.  The string argument is the string we want to print.
        /// </summary>
        public event Action<string> LoggingAction;

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="screen">The picFrame API.</param>
        /// <param name="port">The port to listen on.</param>
        public HttpServer( PictureFrame picFrame, short port = DefaultPort )
        {
            if( HttpListener.IsSupported == false )
            {
                throw new PlatformNotSupportedException(
                    "This platform does not support HTTP Listeners..."
                );
            }

            this.quitReason = QuitReason.None;
            this.quitReasonLock = new object();
            this.quitEvent = new ManualResetEvent( false );

            this.isListeningLock = new object();
            this.IsListening = false;

            this.picFrame = picFrame;

            this.listener = new HttpListener();

            this.listener.Prefixes.Add( "http://*:" + port + "/" );

            this.listeningThread = new Thread( () => this.HandleRequest() );
        }

        /// <summary>
        /// Static Constructor.
        /// </summary>
        static HttpServer()
        {
            hourOptions = new List<int>();
            for( int hour = 0; hour <= 23; ++hour )
            {
                hourOptions.Add( hour );
            }

            minuteOptions = new List<int>();
            for( int minute = 0; minute <= 59; ++minute )
            {
                minuteOptions.Add( minute );
            }

            picChangeIntervalOptions = new List<int>()
            {
                1, 2, 3, 4, 5, 10, 15, 20, 30, 45, 60
            };

            picRefreshIntervalOptions = new List<int>()
            {
                1, 2, 3, 4, 5, 0
            };
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// The reason why the server quit.
        /// Set to None while running.
        /// 
        /// Thread-safe.
        /// </summary>
        public QuitReason ExitReason
        {
            get
            {
                lock( this.quitReasonLock )
                {
                    return this.quitReason;
                }
            }
            private set
            {
                lock( this.quitReasonLock )
                {
                    this.quitReason = value;
                }
            }
        }

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
            // Only set the quit reason if we are currently running.
            // Otherwise, we quit for a different reason, so don't change it.
            if( this.quitReason == QuitReason.None )
            {
                this.quitReason = QuitReason.Disposed;
            }
            this.StopServer();
        }

        /// <summary>
        /// Blocks the calling thread(s) until the quit event is triggered.
        /// </summary>
        /// <returns>
        /// The reason why the user wishes to quit.   It is up to the caller of this class to
        /// call Dispose, and to handle what the user wants to do based on the QuitReason.
        /// </returns>
        public QuitReason WaitForQuitEvent()
        {
            this.quitEvent.WaitOne();
            return this.quitReason;
        }

        private void StopServer()
        {
            // No-op if we are not listening.
            if( this.IsListening )
            {
                this.LoggingAction?.Invoke( "Terminating server due to reason " + this.quitReason + "..." );
                this.IsListening = false;
                this.listener.Stop();
                this.listeningThread.Join();

                this.listener.Close();

                this.LoggingAction?.Invoke( "Terminating server...Done!" );

                this.quitEvent.Set();
            }
        }

        private void HandleRequest()
        {
            // There are bunch of exceptions that can happen here.
            // 1.  Client closes connection.  This will cause our response's stream to close.
            //     We may see errors such as "The specified network name is no longer available"
            //     or "The I/O operation has been aborted because of either a thread exit or an application request".
            //     NEITHER or these should cause the program to crash.  Simply grab the next connection and move on.
            // 2.  ObjectDisposeExceptions can happen if the above happens as well.  We'll handle those when needed.

            try
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
                            this.LoggingAction?.Invoke( "FATAL ERROR (" + err.ErrorCode + "): " + err.ToString() );
                            throw;
                        }
                    }

                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    try
                    {
                        string responseString = string.Empty;
                        try
                        {
                            responseString = HandleRequest( request, response );
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
                    } // End request/response try{}
                    catch( Exception err )
                    {
                        this.LoggingAction?.Invoke( "**********" );
                        this.LoggingAction?.Invoke( "Caught exception when handling resposne: " + err.Message );
                        this.LoggingAction?.Invoke( "This can happen for several expected reasons.  We're okay!" );
                        this.LoggingAction?.Invoke( err.StackTrace );
                        this.LoggingAction?.Invoke( "**********" );
                    }
                } // End while IsListening loop.
            }
            catch( Exception e )
            {
                this.LoggingAction?.Invoke( "**********" );
                this.LoggingAction?.Invoke( "FATAL Exception in HTTP Listener.  Aborting web server, but the picframe will still run: " + e.Message );
                this.LoggingAction?.Invoke( e.StackTrace );
                this.LoggingAction?.Invoke( "**********" );
                this.quitReason = QuitReason.FatalError;
            }

            // Our thread is exiting, notify all threads waiting on it.
            this.quitEvent.Set();
        }

        private string HandleRequest( HttpListenerRequest request, HttpListenerResponse response )
        {
            string responseString = string.Empty;

            // Construct Response.
            // Taken from https://msdn.microsoft.com/en-us/library/system.net.httplistener.begingetcontext%28v=vs.110%29.aspx
            string url = request.RawUrl.ToLower();
            if( ( url == "/" ) || ( url == "/index.html" ) )
            {
                responseString = GetIndexHtml();
            }
            else if( url == "/settings.html" )
            {
                responseString = GetSettingsHtml( string.Empty );
            }
            else if( url == "/turnoff.html" )
            {
                responseString = GetTurnOffHtml( string.Empty );
            }
            else if (url == "/space.html")
            {
                responseString = GetSpaceHtml();
            }
            else if( url == "/about.html" )
            {
                responseString = GetAboutHtml();
            }
            else if( url == "/sleep.html" )
            {
                responseString = HandleSleepRequest( request.HttpMethod );
            }
            else if( url == "/linux.html" )
            {
                responseString = HandleExitToDesktopRequest( request.HttpMethod );
            }
            else if( url == "/restart.html" )
            {
                responseString = HandleRestartSystemRequest( request.HttpMethod );
            }
            else if( url == "/shutdown.html" )
            {
                responseString = HandleShutdownSystemRequest( request.HttpMethod );
            }
            else if( url == "/current.jpg" )
            {
                this.HandleGetCurrentPictureRequest( response );
            }
            else if( url == "/changepicture.html" )
            {
                responseString = this.HandleChangePictureRequest( request.HttpMethod );
            }
            else if( url == "/full.html" )
            {
                responseString = ReadFile( Path.Combine( "html", "full.html" ) );
            }
            else if( url == "/license.txt" )
            {
                responseString = License.BoostLicense;
            }
            else if( url == "/credits.txt" )
            {
                responseString = License.Credits;
            }
            else if( url.EndsWith( ".css" ) || url.EndsWith( ".js" ) )
            {
                responseString = GetJsOrCssFile( url );
                if( string.IsNullOrEmpty( responseString ) )
                {
                    responseString = Get404Html();
                    response.StatusCode = Convert.ToInt32( HttpStatusCode.NotFound );
                }
            }
            else
            {
                responseString = Get404Html();
                response.StatusCode = Convert.ToInt32( HttpStatusCode.NotFound );
            }

            return responseString;
        }

        // ---- HTML Functions ----

        /// <summary>
        /// Reads the given file to the end.
        /// Returns string.Empty if the file does not exist.
        /// </summary>
        /// <param name="path">The path to read.</param>
        /// <returns>The string of the file.</returns>
        private static string ReadFile( string path )
        {
            string fileConents = string.Empty;
            if( File.Exists( path ) )
            {
                using( StreamReader inFile = new StreamReader( path ) )
                {
                    fileConents = inFile.ReadToEnd();
                }
            }

            return fileConents;
        }

        private static Regex cssPattern = new Regex( @"/(?<jsOrCss>(js|css))/(?<pure>pure/)?(?<file>[\w-\d]+\.(css|js))", RegexOptions.Compiled );

        /// <summary>
        /// Reads in the given JS or CSS file.
        /// </summary>
        /// <param name="url">The URL to grab.</param>
        /// <returns>The CSS or JS contents.</returns>
        private static string GetJsOrCssFile( string url )
        {
            string filePath;
            Match cssMatch = cssPattern.Match( url );
            if( cssMatch.Success )
            {
                filePath = Path.Combine(
                    "html",
                    cssMatch.Groups["jsOrCss"].Value,
                    cssMatch.Groups["pure"].Value,
                    cssMatch.Groups["file"].Value
                );
            }
            else
            {
                return string.Empty;
            }

            return ReadFile( filePath );
        }

        /// <summary>
        /// Gets the home page's html
        /// </summary>
        /// <returns>The home page's html.</returns>
        private static string GetIndexHtml()
        {
            string html = ReadFile( Path.Combine( "html", "index.html" ) );
            html = AddCommonHtml( html );

            return html;
        }

        /// <summary>
        /// Gets the setting page's html.
        /// </summary>
        /// <returns>The setting page's html.</returns>
        private static string GetSettingsHtml( string errorMessage )
        {
            string html = ReadFile( Path.Combine( "html", "settings.html" ) );
            html = AddCommonHtml( html );
            html = AddSettingsHtml( html );
            html = html.Replace( "{%ErrorMessage%}", errorMessage );

            return html;
        }

        /// <summary>
        /// Gets the turn-off page's html.
        /// </summary>
        /// <param name="message">Message to tell the user.</param>
        /// <returns>The turn-off page's html.</returns>
        private string GetTurnOffHtml( string message )
        {
            string html = ReadFile( Path.Combine( "html", "turnoff.html" ) );
            html = html.Replace( "{%Message%}", message );
            html = AddCommonHtml( html );

            string onOrOff = this.picFrame.Screen.IsOn ? "Off" : "On";
            html = html.Replace( "{%OnOrOff%}", onOrOff );

            return html;
        }

        /// <summary>
        /// Gets the space-remaining html.
        /// </summary>
        /// <returns>HTML for space remaining.</returns>
        private string GetSpaceHtml()
        {
            string html = ReadFile( Path.Combine( "html", "space.html" ) );
            html = AddCommonHtml( html );

            StringBuilder javascriptBuilder = new StringBuilder();
            StringBuilder htmlBuilder = new StringBuilder();

            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach( DriveInfo drive in drives )
            {
                if( drive.IsReady == false )
                {
                    continue;
                }
                // Skip any temp file systems or anything like that.
                else if( drive.TotalSize <= 0 )
                {
                    continue;
                }

                string driveVarName = Regex.Replace( drive.Name, @"[^\w]+", "_" );

                if( driveVarName == "_" )
                {
                    driveVarName = "_root";
                }
                                                            // Kilo       Mega          Giga
                double freeGb = drive.AvailableFreeSpace / ( 1000.0 ) / ( 1000.0 ) / ( 1000.0 );
                double usedGb = ( drive.TotalSize - drive.AvailableFreeSpace ) / ( 1000.0 ) / ( 1000.0 ) / ( 1000.0 );
                double driveSive = drive.TotalSize / ( 1000.0 ) / ( 1000.0 ) / ( 1000.0 );

                javascriptBuilder.AppendLine( "// -------- Begin " + drive.Name + " -------- " );
                javascriptBuilder.AppendLine( "var ctx_" + driveVarName + " = document.getElementById(\"" + driveVarName + "\");" );
                javascriptBuilder.AppendLine( "var data_" + driveVarName + @" = { labels : [ ""Space Used (GB)"", ""Space Remaining (GB)""]," );
                javascriptBuilder.AppendLine( @" datasets : [" );
                javascriptBuilder.AppendFormat( @"{{ data: [{0:0.00},{1:0.00}],{2}", usedGb, freeGb, Environment.NewLine);
                javascriptBuilder.AppendLine( @"backgroundColor: [""#FF6384"", ""#36A2EB""], hoverBackgroundColor: [ ""#FF6384"", ""#36A2EB"" ] }" );
                javascriptBuilder.AppendLine( "] };" );

                javascriptBuilder.AppendLine( "var chart_" + driveVarName + " = new Chart ( ctx_" + driveVarName + ",{" );
                javascriptBuilder.AppendLine( "type: 'doughnut', data: data_" + driveVarName + @", animation: { animateScale: true }});" );
                javascriptBuilder.AppendLine();

                htmlBuilder.AppendLine( @"<div>" );
                htmlBuilder.AppendLine( "<h3>" + drive.Name + "</h3>" );
                htmlBuilder.AppendLine( @"<canvas id=""" + driveVarName + @""" width=""100"" height=""100"" style=""max-width:500px;max-height:500px;""></canvas>" );
                htmlBuilder.AppendFormat( "<p>Used {0:0.00} of {1:0.00} GB.  {2:0.00} GB remain.</p>{3}", usedGb, driveSive, freeGb, Environment.NewLine );
                htmlBuilder.AppendLine( "</div><br/>" );
            }

            html = html.Replace( "{%chartJs%}", javascriptBuilder.ToString() );
            html = html.Replace( "{%chartHtml%}", htmlBuilder.ToString() );

            return html;
        }

        /// <summary>
        /// Get the about page's HTML.
        /// </summary>
        /// <returns>The about page's HTML.</returns>
        private string GetAboutHtml()
        {
            string html = ReadFile( Path.Combine( "html", "about.html" ) );
            html = html.Replace( "{%Version%}", PictureFrame.VersionString );
            html = AddCommonHtml( html );

            return html;
        }

        /// <summary>
        /// Gets the shutdown page's html.
        /// </summary>
        /// <param name="message">The message to tell the user.</param>
        /// <returns>The shutdown page's html.</returns>
        private static string GetShutdownHtml( string message )
        {
            string html = ReadFile( Path.Combine( "html", "shutdown.html" ) );
            html = html.Replace( "{%Message%}", message );
            html = AddCommonHtml( html );

            return html;
        }

        /// <summary>
        /// Adds the common html stuff to each page.
        /// </summary>
        /// <param name="html">The HTML to add the common stuff to.</param>
        /// <returns>HTML with common stuff added.</returns>
        private static string AddCommonHtml( string html )
        {
            html = html.Replace( "{%CommonHead%}", ReadFile( Path.Combine( "html", "CommonHead.html" ) ) );
            html = html.Replace( "{%SideBar%}", ReadFile( Path.Combine( "html", "Sidebar.html" ) ) );

            return html;
        }

        /// <summary>
        /// Adds setting html things.
        /// </summary>
        /// <param name="html">Adds the settings HTML</param>
        /// <returns></returns>
        private static string AddSettingsHtml( string html )
        {
            html = html.Replace( "{%HourOptions%}", ListToHtmlOptions( hourOptions ) );
            html = html.Replace( "{%MinuteOptions%}", ListToHtmlOptions( minuteOptions ) );
            html = html.Replace( "{%RefreshOptions%}", ListToHtmlOptions( picRefreshIntervalOptions ) );
            html = html.Replace( "{%IntervalOptions%}", ListToHtmlOptions( picChangeIntervalOptions ) );

            return html;
        }

        private string HandleSleepRequest( string method )
        {
            string response;
            if( method == "POST" )
            {
                this.picFrame.Screen.IsOn = this.picFrame.Screen.IsOn == false;
                response = GetTurnOffHtml( "Screen should have been toggled." );
            }
            else
            {
                response = GetTurnOffHtml( "Must POST request to toggle screen." );
            }

            return response;
        }

        /// <summary>
        /// Handles a request to exit to the desktop.
        /// </summary>
        /// <param name="request">The HTTP Request that was received.</param>
        /// <returns>The HTML to return to the user.</returns>
        private string HandleExitToDesktopRequest( string method )
        {
            string response;
            if( method == "POST" )
            {
                response = GetShutdownHtml( "The frame will exit to the desktop in ~5 seconds.  This webpage will no longer show up." );
                this.quitReason = QuitReason.ExitToDesktop;
                this.quitEvent.Set();
            }
            else
            {
                response = GetTurnOffHtml( "Must POST request to exit to desktop" );
            }

            return response;
        }

        /// <summary>
        /// Handles a request to shutdown the system.
        /// </summary>
        /// <param name="request">The HTTP Request that was received.</param>
        /// <returns>The HTML to return to the user.</returns>
        private string HandleShutdownSystemRequest( string method )
        {
            string response;
            if( method == "POST" )
            {
                response = GetShutdownHtml( "The frame will start the shutdown sequence in ~5 seconds.  This webpage will no longer show up." );
                this.quitReason = QuitReason.ShuttingDown;
                this.quitEvent.Set();
            }
            else
            {
                response = GetTurnOffHtml( "Must POST request to shutdown system." );
            }

            return response;
        }

        /// <summary>
        /// Handles a request for gettings the current picture.
        /// </summary>
        /// <param name="response">Response being used.</param>
        private void HandleGetCurrentPictureRequest( HttpListenerResponse response )
        {
            response.AddHeader( "Content-Encoding", "gzip" );
            string picLocation = this.picFrame.CurrentPictureLocation;

            byte[] pictureContents;

            using( MemoryStream ms = new MemoryStream() )
            {
                using( GZipStream gzip = new GZipStream( ms, CompressionMode.Compress, true ) )
                {
                    using( BinaryReader br = new BinaryReader( File.Open( picLocation, FileMode.Open, FileAccess.Read ) ) )
                    {
                        byte[] buffer = br.ReadBytes( 1028 );
                        while( buffer.Length > 0 )
                        {
                            gzip.Write( buffer, 0, buffer.Length );
                            buffer = br.ReadBytes( 1028 );
                        }
                    }
                }

                pictureContents = ms.ToArray();
            }

            response.ContentType = "image/" + Path.GetExtension( picLocation ).TrimStart( '.' );
            response.ContentLength64 = pictureContents.Length;
            response.OutputStream.Write( pictureContents, 0, pictureContents.Length );
            response.OutputStream.Flush();
        }

        /// <summary>
        /// Handle the request when a user asks to change the photo.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <returns>HTML to return to the user.</returns>
        private string HandleChangePictureRequest( string method )
        {
            string response;
            if( method == "POST" )
            {
                this.picFrame.ToggleNextPhoto();
                response = GetIndexHtml();
            }
            else
            {
                response = GetIndexHtml();
            }

            return response;
        }

        /// <summary>
        /// Handles a request to shutdown the system.
        /// </summary>
        /// <param name="request">The HTTP Request that was received.</param>
        /// <returns>The HTML to return to the user.</returns>
        private string HandleRestartSystemRequest( string method )
        {
            string response;
            if( method == "POST" )
            {
                response = GetShutdownHtml( "The frame will start the restart sequence in ~5 seconds.  This webpage will no longer show up until it is done rebooting." );
                this.quitReason = QuitReason.Restarting;
                this.quitEvent.Set();
            }
            else
            {
                response = GetTurnOffHtml( "Must POST request to restart system." );
            }

            return response;
        }

        /// <summary>
        /// Converts list to html options
        /// </summary>
        /// <param name="list">List to convert to.</param>
        /// <returns>Returns the HTML options from the list.</returns>
        private static string ListToHtmlOptions<T>( IList<T> list )
        {
            StringBuilder builder = new StringBuilder();
            foreach( T element in list )
            {
                builder.AppendFormat(
                    "<option>{0}</option>{1}",
                    element.ToString(),
                    Environment.NewLine
                );
            }

            return builder.ToString();
        }

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
