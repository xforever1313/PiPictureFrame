###
# Compile
###

git submodule update --init SethCS
nuget restore PiPictureFrame.sln
xbuild /p:Configuration=Release PiPictureFrame.sln

###
# Move Files
###
mkdir -p ~/.config/PiPictureFrame/
cp -n ./PiPictureFrame/config/SampleUserConfig.xml ~/.config/PiPictureFrame/UserConfig.xml

cp -u Desktop/*.sh ~/Desktop

ln -s  `pwd`/PiPictureFrame.Cli/bin/Release/ ~/PiPictureFrame

