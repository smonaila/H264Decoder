using System.Text.Json;
using Decoder.H264ArrayParsers;
using h264.NALUnits;
using h264.syntaxstructures;
using h264.utilities;
using H264Utilities.Descriptors;
using static h264.NALUnits.SliceHeader;
using H264.Global.Variables;
using H264.Global.Methods;
using H264.Types;
using System.IO.Compression;

namespace H264Utilities.Parsers;

public class H264Parsers
{
    private ICodecSettingsService settingsService;
    public H264Parsers()
    {
        settingsService = new CodecSettings();
    }
    public H264Parsers(ICodecSettingsService codecSettingsService)
    {
        settingsService = codecSettingsService;
    }

    public SliceHeader get_slice_header(BitList bitStream, NALUnit NalUnit)
    {
        try
        {
            SettingSets codecSettings = settingsService.GetCodecSettings();
            PPS Pps = codecSettings.GetPPS;
            SPS Sps = codecSettings.GetSPS;
            GlobalVariables globalVariables = codecSettings.GlobalVariables;

            SliceHeader sliceHeader = new SliceHeader(Sps, Pps, NalUnit);

            bool IdrPicFlag = (NalUnit.NalUnitType == 5) ? true : false;

            sliceHeader.first_mb_in_slice = bitStream.ue();
            uint sliceType = bitStream.ue();
            sliceHeader.slice_type = (Slicetype)(sliceType >= 5 ? sliceType - 5 : sliceType);
            sliceHeader.pic_parameter_set_id = bitStream.ue();

            if (Sps.separate_colour_plane == 1)
            {
                sliceHeader.colour_plane_id = Convert.ToUInt32(bitStream.read_bits(2));
            }
            sliceHeader.frame_num = bitStream.u(Sps.log2_max_frame_num_minus4 + 4);
            if (!Sps.frame_mbs_only_flag)
            {
                sliceHeader.field_pic_flag = Convert.ToUInt32(bitStream.read_bits(1)) == 1;
                if (sliceHeader.field_pic_flag)
                {
                    sliceHeader.bottom_field_flag = Convert.ToUInt32(bitStream.read_bits(1)) == 1;
                }
            }
            if (IdrPicFlag)
            {
                sliceHeader.idr_pic_id = bitStream.ue();
            }
            if (Sps.pic_order_cnt_type == 0)
            {
                sliceHeader.pic_order_cnt_lsb = bitStream.ue();
                if (Pps.bottom_field_pic_order_in_frame_present_flag)
                {
                    sliceHeader.delta_pic_order_cnt_bottom = (uint)bitStream.se();
                }
            }

            if (Sps.pic_order_cnt_type == 1 && !Sps.delta_pic_order_always_zero_flag)
            {
                sliceHeader.delta_pic_order_cnt.Add((uint)bitStream.se());
                if (Pps.bottom_field_pic_order_in_frame_present_flag && !sliceHeader.field_pic_flag)
                {
                    sliceHeader.delta_pic_order_cnt.Add((uint)bitStream.se());
                }
            }
            if (Pps.redundant_pic_cnt_present_flag)
            {
                sliceHeader.redundant_pic_cnt = bitStream.ue();
            }
            if (sliceHeader.slice_type == Slicetype.B)
            {
                sliceHeader.direct_spatial_mv_pred_flag = Convert.ToUInt32(bitStream.read_bits(1)) == 1;
            }
            if (sliceHeader.slice_type == Slicetype.P || sliceHeader.slice_type == Slicetype.SP || sliceHeader.slice_type == Slicetype.B)
            {
                sliceHeader.num_ref_idx_active_override_flag = Convert.ToUInt32(bitStream.read_bits(1)) == 1;
                if (sliceHeader.num_ref_idx_active_override_flag)
                {
                    sliceHeader.num_ref_idx_l0_active_minus1 = bitStream.ue();
                    if (sliceHeader.slice_type == Slicetype.B)
                    {
                        sliceHeader.num_ref_idx_l1_active_minus1 = bitStream.ue();
                    }
                }
            }
            if (NalUnit.NalUnitType == 20 || NalUnit.NalUnitType == 21)
            {
                ref_pic_list_mvc_modification(bitStream);
            }
            else
            {
                ref_pic_list_modification(bitStream, sliceHeader);
            }
            if ((Pps.weighted_pred_flag && (sliceHeader.slice_type == (uint)Slicetype.P || sliceHeader.slice_type == Slicetype.SP))
            || (Pps.weighted_bipred_idc == 1 && sliceHeader.slice_type == Slicetype.B))
            {
                pred_weight_table(bitStream);
            }
            if (NalUnit.NalRefIdc != 0)
            {
                dec_ref_pic_marking(bitStream, NalUnit);
            }
            if (Pps.entropy_coding_mode_flag && sliceHeader.slice_type != Slicetype.I && sliceHeader.slice_type != Slicetype.SI)
            {
                sliceHeader.cabac_init_idc = bitStream.ue();
            }
            sliceHeader.slice_qp_delta = bitStream.se();
            if (sliceHeader.slice_type == Slicetype.SP || sliceHeader.slice_type == Slicetype.SI)
            {
                if (sliceHeader.slice_type == Slicetype.SP)
                {
                    sliceHeader.sp_for_switch_flag = Convert.ToUInt32(bitStream.read_bits(1)) == 1;
                }
                sliceHeader.slice_qs_delta = bitStream.se();
            }
            if (Pps.deblocking_filter_control_present_flag)
            {
                sliceHeader.disable_deblocking_filter_idc = bitStream.ue();
                if (sliceHeader.disable_deblocking_filter_idc != 1)
                {
                    sliceHeader.slice_alpha_c0_offset_div2 = bitStream.se();
                    sliceHeader.slice_beta_offset_div2 = bitStream.se();
                }
            }
            if (Pps.num_slice_groups_minus1 > 0 && Pps.slice_group_map_type >= 3 && Pps.slice_group_map_type <= 5)
            {
                sliceHeader.slice_group_change_cycle = bitStream.ue();
            }
            globalVariables.MbaffFrameFlag = ((Sps.mb_adaptive_frame_field_flag == 1) && (!(sliceHeader.field_pic_flag == true))) == true ? 1 : 0;
            globalVariables.PicHeightInMbs = globalVariables.FrameHeightInMbs / (1 + (sliceHeader.field_pic_flag == true ? 1 : 0));
            globalVariables.PicHeightInSamplesL = globalVariables.PicHeightInMbs * globalVariables.MbHeightC;
            globalVariables.PicSizeInMbs = globalVariables.PicWidthInMbs * globalVariables.PicHeightInMbs;
            globalVariables.MaxPicNum = !sliceHeader.field_pic_flag ? globalVariables.MaxFrameNum : 2 * globalVariables.MaxFrameNum;
            globalVariables.CurrPicNum = !sliceHeader.field_pic_flag ? sliceHeader.frame_num : 2 * sliceHeader.frame_num;
            globalVariables.SliceQPY = 26 + Pps.pic_init_qp_minus26 + sliceHeader.slice_qp_delta;
            globalVariables.QSY = 26 + Pps.pic_init_qp_minus26 + sliceHeader.slice_qp_delta;
            globalVariables.FilterOffsetA = sliceHeader.slice_alpha_c0_offset_div2 << 1;
            globalVariables.FilterOffsetB = sliceHeader.slice_beta_offset_div2 << 1;
            globalVariables.MapUnitsInSliceGroup0 = (int)Math.Min(sliceHeader.slice_group_change_cycle * globalVariables.SliceGroupChangeRate,
            globalVariables.PicSizeInMapUnits);

            codecSettings.GlobalVariables = globalVariables;
            settingsService.Update<GlobalVariables>(globalVariables);
            settingsService.Update<SliceHeader>(sliceHeader);

            return sliceHeader;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static DecRefPicMarking dec_ref_pic_marking(BitList bitStream, NALUnit NalUnit)
    {
        try
        {
            DecRefPicMarking decRefPicMarking = new DecRefPicMarking();
            bool IdrPicFlag = (NalUnit.NalUnitType == 5);
            if (IdrPicFlag)
            {
                decRefPicMarking.no_output_of_prior_pics_flags = bitStream.u(1) == 1;
                decRefPicMarking.long_term_reference_flags = bitStream.u(1) == 1;
            }
            else
            {
                decRefPicMarking.adaptive_ref_pic_marking_mode_flags = bitStream.u(1) == 1;
                if (decRefPicMarking.adaptive_ref_pic_marking_mode_flags)
                {
                    do
                    {
                        decRefPicMarking.memory_management_control_operation = bitStream.ue();
                        if (decRefPicMarking.memory_management_control_operation == 1 ||
                            decRefPicMarking.memory_management_control_operation == 3)
                        {
                            decRefPicMarking.different_of_pic_nums_minus1 = bitStream.ue();
                        }
                        if (decRefPicMarking.memory_management_control_operation == 2)
                        {
                            decRefPicMarking.long_term_pic_num = bitStream.ue();
                        }
                        if (decRefPicMarking.memory_management_control_operation == 3 ||
                            decRefPicMarking.memory_management_control_operation == 6)
                        {
                            decRefPicMarking.long_term_frame_idx = bitStream.ue();
                        }
                        if (decRefPicMarking.memory_management_control_operation == 4)
                        {
                            decRefPicMarking.max_long_term_frame_idx_plus1 = bitStream.ue();
                        }

                    } while (decRefPicMarking.memory_management_control_operation != 0);
                }
            }
            return decRefPicMarking;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static void pred_weight_table(BitList bitStream)
    {

    }

    public static RefPicListModification ref_pic_list_modification(BitList bitStream, SliceHeader sliceHeader)
    {
        try
        {
            RefPicListModification refPicListModification = new RefPicListModification(sliceHeader);
            return refPicListModification;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static void ref_pic_list_mvc_modification(BitList bitStream)
    {

    }

    public static AccessUnitDelimiter access_unit_delimiter_rbsp(byte[] audBytes)
    {
        AccessUnitDelimiter unitDelimiter = new AccessUnitDelimiter();
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(audBytes))
            {
                BitList bitStream = new BitList(audBytes);
                unitDelimiter.primary_pic_type = Convert.ToUInt32(bitStream.read_bits(3), 2);
                bitStream.rbsp_trailing_bits();

                return unitDelimiter;
            }
        }
        catch (System.Exception ex)
        {
            throw new Exception("Problem parsing Aud", ex);
        }
    }

    public static NALUnit nal_unit(byte[] NALrbsp, long NalUnitLength)
    {
        NALUnit NALUnit = new NALUnit();
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(NALrbsp))
            {
                BitList bitStream = new BitList(NALrbsp);
                int NumBytesInRBSP = 0;
                int nalUnitHeaderBytes = 1;
                NALUnit.ForbiddenZeroBit = bitStream.f(1) == string.Format(@"{0}", 1) ? 1 : 0;
                NALUnit.NalRefIdc = (int)bitStream.u(2);
                NALUnit.NalUnitType = (int)bitStream.u(5);

                NALUnit.rbsp_byte = new byte[NalUnitLength - 1];
                if (NALUnit.NalUnitType == 14 || NALUnit.NalUnitType == 20 || NALUnit.NalUnitType == 21)
                {
                    if (NALUnit.NalUnitType != 21)
                    {
                        NALUnit.svc_extension_flag = (uint)(bitStream.u(1) == 1 ? 1 : 0);
                    }
                    else
                    {
                        NALUnit.avc_3d_extension_flag = (uint)(bitStream.u(1) == 1 ? 1 : 0);
                    }
                    if (NALUnit.svc_extension_flag == 1)
                    {
                        // nal_unit_header_svc_extension();
                        NumBytesInRBSP += 3;
                    }
                    else if (NALUnit.avc_3d_extension_flag == 1)
                    {
                        // nal_unit_header_3davc_extension();
                        nalUnitHeaderBytes += 2;
                    }
                    else
                    {
                        // nal_unit_header_mvc_extension()
                        nalUnitHeaderBytes += 3;
                    }
                }

                for (int i = nalUnitHeaderBytes; i < NalUnitLength; i++)
                {
                    uint nextbits = Convert.ToUInt32(bitStream.next_bits(24), 2);
                    if (i + 2 < NalUnitLength && nextbits == 0x000003)
                    {
                        NALUnit.rbsp_byte[NumBytesInRBSP++] = bitStream.b(8);
                        NALUnit.rbsp_byte[NumBytesInRBSP++] = bitStream.b(8);
                        i += 2;
                        NALUnit.emulation_prevention_three_byte = (uint)(bitStream.f(8) == string.Format(@"00000011") ? 3 : 0);
                    }
                    else
                    {
                        NALUnit.rbsp_byte[NumBytesInRBSP++] = bitStream.b(8);
                    }
                }
            }
        }
        catch (System.Exception)
        {
            throw;
        }
        return NALUnit;
    }

    public static HrdParameters set_hrd_parameters(BitList bitStream)
    {
        try
        {
            HrdParameters hrdParameters = new HrdParameters();

            hrdParameters.cpb_cnt_minus1 = bitStream.ue() + 1;
            hrdParameters.bit_rate_scale = bitStream.u(4);
            hrdParameters.cpb_size_scale = bitStream.u(4);
            hrdParameters.bit_rate_value_minus1 = new List<uint>();
            hrdParameters.cpb_size_value_minus1 = new List<uint>();
            hrdParameters.cbr_flag = new List<bool>();

            for (int SchedSelIdx = 0; SchedSelIdx <= hrdParameters.cpb_cnt_minus1; SchedSelIdx++)
            {
                hrdParameters.bit_rate_value_minus1.Add(bitStream.ue());
                hrdParameters.cpb_size_value_minus1.Add(bitStream.ue());
                hrdParameters.cbr_flag.Add(bitStream.u(1) == 1);
            }
            hrdParameters.initial_cpb_removal_delay_length_minus1 = bitStream.u(5);
            hrdParameters.cpb_removal_delay_length_minus1 = bitStream.u(5);
            hrdParameters.dpb_output_delay_length_minus = bitStream.u(5);
            hrdParameters.time_offset_length = bitStream.u(5);
            return hrdParameters;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static VuiParameters vui_parameters(BitList bitStream)
    {
        try
        {
            VuiParameters vuiParameters = new VuiParameters();
            vuiParameters.aspect_ratio_info_present_flag = bitStream.u(1) == 1;

            uint Extended_SAR = 255;
            if (vuiParameters.aspect_ratio_info_present_flag)
            {
                vuiParameters.aspect_ratio_idc = bitStream.u(8);
                if (vuiParameters.aspect_ratio_idc == Extended_SAR)
                {
                    vuiParameters.sar_width = bitStream.u(16);
                    vuiParameters.sar_height = bitStream.u(16);
                }
            }
            vuiParameters.overscan_info_present_flag = bitStream.u(1) == 1;
            if (vuiParameters.overscan_info_present_flag)
            {
                vuiParameters.overscan_appropriate_flag = bitStream.u(1) == 1;
            }
            vuiParameters.video_signal_type_present_flag = bitStream.u(1) == 1;
            if (vuiParameters.video_signal_type_present_flag)
            {
                vuiParameters.video_format = bitStream.u(3);
                vuiParameters.video_full_range_flag = bitStream.u(1) == 1;
                vuiParameters.colour_discription_present_flag = bitStream.u(1) == 1;

                if (vuiParameters.colour_discription_present_flag)
                {
                    vuiParameters.colour_primaries = bitStream.u(8);
                    vuiParameters.transfer_characteristics = bitStream.u(8);
                    vuiParameters.matrix_coefficients = bitStream.u(8);
                }
            }
            vuiParameters.chroma_loc_info_present_flag = bitStream.u(1) == 1;
            if (vuiParameters.chroma_loc_info_present_flag)
            {
                vuiParameters.chroma_sample_loc_top_field = bitStream.ue();
                vuiParameters.chroma_sample_loc_type_bottom_field = bitStream.ue();
            }
            vuiParameters.timing_info_present_flag = bitStream.u(1) == 1;
            if (vuiParameters.timing_info_present_flag)
            {
                vuiParameters.num_units_in_stick = bitStream.u(32);
                vuiParameters.time_scale = bitStream.u(32);
                vuiParameters.fixed_frame_rate_flag = bitStream.u(1) == 1;
            }
            vuiParameters.nal_hrd_parameters_present_flag = bitStream.u(1) == 1;
            if (vuiParameters.nal_hrd_parameters_present_flag)
            {
                // Hrd_Parameters.
                HrdParameters hrdParameters = H264Parsers.set_hrd_parameters(bitStream);
            }
            vuiParameters.vcl_hrd_parameters_present_flag = bitStream.u(1) == 1;
            if (vuiParameters.vcl_hrd_parameters_present_flag)
            {
                // Hrd_Parameters.
                HrdParameters hrdParameters = H264Parsers.set_hrd_parameters(bitStream);
            }
            if (vuiParameters.nal_hrd_parameters_present_flag || vuiParameters.vcl_hrd_parameters_present_flag)
            {
                vuiParameters.low_delay_hrd_flag = bitStream.u(1) == 1;
            }
            vuiParameters.pic_struct_present_flag = bitStream.u(1) == 1;
            vuiParameters.bitstream_restriction_flag = bitStream.u(1) == 1;
            if (vuiParameters.bitstream_restriction_flag)
            {
                vuiParameters.motion_vectors_over_pic_boundaries_flag = bitStream.u(1) == 1;
                vuiParameters.max_bytes_per_pic_denom = bitStream.ue();
                vuiParameters.max_bits_per_mb_denom = bitStream.ue();
                vuiParameters.log2_max_mv_length_horizontal = bitStream.ue();
                vuiParameters.log2_max_mv_length_vertical = bitStream.ue();
                vuiParameters.max_num_reorder_frames = bitStream.ue();
                vuiParameters.max_dec_frame_buffering = bitStream.ue();
            }

            var vuiParameter = new
            {
                aspect_ratio_info_present_flag = vuiParameters.aspect_ratio_info_present_flag,
                aspect_ratio_idc = vuiParameters.aspect_ratio_idc,
                sar_width = vuiParameters.sar_width,
                sar_height = vuiParameters.sar_height,
                chroma_loc_info_present_flag = vuiParameters.chroma_loc_info_present_flag,
                overscan_info_present_flag = vuiParameters.overscan_info_present_flag,
                video_signal_type_present_flag = vuiParameters.video_signal_type_present_flag,
                motion_vectors_over_pic_boundaries_flag = vuiParameters.motion_vectors_over_pic_boundaries_flag,
                max_bytes_per_pic_denom = vuiParameters.max_bytes_per_pic_denom,
                max_bits_per_mb_denom = vuiParameters.max_bits_per_mb_denom,
                log2_max_mv_length_horizontal = vuiParameters.log2_max_mv_length_horizontal,
                log2_max_mv_length_vertical = vuiParameters.log2_max_mv_length_vertical,
                max_num_reorder_frames = vuiParameters.max_num_reorder_frames,
                nal_hrd_parameters_present_flag = vuiParameters.nal_hrd_parameters_present_flag,
                timing_info_present_flag = vuiParameters.timing_info_present_flag,
                num_units_in_stick = vuiParameters.num_units_in_stick,
                time_scale = vuiParameters.time_scale,
                fixed_frame_rate_flag = vuiParameters.fixed_frame_rate_flag,
                pic_struct_present_flag = vuiParameters.pic_struct_present_flag,
                bitstream_restriction_flag = vuiParameters.bitstream_restriction_flag,
            };
            // Console.WriteLine(@"VuiParametersJson: {0}", JsonSerializer.Serialize(vuiParameters));
            return vuiParameters;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public PPS pic_parameter_set_rbsp(byte[] ppsBytes)
    {
        SettingSets? settingSets = settingsService.GetCodecSettings();
        SPS SPS = settingSets.GetSPS;
        PPS PPSUnit = settingSets.GetPPS;
        GlobalVariables globalVariables = settingSets.GlobalVariables;

        try
        {
            using (MemoryStream memoryStream = new MemoryStream(ppsBytes))
            {
                BitList bitStream = new BitList(ppsBytes);

                PPSUnit.pic_parameter_set_id = bitStream.ue();
                PPSUnit.seq_parameter_set_id = bitStream.ue();
                PPSUnit.entropy_coding_mode_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                PPSUnit.bottom_field_pic_order_in_frame_present_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                PPSUnit.num_slice_groups_minus1 = bitStream.ue();

                if (PPSUnit.num_slice_groups_minus1 > 0)
                {
                    PPSUnit.slice_group_map_type = bitStream.ue();
                    if (PPSUnit.slice_group_map_type == 0)
                    {
                        for (int iGroup = 0; iGroup < PPSUnit.num_slice_groups_minus1; iGroup++)
                        {
                            PPSUnit.run_length_minus1.Add(bitStream.ue());
                        }
                    }
                    else if (PPSUnit.slice_group_map_type == 2)
                    {
                        for (int iGroup = 0; iGroup <= PPSUnit.num_slice_groups_minus1; iGroup++)
                        {
                            PPSUnit.top_left.Add(bitStream.ue());
                            PPSUnit.bottom_right.Add(bitStream.ue());
                        }
                    }
                    else if (PPSUnit.slice_group_map_type == 3 ||
                            PPSUnit.slice_group_map_type == 4 ||
                            PPSUnit.slice_group_map_type == 5)
                    {
                        PPSUnit.slice_group_change_direction_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                        PPSUnit.slice_group_change_rate_minus1 = bitStream.ue();
                    }
                    else if (PPSUnit.slice_group_map_type == 6)
                    {
                        PPSUnit.pic_size_in_map_units_minus1 = bitStream.ue();
                        for (int i = 0; i <= PPSUnit.pic_size_in_map_units_minus1; i++)
                        {
                            PPSUnit.slice_group_id.Add(Convert.ToUInt32(bitStream.read_bits((uint)Math.Ceiling(Math.Log2(PPSUnit.num_slice_groups_minus1)))));
                        }
                    }
                }
                PPSUnit.num_ref_idx_l0_default_active_minus1 = bitStream.ue();
                PPSUnit.num_ref_idx_l1_default_active_minus1 = bitStream.ue();
                PPSUnit.weighted_pred_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                PPSUnit.weighted_bipred_idc = Convert.ToUInt32(bitStream.read_bits(2), 2);

                PPSUnit.pic_init_qp_minus26 = bitStream.se();
                PPSUnit.pic_init_qs_minus26 = bitStream.se();
                PPSUnit.chroma_qp_index_offset = bitStream.se();
                PPSUnit.deblocking_filter_control_present_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                PPSUnit.constrained_intra_pred_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                PPSUnit.redundant_pic_cnt_present_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;

                if (bitStream.more_rbsp_data())
                {
                    PPSUnit.transform_8x8_mode_flag = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                    PPSUnit.pic_scaling_matrix_present = Convert.ToUInt32(bitStream.read_bits(1), 2) == 1;
                    if (PPSUnit.pic_scaling_matrix_present)
                    {
                        for (int i = 0; i < (6 + ((SPS.chroma_format_idc != 3) ? 2 : 6) * Convert.ToInt32(PPSUnit.transform_8x8_mode_flag)); i++)
                        {
                            PPSUnit.pic_scaling_list_present_flag.Add(Convert.ToUInt32(bitStream.read_bits(1), 2) == 1);
                            if (PPSUnit.pic_scaling_list_present_flag[i])
                            {
                                if (i < 6)
                                {
                                    PPSUnit.scaling_list4x4[i] = new int[16];
                                    PPSUnit.scaling_list4x4.Add(bitStream.scaling_list(PPSUnit.scaling_list4x4[i], 16, PPSUnit.UseDefaultScaling4x4Flag[i]));
                                }
                                else
                                {
                                    PPSUnit.scaling_list8x8[i - 6] = new int[64];
                                    PPSUnit.scaling_list8x8.Add(bitStream.scaling_list(PPSUnit.scaling_list8x8[i - 6], 64, PPSUnit.UseDefaultScaling8x8Flag[i - 6]));
                                }
                            }
                        }
                    }
                    PPSUnit.second_chroma_qp_index_offset = bitStream.se();

                    var PPSJson = new
                    {
                        pic_parameter_set_id = PPSUnit.pic_parameter_set_id,
                        seq_parameter_set_id = PPSUnit.seq_parameter_set_id,
                        entropy_coding_mode_flag = PPSUnit.entropy_coding_mode_flag,
                        bottom_field_pic_order_in_frame_present_flag = PPSUnit.bottom_field_pic_order_in_frame_present_flag,
                        num_slice_groups_minus1 = PPSUnit.num_slice_groups_minus1,
                        num_ref_idx_l0_default_active_minus1 = PPSUnit.num_ref_idx_l0_default_active_minus1,
                        num_ref_idx_l1_default_active_minus1 = PPSUnit.num_ref_idx_l1_default_active_minus1,
                        weighted_pred_flag = PPSUnit.weighted_pred_flag,
                        weighted_bipred_idc = PPSUnit.weighted_bipred_idc,
                        pic_init_qp_minus26 = PPSUnit.pic_init_qp_minus26,
                        pic_init_qs_minus26 = PPSUnit.pic_init_qs_minus26,
                        chroma_qp_index_offset = PPSUnit.chroma_qp_index_offset,
                        deblocking_filter_control_present_flag = PPSUnit.deblocking_filter_control_present_flag,
                        constrained_intra_pred_flag = PPSUnit.constrained_intra_pred_flag,
                        redundant_pic_cnt_present_flag = PPSUnit.redundant_pic_cnt_present_flag,
                        transform_8x8_mode_flag = PPSUnit.transform_8x8_mode_flag,
                        pic_scaling_matrix_present = PPSUnit.pic_scaling_matrix_present,
                        second_chroma_qp_index_offset = PPSUnit.second_chroma_qp_index_offset

                    };
                    // Console.WriteLine("PPSJSON: {0}, Position: {1}", PPSJson.ToString(), bitStream.Position);
                }
                // bitStream.rbsp_trailing_bits();
            }
            settingsService.Update<PPS>(PPSUnit);
            return PPSUnit;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public SPS seq_parameter_set_rbsp(byte[] spsBytes)
    {
        using (MemoryStream memoryStream = new MemoryStream(spsBytes))
        {
            BitList bitStream = new BitList(spsBytes);
            SettingSets settingSets = settingsService.GetCodecSettings();
            SPS Sps = settingSets.GetSPS;
            GlobalVariables globalVariables = settingSets.GlobalVariables;

            Sps.profile_Idc = bitStream.u(8);
            uint constraint_value = bitStream.u(8);

            Sps.constraint_set0_flag = (constraint_value & 255) == 128;
            Sps.constraint_set1_flag = (constraint_value & 255) == 64;
            Sps.constraint_set2_flag = (constraint_value & 255) == 32;
            Sps.constraint_set3_flag = (constraint_value & 255) == 16;
            Sps.constraint_set4_flag = (constraint_value & 255) == 8;
            Sps.constraint_set5_flag = (constraint_value & 255) == 4;
            Sps.reserved_zero_2bits = constraint_value & 3;

            Sps.level_idc = bitStream.u(8);
            Sps.seq_parameter_set_id = bitStream.ue();

            if (Sps.profile_Idc == 100 || Sps.profile_Idc == 110 || Sps.profile_Idc == 122 ||
                Sps.profile_Idc == 244 || Sps.profile_Idc == 44 || Sps.profile_Idc == 83 ||
                Sps.profile_Idc == 86 || Sps.profile_Idc == 118 || Sps.profile_Idc == 138 ||
                Sps.profile_Idc == 139 || Sps.profile_Idc == 134 || Sps.profile_Idc == 135)
            {
                Sps.chroma_format_idc = bitStream.ue();
                if (Sps.chroma_format_idc == 3)
                {
                    Sps.separate_colour_plane = bitStream.u(1);
                }
                Sps.bit_depth_luma_minus8 = bitStream.ue();
                Sps.bit_depth_chroma_minus8 = bitStream.ue();
                Sps.qpprime_y_zero_transform_bypass_flag = bitStream.u(1);
                Sps.seq_scaling_matrix_present_flag = bitStream.u(1);

                if (Sps.seq_scaling_matrix_present_flag == 1)
                {
                    for (int i = 0; i < ((Sps.chroma_format_idc != 3) ? 8 : 12); i++)
                    {
                        Sps.seq_scaling_list_present_flag[i] = bitStream.u(1);
                        if (Sps.seq_scaling_list_present_flag[i] == 1)
                        {
                            if (i < 6)
                            {
                                Sps.scaling_list4x4[i] = new int[16];
                                bitStream.scaling_list(Sps.scaling_list4x4[i], 16, Sps.UseDefaultScaling4x4Flag[i]);
                            }
                            else
                            {
                                Sps.scaling_list8x8[i - 6] = new int[16];
                                bitStream.scaling_list(Sps.scaling_list8x8[i - 6], 64, Sps.UseDefaultScaling8x8Flag[i - 6]);
                            }
                        }
                    }
                }
            }
            Sps.log2_max_frame_num_minus4 = bitStream.ue();
            Sps.pic_order_cnt_type = bitStream.ue();

            if (Sps.pic_order_cnt_type == 0)
            {
                Sps.log2_max_pic_order_cnt_lsb_minus4 = bitStream.ue();
            }
            else if (Sps.pic_order_cnt_type == 1)
            {
                Sps.delta_pic_order_always_zero_flag = bitStream.u(1) == 1;
                // Sps.offset_for_non_ref_pic = memoryStream.se(memoryStream.ue());
            }
            Sps.max_num_ref_frames = bitStream.ue();
            Sps.gaps_in_frame_num_value_allowed_flag = bitStream.u(1);
            Sps.pic_width_in_mbs_minus1 = bitStream.ue();
            Sps.pic_height_in_map_units_minus1 = bitStream.ue();
            Sps.frame_mbs_only_flag = bitStream.u(1) == 1;

            if (!Sps.frame_mbs_only_flag)
            {
                Sps.mb_adaptive_frame_field_flag = bitStream.u(1);
            }
            Sps.direct_8x8_inference_flag = bitStream.u(1);
            Sps.frame_cropping_flag = bitStream.u(1);

            if (Sps.frame_cropping_flag == 1)
            {
                Sps.frame_crop_left_offset = bitStream.ue();
                Sps.frame_crop_right_offset = bitStream.ue();
                Sps.frame_crop_top_offset = bitStream.ue();
                Sps.fram_crop_bottom_offset = bitStream.ue();
            }
            Sps.vui_parameters_present_flag = bitStream.u(1);

            var sps = new
            {
                profile_idc = Sps.profile_Idc,
                constraint_set0_flag = Sps.constraint_set0_flag,
                constraint_set1_flag = Sps.constraint_set1_flag,
                constraint_set2_flag = Sps.constraint_set2_flag,
                constraint_set3_flag = Sps.constraint_set3_flag,
                constraint_set4_flag = Sps.constraint_set4_flag,
                constraint_set5_flag = Sps.constraint_set5_flag,
                level_idc = Sps.level_idc,
                reserved_zero_2bits = Sps.reserved_zero_2bits,
                seq_parameter_set_id = Sps.seq_parameter_set_id,
                log2_max_frame_num_minus4 = Sps.log2_max_frame_num_minus4,
                pic_order_cnt_type = Sps.pic_order_cnt_type,
                max_num_ref_frames = Sps.max_num_ref_frames,
                gaps_in_frame_num_value_allowed_flag = Sps.gaps_in_frame_num_value_allowed_flag,
                pic_width_in_mbs_minus1 = Sps.pic_width_in_mbs_minus1,
                pic_height_in_map_units_minus1 = Sps.pic_height_in_map_units_minus1,
                frame_mbs_only_flag = Sps.frame_mbs_only_flag,
                direct_8x8_inference_flag = Sps.direct_8x8_inference_flag,
                frame_cropping_flag = Sps.frame_cropping_flag,
                frame_crop_left_offset = Sps.frame_crop_left_offset,
                frame_crop_right_offset = Sps.frame_crop_right_offset,
                frame_crop_top_offset = Sps.frame_crop_top_offset,
                fram_crop_bottom_offset = Sps.fram_crop_bottom_offset,
                vui_parameters_present_flag = Sps.vui_parameters_present_flag
            };
            if (Sps.vui_parameters_present_flag == 1)
            {
                // Vui parameters.
                VuiParameters vuiParameters = H264Parsers.vui_parameters(bitStream);
            }
            globalVariables.BitDepthY = 8 + Sps.bit_depth_luma_minus8;
            globalVariables.QpBdOffsetY = 6 * Sps.bit_depth_luma_minus8;
            globalVariables.BitDepthC = 8 + Sps.bit_depth_chroma_minus8;
            globalVariables.QpBdOffsetC = 6 * Sps.bit_depth_chroma_minus8;
            globalVariables.RawMbBits = 256 * globalVariables.BitDepthY + 2 * globalVariables.MbWidthC * globalVariables.MbHeightC * globalVariables.BitDepthC;
            globalVariables.MaxFrameNum = (int)Math.Pow(2, Sps.log2_max_frame_num_minus4 + 4);
            globalVariables.MaxPicOrderCntLsb = (int)Math.Pow(2, Sps.log2_max_pic_order_cnt_lsb_minus4 + 4);
            globalVariables.PicWidthInSamplesC = globalVariables.PicWidthInMbs * globalVariables.MbWidthC;
            int ExpectedDeltaPerPicOrderCntCycle = 0;
            for (int i = 0; i < Sps.num_ref_frames_in_pic_order_cnt_cycle; i++)
            {
                ExpectedDeltaPerPicOrderCntCycle += (int)Sps.offset_for_ref_frames[i];
            }
            globalVariables.ExpectedDeltaPerPicOrderCntCycle = ExpectedDeltaPerPicOrderCntCycle;
            globalVariables.PicWidthInMbs = (int)Sps.pic_width_in_mbs_minus1 + 1;
            globalVariables.PicWidthInSamplesL = globalVariables.PicWidthInMbs * 16;
            globalVariables.PicWidthInSamplesC = globalVariables.PicHeightInMbs * globalVariables.MbWidthC;
            globalVariables.PicHeightInMapUnits = (int)Sps.pic_height_in_map_units_minus1 + 1;
            globalVariables.PicSizeInMapUnits = globalVariables.PicWidthInMbs * globalVariables.PicHeightInMapUnits;
            globalVariables.FrameHeightInMbs = (2 - (Sps.frame_mbs_only_flag ? 1 : 0)) * globalVariables.PicHeightInMapUnits;

            if (!(Sps.separate_colour_plane == 1))
            {
                globalVariables.ChromaArrayType = (ushort)Sps.chroma_format_idc;
            }else
            {
                globalVariables.ChromaArrayType = 0;
            }
            if (globalVariables.ChromaArrayType == 0)
            {
                globalVariables.CropUnitX = 1;
                globalVariables.CropUnitY = 2 - (Sps.frame_mbs_only_flag ? 1 : 0);
            }
            else
            {
                globalVariables.CropUnitX = (int)globalVariables.SubWidthC;
                globalVariables.CropUnitY = (int)globalVariables.SubHeightC * (2 - (Sps.frame_mbs_only_flag ? 1 : 0));
            }
            settingsService.Update<GlobalVariables>(globalVariables);
            settingsService.Update<SPS>(Sps);
            return Sps;
        }
    }

    public MacroblockLayer parse_macroblock_layer(BitList bitStream, SynElemSlice synElemSlice)
    {
        try
        {
            MacroblockLayer macroblockLayer = new MacroblockLayer();
            MicroblockTypes macroblockTypes = new MicroblockTypes(settingsService, synElemSlice);

            SettingSets settingSets = settingsService.GetCodecSettings();
            PPS Pps = settingSets.GetPPS;
            SPS Sps = settingSets.GetSPS;
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            SliceHeader sliceHeader = settingSets.SliceHeader;
            Extras extras = settingSets.Extras;

            MacroblockSlice macroblockSlice = new MacroblockSlice();
            bool noSubMbPartSizeLessThan8x8Flag = true;

            synElemSlice.settingSets = settingSets;

            macroblockLayer.mb_type = Pps.entropy_coding_mode_flag ? (uint)bitStream.ae(synElemSlice) : bitStream.ue();
            if (macroblockLayer.mb_type == (uint)MicroblockType.I_PCM)
            {
                while (!bitStream.byte_aligned())
                {

                }
                for (int i = 0; i < 256; i++)
                {
                    macroblockLayer.pcm_sample_luma[i] = (uint)i;
                }
            }
            else
            {
                noSubMbPartSizeLessThan8x8Flag = true;
                if (macroblockLayer.mb_type != (uint)MicroblockType.I_NxN &&
                macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0) != PredictionModes.Intra_16x16 &&
                macroblockTypes.NumMbPart(macroblockLayer.mb_type) == NumMbPartModes.Four)
                {
                    SubMbPredLayer subMbPredLayer = new SubMbPredLayer(macroblockLayer.mb_type).GetSubMbPredLayer();
                    SliceSubMacroblock BSliceSubMB = macroblockSlice.BSubMacroblock.First(b => b.SubMacroblockName == "B_Direct_8x8");
                    for (int mbPartIdx = 0; mbPartIdx < 4; mbPartIdx++)
                    {
                        if (subMbPredLayer.sub_mb_type[mbPartIdx] != BSliceSubMB.SubMbType)
                        {
                            if ((int)macroblockTypes.NumSubMbPart(subMbPredLayer.sub_mb_type[mbPartIdx]) > 1)
                            {
                                noSubMbPartSizeLessThan8x8Flag = false;
                            }
                        }
                        else if (!(Sps.direct_8x8_inference_flag == 1))
                        {
                            noSubMbPartSizeLessThan8x8Flag = false;
                        }
                    }
                }
                else
                {
                    if (Pps.transform_8x8_mode_flag && (macroblockLayer.mb_type == (uint)MicroblockType.I_NxN))
                    {
                        // Read transform_8x8_mode_flag from the stream.
                        macroblockLayer.transform_size_8x8_flag = Pps.entropy_coding_mode_flag ? bitStream.u(1) == 1 : bitStream.ae(synElemSlice) == 1;
                    }       
                    extras.MacroblockLayer = macroblockLayer;
                    settingsService.Update(extras);
                    parse_mb_pred(bitStream, macroblockLayer.mb_type, synElemSlice);
                }
                if (macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0) != PredictionModes.Intra_16x16)
                {
                    // Read the value of coded_block_pattern from the stream.
                    macroblockLayer.coded_block_pattern = bitStream.me();
                    List<I_SliceMicroblock> IsliceMacroblocks = macroblockTypes.ISliceTypeTable;
                    PredictionModes currentMacroblock = macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0);
                    var bsliceMacroblock = (from bslicemb in IsliceMacroblocks
                                            where bslicemb.MbType == macroblockLayer.mb_type &&
                                            macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0) == currentMacroblock
                                            select bslicemb).FirstOrDefault();
                    bsliceMacroblock = bsliceMacroblock != null ? bsliceMacroblock : new I_SliceMicroblock();
                    NumMbPartions? predictionPart = (from pp in bsliceMacroblock.NumMbPartions
                                                     where pp.NumberOfPart == 0
                                                     select pp).FirstOrDefault();
                    predictionPart = predictionPart != null ? predictionPart : new NumMbPartions();
                    PredictionModes predictionModes = predictionPart.PredictionModes;
                    
                    bsliceMacroblock.CodedBlockPatternLuma = (CodedBlockPatterLumaValue)(macroblockLayer.coded_block_pattern % 16);
                    bsliceMacroblock.CodedBlockPatternChroma = (CodedBlockPatternChromaValue)(macroblockLayer.coded_block_pattern / 16);
                    
                    globalVariables.CodedBlockPatternLuma = (ushort)bsliceMacroblock.CodedBlockPatternLuma;
                    globalVariables.CodedBlockPatternChroma = (ushort)bsliceMacroblock.CodedBlockPatternChroma;
                    settingsService.Update(globalVariables);

                    if (globalVariables.CodedBlockPatternLuma > 0 &&
                    Pps.transform_8x8_mode_flag &&
                    noSubMbPartSizeLessThan8x8Flag &&
                    (predictionModes != PredictionModes.Intra_16x16))
                    {
                        // Read coded_block_pattern from the stream.
                        if (globalVariables.CodedBlockPatternLuma > 0 &&
                        Pps.transform_8x8_mode_flag &&
                        bsliceMacroblock.NameOfMb != "I_NxN" &&
                        (bsliceMacroblock.NameOfMb != "B_Direct_16x16" || (Sps.direct_8x8_inference_flag == 1)))
                        {
                            // Read transform_size_8x8_flag from the stream.
                        }
                    }
                    if (globalVariables.CodedBlockPatternLuma > 0 || globalVariables.CodedBlockPatternChroma > 0
                    || macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0) == PredictionModes.Intra_16x16)
                    {
                        // Read the mb_qp_delta
                        macroblockLayer.mb_qp_delta = Pps.entropy_coding_mode_flag ? (uint)bitStream.ae() : (uint)bitStream.se();
                        extras.MacroblockLayer = macroblockLayer;
                        settingsService.Update(extras);
                        parse_residual(bitStream, 0, 15);
                        
                        // Residual(0, 15);
                    }
                }                
            }
            return macroblockLayer;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public Residual parse_residual(BitList bitStream, int startIdx, int endIdx)
    {
        try
        {
            SettingSets settingSets = settingsService.GetCodecSettings();
            ICoefficients ResidualBlock;
            PPS Pps = settingSets.GetPPS;
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            SPS Sps = settingSets.GetSPS;
            ResidualLuma residualLuma;
            Residual residual = new Residual();

            int[] I16x16DCLevel = new int[16];
            int[,] I16x16ACLevel = new int[16, 15];
            int[,] Level4x4 = new int[16, 16];
            int[,] Level8x8 = new int[4, 16];

            if (!Pps.entropy_coding_mode_flag)
            {
                ResidualBlockCAVLC residualBlockCAVLC = new ResidualBlockCAVLC(bitStream);
                ResidualBlock = residualBlockCAVLC;
                residualLuma = new ResidualLuma(residualBlockCAVLC, settingsService);                
            } else
            {
                ResidualBlockCabac residualBlockCabac = new ResidualBlockCabac(bitStream);
                ResidualBlock = residualBlockCabac;
                residualLuma = new ResidualLuma(residualBlockCabac, settingsService);                
            }
            residualLuma.GetResidualLuma(out I16x16DCLevel, out I16x16ACLevel, out Level4x4, out Level8x8, startIdx, endIdx);
            residual.Intra16x16DCLevel = I16x16DCLevel;
            residual.Intra16x16ACLevel = I16x16ACLevel;
            residual.LumaLevel4x4 = Level4x4;
            residual.LumaLevel8x8 = Level8x8;

            using (StreamReader streamReader = new StreamReader(@"Data\SliceMicroblockTables.json"))
           {
                string jsonChromaString = streamReader.ReadToEnd();
                List<ChromaFormat>? chromaFormats = JsonSerializer.Deserialize<List<ChromaFormat>>(jsonChromaString);
                ChromaFormat? chromaFormat = (from cf in chromaFormats
                                            where (uint)cf.ChromaFormatIdc == Sps.chroma_format_idc &&
                                            cf.SeparateColorPlaneFlag == Sps.separate_colour_plane
                                            select cf).FirstOrDefault();
                chromaFormat = chromaFormat != null ? chromaFormat : new ChromaFormat();
                globalVariables.NumC8x8 = (int)(4 / ((uint)chromaFormat.SubWidthC * (uint)chromaFormat.SubHeightC));

                if (globalVariables.ChromaArrayType == 1 || globalVariables.ChromaArrayType == 2)
                {                    
                    residual.ChromaDCLevel = new int[2, 4 * globalVariables.NumC8x8];
                    residual.ChromaACLevel = new int[2, globalVariables.NumC8x8, 15];

                    for (int iCbCr = 0; iCbCr < 2; iCbCr++)
                    {                                                
                        if ((globalVariables.CodedBlockPatternChroma & 3) > 0 && (startIdx == 0))
                        {
                            /* Chroma DC residual present */
                            int[] tempCoefficients = new int[4 * globalVariables.NumC8x8];
                            int[] coefficients = ResidualBlock.GetCoefficients(tempCoefficients, 0, 
                            4 * globalVariables.NumC8x8 - 1, 4 * globalVariables.NumC8x8);
                            residual.ChromaDCLevel = h264Array.Copy2DArray(residual.ChromaDCLevel, iCbCr, coefficients, 0, coefficients.Length);                            
                        }
                        else
                        {
                            for (int i = 0; i < 4 * globalVariables.NumC8x8; i++)
                            {
                                residual.ChromaDCLevel[iCbCr, i] = 0;
                            }
                        }
                    }

                    for (int iCbCr = 0; iCbCr < 2; iCbCr++)
                    {
                        for (int i8x8 = 0; i8x8 < globalVariables.NumC8x8; i8x8++)
                        {
                            for (int i4x4 = 0; i4x4 < 4; i4x4++)
                            {
                                if ((globalVariables.CodedBlockPatternChroma & 2) > 0)
                                {
                                    /* Chroma AC residual present */
                                    int[] tempCoefficient = new int[15];
                                    int[] coefficients = ResidualBlock.GetCoefficients(tempCoefficient, 
                                    Math.Max(0, startIdx - 1), endIdx - 1, 15);
                                    residual.ChromaACLevel = h264Array.Copy3DArray(residual.ChromaACLevel, iCbCr, i8x8, coefficients, 0, coefficients.Length);
                                }
                                else
                                {
                                    for (int i = 0; i < 15; i++)
                                    {
                                        residual.ChromaACLevel[iCbCr, i8x8 * 4 + i4x4, i] = 0;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (globalVariables.ChromaArrayType == 3  )
                {
                    residualLuma.GetResidualLuma(out I16x16DCLevel, out I16x16ACLevel, out Level4x4, out Level8x8, startIdx, endIdx);
                    residual.CbIntra16x16DCLevel = I16x16DCLevel;
                    residual.CbIntra16x16ACLevel = I16x16ACLevel;
                    residual.CbLevel4x4 = Level4x4;
                    residual.CbLevel8x8 = Level8x8;

                    residualLuma.GetResidualLuma(out I16x16DCLevel, out I16x16ACLevel, out Level4x4, out Level8x8, startIdx, endIdx);
                    residual.CrIntra16x16DCLevel = I16x16DCLevel;
                    residual.CrIntra16x16ACLevel = I16x16ACLevel;
                    residual.CrLevel4x4 = Level4x4;
                    residual.CrLevel8x8 = Level8x8;
                }                
           }
            return residual;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public SliceData get_slice_data(BitList bitStream, SliceHeader sliceHeader)
    {
        try
        {
            SliceData sliceData = new SliceData();
            SettingSets codecSettings = settingsService.GetCodecSettings();
            GlobalVariables globalVariables = codecSettings.GlobalVariables;

            GlobalFunctions globalFunctions = new GlobalFunctions(settingsService, sliceHeader);

            PPS Pps = codecSettings.GetPPS;

            bool moreDataFlag = true;
            bool prevMbSkipped = false;
            if (Pps.entropy_coding_mode_flag)
            {
                while (!SyntaxFunction.byte_aligned(bitStream))
                {
                    sliceData.cabac_alignment_one_bit = bitStream.f(1);
                }
            }
            globalVariables.CurrMbAddr = (int)sliceHeader.first_mb_in_slice * (1 + globalVariables.MbaffFrameFlag);
            SynElemSlice synElemSlice = new SynElemSlice();
            synElemSlice.Slicetype = sliceHeader.slice_type;
            do
            {
                if (sliceHeader.slice_type != Slicetype.I && sliceHeader.slice_type != Slicetype.SI)
                {
                    if (!Pps.entropy_coding_mode_flag)
                    {
                        sliceData.mb_skip_run = bitStream.ue();
                        prevMbSkipped = sliceData.mb_skip_run > 0;
                        for (int i = 0; i < sliceData.mb_skip_run; i++)
                        {
                            globalVariables.CurrMbAddr = globalFunctions.NextMbAddress(globalVariables.CurrMbAddr);
                        }
                        if (sliceData.mb_skip_run > 0)
                        {
                            moreDataFlag = bitStream.more_rbsp_data();
                        }
                    }
                    else
                    {
                        synElemSlice.SynElement = SynElement.mb_skip_flag;
                        sliceData.mb_skip_flag = bitStream.ae(synElemSlice) == 1;
                        moreDataFlag = !sliceData.mb_skip_flag;
                    }
                }
                if (moreDataFlag)
                {
                    if (globalVariables.MbaffFrameFlag == 1 && (globalVariables.CurrMbAddr % 2 == 0 ||
                    (globalVariables.CurrMbAddr % 2 == 1 && prevMbSkipped)))
                    {
                        sliceData.mb_field_decoding_flag = Pps.entropy_coding_mode_flag ? bitStream.ae() == 1 : bitStream.u(1) == 1;
                    }
                    settingsService.Update<SliceData>(sliceData);
                    parse_macroblock_layer(bitStream, synElemSlice);
                }
                if (!Pps.entropy_coding_mode_flag)
                {
                    moreDataFlag = bitStream.more_rbsp_data();
                }
                else
                {
                    if (sliceHeader.slice_type != Slicetype.I && sliceHeader.slice_type != Slicetype.SI)
                    {
                        prevMbSkipped = sliceData.mb_skip_flag;
                    }
                    if (globalVariables.MbaffFrameFlag == 1 && globalVariables.CurrMbAddr % 2 == 0)
                    {
                        moreDataFlag = true;
                    }
                    else
                    {
                        sliceData.end_of_slice_flag = bitStream.ae() == 1;
                        moreDataFlag = !sliceData.end_of_slice_flag;
                    }
                }
                globalVariables.CurrMbAddr = globalFunctions.NextMbAddress(globalVariables.CurrMbAddr);
            } while (moreDataFlag);
            settingsService.Update<GlobalVariables>(globalVariables);
            return sliceData;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public MbPred parse_mb_pred(BitList bitStream, uint mb_type, SynElemSlice synElemSlice)
    {
        try
        {
            MicroblockTypes macroblockTypes = new MicroblockTypes(settingsService, synElemSlice);
            SettingSets settingSets = settingsService.GetCodecSettings();
            PPS Pps = settingSets.GetPPS;
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            SliceHeader sliceHeader = settingSets.SliceHeader;
            SliceData sliceData = settingSets.SliceData;           

            MbPred MbPred = new MbPred();

            if (macroblockTypes.MbPartPredMode(mb_type, 0) == PredictionModes.Intra_4x4 ||
                macroblockTypes.MbPartPredMode(mb_type, 0) == PredictionModes.Intra_8x8 ||
                macroblockTypes.MbPartPredMode(mb_type, 0) == PredictionModes.Intra_16x16)
            {
                if (macroblockTypes.MbPartPredMode(mb_type, 0) == PredictionModes.Intra_4x4)
                {
                    MbPred.rem_intra4x4_pred_mode = new List<int>();
                    for (int luma4x4BlkIdx = 0; luma4x4BlkIdx < 16; luma4x4BlkIdx++)
                    {
                        MbPred.prev_intra4x4_pred_mode_flag[luma4x4BlkIdx] =  Pps.entropy_coding_mode_flag ? bitStream.ae() == 1: bitStream.u(1) == 1;
                        if (!MbPred.prev_intra4x4_pred_mode_flag[luma4x4BlkIdx])
                        {
                            MbPred.rem_intra4x4_pred_mode.Add(Pps.entropy_coding_mode_flag ? bitStream.ae(): (int)bitStream.u(3));
                        }
                    }
                    if (globalVariables.ChromaArrayType == 1 || globalVariables.ChromaArrayType == 2)
                    {
                        MbPred.intra_chroma_pred_mode = Pps.entropy_coding_mode_flag ? bitStream.ae() : (int)bitStream.ue();
                    }
                }
            } else if (macroblockTypes.MbPartPredMode(mb_type, 0) != PredictionModes.Direct)
            {
                for (int mbPartIdx = 0; mbPartIdx < (int)macroblockTypes.NumMbPart(mb_type); mbPartIdx++)
                {
                    if ((sliceHeader.num_ref_idx_l0_active_minus1 > 0)
                        || sliceData.mb_field_decoding_flag != sliceHeader.field_pic_flag
                        && macroblockTypes.MbPartPredMode(mb_type, (uint)mbPartIdx) != PredictionModes.Pred_L0)
                    {
                        int maxRangeX = globalVariables.MbaffFrameFlag == 1 || !sliceData.mb_field_decoding_flag ? (int)sliceHeader.num_ref_idx_l0_active_minus1 : (int)(2 * sliceHeader.num_ref_idx_l0_active_minus1 + 1);
                        MbPred.ref_idx_l0[mbPartIdx] = Pps.entropy_coding_mode_flag ? bitStream.ae() : bitStream.te(maxRangeX);
                    }
                }
                MbPred.mvd_l0 = new int[(int)macroblockTypes.NumMbPart(mb_type), 1, 2];
                for (int mbPartIdx = 0; mbPartIdx < (int)macroblockTypes.NumMbPart(mb_type); mbPartIdx++)
                {
                    if (macroblockTypes.MbPartPredMode(mb_type, (uint)mbPartIdx) != PredictionModes.Pred_L1)
                    {                       
                        for (int compIdx = 0; compIdx < 2; compIdx++)
                        {
                            MbPred.mvd_l0[mbPartIdx, 0, compIdx] = Pps.entropy_coding_mode_flag ? bitStream.ae() : bitStream.se();
                        }
                    }
                }
                MbPred.mvd_l1 = new int[(int)macroblockTypes.NumMbPart(mb_type), 1, 2];
                for (int mbPartIdx = 0; mbPartIdx < (int)macroblockTypes.NumMbPart(mb_type); mbPartIdx++)
                {
                    if (macroblockTypes.MbPartPredMode(mb_type, (uint)mbPartIdx) != PredictionModes.Pred_L0)
                    {
                        for (int compIdx = 0; compIdx < 2; compIdx++)
                        {
                            MbPred.mvd_l1[mbPartIdx, 0, compIdx] = Pps.entropy_coding_mode_flag ? bitStream.ae() : bitStream.se();
                        }
                    }
                }
            }
            return MbPred;
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}