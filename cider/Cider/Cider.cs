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
            cpu = new CPU();
            cpu.test1();
            cpu.test2();

        }
        
    }
}
