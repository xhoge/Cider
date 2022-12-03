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
            register_y = 0;
            status = 0;
            sp = 0;
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
                pc+=1;
                switch (code) {

                /* 転送命令 */
                    //LDA 
                    case 0xA9: lda(AddressingMode.Immediate) ; pc +=1; break;
                    case 0xA5: lda(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0xB5: lda(AddressingMode.ZeroPage_X); pc +=1; break;
                    case 0xAD: lda(AddressingMode.Absolute)  ; pc +=2; break;
                    case 0xBD: lda(AddressingMode.Absolute_X); pc +=2; break;
                    case 0xB9: lda(AddressingMode.Absolute_Y); pc +=2; break;
                    case 0xA1: lda(AddressingMode.Indirect_X); pc +=1; break;
                    case 0xB1: ldx(AddressingMode.Indirect_Y); pc +=1; break;
                    
                    //LDX 
                    case 0xA2: ldx(AddressingMode.Immediate) ; pc +=1; break;
                    case 0xA6: ldx(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0xB6: ldx(AddressingMode.ZeroPage_Y); pc +=1; break;
                    case 0xAE: ldx(AddressingMode.Absolute)  ; pc +=2; break;
                    case 0xBE: ldx(AddressingMode.Absolute_Y); pc +=2; break;

                    
                    //LDY 
                    case 0xA0: ldx(AddressingMode.Immediate) ; pc +=1; break;
                    case 0xA4: ldx(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0xB4: ldx(AddressingMode.ZeroPage_X); pc +=1; break;
                    case 0xAC: ldx(AddressingMode.Absolute)  ; pc +=2; break;
                    case 0xBC: ldx(AddressingMode.Absolute_X); pc +=2; break;

                    //STA 
                    case 0x85: sta(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0x95: sta(AddressingMode.ZeroPage_X); pc +=1; break;
                    case 0x8D: sta(AddressingMode.Absolute)  ; pc +=2; break;
                    case 0x9D: sta(AddressingMode.Absolute_X); pc +=2; break;
                    case 0x99: sta(AddressingMode.Absolute_Y); pc +=2; break;
                    case 0x81: sta(AddressingMode.Indirect_X); pc +=1; break;
                    case 0x91: sta(AddressingMode.Indirect_Y); pc +=1; break;
                    
                    //STX 
                    case 0x86: stx(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0x96: stx(AddressingMode.ZeroPage_Y); pc +=1; break;
                    case 0x8E: stx(AddressingMode.Absolute)  ; pc +=2; break;
                    
                    //STY 
                    case 0x84: sty(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0x94: sty(AddressingMode.ZeroPage_X); pc +=1; break;
                    case 0x8C: sty(AddressingMode.Absolute)  ; pc +=2; break;

                    //TAX 
                    case 0xAA: tax(); break;

                    //TAY 
                    case 0xA8: tay(); break;

                    //TSX 
                    case 0xBA: tsx(); break;

                    //TXA 
                    case 0x8A: txa(); break;

                    //TXS 
                    case 0x9A: txs(); break;

                    //TYA 
                    case 0x98: tya(); break;
                    
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
                    case 0xC9: and(AddressingMode.Immediate) ; pc +=1; break;
                    case 0xC5: and(AddressingMode.ZeroPage)  ; pc +=1; break;
                    case 0xD5: and(AddressingMode.ZeroPage_X); pc +=1; break;
                    case 0xCD: and(AddressingMode.Absolute)  ; pc +=2; break;
                    case 0xDD: and(AddressingMode.Absolute_X); pc +=2; break;
                    case 0xD9: and(AddressingMode.Absolute_Y); pc +=2; break;
                    case 0xC1: and(AddressingMode.Indirect_X); pc +=1; break;
                    case 0xD1: and(AddressingMode.Indirect_Y); pc +=1; break;

                    //CPX 
                    case 0xE0: cpx(AddressingMode.Immediate); pc +=1; break;
                    case 0xE4: cpx(AddressingMode.ZeroPage) ; pc +=1; break;
                    case 0xEC: cpx(AddressingMode.Absolute) ; pc +=2; break;

                    //CPY 
                    case 0xC0: cpx(AddressingMode.Immediate); pc +=1; break;
                    case 0xC4: cpx(AddressingMode.ZeroPage) ; pc +=1; break;
                    case 0xCC: cpx(AddressingMode.Absolute) ; pc +=2; break;

                    //DEC 
                    case 0xC6: dec(AddressingMode.Immediate); pc +=1; break;
                    case 0xD6: dec(AddressingMode.ZeroPage) ; pc +=1; break;
                    case 0xCE: dec(AddressingMode.Absolute) ; pc +=2; break;
                    case 0xDE: dec(AddressingMode.ZeroPage) ; pc +=2; break;

                    //DEX 
                    case 0xCA: register_x++; break;

                    //DEY 
                    case 0x88: register_y--; break;
                    
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
                    case 0xE8: register_x++; break;

                    //INY 
                    case 0xC8: register_y++; break;
                    
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
                    case 0x08: stack_push(status); break;

                    //PLA
                    case 0x68: register_a = stack_pop(); break;

                    //PLP
                    case 0x28: status = stack_pop(); break;

                /* ジャンプ命令 */
                    //JMP
                    case 0x4C: pc = get_operand_address(AddressingMode.Absolute); break;
                    case 0x6C:
                        UInt16 addr =  mem_read_u16(pc);
                        UInt16 jump_addr = 0;
                        if ((UInt16)(addr & 0x00ff) == 0x00ff)
                        {
                            byte _lo = mem_read(addr);
                            byte _hi = mem_read((UInt16)(addr & 0xff00));
                            jump_addr = (UInt16)((_hi << 8) | _lo);
                        }
                        else 
                        {
                            jump_addr = mem_read_u16(addr);
                        }
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
                        break;

                /* 分岐命令 */
                    //BCC
                    case 0x90: branch(!get_status_flg(CpuStatus.CarryFlg)); pc +=1; break;

                    //BCS
                    case 0xB0: branch(get_status_flg(CpuStatus.CarryFlg)); pc +=1; break;

                    //BEQ
                    case 0xF0: branch(get_status_flg(CpuStatus.ZeroFlg)); pc +=1; break;

                    //BMI
                    case 0x30: branch(get_status_flg(CpuStatus.NegativeFlg)); pc +=1; break;

                    //BNE
                    case 0xD0: branch(!get_status_flg(CpuStatus.ZeroFlg)); pc +=1; break;

                    //BPL
                    case 0x10: branch(!get_status_flg(CpuStatus.NegativeFlg)); pc +=1; break;
                    //BVC
                    case 0x50: branch(!get_status_flg(CpuStatus.OverFlowFlg)); pc +=1; break;

                    //BVS
                    case 0x70: branch(get_status_flg(CpuStatus.OverFlowFlg)); pc +=1; break;

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

                    /* その他 */
                    //NOP 
                    case 0xEA: break;

                    //BRK 
                    case 0x00: return; 
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

        private void ldx(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_x = value;
            update_zero_and_negative_flags(register_x);
        }

        private void ldy(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_y = value;
            update_zero_and_negative_flags(register_y);
        }

        private void sta(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            mem_write(addr,register_a);
        }

        private void stx(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            mem_write(addr, register_x);
        }

        private void sty(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            mem_write(addr, register_y);
        }

        private void tax()
        { register_x = register_a; update_zero_and_negative_flags(register_x); }
        
        private void tay() 
        { register_y = register_a; update_zero_and_negative_flags(register_y); }
        
        private void tsx() 
        { register_x = sp; update_zero_and_negative_flags(register_x); }
        
        private void txa() 
        { register_x = register_a; update_zero_and_negative_flags(register_x); }
        
        private void txs() 
        { sp = register_x; update_zero_and_negative_flags(sp); }

        private void tya() 
        { register_y = register_a; update_zero_and_negative_flags(register_y); }

        private void adc(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value  = mem_read(addr);
            byte c_flg  = (byte)(status & 0b0000_0001);
            UInt16 sum  = (byte)(register_a + value + c_flg);
            byte result = (byte)sum;
            if (sum > 0xff) 
            {
                update_status_flg(CpuStatus.CarryFlg,1);
            }
            else 
            {
                update_status_flg(CpuStatus.CarryFlg,0);
            }
            if (((value ^ result) & (register_a ^ result) & 0x80) != 0)
            {
                update_status_flg(CpuStatus.OverFlowFlg,1);
            } else
            {
                update_status_flg(CpuStatus.OverFlowFlg,0);
            }
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
        private void asl(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value >> 7));
            value = (byte)(value << 1);
            update_zero_and_negative_flags(value);
        }

        private void bit(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte result = (byte)(value & 0xff);
            update_zero_and_negative_flags(result);
        }

        private void cmp(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            UInt16 diff = (byte)(register_a - value);
            byte result = (byte)(diff & 0x100);
            update_zero_and_negative_flags((byte)diff);
        }

        private void cpx(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            UInt16 diff = (byte)(register_a - value);
            byte result = (byte)(diff & 0x100);
            update_zero_and_negative_flags((byte)diff);
        }

        private void cpy(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            UInt16 diff = (byte)(register_y - value);
            byte result = (byte)(diff & 0x100);
            update_zero_and_negative_flags((byte)diff);
        }

        private void dec(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = (byte)(mem_read(addr) - 1);
            mem_write(addr, value);
        }

        private void eor(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a = (byte)(register_a ^ value);
            update_zero_and_negative_flags(register_a);
        }

        private void inc(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            value +=1;
            mem_write(addr, value);
        }
        private void lsr_Accumulator()
        {
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a & 0x01));
            register_a >>= 1;
            update_zero_and_negative_flags(register_a);
        }
        private void lsr(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value & 0x01));
            value >>= 1;
            update_zero_and_negative_flags(value);
        }

        private void ora(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            register_a = (byte)(register_a | value);
            update_zero_and_negative_flags(register_a);
        }
        private void rol_Accumulator()
        {
            byte c_flg = (byte)(status & 0b0000_0001);
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a >> 0x01));
            register_a >>= 1;
            if (c_flg == 1)
            {
                register_a = (byte)(register_a | 0b1000_0000);
            }
            update_zero_and_negative_flags(register_a);
        }
        private void rol(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte c_flg = (byte)(status & 0b0000_0001);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value >> 0x01));
            value >>= 1;
            if(c_flg == 1)
            {
                value = (byte)(value | 0b1000_0000);
            }
            update_zero_and_negative_flags(value);
        }
        private void ror_Accumulator()
        {
            byte c_flg = (byte)(status & 0b0000_0001);
            update_status_flg(CpuStatus.CarryFlg, (byte)(register_a << 0x01));
            register_a <<= 1;
            if (c_flg == 1)
            {
                register_a = (byte)(register_a | 0b0000_0001);
            }
            update_zero_and_negative_flags(register_a);
        }
        private void ror(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte c_flg = (byte)(status & 0b0000_0001);
            update_status_flg(CpuStatus.CarryFlg, (byte)(value << 0x01));
            value <<= 1;
            if (c_flg == 1)
            {
                value = (byte)(value | 0b0000_0001);
            }
            update_zero_and_negative_flags(value);
        }
        private void sbc(AddressingMode mode)
        {
            UInt16 addr = get_operand_address(mode);
            byte value = mem_read(addr);
            byte c_flg = (byte)((status & 0x01)^0x01);
            UInt16 sum = (byte)(register_a - value - c_flg);
            byte result = (byte)sum;
            if (sum > 0xff)
            {
                update_status_flg(CpuStatus.CarryFlg, 1);
            }
            else
            {
                update_status_flg(CpuStatus.CarryFlg, 0);
            }
            if (((value ^ result) & (register_a ^ result) & 0x80) != 0)
            {
                update_status_flg(CpuStatus.OverFlowFlg, 1);
            }
            else
            {
                update_status_flg(CpuStatus.OverFlowFlg, 0);
            }
            register_a = result;
            update_zero_and_negative_flags(register_a);
        }

        private void stack_push(byte value)
        {
            sp += 1;
            mem_write(sp, value);
        }
        private byte stack_pop()
        {
            byte value = mem_read(sp);
            sp -= 1;
            return value;
        }
        private void stack_push_u16(UInt16 value)
        {
            byte _lo = (byte)(value & 0xff);
            byte _hi = (byte)(value >> 8);
            mem_write(sp, _hi);
            mem_write((UInt16)(sp + 1), _lo);
            sp += 2;
        }
        private UInt16 stack_pop_u16()
        {
            byte _lo = mem_read(sp);
            byte _hi = mem_read((UInt16)(sp + 1));
            UInt16 value = (UInt16)((_hi << 8) | _lo);
            sp -= 2;
            return value;
        }
        
        private void branch(bool state)
        {
            if(state)
            {
                sbyte value = (sbyte)mem_read(pc);
                UInt16 jump_addr = (UInt16)(pc + 1 + value);
                pc = jump_addr;
            }
        }
        private void update_status_flg(CpuStatus mode,byte c) 
        {
            byte value;
            if (c == 0)
            {
                value = (byte)(0xfe << (byte)mode);
                status = (byte)(status & value);
            }
            else
            {
                value = (byte)(0x01 << (byte)mode);
                status = (byte)(status | value);
            }
        }
        private bool get_status_flg(CpuStatus mode)
        {
            switch(mode)
            {
                case CpuStatus.CarryFlg: 
                    return ((status & 0b0000_0001)==1);
                case CpuStatus.ZeroFlg: 
                    return ((status & 0b0000_0010) == 1);
                case CpuStatus.IRQFlg: 
                    return ((status & 0b0000_0100) == 1);
                case CpuStatus.BreakModeFlg: 
                    return ((status & 0b0001_0000) == 1);
                case CpuStatus.OverFlowFlg: 
                    return ((status & 0b0100_0000) == 1);
                case CpuStatus.NegativeFlg: 
                    return ((status & 0b1000_0000) == 1);
            }
            return false;
        }

        private void update_zero_and_negative_flags(byte result) {

            if (result == 0) 
                status = (byte)(status | 0b0000_0010);
            else
                status = (byte)(status & 0b0111_1111);

            if ((byte)(result & 0b1000_0000) != 0)
                status = (byte)(status | 0b1000_0000);
            else
                status = (byte)(status & 0b0111_1111);
        }
    }
}
