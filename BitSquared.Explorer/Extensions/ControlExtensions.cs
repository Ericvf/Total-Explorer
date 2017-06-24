using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace BitSquared.Explorer.Extensions
{
    public static class ControlExtensions
    {
        public static Rectangle ScreenRectangle(this Control control)
        {
            return control.RectangleToScreen(control.ClientRectangle);
        }
    }
}
