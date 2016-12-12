
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.IO;
using System.Text;
using System.Xml;
using SethCS.Exceptions;

namespace PiPictureFrame.Core
{
    public class PictureFrameConfig
    {
        // ---------------- Fields ----------------

        /// <summary>
        /// The XML element name that this gets appended to.
        /// </summary>
        public const string XmlNodeName = "pictureframeconfig";

        private DateTime? sleepTime;

        private DateTime? awakeTime;

        /// <summary>
        /// The default DateTime object to create others from (where we only modify
        /// the minutes and hours).
        /// </summary>
        private static readonly DateTime defaultDateTime = new DateTime( 2016, 12, 25 );

        // ---------------- Constructor ----------------

        /// <summary>
        /// Creates a Picture Frame config from the given XML node.
        /// </summary>
        /// <param name="node">The node to create the element from.</param>
        /// <returns>The PictureFrameConfig based on the given XML Element.</returns>
        public static PictureFrameConfig FromXml( XmlNode node )
        {
            if( node.Name != XmlNodeName )
            {
                throw new ArgumentException( "Xml Element name must be '" + XmlNodeName + "', got: " + node.Name, nameof( node ) );
            }

            PictureFrameConfig config = new PictureFrameConfig();

            foreach( XmlNode childNode in node.ChildNodes )
            {
                switch( childNode.Name )
                {
                    case "sleeptime":
                        int sleepHour = -1;
                        int sleepMinute = -1;
                        foreach( XmlAttribute attr in childNode.Attributes )
                        {
                            switch( attr.Name )
                            {
                                case "hour":
                                    sleepHour = int.Parse( attr.Value );
                                    break;
                                case "minute":
                                    sleepMinute = int.Parse( attr.Value );
                                    break;
                            }
                        }
                        if( ( sleepHour > -1 ) && ( sleepMinute > -1 ) )
                        {
                            config.SleepTime = new DateTime( defaultDateTime.Year, defaultDateTime.Month, defaultDateTime.Day, sleepHour, sleepMinute, 0 );
                        }
                        break;

                    case "awaketime":
                        int awakeHour = -1;
                        int awakeMinute = -1;
                        foreach( XmlAttribute attr in childNode.Attributes )
                        {
                            switch( attr.Name )
                            {
                                case "hour":
                                    awakeHour = int.Parse( attr.Value );
                                    break;
                                case "minute":
                                    awakeMinute = int.Parse( attr.Value );
                                    break;
                            }
                        }
                        if( ( awakeHour > -1 ) && ( awakeMinute > -1 ) )
                        {
                            DateTime today = DateTime.UtcNow;
                            config.AwakeTime = new DateTime( defaultDateTime.Year, defaultDateTime.Month, defaultDateTime.Day, awakeHour, awakeMinute, 0 );
                        }
                        break;

                    case "photodirectory":
                        config.PhotoDirectory = childNode.InnerText;
                        break;

                    case "refreshinterval":
                        config.PhotoRefreshInterval = new TimeSpan( 0, 0, int.Parse( childNode.InnerText ) );
                        break;

                    case "photochangeinterval":
                        config.PhotoChangeInterval = new TimeSpan( 0, 0, int.Parse( childNode.InnerText ) );
                        break;

                    case "httpport":
                        config.Port = short.Parse( childNode.InnerText );
                        break;

                    case "brightness":
                        config.Brightness = ushort.Parse( childNode.InnerText );
                        break;
                }
            }

            config.Validate();

            return config;
        }

