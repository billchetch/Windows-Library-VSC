using System;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Forms;

namespace Chetch.Windows;

abstract public class SysTrayApplicationContext : ApplicationContext
{
    protected Dictionary<String, Object> Config { get; set; } = new Dictionary<String, Object>();
    protected String? NotifyIconPath { get; set; }
    protected String? NotifyIconText { get; set; }

    private Container? _components;
    protected NotifyIcon? NotifyIcon;
    private Form? _mainForm;

    public SysTrayApplicationContext(bool asSysTray = true)
    {
        String[] settingsFiles = ["appsettings.local.json", "appsettings.json"];
        foreach (var f in settingsFiles) {
            if (File.Exists(f))
            {
                String json = File.ReadAllText(f);
                var cfg = JsonSerializer.Deserialize<Dictionary<String, Object>>(json);
                if (cfg != null)
                {
                    Config = cfg;
                    break;
                }
            }
        }
        InitializeContext(asSysTray);
    }

    virtual protected void InitializeContext(bool asSysTray)
    {
        if (asSysTray)
        {
            if(String.IsNullOrEmpty(NotifyIconPath))
            {
                throw new Exception("Cannot Initialise Context as no icon path specified");
            }

            if(String.IsNullOrEmpty(NotifyIconText))
            {
                throw new Exception("Cannot Initialise Context as no icon text specified");
            }

            _components = new Container();
            NotifyIcon = new NotifyIcon(_components);
            NotifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(NotifyIconPath);
            NotifyIcon.Text = NotifyIconText;

            NotifyIcon.Visible = true;
            NotifyIcon.DoubleClick += this.notifyIcon_DoubleClick;
            NotifyIcon.ContextMenuStrip = new ContextMenuStrip();

            AddNotifyIconContextMenuItem("Open...", "Open");
            AddNotifyIconContextMenuItem("Exit");
        }
    }

    protected void AddNotifyIconContextMenuItem(String text, String? tag = null)
    {
        var tsi = NotifyIcon?.ContextMenuStrip?.Items.Add(text, null, this.contextMenuItem_Click);
        if (tsi != null)
        {
            tsi.Tag = tag == null ? text.ToUpper() : tag.ToUpper();
        }
    }

    abstract protected Form CreateMainForm();

    virtual protected void contextMenuItem_Click(Object? sender, EventArgs e)
    {
        if (sender == null) return;
        ToolStripItem tsi = (ToolStripItem)sender;
        if (tsi == null || tsi.Tag == null) return;

        switch (tsi.Tag.ToString()?.ToUpper())
        {
            case "EXIT":
                Application.Exit();
                break;

            case "OPEN":
                OpenMainForm();
                break;
        }
    }

    private void OpenMainForm()
    {
        if (_mainForm == null)
        {
            _mainForm = CreateMainForm();
            _mainForm.FormClosed += mainForm_FormClosed;
        }
        _mainForm.Show();
    }

    private void notifyIcon_DoubleClick(object? sender, EventArgs e)
    {
        OpenMainForm();
    }

    private void mainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _mainForm = null;
    }

    protected override void ExitThreadCore()
    {
        if (_mainForm != null) { _mainForm.Close(); }
        if (NotifyIcon != null) { NotifyIcon.Visible = false; } // should remove lingering tray icon!
        base.ExitThreadCore();
    }
}

