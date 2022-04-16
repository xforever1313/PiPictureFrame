const string buildTarget = "build";
const string testTarget = "run_unit_tests";

string target = Argument( "target", buildTarget );

FilePath sln = File( "PiPictureFrame.sln" );

Task( buildTarget )
.Does(
    () =>
    {
        var settings = new DotNetBuildSettings
        {
            Configuration = "Debug"
        };

        DotNetBuild( sln.ToString(), settings );
    }
).Description( "Builds for debug" );

RunTarget( target );
