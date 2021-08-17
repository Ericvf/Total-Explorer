using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fex.TotalExplorer.Controls
{
    public class BrowserTabControl : TabControl
    {
        public bool ShowAppByFex { get; set; }

        private BrowserTabPage dragSource;

        public BrowserTabControl()
            : base()
        {
            this.SizeMode = TabSizeMode.Normal;
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.ItemSize = new Size(0, 20);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (this.TabCount == 2)
                return;

            var selectedTab = this.SelectedTab;
            this.SelectedIndex = Math.Max(this.SelectedIndex - 1, 0);
            this.TabPages.Remove(selectedTab);

            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                this.dragSource = this.GetTabPageFromXY(e.X, e.Y);

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right && this.dragSource != null)
            {
                var currTabPage = this.GetTabPageFromXY(e.X, e.Y);
                if (currTabPage != null)
                {
                    var currentIndex = TabPages.IndexOf(currTabPage);
                    var sourceIndex = TabPages.IndexOf(dragSource);

                    if (currentIndex == this.TabCount - 1)
                        this.Cursor = Cursors.No;
                    else if (currentIndex < sourceIndex)
                        this.Cursor = Cursors.PanWest;
                    else if (currentIndex > sourceIndex)
                        this.Cursor = Cursors.PanEast;
                }
                else
                {
                    this.Cursor = Cursors.No;
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right && this.dragSource != null)
            {
                var currTabPage = this.GetTabPageFromXY(e.X, e.Y);
                if (currTabPage != null && this.dragSource != currTabPage)
                {
                    var currentIndex = TabPages.IndexOf(currTabPage);
                    var sourceIndex = TabPages.IndexOf(dragSource);

                    var currRect = this.GetTabRect(currentIndex);
                    this.TabPages.Remove(dragSource);
                    this.TabPages.Insert(Math.Min(currentIndex, this.TabCount - 1), dragSource);
                    this.SelectedTab = dragSource;
                }
            }

            this.Cursor = Cursors.Default;
            base.OnMouseUp(e);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index >= this.TabCount)
                return;

            var tabPage = this.TabPages[e.Index] as BrowserTabPage;
            var tabArea = this.GetTabRect(e.Index);
            tabArea = new Rectangle(tabArea.X, tabArea.Y, tabArea.Width, tabArea.Height + 2); // These two pixels are needed for the line at the bottom

            FontStyle headerFs = FontStyle.Regular;

            // Default background, grey gradient
            Brush headerBg = new LinearGradientBrush(tabArea,
                                    SystemColors.ControlLight, 
                                    SystemColors.Control,
                                    LinearGradientMode.Vertical);

            // Overwrite the background for locked tabs          
            if (tabPage.IsLocked)
            {
                headerBg = new LinearGradientBrush(tabArea,
                    Color.FromArgb(255, 255, 200), SystemColors.ControlLight,
                    LinearGradientMode.Vertical);
            }

            // If the tab is selected
            if (e.Index == this.SelectedIndex)
            {
                // Make it bold
                if (this.ContainsFocus)
                    headerFs = FontStyle.Underline;
                
                // If tab is locked
                if (tabPage.IsLocked)
                {
                    // Yellow to white
                    headerBg = new LinearGradientBrush(tabArea,
                            Color.FromArgb(255, 255, 200), 
                            Color.White,
                            LinearGradientMode.Vertical);
                }
                else
                {
                    // Grey to white
                    headerBg = new LinearGradientBrush(tabArea,
                       SystemColors.ControlLight, 
                       Color.White,
                       LinearGradientMode.Vertical);
                }
            }

            // This only happens once
            if (e.Index == 0)
            {
                var tabRect = this.GetTabRect(this.TabPages.Count - 1);
                var tabDarkArea = new RectangleF(
                    tabRect.X + tabRect.Width, 
                    tabRect.Y - 5,
                    this.Width - (tabRect.X + tabRect.Width), 
                    tabRect.Height + 5);

                var textArea = new RectangleF(
                    tabRect.X + tabRect.Width, 
                    tabRect.Y + 2,
                    this.Width - (tabRect.X + tabRect.Width), 
                    tabRect.Height);

                // Overwrite the default gray bg color
                e.Graphics.FillRectangle(new SolidBrush(this.Parent.BackColor), tabDarkArea);

                // Show logo
                if (this.ShowAppByFex)
                {
                    var appbyfexFont = new Font(new FontFamily("Segoe UI"), (float)8, FontStyle.Regular, GraphicsUnit.Point);
                    e.Graphics.DrawString(@"appbyfex v1.0", appbyfexFont, new SolidBrush(SystemColors.ControlDarkDark), textArea.Right - 50, textArea.Y);
                }
            }

            e.Graphics.FillRectangle(headerBg, tabArea);


            // Thumbnail for tab
            if (tabPage.ThumbnailBitmap != null)
                e.Graphics.DrawImage(tabPage.ThumbnailBitmap /*Resources.folder_lock*/, 
                    new Rectangle(tabArea.X + 2, tabArea.Y + 2, 15, 15));

            // Draw string
            int offsetX = 3; int offsetY = 5;
            var headerFont = new Font(new FontFamily("Segoe UI"), (float)8, headerFs, GraphicsUnit.Point);
            e.Graphics.DrawString(this.TabPages[e.Index].Text, headerFont, new SolidBrush(Color.Black), new PointF(tabArea.X + offsetX, tabArea.Y + offsetY));

            // Bottom line
            if (e.Index == this.SelectedIndex && this.ContainsFocus)
            {
                e.Graphics.DrawLine(new Pen(Color.White), new Point(tabArea.X, tabArea.Y + tabArea.Height - 1), new Point(tabArea.X + tabArea.Width, tabArea.Y + tabArea.Height - 1));
            }
            else
            {
                e.Graphics.DrawLine(new Pen(SystemColors.ControlLight), new Point(tabArea.X, tabArea.Y + tabArea.Height - 1), new Point(tabArea.X + tabArea.Width, tabArea.Y + tabArea.Height - 1));
            }
        }

        public BrowserTabPage GetTabPageFromXY(int x, int y)
        {
            for (int i = 0; i < this.TabCount; i++)
            {
                if (this.GetTabRect(i).Contains(x, y))
                    return this.TabPages[i] as BrowserTabPage;
            }

            return null;
        }

        public browserExplorerTab GetControl()
        {
            if (this.SelectedTab != null)
            {
                var selectedTab = this.SelectedTab as BrowserTabPage;
                return selectedTab.GetControl();
            }

            return null;
        }

        public BrowserTabPage AddTab(string tabName, ShellObject shellObject, int index = -1, bool select = false, bool isLocked = false)
        {
            var tabPage = new BrowserTabPage();
            if (tabName != "...")
            {
                var c = new browserExplorerTab(shellObject);
                tabPage.Controls.Add(c);
                c.IsLocked = isLocked;
                c.SetText(tabName);

                if (shellObject != null)
                    c.SetImage(shellObject);
            }
            else
            {
                tabPage.Text = tabName;
            }

            if (index < 0)
            {
                this.TabPages.Add(tabPage);
            }
            else
            {
                this.TabPages.Insert(index, tabPage);
            }

            if (select)
                this.SelectedTab = tabPage;

            return tabPage;
        }

        public void SwitchTab(BrowserTabControl targetTab)
        {
            if (this.ContainsFocus)
            {
                var selectedTab = this.SelectedTab as BrowserTabPage;
                if (selectedTab != null)
                {
                    int index = Math.Max(0, targetTab.SelectedIndex + 1);
                    this.SelectedIndex = Math.Max(0, this.SelectedIndex - 1);

                    this.TabPages.Remove(selectedTab);
                    targetTab.TabPages.Insert(index, selectedTab);
                    targetTab.SelectedTab = selectedTab;

                    if (selectedTab.HasChildren)
                        selectedTab.GetControl().Activate();
                }
            }
        }

        public void Activate()
        {
            var control = this.GetControl();

            if (control != null)
                control.Activate();
        }

        public void SelectPrevTab()
        {
            int index = this.SelectedIndex - 1;
            if (index < 0) index = this.TabCount - 2;
            this.SelectedIndex = index;
            this.Activate();
        }

        public void SelectNextTab()
        {
            int index = this.SelectedIndex + 1;
            if (index >= this.TabCount - 1) index = 0;
            this.SelectedIndex = index;
            this.Activate();
        }

        public bool CloseActiveTab()
        {
            var control = this.GetControl();
            if (control != null && control.IsFocus())
            {
                if (this.SelectedTab == null)
                    return false;

                if (this.SelectedIndex == 0)
                    return true;

                var selectedTab = this.SelectedTab;
                this.SelectedIndex = Math.Max(0, this.SelectedIndex - 1);

                if (!control.IsLocked)
                    this.TabPages.Remove(selectedTab);

            }

            return false;
        }

        public bool CheckNavigationToTabSibling(BrowserTabPage source, ShellObject pendingLocation)
        {
            foreach (BrowserTabPage tab in this.TabPages)
            {
                if (tab == source)
                    continue;

                var currentLocation = tab.GetLocation();
                if (currentLocation != null)
                {
                    if (currentLocation == pendingLocation || currentLocation.ParsingName == pendingLocation.ParsingName)
                    {
                        this.SelectedTab = tab;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
