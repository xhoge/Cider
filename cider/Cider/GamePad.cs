using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cider
{
    internal class GamePad
    {
        public enum KeyState
        {
            Up,
            Down,
        }

        public byte key_code;

        public GamePad() {
        }
        public void GetKey(KeyEventArgs e,KeyState state)
        {
            if(state ==KeyState.Down)
            {
                switch (e.KeyData)
                {
                    case Keys.W: key_code = 0x77; return;
                    case Keys.A: key_code = 0x61; return;
                    case Keys.S: key_code = 0x73; return;
                    case Keys.D: key_code = 0x64; return;
                }
            }
        }
    }
}
