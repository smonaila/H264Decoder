
using System.Text.Json;
using Decoder.H264ArrayParsers;
using H264.Global.Variables;
using H264.Types;
using H264Utilities.Parsers;
using SynElem.Binarization;

namespace H264Utilities.Descriptors;

public static class SyntaxDescriptors
{
    
    public static uint me(this BitList bitStream)
    {
        try
        {
            int codeNum = (int)bitStream.ue();
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;

            int mappedValue = GetCodedBlockPatternValue(codeNum, globalVariables.ChromaArrayType);

            return (uint)mappedValue;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private static int GetCodedBlockPatternValue(int codeNum, ushort chromaArrayType)
    {
        try
        {
            using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\CodedBlockPatternMapping.json"))
            {
                string JsonCodeNum = streamReader.ReadToEnd();
                var ChromaArrayIdc = JsonSerializer.Deserialize<ChromaArrayIdc>(JsonCodeNum);
                int value = 0;
                if ((chromaArrayType == 1 || chromaArrayType == 2) && ChromaArrayIdc != null)
                {
                    var ChromaPatternIdc = (from cATpe in ChromaArrayIdc.CodedBlockPatternsChroma1or2
                             where cATpe.CodeNum == codeNum
                             select cATpe).FirstOrDefault();
                    if (ChromaPatternIdc != null)
                    {
                        value = ChromaPatternIdc.CodedBlockPattern.Intra4x4_8x8;
                    }                    
                } else if ((chromaArrayType == 0 || chromaArrayType == 3) && ChromaArrayIdc != null)
                {
                    var ChromaPatternIdc = (from cATpe in ChromaArrayIdc.CodedBlockPatternsChroma0or3
                                            where cATpe.CodeNum == codeNum
                                            select cATpe).FirstOrDefault();
                    if (ChromaPatternIdc != null)
                    {
                        value = ChromaPatternIdc.CodedBlockPattern.Intra4x4_8x8;
                    }
                } else
                {
                    value = -1;
                }
                return value;
            }
        }
        catch (System.Exception)
        {
            
            throw;
        }
    }

    public static int te(this BitList bitStream, int maxRangeX)
    {
        try
        {
            int CodeNum = 0;
            if (maxRangeX > 1)
            {
                CodeNum = (int)bitStream.ue();
            } else
            {
                CodeNum = int.Parse(bitStream.read_bits(1)) == 1 ? 1 : 0;
            }
            return CodeNum;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public static int ae(this BitList bitStream, SynElemSlice synElemSlice)
    {
        try
        {
            BinarizationSchemes binarizationSchemes = new BinarizationSchemes();
            List<ContextVariable> contextVariables = binarizationSchemes.Initialization(synElemSlice);          


            return 0;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public static int ae(this BitList bitStream)
    {
        try
        {
            return 0;
        }
        catch (System.Exception)
        {
            
            throw;
        }
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