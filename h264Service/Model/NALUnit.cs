using System.Collections;
using h264.utilities;
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

public class SPS : NALUnit
{
    public SPS()
    {

    }

    public uint profile_Idc { get; set; }
    public bool constraint_set0_flag { get; set; }
    public bool constraint_set1_flag { get; set; }
    public bool constraint_set2_flag { get; set; }
    public bool constraint_set3_flag { get; set; }
    public bool constraint_set4_flag { get; set; }
    public bool constraint_set5_flag { get; set; }

    public uint reserved_zero_2bits { get; set; }
    public uint level_idc { get; set; }
    public uint seq_parameter_set_id { get; set; }

    public uint chroma_format_idc { get; set; }
    public uint separate_colour_plane { get; set; }
    public uint bit_depth_luma_minus8 { get; set; }
    public uint bit_depth_chroma_minus8 { get; set; }
    public uint qpprime_y_zero_transform_bypass_flag { get; set; }
    public uint seq_scaling_matrix_present_flag { get; set; }
    public List<uint> seq_scaling_list_present_flag { get; set; } = default!;
    public uint log2_max_frame_num_minus4 { get; set; }
    public uint pic_order_cnt_type { get; set; }
    public uint log2_max_pic_order_cnt_lsb_minus4 { get; set; }

    public bool delta_pic_order_always_zero_flag { get; set; }
    public uint offset_for_non_ref_pic { get; set; }
    public uint offset_for_top_to_bottom_field { get; set; }
    public uint num_ref_frames_in_pic_order_cnt_cycle { get; set; }
    public uint[] offset_for_ref_frames { get; set; } = default!;

    public uint max_num_ref_frames { get; set; }
    public uint gaps_in_frame_num_value_allowed_flag { get; set; }
    public uint pic_width_in_mbs_minus1 { get; set; }
    public uint pic_height_in_map_units_minus1 { get; set; }
    public bool frame_mbs_only_flag { get; set; }
    public uint mb_adaptive_frame_field_flag { get; set; }
    public uint direct_8x8_inference_flag { get; set; }
    public uint frame_cropping_flag { get; set; }
    public uint frame_crop_left_offset { get; set; }
    public uint frame_crop_right_offset { get; set; }
    public uint frame_crop_top_offset { get; set; }
    public uint fram_crop_bottom_offset { get; set; }

    public uint vui_parameters_present_flag { get; set; }

    public List<int[]> scaling_list4x4 { get; set; } = new List<int[]>();
    public List<int[]> scaling_list8x8 { get; set; } = new List<int[]>();
    public Dictionary<int, bool> UseDefaultScaling4x4Flag { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> UseDefaultScaling8x8Flag { get; set; } = new Dictionary<int, bool>();
}

public class VuiParameters
{
    public VuiParameters()
    {

    }

    public uint max_num_reorder_frames { get; set; }
    public bool aspect_ratio_info_present_flag { get; set; }
    public bool overscan_info_present_flag { get; set; }
    public uint aspect_ratio_idc { get; set; }
    public uint sar_width { get; set; }
    public uint sar_height { get; set; }
    public bool overscan_appropriate_flag { get; set; }
    public bool video_signal_type_present_flag { get; set; }
    public uint video_format { get; set; }
    public bool video_full_range_flag { get; set; }
    public bool colour_discription_present_flag { get; set; }
    public uint colour_primaries { get; set; }
    public uint transfer_characteristics { get; set; }
    public uint matrix_coefficients { get; set; }
    public bool chroma_loc_info_present_flag { get; set; }
    public uint chroma_sample_loc_top_field { get; set; }
    public uint chroma_sample_loc_type_bottom { get; set; }
    public bool timing_info_present_flag { get; set; }
    public uint num_units_in_stick { get; set; }
    public uint time_scale { get; set; }
    public bool fixed_frame_rate_flag { get; set; }
    public bool nal_hrd_parameters_present_flag { get; set; }
    public bool vcl_hrd_parameters_present_flag { get; set; }
    public bool low_delay_hrd_flag { get; set; }
    public bool pic_struct_present_flag { get; set; }
    public bool bitstream_restriction_flag { get; set; }
    public bool motion_vectors_over_pic_boundaries_flag { get; set; }
    public uint max_bytes_per_pic_denom { get; set; }
    public uint max_bits_per_mb_denom { get; set; }
    public uint log2_max_mv_length_horizontal { get; set; }
    public uint log2_max_mv_length_vertical { get; set; }
    public uint max_dec_frame_buffering { get; set; }
    public uint chroma_sample_loc_type_bottom_field { get; set; }
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