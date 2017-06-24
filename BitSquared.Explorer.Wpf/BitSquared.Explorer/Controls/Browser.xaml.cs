using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;


namespace BitSquared.Explorer.Controls
{
    /// <summary>
    /// Interaction logic for ExplorerBrowser.xaml
    /// </summary>
    public partial class Browser : UserControl
    {
        public TabControl ParentControl;

        public Browser(TabControl parentControl = null)
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(ExplorerBrowser_Loaded);

            this.explorerBrowser.ExplorerBrowserControl.NavigationComplete += new ExplorerBrowserNavigationCompleteEventHandler(ExplorerBrowserControl_NavigationComplete);
            this.explorerBrowser.NavigationPane = PaneVisibilityState.Show;
            this.explorerBrowser.CommandsPane = PaneVisibilityState.Show;
            this.explorerBrowser.DetailsPane = PaneVisibilityState.Show;

            this.ParentControl = parentControl;
        }

        private SearchCondition GetSearchCondition(string searchText)
        {
            // Einzelne Suchbegriffe aus dem Textbilden 
            string[] words = searchText.Split(' ');
            // Das Suchergebnis ist erstmal leer 
            SearchCondition combinedPropertyCondition = null;

            // Für jedes Word im Suchtext eine Suchbedingung erzeugen 
            foreach (string word in words)
            {
                // Erste Suchbedingung --> Suche nach Dateinamen 
                SearchCondition propertyCondition1 = SearchConditionFactory.CreateLeafCondition(
                    SystemProperties.System.FileName,
                    word,
                    SearchConditionOperation.ValueContains);

                // verknüpfe die neue und die alten mit AND 
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



        void ExplorerBrowserControl_NavigationComplete(object sender, NavigationCompleteEventArgs e)
        {
            if (this.isForward)
            {
                this.isForward = false;
                this.ForwardButtonClick(null, null);
                return;
            }

            var location = this.explorerBrowser.ExplorerBrowserControl.NavigationLog.CurrentLocation;
            if (!location.IsFileSystemObject)
            {
                this.tbAddress.Text = location.Name;
            }
            else
            {
                this.tbAddress.Text = location.ParsingName;
            }
        }

        void ExplorerBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            if (explorerBrowser.ExplorerBrowserControl.NavigationLog.CurrentLocation == null)
                this.explorerBrowser.NavigationTarget = (ShellContainer)KnownFolders.Computer;
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            this.explorerBrowser.ExplorerBrowserControl.NavigateLogLocation(NavigationLogDirection.Backward);
        }

        private void ForwardButtonClick(object sender, RoutedEventArgs e)
        {
            this.explorerBrowser.ExplorerBrowserControl.NavigateLogLocation(NavigationLogDirection.Forward);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var c = this.GetSearchCondition(this.tbSearch.Text);

            ShellObject ss = new ShellSearchFolder(c, (ShellContainer)this.explorerBrowser.ExplorerBrowserControl.NavigationLog.CurrentLocation);
            this.explorerBrowser.NavigationTarget = ss;

            //for (int i = 1; i <= 6; i++)
            //{

            //    var f = i < 5 ? (ShellObject)sf : ss;
            //    var ex = this.FindName("explorerBrowser" + i.ToString()) as ExplorerBrowser;
            //    if (ex != null)
            //        ex.NavigationTarget = f;

            //    ex.Visibility = System.Windows.Visibility.Collapsed;
            //    ex.Loaded += new RoutedEventHandler(ex_Loaded);
            //    ex.Tag = i;
            //}
        }

        private void tbSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.SearchButton_Click(this, null);
        }

        private void tbAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.explorerBrowser.NavigationTarget = (ShellContainer)KnownFolderHelper.FromPath(this.tbAddress.Text);
            }
        }

        private void SwitchButtonClick(object sender, RoutedEventArgs e)
        {
            App.MainWindow.SwitchTab(this);
        }

        private void OrganizeClick(object sender, RoutedEventArgs e)
        {
            var newValue = this.explorerBrowser.CommandsPane == PaneVisibilityState.Show ? PaneVisibilityState.Hide : PaneVisibilityState.Show;
            this.explorerBrowser.CommandsPane = newValue;
            this.RefreshControl();
        }

        private void NavigationClick(object sender, RoutedEventArgs e)
        {
            var newValue = this.explorerBrowser.NavigationPane == PaneVisibilityState.Show ? PaneVisibilityState.Hide : PaneVisibilityState.Show;
            this.explorerBrowser.NavigationPane = newValue;
            this.RefreshControl();

        }

        private void DetailsClick(object sender, RoutedEventArgs e)
        {
            var newValue = this.explorerBrowser.DetailsPane == PaneVisibilityState.Show ? PaneVisibilityState.Hide : PaneVisibilityState.Show;
            this.explorerBrowser.DetailsPane = newValue;
            this.RefreshControl();

        }


        private bool isForward = false;
        private void RefreshControl()
        {
            this.isForward = true;
            this.BackButtonClick(null, null);
            //this.explorerBrowser.NavigationTarget = (ShellContainer)KnownFolders.Computer;
        }

        public void Close()
        {

            this.explorerBrowser.ExplorerBrowserControl.Dispose();
            this.explorerBrowser = null;

        }

    }
}
