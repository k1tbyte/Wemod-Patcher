using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeModPatcher.Utils
{
public class PatternScanner
{
    public static int FindPatternInBuffer(byte[] buffer, int bytesRead, byte[] signature, string mask)
    {
        int bufferLength = bytesRead + signature.Length - 1;

        for (int i = 0; i <= bytesRead - signature.Length; i++)
        {
            if (IsMatch(buffer, signature, mask, i))
                return i;
        }

        return -1;
    }

    private static bool IsMatch(byte[] buffer, byte[] signature, string mask, int offset)
    {
        for (int i = 0; i < signature.Length; i++)
        {
            if (mask[i] == 'x' && buffer[offset + i] != signature[i])
                return false;
        }
        return true;
    }

    public static (byte[] signature, string mask) ParseSignature(string signature)
    {
        var signatureBytes = new List<byte>();
        var mask = new StringBuilder();

        var tokens = signature.Split(' ');
        foreach (var token in tokens)
        {
            if (token == "??" || token == "?")
            {
                signatureBytes.Add(0);
                mask.Append('?');
            }
            else
            {
                signatureBytes.Add(Convert.ToByte(token, 16));
                mask.Append('x');
            }
        }

        return (signatureBytes.ToArray(), mask.ToString());
    }

    public static async Task<int> PatchBySignature(string filePath, string functionSignature, byte[] patchBytes, int patchOffset)
    {
        var (signature, mask) = ParseSignature(functionSignature);
        const int bufferSize = 8192;
        var buffer = new byte[bufferSize + signature.Length - 1];

        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            int filePosition = 0;
            while (true)
            {
                int bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize);
                if (bytesRead == 0) break;

                int matchIndex = FindPatternInBuffer(buffer, bytesRead, signature, mask);
                if (matchIndex != -1)
                {
                    int functionStartPosition = filePosition + matchIndex;

                    var checkBuffer = new byte[patchBytes.Length];
                    fileStream.Seek(functionStartPosition + patchOffset, SeekOrigin.Begin);
                    await fileStream.ReadAsync(checkBuffer, 0, patchBytes.Length);

                    if (checkBuffer.SequenceEqual(patchBytes))
                    {
                        return 0; // Memory already patched
                    }

                    // Go to patch position
                    fileStream.Seek(functionStartPosition + patchOffset, SeekOrigin.Begin);
                    await fileStream.WriteAsync(patchBytes, 0, patchBytes.Length);

                    return functionStartPosition; // Return the address of the function start by signature
                }

                filePosition += bytesRead;
                Array.Copy(buffer, bufferSize, buffer, 0, signature.Length - 1);
            }
        }

        return -1;
    }
}
}