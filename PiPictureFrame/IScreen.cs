using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiPictureFrame.Core
{
    /// <summary>
    /// Represents screens to run.
    /// </summary>
    public interface IScreen
    {
        // -------- Properties --------

        /// <summary>
        /// The brightness of the screen on
        /// a scale of 0-100.
        /// </summary>
        short Brightness { get; set; }

        /// <summary>
        /// Whether or not the screen is on.
        /// </summary>
        bool IsOn { get; set; }

        // -------- Functions --------

        /// <summary>
        /// Populates the properties by reading from the OS.
        /// </summary>
        void Refresh();
    }
}
