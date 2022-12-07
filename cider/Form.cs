
using static cider.GamePad;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace cider
{
    public partial class Form : System.Windows.Forms.Form
    {
        private Cider cider;
        public Form()
        {
            InitializeComponent();
            DoubleBuffered = true;
            KeyUp += new KeyEventHandler(Key_Up);
            KeyDown += new KeyEventHandler(Key_Down);
            Shown += new EventHandler(Form_Shown);
            cider = new Cider(this);
        }
        public void Form_Shown(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "NES|*.nes";
            if (open.ShowDialog() == DialogResult.OK)
            {
                cider.Run(open.FileName);
            }

        }
        private void Key_Down(object sender, KeyEventArgs e)
        {
            cider.gamepad.GetKey(e, KeyState.Down);
        }
        private void Key_Up(object sender, KeyEventArgs e)
        {
            cider.gamepad.GetKey(e, KeyState.Up);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            cider.Drawing(e);
        }
    }
}