using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing;

namespace BitSquared.Explorer.Controls
{
    public class BrowserTabPage : TabPage
    {
        public Bitmap ThumbnailBitmap { get; set; }
        public bool IsLocked
        {
            get
            {
                var control = this.GetControl();
                if (control != null)
                    return control.IsLocked;

                return false;
            }
        }

        public BrowserTabPage() :
            base()
        {
            this.DoubleBuffered = true;
        }

        public browserExplorerTab GetControl()
        {
            if (this.HasChildren)
            {
                var c = this.Controls[0] as browserExplorerTab;
                return c;
            }

            return null;
        }

        public ShellObject GetLocation()
        {
            var control = this.GetControl();
            if (control != null)
                return control.CurrentLocation;

            return null;
        }


        public void SetText(string tabName)
        {
            if (tabName.Length > 25) tabName = tabName.Substring(0, 25) + "...";
            this.Text = tabName + "  ";
            //if (this.GetControl().IsLocked)
                this.Text = "     " + tabName + "  ";
        }

        internal void SetImage(ShellObject currentLocation)
        {
            this.ThumbnailBitmap = currentLocation.Thumbnail.SmallBitmap;
            this.ThumbnailBitmap.MakeTransparent();
        }
    }
}
