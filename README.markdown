# NVidiaOptimusFixer

Website: https://github.com/Dwedit/NVidiaOptimusFixer

NVidia mobile graphics adapters such as the GeForce GTX 960M suffer from a [Diagonal Tearing Problem](https://forums.geforce.com/default/topic/903422/geforce-mobile-gpus/diagonal-screen-tearing-issues-on-gtx-860m-870m-960m-965m-970m-980m-/) when playing DirectX 9 and OpenGL games.
There is a synchronization problem involving the graphics driver and Desktop Window Manager (DWM).  NVidia blames Microsoft, and Microsoft blames NVidia, and nothing is getting done to fix this problem.
It just so happens that calling GDI GetPixel on the main screen forces a vblank wait, and if IDirect3DDevice9->Present is called immediately afterwards, the display is synchronized properly without diagonal tearing.

This is a program that hooks into Direct3D 9 games, and forces the game to call GDI GetPixel before calling IDirect3DDevice9->Present.  However, this reduces performance.  If a game can't reach 60FPS, it will go down to 30FPS.

Although this is not a cheating program in any way whatsoever, I'm not responsible if Valve decides to VAC-ban you for using this.  It hooks into other processes and overrides the behavior of Direct3D 9, just like other programs like Fraps.

Based on the project https://github.com/spazzarama/Direct3DHook

## Known Issues

* Performance sucks if second monitor has been connected at any point since reboot:

** If you are using a second monitor, you probably don't need this program. But you should reboot and try again, then it should run faster.
