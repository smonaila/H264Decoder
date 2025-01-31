using System.Text.Json.Serialization;

namespace H264.Types;

public enum SliceGroupMapType
{
    BoxOutClockwise = 0,
    BoxOutCounterClockwise = 1,
    RasterScan = 2,
    ReverseRasterScan = 3,
    WipeRight = 4,
    WipeLeft = 5
}
public enum ChromaFormatIdc
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3
}
public enum ChromaTypes
{
    Monochrome = 0,
    FourTwoZero = 1,
    FourTwoTwo = 2,
    FourFourFour = 3
}
public enum SubWidthHeight
{
    Undefined = 0,
    One = 1,
    Two = 2
}
public class ChromaFormat
{
    [JsonPropertyName("chroma_format_idc")]
    public ChromaFormatIdc ChromaFormatIdc { get; set; }
    [JsonPropertyName("separate_colour_plane_flag")]
    public uint SeparateColorPlaneFlag { get; set; }
    [JsonPropertyName("chroma_format")]
    public ChromaFormatIdc Format { get; set; }
    [JsonPropertyName("sub_width_c")]
    public SubWidthHeight SubWidthC { get; set; }
    [JsonPropertyName("sub_height_c")]
    public SubWidthHeight SubHeightC { get; set; }
}