using System;
using h264.NALUnits;

namespace h264.syntaxstructures;

public class SliceData
{
    private SliceHeader slice_header;
    private PPS Pps;
    public SliceData() 
    { 
        slice_header = new SliceHeader();
        Pps = new PPS();
    }
    public SliceData(SliceHeader sliceHeader, PPS Pps) 
    {
        this.Pps = Pps;
        slice_header = sliceHeader;
    }

    public string cabac_alignment_one_bit { get; set; } = string.Empty;
    public uint mb_skip_run { get; set; }
    public bool mb_skip_flag { get; set; }
    public bool mb_field_decoding { get; set; }
    public bool end_of_slice_flag { get; set; }
}

public class DecRefPicMarking
{
    public DecRefPicMarking()
    { 

    }
    
    public bool no_output_of_prior_pics_flags { get; set; }
    public bool long_term_reference_flags { get; set; }
    public bool adaptive_ref_pic_marking_mode_flags { get; set; }
    public uint memory_management_control_operation { get; set; }
    public uint different_of_pic_nums_minus1 { get; set; }
    public uint long_term_pic_num { get; set; }
    public uint long_term_frame_idx { get; set; }
    public uint max_long_term_frame_idx_plus1 { get; set; }
}

public class RefPicListModification
{
    private readonly SliceHeader slice_header;
    public RefPicListModification() 
    { 
        slice_header = new SliceHeader();
    }

    public RefPicListModification(SliceHeader sliceHeader)
    {
        slice_header = sliceHeader;
    }

    public bool ref_pic_list_modification_flag_l0 { get; set; }
    public uint modification_of_pic_nums_idc { get; set; }
    public uint abs_diff_pic_num_minus1 { get; set; }
    public uint long_term_pic_num { get; set; }
}

public class SubMbPredLayer
{
    private uint mbtype;
    public SubMbPredLayer(uint mb_type)
    {
        mbtype = mb_type;
        
    }

    public SubMbPredLayer GetSubMbPredLayer()
    {
        return this;
    }

    public List<uint> sub_mb_type { get; set; } = new List<uint>();
    public List<uint> ref_idx_l0 { get; set; } = new List<uint>();
    public List<uint> ref_idx_l1 { get; set; } = new List<uint>();
    public List<uint> mvd_l0 { get; set; } = new List<uint>();
    public List<uint> mvd_l1 { get; set; } = new List<uint>();
}

// The mb_pred Syntax structure.
public class MbPred
{
    
}