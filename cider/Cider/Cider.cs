using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace cider
{

    internal class Cider
    {
        Form window;
        public Cider(Form window) { 
            this.window = window;
        }

        private CPU cpu;
        private Bus bus;
        public GamePad gamepad;
        public Cartridge cartridge;

        public void Run(string rompath)
        {

            byte[] rom = File.ReadAllBytes(rompath);
            cartridge = new Cartridge();
            if(!cartridge.LoadRom(rom))
            {
                new Exception("読み込みに失敗しました");
            }
            bus = new Bus(cartridge);
            cpu = new CPU(bus);
            gamepad = new GamePad();
            cpu.reset();

            Int32[] Bits = new int[32 * 32];
            GCHandle bitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap img = new Bitmap(32, 32, 32*4 , PixelFormat.Format32bppRgb, bitsHandle.AddrOfPinnedObject());
            window.monitor.Image = img;
            bool progress =true;
            Random r1 = new Random();
            

            Task myTask = Task.Run(() =>
            {
                while(progress)
                {
                    cpu.mem_write(0xfe, (byte)r1.Next(1,16));
                    cpu.mem_write(0xff, gamepad.key_code);
                    progress = cpu.exec();
                    
                    foreach (UInt16 i in Enumerable.Range(0x200, 0x400))
                    {
                        Int32 c = color(cpu.mem_read(i));
                        if (Bits[i - 0x200] != c) {
                            Bits[i - 0x200] = c;
                        }
                    }

                    Thread.Sleep(1/10);
                    window.monitor.Invalidate();
                }
            });
        }
        private Int32 color(byte value)
        {
            switch (value) {
                case 0: return ColorTranslator.ToWin32(Color.Black);
                case 1: return ColorTranslator.ToWin32(Color.White);
                case 2: case 9: return ColorTranslator.ToWin32(Color.Gray);
                case 3: case 10: return ColorTranslator.ToWin32(Color.Red);
                case 4: case 11: return ColorTranslator.ToWin32(Color.Green);
                case 5: case 12: return ColorTranslator.ToWin32(Color.Blue);
                case 6: case 13: return ColorTranslator.ToWin32(Color.Magenta);
                case 7: case 14: return ColorTranslator.ToWin32(Color.Yellow);
                default: return ColorTranslator.ToWin32(Color.Cyan);
            }
        }
        
    }
}
