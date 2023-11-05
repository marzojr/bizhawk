// blatantly stolen from target-libretro

#include <heuristics/heuristics.hpp>
#include <heuristics/heuristics.cpp>
#include <heuristics/super-famicom.cpp>
#include <heuristics/game-boy.cpp>
#include <heuristics/bs-memory.cpp>

#include "resources.hpp"
#include <nall/vfs/biz_file.hpp>
#include <vector>

static Emulator::Interface *emulator;
static std::vector<short> audioBuffer;

struct Program : Emulator::Platform
{
	Program();

	auto open(uint id, string name, vfs::file::mode mode, bool required) -> shared_pointer<vfs::file> override;
	auto load(uint id, string name, string type, vector<string> options = {}) -> Emulator::Platform::Load override;
	auto videoFrame(const uint16* data, uint pitch, uint width, uint height, uint scale) -> void override;
	auto audioFrame(const double* samples, uint channels) -> void override;
	auto inputPoll(uint port, uint device, uint input) -> int16 override;
	auto inputRumble(uint port, uint device, uint input, bool enable) -> void override;
	auto notify(string text) -> void override;
	auto getBackdropColor() -> uint16 override;
	auto cpuTrace(vector<string>) -> void override;
	auto readHook(uint address, uint8 value) -> void override;
	auto writeHook(uint address, uint8 value) -> void override;
	auto execHook(uint address) -> void override;
	auto time() -> int64 override;

	auto load() -> void;
	auto loadSuperFamicom() -> bool;
	auto loadGameBoy() -> bool;
	auto loadBSMemory() -> bool;

	auto save() -> void;

	auto openFileSuperFamicom(string name, vfs::file::mode mode, bool required) -> shared_pointer<vfs::file>;
	auto openFileGameBoy(string name, vfs::file::mode mode, bool required) -> shared_pointer<vfs::file>;

	auto hackPatchMemory(vector<uint8_t>& data) -> void;

	bool overscan = false;
	uint16_t backdropColor;
	int regionOverride = 0;
	bool breakOnLatch;

public:
	struct Game {
		string option;
		string manifest;
		Markup::Node document;
		boolean patched;
		boolean verified;
	};

	struct SuperFamicom : Game {
		vector<uint8_t> raw_data;
		string title;
		string region;
		vector<uint8_t> program;
		vector<uint8_t> data;
		vector<uint8_t> expansion;
		vector<uint8_t> firmware;
	} superFamicom;

	struct GameBoy : Game {
		vector<uint8_t> program;
	} gameBoy;

	struct BSMemory : Game {
		vector<uint8_t> program;
	} bsMemory;
};

static Program *program = nullptr;

Program::Program()
{
	platform = this;
}

auto Program::save() -> void
{
	if(!emulator->loaded()) return;
	emulator->save();
}

auto Program::open(uint id, string name, vfs::file::mode mode, bool required) -> shared_pointer<vfs::file>
{
	fprintf(stderr, "name \"%s\" was requested\n", name.data());

	shared_pointer<vfs::file> result;

	if (name == "ipl.rom" && mode == File::Read) {
		result = vfs::memory::file::open(iplrom, sizeof(iplrom));
	}

	if (name == "boards.bml" && mode == File::Read) {
		result = vfs::memory::file::open(Boards, sizeof(Boards));
	}

	if (id == 1) { //Super Famicom
		if (name == "manifest.bml" && mode == File::Read) {
			result = vfs::memory::file::open(superFamicom.manifest.data<uint8_t>(), superFamicom.manifest.size());
		}
		else if (name == "program.rom" && mode == File::Read) {
			result = vfs::memory::file::open(superFamicom.program.data(), superFamicom.program.size());
		}
		else if (name == "data.rom" && mode == File::Read) {
			result = vfs::memory::file::open(superFamicom.data.data(), superFamicom.data.size());
		}
		else if (name == "expansion.rom" && mode == File::Read) {
			result = vfs::memory::file::open(superFamicom.expansion.data(), superFamicom.expansion.size());
		}
		else {
			result = openFileSuperFamicom(name, mode, required);
		}
	}
	else if (id == 2) { //Game Boy
		if (name == "manifest.bml" && mode == File::Read) {
			result = vfs::memory::file::open(gameBoy.manifest.data<uint8_t>(), gameBoy.manifest.size());
		}
		else if (name == "program.rom" && mode == File::Read) {
			result = vfs::memory::file::open(gameBoy.program.data(), gameBoy.program.size());
		}
		else {
			result = openFileGameBoy(name, mode, required);
		}
	}
	else if (id == 3) {  //BS Memory
		if (name == "manifest.bml" && mode == File::Read) {
			result = vfs::memory::file::open(bsMemory.manifest.data<uint8_t>(), bsMemory.manifest.size());
		}
		else if (name == "program.rom" && mode == File::Read) {
			result = vfs::memory::file::open(bsMemory.program.data(), bsMemory.program.size());
		}
		else if(name == "program.flash") {
			//writes are not flushed to disk in bsnes
			result = vfs::memory::file::open(bsMemory.program.data(), bsMemory.program.size());
		}
		else {
			result = {};
		}
		// sufami turbo would be id 4 and 5 and is ignored for reasons? do we support it in bizhawk? TODO check this
	}
	return result;
}

