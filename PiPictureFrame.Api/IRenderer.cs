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
    /// <summary>
    /// How to render pictures.
    /// </summary>
    public interface IRenderer
    {
        // ---------------- Properties ----------------

        /// <summary>
        /// Returns the current picture path.
        /// </summary>
        string CurrentPicturePath { get; }

        // ---------------- Functions ----------------

        /// <summary>
        /// Inits the renderer.
        /// </summary>
        /// <param name="pictureDirectory">Where the picture directory is.</param>
        void Init( string pictureDirectory );

        /// <summary>
        /// Tells the renderer to go to the next picture.
        /// </summary>
        void GoToNextPicture();
    }
}
