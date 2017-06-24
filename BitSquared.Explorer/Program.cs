using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BitSquared.Explorer.Tools;
using System.Diagnostics;

namespace BitSquared.Explorer
{
    static class Program
    {
        static MainForm form;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
      
            bool isFirst = SingleInstanceApp.InitializeAsFirstInstance(Application.ProductName, SignalExternalCommandLineArgs);
            if (isFirst)
            {
                Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
                var args = SingleInstanceApp.GetCommandLineArgs(Application.ProductName);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                form = new MainForm(args);
                Application.Run(form);
            }

        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                var msg = "Zeus has given you the opportunity to submit an error report!" + Environment.NewLine +
                    "Do you want to sacrifice your time to do this?!" + Environment.NewLine + Environment.NewLine +
                    e.Exception.ToString();

                var result = MessageBox.Show(msg, "Unhandled exception! xD", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    string filename = "mailto:e.feggelen@gmail.com?subject=[TotalExplorer] Unhandled Exception&body=" + e.Exception.Message
                        + Environment.NewLine + Environment.NewLine + e.Exception.StackTrace;

                    Process myProcess = new Process();
                    myProcess.StartInfo.FileName = filename;
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.RedirectStandardOutput = false;
                    myProcess.Start();
                }
                else
                {
                    Application.Exit();
                }
            }
            catch
            {
            }
        }

        static void SignalExternalCommandLineArgs(IList<string> args)
        {
            form.ProcessCommandLine(args);
        }
    }
}
