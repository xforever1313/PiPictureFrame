# Maintainer: Seth Hendrick <seth@shendrick.net>
pkgname=pipictureframe
pkgver=0.1.0
pkgrel=1
epoch=
pkgdesc="Raspberry Pi Picture Frame."
arch=('any')
url="https://github.com/xforever1313/PiPictureFrame"
license=('BSL')
groups=()
depends=('mono>=4.2.2' 'git' 'nuget')
makedepends=()
checkdepends=()
optdepends=()
provides=()
conflicts=()
replaces=()
backup=(
    "home/picframe/.config/PiPictureFrame/UserConfig.xml"
)
options=()
install=
changelog=
source=("https://github.com/xforever1313/PiPictureFrame/archive/master.tar.gz")
noextract=()
md5sums=('SKIP')
validpgpkeys=()

prepare() {
    echo "Nothing to prepare"
}

build() {
        cd "$srcdir/PiPictureFrame-master"
        git clone https://github.com/xforever1313/sethcs SethCS
        nuget restore ./PiPictureFrame.sln
        xbuild /p:Configuration=Release ./PiPictureFrame.sln
}

check() {
}

package() {

        cd "$srcdir/PiPictureFrame-master"

        #Where everything will be installed
        installLocation=/home/picframe/

        #Where the exe files will go.
        exeLocation=$installLocation/bin/

        #Where the config stuff goes.  Should be the same as
        #where mono puts application data.
        configLocation=$installLocation/.config/PiPictureFrame

        # Copy all the things!

        # Start with the executable and main dlls.
        mkdir -p $pkgdir/$exeLocation
        cp ./PiPictureFrame.Service/bin/Release/PiPictureFrame.Service.exe $pkgdir/$exeLocation
        cp ./PiPictureFrame.Service/bin/Release/PiPictureFrame.Service.exe.config $pkgdir/$exeLocation
        cp ./PiPictureFrame.Service/bin/Release/PiPictureFrame.Core.dll $pkgdir/$exeLocation

        # Next is the configs.
        mkdir -p $pkgdir/$configLocation
        cp ./PiPictureFrame.Core/Config/SampleUserConfig.xml $pkgdir/$configLocation/UserConfig.xml
}

pre_install() {
    id -u picframe &> /dev/null || useradd -m picframe

    # Enables picframe to shutdown the system.
    usermod --groups -a power picframe
}

post_remove() {
    userdel picframe
}
