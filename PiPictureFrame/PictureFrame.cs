
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using PiPictureFrame.Core.Renderers;
using PiPictureFrame.Core.Screens;
using SethCS.Basic;

namespace PiPictureFrame.Core
{
    public class PictureFrame : IDisposable
    {
        // ---------------- Fields ----------------

        public const string VersionString = "1.0.0";

        public static readonly SemanticVersion Version = SemanticVersion.Parse( VersionString );

        private bool isDisposed;

        private HttpServer server;

        private Action<string> loggingAction;

        private PictureFrameConfig config;
        private object configLock;

        private static readonly string rootFolder;

        private Thread nextPictureThread;
        private AutoResetEvent nextPictureEvent;

        private bool isRunning;

        private object isRunningLock;

        private IRenderer renderer;

        private EventScheduler scheduler;
        private int awakeEventId;
        private int sleepEventId;

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.
        /// </summary>
        public PictureFrame()
        {
            this.isDisposed = false;
            this.scheduler = new EventScheduler();

            this.nextPictureEvent = new AutoResetEvent( false );

            this.configLock = new object();

            this.isRunningLock = new object();
            this.isRunning = false;

            this.awakeEventId = -1;
            this.sleepEventId = -1;
        }

        /// <summary>
        /// Static Constructor.
        /// </summary>
        static PictureFrame()
        {
            rootFolder = Path.GetDirectoryName( UserConfigLocation );
        }

        // ---------------- Properties ----------------

        public IScreen Screen { get; private set; }

        /// <summary>
        /// Gets the current picture location.
        /// </summary>
        public string CurrentPictureLocation
        {
            get
            {
                return this.renderer.CurrentPicturePath;
            }
        }

