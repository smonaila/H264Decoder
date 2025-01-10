namespace h264.NALUnit

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