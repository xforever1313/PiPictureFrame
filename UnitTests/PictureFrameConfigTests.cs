using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using PiPictureFrame.Core;
using SethCS.Exceptions;

namespace UnitTests
{
    [TestFixture]
    public class PictureFrameConfigTests
    {
        // -------- Fields --------

        private PictureFrameConfig uut;

        // -------- Setup / Teardown --------

        [SetUp]
        public void TestSetup()
        {
            this.uut = new PictureFrameConfig();
        }

        [TearDown]
        public void TestTeardown()
        {
        }

        // -------- Tests --------

        /// <summary>
        /// Ensures the equals function works correctly.
        /// </summary>
        [Test]
        public void EqualsTest()
        {
            Assert.IsFalse( this.uut.Equals( null ) );
            Assert.IsFalse( this.uut.Equals( 12 ) );

            PictureFrameConfig config2 = new PictureFrameConfig();

            Assert.AreEqual( this.uut, config2 );
            Assert.AreEqual( this.uut.GetHashCode(), config2.GetHashCode() );

            // Start changing properties!

            config2.AwakeTime = DateTime.MaxValue;
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.SleepTime = DateTime.MaxValue;
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.PhotoDirectory = ".";
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.PhotoRefreshInterval = this.uut.PhotoRefreshInterval + new TimeSpan( 1 );
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.PhotoChangeInterval = this.uut.PhotoChangeInterval + new TimeSpan( 1 );
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.ShutdownCommand = this.uut.ShutdownCommand + ".";
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.RebootCommand = this.uut.RebootCommand + ".";
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.ExitToDesktopCommand = this.uut.ExitToDesktopCommand + ".";
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            config2.Port = (short)( this.uut.Port + 1 );
            Assert.AreNotEqual( this.uut, config2 );
            Assert.AreNotEqual( config2, this.uut );
            Assert.AreNotEqual( this.uut.GetHashCode(), config2.GetHashCode() );
            config2 = new PictureFrameConfig();

            // Ensure different DateTimes, but same hour/minute are still equal.
            // Everything but hour/minute is ignored.
            this.uut.AwakeTime = new DateTime( 2000, 1, 1, 12, 1, 3 );
            this.uut.SleepTime = new DateTime( 2000, 1, 1, 13, 2, 3 );

            config2.AwakeTime = new DateTime( 2001, 2, 3, 12, 1, 4 );
            config2.SleepTime = new DateTime( 2001, 2, 3, 13, 2, 4 );

            Assert.AreEqual( this.uut, config2 );
            Assert.AreEqual( this.uut.GetHashCode(), config2.GetHashCode() );
        }

        /// <summary>
        /// Tests the clone method.
        /// </summary>
        [Test]
        public void CloneTest()
        {
            // With nulls
            {
                this.uut.AwakeTime = null;
                this.uut.SleepTime = null;
                PictureFrameConfig config2 = this.uut.Clone();
                Assert.AreEqual( this.uut, config2 );
                Assert.AreNotSame( this.uut, config2 );
            }

            // Without Nulls
            {
                this.uut.AwakeTime = new DateTime( 2000, 12, 11, 1, 2, 3 ); // Days are arbitrary.
                this.uut.SleepTime = new DateTime( 2001, 11, 14, 3, 4, 5 );
                PictureFrameConfig config2 = this.uut.Clone();
                Assert.AreEqual( this.uut, config2 );
                Assert.AreNotSame( this.uut, config2 );
            }
        }

