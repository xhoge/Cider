using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cider
{
    
    internal class Cider
    {
        Form window;
        public Cider(Form window) { 
            this.window = window;
        }

        private CPU cpu;

        public void Run(string romName)
        {
            
            //cpu_test1();
            //cpu_test2();
            cpu_test3();
        }
        public void cpu_test1()
        {
            cpu = new CPU();
            cpu.register_a = 10;
            cpu.mem_load_and_run(new byte[] { 0xaa, 0x00 });
            Debug.Assert(cpu.register_x != 10);
        }
        public void cpu_test2()
        {
            cpu = new CPU();
            cpu.register_x = 0xff;
            cpu.mem_load_and_run(new byte[] { 0xe8, 0xe8, 0x00 });
            Debug.Assert(cpu.register_x != 1);
        }
        public void cpu_test3()
        {
            cpu = new CPU();
            cpu.mem_write(0x10, 0x55);
            
            Debug.Assert(cpu.mem_read(0x10) == 0x55);
            cpu.mem_load_and_run(new byte[] { 0xa5, 0x10, 0x00 });
            Debug.Assert(cpu.register_a == 0x55);
        }
    }
}
