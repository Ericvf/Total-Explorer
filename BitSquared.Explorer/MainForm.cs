using BitSquared.Explorer.Extensions;

using System;
using System.Windows.Forms;
using BitSquared.Explorer.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;
using BitSquared.Explorer.Tools;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace BitSquared.Explorer
{
    public partial class MainForm : Form
    {
        const string APP_TITLE = @"Total Explorer (press F1 for help) - appbyfex";

        IList<string> commandLineArgs;
        bool isActive = false;

        BrowserTabControl otherTabControl;
        BrowserTabControl selectedTabControl;

        public MainForm()
        {
            // Init UI
            InitializeComponent();

            this.Init();
            this.DoubleBuffered = true;
        }

        public MainForm(IList<string> args)
        {
            // Init UI
            InitializeComponent();
            this.Text = APP_TITLE;

            this.commandLineArgs = args;
            this.Init();
        }

        public void ProcessCommandLine(IList<string> args)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                {
                    if (args.Count == 2)
                    {
                        var filepath = args[1];
                        if (File.Exists(filepath))
                        {
                            var fileInfo = new FileInfo(filepath);
                            if (fileInfo.Extension.Equals(".nzb"))
                            {
                                this.HandleMovableFile(fileInfo);
                                return;
                            }

                            this.DetermineActiveTabControl();
                            var shellObject = ShellObject.FromParsingName(filepath);
                            this.selectedTabControl.AddTab(shellObject.Name, shellObject, this.selectedTabControl.SelectedIndex + 1, true);
                        }
                    }

                    this.Activate();
                }
            }));
        }

        private void Init()
        {
            this.tabControl1.ShowAppByFex = false;
            this.tabControl2.ShowAppByFex = true;

            // Load Settings
            Settings.Load();

            // Init Window UI (from Settings)
            this.InitializeLayout();

            // Init KeyboardHook & MouseHook
            KeyboardHook.RegisterHook();
            MouseHook.RegisterHook();

            KeyboardHook.OnKeyHookEvent += new EventHandler<KeyboardHook.KeyHookEventArgs>(KeyboardHook_OnKeyHookEvent);
            MouseHook.OnMouseHookEvent += new EventHandler<MouseHook.MouseHookEventArgs>(MouseHook_OnMouseHookEvent);

            this.Load += (s, e) => this.InitTabControls();
            this.FormClosing += (s, e) =>
                {
                    KeyboardHook.OnKeyHookEvent -= new EventHandler<KeyboardHook.KeyHookEventArgs>(KeyboardHook_OnKeyHookEvent);
                    MouseHook.OnMouseHookEvent -= new EventHandler<MouseHook.MouseHookEventArgs>(MouseHook_OnMouseHookEvent);


                    KeyboardHook.UnregisterHook();
                    MouseHook.UnregisterHook();

                };
        }

        private void InitializeLayout()
        {
            var currentDesktop = SystemInformation.VirtualScreen;

            try
            {
                var size = new Size(
                    Math.Min(Math.Max(Settings.Instance.Size.Width, 800), currentDesktop.Width),
                    Math.Min(Math.Max(Settings.Instance.Size.Height, 600), currentDesktop.Height)
                    );

                var location = new Point(
                    Math.Min(Math.Max(Settings.Instance.Location.X, currentDesktop.Left), currentDesktop.Width - 100),
                    Math.Min(Math.Max(Settings.Instance.Location.Y, currentDesktop.Top), currentDesktop.Height - 100)
                    );

                this.Size = size;
                //this.CenterToScreen();
                this.Location = location;
            }
            catch
            {
                this.Size = new Size(800, 600);
            }
        }

        private void InitTabControls()
        {
            if (Settings.Instance.TabRightPages.Count == 0)
            {
                Settings.Instance.TabLeftIndex = 0;
                Settings.Instance.TabLeftPages.Add(new Settings.PageSetting()
                {
                    Name = string.Empty,
                    CurrentLocation = KnownFolders.Computer.ParsingName,
                    IsLocked = false,
                });

                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady && d.DriveType == DriveType.Fixed)
                    {
                        Settings.Instance.TabLeftPages.Add(new Settings.PageSetting()
                        {
                            Name = d.RootDirectory.Name,
                            CurrentLocation = d.RootDirectory.FullName,
                            IsLocked = false,
                        });
                    }
                }
            }

            if (Settings.Instance.TabRightPages.Count == 0)
            {
                string userLibraryFolder = String.Format(@"{0}\Microsoft\Windows\Libraries\", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                string[] libraries = Directory.GetFiles(userLibraryFolder, "*.library-ms", SearchOption.TopDirectoryOnly);
                foreach (string s in libraries)
                {
                    //The Name of a Library is just its file name without the extension
                    var name = Path.GetFileNameWithoutExtension(s);
                    Settings.Instance.TabRightPages.Add(new Settings.PageSetting()
                    {
                        Name = name,
                        CurrentLocation = s,
                        IsLocked = false,
                    });
                }

                Settings.Instance.TabRightIndex = 0;
            }

            this.InitTabControl(this.tabControl1, Settings.Instance.TabLeftPages);
            this.InitTabControl(this.tabControl2, Settings.Instance.TabRightPages);

            if (this.commandLineArgs.Count > 1)
                this.ProcessCommandLine(this.commandLineArgs);

            this.tabControl1.SelectedIndex = Settings.Instance.TabLeftIndex;
            this.tabControl2.SelectedIndex = Settings.Instance.TabRightIndex;

            this.tabControl2.Activate();
            this.tabControl1.Activate();
        }

        private void InitTabControl(BrowserTabControl tabControl, List<Settings.PageSetting> pageSettings)
        {
            if (pageSettings.Count > 0)
            {
                foreach (var page in pageSettings)
                {
                    try
                    {
                        var shellObject = ShellObject.FromParsingName(page.CurrentLocation);
                        var tabPage = tabControl.AddTab(page.Name, shellObject, isLocked: page.IsLocked);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("The previously opened tab could not be openend because the path does not exist."
                            + Environment.NewLine + Environment.NewLine + page.CurrentLocation
                            + Environment.NewLine + Environment.NewLine + ex.ToString()
                            );
                    }
                }
            }
            else
            {
                tabControl.AddTab(string.Empty, null);
            }

            tabControl.AddTab("...", null);

            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl.SelectedIndex == tabControl.TabCount - 1)
                    tabControl.AddTab(string.Empty, null, tabControl.TabCount - 1, true);

                tabControl.Activate();

                this.tabControl1.Invalidate();
                this.tabControl2.Invalidate();
            };

        }

        void KeyboardHook_OnKeyHookEvent(object sender, KeyboardHook.KeyHookEventArgs e)
        {
            if (!isActive)
                return;

            switch (e.Key)
            {
                case Keys.F3:
                    if (!e.IsDown)
                    {
                        this.DetermineActiveTabControl();
                        this.selectedTabControl.GetControl().FocusSearch();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.A:
                    if (!e.IsDown && KeyboardHook.IsDown(Keys.LMenu))
                    {
                        this.ToggleLock();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.F1:
                    if (!e.IsDown)
                    {
                        this.ShowHelp();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.F4:
                    if (!e.IsDown)
                    {
                        this.SelectAddressBar();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.F6:
                    if (!e.IsDown)
                    {
                        this.StartCopy();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.F7:
                    if (!e.IsDown)
                    {
                        this.StartMove();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.F8:
                    if (!e.IsDown)
                    {
                        this.StartDelete();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.Escape:
                    if (!e.IsDown)
                    {
                        this.CloseOpenTab();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.Tab:
                    if (!e.IsDown)
                    {
                        if (!KeyboardHook.IsDown(Keys.LControlKey))
                        {
                            this.SwitchTabControl();
                        }
                        else
                        {
                            this.SelectNextTab();
                        }
                    }
                    break;

                case Keys.Right:
                    if (e.IsDown && KeyboardHook.IsDown(Keys.LMenu) && KeyboardHook.IsDown(Keys.LShiftKey))
                    {

                        this.SwitchTab(this.tabControl1, this.tabControl2);
                        e.IsHandled = true;
                    }
                    else if (e.IsDown && KeyboardHook.IsDown(Keys.LMenu))
                    {
                        this.SelectNextTab();
                        e.IsHandled = true;
                    }
                    break;

                case Keys.Left:
                    if (e.IsDown && KeyboardHook.IsDown(Keys.LMenu) && KeyboardHook.IsDown(Keys.LShiftKey))
                    {
                        this.SwitchTab(this.tabControl2, this.tabControl1);
                        e.IsHandled = true;
                    }
                    else if (e.IsDown && KeyboardHook.IsDown(Keys.LMenu))
                    {
                        this.SelectPrevTab();
                        e.IsHandled = true;
                    }

                    break;

                case Keys.Up:
                    if (e.IsDown && KeyboardHook.IsDown(Keys.LMenu))
                    {
                        this.FolderUp();
                        e.IsHandled = true;
                    }

                    break;
            }
        }

        void MouseHook_OnMouseHookEvent(object sender, MouseHook.MouseHookEventArgs e)
        {
            if (!this.isActive)
                return;


            this.DetermineActiveTabControl();
            var control = this.selectedTabControl.GetControl();

            if (control != null)
            {
                switch (e.mouseMessages)
                {
                    case MouseHook.MouseMessages.WM_LBUTTONUP:
                    case MouseHook.MouseMessages.WM_RBUTTONUP:
                        var pt = new Point(e.hookStruct.pt.x, e.hookStruct.pt.y);
                        bool refresh = false;

                        if (this.tabControl1.GetControl().ScreenRectangle().Contains(pt))
                        {
                            if (!this.tabControl1.ContainsFocus)
                                this.tabControl1.GetControl().Activate();

                            refresh = true;
                        }
                        else if (this.tabControl2.GetControl().ScreenRectangle().Contains(pt))
                        {
                            if (!this.tabControl2.ContainsFocus)
                                this.tabControl2.GetControl().Activate();

                            refresh = true;
                        }

                        if (refresh)
                        {
                            this.tabControl1.Invalidate();
                            this.tabControl2.Invalidate();

                        }
                        break;

                    case MouseHook.MouseMessages.WM_BUTTON4UP:

                        this.DetermineActiveTabControl();
                        if (this.selectedTabControl != null)
                        {
                            var tab = this.selectedTabControl.SelectedTab as BrowserTabPage;

                            if (e.mouseData == 65536)
                            {
                                if (!control.Backwards() && !tab.IsLocked)
                                {
                                    this.selectedTabControl.CloseActiveTab();
                                }
                            }
                            else if (e.mouseData == 131072)
                            {
                                control.Forward();
                            }
                        }
                        break;
                }
            }
        }

        private void DetermineActiveTabControl()
        {
            if (tabControl2.ContainsFocus)
            {
                this.selectedTabControl = tabControl2;
                this.otherTabControl = tabControl1;
            }
            else
            {
                this.selectedTabControl = tabControl1;
                this.otherTabControl = tabControl2;
            }
        }

        private void SwitchTab(BrowserTabControl sourceTab, BrowserTabControl targetTab)
        {
            sourceTab.SwitchTab(targetTab);
        }

        private void SelectNextTab()
        {
            this.DetermineActiveTabControl();
            this.selectedTabControl.SelectNextTab();
        }

        private void SelectPrevTab()
        {
            this.DetermineActiveTabControl();
            this.selectedTabControl.SelectPrevTab();
        }

        private void SwitchTabControl()
        {
            this.DetermineActiveTabControl();
            if (this.otherTabControl != null)
            {
                this.otherTabControl.Activate();

                this.tabControl1.Invalidate();
                this.tabControl2.Invalidate();
            }
        }

        private void StartDelete()
        {
            var ex1 = this.selectedTabControl.GetControl();
            var ex2 = this.otherTabControl.GetControl();

            var items1 = ex1.GetSelectedItems();
            var items2 = ex2.GetPath();

            var sb = new StringBuilder();
            foreach (var item in items1)
                sb.AppendLine(item.ParsingName);

            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var applicationStoragePath = Path.Combine(applicationDataPath, Settings.applicationName);
            var listfile = applicationStoragePath + @"\filelist.txt";

            File.WriteAllText(listfile, sb.ToString());

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = @"C:\Program Files\TeraCopy\TeraCopy.exe";
                p.StartInfo.Arguments = string.Format("Delete *\"{0}\"", listfile);
                p.Start();
            }
            catch
            {

            }
        }

        private void FolderUp()
        {
            this.DetermineActiveTabControl();
            this.selectedTabControl.GetControl().FolderUp();
        }

        private void SelectAddressBar()
        {
            this.DetermineActiveTabControl();
            this.selectedTabControl.GetControl().FocusAddressBar();
        }

        private void ToggleLock()
        {
            this.DetermineActiveTabControl();
            this.selectedTabControl.GetControl().ToggleLock();
        }

        private void CloseOpenTab()
        {
            this.DetermineActiveTabControl();
            if (this.selectedTabControl != null)
            {
                if (this.selectedTabControl.CloseActiveTab())
                {
                    this.SwitchTabControl();
                    this.DetermineActiveTabControl();
                    this.selectedTabControl.SelectedIndex = this.selectedTabControl.TabCount - 2;
                }
            }
        }

        private void StartCopy()
        {
            var ex1 = this.selectedTabControl.GetControl();
            var ex2 = this.otherTabControl.GetControl();

            var items1 = ex1.GetSelectedItems();
            var items2 = ex2.GetPath();

            var sb = new StringBuilder();
            foreach (var item in items1)
                sb.AppendLine(item.ParsingName);

            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var applicationStoragePath = Path.Combine(applicationDataPath, Settings.applicationName);
            var listfile = applicationStoragePath + @"\filelist.txt";

            File.WriteAllText(listfile, sb.ToString());

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = @"C:\Program Files\TeraCopy\TeraCopy.exe";
                p.StartInfo.Arguments = string.Format("Copy *\"{0}\" \"{1}\"", listfile, items2);
                p.Start();
            }
            catch
            {

            }
        }

        private void StartMove()
        {
            var ex1 = this.selectedTabControl.GetControl();
            var ex2 = this.otherTabControl.GetControl();

            var items1 = ex1.GetSelectedItems();
            var items2 = ex2.GetPath();

            var sb = new StringBuilder();
            foreach (var item in items1)
                sb.AppendLine(item.ParsingName);

            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var applicationStoragePath = Path.Combine(applicationDataPath, Settings.applicationName);
            var listfile = applicationStoragePath + @"\filelist.txt";

            File.WriteAllText(listfile, sb.ToString());

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = @"C:\Program Files\TeraCopy\TeraCopy.exe";
                p.StartInfo.Arguments = string.Format("Move *\"{0}\" \"{1}\"", listfile, items2);
                p.Start();
            }
            catch
            {
            }
        }

        private void HandleMovableFile(FileInfo fileInfo)
        {
            string location = @"D:\Downloads\nzb\";

            try
            {
                var fileCopy = fileInfo.CopyTo(location + fileInfo.Name, true);
                if (fileCopy != null)
                    MessageBox.Show(string.Format("File copied to location: {0}", location));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (this.selectedTabControl != null)
                this.selectedTabControl.Activate();

            this.isActive = true;
            this.Text = APP_TITLE;
            //KeyboardHook.RegisterHook();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveTabControlLayout();
            Settings.Save();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            this.DetermineActiveTabControl();
            this.isActive = false;
            this.Text = APP_TITLE + @" (inactive)";
        }

        private void SaveTabControlLayout()
        {
            Func<List<Settings.PageSetting>, TabControl, bool> addPages = (List<Settings.PageSetting> p, TabControl c) =>
            {
                foreach (BrowserTabPage page in c.TabPages)
                {
                    if (!page.HasChildren)
                        continue;

                    var bc = page.GetControl();
                    if (bc != null)
                    {
                        try
                        {
                            var title = bc.GetTabTitle();

                            if (title.Equals("..."))
                                continue;

                            var location = ShellObject.FromParsingName(bc.GetCurrentLocation());

                            p.Add(new Settings.PageSetting()
                            {
                                Name = bc.GetTabTitle(),
                                CurrentLocation = bc.GetCurrentLocation(),
                                IsLocked = bc.IsLocked,
                            });
                        }
                        catch
                        {
                            // Unable to save page, continue since we're closing
                        }
                    }
                }

                return true;
            };

            Settings.Instance.Location = this.Location;
            Settings.Instance.Size = this.Size;
            Settings.Instance.TabLeftPages.Clear();
            Settings.Instance.TabLeftIndex = this.tabControl1.SelectedIndex;
            addPages(Settings.Instance.TabLeftPages, this.tabControl1);

            Settings.Instance.TabRightPages.Clear();
            Settings.Instance.TabRightIndex = this.tabControl2.SelectedIndex;
            addPages(Settings.Instance.TabRightPages, this.tabControl2);
        }

        private void ShowHelp()
        {
            var msg = @"
RIGHTMOUSE + DRAG
 - Reorder tabs

MOUSEBUTTON-BACK/FORWARD 
 - Go back and forward in the active tab

TAB
 - Toggle the active tab control, left or right

CTRL + TAB
 - Next tab in the active tab control

ALT + A
 - Toggle tab lock

ALT + ARROWUP
 - Go to the parent directory

ALT + (ARROWLEFT or ARROWRIGHT)
 - Navigate through tabs in the active tab control

ALT + SHIFT + (ARROWLEFT or ARROWRIGHT)
 - Move the active tab to the other tab control

ESCAPE
 - Close active tab (if not locked)

F2
 - Rename selected file or directory

F3
 - Focus search textbox

F4
 - Focus address textbox

F5
 - Refresh

F6 
 - COPY selected items from active tab control to other tab directory
 - (REQUIRES TeraCopy to be installed into program files)

F7
 - MOVE selected items from active tab control to other tab directory
 - (REQUIRES TeraCopy to be installed into program files)

F8
 - DELETE selected items from active tab control to other tab directory
 - (REQUIRES TeraCopy to be installed into program files)
";

            MessageBox.Show(msg, "Appbyfex presents: Total Explorer",  MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
