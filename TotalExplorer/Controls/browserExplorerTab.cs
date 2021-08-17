using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.Controls.WindowsForms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Fex.TotalExplorer.Controls
{
    public partial class browserExplorerTab : UserControl
    {
        private ExplorerBrowser explorerBrowser;
        private ShellObject currentSafeLocation;
        private ShellObject currentLocation;
        public ShellObject CurrentLocation
        {
            get
            {
                return this.currentLocation;
            }
        }

        private bool isFalseBackwards;
        private bool isLoaded;
        private bool isClean;
        private bool isSearch;

        public string Title { get; set; }
        public bool IsLocked { get; set; }

        public browserExplorerTab(ShellObject shellObject = null)
        {
            InitializeComponent();

            this.InitSearch();

            this.isClean = true;

            if (shellObject == null)
                shellObject = (ShellObject)KnownFolders.Computer;

            this.explorerBrowser = new ExplorerBrowser();
            this.explorerBrowser.Dock = DockStyle.Fill;
            this.explorerBrowser.NavigationPending += new EventHandler<NavigationPendingEventArgs>(explorerBrowser_NavigationPending);
            this.explorerBrowser.NavigationComplete += explorerBrowser_NavigationComplete;
            this.explorerBrowser.NavigationFailed += new EventHandler<NavigationFailedEventArgs>(explorerBrowser_NavigationFailed);
            this.explorerBrowser.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Show;
            this.currentSafeLocation = shellObject;
            this.currentLocation = shellObject;
            this.contentPanel.Controls.Add(this.explorerBrowser);
            this.Dock = DockStyle.Fill;

            this.tbAddress.MouseDown += new MouseEventHandler(explorerBrowser_MouseDown);

        }

        void explorerBrowser_MouseDown(object sender, MouseEventArgs e)
        {
            //if (e.Button == System.Windows.Forms.MouseButtons.XButton1)
            //{

            //}
            //else if (e.Button == System.Windows.Forms.MouseButtons.XButton2)
            //{

            //}
        }

        private bool CheckNavigationToTabSibling(ShellObject shellObject)
        {
            var tabControl = this.Parent.Parent as BrowserTabControl;
            var tabPage = this.Parent as BrowserTabPage;
            if (tabControl != null)
                return tabControl.CheckNavigationToTabSibling(tabPage, shellObject);

            return false;
        }

        private void InitSearch()
        {
            this.tbSearch.Text = "Search...";
            this.tbSearch.GotFocus += (s, e) =>
            {
                if (this.tbSearch.Text == "Search...")
                    this.tbSearch.Text = string.Empty;
            };

            this.tbSearch.LostFocus += (s, e) =>
                {
                    if (this.tbSearch.Text.Trim() == string.Empty)
                        this.tbSearch.Text = "Search...";
                };
        }

        public string GetTabTitle()
        {
            return this.Title;
        }

        public string GetCurrentLocation()
        {
            return this.currentLocation.ParsingName;
        }

        public string GetPath()
        {
            var shellFolder = this.currentLocation as ShellNonFileSystemFolder;
            if (shellFolder != null)
            {
                var firstItem = shellFolder.FirstOrDefault();
                if (firstItem != null)
                {
                    var path = firstItem.ParsingName;

                    try
                    {
                        var directoryInfo = new DirectoryInfo(path);
                        return directoryInfo.Parent.FullName;
                    }
                    catch
                    {
                        var root = path.LastIndexOf("\\");
                        if (root > 0)
                            return path.Substring(0, root);
                    }
                }
            }

            return this.currentLocation.ParsingName.ToString();
        }

        public ShellObject[] GetSelectedItems()
        {
            return this.explorerBrowser.SelectedItems.ToArray();
        }

        public void Activate()
        {
            try
            {
                if (this.isClean)
                {
                    this.explorerBrowser.Navigate(this.currentSafeLocation);
                    this.isClean = false;
                }

                if (this.explorerBrowser != null)
                    this.explorerBrowser.UIActivate();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                this.explorerBrowser.Navigate((ShellObject)KnownFolders.Computer);
            }
        }

        void explorerBrowser_NavigationPending(object sender, NavigationPendingEventArgs e)
        {
            if (!this.isLoaded)
            {
                e.Cancel = false;
                return;
            }

            if (this.CheckNavigationToTabSibling(e.PendingLocation))
            {
                e.Cancel = true;
                return;
            }

            if (this.IsLocked || this.isSearch)
            {
                var control = this.Parent.Parent as BrowserTabControl;
                control.AddTab(e.PendingLocation.Name, e.PendingLocation, control.SelectedIndex + 1, true);

                e.Cancel = true;
                return;
            }
        }

        void explorerBrowser_NavigationComplete(object sender, Microsoft.WindowsAPICodePack.Controls.NavigationCompleteEventArgs e)
        {
            this.isLoaded = true;

            if (this.isFalseBackwards)
            {
                this.isFalseBackwards = false;
                this.explorerBrowser.NavigateLogLocation(NavigationLogDirection.Forward);
                return;
            }

            var currentLocation = this.explorerBrowser.NavigationLog.CurrentLocation;
            //if (currentLocation.Name.StartsWith("Search Results"))
            //    this.isSearch = true;

            //if (!isSearch)
            //{
                if (currentLocation.IsFileSystemObject || currentLocation.IsLink)
                    this.currentSafeLocation = currentLocation;

                this.currentLocation = currentLocation;
                this.SetText(currentLocation.Name);
                this.SetImage(currentLocation);
            //}
            //else
            //{
            //    this.explorerBrowser.RefreshExplorer();
            //    this.isSearch = false;
            //}

            var address = currentLocation.IsFileSystemObject ? currentLocation.ParsingName : currentLocation.Name;
            this.tbAddress.Text = address;
        }

        void explorerBrowser_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Debug.WriteLine("Failed: " + e.FailedLocation);
           // MessageBox.Show(e.FailedLocation.Name, "Navigation exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.explorerBrowser.NavigateLogLocation(NavigationLogDirection.Backward);
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            this.explorerBrowser.NavigateLogLocation(NavigationLogDirection.Forward);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            this.explorerBrowser.RefreshExplorer();
        }

        private void btnToggleNavigation_Click(object sender, EventArgs e)
        {
            var newState = this.explorerBrowser.NavigationOptions.PaneVisibility.Navigation == PaneVisibilityState.Show ?
                PaneVisibilityState.Hide : PaneVisibilityState.Show;

            this.explorerBrowser.NavigationOptions.PaneVisibility.Navigation = newState;
            this.explorerBrowser.NavigationOptions.PaneVisibility.Details = newState;
            this.explorerBrowser.NavigateLogLocation(NavigationLogDirection.Backward);
            this.isFalseBackwards = true;

            this.explorerBrowser.UIActivate();
        }

        private void tbAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var shellObject = ShellObject.FromParsingName(this.tbAddress.Text);
                if (shellObject != null)
                {
                    this.explorerBrowser.Navigate(shellObject);
                }
            }
        }

        private void tbSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.Search(this.tbSearch.Text, (ShellContainer)this.currentLocation);
        }

        private void btnLock_Click(object sender, EventArgs e)
        {
            this.IsLocked = !this.IsLocked;
            this.SetText(this.Title);
        }

        private SearchCondition GetSearchCondition(string searchText)
        {
            string[] words = searchText.Split(' ');
            SearchCondition combinedPropertyCondition = null;

            foreach (string word in words)
            {
                SearchCondition propertyCondition1 = SearchConditionFactory.CreateLeafCondition(
                    SystemProperties.System.FileName,
                    word,
                    SearchConditionOperation.ValueContains);

                if (combinedPropertyCondition != null)
                {
                    combinedPropertyCondition = SearchConditionFactory.CreateAndOrCondition(
                        SearchConditionType.And,
                        false,
                        combinedPropertyCondition,
                        propertyCondition1);
                }
                else
                {
                    combinedPropertyCondition = propertyCondition1;
                }
            }
            return combinedPropertyCondition;
        }

        private void Search(string searchText, ShellContainer obj)
        {
            if (obj == null && this.currentSafeLocation != null)
                obj = (ShellContainer)this.currentSafeLocation;

            var searchCondition = this.GetSearchCondition(this.tbSearch.Text);
            ShellObject shellObject = new ShellSearchFolder(searchCondition, obj);
            this.explorerBrowser.Navigate(shellObject);
            this.isSearch = true;
        }

        internal void FocusSearch()
        {
            this.tbSearch.Focus();
        }

        internal void FocusAddressBar()
        {
            this.tbAddress.Focus();
        }

        internal void FolderUp()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(this.GetPath());
                this.NavigateToPath(directoryInfo.Parent.FullName);
            }
            catch
            {
                var root = this.GetPath().LastIndexOf("\\");
                if (root > 0)
                    this.NavigateToPath(this.GetPath().Substring(0, root));
            }
        }

        internal void ToggleLock()
        {
            this.IsLocked = !this.IsLocked;
            this.SetText(this.Title);
        }

        internal bool IsFocus()
        {
            return this.explorerBrowser.ContainsFocus;
        }

        internal void NavigateToPath(string path)
        {
            var shellObject = ShellObject.FromParsingName(path);
            if (shellObject != null)
                this.explorerBrowser.Navigate(shellObject);
        }

        public void SetText(string tabName)
        {
            this.Title = tabName;
            var page = this.Parent as BrowserTabPage;
            page.SetText(this.Title);
        }

        public void SetImage(ShellObject currentLocation)
        {
            var page = this.Parent as BrowserTabPage;
            page.SetImage(currentLocation);
        }

        public bool Backwards()
        {
            var navigated = this.explorerBrowser.NavigateLogLocation(NavigationLogDirection.Backward);
            return navigated;
        }

        public void Forward()
        {
            this.explorerBrowser.NavigateLogLocation(NavigationLogDirection.Forward);
        }
    }
}
