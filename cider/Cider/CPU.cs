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
        private byte register_a;
        private byte register_x;
        private byte register_y;
        private byte sp;
        private byte status;
        private byte[] mem;
        private UInt16 pc;
        public CPU() {
            register_a= 0;
            status= 0;
            pc= 0;
        }
        
        public void interpret(byte[] program)
        {
            pc = 0;
            while (true)
            {
                byte opcode = program[pc];
                byte param;
                pc++;
                switch (opcode) {
                    case 0xA9: // LDA
                        param = program[pc];
                        pc++;
                        lda(param);
                        break;
                    case 0xAA:
                        tax();
                        break;
                    case 0xc0:
                        param = program[pc];
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
        private void lda(byte value) 
        {
            register_a = value;
            update_zero_and_negative_flags(register_a);
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
        public void test1()
        {
            register_a = 10;
            interpret(new byte[] { 0xaa, 0x00 });
            Debug.Assert(register_x == 10);
        }
        public void test2()
        {
            register_x = 0xff;
            interpret(new byte[] { 0xe8, 0xe8, 0x00 });
            Debug.Assert(register_x == 1);
        }
    }
}
