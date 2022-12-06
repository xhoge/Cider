using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cider
{
    internal class Cartridge
    {
        public enum Mirroring
        {
            VERTICAL,
            HORIZONTAL,
            FOUR_SCREEN,
        }

        public byte[] prg_rom;
        public byte[] chr_rom;
        public byte mapper;
        public Mirroring screen_mirroring;

        const UInt16 PRG_ROM_PAGE_SIZE = 16000;
        const UInt16 CHR_ROM_PAGE_SIZE = 8000;

        public bool LoadRom(byte[] raw)
        {
            byte[] NES_TAG = { 0x4E, 0x45, 0x53, 0x1A };

            if (raw[0..4] != NES_TAG) {
                return false; //File is not in INes file format
            }
            byte _mapper = (byte)((raw[7] & 0b1111_0000) | (raw[6] >> 4));
            byte ines_ver = (byte)((raw[7] >> 2) & 0b11);
            if(ines_ver != 0)
            {
                return false; //NES2.0 format is not supported
            }

            Mirroring _screen_mirroring;
            if ((raw[6] & 0x8) == 0x8)
            {
                _screen_mirroring = Mirroring.FOUR_SCREEN;
            }
            else
            {
                _screen_mirroring = (Mirroring)(raw[6] & 0x01);
            }
            UInt16 prg_rom_size = (UInt16)(raw[4] * PRG_ROM_PAGE_SIZE);
            UInt16 chr_rom_size = (UInt16)(raw[5] * CHR_ROM_PAGE_SIZE);

            UInt16 skip_trainer = (UInt16)((raw[6] & 0b100) != 0 ? 512 : 0);
            UInt16 prg_rom_start = (UInt16)(16 + skip_trainer);
            UInt16 chr_rom_start = (UInt16)(prg_rom_start + skip_trainer);

            prg_rom = raw[prg_rom_start..(prg_rom_start+prg_rom_size)].ToArray();
            chr_rom = raw[chr_rom_start..(chr_rom_start+chr_rom_size)].ToArray();
            mapper = _mapper;
            screen_mirroring = _screen_mirroring;

            return true;
        }
    }
}
