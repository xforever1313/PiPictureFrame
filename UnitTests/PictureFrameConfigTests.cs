using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PiPictureFrame.Core;

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
        }
    }
}
