CFLAGS := -Wall -std=gnu17 -O3 -flto -fPIC -I../mupen64plus-core/src
LDFLAGS := -shared -s

SRCS := main.c
TARGET := mupen64plus-audio-bkm

ifeq ($(OS),Windows_NT)
	SRCS += ../mupen64plus-core/src/osal/dynamiclib_win32.c
	TARGET := $(TARGET).dll
else
	SRCS += ../mupen64plus-core/src/osal/dynamiclib_unix.c
	TARGET := lib$(TARGET).so
endif

all: $(TARGET)

$(TARGET): $(SRCS)
	$(LINK.c) $^ -o $@

install: $(TARGET)
	cp $(TARGET) ../../../Assets/dll/

clean:
	rm $(TARGET)
