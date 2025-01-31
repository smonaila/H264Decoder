
using System.Globalization;
using h264.NALUnits;

namespace H264.Global.Methods;

public class GlobalFunctions
{
    private PPS Pps;
    private SliceHeader SliceHeader;

    public GlobalFunctions()
    {
        Pps = new PPS();
        SliceHeader = new SliceHeader();
    }

    public GlobalFunctions(SliceHeader SliceHeader, PPS Pps)
    {
        this.Pps = Pps;
        this.SliceHeader = SliceHeader;
    }

    public int NextMbAddress(int CurrMbAddr)
    {
        int 
        if (Pps.num_slice_groups_minus1 == 1)
        {
            
        }
        return 0;
    }
}