{ lib
, stdenv
# infrastructure
, populateHawkSourceInfo
, replaceDotWithUnderscore
, buildDotnetModule
, fetchpatch
, fetchzip
, hardLinkJoin
, launchScriptsFor
, makeDesktopItem
, releaseTagSourceInfos
, runCommand
, symlinkJoin
, writeShellScriptBin
# makedeps
, git
# rundeps
, gnome-themes-extra
, gtk2-x11
, libgdiplus
, libGL
, lua
, mono
, monoBasic
, openal
, SDL2
, udev
, zstd
# other parameters
, buildConfig
, doCheck
, extraDefines
, extraDotnetBuildFlags
}: let
	getMainOutput = lib.getOutput "out";
	/** to override just one, you'll probably want to manually import packages-managed.nix, and combine that with the output of this */
	buildExtraManagedDepsFor = hawkSourceInfo: let
		pm = import ./packages-managed.nix {
			inherit lib
				buildDotnetModule runCommand
				hawkSourceInfo;
		};
	in symlinkJoin {
		name = "bizhawk-managed-deps";
		paths = builtins.map (s: pm.${s}) (lib.sort lib.lessThan hawkSourceInfo.neededExtraManagedDeps);
	};
	/**
	 * NOTE: This impl. is unfinished, only useful for checking that overriding is possible.
	 * For actually overriding `extraUnmanagedDeps`, roll your own `symlinkJoin` impl. or something.
	 *
	 * TODO replace this with something more like `buildExtraManagedDepsFor`; for existing Nix exprs see https://gitlab.com/YoshiRulz/yoshis-hawk-thoughts/-/issues/10
	 */
	buildUnmanagedDepsFor = hawkSourceInfo: stdenv.mkDerivation {
		inherit (hawkSourceInfo) src version;
		pname = "bizhawk-native-deps";
		dontBuild = true;
		installPhase = ''
			runHook preInstall

			mkdir -p $out; cp -vt $out Assets/dll/*.so
			chmod -x $out/*

			runHook postInstall
		'';
		dontFixup = true;
	};
	genDepsHostTargetFor = { hawkSourceInfo, gtk2-x11' ? getMainOutput gtk2-x11, mono' ? mono }: [
		gtk2-x11'
		(getMainOutput libgdiplus)
		lua
		mono'
		openal
		(getMainOutput zstd)
	] ++ lib.optionals hawkSourceInfo.needsSDL [ SDL2 (getMainOutput udev) ]
		++ lib.optional hawkSourceInfo.needsLibGLVND (getMainOutput libGL);
	/**
	 * see splitReleaseArtifact re: outputs
	 * and no you can't build only DiscoHawk and its deps; deal with it
	 */
	buildAssembliesFor = hawkSourceInfo': let
		hawkSourceInfo = populateHawkSourceInfo hawkSourceInfo';
		extraManagedDeps = hawkSourceInfo.extraManagedDeps or buildExtraManagedDepsFor hawkSourceInfo;
	in buildDotnetModule (lib.fix (finalAttrs: { # proper `finalAttrs` not supported >:(
		inherit doCheck gnome-themes-extra mono;
		inherit (hawkSourceInfo) __contentAddressed dotnet-sdk nugetDeps src version;
		pname = "BizHawk";
		isLocalBuild = lib.hasSuffix "-local" finalAttrs.version;
		postUnpack = lib.optionalString finalAttrs.isLocalBuild ''(cd BizHawk-*-local; Dist/CleanupBuildOutputDirs.sh)'';
		outputs = [ "out" "assets" "extraUnmanagedDeps" "waterboxCores" ];
		propagatedBuildOutputs = []; # without this, other outputs depend on `out`
		strictDeps = true;
		nativeBuildInputs = lib.optional finalAttrs.doCheck finalAttrs.mono
			++ lib.optional finalAttrs.isLocalBuild git;
		gtk2-x11 = getMainOutput gtk2-x11;
		buildInputs = genDepsHostTargetFor {
			inherit hawkSourceInfo;
			gtk2-x11' = finalAttrs.gtk2-x11;
			mono' = finalAttrs.mono;
		};
		patches = lib.optional (!hawkSourceInfo.hasMiscTypeCheckerPatch_6afb3be98) (fetchpatch {
			url = "https://github.com/TASEmulators/BizHawk/commit/6afb3be98cd3d8bf111c8e61fdc29fc3136aab1e.patch";
			hash = "sha512-WpLGbng7TkEHU6wWBfk0ogDkK7Ub9Zh5PKkIQZkXDrERkEtQKrdTOOYGhswFEfJ0W4Je5hl5iZ6Ona140BxhhA==";
		});
		postPatch = ''
			sed -e 's/SimpleSubshell("uname", "-r", [^)]\+)/"${builtins.toString stdenv.hostPlatform.uname.release}"/' \
				-e 's/SimpleSubshell("uname", "-s", [^)]\+)/"${builtins.toString stdenv.hostPlatform.uname.system}"/' \
				-i src/BizHawk.Common/OSTailoredCode.cs

			sed '/Assets\/\*\*/d' -i ${if hawkSourceInfo.copyingAssetsInEmuHawkProj # stop MSBuild from copying Assets, we'll do that in installPhase
				then "src/BizHawk.Client.EmuHawk/BizHawk.Client.EmuHawk.csproj"
				else "src/BizHawk.Common/BizHawk.Common.csproj"}

			rm -fr References; cp -LrT '${extraManagedDeps}' References
		'';
		buildType = buildConfig; #TODO move debug symbols to `!debug`?
		extraDotnetBuildFlags = let
			s = lib.optionalString (extraDefines != "") "-p:MachineExtraCompilationFlag=${extraDefines} ";
		in "-maxcpucount:$NIX_BUILD_CORES -p:BuildInParallel=true --no-restore -v normal -p:ContinuousIntegrationBuild=true"
			+ " -p:DebugType=portable ${s}${extraDotnetBuildFlags}";
		buildPhase = ''
			runHook preBuild

			${if hawkSourceInfo.versionProjNeedsDoubleBuild then
			''cd src/BizHawk.Version
			dotnet build ${finalAttrs.extraDotnetBuildFlags}
			cd ../..
			'' else ""}Dist/Build${finalAttrs.buildType}.sh ${finalAttrs.extraDotnetBuildFlags}

			runHook postBuild
		'';
		checkNativeInputs = finalAttrs.buildInputs; # doesn't work???
		checkPhase = ''
			runHook preCheck

			export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:${lib.makeLibraryPath finalAttrs.checkNativeInputs}"
			${if hawkSourceInfo.testProjectNeedsCIEnvVar then ''export GITLAB_CI=1
			'' else ""}Dist/BuildTest${finalAttrs.buildType}.sh ${finalAttrs.extraDotnetBuildFlags}

			# can't build w/ extra Analyzers, it fails to restore :(
			Dist/Build${finalAttrs.buildType}.sh -p:MachineRunAnalyzersDuringBuild=true ${finalAttrs.extraDotnetBuildFlags}

			runHook postCheck
		'';
		installPhase = ''
			runHook preInstall

			${if hawkSourceInfo.packageScriptRemovesWinFilesFromLinux then "" else
			''(
				cd Assets/dll;
				rm -f "lua54.dll" \
					"mupen64plus-audio-bkm.dll" "mupen64plus-input-bkm.dll" "mupen64plus-rsp-cxd4-sse2.dll" "mupen64plus-rsp-hle.dll" "mupen64plus-rsp-parallel" "mupen64plus-video-angrylion-plus.dll" "mupen64plus-video-GLideN64.dll" "mupen64plus-video-parallel.dll" "mupen64plus.dll" "octoshock.dll" \
					"bizlynx.dll" "bizswan.dll" "blip_buf.dll" "libbizhash.dll" "libdarm.dll" "libemu83.dll" "libfwunpack.dll" "libgambatte.dll" "libLibretroBridge.dll" "libquicknes.dll" "librcheevos.dll" "libsameboy.dll" "mgba.dll" "MSXHawk.dll" "waterboxhost.dll"
			)
			''}rm Assets/EmuHawkMono.sh # replaced w/ launch-scripts.nix
			cp -avT Assets $assets
			${if hawkSourceInfo.hasAssetsInOutput then ''sed '/packaged_output\/dll/d' -i Dist/Package.sh
			rm output/dll/*.xml # that's what that Package.sh line did, though I've forgotten why and don't care to rediscover it
			mv -t $assets/dll output/dll/*
			'' else ""}chmod -x $assets/dll/*.so # no idea why these are all executable

			mkdir -p $extraUnmanagedDeps/lib; mv -t $extraUnmanagedDeps/lib $assets/dll/*.so*
			mkdir -p $waterboxCores/dll; mv -t $waterboxCores/dll $assets/dll/*.wbx*

			${if hawkSourceInfo.packageScriptNeeds7Zip then ''sed '/7za a -t7z -mx9/d' -i Dist/Package.sh # not worth figuring this out
			'' else ""}Dist/Package.sh linux-x64
			${if hawkSourceInfo.hasAssetsInOutput then ''mkdir packaged_output/dll
			'' else ""}mkdir -p $out; cp -avt $out packaged_output/*.exe* packaged_output/dll
			mv -t $out/dll $assets/dll/*
			mv -t $out $assets/defctrl.json $assets/gamedb $assets/Shaders
			printf '${hawkSourceInfo.frontendPackageFlavour}' >$out/dll/custombuild.txt

			runHook postInstall
		'';
		dontPatchELF = true;
		passthru = {
			inherit extraManagedDeps # could use this to backport changes to ExternalProjects? IDK
				hawkSourceInfo; # simple way to override `nugetDeps` for patching: `buildAssembliesFor (bizhawkAssemblies-latest.hawkSourceInfo // { nugetDeps = /*...*/; })`
			inherit (finalAttrs) gnome-themes-extra mono;
#			extraUnmanagedDeps = buildUnmanagedDepsFor hawkSourceInfo; # this will override the output of the same name, example: `buildEmuHawkInstallableFor { bizhawkAssemblies = bizhawkAssemblies-latest // { extraUnmanagedDeps = /*...*/; }; }`
			# can similarly override `assets` output, only used by launch script to populate `BIZHAWK_DATA_HOME` if the dir doesn't exist at runtime,
			# and `waterboxCores` output, which holds just the Waterbox cores, as the name suggests
		};
		meta.sourceProvenance = [ lib.sourceTypes.binaryNativeCode ]; # `extraUnmanagedDeps` and `waterboxCores` outputs; will work on from-source later
	}));
	buildInstallable =
		{ bizhawkAssemblies
		, pname
		, launchScript
		, desktopName
		, desktopIcon
		}: let
			exeName = "${pname}-${bizhawkAssemblies.hawkSourceInfo.version}";
		in symlinkJoin {
			inherit (bizhawkAssemblies.hawkSourceInfo) __contentAddressed;
			name = exeName;
			paths = [
				(let
					# in versions < 2.9.2, Waterbox cores load from `Assembly.GetEntryAssembly().Location + "/../dll"`, but `Location` resolves symlinks so only `!bin` is visible -_-
					farm = (if bizhawkAssemblies.hawkSourceInfo.exePathRespectsEnvVar then symlinkJoin else hardLinkJoin) {
						inherit (bizhawkAssemblies.hawkSourceInfo) __contentAddressed;
						name = "bizhawk-asms-and-wbox";
						paths = [ bizhawkAssemblies bizhawkAssemblies.waterboxCores ];
					};
				in writeShellScriptBin exeName ''BIZHAWK_HOME='${farm}' exec '${launchScript}' "$@"'')
				(makeDesktopItem {
					inherit desktopName; # actually Name
					exec = exeName;
					icon = desktopIcon;
					name = exeName; # actually filename
				})
			];
			passthru = {
				inherit (bizhawkAssemblies) fetch-deps hawkSourceInfo;
				assemblies = bizhawkAssemblies;
			};
			meta = let
				p = lib.systems.inspect.patterns;
			in {
				platforms = [
					(p.isLinux // p.isAarch64)
					(p.isLinux // p.isx86_32)
					(p.isLinux // p.isx86_64)

					# `isMacOS` seems to be unused in Nixpkgs, though most mentions of `isDarwin` aren't from `lib.systems.inspect.patterns` so maybe it's only right for the legacy method
					# won't bother w/ PPC or x86 Macs, they're too weak
					(p.isDarwin // p.isAarch64)
					(p.isDarwin // p.isx86_64)

					# not sure where Nix' Windows support is at right now, whether isWindows is the relevant pattern, or whether Nix on Windows will be suitable for us once it is usable
#					(p.isWindows // p.isAarch64)
#					(p.isWindows // p.isx86_32)
#					(p.isWindows // p.isx86_64)
				];
				badPlatforms = [ p.isDarwin p.isAarch64 p.isx86_32 ];
				mainProgram = exeName;
			};
		};
	buildInstallable' =
		{ hawkSourceInfo
		, bizhawkAssemblies
		, forNixOS
		, pname
		, launchScriptAttrName
		, desktopName
		, desktopIcon
		}: let
			bizhawkAssembliesFinal = if bizhawkAssemblies != null
				then lib.traceIf (hawkSourceInfo != null) "`hawkSourceInfo` passed to `build{EmuHawk,DiscoHawk}InstallableFor` will be ignored in favour of `bizhawkAssemblies.hawkSourceInfo`"
					bizhawkAssemblies
				else assert lib.assertMsg (hawkSourceInfo != null) "must pass either `hawkSourceInfo` or `bizhawkAssemblies` to `build{EmuHawk,DiscoHawk}InstallableFor`";
					buildAssembliesFor (lib.optionalAttrs forNixOS { needsLibGLVND = true; } // hawkSourceInfo);
		in buildInstallable {
			inherit desktopName pname;
			bizhawkAssemblies = bizhawkAssembliesFinal;
			desktopIcon = if desktopIcon != null then desktopIcon else pname;
			launchScript = (launchScriptsFor bizhawkAssembliesFinal false).${launchScriptAttrName};
		};
in {
	inherit buildAssembliesFor buildExtraManagedDepsFor buildUnmanagedDepsFor;
	/**
	 * assemblies and (managed) dependencies, and some other immutable things like the gamedb, are in the `out` output;
	 * unmanaged/native dependencies are in the `extraUnmanagedDeps` output (under `/lib`);
	 * Waterbox cores are in the `waterboxCores` output (under `/dll`);
	 * the rest of the "assets" (bundled scripts, palettes, etc.) are in the `assets` output
	 */
	splitReleaseArtifact =
		{ hawkSourceInfo
		, hashPrePatching
		, crossPlatformArtifact ? false
		, zippedTarball ? false
		, url ? "https://github.com/TASEmulators/BizHawk/releases/download/${hawkSourceInfo.version}/BizHawk-${hawkSourceInfo.version}${if crossPlatformArtifact then ".zip" else if zippedTarball then "-linux-x64.tar.zip" else "-linux-x64.tar.gz"}"
		, stripRoot ? true
		}: assert buildConfig == "Release"; let
			artifact = fetchzip { inherit stripRoot url; hash = hashPrePatching; };
			hawkSourceInfo' = populateHawkSourceInfo hawkSourceInfo;
		in runCommand "BizHawk-${hawkSourceInfo.version}-bin" {
			buildInputs = genDepsHostTargetFor { hawkSourceInfo = hawkSourceInfo'; }; # is using `buildInputs` like this correct? it's necessary because the launch script reads from it
			outputs = [ "out" "assets" "extraUnmanagedDeps" "waterboxCores" ];
			passthru = {
				inherit gnome-themes-extra mono;
				hawkSourceInfo = hawkSourceInfo';
			};
			meta.sourceProvenance = [ lib.sourceTypes.binaryNativeCode lib.sourceTypes.binaryBytecode ];
		} ''
			${if zippedTarball then ''mkdir -p $assets; tar -xf '${artifact}'/*.tar -C $assets'' else ''cp -aT '${artifact}' $assets''}
			cd $assets
			find . -type d -exec chmod +w {} \;
			rm -f EmuHawkMono.sh
			${if hawkSourceInfo'.releaseArtifactHasRogueOTKAsmConfig then ''mv -ft dll OpenTK.dll.config
			'' else ""}rmdir Firmware
			mkdir -p ExternalTools; touch ExternalTools/.keep

			mkdir -p $out; mv -t $out defctrl.json DiscoHawk.exe* dll EmuHawk.exe* gamedb [Ss]haders
			${if hawkSourceInfo'.releaseArtifactNeedsVBDotnetReference then ''cp -t $out/dll '${getMainOutput monoBasic}/usr/lib/mono/4.5/Microsoft.VisualBasic.dll'
			'' else ""}${if hawkSourceInfo'.releaseArtifactNeedsLowercaseAsms then ''(cd $out/dll; for s in Client.Common Emulation.Cores; do cp BizHawk.$s.dll Bizhawk.$s.dll; done)
			'' else ""}${if hawkSourceInfo'.releaseArtifactNeedsOTKAsmConfig then ''cp -t $out/dll '${releaseTagSourceInfos.info-2_6.src}/Assets/dll/OpenTK.dll.config'
			'' else ""}printf '${hawkSourceInfo'.frontendPackageFlavour}' >$out/dll/custombuild.txt

			mkdir -p $extraUnmanagedDeps/lib; mv -t $extraUnmanagedDeps/lib $out/dll/*.so*
			mkdir -p $waterboxCores/dll; mv -t $waterboxCores/dll $out/dll/*.wbx*
		'';
	buildDiscoHawkInstallableFor =
		{ bizhawkAssemblies ? null
		, hawkSourceInfo ? null
		, forNixOS ? true # currently only adds Mesa to buildInputs, and DiscoHawk doesn't need that, but it's propagated through here so the asms derivation can be shared between it and EmuHawk
		, desktopName ? "DiscoHawk (Mono Runtime)"
		, desktopIcon ? null
		}: buildInstallable' {
			inherit bizhawkAssemblies desktopIcon desktopName forNixOS hawkSourceInfo;
			pname = "discohawk-monort";
			launchScriptAttrName = "discohawk";
		};
	buildEmuHawkInstallableFor =
		{ bizhawkAssemblies ? null
		, hawkSourceInfo ? null
		, forNixOS ? true
		, desktopName ? "EmuHawk (Mono Runtime)"
		, desktopIcon ? null
		}: buildInstallable' {
			inherit bizhawkAssemblies desktopIcon desktopName forNixOS hawkSourceInfo;
			pname = "emuhawk-monort";
			launchScriptAttrName = if forNixOS then "emuhawk" else "emuhawkNonNixOS";
		};
}
