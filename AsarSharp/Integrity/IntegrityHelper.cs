using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace AsarSharp.Integrity
{
    public static class IntegrityHelper
    {
        private const string ALGORITHM = "SHA256";
        // 4MB default block size
        private const int BLOCK_SIZE = 4 * 1024 * 1024;

        public class FileIntegrity
        {
            [JsonProperty("algorithm")]
            public string Algorithm { get; set; }
            
            [JsonProperty("hash")]
            public string Hash { get; set; }
            
            [JsonProperty("blockSize")]
            public int BlockSize { get; set; }
            
            [JsonProperty("blocks")]
            public List<string> Blocks { get; set; }
        }

        public static FileIntegrity GetFileIntegrity(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using(var fileHash = SHA256.Create())
            {

                var blockHashes = new List<string>();
                var buffer = new byte[BLOCK_SIZE];
                int bytesRead;

                while ((bytesRead = fileStream.Read(buffer, 0, BLOCK_SIZE)) > 0)
                {
                    var block = new byte[bytesRead];
                    Array.Copy(buffer, block, bytesRead);
                    blockHashes.Add(HashBlock(block));
                    fileHash.TransformBlock(block, 0, block.Length, null, 0);
                }

                fileHash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                return new FileIntegrity
                {
                    Algorithm = ALGORITHM,
                    Hash = BitConverter.ToString(fileHash.Hash).Replace("-", "").ToLowerInvariant(),
                    BlockSize = BLOCK_SIZE,
                    Blocks = blockHashes,
                };
            }
        }

        private static string HashBlock(byte[] block)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(block);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}