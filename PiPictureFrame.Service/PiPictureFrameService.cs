
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using PiPictureFrame.Core;

namespace PiPictureFrame.Service
{
    public partial class PiPictureFrameService : ServiceBase
    {
        // ---------------- Fields ----------------

        private PictureFrame frame;

        private Task frameTask;

        // ---------------- Constructor ----------------

        public PiPictureFrameService()
        {
            InitializeComponent();
        }

        // ---------------- Functions ----------------

        protected override void OnStart( string[] args )
        {
            this.frame = new PictureFrame();
            this.frameTask = this.frame.RunAsync();
        }

        protected override void OnStop()
        {
            this.frame?.Dispose();
            this.frameTask?.Wait( 10 * 1000 );
        }
    }
}
