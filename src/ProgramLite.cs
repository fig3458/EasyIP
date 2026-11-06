using System;
using System.Windows.Forms;

namespace EasyIP
{
    internal static class ProgramLite
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainFormLite());
        }
    }
}