        /// <summary>
        /// Whether or not the frame is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock( this.isRunningLock )
                {
                    return this.isRunning;
                }
            }
            private set
            {
                lock( this.isRunningLock )
                {
                    this.isRunning = value;
                }
            }
        }

        /// <summary>
        /// The location of the XML user configuration for the pi picture frame.
        /// </summary>
        public static string UserConfigLocation
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ),
                    "PiPictureFrame",
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

            this.Screen = new PiTouchScreen( this.loggingAction ); // Only have pi trouch screen implemented now.

            this.renderer = new PqivRenderer( this.loggingAction );
            this.renderer.Init( this.config.PhotoDirectory );

            this.ScheduleEventsNoLock();
        }

        /// <summary>
        /// Saves the configuration to the UserXml file.
        /// </summary>
        public void SaveConfig()
        {
            lock( this.configLock )
            {
                this.SaveConfigNoLock();
            }
        }

        /// <summary>
        /// Saves the config with no lock.
        /// </summary>
        private void SaveConfigNoLock()
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

            this.IsRunning = true;

            this.nextPictureThread = new Thread( this.NextPictureThreadRunner );
            this.nextPictureThread.Start();

            this.server = new HttpServer( this, config.Port );
            this.server.LoggingAction += this.loggingAction;
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
            this.IsRunning = false;
            this.nextPictureEvent.Set();

            this.nextPictureThread?.Join();

            try
            {
                if( this.server != null )
                {
                    this.server.LoggingAction -= this.loggingAction;
                    this.server.Dispose();
                }
            }
            finally
            {
                try
                {
                    this.scheduler?.Dispose();
                }
                finally
                {
                    this.renderer?.Dispose();
                }
            }

            this.isDisposed = true;
        }

        /// <summary>
        /// Toggles the next photo to appear.
        /// </summary>
        public void ToggleNextPhoto()
        {
            this.nextPictureEvent.Set();
        }

        /// <summary>
        /// Configures the picture frame.
        /// </summary>
        /// <param name="config">The config to use.</param>
        public void Configure( PictureFrameConfig config )
        {
            config.Validate();
            bool togglePhoto;
            lock( this.configLock )
            {
                togglePhoto = this.config.PhotoChangeInterval != config.PhotoChangeInterval;
                this.config = config.Clone();
                this.Screen.Brightness = this.config.Brightness;
                this.ScheduleEventsNoLock();
                this.SaveConfigNoLock();
            }

            if( togglePhoto )
            {
                this.ToggleNextPhoto();
            }
        }

        /// <summary>
        /// Gets a COPY of the current Picture Frame Config.
        /// </summary>
        /// <returns>A deep copy of the current picture frame config.</returns>
        public PictureFrameConfig GetCurrentConfig()
        {
            lock( this.configLock )
            {
                return this.config.Clone();
            }
        }

        private void ScheduleEventsNoLock()
        {
            if( this.config.AwakeTime.HasValue && this.config.SleepTime.HasValue )
            {
                // If there's a value in both, delete any existing events,
                // and add new ones.
                this.RemoveAwakeSleepEvents();

                // Schedule Awake Time.
                this.awakeEventId = this.scheduler.ScheduleRecurringEvent(
                    CalculateFirstEvent( this.config.AwakeTime.Value ),
                    new TimeSpan( 24, 0, 0 ),
                    () => this.Screen.IsOn = true
                );

                this.sleepEventId = this.scheduler.ScheduleRecurringEvent(
                    CalculateFirstEvent( this.config.SleepTime.Value ),
                    new TimeSpan( 24, 0, 0 ),
                    () => this.Screen.IsOn = false
                );
            }
            else
            {
                this.RemoveAwakeSleepEvents();
            }
        }

        private void RemoveAwakeSleepEvents()
        {
            if( this.awakeEventId != -1 )
            {
                this.scheduler.StopEvent( this.awakeEventId );
                this.awakeEventId = -1;
            }

            if( this.sleepEventId != -1 )
            {
                this.scheduler.StopEvent( this.sleepEventId );
                this.sleepEventId = -1;
            }
        }

        private TimeSpan CalculateFirstEvent( DateTime startTime )
        {
            DateTime now = DateTime.Now;
            startTime = new DateTime( now.Year, now.Month, now.Day, startTime.Hour, startTime.Minute, now.Second );

            TimeSpan delta = startTime - now;

            // If we are less than zero, we need to go to the next day.
            while( delta.TotalMilliseconds < 0 )
            {
                startTime = new DateTime( now.Year, now.Month, now.Day + 1, startTime.Hour, startTime.Minute, now.Second );
                delta = startTime - now;
            }

            return delta;
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
                    this.Restart();
                    break;

                case HttpServer.QuitReason.ShuttingDown:
                    this.Shutdown();
                    break;

                case HttpServer.QuitReason.FatalError:
                    // If we are a fatal error, we should not quit since we don't want the frame
                    // exiting.  Wait forever.
                    this.loggingAction?.Invoke( "A HTTP server fatal error occurred.  Hanging processes until its killed so the frame doesn't stop..." );
                    ManualResetEvent e = new ManualResetEvent( false );
                    e.WaitOne();
                    break;
            }
        }

        private void Restart()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if( ( Environment.OSVersion.Platform == PlatformID.Unix ) || ( Environment.OSVersion.Platform == PlatformID.MacOSX ) )
            {
                startInfo.FileName = Path.Combine( "usr", "bin", "sudo" );
                startInfo.Arguments = "reboot";
            }
            else
            {
                startInfo.FileName = Path.Combine( "C:", "Windows", "System32", "shutdown.exe" );
                startInfo.Arguments = "/r";
            }

            RunProcess( startInfo, "restart" );
        }

        private void Shutdown()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if( ( Environment.OSVersion.Platform == PlatformID.Unix ) || ( Environment.OSVersion.Platform == PlatformID.MacOSX ) )
            {
                startInfo.FileName = Path.Combine( "usr", "bin", "sudo" );
                startInfo.Arguments = "shutdown -Ph now";
            }
            else
            {
                startInfo.FileName = Path.Combine( "C:", "Windows", "System32", "shutdown.exe" );
                startInfo.Arguments = "/s";
            }

            RunProcess( startInfo, "shutdown" );
        }

        private void RunProcess( ProcessStartInfo info, string context )
        {
            try
            {
                using( Process process = new Process() )
                {
                    process.StartInfo = info;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch( Exception err )
            {
                this.loggingAction?.Invoke( "**************************" );
                this.loggingAction?.Invoke( "Error when running " + context + " process..." );
                this.loggingAction?.Invoke( err.ToString() );
                this.loggingAction?.Invoke( "**************************" );
            }
        }

        /// <summary>
        /// Method that handles changing pictures.
        /// </summary>
        private void NextPictureThreadRunner()
        {
            while( this.IsRunning )
            {
                int waitTime;
                lock( this.configLock )
                {
                    waitTime = (int)this.config.PhotoChangeInterval.TotalMilliseconds;
                }

                // Use AutoResetEvent so if it times out, or the user triggers it, change the picture.
                this.nextPictureEvent.WaitOne( waitTime );
                if( this.IsRunning )
                {
                    this.loggingAction?.Invoke( "Changing Picture..." );
                    this.renderer.GoToNextPicture();
                }
                else
                {
                    this.loggingAction?.Invoke( "Next Picture Thread Shutting Down." );
                }
            }
        }
    }
}
