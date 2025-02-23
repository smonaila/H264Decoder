using Decoder.H264ArrayParsers;
using H264Utilities.Descriptors;
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

public partial class SliceHeader
{
    public enum Slicetype : uint
    {
        P,
        B,
        I,
        SP,
        SI,
        PS,
        BS,
        IS,
        SPS,
        SPSL,
        SIS
    }
}

public partial class SliceHeader
{
    private SPS Sps;
    private PPS Pps;
    private NALUnit nalunit;

    public SliceHeader() 
    { 
        this.Sps = new SPS();
        this.Pps = new PPS();
        this.nalunit = new NALUnit();
    }
    public SliceHeader(SPS Sps, PPS pps, NALUnit NalUnit)
    {
        this.Sps = Sps;
        this.Pps = pps;
        this.nalunit = NalUnit;
    }

    public uint first_mb_in_slice { get; set; }
    public bool sp_for_switch_flag { get; set; }
    public Slicetype slice_type { get; set; }
    public uint pic_parameter_set_id { get; set; }
    public uint colour_plane_id { get; set; }
    public uint frame_num { get; set; }
    public bool field_pic_flag { get; set; }
    public bool bottom_field_flag { get; set; }
    public uint idr_pic_id { get; set; }
    public uint pic_order_cnt_lsb { get; set; }
    public uint delta_pic_order_cnt_bottom { get; set; }
    public List<uint> delta_pic_order_cnt { get; set; } = new List<uint>();
    public uint redundant_pic_cnt { get; set; }
    public bool direct_spatial_mv_pred_flag { get; set; }
    public bool num_ref_idx_active_override_flag { get; set; }
    public uint num_ref_idx_l0_active_minus1 { get; set; }
    public uint num_ref_idx_l1_active_minus1 { get; set; }
    public uint cabac_init_idc { get; set; }
    public int slice_qp_delta { get; set; }
    public int slice_qs_delta { get; set; }
    public uint disable_deblocking_filter_idc { get; set; }
    public int slice_alpha_c0_offset_div2 { get; set; }
    public int slice_beta_offset_div2 { get; set; }
    public uint slice_group_change_cycle { get; set; }
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
    public uint chroma_format_idc { get; set; } = 1;
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

public class PPS : NALUnit
{
    public PPS()
    {

    }

    public uint pic_parameter_set_id { get; set; }
    public uint seq_parameter_set_id { get; set; }
    public bool entropy_coding_mode_flag { get; set; }
    public bool bottom_field_pic_order_in_frame_present_flag { get; set; }
    public uint num_slice_groups_minus1 { get; set; }
    public uint slice_group_map_type { get; set; }
    public List<uint> run_length_minus1 { get; set; } = new List<uint>();
    public List<uint> top_left { get; set; } = new List<uint>();
    public List<uint> bottom_right { get; set; } = new List<uint>();
    public bool slice_group_change_direction_flag { get; set; }
    public uint slice_group_change_rate_minus1 { get; set; }
    public uint pic_size_in_map_units_minus1 { get; set; }
    public List<uint> slice_group_id { get; set; } = new List<uint>();
    public uint num_ref_idx_l0_default_active_minus1 { get; set; }
    public uint num_ref_idx_l1_default_active_minus1 { get; set; }
    public bool weighted_pred_flag { get; set; }
    public uint weighted_bipred_idc { get; set; }
    public int pic_init_qp_minus26 { get; set; }
    public int pic_init_qs_minus26 { get; set; }
    public int chroma_qp_index_offset { get; set; }
    public bool deblocking_filter_control_present_flag { get; set; }
    public bool constrained_intra_pred_flag { get; set; }
    public bool redundant_pic_cnt_present_flag { get; set; }
    public bool transform_8x8_mode_flag { get; set; }
    public bool pic_scaling_matrix_present { get; set; }
    public List<bool> pic_scaling_list_present_flag { get; set; } = new List<bool>();
    public int second_chroma_qp_index_offset { get; set; }
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
    public uint primary_pic_type { get; set; }
}

public class DataPartitionC : NALUnit
{
    public DataPartitionC()
    {
        
    }

    public DataPartitionC slice_data_partition_c_layer_rbsp(BitList bitStream)
    {
        try
        {
            this.slice_id = bitStream.ue();
            this.colour_plane_id = Convert.ToUInt32(bitStream.read_bits(2), 2);
            this.redundant_pic_cnt = bitStream.ue();
            
            return this;
        }
        catch (System.Exception)
        {
            throw;
        }
    }
    public uint slice_id { get; set; }
    public uint colour_plane_id { get; set; }
    public uint redundant_pic_cnt { get; set; }
}