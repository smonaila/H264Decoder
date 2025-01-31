using System.Collections;
using System.IO.Compression;
using H264Utilities.Descriptors;

namespace Decoder.H264ArrayParsers;

public class BitList
{
    private BitArray bitArray;
    public BitList(byte[] bytes) 
    { 
        bitArray = new BitArray(bytes.Length * 8);
        int bitIndex = 0;
        for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
        {
            byte[] tempByte = new byte[1];
            tempByte[0] = bytes[byteIndex];
            BitArray tempArray = new BitArray(tempByte);
            for (int i = tempArray.Length - 1; i >= 0; i--)
            {
                bitArray.Set(bitIndex++, tempArray.Get(i));
            }
        }
    }

    public uint Read(uint BitCounter)
    { 
        string bits = ReadBits(BitCounter);
        int value = Convert.ToInt32(bits, 2);
        return (uint)value;
    }

    public string NextBits(uint BitCounter)
    {
        string bits = string.Empty;
        if (BitCounter + Position <= bitArray.Length && BitCounter > 0)
        {
            bits = GetBits(BitCounter);
        }else
        {
            bits = string.Format("{0}", 0);
        }
        return bits;
    }

    private string GetBits(uint BitCounter)
    {
        string nextbits = string.Empty;
        try
        {
            if (Position + BitCounter < bitArray.Length && BitCounter > 0)
            {
                for (int bitIndex = (int)Position; bitIndex < (BitCounter + (int)Position); bitIndex++)
                {
                    nextbits += string.Format("{0}", bitArray.Get(bitIndex) == true ? 1 : 0);
                }
            }
            else
            {
                nextbits = string.Format(@"{0}", 0);
            }
        }
        catch (OverflowException)
        {

        }
        catch (System.Exception)
        {
            throw;
        }
        return nextbits;
    }

    public string ReadBits(uint BitCounter)
    {
        string bits = string.Empty;
        if (BitCounter > 0 && Position < bitArray.Length)
        {
            bits = GetBits(BitCounter);
            Position += BitCounter;
        }
        return bits == string.Empty ? string.Format(@"{0}", 0) : bits;
    }

    public uint Position { get; set; }
    public uint Length { get { return (uint)bitArray.Length; } }
}

public static class h264Array
{
    public static string Reverse(string bits)
    {
        char[] charBits = bits.ToCharArray();
        Array.Reverse(charBits);
        bits = new string(charBits);
        return bits;
    }

    public static int[,] Copy2DArray(int[,] dest, int destIndex, int[] source, int sourcIndex, int Length)
    {
        for (int coeffientIndex = 0; coeffientIndex < Length; coeffientIndex++)
        {
            dest[destIndex, coeffientIndex] = source[sourcIndex++];
        }
        return dest;
    }

    public static int[,,] Copy3DArray(int[,,] dest, int iCbCr, int col, int[] source, int sourceIndex, int Length)
    {
        for (int coeffientIndex = 0; coeffientIndex < source.Length; coeffientIndex++)
        {
            dest[iCbCr, col, coeffientIndex] = source[coeffientIndex];
        }
        return dest;
    }
}