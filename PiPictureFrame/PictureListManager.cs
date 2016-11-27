using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiPictureFrame.Core
{
    /// <summary>
    /// Manages the pictures to find.
    /// </summary>
    public class PictureListManager
    {
        // ---------------- Fields ----------------

        /// <summary>
        /// Fields to look into.
        /// </summary>
        private DataTable picTable;

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.
        /// </summary>
        public PictureListManager()
        {
            this.picTable = new DataTable( "Pictures" );
        }

        // ---------------- Functions ----------------

        /// <summary>
        /// Clears all the pictures from the list.
        /// </summary>
        public void Clear()
        {
            this.picTable.Clear();
        }

        /// <summary>
        /// Finds all the pictures (recusively) in the given directories.
        /// Any file 
        /// </summary>
        /// <param name="dirs"></param>
        public void Load( IList<string> dirs )
        {
            foreach( string dir in dirs )
            {
            }
        }

        public string NextPicture()
        {
            return string.Empty;
        }
    }
}