        /// <summary>
        /// Constructor.
        /// Sets everything to a default value.
        /// </summary>
        public PictureFrameConfig()
        {
            this.SleepTime = null;
            this.AwakeTime = null;

            this.PhotoDirectory = Path.Combine( "/home", "picframe", "Photos" );
            this.PhotoRefreshInterval = new TimeSpan( 1, 0, 0 );
            this.PhotoChangeInterval = new TimeSpan( 0, 1, 0 );
            this.Port = HttpServer.DefaultPort;
            this.Brightness = 75;
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// Time to turn off the screen.
        /// Null to leave the screen on forever.
        /// 
        /// Only care about hour/minute.  Everything else is ignored.
        /// </summary>
        public DateTime? SleepTime
        {
            get
            {
                return this.sleepTime;
            }
            set
            {
                if( value == null )
                {
                    this.sleepTime = value;
                }
                else
                {
                    this.sleepTime = new DateTime(
                        defaultDateTime.Year,
                        defaultDateTime.Month,
                        defaultDateTime.Day,
                        value.Value.Hour,
                        value.Value.Minute,
                        0
                    );
                }
            }
        }

        /// <summary>
        /// Time to wake up the screen.
        /// Null to leave the screen on forever.
        /// 
        /// Only care about hour/minute.  Everything else is ignored.
        /// </summary>
        public DateTime? AwakeTime
        {
            get
            {
                return this.awakeTime;
            }
            set
            {
                if( value == null )
                {
                    this.awakeTime = value;
                }
                else
                {
                    this.awakeTime = new DateTime(
                        defaultDateTime.Year,
                        defaultDateTime.Month,
                        defaultDateTime.Day,
                        value.Value.Hour,
                        value.Value.Minute,
                        0
                    );
                }
            }
        }

        /// <summary>
        /// Directory to search for photos.
        /// Directory should contain symlinks to other directories that contain the photos.
        /// </summary>
        public string PhotoDirectory { get; set; }

        /// <summary>
        /// How often to check for photos on the disk.  0 for never.
        /// </summary>
        public TimeSpan PhotoRefreshInterval { get; set; }

        /// <summary>
        /// How ofter to change the photo on the screen.
        /// </summary>
        public TimeSpan PhotoChangeInterval { get; set; }

        /// <summary>
        /// The port for the HTTP server to run on.
        /// </summary>
        public short Port { get; set; }

        /// <summary>
        /// The brightness on a scale from 0-100.
        /// </summary>
        public ushort Brightness { get; set; }

        // ---------------- Functions ----------------

        /// <summary>
        /// Ensures all settings are valid.  Returns false otherwise.
        /// </summary>
        /// <param name="errorString">What is wrong with the settings.</param>
        public bool TryValidate( out string errorString )
        {
            errorString = string.Empty;

            StringBuilder builder = new StringBuilder();

            bool success = true;
            if( string.IsNullOrEmpty( this.PhotoDirectory ) || string.IsNullOrWhiteSpace( this.PhotoDirectory ) )
            {
                success = false;
                builder.AppendLine( nameof( this.PhotoDirectory ) + " can not be empty, whitespace, or null" );
            }
            if( this.Port < 0 )
            {
                success = false;
                builder.AppendLine( nameof( this.Port ) + " can not be negative" );
            }
            if( this.Brightness > 100 )
            {
                success = false;
                builder.AppendLine( nameof( this.Brightness ) + " can not be more than 100" );
            }

            if( success == false )
            {
                errorString = builder.ToString();
            }
            return success;
        }

        /// <summary>
        /// Ensures all settings are valid.  Throws exceptions otherwise.
        /// </summary>
        ///
        public void Validate()
        {
            string errorString;
            if( this.TryValidate( out errorString ) == false )
            {
                throw new ValidationException( errorString );
            }
        }

        /// <summary>
        /// Checks to see if the given object matches this one.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if obj matches this instance, otherwise false.</returns>
        public override bool Equals( object obj )
        {
            PictureFrameConfig other = obj as PictureFrameConfig;
            if( other == null )
            {
                return false;
            }

            bool match = true;

            if( this.SleepTime == null )
            {
                match &= ( other.SleepTime == null );
            }
            else
            {
                match &= this.SleepTime.Equals( other.SleepTime );
            }

            if( this.AwakeTime == null )
            {
                match &= ( other.AwakeTime == null );
            }
            else
            {
                match &= this.AwakeTime.Equals( other.AwakeTime );
            }

            match &= ( this.PhotoDirectory == other.PhotoDirectory );
            match &= ( this.PhotoRefreshInterval.Equals( other.PhotoRefreshInterval ) );
            match &= ( this.PhotoChangeInterval.Equals( other.PhotoChangeInterval ) );
            match &= ( this.Port == other.Port );
            match &= ( this.Brightness == other.Brightness );

            return match;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;

            if( this.AwakeTime != null )
            {
                hashCode += this.AwakeTime.GetHashCode();
            }

            if( this.SleepTime != null )
            {
                hashCode += this.SleepTime.GetHashCode();
            }

            hashCode += this.PhotoDirectory.GetHashCode();
            hashCode += this.PhotoRefreshInterval.GetHashCode();
            hashCode += this.PhotoChangeInterval.GetHashCode();
            hashCode += this.Port.GetHashCode();
            hashCode += this.Brightness.GetHashCode();

            return hashCode;
        }

        /// <summary>
        /// Creates a deep-copy of this instance.
        /// </summary>
        /// <returns>A deep copy of this instance.</returns>
        public PictureFrameConfig Clone()
        {
            return (PictureFrameConfig)this.MemberwiseClone();
        }

        /// <summary>
        /// Appends this class to the parent node in XML format.
        /// </summary>
        /// <param name="parentNode">The parent node to append to.</param>
        /// <param name="doc">The XML document we are appending to.</param>
        public void ToXml( XmlNode parentNode, XmlDocument doc )
        {
            // Add sleep time.
            {
                XmlElement sleepNode = doc.CreateElement( "sleeptime" );

                XmlAttribute hourAttr = doc.CreateAttribute( "hour" );
                XmlAttribute minAttr = doc.CreateAttribute( "minute" );
                if( this.SleepTime == null )
                {
                    hourAttr.Value = "-1";
                    minAttr.Value = "-1";
                }
                else
                {
                    hourAttr.Value = this.SleepTime.Value.Hour.ToString();
                    minAttr.Value = this.SleepTime.Value.Minute.ToString();
                }

                sleepNode.Attributes.Append( hourAttr );
                sleepNode.Attributes.Append( minAttr );
                parentNode.AppendChild( sleepNode );
            }

            // Add awake time.
            {
                XmlElement awakeNode = doc.CreateElement( "awaketime" );

                XmlAttribute hourAttr = doc.CreateAttribute( "hour" );
                XmlAttribute minAttr = doc.CreateAttribute( "minute" );
                if( this.AwakeTime == null )
                {
                    hourAttr.Value = "-1";
                    minAttr.Value = "-1";
                }
                else
                {
                    hourAttr.Value = this.AwakeTime.Value.Hour.ToString();
                    minAttr.Value = this.AwakeTime.Value.Minute.ToString();
                }

                awakeNode.Attributes.Append( hourAttr );
                awakeNode.Attributes.Append( minAttr );
                parentNode.AppendChild( awakeNode );
            }

            // Photo Directory
            {
                XmlElement photoDirNode = doc.CreateElement( "photodirectory" );
                photoDirNode.InnerText = this.PhotoDirectory;
                parentNode.AppendChild( photoDirNode );
            }

            // Refresh Interval
            {
                XmlElement refreshNode = doc.CreateElement( "refreshinterval" );
                refreshNode.InnerText = ( (int)this.PhotoRefreshInterval.TotalSeconds ).ToString();
                parentNode.AppendChild( refreshNode );
            }

            // Photo Change Interval
            {
                XmlElement changeNode = doc.CreateElement( "photochangeinterval" );
                changeNode.InnerText = ( (int)this.PhotoChangeInterval.TotalSeconds ).ToString();
                parentNode.AppendChild( changeNode );
            }

            // HTTP Port
            {
                XmlElement httpPort = doc.CreateElement( "httpport" );
                httpPort.InnerText = this.Port.ToString();
                parentNode.AppendChild( httpPort );
            }

            // Brightness
            {
                XmlElement brightness = doc.CreateElement( "brightness" );
                brightness.InnerText = this.Brightness.ToString();
                parentNode.AppendChild( brightness );
            }
        }
    }
}