        /// <summary>
        /// Ensures we can serialize/deserialize correctly.
        /// </summary>
        [Test]
        public void XmlTest()
        {
            // With Nulls.
            {
                this.uut.AwakeTime = null;
                this.uut.SleepTime = null;

                XmlDocument doc = new XmlDocument();
                XmlNode parent = doc.CreateElement( PictureFrameConfig.XmlNodeName );
                this.uut.ToXml( parent, doc );

                PictureFrameConfig config2 = PictureFrameConfig.FromXml( parent );
                Assert.AreEqual( this.uut, config2 );
            }

            // Without Nulls.
            {
                this.uut.AwakeTime = new DateTime( 2000, 12, 12, 5, 6, 7 ); // Days are arbitrary.
                this.uut.SleepTime = new DateTime( 2001, 12, 12, 3, 2, 1 );

                XmlDocument doc = new XmlDocument();
                XmlNode parent = doc.CreateElement( PictureFrameConfig.XmlNodeName );
                this.uut.ToXml( parent, doc );

                PictureFrameConfig config2 = PictureFrameConfig.FromXml( parent );
                Assert.AreEqual( this.uut, config2 );
            }

            // Different NodeName
            {
                XmlDocument doc = new XmlDocument();
                XmlNode parent = doc.CreateElement( "Derp" );
                Assert.Throws<ArgumentException>( () => PictureFrameConfig.FromXml( parent ) );
            }
        }

        [Test]
        public void ValidateTest()
        {
            // Default should validate.
            this.Validates();

            // Null times should still validate.
            this.uut.AwakeTime = new DateTime( 2000, 12, 12, 5, 6, 7 ); // Days are arbitrary.
            this.uut.SleepTime = new DateTime( 2001, 12, 12, 3, 2, 1 );
            this.Validates();
            this.uut = new PictureFrameConfig();

            // Empty photo directory should not validate.
            {
                this.uut.PhotoDirectory = string.Empty;
                this.DoesNotValidate();
                this.uut.PhotoDirectory = null;
                this.DoesNotValidate();
                this.uut = new PictureFrameConfig();
            }

            // Empty shutdown command should not validate.
            {
                this.uut.ShutdownCommand = string.Empty;
                this.DoesNotValidate();
                this.uut.ShutdownCommand = null;
                this.DoesNotValidate();
                this.uut = new PictureFrameConfig();
            }

            // Empty reboot command should not validate.
            {
                this.uut.RebootCommand = string.Empty;
                this.DoesNotValidate();
                this.uut.RebootCommand = null;
                this.DoesNotValidate();
                this.uut = new PictureFrameConfig();
            }


            // Empty exit command should not validate.
            {
                this.uut.ExitToDesktopCommand = string.Empty;
                this.DoesNotValidate();
                this.uut.ExitToDesktopCommand = null;
                this.DoesNotValidate();
                this.uut = new PictureFrameConfig();
            }

            // Negative Port should not validate.
            {
                this.uut.Port = -1;
                this.DoesNotValidate();
                this.uut = new PictureFrameConfig();
            }

            // Brightness over 100 should not validate.
            {
                this.uut.Brightness = 101;
                this.DoesNotValidate();
                this.uut = new PictureFrameConfig();

                this.uut.Brightness = 100;
                this.Validates();
                this.uut = new PictureFrameConfig();
            }
        }

        /// <summary>
        /// Ensures our sample XML matches the default config.
        /// </summary>
        [Test]
        public void DefaultConfigTest()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load( Path.Combine( "config", "SampleUserConfig.xml" ) ); // Copied over.

            this.uut.PhotoDirectory = this.uut.PhotoDirectory.Replace( "\\", "/" ); // Consistent slashes.

            PictureFrameConfig config = PictureFrameConfig.FromXml( doc.DocumentElement );
            Assert.AreEqual( this.uut, config );
        }

        // -------- Test Helpers --------

        /// <summary>
        /// Ensures the uut validates.
        /// </summary>
        private void Validates()
        {
            string errorString;
            Assert.IsTrue( this.uut.TryValidate( out errorString ) );
            Assert.DoesNotThrow( () => this.uut.Validate() );
            Assert.IsEmpty( errorString );
        }

        /// <summary>
        /// Ensures the uut does NOT validate.
        /// </summary>
        private void DoesNotValidate()
        {
            string errorString;
            Assert.IsFalse( this.uut.TryValidate( out errorString ) );
            Assert.Throws<ValidationException>( () => this.uut.Validate() );
            Assert.IsNotEmpty( errorString );
        }
    }
}
