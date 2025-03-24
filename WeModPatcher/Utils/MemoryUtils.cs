using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WeModPatcher.Models;
using WeModPatcher.Utils.Win32;

namespace WeModPatcher.Utils
{
public class MemoryUtils
{
    public static int ScanMemoryBlock(byte[] buffer, int bufferLength, byte[] pattern, byte[] mask)
    {
        var patternLength = pattern.Length;
        if (bufferLength < patternLength)
        {
            return -1; 
        }
    
        // Make a length of length outside the first cycle for optimization
        var searchEnd = bufferLength - patternLength;
    
        // first pass - use the first non-empty byte of the mask for a quick check
        var firstValidIndex = -1;
        for (var i = 0; i < patternLength; i++)
        {
            if (mask[i] == 1)
            {
                firstValidIndex = i;
                break;
            }
        }

        if (firstValidIndex == -1)
        {
            return 0;
        }
        
        var firstByte = pattern[firstValidIndex];
    
        for (var i = 0; i <= searchEnd; i++)
        {
            // quick check by the first byte before full comparison
            if (buffer[i + firstValidIndex] != firstByte)
                continue;
        
            var found = true;
        
            // check only those positions where mask = 1
            for (var j = 0; j < patternLength; j++)
            {
                if (mask[j] == 0 || buffer[i + j] == pattern[j])
                {
                    continue;
                }
                
                found = false;
                break;
            }

            if (found)
            {
                return i;
            }
        }
    
        return -1;
    }

    public static void ParseSignature(string signatureStr, out byte[] pattern, out byte[] mask)
    {
        var parts = signatureStr.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var length = parts.Length;
    
        pattern = new byte[length];
        mask = new byte[length];
    
        for (var i = 0; i < length; i++)
        {
            if (parts[i] == "??" || parts[i] == "?")
            {
                pattern[i] = 0; 
                // wildcard byte
                mask[i] = 0;   
                continue;
            }

            pattern[i] = Convert.ToByte(parts[i], 16);
            mask[i] = 1;  
        }
    }
    
    public static bool SafeWriteVirtualMemory(IntPtr hProcess, IntPtr address, byte[] bytes)
    {
        if (!Imports.VirtualProtectEx(hProcess, address, (IntPtr)1, 0x40, out uint oldProtect))
        {
            return false;
        }
            
        bool result = Imports.WriteProcessMemory(hProcess, address, bytes, bytes.Length, out _);
        
        // Restore the previous access rights
        Imports.VirtualProtectEx(hProcess, address, (IntPtr)1, oldProtect, out _);
        return result;
    }
    
    public static IntPtr ScanVirtualMemory(IntPtr hProcess, IntPtr startAddress, int searchSize, byte[] signature, byte[] mask)
    {
        const int BUFFER_SIZE = 4096;
        byte[] buffer = new byte[BUFFER_SIZE];

        // We can't copy all the crap of the process into a byte array at once. Don't try this
        for (long currentAddress = startAddress.ToInt64(); 
             currentAddress < startAddress.ToInt64() + searchSize; 
             currentAddress += BUFFER_SIZE - signature.Length)
        {
            if (!Imports.ReadProcessMemory(hProcess, new IntPtr(currentAddress), buffer, BUFFER_SIZE, out int bytesRead) || bytesRead == 0)
            {
                // Read error or end of memory, throw mb?
                continue;
            }
                
            var i = ScanMemoryBlock(buffer, bytesRead, signature, mask);
            if (i != -1)
            {
                return new IntPtr(currentAddress + i);
            }
        }

        return IntPtr.Zero;
    }

    public static int PatchFile(string filePath, Signature signature, byte[] patchBytes)
    {
        const int bufferSize = 8192;
        var buffer = new byte[bufferSize + signature.Length - 1];
        
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            int filePosition = 0;
            while (true)
            {
                int bytesRead = fileStream.Read(buffer, 0, bufferSize);
                if (bytesRead == 0) break;

                int matchIndex = ScanMemoryBlock(buffer, bytesRead, signature, signature.Mask);
                if (matchIndex != -1)
                {
                    int functionStartPosition = filePosition + matchIndex;

                    var checkBuffer = new byte[patchBytes.Length];
                    fileStream.Seek(functionStartPosition + signature.Offset, SeekOrigin.Begin);
                    fileStream.Read(checkBuffer, 0, patchBytes.Length);

                    if (checkBuffer.SequenceEqual(patchBytes))
                    {
                        return 0; // Memory already patched
                    }

                    // Go to patch position
                    fileStream.Seek(functionStartPosition + signature.Offset, SeekOrigin.Begin);
                    fileStream.Write(patchBytes, 0, patchBytes.Length);

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