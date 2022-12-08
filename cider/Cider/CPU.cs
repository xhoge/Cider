using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
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
        public enum CpuStatus
        {
            CarryFlg,
            ZeroFlg,
            IRQFlg,
            DecimalModeFlg,
            BreakModeFlg,
            Reserved,
            OverFlowFlg,
            NegativeFlg,
        }

        public byte register_a;
        public byte register_x;
        public byte register_y;
        public byte sp;
        private byte status;
        private UInt16 pc;
        private Bus bus;
        const UInt16 STACK_BASE = 0x100;

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
        
        public CPU(Bus _bus) {
            register_a = 0;
            register_x = 0;
            register_y = 0;
            status = 0;
            sp = 0xfd;
            bus = _bus;
        }
        public void reset() {
            register_a = 0;
            register_x = 0;
            status = 0;
            sp = 0xfd;

            pc = mem_read_u16(0xFFFC);
        }

        public byte mem_read(UInt16 addr) {
            return bus.mem_read(addr);
        }
        public void mem_write(UInt16 addr, byte data) {
            bus.mem_write(addr, data);
        }
        public UInt16 mem_read_u16(UInt16 pos){
            UInt16 lo = bus.mem_read(pos);
            UInt16 hi = bus.mem_read((UInt16)(pos + 1));
            return (UInt16)((hi << 8) | lo);
        }
        public void mem_write_u16(UInt16 addr,UInt16 data)
        {
            byte hi = (byte)(data >> 8);
            byte lo = (byte)(data & 0xff);
            bus.mem_write(addr,lo);
            bus.mem_write((UInt16)(addr + 1), hi);
        }

        public void run()
        {
            bool state = true;
            while (state)
            {
                state = exec();
            }
        }
        public bool exec()
        {
            
            byte code = mem_read(pc);
            pc += 1;
            switch (code) {
            /* 転送命令 */
                //LDA 
                case 0xA9: register_a = ld_register(AddressingMode.Immediate) ; pc +=1; break;
                case 0xA5: register_a = ld_register(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0xB5: register_a = ld_register(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0xAD: register_a = ld_register(AddressingMode.Absolute)  ; pc +=2; break;
                case 0xBD: register_a = ld_register(AddressingMode.Absolute_X); pc +=2; break;
                case 0xB9: register_a = ld_register(AddressingMode.Absolute_Y); pc +=2; break;
                case 0xA1: register_a = ld_register(AddressingMode.Indirect_X); pc +=1; break;
                case 0xB1: register_a = ld_register(AddressingMode.Indirect_Y); pc +=1; break;
                
                //LDX 
                case 0xA2: register_x = ld_register(AddressingMode.Immediate) ; pc +=1; break;
                case 0xA6: register_x = ld_register(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0xB6: register_x = ld_register(AddressingMode.ZeroPage_Y); pc +=1; break;
                case 0xAE: register_x = ld_register(AddressingMode.Absolute)  ; pc +=2; break;
                case 0xBE: register_x = ld_register(AddressingMode.Absolute_Y); pc +=2; break;
 
                //LDY 
                case 0xA0: register_y = ld_register(AddressingMode.Immediate) ; pc +=1; break;
                case 0xA4: register_y = ld_register(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0xB4: register_y = ld_register(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0xAC: register_y = ld_register(AddressingMode.Absolute)  ; pc +=2; break;
                case 0xBC: register_y = ld_register(AddressingMode.Absolute_X); pc +=2; break;

                //STA 
                case 0x85: st_register(AddressingMode.ZeroPage, register_a)  ; pc +=1; break;
                case 0x95: st_register(AddressingMode.ZeroPage_X, register_a); pc +=1; break;
                case 0x8D: st_register(AddressingMode.Absolute, register_a)  ; pc +=2; break;
                case 0x9D: st_register(AddressingMode.Absolute_X, register_a); pc +=2; break;
                case 0x99: st_register(AddressingMode.Absolute_Y, register_a); pc +=2; break;
                case 0x81: st_register(AddressingMode.Indirect_X, register_a); pc +=1; break;
                case 0x91: st_register(AddressingMode.Indirect_Y, register_a); pc +=1; break;
                
                //STX 
                case 0x86: st_register(AddressingMode.ZeroPage, register_x)  ; pc +=1; break;
                case 0x96: st_register(AddressingMode.ZeroPage_Y, register_x); pc +=1; break;
                case 0x8E: st_register(AddressingMode.Absolute, register_x)  ; pc +=2; break;
                
                //STY 
                case 0x84: st_register(AddressingMode.ZeroPage, register_y)  ; pc +=1; break;
                case 0x94: st_register(AddressingMode.ZeroPage_X, register_y); pc +=1; break;
                case 0x8C: st_register(AddressingMode.Absolute, register_y)  ; pc +=2; break;

                //TAX 
                case 0xAA: 
                    register_x = register_a; 
                    update_zero_and_negative_flags(register_x); 
                    break;

                //TAY 
                case 0xA8: 
                    register_y = register_a; 
                    update_zero_and_negative_flags(register_y); 
                    break;

                //TSX 
                case 0xBA: 
                    register_x = sp; 
                    update_zero_and_negative_flags(register_x); 
                    break;

                //TXA 
                case 0x8A: 
                    register_a = register_x; 
                    update_zero_and_negative_flags(register_a); 
                    break;

                //TXS 
                case 0x9A:
                    sp = register_x; 
                    update_zero_and_negative_flags(sp); 
                    break;

                //TYA 
                case 0x98: 
                    register_a = register_y; 
                    update_zero_and_negative_flags(register_a); 
                    break;
                
            /* 算術命令 */
                //ADC 
                case 0x69: adc(AddressingMode.Immediate) ; pc +=1; break;
                case 0x65: adc(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x75: adc(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x6D: adc(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x7D: adc(AddressingMode.Absolute_X); pc +=2; break;
                case 0x79: adc(AddressingMode.Absolute_Y); pc +=2; break;
                case 0x61: adc(AddressingMode.Indirect_X); pc +=1; break;
                case 0x71: adc(AddressingMode.Indirect_Y); pc +=1; break;

                //AND 
                case 0x29: and(AddressingMode.Immediate) ; pc +=1; break;
                case 0x25: and(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x35: and(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x2D: and(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x3D: and(AddressingMode.Absolute_X); pc +=2; break;
                case 0x39: and(AddressingMode.Absolute_Y); pc +=2; break;
                case 0x21: and(AddressingMode.Indirect_X); pc +=1; break;
                case 0x31: and(AddressingMode.Indirect_Y); pc +=1; break;

                //ASL  
                case 0x0A: asl_Accumulator(); break;
                case 0x06: asl(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x16: asl(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x0E: asl(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x1E: asl(AddressingMode.Absolute_X); pc +=2; break;

                //BIT 
                case 0x24: bit(AddressingMode.ZeroPage); pc +=1; break;
                case 0x2C: bit(AddressingMode.Absolute); pc +=2; break;

                //CMP 
                case 0xC9: cmp(AddressingMode.Immediate,register_a)  ; pc +=1; break;
                case 0xC5: cmp(AddressingMode.ZeroPage, register_a)  ; pc +=1; break;
                case 0xD5: cmp(AddressingMode.ZeroPage_X, register_a); pc +=1; break;
                case 0xCD: cmp(AddressingMode.Absolute, register_a)  ; pc +=2; break;
                case 0xDD: cmp(AddressingMode.Absolute_X, register_a); pc +=2; break;
                case 0xD9: cmp(AddressingMode.Absolute_Y, register_a); pc +=2; break;
                case 0xC1: cmp(AddressingMode.Indirect_X, register_a); pc +=1; break;
                case 0xD1: cmp(AddressingMode.Indirect_Y, register_a); pc +=1; break;

                //CPX 
                case 0xE0: cmp(AddressingMode.Immediate, register_x); pc +=1; break;
                case 0xE4: cmp(AddressingMode.ZeroPage, register_x) ; pc +=1; break;
                case 0xEC: cmp(AddressingMode.Absolute, register_x) ; pc +=2; break;

                //CPY 
                case 0xC0: cmp(AddressingMode.Immediate, register_y); pc +=1; break;
                case 0xC4: cmp(AddressingMode.ZeroPage, register_y) ; pc +=1; break;
                case 0xCC: cmp(AddressingMode.Absolute, register_y) ; pc +=2; break;

                //DEC 
                case 0xC6: dec(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0xD6: dec(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0xCE: dec(AddressingMode.Absolute)  ; pc +=2; break;
                case 0xDE: dec(AddressingMode.Absolute_X); pc +=2; break;

                //DEX 
                case 0xCA: update_zero_and_negative_flags(--register_x); break;

                //DEY 
                case 0x88: update_zero_and_negative_flags(--register_y); break;
                
                //EOR 
                case 0x49: eor(AddressingMode.Immediate) ; pc +=1; break;
                case 0x45: eor(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x55: eor(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x4D: eor(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x5D: eor(AddressingMode.Absolute_X); pc +=2; break;
                case 0x59: eor(AddressingMode.Absolute_Y); pc +=2; break;
                case 0x41: eor(AddressingMode.Indirect_X); pc +=1; break;
                case 0x51: eor(AddressingMode.Indirect_Y); pc +=1; break;
               
                //INC 
                case 0xE6: inc(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0xF6: inc(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0xEE: inc(AddressingMode.Absolute)  ; pc +=2; break;
                case 0xFE: inc(AddressingMode.Absolute_X); pc +=2; break;

                //INX 
                case 0xE8: update_zero_and_negative_flags(++register_x); break;

                //INY 
                case 0xC8: update_zero_and_negative_flags(++register_y); break;
                
                //LSR 
                case 0x4A: lsr_Accumulator(); break;
                case 0x46: lsr(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x56: lsr(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x4E: lsr(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x5E: lsr(AddressingMode.Absolute_X); pc +=2; break;
                
                //ORA 
                case 0x09: ora(AddressingMode.Immediate) ; pc +=1; break;
                case 0x05: ora(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x15: ora(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x0D: ora(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x1D: ora(AddressingMode.Absolute_X); pc +=2; break;
                case 0x19: ora(AddressingMode.Absolute_Y); pc +=2; break;
                case 0x01: ora(AddressingMode.Indirect_X); pc +=1; break;
                case 0x11: ora(AddressingMode.Indirect_Y); pc +=1; break;

                //ROL 
                case 0x2A: rol_Accumulator(); break;
                case 0x26: rol(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x36: rol(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x2E: rol(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x3E: rol(AddressingMode.Absolute_X); pc +=2; break;

                //ROR 
                case 0x6A: ror_Accumulator(); break;
                case 0x66: ror(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0x76: ror(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0x6E: ror(AddressingMode.Absolute)  ; pc +=2; break;
                case 0x7E: ror(AddressingMode.Absolute_X); pc +=2; break;

                //SBC
                case 0xE9: sbc(AddressingMode.Immediate) ; pc +=1; break;
                case 0xE5: sbc(AddressingMode.ZeroPage)  ; pc +=1; break;
                case 0xF5: sbc(AddressingMode.ZeroPage_X); pc +=1; break;
                case 0xED: sbc(AddressingMode.Absolute)  ; pc +=2; break;
                case 0xFD: sbc(AddressingMode.Absolute_X); pc +=2; break;
                case 0xF9: sbc(AddressingMode.Absolute_Y); pc +=2; break;
                case 0xE1: sbc(AddressingMode.Indirect_X); pc +=1; break;
                case 0xF1: sbc(AddressingMode.Indirect_Y); pc +=1; break;

            /* スタック命令 */
                //PHA
                case 0x48: stack_push(register_a); break;

                //PHP
                case 0x08:
                    byte status_clone = status;
                    status_clone |= 0b0011_0000;
                    stack_push(status_clone); 
                    break;

                //PLA
                case 0x68: 
                    register_a = stack_pop();
                    update_zero_and_negative_flags(register_a);
                    break;

                //PLP
                case 0x28: 
                    status = stack_pop();
                    update_status_flg(CpuStatus.BreakModeFlg, 0);
                    update_status_flg(CpuStatus.Reserved, 1);
                    break;

            /* ジャンプ命令 */
                //JMP
                case 0x4C: pc = mem_read_u16(pc); break;
                case 0x6C:
                    UInt16 addr =  mem_read_u16(pc);
                    UInt16 jump_addr = 0;

                    if ((addr & 0x00ff) == 0x00ff)
                    {
                        byte _lo = mem_read(addr);
                        byte _hi = mem_read((UInt16)(addr & 0xff00));
                        jump_addr = (UInt16)((_hi << 8) | _lo);
                    }
                    else
                        { jump_addr = mem_read_u16(addr); }
                    pc = jump_addr;
                    break;

                //JSR
                case 0x20:
                    stack_push_u16((UInt16)(pc + 2 - 1));
                    pc = mem_read_u16(pc); 
                    break;

                //RTS
                case 0x60: pc = (UInt16)(stack_pop_u16() + 1);  break;

                //RTI
                case 0x40:
                    status = stack_pop();
                    update_status_flg(CpuStatus.BreakModeFlg, 0);
                    update_status_flg(CpuStatus.Reserved, 1);
                    pc = stack_pop_u16();
                    break;

            /* 分岐命令 */
                //BCC
                case 0x90: branch(get_status_flg(CpuStatus.CarryFlg)==0); break;

                //BCS
                case 0xB0: branch(get_status_flg(CpuStatus.CarryFlg)==1); break;

                //BEQ
                case 0xF0: branch(get_status_flg(CpuStatus.ZeroFlg) == 1); break;

                //BMI
                case 0x30: branch(get_status_flg(CpuStatus.NegativeFlg) == 1); break;

                //BNE
                case 0xD0: branch(get_status_flg(CpuStatus.ZeroFlg) == 0); break;

                //BPL
                case 0x10: branch(get_status_flg(CpuStatus.NegativeFlg) == 0); break;

                //BVC
                case 0x50: branch(get_status_flg(CpuStatus.OverFlowFlg) == 0); break;

                //BVS
                case 0x70: branch(get_status_flg(CpuStatus.OverFlowFlg) == 1); break;

            /* フラグ変更命令 */
                //CLC
                case 0x18: update_status_flg(CpuStatus.CarryFlg,0); break;

                //CLD
                case 0xD8: break;

                //CLI
                case 0x58: update_status_flg(CpuStatus.IRQFlg, 0); break;

                //CLV
                case 0xB8: update_status_flg(CpuStatus.OverFlowFlg, 0); break;

                //SEC
                case 0x38: update_status_flg(CpuStatus.CarryFlg, 1); break;

                //SED
                case 0xF8: break;

                //SEI
                case 0x78: update_status_flg(CpuStatus.IRQFlg, 1); break;


            /* 非公式命令 */
                //AAC
                case 0x0B: aac(AddressingMode.Immediate); pc += 1; break;
                case 0x2B: aac(AddressingMode.Immediate); pc += 1; break;

                //AAX
                case 0x87: aax(AddressingMode.ZeroPage); pc += 1; break;
                case 0x97: aax(AddressingMode.ZeroPage_Y); pc += 1; break;
                case 0x83: aax(AddressingMode.Indirect_X); pc += 1; break;
                case 0x8F: aax(AddressingMode.Absolute); pc += 2; break;

                //ARR
                case 0x6B: arr(AddressingMode.Immediate); pc += 1; break;

                //ASR[ALR]
                case 0x4B: asr(AddressingMode.Immediate); pc += 1; break;

                //ATX[OAL]
                case 0xAB: atx(AddressingMode.Immediate); pc += 1; break;

                //AXA [AXA] 
                case 0x9F: axa(AddressingMode.Absolute_Y); pc += 2; break;
                case 0x93: axa(AddressingMode.Indirect_Y); pc += 1; break;

                //AXS[SAX] 
                case 0xCB: axs(AddressingMode.Immediate); pc += 1; break;

                //DCP[DCM]
                case 0xC7: dcp(AddressingMode.ZeroPage); pc += 1; break;
                case 0xD7: dcp(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0xCF: dcp(AddressingMode.Absolute); pc += 2; break;
                case 0xDF: dcp(AddressingMode.Absolute_X); pc += 2; break;
                case 0xDB: dcp(AddressingMode.Absolute_Y); pc += 2; break;
                case 0xC3: dcp(AddressingMode.Indirect_X); pc += 1; break;
                case 0xD3: dcp(AddressingMode.Indirect_Y); pc += 1; break;

                //DOP
                case 0x04: dop(AddressingMode.ZeroPage); pc += 1; break;
                case 0x14: dop(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x34: dop(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x44: dop(AddressingMode.ZeroPage); pc += 1; break;
                case 0x54: dop(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x64: dop(AddressingMode.ZeroPage); pc += 1; break;
                case 0x74: dop(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x80: dop(AddressingMode.Immediate); pc += 1; break;
                case 0x82: dop(AddressingMode.Immediate); pc += 1; break;
                case 0x89: dop(AddressingMode.Immediate); pc += 1; break;
                case 0xC2: dop(AddressingMode.Immediate); pc += 1; break;
                case 0xD4: dop(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0xE2: dop(AddressingMode.Immediate); pc += 1; break;
                case 0xF4: dop(AddressingMode.ZeroPage_X); pc += 1; break;

                //ISC
                case 0xE7: isc(AddressingMode.ZeroPage); pc += 1; break;
                case 0xF7: isc(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0xEF: isc(AddressingMode.Absolute); pc += 2; break;
                case 0xFF: isc(AddressingMode.Absolute_X); pc += 2; break;
                case 0xFB: isc(AddressingMode.Absolute_Y); pc += 2; break;
                case 0xE3: isc(AddressingMode.Indirect_X); pc += 1; break;
                case 0xF3: isc(AddressingMode.Indirect_Y); pc += 1; break;

                //KIL(JAM)
                case 0x02:case 0x12:case 0x22:case 0x32: case 0x42:case 0x52:
                case 0x62:case 0x72:case 0x92:case 0xB2: case 0xD2:case 0xF2:
                break;


                //LAR
                case 0xBB: lar(AddressingMode.Absolute_Y); pc += 2; break;

                //LAX
                case 0xA7: lax(AddressingMode.ZeroPage); pc += 1; break;
                case 0xB7: lax(AddressingMode.ZeroPage_Y); pc += 1; break;
                case 0xAF: lax(AddressingMode.Absolute); pc += 2; break;
                case 0xBF: lax(AddressingMode.Absolute_Y); pc += 2; break;
                case 0xA3: lax(AddressingMode.Indirect_X); pc += 1; break;
                case 0xB3: lax(AddressingMode.Indirect_Y); pc += 1; break;

                //NOP
                case 0x1A:case 0x3A:case 0x5A:case 0x7A:case 0xDA:case 0xFA: break;

                //RLA
                case 0x27: rla(AddressingMode.ZeroPage); pc += 1; break;
                case 0x37: rla(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x2F: rla(AddressingMode.Absolute); pc += 2; break;
                case 0x3F: rla(AddressingMode.Absolute_X); pc += 2; break;
                case 0x3B: rla(AddressingMode.Absolute_Y); pc += 2; break;
                case 0x23: rla(AddressingMode.Indirect_X); pc += 1; break;
                case 0x33: rla(AddressingMode.Indirect_Y); pc += 1; break;

                //RRA
                case 0x67: rra(AddressingMode.ZeroPage); pc += 1; break;
                case 0x77: rra(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x6F: rra(AddressingMode.Absolute); pc += 2; break;
                case 0x7F: rra(AddressingMode.Absolute_X); pc += 2; break;
                case 0x7B: rra(AddressingMode.Absolute_Y); pc += 2; break;
                case 0x63: rra(AddressingMode.Indirect_X); pc += 1; break;
                case 0x73: rra(AddressingMode.Indirect_Y); pc += 1; break;

                //SBC
                case 0xEB: sbc(AddressingMode.Immediate); pc += 1; break;


                //SLO
                case 0x07: slo(AddressingMode.ZeroPage); pc += 1; break;
                case 0x17: slo(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x0F: slo(AddressingMode.Absolute); pc += 2; break;
                case 0x1F: slo(AddressingMode.Absolute_X); pc += 2; break;
                case 0x1B: slo(AddressingMode.Absolute_Y); pc += 2; break;
                case 0x03: slo(AddressingMode.Indirect_X); pc += 1; break;
                case 0x13: slo(AddressingMode.Indirect_Y); pc += 1; break;

                //SRE
                case 0x47: sre(AddressingMode.ZeroPage); pc += 1; break;
                case 0x57: sre(AddressingMode.ZeroPage_X); pc += 1; break;
                case 0x4F: sre(AddressingMode.Absolute); pc += 2; break;
                case 0x5F: sre(AddressingMode.Absolute_X); pc += 2; break;
                case 0x5B: sre(AddressingMode.Absolute_Y); pc += 2; break;
                case 0x43: sre(AddressingMode.Indirect_X); pc += 1; break;
                case 0x53: sre(AddressingMode.Indirect_Y); pc += 1; break;


                //SXA
                case 0x9E: sxa();pc += 2; break;

                //SYA(SHY)
                case 0x9C: sya(); pc += 2; break;

                //TOP(NOP)
                case 0x0C: top(AddressingMode.Absolute); pc += 2; break;
                case 0x1C: top(AddressingMode.Absolute_X); pc += 2; break;
                case 0x3C: top(AddressingMode.Absolute_X); pc += 2; break;
                case 0x5C: top(AddressingMode.Absolute_X); pc += 2; break;
                case 0x7C: top(AddressingMode.Absolute_X); pc += 2; break;
                case 0xDC: top(AddressingMode.Absolute_X); pc += 2; break;
                case 0xFC: top(AddressingMode.Absolute_X); pc += 2; break;

                //XAA(ANE)
                case 0x8B: xaa(AddressingMode.Immediate); pc += 1; break;

                //XAS
                case 0x9B: xas(AddressingMode.Absolute_Y); pc += 2; break;
                
            /* その他 */
                //NOP 
                case 0xEA: break;

                //BRK 
                case 0x00: return false; 
            }
            
            return true;
            
        }
        private byte ld_register(AddressingMode mode)  
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            update_zero_and_negative_flags(value);
            return value;
        }
        private void st_register(AddressingMode mode,byte value)
        {
            UInt16 addr = get_operand_address(mode);
            mem_write(addr, value);
        }


        private void adc(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value  = mem_read(addr);
            byte c_flg  = get_status_flg(CpuStatus.CarryFlg);
            UInt16 sum  = (UInt16)(register_a + value + c_flg);
            byte result = (byte)sum;

            if (sum > 0xff) 
                { update_status_flg(CpuStatus.CarryFlg,1); }
            else 
                { update_status_flg(CpuStatus.CarryFlg,0); }

            if (((value ^ result) & (register_a ^ result) & 0x80) != 0)
                { update_status_flg(CpuStatus.OverFlowFlg,1); } 
            else
                { update_status_flg(CpuStatus.OverFlowFlg,0); }

            register_a = result;
            update_zero_and_negative_flags(register_a);
        }

        private void and(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a = (byte)(register_a & value);
            update_zero_and_negative_flags(register_a);
        }
        private void asl_Accumulator()
        {
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a >> 7));
            register_a = (byte)(register_a << 1);
            update_zero_and_negative_flags(register_a);
        }
        private byte asl(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value >> 7));
            value <<= 1;
            mem_write(addr, value);
            update_zero_and_negative_flags(value);
            return value;
        }

        private void bit(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);

            if((value & register_a) == 0)
                update_status_flg(CpuStatus.ZeroFlg, 1);
            else
                update_status_flg(CpuStatus.ZeroFlg, 0);

            update_status_flg(CpuStatus.NegativeFlg, (byte)(value >>7));
            update_status_flg(CpuStatus.OverFlowFlg, (byte)((value & 0b0100_0000) >> 6));
        }

        private void cmp(AddressingMode mode,byte target)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            UInt16 diff = (UInt16)(target - value);
            byte c_flg = (byte)(((diff & 0x100) >> 8)^0x01);
            update_status_flg(CpuStatus.CarryFlg, c_flg);
            update_zero_and_negative_flags((byte)diff);
        }

        private byte dec(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            value -= 1;
            mem_write(addr, value);
            update_zero_and_negative_flags(value);
            return value;
        }

        private void eor(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a ^= value;
            update_zero_and_negative_flags(register_a);
        }

        private byte inc(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            value +=1;
            mem_write(addr, value);
            update_zero_and_negative_flags(value);
            return value;
        }
        private void lsr_Accumulator()
        {
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a & 0x01));
            register_a >>= 1;
            update_zero_and_negative_flags(register_a);
        }
        private byte lsr(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value & 0x01));
            value >>= 1;
            mem_write(addr, value);
            update_zero_and_negative_flags(value);
            return value;
        }

        private void ora(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a |= value;
            update_zero_and_negative_flags(register_a);
        }
        private void rol_Accumulator()
        {
            byte old_carry = get_status_flg(CpuStatus.CarryFlg);
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a >> 7));
            register_a <<= 1;
            if (old_carry == 1)
            {
                register_a |= 0b1000_0001;
            }
            update_zero_and_negative_flags(register_a);
        }
        private byte rol(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte old_carry = get_status_flg(CpuStatus.CarryFlg);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value >> 7));
            value <<= 1;
            if(old_carry == 1)
            {
                value |= 0b0000_0001;
            }
            mem_write(addr, value);
            update_status_flg(CpuStatus.NegativeFlg, (byte)(value >> 7));
            return value;
        }
        private void ror_Accumulator()
        {
            byte old_carry = get_status_flg(CpuStatus.CarryFlg);
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a & 0x01));
            register_a >>= 1;
            if (old_carry == 1)
            {
                register_a |= 0b1000_0000;
            }
            update_zero_and_negative_flags(register_a);
        }
        private byte ror(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte old_carry = get_status_flg(CpuStatus.CarryFlg);
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a & 0x01));
            value >>= 1;
            if (old_carry == 1)
            {
                value |= 0b1000_0000;
            }
            mem_write(addr, value);
            update_status_flg(CpuStatus.NegativeFlg, (byte)(value >> 7));
            return value;
        }
        private void sbc(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte c_flg = (byte)(get_status_flg(CpuStatus.CarryFlg) ^ 0x01);
            UInt16 diff = (UInt16)(register_a - value - c_flg);
            byte result = (byte)diff;

            if (diff > 0xff)
                update_status_flg(CpuStatus.CarryFlg, 0);   
            else
                update_status_flg(CpuStatus.CarryFlg, 1);

            if (((~value ^ diff) & (register_a ^ diff) & 0x80) != 0)
                update_status_flg(CpuStatus.OverFlowFlg, 1);
            else
                update_status_flg(CpuStatus.OverFlowFlg, 0);

            register_a = result;
            update_zero_and_negative_flags(register_a);
        }

        private void aac(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a &= value;
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a >> 7));
            update_zero_and_negative_flags(register_a);
        }
        private void aax(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            register_a &= register_x;
            mem_write(addr, register_a);
            update_zero_and_negative_flags(register_a);
        }
        private void arr(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a &= value;
            ror_Accumulator();
            switch (register_a)
            {
                case byte i when (i & 0b0100_0000)!=0 && (i & 0b0010_0000) != 0 :
                    update_status_flg(CpuStatus.CarryFlg, 1);
                    update_status_flg(CpuStatus.OverFlowFlg, 0);
                    break;
                case byte i when (i & 0b0010_0000) != 0: // 5bit
                    update_status_flg(CpuStatus.CarryFlg, 0);
                    update_status_flg(CpuStatus.OverFlowFlg, 1);
                    break;
                case byte i when (i & 0b0100_0000) != 0://6bit
                    update_status_flg(CpuStatus.CarryFlg, 1);
                    update_status_flg(CpuStatus.OverFlowFlg, 0);
                    break;
                default:
                    update_status_flg(CpuStatus.CarryFlg, 0);
                    update_status_flg(CpuStatus.OverFlowFlg, 0);
                    break;
            }
            update_zero_and_negative_flags(register_a);
        }
        private void asr(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a &= value;
            lsr_Accumulator();
        }
        private void atx(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a &= value;
            register_x = register_a;
            update_zero_and_negative_flags(register_x);
        }
        private void axa(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            register_a &= register_x;
            mem_write(addr, (byte)(register_a+7));
        }
        private void axs(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte result = (byte)(register_x & register_a);
            
            if(result <= value)
            {
                update_status_flg(CpuStatus.CarryFlg, 1);
            }
            result -= value;
            register_x = result;
            update_zero_and_negative_flags(register_x);
        }

        private void dcp(AddressingMode mode) 
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte result = (byte)(value - 1);
            if(result > value) update_status_flg(CpuStatus.CarryFlg, 1);
            mem_write(addr, value);
        }
        private void dop(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            //何もしない
        }
        private void isc(AddressingMode mode)
        {
            byte data = inc(mode);
            byte c_flg = (byte)(get_status_flg(CpuStatus.CarryFlg) ^ 0x01);
            UInt16 diff = (UInt16)(register_a - data - c_flg);
            byte result = (byte)diff;

            if (diff > 0xff)
                update_status_flg(CpuStatus.CarryFlg, 0);
            else
                update_status_flg(CpuStatus.CarryFlg, 1);

            if (((~data ^ diff) & (register_a ^ diff) & 0x80) != 0)
                update_status_flg(CpuStatus.OverFlowFlg, 1);
            else
                update_status_flg(CpuStatus.OverFlowFlg, 0);

            register_a = result;
            update_zero_and_negative_flags(register_a);

        }
        private void lar(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte result = (byte)(sp & value);
            register_a = result;
            register_x = result;
            sp = result;
            update_zero_and_negative_flags(result);

        }
        private void lax(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a = value;
            register_x = value;
            update_zero_and_negative_flags(value);

        }
        private void rla(AddressingMode mode) {
            byte data = rol(mode);
            register_a &= data;
            update_zero_and_negative_flags(register_a);
        }
        private void rra(AddressingMode mode) {
            byte data = rol(mode);
            byte c_flg = get_status_flg(CpuStatus.CarryFlg);
            UInt16 sum = (UInt16)(register_a + data + c_flg);
            byte result = (byte)sum;

            if (sum > 0xff)
            { update_status_flg(CpuStatus.CarryFlg, 1); }
            else
            { update_status_flg(CpuStatus.CarryFlg, 0); }

            if (((data ^ result) & (register_a ^ result) & 0x80) != 0)
            { update_status_flg(CpuStatus.OverFlowFlg, 1); }
            else
            { update_status_flg(CpuStatus.OverFlowFlg, 0); }

            register_a = result;
            update_zero_and_negative_flags(register_a);

        }
        private void slo(AddressingMode mode) {
            byte data = asl(mode);
            register_a |= data;
            update_zero_and_negative_flags(register_a);
        }
        

        private void sre(AddressingMode mode) {
            byte data = lsr(mode);
            register_a |= data;
            update_zero_and_negative_flags(register_a);
        }
        private void sxa() {
            UInt16 addr = (UInt16)(mem_read_u16(pc) + register_x);
            byte data = (byte)(register_x & ((addr >> 8) + 1));
            mem_write(addr, data);
        }
        private void sya() {
            UInt16 addr = (UInt16)(mem_read_u16(pc) + register_y);
            byte data = (byte)(register_x & ((addr >> 8) + 1));
            mem_write(addr, data);
        }
        private void top(AddressingMode mode) {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            //何もしない
        }
        private void xaa(AddressingMode mode) {
            register_a = register_x;
            update_zero_and_negative_flags(register_a);

            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a &= value;
            update_zero_and_negative_flags(register_a);
        }
        private void xas(AddressingMode mode) {
            byte data = (byte)(register_a & register_x);
            sp = data;
            UInt16 addr = (UInt16)(mem_read_u16(pc) + register_y);

            data = (byte)(((addr >> 8) + 1 ) & sp);
            mem_write(addr, data);
        }


        private void stack_push(byte value)
        {
            mem_write((UInt16)(STACK_BASE + sp), value);
            sp -= 1;
        }
        private byte stack_pop()
        {
            sp += 1;
            byte value = mem_read((UInt16)(STACK_BASE + sp));
            return value;
        }
        private void stack_push_u16(UInt16 value)
        {
            stack_push((byte)(value >> 8));
            stack_push((byte)(value & 0xff));
        }
        private UInt16 stack_pop_u16()
        {
            byte _lo = stack_pop();
            byte _hi = stack_pop();
            return (UInt16)((_hi << 8) | _lo);
        }
        
        private void branch(bool state)
        {
            if (state)
            {
                sbyte value = (sbyte)mem_read(pc);
                UInt16 jump_addr = (UInt16)(pc + 1 + value);
                pc = jump_addr;
            }
            else {
                pc += 1;
            }
        }
        private void update_status_flg(CpuStatus mode,byte c) 
        {
            byte value;
            if (c == 0)
            {
                value = (byte)((0x01 << (byte)mode)^0xff);
                status = (byte)(status & value);
            }
            else
            {
                value = (byte)(0x01 << (byte)mode);
                status = (byte)(status | value);
            }
        }
        private byte get_status_flg(CpuStatus mode)
        {
            switch(mode)
            {
                case CpuStatus.CarryFlg: 
                    return (byte)(status & 0b0000_0001);
                case CpuStatus.ZeroFlg:  
                    return (byte)((status & 0b0000_0010) >>1);
                case CpuStatus.IRQFlg:   
                    return (byte)((status & 0b0000_0100) >>2);
                case CpuStatus.BreakModeFlg: 
                    return (byte)((status & 0b0001_0000) >>4);
                case CpuStatus.OverFlowFlg:  
                    return (byte)((status & 0b0100_0000) >>6);
                case CpuStatus.NegativeFlg:  
                    return (byte)((status & 0b1000_0000) >>7);
            }
            return 0x00;
        }

        private void update_zero_and_negative_flags(byte result) {

            if (result == 0)
                update_status_flg(CpuStatus.ZeroFlg, 1);
            else
                update_status_flg(CpuStatus.ZeroFlg, 0);

            if ((result & 0b1000_0000) != 0)
                update_status_flg(CpuStatus.NegativeFlg, 1);
            else
                update_status_flg(CpuStatus.NegativeFlg, 0);
        }
    }
}
