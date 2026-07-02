# Fuck WinForm WPF MAUI WinUI3 MFC GTK QT, ASP.NET Core Blazor + WebView2 is the best.  
DO NOT use WinForm or WPF to create WebView2 because once you use netx.x-windows, blazor environment is fucked.  
If you never tried ASP.NET Core Blazor, you should give it a try, C# can replace javascript in a lot of cases! HTML always wins.   
### Tired of searching for UI libraries
No UI language can compare to HTML, so why not use it? WinForms is terrible.
### Electron Alternatives
If you don't particularly like JavaScript, ASP.NET Core Blazor might be a good alternative. You can almost completely replace all your JavaScript code with C#.  
### Blazor syntax:  

<img width="320" height="195" alt="QQ_1782924174410" src="https://github.com/user-attachments/assets/9a677c0b-bd9d-44d0-bfb1-1c1d547f6181" />  

### My example application: 
<img width="1584" height="892" alt="QQ_1782922465552" src="https://github.com/user-attachments/assets/796445c6-8d6f-45d2-a0e6-525317eefe63" />   

```cs
using Microsoft.Web.WebView2.Core;
using System.Drawing;
using System.Runtime.InteropServices;

public partial class Program
{
    const string Url = "http://localhost:5000";

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder([]);

        builder.WebHost.UseUrls(Url);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        /* Omit */
        
        Thread staThread = new Thread(StartMessageLoop);
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();

        Task.WhenAny(Task.Run(app.Run), Task.Run(staThread.Join)).Wait();
    }

    private static Rectangle _bounds = new Rectangle(0, 0, 1600, 900);
    private static CoreWebView2Controller _controller;
    private static IntPtr _hWnd;
    private const uint WM_USER_INVOKE = 0x0400 + 100;

    static void StartMessageLoop()
    {
        SetProcessDPIAware();

        IntPtr hInstance = GetModuleHandle(null);
        string className = "WebView2ConsoleHost";
        string title = "WebView2 Complete Console Host";

        WNDCLASS wc = new WNDCLASS
        {
            style = 0,
            lpfnWndProc = MyWndProc,
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = hInstance,
            hIcon = IntPtr.Zero,
            hCursor = LoadCursor(IntPtr.Zero, 32512),
            hbrBackground = (IntPtr)6,
            lpszMenuName = null,
            lpszClassName = className
        };

        RegisterClass(ref wc);

        _hWnd = CreateWindowExW(0, className, title, 0x00CF0000, 100, 100, _bounds.Width, _bounds.Height, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        ShowWindow(_hWnd, 1);
        UpdateWindow(_hWnd);

        SynchronizationContext.SetSynchronizationContext(new Win32SynchronizationContext(_hWnd));
        _ = InitializeWebViewAsync();

        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    static async Task InitializeWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync();
            _controller = await env.CreateCoreWebView2ControllerAsync(_hWnd);
            _controller.Bounds = _bounds;
            _controller.CoreWebView2.Navigate(Url);
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private static IntPtr MyWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_USER_INVOKE)
        {
            GCHandle gch = GCHandle.FromIntPtr(lParam);
            if (gch.Target is Action action) action();
            gch.Free();
        }
        else if (msg == 0x0005)
        {
            if (_controller != null)
                _controller.Bounds = new Rectangle(0, 0, (int)(lParam & 0xFFFF), (int)(lParam >> 16));
        }
        else if (msg == 0x0002) PostQuitMessage(0);
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    class Win32SynchronizationContext : SynchronizationContext
    {
        private readonly IntPtr _hwnd;
        public Win32SynchronizationContext(IntPtr hwnd) => _hwnd = hwnd;
        public override void Post(SendOrPostCallback d, object state)
        {
            Action action = () => d(state);
            PostMessage(_hwnd, WM_USER_INVOKE, IntPtr.Zero, GCHandle.ToIntPtr(GCHandle.Alloc(action)));
        }
    }

    delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASS
    {
        public uint style;
        public WndProc lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName, lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd, message, wParam, lParam;
        public uint time;
        public int x, y;
    }

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern ushort RegisterClass(ref WNDCLASS lp);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowExW(int ex, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);

    [DllImport("user32.dll")]
    static extern bool GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);

    [DllImport("user32.dll")]
    static extern bool DispatchMessage(ref MSG msg);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern void ShowWindow(IntPtr hWnd, int cmd);

    [DllImport("user32.dll")]
    static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern void PostQuitMessage(int exit);

    [DllImport("user32.dll")]
    static extern IntPtr LoadCursor(IntPtr inst, int name);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandle(string name);
}
```
