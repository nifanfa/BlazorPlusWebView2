using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Web.WebView2.Core;
using System.Drawing;
using System.Runtime.InteropServices;

[assembly: HostingStartup(typeof(ServerDiscoveryStartup))]

public class ServerDiscoveryStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, ServerAddressCaptureFilter>();
        });
    }
}

public class ServerAddressCaptureFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var server = app.ServerFeatures.Get<IServerAddressesFeature>();
            var urls = server?.Addresses.ToList();

            Thread staThread = new Thread(new ParameterizedThreadStart(StartMessageLoop));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start(urls.First());

            next(app);
        };
    }

    private static Rectangle _bounds = new Rectangle(0, 0, 1600, 900);
    private static CoreWebView2Controller _controller;
    private static IntPtr _hWnd;
    private const uint WM_USER_INVOKE = 0x0400 + 100;

    static void StartMessageLoop(object obj)
    {
        string url = (string)obj;

        SetProcessDPIAware();

        IntPtr hInstance = GetModuleHandle(null);
        string className = "WebView2ConsoleHost";
        string title = AppDomain.CurrentDomain.FriendlyName;

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
        _ = InitializeWebViewAsync(url);

        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        Environment.Exit(0);
    }

    static async Task InitializeWebViewAsync(string url)
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync();
            _controller = await env.CreateCoreWebView2ControllerAsync(_hWnd);
            _controller.Bounds = _bounds;
            _controller.CoreWebView2.Navigate(url);
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