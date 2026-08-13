using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Operator.AI;

public static class OperatorSecrets
{
    private const string OpenAiTarget = "OperatorAI/OpenAIApiKey";

    public static string? GetOpenAiApiKey()
    {
        string? env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        return !string.IsNullOrWhiteSpace(env) ? env.Trim() : ReadGenericCredential(OpenAiTarget);
    }

    public static bool HasOpenAiApiKey() => !string.IsNullOrWhiteSpace(GetOpenAiApiKey());

    public static void SaveOpenAiApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key cannot be empty.");

        WriteGenericCredential(OpenAiTarget, apiKey.Trim());
    }

    public static void DeleteOpenAiApiKey() => DeleteGenericCredential(OpenAiTarget);

    public static void SaveDeviceSecret(string deviceId, string secret)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Device ID and secret are required.");

        WriteGenericCredential(GetDeviceTarget(deviceId), secret);
    }

    public static string? GetDeviceSecret(string deviceId) =>
        string.IsNullOrWhiteSpace(deviceId) ? null : ReadGenericCredential(GetDeviceTarget(deviceId));

    public static void DeleteDeviceSecret(string deviceId)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
            DeleteGenericCredential(GetDeviceTarget(deviceId));
    }

    private static string GetDeviceTarget(string deviceId) => "OperatorAI/RemoteDevice/" + deviceId.Trim();

    private const uint CRED_TYPE_GENERIC = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    private static void WriteGenericCredential(string target, string secret)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(secret);
        IntPtr blob = Marshal.AllocCoTaskMem(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);

            CREDENTIAL credential = new()
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = target,
                Comment = "Operator AI secure credential",
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blob,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = Environment.UserName,
                TargetAlias = ""
            };

            if (!CredWrite(ref credential, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeCoTaskMem(blob);
            Array.Clear(bytes, 0, bytes.Length);
        }
    }

    private static string? ReadGenericCredential(string target)
    {
        if (!CredRead(target, CRED_TYPE_GENERIC, 0, out IntPtr ptr))
            return null;

        try
        {
            CREDENTIAL credential = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                return "";

            byte[] bytes = new byte[(int)credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);

            try
            {
                return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }
        finally
        {
            CredFree(ptr);
        }
    }

    private static void DeleteGenericCredential(string target) =>
        CredDelete(target, CRED_TYPE_GENERIC, 0);
}
