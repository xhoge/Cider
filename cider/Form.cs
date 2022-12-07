
using static cider.GamePad;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace cider
{
    public partial class Form : System.Windows.Forms.Form
    {
        private Cider cider;
        private bool debug = true;
        public PictureBox monitor;
        public Form()
        {
            InitializeComponent();

            monitor = new PictureBox();
            ClientSize = new Size(320, 320);
            monitor.Location = new Point(0, 0);
            monitor.Name = "monitor";
            monitor.Size = new Size(320, 320);
            monitor.SizeMode = PictureBoxSizeMode.StretchImage;
            monitor.TabIndex = 0;
            Controls.Add(this.monitor);
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
    }
}