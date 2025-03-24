using System;
using System.Reflection;
using WeModPatcher.Models;

namespace WeModPatcher
{
    public static class Constants
    {
        public const string RepoName = "Wemod-Patcher";
        public const string Owner = "k1tbyte";
        public static readonly string RepositoryUrl = $"https://github.com/{Owner}/{RepoName}";
        public static readonly Version Version;

        // cmp     dword ptr [rdx], 0
        // jnz     loc_XXXXXXXX 
        // mov     rsi, rdx
        public static Signature ExePatchSignature = new Signature(
            "83 3A 00 0F ?? ?? 01 00 00 48 89 D6 48 B8",
            4,
            new byte[]{ 0x84, 0x17 },
            new byte[]{ 0x85, 0x22 }
        );
        
        /*// ...
        // test eax, eax (0x85 for	r/m16/32/64)
        // jnz      short loc_1403A4DD2 (Integrity check failed)
        // call    near ptr funk_1445527E0
        // ...
        private const string PatchSignature = "E8 ?? ?? ?? ?? ?? C0 75 ?? F6 C3 01 74 ?? 48 89 F9 E8 ?? ?? ?? ??";
        private static readonly byte[] PatchBytes = { 0x31 };
        private const int PatchOffset = 0x5;*/

        static  Constants()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}