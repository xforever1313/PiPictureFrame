Pi Picture Frame ==========A way to turn your Raspberry Pi 3 into a digital picture frame!Build Status--------------[![Build Status](https://travis-ci.org/xforever1313/PiPictureFrame.svg?branch=master)](https://travis-ci.org/xforever1313/PiPictureFrame)Installing--------------When using the Raspberry Pi official touchscreen, you need the latest drivers by running: * sudo rpi-updateDependencies:----------Download the following packages using your favorite package manager: * gtk3 * nuget * git * matchbox-keyboard (Optional: useful for touchscreen however) * xscreensaver (To prevent the screen from turning off).Installing Mono:-----Raspbian does not have an up-to-date version of Mono.  Get the most up-to-date by running the following commands:Taken from: [http://www.mono-project.com/docs/getting-started/install/linux/](http://www.mono-project.com/docs/getting-started/install/linux/)``` - sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
 - echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
 - sudo apt-get update
 - sudo apt-get install mono-complete```Building:----------
 * git clone https://github.com/xforever1313/PiPictureFrame.git
 * git submodule update --init SethCS
 * cd PiPictureFrame
 * nuget restore ./PiPictureFrame.sln
 * xbuild /p:Configuration=Release ./PiPictureFrame.sln
 * cd PiPictureFrame.Cli/bin/Release
 * mono PiPictureFrame.Cli.exe # Should fail, as the user is not set up to use port 80.  However, a config will appear in /home/pi/.config/PiPictureFrame

Configuration:----------Configuration is located in C:\UserName\AppData\PiPictureFrame on Windows.  On linux, this is in /home/userName/.config/PiPictureFrame.```<?xml version="1.0" encoding="UTF-8"?>
<pictureframeconfig>
  <sleeptime hour="-1" minute="-1" />
  <awaketime hour="-1" minute="-1" />
  <photodirectory>D:\Seth\Pictures</photodirectory>
  <refreshinterval>3600</refreshinterval>
  <photochangeinterval>10</photochangeinterval>
  <shutdowncommand>shutdown -Ph now</shutdowncommand>
  <rebootcommand>reboot</rebootcommand>
  <exittodesktopcommand>echo "Exiting"</exittodesktopcommand>
  <httpport>10013</httpport>
  <brightness>75</brightness>
</pictureframeconfig>
```

 * sleeptime: Time to turn off the screen.  Hour is 0-23, minute is 0-59.  -1 on both is NEVER turn off the screen.
 * awaketime: Time to turn on the screen.  Hour is 0-23, minute is 0-59.  -1 on both is NEVER turn on the screen.
 * photodirectory: Where we search for photos (recursively).
 * refreshinterval: How often to check for new photos in SECONDS.  Ignored in this release.
 * photochangeinterval: How often to change the photo on the screen in SECONDS.
 * shutdowncommand: Command that is run when a user presses the shutdown button.
 * rebootcommand: Command that is run when a user presses the reboot button
 * exittodesktopcommand: Command that is run when a user presses the "Exit to Desktop" button.
 * httpport: Port to run the HTTP server on.
 * brightness: Scale from 1-100 on how bright the screen should be.

Auto Login:-----This is how to auto-login to the desktop so the user can start the frame:Edit /etc/lightdm/lightdm.confThis should be there by default, but if its not:```"autologin-user=userName"```Security:-----Change Root Password: * sudo su * passwd * change password * exitForce key login for SSH:Edit /etc/ssh/sshd_config```PermitRootLogin no
PasswordAuthentication no
Port XXXX  # Change port if desired
```