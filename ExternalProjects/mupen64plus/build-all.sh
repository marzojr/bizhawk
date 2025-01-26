#!/bin/sh
set -e

# TODO: .dll is hardcoded, make it support .so as well
# this also requires patching some dependencies, like mupen64plus-core links system SDL2, so we'll want to patchelf --replace-needed libSDL2-2.0.so.0 libSDL2.so

install_to_bizhawk () {
	strip -p "$1"
	cp "$1" ../../Assets/dll/
	if [ -d ../../output/dll ]; then
		cp "$1" ../../output/dll/
	fi
}

rm -r mupen64plus-core/projects/unix/_obj 2> /dev/null || true
# do we want ACCURATE_FPU=1 here?
make -C mupen64plus-core/projects/unix VULKAN=0 NEW_DYNAREC=1 OSD=0 ACCURATE_FPU=0 KEYBINDINGS=0 DEBUGGER=1 all -j4
install_to_bizhawk mupen64plus-core/projects/unix/mupen64plus.dll

make -C mupen64plus-audio-bkm
install_to_bizhawk mupen64plus-audio-bkm/mupen64plus-audio-bkm.dll

make -C mupen64plus-input-bkm
install_to_bizhawk mupen64plus-input-bkm/mupen64plus-input-bkm.dll

rm -r mupen64plus-rsp-cxd4/projects/unix/_obj-sse2 2> /dev/null || true
make -C mupen64plus-rsp-cxd4/projects/unix all -j4
install_to_bizhawk mupen64plus-rsp-cxd4/projects/unix/mupen64plus-rsp-cxd4-sse2.dll

rm -r mupen64plus-rsp-hle/projects/unix/_obj 2> /dev/null || true
make -C mupen64plus-rsp-hle/projects/unix all -j4
install_to_bizhawk mupen64plus-rsp-hle/projects/unix/mupen64plus-rsp-hle.dll

rm -r build/rsp-parallel && mkdir -p build/rsp-parallel
cmake -S mupen64plus-rsp-parallel -B build/rsp-parallel -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/rsp-parallel
install_to_bizhawk build/rsp-parallel/mupen64plus-rsp-parallel.dll

rm -r build/video-angrylion-plus && mkdir -p build/video-angrylion-plus
cmake -S mupen64plus-video-angrylion-plus -B build/video-angrylion-plus -G Ninja -DCMAKE_BUILD_TYPE=MinSizeRel
cmake --build build/video-angrylion-plus
install_to_bizhawk build/video-angrylion-plus/mupen64plus-video-angrylion-plus.dll

rm -r build/video-GLideN64 && mkdir -p build/video-GLideN64
cmake -S mupen64plus-video-GLideN64/src -B build/video-GLideN64 -G Ninja -DCMAKE_BUILD_TYPE=Release -DVEC4_OPT=On -DCRC_OPT=On -DNOHQ=On -DMUPENPLUSAPI=On -DNO_OSD=On
cmake --build build/video-GLideN64
install_to_bizhawk build/video-GLideN64/mupen64plus-video-GLideN64.dll

# TODO: this submodule will NOT build a proper working dll, current source used: https://github.com/Rosalie241/RMG/tree/master/Source/3rdParty/mupen64plus-video-parallel
rm -r build/video-parallel && mkdir -p build/video-parallel
cmake -S mupen64plus-video-parallel -B build/video-parallel -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/video-parallel
install_to_bizhawk build/video-parallel/mupen64plus-video-parallel.dll
