
//          Copyright Seth Hendrick 2016.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file ../LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)

using System;
using System.Collections.Generic;
using System.IO;
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
        private List<string> pictures;

        private object picturesLock;

        private string currentPicture;

        private Random random;

        private static readonly List<string> acceptedFileExtensions = new List<string>()
        {
            "jpg",
            "jpeg",
            "png",
            "gif",
            "tiff"
        };      

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.
        /// </summary>
        public PictureListManager()
        {
            this.pictures = new List<string>();
            this.picturesLock = new object();
            this.currentPicture = string.Empty;
            this.random = new Random();
        }

        // ---------------- Properties ----------------
        
        /// <summary>
        /// Path to the photo that should be displayed.
        /// </summary>
        public string CurrentPicture
        {
            get
            {
                lock( this.picturesLock )
                {
                    return currentPicture;
                }
            }

            private set
            {
                lock( this.picturesLock )
                {
                    this.currentPicture = value;
                }
            }
        }

        /// <summary>
        /// Number of photos found.
        /// </summary>
        public int FoundPhotos
        {
            get
            {
                lock( this.picturesLock )
                {
                    return this.pictures.Count;
                }
            }
        }
        // ---------------- Functions ----------------

        /// <summary>
        /// Clears all the pictures from the list.
        /// </summary>
        public void Clear()
        {
            lock( this.picturesLock )
            {
                this.pictures.Clear();
            }
        }

        /// <summary>
        /// Finds all the pictures in the given directory,
        /// Any file that ends in .jpg, .jpeg, .png, .tiff, .gif are
        /// added.
        /// 
        /// Runs in current thread.
        /// </summary>
        public void Load( string path )
        {
            List<string> newPics = FindPictures( path );
            lock( this.picturesLock )
            {
                this.pictures.Clear();
                this.pictures = newPics;
                this.NextPictureNoLock();
            }
        }

        /// <summary>
        /// Updates this.CurrentPicture to a new picture on the file system.
        /// </summary>
        public void NextPicture()
        {
            lock( this.picturesLock )
            {
                this.NextPictureNoLock();
            }
        }

        /// <summary>
        /// Updates this.CurrentPicture to a new picture on the file system
        /// without a lock.
        /// </summary>
        private void NextPictureNoLock()
        {
            do
            {
                int index = this.random.Next( 0, this.pictures.Count );
                this.currentPicture = this.pictures[index];
            }
            while( File.Exists( this.currentPicture ) == false );
        }

        private List<string> FindPictures( string path )
        {
            List<string> pictures = new List<string>();

            string[] files = Directory.GetFiles(
                path,
                "*",
                SearchOption.AllDirectories
            );

            foreach( string file in files )
            {
                foreach( string ext in acceptedFileExtensions )
                {
                    if( file.ToLower().EndsWith( ext ) )
                    {
                        pictures.Add( file );
                        break;
                    }
                }
            }

            return pictures;
        }
    }
}
