import os
import sys
import shutil

def main(args):
    cs_dir = os.path.join('output', 'csharp', 'SdlDemo')
    assets_dir = os.path.join('assets', 'csharp')

    include_bridge = len(args) == 1 and args[0] == 'full'

    for file in [
        'CrayonSdlBridge.cs',
        'Interpreter.csproj',
        'NativeTunnelSdl.cs',
        'Program.cs',
        'SDL2_image.cs',
        'SDL2_mixer.cs',
        'SDL2_ttf.cs',
        'SDL2.cs',
        'SDL2.dll',
        'libjpeg-9.dll',
        'libpng16-16.dll',
        'libtiff-5.dll',
        'libwebp-7.dll',
        'SDL2_image.dll',
        'zlib1.dll',
    ]:
        if file != 'CrayonSdlBridge.cs' or include_bridge:
            shutil.copyfile(os.path.join(assets_dir, file), os.path.join(cs_dir, file))

    print("Done")

main(sys.argv[1:])
