#pragma once

namespace Emulator {

struct Platform {
  struct Load {
    Load() = default;
    Load(uint pathID, string option = "") : valid(true), pathID(pathID), option(option) {}
    explicit operator bool() const { return valid; }

    bool valid = false;
    uint pathID = 0;
    string option;
  };

  virtual auto path(uint id) -> string { return ""; }
  virtual auto open(uint id, string name, vfs::file::mode mode, bool required = false) -> shared_pointer<vfs::file> { return {}; }
  virtual auto load(uint id, string name, string type, vector<string> options = {}) -> Load { return {}; }
  virtual auto videoFrame(const uint16* data, uint pitch, uint width, uint height, uint scale) -> void {}
  virtual auto audioFrame(const double* samples, uint channels) -> void {}
  virtual auto inputPoll(uint port, uint device, uint input) -> int16 { return 0; }
  virtual auto inputRumble(uint port, uint device, uint input, bool enable) -> void {}
  virtual auto dipSettings(Markup::Node node) -> uint { return 0; }
  virtual auto notify(string text) -> void {}
  // 03-may-2021 manual addition. unused currently but let's hope for the best
  virtual auto getBackdropColor() -> uint16 { return 0; }

  bool traceEnabled = false;
  bool readHookEnabled = false;
  bool writeHookEnabled = false;
  bool executeHookEnabled = false;
  virtual auto cpuTrace(vector<string>) -> void {}
  virtual auto readHook(uint address, uint8 value) -> void {}
  virtual auto writeHook(uint address, uint8 value) -> void {}
  virtual auto execHook(uint address) -> void {}
  virtual auto time() -> int64 { return ::time(0); }
};

extern Platform* platform;

}
