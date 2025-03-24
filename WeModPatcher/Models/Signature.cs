using WeModPatcher.Utils;

namespace WeModPatcher.Models
{
    public sealed class Signature
    {
        public readonly byte[] OriginalBytes;
        public readonly byte[] PatchBytes;
        public readonly byte[] Sequence;
        public readonly byte[] Mask;
        public readonly int Offset;
        
        public int Length => Sequence.Length;
        
        public static implicit operator byte[](Signature signature) => signature.Sequence;
        
        public Signature(string signature, int offset, byte[] patchBytes, byte[] originalBytes)
        {
            MemoryUtils.ParseSignature(signature, out Sequence, out Mask);
            PatchBytes = patchBytes;
            OriginalBytes = originalBytes;
            Offset = offset;
        }
    }
}