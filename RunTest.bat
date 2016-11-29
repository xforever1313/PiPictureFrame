:: Create CodeCoverage directory.
if not exist CodeCoverage mkdir CodeCoverage

:: Remove old files
del /Q CodeCoverage\*

::Run NUnit via OpenCover.  Output to the CodeCoverage directory.
packages\OpenCover.4.6.519\tools\OpenCover.Console.exe -register:user -filter:+[*]PiPictureFrame.Core* -returntargetcode -searchdirs:UnitTests\bin\Debug -target:packages\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe -targetargs:"--result=CodeCoverage\TestResult.xml UnitTests\bin\Debug\UnitTests.dll" -output:CodeCoverage\coverage.xml

::Open ReportGenerator
packages\ReportGenerator.2.5.1\tools\ReportGenerator.exe -reports:CodeCoverage\coverage.xml -targetdir:CodeCoverage