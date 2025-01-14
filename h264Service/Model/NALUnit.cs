using System.Collections;
namespace h264.NALUnits;

public class NALUnit
{
    public int ForbiddenZeroBit { get; set; }
    public int NalRefIdc { get; set; }
    public int NalUnitType { get; set; }
    public long Start { get; set; }
    public long End { get; set; }
    public ulong NalUnitLength { get; set; }
    public uint svc_extension_flag { get; set; }
    public uint avc_3d_extension_flag { get; set; }
    public byte[] rbsp_byte { get; set; } = default!;
    public uint emulation_prevention_three_byte { get; set; }
}

public class AccessUnitDelimiter : NALUnit
{
    public AccessUnitDelimiter()
    {

    }

    public AccessUnitDelimiter access_unit_delimiter_rbsp(byte[] audBytes)
    {
        try
        {
            // this.primary_pic_type = bitStream.read_bits(3);
            // bitStream.rbsp_trailing_bits();
            
            return this;
        }
        catch (System.Exception ex)
        {
            throw new Exception("Problem parsing Aud", ex);
        }
    }
    public uint primary_pic_type { get; set; }
}