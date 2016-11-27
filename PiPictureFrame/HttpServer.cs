
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
using System.Text.RegularExpressions;
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
                        responseString = GetIndexHtml();
                    }
                    else if( url == "/settings.html" )
                    {
                        responseString = GetSettingsHtml( string.Empty );
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
