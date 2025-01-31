
using Decoder.H264ArrayParsers;
using H264Utilities.Parsers;

namespace H264Utilities.Descriptors;

public static class SyntaxDescriptors
{
    public static uint u(this BitList bitStream)
    {
        uint num = 0;
        return num;
    }

    public static string f(this BitList bitStream, uint n)
    {
        try
        {
            uint Value = Convert.ToUInt32(bitStream.read_bits(n), 2);
            return Convert.ToString(Value, toBase: 2);
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static uint u(this BitList bitStream, uint bitCounter)
    {
        string bits = bitStream.read_bits(bitCounter);
        // bits = h264Array.Reverse(bits);
        uint byteValue = Convert.ToUInt32(bits, 2);
        return byteValue;
    }

    public static uint ue(this BitList bitStream)
    {
        uint CodeNum;
        try
        {
            int leadingZeroBits = -1;
            for (bool b = false; !b; leadingZeroBits++)
            {
                if (!bitStream.more_rbsp_data())
                {
                    leadingZeroBits = 0;
                    break;
                }
                b = int.Parse(bitStream.read_bits(1)) == 1;
            }
            string bits = bitStream.more_rbsp_data() == true ? bitStream.read_bits((uint)leadingZeroBits) : string.Format(@"{0}", 0);
            uint leadingZerosInt =  Convert.ToUInt32(bits, 2);
            CodeNum = (uint)Math.Pow(2, leadingZeroBits) - 1 + leadingZerosInt;
        }
        catch (System.Exception)
        {
            throw;
        }
        return CodeNum;
    }

    
    public static byte b(this BitList bitStream, int bitCounter)
    {
        string bits = bitStream.read_bits((uint)bitCounter);
        byte value = Convert.ToByte(bits, 2);

        return value;
    }

    public static int se(this BitList bitStream)
    {
        try
        {
            uint CodeNum = bitStream.ue();
            int SignedNum = (int)Math.Pow(-1, CodeNum + 1) * (int)Math.Ceiling((decimal)(CodeNum / 2));
            return SignedNum;
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}