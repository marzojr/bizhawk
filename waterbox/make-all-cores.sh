#!/bin/sh
set -e

cd emulibc && make -f Makefile $1 -j && cd -
cd libco && make -f Makefile $1 -j && cd -
cd ares64 && ./make-both.sh $1 && cd -
cd bsnescore && make -f Makefile $1 -j && cd -
cd gpgx && make -f Makefile $1 -j && cd -
cd melon && make -f Makefile $1 -j && cd -
cd picodrive && make -f Makefile $1 -j && cd -
cd stella && make -f Makefile $1 -j && cd -
cd snes9x && make -f Makefile $1 -j && cd -
cd tic80 && make -f Makefile $1 -j && cd -
cd uae && make -f Makefile $1 -j && cd -
cd uzem && make -f Makefile $1 -j && cd -
cd virtualjaguar && make -f Makefile $1 -j && cd -
cd nyma && ./make-all-released-cores.sh $1 && cd -

# this won't include MAME by default, due to the large amount of time it takes to build it
# to include MAME just do INCLUDE_MAME=1 ./make-all-cores.sh
if test "$INCLUDE_MAME" ; then
	cd mame-arcade && make -f Makefile $1 -j && cd -
fi
