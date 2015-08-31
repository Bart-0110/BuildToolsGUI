using System;
using System.Threading;
using System.Windows.Forms;

namespace BuildTools
{
    public partial class BuildTools : Form
    {
        // Delegates
        private delegate void AppendTextDelegate(string text);
        private AppendTextDelegate ThisDelegate;

        private delegate void DisableDelegate();
        private DisableDelegate ThisDisableDelegate;

        private delegate void EnableDelegate();
        private EnableDelegate ThisEnableDelegate;

        // Stuff
        private readonly Runner _runner;
        private string lastLog = "";

        public BuildTools()
        {
            InitializeComponent();
            _runner = new Runner(this);
            undoBT.Visible = false;
            ThisDelegate = AppendText;
            ThisDisableDelegate = DisableTS;
            ThisEnableDelegate = EnableTS;
        }

        // Run BuildTools Button Clicked
        private void runBT_Click(object sender, EventArgs e)
        {
            bool update = autoUpdateCB.Checked;
            Thread thread = new Thread(delegate()
            {
                _runner.RunBuildTools(update);
                Enable();
            });
            Disable();
            thread.Start();
        }

        // Update BuildTools Button Clicked
        private void updateBT_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(delegate()
            {
                _runner.RunUpdate();
                Enable();
            });
            Disable();
            thread.Start();
        }

        // Clear Log Button Clicked
        private void clearBT_Click(object sender, EventArgs e)
        {
            lastLog = outputTB.Text;
            outputTB.Text = "";
            undoBT.Visible = true;
        }

        // Undo Button Clicked
        private void undoBT_Click(object sender, EventArgs e)
        {
            outputTB.Text = lastLog + outputTB.Text;
            lastLog = "";
            undoBT.Visible = false;
        }

        /// <summary>
        /// Append text to the output textbox. This method will ensure thread safety on the AppendText method call.
        /// This will also automatically append the given text with a new-line character.
        /// </summary>
        /// <param name="text">Text to append to the output textbox.</param>
        public void AppendText(string text)
        {
            if (outputTB.InvokeRequired)
            {
                Invoke(ThisDelegate, text);
            }
            else
            {
                outputTB.AppendText(text + "\n");
            }
        }

        // Disable
        private void Disable()
        {
            Invoke(ThisDisableDelegate);
        }

        private void DisableTS()
        {
            updateBT.Enabled = false;
            runBT.Enabled = false;
        }

        // Enable
        private void Enable()
        {
            Invoke(ThisEnableDelegate);
        }

        private void EnableTS()
        {
            updateBT.Enabled = true;
            runBT.Enabled = true;
        }
    }
}