auto Program::openFileSuperFamicom(string name, vfs::file::mode mode, bool required) -> shared_pointer<vfs::file>
{
	// TODO: the original bsnes code handles a lot more paths; *.data.ram, time.rtc and download.ram
	// I believe none of these can currently be correctly served by bizhawk and I therefor ignore them here
	// This should probably be changed? Not sure how much can break from not having them
	if(name == "msu1/data.rom" || name == "save.ram")
	{
		return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, name, required), mode);
	}

	if(name.match("msu1/track*.pcm"))
	{
		snesCallbacks.snes_msu_open(strtol(name.data() + 11, nullptr, 10));
		return shared_pointer<vfs::biz_file>{new vfs::biz_file};
	}

	if(name == "arm6.program.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0x28000) {
			return vfs::memory::file::open(&superFamicom.firmware.data()[0x00000], 0x20000);
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Program,architecture=ARM6)"]) {
			return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
		}
	}

	if(name == "arm6.data.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0x28000) {
			return vfs::memory::file::open(&superFamicom.firmware.data()[0x20000], 0x08000);
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Data,architecture=ARM6)"]) {
			auto file = vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
			if (file) file->seek(0x20000);
			return file;
		}
	}

	if(name == "hg51bs169.data.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0xc00) {
			return vfs::memory::file::open(superFamicom.firmware.data(), superFamicom.firmware.size());
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Data,architecture=HG51BS169)"]) {
			return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
		}
	}

	if(name == "lr35902.boot.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0x100) {
			return vfs::memory::file::open(superFamicom.firmware.data(), superFamicom.firmware.size());
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Boot,architecture=LR35902)"]) {
			return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
		}
	}

	if(name == "upd7725.program.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0x2000) {
			return vfs::memory::file::open(&superFamicom.firmware.data()[0x0000], 0x1800);
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Program,architecture=uPD7725)"]) {
			auto file = vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
			return file;
		}
	}

	if(name == "upd7725.data.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0x2000) {
			return vfs::memory::file::open(&superFamicom.firmware.data()[0x1800], 0x0800);
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Data,architecture=uPD7725)"]) {
			auto file = vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
			if (file) file->seek(0x1800);
			return file;
		}
	}

	if(name == "upd96050.program.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0xd000) {
			return vfs::memory::file::open(&superFamicom.firmware.data()[0x0000], 0xc000);
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Program,architecture=uPD96050)"]) {
			auto file = vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
			return file;
		}
	}

	if(name == "upd96050.data.rom" && mode == File::Read) {
		if(superFamicom.firmware.size() == 0xd000) {
			return vfs::memory::file::open(&superFamicom.firmware.data()[0xc000], 0x1000);
		}
		if(auto memory = superFamicom.document["game/board/memory(type=ROM,content=Data,architecture=uPD96050)"]) {
			auto file = vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, memory["identifier"].text().downcase(), required), mode);
			if (file) file->seek(0xc000);
			return file;
		}
	}

	return {};
}

auto Program::openFileGameBoy(string name, vfs::file::mode mode, bool required) -> shared_pointer<vfs::file>
{
	if(name == "save.ram")
	{
		return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::GameBoy, name, required), mode);
	}

	if(name == "time.rtc")
	{
		return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::GameBoy, name, required), mode);
	}

	if(name == "sgb")
	{
		return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::GameBoy, name, required), mode);
	}

	if(name == "sgb2")
	{
		return vfs::fs::file::open(snesCallbacks.snes_path_request(ID::GameBoy, name, required), mode);
	}

	return {};
}

