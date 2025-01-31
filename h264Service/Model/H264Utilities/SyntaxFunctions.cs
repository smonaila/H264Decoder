using Decoder.H264ArrayParsers;
using h264.utilities;

namespace H264Utilities.Descriptors;

public static class SyntaxFunction
{
    public static bool byte_aligned(this BitList bitStream)
    {
        try
        {
            bool is_aligned = bitStream.Position % 8 == 0;
            return is_aligned;
        }
        catch (System.Exception)
        {
            throw;
        }
    }
    public static void rbsp_trailing_bits(this BitList bitStream)
    {
        try
        {
            string rbsp_stop_one_bit = bitStream.f(1);
            string rbsp_alignment_zero_bit = string.Empty;
            while (!bitStream.byte_aligned())
            {
                rbsp_alignment_zero_bit += bitStream.f(1);
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static string read_bits(this BitList bitStream, uint BitCounter)
    {
        string bits = string.Empty;
        if (BitCounter + bitStream.Position <= bitStream.Length && BitCounter > 0)
        {
            bits = bitStream.ReadBits(BitCounter);
        }
        return BitCounter == 0 ? string.Format(@"{0}", 0) : bits;
    }

    public static string next_bits(this BitList bitStream, uint BitCounter)
    {
        string nextbits = bitStream.NextBits(BitCounter);
        return nextbits;
    }

    public static bool more_rbsp_data(this BitList bitStream)
    {
        try
        {
            if (bitStream.Position < bitStream.Length)
            {
                return true;
            }
            return false;
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}