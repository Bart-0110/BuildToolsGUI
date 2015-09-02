using System;
using System.Threading;
using System.Windows.Forms;

namespace BuildTools
{
    public partial class BuildTools : Form
    {
        // Delegates
        private delegate void AppendTextDelegate(string text);
        private readonly AppendTextDelegate _appendDelegate;

        private delegate void AppendRawTextDelegat(string text);
        private readonly AppendRawTextDelegat _appendRawDelegate;

        private delegate void DisableDelegate();
        private readonly DisableDelegate _disableDelegate;

        private delegate void EnableDelegate();
        private readonly EnableDelegate _enableDelegate;

        private delegate void ShowProgressDelegate();
        private readonly ShowProgressDelegate _showProgressDelegate;

        private delegate void HideProgressDelegate();
        private readonly HideProgressDelegate _hideProgressDelegate;

        private delegate void IndeterminateProgressDelegate();
        private readonly IndeterminateProgressDelegate _indeterminateProgressDelegate;

        private delegate void ProgressPercentDelegate(int place, int total);
        private readonly ProgressPercentDelegate _progressPercentDelegate;

        // Stuff
        private readonly Runner _runner;
        private string _lastLog = "";

        public BuildTools()
        {
            InitializeComponent();
            _runner = new Runner(this);
            undoBT.Visible = false;
            progress.Visible = false;

            // delegates
            _appendDelegate = AppendText;
            _appendRawDelegate = AppendRawText;
            _disableDelegate = Disable;
            _enableDelegate = Enable;
            _showProgressDelegate = ProgressShow;
            _hideProgressDelegate = ProgressHide;
            _indeterminateProgressDelegate = ProgressIndeterminate;
            _progressPercentDelegate = Progress;
        }

        // Run BuildTools Button Clicked
        private void runBT_Click(object sender, EventArgs e)
        {
            bool update = autoUpdateCB.Checked;
            Thread thread = new Thread(delegate()
            {
                _runner.RunBuildTools(update);
                Enable();
                ProgressHide();
            });
            Disable();
            thread.Start();
        }

        // Update BuildTools Button Clicked
        private void updateBT_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(delegate()
            {
                _runner.UpdateJar();
                Enable();
                ProgressHide();
            });
            Disable();
            thread.Start();
        }

        // Clear Log Button Clicked
        private void clearBT_Click(object sender, EventArgs e)
        {
            _lastLog += outputTB.Text;
            outputTB.Text = "";
            undoBT.Visible = true;
        }

        // Undo Button Clicked
        private void undoBT_Click(object sender, EventArgs e)
        {
            outputTB.Text = _lastLog + outputTB.Text;
            _lastLog = "";
            undoBT.Visible = false;
        }

        /// <summary>
        /// Append text to the output textbox. This method will ensure thread safety on the AppendText method call.
        /// This will also automatically append the given text with a new-line character.
        /// </summary>
        /// <param name="text">Text to append to the output textbox.</param>
        public void AppendText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(_appendDelegate, text);
            }
            else
            {
                outputTB.AppendText("--- " + text + "\n");
                outputTB.ScrollToCaret();
            }
        }

        public void AppendRawText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(_appendRawDelegate, text);
            }
            else
            {
                outputTB.AppendText(text + "\n");
            }
        }

        // Disable
        private void Disable()
        {
            if (InvokeRequired)
            {
                Invoke(_disableDelegate);
            }
            else
            {
                updateBT.Enabled = false;
                runBT.Enabled = false;
            }
            
        }

        // Enable
        private void Enable()
        {
            if (InvokeRequired)
            {
                Invoke(_enableDelegate);
            }
            else
            {
                updateBT.Enabled = true;
                runBT.Enabled = true;
            }
        }

        // Progress Show
        public void ProgressShow()
        {
            if (InvokeRequired)
            {
                Invoke(_showProgressDelegate);
            }
            else
            {
                progress.Visible = true;
            }
        }

        // Progress Hide
        public void ProgressHide()
        {
            if (InvokeRequired)
            {
                Invoke(_hideProgressDelegate);
            }
            else
            {
                progress.Visible = false;
            }
        }

        // Progress indeterminate
        public void ProgressIndeterminate()
        {
            if (InvokeRequired)
            {
                Invoke(_indeterminateProgressDelegate);
            }
            else
            {
                progress.Style = ProgressBarStyle.Marquee;
            }
        }

        // Progress percent
        public void Progress(int place, int total)
        {
            if (InvokeRequired)
            {
                Invoke(_progressPercentDelegate, place, total);
            }
            else
            {
                progress.Style = ProgressBarStyle.Continuous;
                progress.Minimum = 0;
                progress.Maximum = total;
                progress.Value = place;
            }
        }

        private void BuildTools_FormClosed(object sender, FormClosedEventArgs e)
        {
            _runner.CleanUp();
            Application.Exit();
            Environment.Exit(0);
        }
    }
}
