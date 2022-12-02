using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace cider
{
    internal class CPU
    {
        public enum AddressingMode
        {
            Immediate,
            ZeroPage,
            ZeroPage_X,
            ZeroPage_Y,
            Absolute,
            Absolute_X,
            Absolute_Y,
            Indirect_X,
            Indirect_Y,
            NoneAddressing,
        }

        public byte register_a;
        public byte register_x;
        public byte register_y;
        private byte sp;
        private byte status;
        private byte[] mem;
        private UInt16 pc;

        private UInt16 get_operand_address(AddressingMode mode) {
            UInt16 _pos,_addr,_base,_lo,_hi;
            switch (mode)
            {
                case AddressingMode.Immediate: return pc;

                case AddressingMode.ZeroPage: return mem_read(pc);

                case AddressingMode.Absolute: return mem_read_u16(pc);

                case AddressingMode.ZeroPage_X:
                    _pos = mem_read(pc);
                    _addr = (UInt16)(_pos + register_x);
                    return _addr;
                case AddressingMode.ZeroPage_Y:
                    _pos = mem_read(pc);
                    _addr = (UInt16)(_pos +register_y);
                    return _addr;
                case AddressingMode.Absolute_X:
                    _base = mem_read_u16(pc);
                    _addr = (UInt16)(_base + register_x);
                    return _addr;
                case AddressingMode.Absolute_Y:
                    _base = mem_read_u16(pc);
                    _addr = (UInt16)(_base + register_y);
                    return _addr;
                case AddressingMode.Indirect_X:
                    _base = mem_read(pc);
                    byte _ptr = (byte)(_base + register_x);
                    _lo = mem_read(_ptr);
                    _hi = mem_read((UInt16)(_ptr + 1));
                    return (UInt16)((_hi << 8) | _lo);

                case AddressingMode.Indirect_Y:
                    _base = mem_read(pc);

                    _lo = mem_read(_base);
                    _hi = mem_read((UInt16)(_base + 1));
                    UInt16 deref_base = (UInt16)((_hi << 8) | _lo);
                    UInt16 deref = (UInt16)(deref_base + register_y);
                    return deref;

                case AddressingMode.NoneAddressing:
                    throw new Exception($"mode {mode} is not null");
                default: throw new Exception();
            }
        }
        
        public CPU() {
            mem = new byte[0xffff];
            register_a = 0;
            register_x = 0;
            status = 0;
            register_y = 0;
        }
        public void reset() {
            register_a = 0;
            register_x = 0;
            status = 0;

            pc = mem_read_u16(0xFFFC);
        }

        public byte mem_read(UInt16 addr) {
            return mem[addr];
        }
        public void mem_write(UInt16 addr, byte data) {
            mem[addr] = data;
        }
        public UInt16 mem_read_u16(UInt16 pos){
            UInt16 lo = mem_read(pos);
            UInt16 hi = mem_read((UInt16)(pos + 1));
            return (UInt16)((hi << 8) | lo);
        }
        public void mem_write_u16(UInt16 addr,UInt16 data)
        {
            byte hi = (byte)(data >> 8);
            byte lo = (byte)(data & 0xff);
            mem_write(addr,lo);
            mem_write((UInt16)(addr + 1), hi);
        }
        public void mem_load_and_run(byte[] program) {
            load(program);
            reset();
            run();
        }
        private void load(byte[] program) {
            Array.Copy(program, 0,mem, 0x8000, program.Length);
            mem_write_u16(0xFFFC, 0x8000);
        }

        public void run()
        {
            while (true)
            {
                byte code = mem_read(pc);
                pc+= 1;
                switch (code) {
                    case 0xA9: // LDA
                        lda(AddressingMode.Immediate);
                        pc+= 1;
                        break;
                    case 0xA5: // LDA
                        lda(AddressingMode.ZeroPage);
                        break;
                    case 0xAD: // LDA
                        lda(AddressingMode.Absolute);
                        pc += 2;
                        break;
                    case 0x85:
                        sta(AddressingMode.ZeroPage);
                        pc += 1;
                        break;
                    case 0x95:
                        sta(AddressingMode.ZeroPage_X);
                        pc += 1;
                        break;
                    case 0xAA:
                        tax();
                        break;
                    case 0xc0:
                        byte param = mem_read(pc);
                        pc++;
                        cpy(param);
                        break;
                    case 0xe8:
                        register_x++;
                        break;
                    case 0x00:
                        return;
                }
            }
            
        }
        private void lda(AddressingMode mode)  
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a = value;
            update_zero_and_negative_flags(register_a);
        }
        private void sta(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            mem_write(addr,register_a);
        }
        private void tax()
        {
            register_x = register_a;
            update_zero_and_negative_flags(register_x);
        }
        private void cpy(byte value)
        {
            UInt16 diff = (byte)(register_y - value);
            if ((diff & 0x100)!=0) {
                status = (byte)(status | 0b0000_0001);
            }
            update_zero_and_negative_flags((byte)diff);
        }

        private void update_zero_and_negative_flags(byte result) {
            if (result == 0)
            {
                status = (byte)(status | 0b0000_0010);
            }
            else
            {
                status = (byte)(status & 0b0111_1111);
            }
            if ((byte)(result & 0b1000_0000) != 0)
            {
                status = (byte)(status | 0b1000_0000);
            }
            else
            {
                status = (byte)(status & 0b0111_1111);
            }
        }
    }
}
