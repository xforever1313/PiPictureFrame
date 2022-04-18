//
// PiPictureFrame - Digital Picture Frame built for the Raspberry Pi.
// Copyright (C) 2022 Seth Hendrick
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

namespace PiPictureFrame.Api
{
    public interface IScreen
    {
        // ------------ Properties ------------

        /// <summary>
        /// The brightness of the screen on
        /// a scale of 0-100.
        /// </summary>
        byte Brightness { get; set; }

        /// <summary>
        /// Whether or not the screen is on.
        /// </summary>
        bool IsOn { get; set; }

        // ---------------- Functions ----------------

        /// <summary>
        /// Populates the properties by reading from the OS.
        /// </summary>
        void Refresh();
    }
}
