
using H264.Types;

namespace H264.Global.Variables;
public class GlobalVariables
{
    public ushort CodedBlockPatternLuma { get; set; }
    public ushort CodedBlockPatternChroma { get; set; }
    public ushort ChromaArrayType { get; set; }
    public int NumC8x8 { get; set; }
    public SubWidthHeight SubWidthC { get; set; }
    public SubWidthHeight SubHeightC { get; set; }
    public int CurrMbAddr { get; set; }
    public int PicWidthInMbs { get; set; }
    public int MbaffFrameFlag { get; set; }
}