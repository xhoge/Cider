
namespace cider
{
    public partial class Form : System.Windows.Forms.Form
    {
        Cider cider;
        bool debug = true;
        public Form()
        {
            InitializeComponent();
            cider = new Cider(this);
            KeyUp += new KeyEventHandler(Key_Up);
            KeyDown += new KeyEventHandler(Key_Down);
            cider.Run("hoge");
        }

        private void Key_Down(object sender, KeyEventArgs e)
        {
            if (debug) this.Text = e.KeyData.ToString();
        }
        private void Key_Up(object sender, KeyEventArgs e)
        {
            if (debug) this.Text = e.KeyData.ToString();
        }
    }
}