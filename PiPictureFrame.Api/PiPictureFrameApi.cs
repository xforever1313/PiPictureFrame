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

using Microsoft.Extensions.Logging;

namespace PiPictureFrame.Api
{
    public sealed class PiPictureFrameApi : IDisposable
    {
        // ---------------- Constructor ----------------

        public PiPictureFrameApi()
        {
        }

        // ---------------- Properties ----------------

        public IRenderer Renderer { get; private set; }

        public IScreen Screen { get; private set; }

        public ILogger Log { get; private set; }

        // ---------------- Functions ----------------

        public void Init()
        {
        }

        public void Dispose()
        {
        }
    }
}