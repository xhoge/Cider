using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace cider
{
    internal class Bus
    {
        const UInt16 RAM = 0x0000;
        const UInt16 RAM_MIRRORS_END = 0x1fff;
        const UInt16 PPU_REGISTERS = 0x2000;
        const UInt16 PPU_REGISTERS_MIRRORS_END = 0x3fff;

        public byte[] cpu_vram;
        public Cartridge cartridge;
        public Bus(Cartridge cart) {
            cpu_vram = new byte[2048];
            cartridge = cart;
        }

        public byte mem_read(UInt16 addr) {
            switch (addr)
            {
                case UInt16 i when RAM < i && i < RAM_MIRRORS_END:
                    UInt16 mirror_down_addr = (UInt16)(addr & 0b00000111_11111111);
                    return cpu_vram[mirror_down_addr];

                case UInt16 i when PPU_REGISTERS < i && i < PPU_REGISTERS_MIRRORS_END:
                    UInt16 _mirror_down_addr = (UInt16)(addr & 0b00100000_00000111);
                    return 0;
                case UInt16 i when 0x8000 < i && i < 0xffff:
                    return read_prg_rom(addr);
                default:
                    return 0;
            }
        }

        public void mem_write(UInt16 addr,byte data)
        {
            switch (addr)
            {
                case UInt16 i when RAM < i && i < RAM_MIRRORS_END:
                    UInt16 mirror_down_addr = (UInt16)(addr & 0b11111111111);
                    cpu_vram[mirror_down_addr] = data;
                    break;
                case UInt16 i when PPU_REGISTERS < i && i < PPU_REGISTERS_MIRRORS_END:
                    UInt16 _mirror_down_addr = (UInt16)(addr & 0b00100000_00000111);
                    break;
                case UInt16 i when 0x8000 < i && i < 0xffff:
                    new Exception("書き込み不可");
                    break;
            }
        }

        public byte read_prg_rom(UInt16 addr) {
            addr -= 0x8000;
            if (cartridge.prg_rom.Length == 0x4000 && addr > 0x4000) {
                addr %= 0x4000;
            }
            return cartridge.prg_rom[addr];
        }
    }
}
