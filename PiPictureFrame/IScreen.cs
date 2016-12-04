
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

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