auto Program::load() -> void {
	emulator->unload();
	emulator->load();

	// per-game hack overrides
	auto title = superFamicom.title;
	auto region = superFamicom.region;

	//relies on mid-scanline rendering techniques
	if(title == "AIR STRIKE PATROL" || title == "DESERT FIGHTER") emulator->configure("Hacks/PPU/Fast", false);

	//the dialogue text is blurry due to an issue in the scanline-based renderer's color math support
	if(title == "マーヴェラス") emulator->configure("Hacks/PPU/Fast", false);

	//stage 2 uses pseudo-hires in a way that's not compatible with the scanline-based renderer
	if(title == "SFC クレヨンシンチャン") emulator->configure("Hacks/PPU/Fast", false);

	//title screen game select (after choosing a game) changes OAM tiledata address mid-frame
	//this is only supported by the cycle-based PPU renderer
	if(title == "Winter olympics") emulator->configure("Hacks/PPU/Fast", false);

	//title screen shows remnants of the flag after choosing a language with the scanline-based renderer
	if(title == "WORLD CUP STRIKER") emulator->configure("Hacks/PPU/Fast", false);

	//relies on cycle-accurate writes to the echo buffer
	if(title == "KOUSHIEN_2") emulator->configure("Hacks/DSP/Fast", false);

	//will hang immediately
	if(title == "RENDERING RANGER R2") emulator->configure("Hacks/DSP/Fast", false);

	//will hang sometimes in the "Bach in Time" stage
	if(title == "BUBSY II" && region == "PAL") emulator->configure("Hacks/DSP/Fast", false);

	//fixes an errant scanline on the title screen due to writing to PPU registers too late
	if(title == "ADVENTURES OF FRANKEN" && region == "PAL") emulator->configure("Hacks/PPU/RenderCycle", 32);

	//fixes an errant scanline on the title screen due to writing to PPU registers too late
	if(title == "FIREPOWER 2000" || title == "SUPER SWIV") emulator->configure("Hacks/PPU/RenderCycle", 32);

	//fixes an errant scanline on the title screen due to writing to PPU registers too late
	if(title == "NHL '94" || title == "NHL PROHOCKEY'94") emulator->configure("Hacks/PPU/RenderCycle", 32);

	//fixes an errant scanline on the title screen due to writing to PPU registers too late
	if(title == "Sugoro Quest++") emulator->configure("Hacks/PPU/RenderCycle", 128);

	if (emulator->configuration("Hacks/Hotfixes")) {
		//this game transfers uninitialized memory into video RAM: this can cause a row of invalid tiles
		//to appear in the background of stage 12. this one is a bug in the original game, so only enable
		//it if the hotfixes option has been enabled.
		if(title == "The Hurricanes") emulator->configure("Hacks/Entropy", "None");

		//Frisky Tom attract sequence sometimes hangs when WRAM is initialized to pseudo-random patterns
		if (title == "ニチブツ・アーケード・クラシックス") emulator->configure("Hacks/Entropy", "None");
	}

	emulator->power();
	bus.lock();
}

auto Program::load(uint id, string name, string type, vector<string> options) -> Emulator::Platform::Load {
	// This needs to occur here rather than snes_init, as callbacks aren't set yet then
	emulator->synchronize(time());

	if (id == 1)
	{
		if (loadSuperFamicom())
		{
			return {id, superFamicom.region};
		}
	}
	else if (id == 2)
	{
		if (loadGameBoy())
		{
			return { id, NULL };
		}
	}
	else if (id == 3)
	{
		if (loadBSMemory())
		{
			return { id, NULL };
		}
	}
	return { id, options(0) };
}

auto Program::loadSuperFamicom() -> bool
{
	vector<uint8_t>& rom = superFamicom.raw_data;
	fprintf(stderr, "rom size: %ld\n", rom.size());

	if(rom.size() < 0x8000) return false;

	auto heuristics = Heuristics::SuperFamicom(rom, "");

	superFamicom.title = heuristics.title();
	switch (regionOverride) {
		case 0: superFamicom.region = heuristics.videoRegion(); break;
		case 1: superFamicom.region = "NTSC"; break;
		case 2: superFamicom.region = "PAL"; break;
	}
	if (auto fp = vfs::fs::file::open(snesCallbacks.snes_path_request(ID::SuperFamicom, "manifest.bml", false), File::Read)) {
		superFamicom.manifest = fp->reads();
	} else {
		superFamicom.manifest = heuristics.manifest();
	}

	hackPatchMemory(rom);
	superFamicom.document = BML::unserialize(superFamicom.manifest);
	fprintf(stderr, "loaded game manifest: \"\n%s\"\n", superFamicom.manifest.data());

	uint offset = 0;
	if(auto size = heuristics.programRomSize()) {
		superFamicom.program.acquire(rom.data() + offset, size);
		offset += size;
	}
	if(auto size = heuristics.dataRomSize()) {
		superFamicom.data.acquire(rom.data() + offset, size);
		offset += size;
	}
	if(auto size = heuristics.expansionRomSize()) {
		superFamicom.expansion.acquire(rom.data() + offset, size);
		offset += size;
	}
	if(auto size = heuristics.firmwareRomSize()) {
		superFamicom.firmware.acquire(rom.data() + offset, size);
		offset += size;
	}
	return true;
}

