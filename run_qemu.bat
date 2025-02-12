qemu-system-x86_64 ^
  -d guest_errors ^
  -bios ./ovmf/RELEASEX64_OVMF.fd ^
  -m 2G ^
  -device ahci,id=ahci ^
  -drive id=drive1,file=fat:rw:./bin/,if=none,format=raw ^
  -device ide-hd,drive=drive1,bus=ahci.0 ^
  -drive id=drive2,file=./hdd/hdd.img,if=none,format=raw ^
  -device ide-hd,drive=drive2,bus=ahci.1
::  -drive id=drive3,file=fat:rw:./hdd/,if=none,format=raw ^
::  -device ide-hd,drive=drive3,bus=ahci.2