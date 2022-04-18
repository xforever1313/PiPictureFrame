
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System.Text;
using Microsoft.Extensions.Logging;

namespace PiPictureFrame.Api.Screens
{
    internal sealed class PiTouchScreen : IScreen
    {
        // ---------------- Fields ----------------

        /// <summary>
        /// File to control brightness.
        /// </summary>
        private const string brightnessFile = "/sys/class/backlight/rpi_backlight/brightness";

        /// <summary>
        /// File to turn on/off screen.
        /// </summary>
        private const string powerFile = "/sys/class/backlight/rpi_backlight/bl_power";

        private byte brightness;
        private bool isOn;

        private readonly ILogger log;

        // ---------------- Constructor ----------------

        public PiTouchScreen( ILogger log )
        {
            this.log = log;
            this.Refresh();
        }

        // ---------------- Properties ----------------

        public byte Brightness
        {
            get
            {
                return this.brightness;
            }
            set
            {
                if( value != this.Brightness )
                {
                    if( ( value > 100 ) || ( value < 0 ) )
                    {
                        throw new ArgumentException( "Brightness must be between 0-100" );
                    }

                    // De-normalize.
                    int brightness = (int)Math.Floor( ( value ) / ( 100.0 ) * 255.0 );
                    lock( brightnessFile )
                    {
                        if( this.WriteFile( brightnessFile, brightness.ToString() ) )
                        {
                            this.brightness = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Whether or not the screen is on.
        /// </summary>
        public bool IsOn
        {
            get
            {
                return this.isOn;
            }
            set
            {
                if( value != this.IsOn )
                {
                    lock( powerFile )
                    {
                        // 0 to turn on screen, else 1.
                        string s = value ? "0" : "1";
                        if( this.WriteFile( powerFile, s ) )
                        {
                            this.isOn = value;
                        }
                    }
                }
            }
        }

        // ---------------- Functions ----------------

        /// <summary>
        /// Populates the properties by reading from the OS.
        /// </summary>
        public void Refresh()
        {
            this.RefreshIsOn();
            this.RefreshBrightness();
        }

        private void RefreshIsOn()
        {
            lock( powerFile )
            {
                string isOnString = this.ReadFile( powerFile );
                if( string.IsNullOrWhiteSpace( isOnString ) == false )
                {
                    if( isOnString.StartsWith( "0" ) )
                    {
                        this.isOn = true;
                    }
                    else
                    {
                        this.isOn = false;
                    }
                }
            }
        }

        private void RefreshBrightness()
        {
            lock( brightnessFile )
            {
                string isOnString = this.ReadFile( brightnessFile );
                if( string.IsNullOrWhiteSpace( isOnString ) == false )
                {
                    int brightness;
                    if( int.TryParse( isOnString, out brightness ) && ( brightness > 0 ) )
                    {
                        // Let 20 be 0, 255 be 100.
                        // Normalized taken from here: https://docs.tibco.com/pub/spotfire/7.0.1/doc/html/norm/norm_scale_between_0_and_1.htm
                        double normalized = ( brightness - 20.0 ) / ( 255.0 - 20 ) * 100;
                        this.brightness = (byte)Math.Ceiling( normalized );
                    }
                }
            }
        }

        private string ReadFile( string filePath )
        {
            try
            {
                using( var stream = new FileStream( filePath, FileMode.Open, FileAccess.Read ) )
                {
                    using( var reader = new StreamReader( stream ) )
                    {
                        string value = reader.ReadToEnd().TrimEnd();
                        this.log.LogInformation( "Read '" + value + "' from " + filePath );
                        return value;
                    }
                }
            }
            catch( Exception err )
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine( "Error when reading file " + filePath + ":" );
                builder.AppendLine( err.ToString() );

                this.log.LogError( builder.ToString() );
            }

            return string.Empty;
        }

        private bool WriteFile( string filePath, string value )
        {
            try
            {
                using( var stream = new FileStream( filePath, FileMode.Open, FileAccess.Write ) )
                {
                    using( var writer = new StreamWriter( stream ) )
                    {
                        this.log.LogInformation( "Writing '" + value + "' to " + filePath );
                        writer.Write( value );
                        writer.Flush();
                        return true;
                    }
                }
            }
            catch( Exception err )
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine( "Error when writing to file " + filePath + ":" );
                builder.AppendLine( err.ToString() );

                this.log.LogError( builder.ToString() );
            }

            return false;
        }
    }
}