auto Program::loadGameBoy() -> bool {
	if (gameBoy.program.size() < 0x4000) return false;

	auto heuristics = Heuristics::GameBoy(gameBoy.program, "");

	gameBoy.manifest = heuristics.manifest();
	gameBoy.document = BML::unserialize(gameBoy.manifest);

	return true;
}

auto Program::loadBSMemory() -> bool {
	if (bsMemory.program.size() < 0x8000) return false;

	auto heuristics = Heuristics::BSMemory(bsMemory.program, "");

	bsMemory.manifest = heuristics.manifest();
	bsMemory.document = BML::unserialize(bsMemory.manifest);

	return true;
}

auto Program::videoFrame(const uint16* data, uint pitch, uint width, uint height, uint scale) -> void {

	// note: scale is not used currently, but as bsnes has builtin scaling support (something something mode 7)
	// we might actually wanna make use of that?
	pitch >>= 1;
	if (!overscan)
	{
		uint multiplier = height / 240;
		data += 8 * pitch * multiplier;
		height -= 16 * multiplier;
	}

	// fprintf(stderr, "got a video frame with dimensions h: %d, w: %d, p: %d, overscan: %d, scale: %d\n", height, width, pitch, overscan, scale);

 	snesCallbacks.snes_video_frame(data, width, height, pitch);
}

// Double the fun!
static int16_t d2i16(double v)
{
	v *= 0x8000;
	if (v > 0x7fff)
		v = 0x7fff;
	else if (v < -0x8000)
		v = -0x8000;
	return int16_t(floor(v + 0.5));
}

auto Program::audioFrame(const double* samples, uint channels) -> void
{
	audioBuffer.push_back(d2i16(samples[0]));
	audioBuffer.push_back(d2i16(samples[1]));
}

auto Program::notify(string message) -> void
{
	if (message == "NO_LAG")
		snesCallbacks.snes_no_lag(false);
	else if (message == "NO_LAG_SGB")
		snesCallbacks.snes_no_lag(true);
	else if (message == "LATCH") {
		if (breakOnLatch) {
			scheduler.StepOnce = true;
			breakOnLatch = false;
		}
		snesCallbacks.snes_controller_latch();
	}
}

auto Program::cpuTrace(vector<string> parts) -> void
{
	snesCallbacks.snes_trace(parts[0], parts[1]);
}

auto Program::readHook(uint address, uint8 value) -> void
{
	snesCallbacks.snes_read_hook(address, value);
}

auto Program::writeHook(uint address, uint8 value) -> void
{
	snesCallbacks.snes_write_hook(address, value);
}

auto Program::execHook(uint address) -> void
{
	snesCallbacks.snes_exec_hook(address);
}

auto Program::time() -> int64
{
	return snesCallbacks.snes_time();
}

auto Program::getBackdropColor() -> uint16
{
	return backdropColor;
}

auto Program::inputPoll(uint port, uint device, uint input) -> int16
{
	int index = 0;
	int id = input;
	if (device == ID::Device::SuperMultitap) {
		index = input / 12;
		id = input % 12;
	} else if (device == ID::Device::Payload) {
		index = input / 16;
		id = input % 16;
	} else if (device == ID::Device::Justifiers) {
		index = input / 4;
		id = input % 4;
	}

	return snesCallbacks.snes_input_poll(port, index, id);
}

auto Program::inputRumble(uint port, uint device, uint input, bool enable) -> void
{
}


auto Program::hackPatchMemory(vector<uint8_t>& data) -> void
{
	auto title = superFamicom.title;

	if(title == "Satellaview BS-X" && data.size() >= 0x100000) {
		//BS-X: Sore wa Namae o Nusumareta Machi no Monogatari (JPN) (1.1)
		//disable limited play check for BS Memory flash cartridges
		//benefit: allow locked out BS Memory flash games to play without manual header patching
		//detriment: BS Memory ROM cartridges will cause the game to hang in the load menu
		if(data[0x4a9b] == 0x10) data[0x4a9b] = 0x80;
		if(data[0x4d6d] == 0x10) data[0x4d6d] = 0x80;
		if(data[0x4ded] == 0x10) data[0x4ded] = 0x80;
		if(data[0x4e9a] == 0x10) data[0x4e9a] = 0x80;
	}
}
