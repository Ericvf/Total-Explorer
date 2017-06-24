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
using BitSquared.Explorer.Controls;

namespace BitSquared.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            App.MainWindow = this;

            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            OpenNewTab(this.tabControl1);
            OpenNewTab(this.tabControl2);
        }

        private void OpenNewTab(TabControl tabControl, string name = "Window")
        {
            var tabItem = new TabItem();
            tabItem.MouseDoubleClick += new MouseButtonEventHandler(tabItem_MouseDoubleClick);
            var browser = new Browser(tabControl);
            tabItem.Content = browser;
            tabItem.Header = name;

            tabControl.Items.Add(tabItem);
            tabControl.SelectedValue = tabItem;
        }

        void tabItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var tabItem = sender as TabItem;
            var tabControl = tabItem.Parent as TabControl;
            if (tabControl != null)
            {
                var browser = tabItem.Content as Browser;
                if (browser != null)
                {
                    browser.Close();
                    
                    tabItem.Content = null;

                    GC.Collect();
                }
                tabControl.Items.Remove(tabItem);
            }
        }


        public void SwitchTab(Browser browser)
        {
            var parentControl = browser.ParentControl;
            var tabItem = browser.Parent as TabItem;

            var targetControl = parentControl == this.tabControl1 ? this.tabControl2 : this.tabControl1;
            var sourceControl = parentControl == this.tabControl1 ? this.tabControl1 : this.tabControl2;

            sourceControl.Items.Remove(tabItem);
            targetControl.Items.Add(tabItem);

            browser.ParentControl = targetControl;

            targetControl.SelectedItem = tabItem;
        }

        private void NewTabClick(object sender, RoutedEventArgs e)
        {
            var btn = (sender as Button);
            if (btn != null)
            {
                var tabControl = this.FindName(btn.Tag.ToString()) as TabControl;
                if (tabControl != null)
                {
                    this.OpenNewTab(tabControl);
                }
            }
        }
    }
}
