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
using System.Text.RegularExpressions;
using System.Drawing;
using MathExtensionMethods;
using MbAddressLocations;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

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
            globalVariables.PicWidthInMbs = (int)Sps.pic_width_in_mbs_minus1 + 1;
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
            
            using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\ChromaQuntization.json"))
            {
                ChromaQParameter? cQparameters = JsonSerializer.Deserialize<ChromaQParameter>(streamReader.ReadToEnd());
                cQparameters = cQparameters != null ? cQparameters : throw new Exception();
                globalVariables.Qparameters = cQparameters;
            }
            codecSettings.GlobalVariables = globalVariables;
            settingsService.Update<GlobalVariables>(globalVariables);
            settingsService.Update<SliceHeader>(sliceHeader);

            Extras extras = settingsService.GetCodecSettings().Extras;
            List<MbAddress> MbAddresses = new List<MbAddress>();
            extras.MbAddresses = MbAddresses;
            settingsService.Update<Extras>(extras);

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
                        int ListCounter = (6 + ((SPS.chroma_format_idc != 3) ? 2 : 6) * Convert.ToInt32(PPSUnit.transform_8x8_mode_flag));
                        int[,] ScalingList4x4 = new int[ListCounter, 16];
                        int[,] ScalingList8x8 = new int[ListCounter, 64];

                        for (int i = 0; i < ListCounter; i++)
                        {
                            PPSUnit.pic_scaling_list_present_flag.Add(Convert.ToUInt32(bitStream.read_bits(1), 2) == 1);
                            if (PPSUnit.pic_scaling_list_present_flag[i])
                            {
                                if (i < 6)
                                {
                                    ScalingList4x4 = bitStream.scaling_list(ScalingList4x4, i, 16, PPSUnit.UseDefaultScaling4x4Flag[i]);
                                    globalVariables.ScalingList4x4 = Serializable2DArray(ScalingList4x4);
                                }
                                else
                                {
                                    ScalingList8x8 = bitStream.scaling_list(ScalingList8x8, i - 6, 64, PPSUnit.UseDefaultScaling8x8Flag[i - 6]);
                                    globalVariables.ScalingList8x8 = Serializable2DArray(ScalingList8x8);
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

            globalVariables.ScalingList4x4 = new List<Serializable2DArray>();
            globalVariables.ScalingList8x8 = new List<Serializable2DArray>();

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
                    int[,] ScalingList4x4 = new int[6, 16];
                    int[,] ScalingList8x8 = new int[6, 64];
                    for (int i = 0; i < ((Sps.chroma_format_idc != 3) ? 8 : 12); i++)
                    {
                        Sps.seq_scaling_list_present_flag[i] = bitStream.u(1);
                        if (Sps.seq_scaling_list_present_flag[i] == 1)
                        {
                            if (i < 6)
                            {
                                Sps.scaling_list4x4[i] = new int[16];
                                ScalingList4x4 = bitStream.scaling_list(ScalingList4x4, i, 16, Sps.UseDefaultScaling4x4Flag[i]);
                                globalVariables.ScalingList4x4 = Serializable2DArray(ScalingList4x4);
                            }
                            else
                            {
                                Sps.scaling_list8x8[i - 6] = new int[16];
                                ScalingList8x8 = bitStream.scaling_list(ScalingList8x8, i - 6, 64, Sps.UseDefaultScaling8x8Flag[i - 6]);
                                globalVariables.ScalingList8x8 = Serializable2DArray(ScalingList4x4);
                            }
                        }
                    }
                } else
                {
                    int[] Flat_4x4_16 = new int[16];
                    int[] Flat_8x8_16 = new int[64];

                    int ListCount = Sps.chroma_format_idc != 3 ? 8 : 12;
                    for (int scaleListIndex = 0; scaleListIndex < ListCount; scaleListIndex++)
                    {
                        Serializable2DArray serializable2DArray = new Serializable2DArray();
                        serializable2DArray.IntIdx = scaleListIndex;
                        if (scaleListIndex < 6)
                        {                            
                            for (int scaleIndex = 0; scaleIndex < 16; scaleIndex++)
                            {                                
                                serializable2DArray.Ints[scaleIndex] = 16;
                            }      
                            globalVariables.ScalingList4x4.Add(serializable2DArray);                   
                        } else
                        {
                            for (int scaleIndex = 0; scaleIndex < 64; scaleIndex++)
                            {
                                serializable2DArray.Ints[scaleIndex] = 16;                                
                            }    
                            globalVariables.ScalingList8x8.Add(serializable2DArray);                        
                        }
                    }
                }
            } else
            {
                int[] Flat_4x4_16 = new int[16];
                int[] Flat_8x8_16 = new int[64];

                int ListCount = Sps.chroma_format_idc != 3 ? 8 : 12;
                for (int scaleListIndex = 0; scaleListIndex < ListCount; scaleListIndex++)
                {
                    Serializable2DArray serializable2DArray = new Serializable2DArray();
                    serializable2DArray.IntIdx = scaleListIndex;                   

                    if (scaleListIndex < 6)
                    {
                        serializable2DArray.Ints = new int[16];
                        for (int scaleIndex = 0; scaleIndex < 16; scaleIndex++)
                        {
                            serializable2DArray.Ints[scaleIndex] = 16;
                        }
                        globalVariables.ScalingList4x4.Add(serializable2DArray);
                    }
                    else
                    {
                        serializable2DArray.Ints = new int[64];
                        for (int scaleIndex = 0; scaleIndex < 64; scaleIndex++)
                        {
                            serializable2DArray.Ints[scaleIndex] = 16;
                        }
                        globalVariables.ScalingList8x8.Add(serializable2DArray);
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
            } else
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
            SliceMicroblock? SliceMb = macroblockTypes.GetMbType(macroblockLayer.mb_type);
            List<MbAddress> MbAddresses = extras.MbAddresses;
            MbAddress? MbAddress = MbAddresses.Where(mbA => mbA.Address == globalVariables.CurrMbAddr).FirstOrDefault();
            MbAddress = MbAddress != null ? MbAddress : new MbAddress();
            MbAddress.Address = globalVariables.CurrMbAddr;
            MbAddress.FrameNum = (int)sliceHeader.frame_num;
            MbAddress.MbType = (int)macroblockLayer.mb_type;
            MbAddress.SliceType = (int)sliceHeader.slice_type;           

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
                    MbPred MbPred = parse_mb_pred(bitStream, macroblockLayer.mb_type, synElemSlice);
                    MbAddress.IntraMbPredMode = MbPred;
                    settingsService.Update(extras);                       
                }
                PredictionModes predictionModes = PredictionModes.Invalid;
                I_SliceMicroblock? bsliceMacroblock = new I_SliceMicroblock();
                if (macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0) != PredictionModes.Intra_16x16)
                {
                    // Read the value of coded_block_pattern from the stream.
                    macroblockLayer.coded_block_pattern = bitStream.me();
                    List<I_SliceMicroblock> IsliceMacroblocks = macroblockTypes.ISliceTypeTable;
                    PredictionModes currentMacroblock = macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0);
                    bsliceMacroblock = (from bslicemb in IsliceMacroblocks
                                            where bslicemb.MbType == macroblockLayer.mb_type &&
                                            macroblockTypes.MbPartPredMode(macroblockLayer.mb_type, 0) == currentMacroblock
                                            select bslicemb).FirstOrDefault();
                    bsliceMacroblock = bsliceMacroblock != null ? bsliceMacroblock : throw new Exception();
                    NumMbPartions? predictionPart = (from pp in bsliceMacroblock.NumMbPartions
                                                     where pp.NumberOfPart == 0
                                                     select pp).FirstOrDefault();
                    predictionPart = predictionPart != null ? predictionPart : throw new Exception();
                    predictionModes = predictionPart.PredictionModes;
                    
                    bsliceMacroblock.CodedBlockPatternLuma = (CodedBlockPatterLumaValue)(macroblockLayer.coded_block_pattern % 16);
                    bsliceMacroblock.CodedBlockPatternChroma = (CodedBlockPatternChromaValue)(macroblockLayer.coded_block_pattern / 16);
                    
                    globalVariables.CodedBlockPatternLuma = (ushort)bsliceMacroblock.CodedBlockPatternLuma;
                    globalVariables.CodedBlockPatternChroma = (ushort)bsliceMacroblock.CodedBlockPatternChroma;
                    settingsService.Update(globalVariables);
                }
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

                    globalVariables.QPY = ((globalVariables.QPYprev + macroblockLayer.mb_qp_delta + 52 + 2 * globalVariables.QpBdOffsetY) %
                    (52 + globalVariables.QpBdOffsetY)) - globalVariables.QpBdOffsetY;
                    globalVariables.QPprimeY = globalVariables.QPY + globalVariables.QpBdOffsetY;
                    if (Sps.qpprime_y_zero_transform_bypass_flag == 1 && (globalVariables.QPprimeY == 0))
                    {
                        globalVariables.TransformBypassModeFlag = true;
                    }
                    else
                    {
                        globalVariables.TransformBypassModeFlag = false;
                    }
                    extras.MacroblockLayer = macroblockLayer;
                    settingsService.Update(extras);
                    settingsService.Update(globalVariables);
                    parse_residual(bitStream, 0, 15);

                    // Residual(0, 15);
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
            GlobalFunctions globalFunctions = new GlobalFunctions();
            Extras extras = settingSets.Extras;
            MbAddress? CurrMbAddress = (from mbA in extras.MbAddresses
                                        where mbA.Address == globalVariables.CurrMbAddr
                                        select mbA).FirstOrDefault();
            CurrMbAddress = CurrMbAddress != null ? CurrMbAddress : throw new Exception();
            
            int[] I16x16DCLevel = new int[16];
            int[,] I16x16ACLevel = new int[16, 15];
            int[,] Level4x4 = new int[16, 16];
            int[,] Level8x8 = new int[4, 64];

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

            CurrMbAddress.IntraDCLevels = residual.Intra16x16DCLevel;
            CurrMbAddress.ChromaACLevels = Serializable2DArray(residual.Intra16x16ACLevel);
            CurrMbAddress.LumaLevels4x4 = Serializable2DArray(residual.LumaLevel4x4);
            CurrMbAddress.LumaLevels8x8 = Serializable2DArray(residual.LumaLevel8x8);

            using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\SliceMicroblockTables.json"))
           {
                string jsonChromaString = streamReader.ReadToEnd();
                SliceTypeMicroblock? sliceTypeMacroblocks = JsonSerializer.Deserialize<SliceTypeMicroblock>(jsonChromaString);
                sliceTypeMacroblocks = sliceTypeMacroblocks != null ? sliceTypeMacroblocks : new SliceTypeMicroblock();
                List<ChromaFormat> chromaFormats = sliceTypeMacroblocks.ChromaWidthHeight;
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
                else if (globalVariables.ChromaArrayType == 3)
                {
                    residualLuma.GetResidualLuma(out I16x16DCLevel, out I16x16ACLevel, out Level4x4, out Level8x8, startIdx, endIdx);
                    residual.CbIntra16x16DCLevel = I16x16DCLevel;
                    residual.CbIntra16x16ACLevel = I16x16ACLevel;
                    residual.CbLevel4x4 = Level4x4;
                    residual.CbLevel8x8 = Level8x8;

                    CurrMbAddress.CbIntra16x16DCLevel = residual.CbIntra16x16DCLevel;
                    CurrMbAddress.CbIntra16x16ACLevel = Serializable2DArray(residual.CbIntra16x16ACLevel);
                    CurrMbAddress.CbLevel4x4 = Serializable2DArray(residual.CbLevel4x4);
                    CurrMbAddress.CbLevel8x8 = Serializable2DArray(residual.CbLevel8x8);

                    residualLuma.GetResidualLuma(out I16x16DCLevel, out I16x16ACLevel, out Level4x4, out Level8x8, startIdx, endIdx);
                    residual.CrIntra16x16DCLevel = I16x16DCLevel;
                    residual.CrIntra16x16ACLevel = I16x16ACLevel;
                    residual.CrLevel4x4 = Level4x4;
                    residual.CrLevel8x8 = Level8x8;

                    CurrMbAddress.CrIntra16x16DCLevel = residual.CrIntra16x16DCLevel;
                    CurrMbAddress.CrIntra16x16ACLevel = Serializable2DArray(residual.CrIntra16x16ACLevel);
                    CurrMbAddress.CrLevel4x4 = Serializable2DArray(residual.CrLevel4x4);
                    CurrMbAddress.CrLevel8x8 = Serializable2DArray(residual.CrLevel8x8);
                }                
           }
           settingsService.Update(extras);
           return residual;
        }
        catch (System.Exception ex)
        {            
            throw;
        }
    }

    private List<Serializable2DArray> Serializable2DArray(int[,] Source2DArray)
    {
        try
        {
            List<Serializable2DArray> serializable2DArrays = new List<Serializable2DArray>();
            for (int row = 0; row < Source2DArray.GetLength(0); row++)
            {
                Serializable2DArray serializable2DArray = new Serializable2DArray();
                serializable2DArray.IntIdx = row;
                serializable2DArray.Ints = new int[Source2DArray.GetLength(1)];
                for (int col = 0; col < Source2DArray.GetLength(1); col++)
                {
                    serializable2DArray.Ints[col] = Source2DArray[row, col];
                }
                serializable2DArrays.Add(serializable2DArray);
            }
            return serializable2DArrays;
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
            Extras extras = codecSettings.Extras;

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
                    MbAddress? MbAddress = new MbAddress();
                    MbAddress.Address = globalVariables.CurrMbAddr;
                    MbAddress.FrameNum = (int)sliceHeader.frame_num; 
                    MbAddress.SliceType = (int)sliceHeader.slice_type;
                    
                    bool IsFirstBool = MbAddress.Address == 0;
                    globalVariables.QPYprev = IsFirstBool ? sliceHeader.slice_qp_delta : MbAddress.QP;
                    extras.MbAddresses.Add(MbAddress);
                    settingsService.Update<GlobalVariables>(globalVariables);
                    settingsService.Update<Extras>(extras);
                    parse_macroblock_layer(bitStream, synElemSlice);

                    // Decode Transformed Coefficients
                    TransCoeffDec(MbAddress);
                    codecSettings = settingsService.GetCodecSettings();
                    extras = codecSettings.Extras;
                    MbAddress = extras.MbAddresses.Where(mb => mb.Address == MbAddress.Address).FirstOrDefault();
                    MbAddress = MbAddress != null ? MbAddress : throw new Exception();
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
        catch (System.Exception ex)
        {
            throw;
        }
    }

    private void TransCoeffDec(MbAddress CurrMb)
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            SliceHeader sliceHeader = settingSets.SliceHeader;
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            SynElemSlice synElemSlice = new SynElemSlice();
            synElemSlice.Slicetype = sliceHeader.slice_type;
            synElemSlice.SynElement = SynElement.mb_skip_flag;

            MicroblockTypes macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);
            PredictionModes predictionModes = macroblockTypes.MbPartPredMode((uint)CurrMb.MbType, 0);
            int[,] c = new int[4, 4], u;
            int[,] r = new int[4, 4];

            if (predictionModes == PredictionModes.Intra_4x4)
            {
                u = Res4x4DecProc(CurrMb);
            } else if (predictionModes == PredictionModes.Intra_8x8)
            {
                u = Res8x8DecProc(CurrMb);
            }
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] Res8x8DecProc(MbAddress currMb)
    {
        try
        {
            return new int[4, 4];
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] Res4x4DecProc(MbAddress CurrAddr)
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            SliceHeader sliceHeader = settingSets.SliceHeader;

            SynElemSlice synElemSlice = new SynElemSlice();
            synElemSlice.Slicetype = sliceHeader.slice_type;
            synElemSlice.SynElement = SynElement.mb_skip_flag;

            MicroblockTypes macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);
            int[,] c, r, u = new int[4, 4];
            MbAddressComputation mbAddressComputation = new MbAddressComputation();
            ResSampleSource resSampleSource = new ResSampleSource();
            int[,] LumaLevels4x4 = DeserializeTo2DArray(CurrAddr.LumaLevels4x4);
            for (int Luma4x4BlkIdx = 0; Luma4x4BlkIdx < LumaLevels4x4.GetLength(0); Luma4x4BlkIdx++)
            {

                c = InverseScanning4x4(LumaLevels4x4, Luma4x4BlkIdx);
                resSampleSource = new ResSampleSource();
                resSampleSource.Cols = 4;
                resSampleSource.Rows = 4;
                resSampleSource.Luma4x4BlkIdx = Luma4x4BlkIdx;
                resSampleSource.U = c;
                r = ScalingAndTransResidual4x4(resSampleSource);

                Intra4x4PredModes predModes = Intra4x4PredMode(Luma4x4BlkIdx);
                if (globalVariables.TransformBypassModeFlag && 
                    macroblockTypes.MbPartPredMode((uint)CurrAddr.MbType, 0) == PredictionModes.Intra_4x4 &&
                    (predModes == Intra4x4PredModes.Intra4x4Vertical || 
                     predModes == Intra4x4PredModes.Intra4x4Horizontal))
                {
                    int nW = 4, nH = 4;
                    r = IResTransBypassDec(nW, nH, predModes, r);                    
                }
                Point UpperLeftLuma = mbAddressComputation.Get4x4LumaLocation(Luma4x4BlkIdx);

                int[,] pred4x4L = pred4x4Y(Luma4x4BlkIdx);
                int[,] predL = GetPredY(pred4x4L, UpperLeftLuma);
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        u[row, col] = Mathematics.Clip1Y(predL[UpperLeftLuma.X + col, UpperLeftLuma.Y + row] + r[row, col], (int)globalVariables.BitDepthY);
                    }
                }
                resSampleSource = new ResSampleSource();
                resSampleSource.U = u;
                resSampleSource.SampleType = SampleType.Y;
                resSampleSource.Rows = 4;
                resSampleSource.Cols = 4;
                resSampleSource.Luma4x4BlkIdx = Luma4x4BlkIdx;
                ConPicPrioDiBlPicFil(resSampleSource);
            }
            return u;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] GetPredY(int[,] pred4x4L, Point UpperLeftLoc)
    {
        try
        {
            int[,] predY = new int[UpperLeftLoc.X + pred4x4L.GetLength(0), 
            UpperLeftLoc.Y + pred4x4L.GetLength(1)];

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    predY[UpperLeftLoc.X + row, UpperLeftLoc.Y + col] = pred4x4L[row, col];
                }
            }
            return predY;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] DeserializeTo2DArray(List<Serializable2DArray> lumaLevels4x4)
    {
        try
        {
            int MaxBlkIdx = lumaLevels4x4.Max(blkIdx => blkIdx.IntIdx);
            int[,] LumaLevel4x4 = new int[16, 16];            
            for (int row = 0; row <= MaxBlkIdx; row++)
            {
                Serializable2DArray? serializable2DArray = (from lLevel in lumaLevels4x4
                                                            where lLevel.IntIdx == row
                                                            select lLevel).FirstOrDefault();
                serializable2DArray = serializable2DArray != null ? serializable2DArray : throw new Exception();
                
                for (int col = 0; col < serializable2DArray.Ints.Length; col++)
                {
                    LumaLevel4x4[row, col] = serializable2DArray.Ints[col];
                }
            }
            return LumaLevel4x4;
        }
        catch (System.Exception)
        {
            
            throw;
        }
    }

    public void ConPicPrioDiBlPicFil(ResSampleSource resSampleSource)
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            MbAddressComputation mbAddressComputation = new MbAddressComputation();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            Extras extras = settingSets.Extras;
            MbAddress? CurrAddress = extras.MbAddresses.Where(a => a.Address == globalVariables.CurrMbAddr).FirstOrDefault();
            CurrAddress = CurrAddress != null ? CurrAddress : throw new Exception();
            Point UpperLeftLuma = InverseMBScan(CurrAddress);
            int[,] u = resSampleSource.U;
            int nE = 0;

            if (resSampleSource.SampleType == SampleType.Y)
            {
                Point UpperLeftSample = new Point(0, 0);                
                if (resSampleSource.Cols == 16 && resSampleSource.Rows == 16)
                {
                    UpperLeftSample = new Point(0, 0);
                    nE = 16;
                } else if (resSampleSource.Cols == 4 && resSampleSource.Rows == 4)
                {
                    UpperLeftSample = mbAddressComputation.Get4x4LumaLocation(resSampleSource.Luma4x4BlkIdx); 
                    nE = 4;
                } else if (resSampleSource.Cols == 8 && resSampleSource.Rows == 8)
                {
                    UpperLeftSample = mbAddressComputation.Get8x8LumaLocation(resSampleSource.Luma4x4BlkIdx);
                }                
                CurrAddress.ConstructedLumas = CurrAddress.ConstructedLumas != null ? CurrAddress.ConstructedLumas : new List<Serializable2DArray>();
                Serializable2DArray serializable2DArray = default!;
                int j = 0, i = 0;
                if (globalVariables.MbaffFrameFlag == 1)
                {                      
                    for (int row = 0; row < nE - 1; row++)
                    {
                        serializable2DArray = new Serializable2DArray();  
                        serializable2DArray.IntIdx = UpperLeftLuma.X + UpperLeftSample.X + j;
                        serializable2DArray.Ints = new int[nE - 1 + (UpperLeftLuma.Y + (2 * (UpperLeftSample.Y + row)))];
                        for (int col = 0; col < nE - 1; col++)
                        {
                            serializable2DArray.Ints[UpperLeftLuma.Y + (2 * (UpperLeftSample.Y + row))] = u[row, col];
                        }
                        CurrAddress.ConstructedLumas.Add(serializable2DArray);
                    }
                    j = 0; i = 0;
                } else
                {
                    for (int row = 0; row < nE - 1; row++)
                    {                        
                        serializable2DArray = new Serializable2DArray();
                        serializable2DArray.IntIdx = UpperLeftLuma.X + UpperLeftSample.X + j++;
                        serializable2DArray.Ints = new int[nE - 1 + (UpperLeftLuma.Y + UpperLeftSample.Y + row)];
                        for (int col = 0; col < nE - 1; col++)
                        {                            
                            serializable2DArray.Ints[UpperLeftLuma.Y + UpperLeftSample.Y + i++] = u[row, col];
                        } 
                        i = 0;
                        CurrAddress.ConstructedLumas.Add(serializable2DArray);                       
                    }
                    j = 0; i = 0;
                }
                settingsService.Update(extras);                
            }
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public int[,] pred4x4Y(int Luma4x4BlkIdx)
    {
        try
        {
            MbAddressComputation mbAddressComputation = new MbAddressComputation();
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            SliceHeader sliceHeader = settingSets.SliceHeader;
            PPS Pps = settingSets.GetPPS;
            SPS Sps = settingSets.GetSPS;
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            
            SynElemSlice synElemSlice = new SynElemSlice();
            synElemSlice.Slicetype = sliceHeader.slice_type;
            synElemSlice.SynElement = SynElement.mb_skip_flag;

            MicroblockTypes macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);

            MbAddress? CurrMbAddress = settingSets.Extras.MbAddresses.Where(mb => mb.Address == globalVariables.CurrMbAddr).FirstOrDefault();
            CurrMbAddress = CurrMbAddress != null ? CurrMbAddress : new MbAddress();
            Point UpperLeftLumaSample = mbAddressComputation.Get4x4LumaLocation(Luma4x4BlkIdx);
            List<Sample> samples =
            [
                new Sample(){ Location = new Point(-1, -1)},
                new Sample(){ Location = new Point(-1, 0)},
                new Sample(){ Location = new Point(-1, 1)},
                new Sample(){ Location = new Point(-1, 2)},
                new Sample(){ Location = new Point(-1, 3)},
                new Sample(){ Location = new Point(0, -1)},
                new Sample(){ Location = new Point(1, -1)},
                new Sample(){ Location = new Point(2, -1)},
                new Sample(){ Location = new Point(3, -1)},
                new Sample(){ Location = new Point(4, -1)},
                new Sample(){ Location = new Point(5, -1)},
                new Sample(){ Location = new Point(6, -1)},
                new Sample(){ Location = new Point(7, -1)},
            ];

            foreach (var sample in samples)
            {
                sample.LumaOrChromaLocation = new Point(sample.Location.X + UpperLeftLumaSample.X, sample.Location.Y + UpperLeftLumaSample.Y);
                NeighbouringLocation neighbouringLocation = mbAddressComputation.GetNeighbouringLocation(sample.LumaOrChromaLocation, true);
                NeighbouringMbAndAvailability neighbouringMbAndAvailability = mbAddressComputation.GetNeighbouringMbAndAvailability();
                neighbouringMbAndAvailability = neighbouringMbAndAvailability != null ? neighbouringMbAndAvailability : new NeighbouringMbAndAvailability();
                MbAddress? MbAddressN = new MbAddress(); 
                if (neighbouringLocation.MbAddress == MbAddressNeighbour.MbAddressA)
                {
                    MbAddressN = neighbouringMbAndAvailability.MbAddressA;
                } else if (neighbouringLocation.MbAddress == MbAddressNeighbour.MbAddressB)
                {
                    MbAddressN = neighbouringMbAndAvailability.MbAddressB;
                } else if (neighbouringLocation.MbAddress == MbAddressNeighbour.MbAddressC)
                {
                    MbAddressN = neighbouringMbAndAvailability.MbAddressC;
                } else if (neighbouringLocation.MbAddress == MbAddressNeighbour.MbAddressD)
                {
                    MbAddressN = neighbouringMbAndAvailability.MbAddressD;
                }
                MbAddressN = MbAddressN != null ? MbAddressN : new MbAddress();
                if (!MbAddressN.Available || ((MbAddressN.SliceType == (int)Slicetype.B ||
                                              MbAddressN.SliceType == (int)Slicetype.P) && 
                                              Pps.constrained_intra_pred_flag) || 
                                              (MbAddressN.SliceType == (int)Slicetype.SI && Pps.constrained_intra_pred_flag) ||
                                              (CurrMbAddress.SliceType != (int)Slicetype.SI) ||
                                              (sample.Location.X > 3 && (Luma4x4BlkIdx == 3 || Luma4x4BlkIdx == 11)))
                {
                    sample.SampleAvailable = false;
                } else
                {
                    sample.SampleAvailable = true;
                    Point UpperLeftLumaN = InverseMBScan(MbAddressN);
                    Serializable2DArray? serializable2DArray = (from s2DArr in MbAddressN.ConstructedLumas
                                                                where s2DArr.IntIdx == neighbouringLocation.Location.X + UpperLeftLumaN.X
                                                                select s2DArr).FirstOrDefault();
                    serializable2DArray = serializable2DArray != null ? serializable2DArray : throw new Exception();
                    if (globalVariables.MbaffFrameFlag == 1)
                    {
                        sample.SampleValue = serializable2DArray.Ints[neighbouringLocation.Location.Y + 2 * UpperLeftLumaN.Y];
                    } else
                    {
                        sample.SampleValue = serializable2DArray.Ints[neighbouringLocation.Location.Y + UpperLeftLumaN.Y];
                    }
                }
            }     

            foreach (var sample in samples)
            {
                if (sample.Location.X >= 4 && sample.Location.X <= 7 && (sample.Location.Y == -1))
                {
                    Sample? p = (from s in samples
                                where s.Location.X == 3 && s.Location.Y == -1
                                select s).FirstOrDefault();
                    p = p != null ? p : throw new Exception();
                    if (!sample.SampleAvailable && p.SampleAvailable)
                    {
                        sample.SampleValue = p.SampleValue;
                        sample.SampleAvailable = true;
                    }
                }
            }
            int[, ] pred4x4Y = new int[4, 4];
            Intra4x4PredModes intra4X4PredMode = Intra4x4PredMode(Luma4x4BlkIdx);
            if (intra4X4PredMode == Intra4x4PredModes.Intra4x4Vertical)
            {
                var currentSamples = samples.Where(s => (s.Location.X >= 0 && s.Location.X <= 3 && s.Location.Y == -1) && s.SampleAvailable).ToList();
                if (currentSamples.Count > 0)
                {
                    for (int predYrow = 0; predYrow < 4; predYrow++)
                    {
                        for (int predYcol = 0; predYcol < 4; predYcol++)
                        {
                            Sample? cY = (from s in currentSamples
                                         where s.Location.X == predYrow && s.Location.Y == -1
                                         select s).FirstOrDefault();
                            cY = cY != null ? cY : new Sample();
                            pred4x4Y[predYrow, predYcol] = cY.SampleValue;
                        }
                    }
                }
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4Horizontal)
            {
                var currentSamples = samples.Where(s => (s.Location.X == -1 && 
                s.Location.Y >= 0 && s.Location.Y <= 3 && s.SampleAvailable)).ToList();
                if (currentSamples.Count > 0)
                {
                    for (int predYrow = 0; predYrow < 4; predYrow++)
                    {
                        for (int predYcol = 0; predYcol < 4; predYcol++)
                        {
                            Sample? cY = (from s in currentSamples
                                         where s.Location.X == -1 && s.Location.Y == predYcol
                                         select s).FirstOrDefault();
                            cY = cY != null ? cY : new Sample();
                            pred4x4Y[predYrow, predYcol] = cY.SampleValue;
                        }
                    }
                }
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4DC)
            {
                var currentSamples = samples.Where(s => s.Location.X >= 0 && s.Location.X <= 3 && 
                (s.Location.Y == -1) || (s.Location.X == -1 && s.Location.Y >= 0 && s.Location.Y <= 3)).ToList();
                int predYVal = 0;
                if (currentSamples.Find(s => !s.SampleAvailable) == null)
                {                    
                    predYVal = currentSamples.Sum(c => c.SampleValue) >> 3;
                    for (int predYrow = 0; predYrow < 4; predYrow++)
                    {
                        for (int predYcol = 0; predYcol < 4; predYcol++)
                        {
                            pred4x4Y[predYrow, predYcol] = predYVal;
                        }
                    }
                } else if (currentSamples.Exists(s => (s.Location.X >= 0 
                && s.Location.X <= 3 && s.Location.Y == -1 && !s.SampleAvailable)) && 
                currentSamples.Find(s => s.Location.X == -1 && s.Location.Y >= 0 && s.Location.Y <= 3 
                && !s.SampleAvailable) == null)
                {
                    var xSamples = currentSamples.Where(s => s.Location.X >= 0 && s.Location.X <= 3 && s.Location.Y == -1).ToList();
                    predYVal = xSamples.Sum(s => s.SampleValue) >> 2;
                } else if (currentSamples.Find(s => s.Location.Y == -1 && s.Location.X >= 0 && s.Location.X <= 3 
                && s.SampleAvailable) == null && 
                currentSamples.Exists(s => s.Location.X == -1 
                && s.Location.Y >= 0 && s.Location.Y <= 3 && !s.SampleAvailable))
                {
                    var ySamples = currentSamples.Where(s => s.Location.X == -1 && s.Location.Y >= 0 
                    && s.Location.Y <= 3).ToList();
                    predYVal = ySamples.Sum(s => s.SampleValue) >> 2;
                } else
                {
                    predYVal = 1 << ((int)globalVariables.BitDepthY - 1);
                }
                for (int predYrow = 0; predYrow < 4; predYrow++)
                {
                    for (int predYcol = 0; predYcol < 4; predYcol++)
                    {
                        pred4x4Y[predYrow, predYcol] = predYVal;
                    }
                }                
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4DiagonalDownLeft)
            {
                var currentSamples = samples.Where(s => (s.Location.X >= 7 && s.Location.X < 8) && 
                    (s.Location.Y == -1) && s.SampleAvailable);
                if (currentSamples.Count() > 0)
                {
                    foreach (var sample in currentSamples)
                    {
                        if (sample.Location.X == 3 && sample.Location.Y == 3)
                        {
                            var sample1 = samples.Find(s => s.Location.X == 6 && s.Location.Y == -1);
                            var sample2 = samples.Find(s => s.Location.X == 7 && s.Location.Y == -1);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();

                            for (int row = 0; row < 4; row++)
                            {
                                for (int col = 0; col < 4; col++)
                                {
                                    pred4x4Y[row, col] = (sample1.SampleValue + sample2.SampleValue + 2) >> 2;
                                }
                            }
                        } else
                        {
                            for (int row = 0; row < 4; row++)
                            {
                                for (int col = 0; col < 4; col++)
                                {
                                    var sample1 = samples.Find(s => s.Location.X == row + col && s.Location.Y == -1);
                                    var sample2 = samples.Find(s => s.Location.X == row + col + 1 && s.Location.Y == -1);
                                    var sample3 = samples.Find(s => s.Location.X == row + col + 2 && s.Location.Y == -1);

                                    sample1 = sample1 != null ? sample1 : throw new Exception();
                                    sample2 = sample2 != null ? sample2 : throw new Exception();
                                    sample3 = sample3 != null ? sample3 : throw new Exception();

                                    pred4x4Y[row, col] = (sample1.SampleValue + (2 * (sample2.SampleValue)) + sample3.SampleValue) >> 2;
                                }
                            }
                        }
                    }
                }
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4DiagonalDownRight)
            {
                var currentSamples = samples.Where(s => ((s.Location.X >= 0 && s.Location.X <= 3) && s.Location.Y == -1) || 
                (s.Location.X == -1 && (s.Location.Y >= -1 && s.Location.Y <= 3)) && s.SampleAvailable).ToList();

                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        if (row > col)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == row - col - 2)
                            && (s.Location.Y == -1));
                            var sample2 = currentSamples.Find(s => (s.Location.X == row - col - 1) &&
                             (s.Location.Y == -1));
                            var sample3 = currentSamples.Find(s => (s.Location.X == row - col) && s.Location.Y == -1);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = ((sample1.SampleValue + (2 * sample2.SampleValue) + sample3.SampleValue) + 2) >> 2;
                        } else if (row < col)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == -1)
                            && (s.Location.Y == col - row - 2));
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) &&
                             (s.Location.Y == col - row -1));
                            var sample3 = currentSamples.Find(s => (s.Location.X == -1) && 
                            s.Location.Y == col - row);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = ((sample1.SampleValue + (2 * sample2.SampleValue) + sample3.SampleValue) + 2) >> 2;
                        } else
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == 0)
                            && (s.Location.Y == -1));
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) &&
                             (s.Location.Y == -1));
                            var sample3 = currentSamples.Find(s => (s.Location.X == -1) && 
                            s.Location.Y == 0);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = ((sample1.SampleValue + (2 * sample2.SampleValue) + sample3.SampleValue) + 2) >> 2;
                        }
                    }
                }                
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4VerticalRight)
            {
                var currentSamples = samples.Where(s => ((s.Location.X >= 0 && s.Location.X <= 3 && s.Location.Y == -1)
                    || ((s.Location.X == -1) && s.Location.Y >= -1 && s.Location.Y <= 3)) && s.SampleAvailable).ToList();

                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        int zVR = 2 * row - col;
                        if (zVR == 0 || zVR == 2 || zVR == 4 || zVR == 6)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == ((row - (col >> 1)) - 1) && s.Location.Y == -1));
                            var sample2 = currentSamples.Find(s => (s.Location.X == (row - (col >> 1)) && s.Location.Y == -1));
                            
                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + sample2.SampleValue + 1) >> 1;
                        } else if (zVR == 1 || zVR == 3 || zVR == 5)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == (row - (col >> 1) - 2) && s.Location.Y == -1));
                            var sample2 = currentSamples.Find(s => (s.Location.X == (row - (col >> 1) - 1) && s.Location.Y == -1));
                            var sample3 = currentSamples.Find(s => (s.Location.X == (row - (col >> 1))) && s.Location.Y == -1);
                            
                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        } else if (zVR == -1)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == - 1) && s.Location.Y == 0);
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && s.Location.Y == -1);
                            var sample3 = currentSamples.Find(s => (s.Location.X ==  0) && s.Location.Y == -1);
                            
                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        } else if (zVR == -2 || zVR == -3)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == - 1) && s.Location.Y == col - 1);
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && s.Location.Y == col - 2);
                            var sample3 = currentSamples.Find(s => (s.Location.X ==  -1) && s.Location.Y == col - 3);
                            
                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        }
                    }
                }
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4HorizontalDown)
            {
                var currentSamples = samples.Where(s => ((s.Location.X >= 0 && s.Location.X <= 3 && s.Location.Y == -1)
                    || ((s.Location.X == -1) && s.Location.Y >= -1 && s.Location.Y <= 3)) && s.SampleAvailable).ToList();

                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        int zHD = 2 * col - row;
                        if (zHD == 0 || zHD == 2 || zHD == 4 || zHD == 6)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == - 1) && s.Location.Y == col - ( row >> 1) - 1);
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && s.Location.Y == col - ( row >> 1));

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + sample2.SampleValue + 1) >> 1;
                        } else if (zHD == 1 || zHD == 3 || zHD == 5)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == - 1) && s.Location.Y == col - ( row >> 1) - 2);
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && s.Location.Y == col - ( row >> 1) - 1);
                            var sample3 = currentSamples.Find(s => (s.Location.X == -1) && s.Location.Y == col - (row >> 1));

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        } else if(zHD == -1)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == - 1) && s.Location.Y == 0);
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && s.Location.Y == - 1);
                            var sample3 = currentSamples.Find(s => (s.Location.X == 0) && s.Location.Y == - 1);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        } else if(zHD == -2 || zHD == -3)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == row - 1) && s.Location.Y == -1);
                            var sample2 = currentSamples.Find(s => (s.Location.X == row - 2) && s.Location.Y == - 1);
                            var sample3 = currentSamples.Find(s => (s.Location.X == row - 3) && s.Location.Y == - 1);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        }
                    }
                }
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4VerticalLeft)
            {
                var currentSamples = samples.Where(s => (s.Location.X >= 0 && s.Location.X <= 7 && s.Location.Y == -1 
                        && s.SampleAvailable)).ToList();

                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        if (col == 0 || col == 2)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == row + (col >> 1)) && s.Location.Y == -1);
                            var sample2 = currentSamples.Find(s => (s.Location.X == row + (col >> 1) + 1) && s.Location.Y == - 1);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + sample2.SampleValue + 1) >> 1;
                        } else if(col == 1 || col == 3)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == row + (col >> 1)) && s.Location.Y == -1);
                            var sample2 = currentSamples.Find(s => (s.Location.X == row + (col >> 1) + 1) && s.Location.Y == - 1);
                            var sample3 = currentSamples.Find(s => (s.Location.X == row + (col >> 1) + 2) && s.Location.Y == - 1);

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        }
                    }
                }
            } else if (intra4X4PredMode == Intra4x4PredModes.Intra4x4HorizontalUp)
            {
                var currentSamples = samples.Where(s => (s.Location.X == -1 && 
                    (s.Location.Y >= 0 && s.Location.Y <= 3) 
                        && s.SampleAvailable)).ToList();
                
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        int zHU = row + 2 * col;
                        if (zHU == 0 || zHU == 2 || zHU == 4)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == col + (row >> 1)));
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == col + (row >> 1) + 1));

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + sample2.SampleValue + 1) >> 1;
                        } else if (zHU == 1 || zHU == 3)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == col + (row >> 1)));
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == col + (row >> 1) + 1));
                            var sample3 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == col + (row >> 1) + 2));

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();
                            sample3 = sample3 != null ? sample3 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + 2 * sample2.SampleValue + sample3.SampleValue + 2) >> 2;
                        } else if (zHU == 5)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == 2));
                            var sample2 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == 3));

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            sample2 = sample2 != null ? sample2 : throw new Exception();

                            pred4x4Y[row, col] = (sample1.SampleValue + sample2.SampleValue + 2) >> 2;
                        } else if (zHU > 5)
                        {
                            var sample1 = currentSamples.Find(s => (s.Location.X == -1) && (s.Location.Y == 3));

                            sample1 = sample1 != null ? sample1 : throw new Exception();
                            pred4x4Y[row, col] = sample1.SampleValue;
                        }
                    }
                }
            }
            return pred4x4Y;   
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public Intra4x4PredModes Intra4x4PredMode(int luma4x4BlkIdx)
    {
        try
        {            
            MbAddressComputation mbAddressComputation = new MbAddressComputation();
            Neighbouring4x4LumaBlocks neighbouring4X4 = mbAddressComputation.GetNeighbouring4x4LumaBlk(luma4x4BlkIdx);
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            Extras extras = settingSets.Extras;
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            PPS Pps = settingSets.GetPPS;
            
            Intra4x4PredModes[] Intra4X4PredMode = new Intra4x4PredModes[16];

            MbAddress? CurrMbAddress = extras.MbAddresses.Where(mb => mb.Address == globalVariables.CurrMbAddr).FirstOrDefault();
            CurrMbAddress = CurrMbAddress != null ? CurrMbAddress : throw new Exception();

            SliceHeader sliceHeader = settingSets.SliceHeader;
            
            MbAddress? mbAddressA = neighbouring4X4.MbAddressA;
            mbAddressA = mbAddressA != null ? mbAddressA : throw new Exception();
            MbAddress? mbAddressB = neighbouring4X4.MbAddressB;
            mbAddressB = mbAddressB != null ? mbAddressB : throw new Exception();

            mbAddressA = mbAddressComputation.MbAddrAvailable(mbAddressA, CurrMbAddress);
            mbAddressB = mbAddressComputation.MbAddrAvailable(mbAddressB, CurrMbAddress);

            SynElemSlice synElemSlice = new SynElemSlice();      

            bool dcPredModePredictedFlag = false;
            if (!mbAddressA.Available || !mbAddressB.Available || ((mbAddressA.Available && 
            (Slicetype)mbAddressA.SliceType == Slicetype.P || 
            (Slicetype)mbAddressA.SliceType == Slicetype.B) && Pps.constrained_intra_pred_flag) ||
            ((mbAddressB.Available && (Slicetype)mbAddressB.SliceType == Slicetype.P ||
            (Slicetype)mbAddressB.SliceType == Slicetype.B) && Pps.constrained_intra_pred_flag))
            {
                dcPredModePredictedFlag = true;
            }
            int intraMxMPredModelA = 0, intraMxMPredModelB = 0;

            synElemSlice.Slicetype = (Slicetype)mbAddressA.SliceType;
            MicroblockTypes macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);     

            if (dcPredModePredictedFlag || 
            macroblockTypes.MbPartPredMode((uint)mbAddressA.MbType, 0) != PredictionModes.Intra_4x4 ||
            macroblockTypes.MbPartPredMode((uint)mbAddressA.MbType, 0) != PredictionModes.Intra_8x8)
            {
                intraMxMPredModelA = 2;
            }
            synElemSlice.Slicetype = (Slicetype)mbAddressB.SliceType;
            macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);     
            if (dcPredModePredictedFlag || 
            macroblockTypes.MbPartPredMode((uint)mbAddressB.MbType, 0) != PredictionModes.Intra_4x4 ||
            macroblockTypes.MbPartPredMode((uint)mbAddressB.MbType, 0) != PredictionModes.Intra_8x8)
            {
                intraMxMPredModelB = 2;
            }
            if (!dcPredModePredictedFlag)
            {
                synElemSlice.Slicetype = (Slicetype)mbAddressA.SliceType;
                macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);     
                if (macroblockTypes.MbPartPredMode((uint)mbAddressA.MbType, 0) == PredictionModes.Intra_4x4)
                {                    
                    intraMxMPredModelA = (int)mbAddressA.Intra4X4PredMode[luma4x4BlkIdx];
                }
                if (macroblockTypes.MbPartPredMode((uint)mbAddressA.MbType, 0) == PredictionModes.Intra_8x8)
                {
                    intraMxMPredModelA = (int)mbAddressA.Intra4X4PredMode[luma4x4BlkIdx >> 2];
                }
                synElemSlice.Slicetype = (Slicetype)mbAddressB.SliceType;
                macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);
                if (macroblockTypes.MbPartPredMode((uint)mbAddressB.MbType, 0) == PredictionModes.Intra_4x4)
                {
                    intraMxMPredModelB = (int)mbAddressB.Intra4X4PredMode[luma4x4BlkIdx];
                }                
                if (macroblockTypes.MbPartPredMode((uint)mbAddressB.MbType, 0) == PredictionModes.Intra_8x8)
                {                    
                    intraMxMPredModelB = (int)mbAddressB.Intra4X4PredMode[luma4x4BlkIdx >> 2];
                }
            }
            int predIntra4x4PredMode = Math.Min(intraMxMPredModelA, intraMxMPredModelB);
            CurrMbAddress.Intra4X4PredMode = CurrMbAddress.Intra4X4PredMode == null ? 
                new Intra4x4PredModes[16] : CurrMbAddress.Intra4X4PredMode;
            if (CurrMbAddress.IntraMbPredMode.prev_intra4x4_pred_mode_flag[luma4x4BlkIdx])
            {
                CurrMbAddress.Intra4X4PredMode[luma4x4BlkIdx] = (Intra4x4PredModes)predIntra4x4PredMode;
            } else
            {
                if (CurrMbAddress.IntraMbPredMode.rem_intra4x4_pred_mode.Length < luma4x4BlkIdx)
                {
                    if (CurrMbAddress.IntraMbPredMode.rem_intra4x4_pred_mode[luma4x4BlkIdx] < predIntra4x4PredMode)
                    {
                        CurrMbAddress.Intra4X4PredMode[luma4x4BlkIdx] = (Intra4x4PredModes)CurrMbAddress.IntraMbPredMode.rem_intra4x4_pred_mode[luma4x4BlkIdx];
                    } else
                    {
                        CurrMbAddress.Intra4X4PredMode[luma4x4BlkIdx] = (Intra4x4PredModes)(CurrMbAddress.IntraMbPredMode.rem_intra4x4_pred_mode[luma4x4BlkIdx] + 1);
                    }
                }
            }
            return CurrMbAddress.Intra4X4PredMode[luma4x4BlkIdx];
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private Point InverseMBScan(MbAddress currMb)
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;

            Point LumaSampleLoc = new Point(0, 0);
            if (globalVariables.MbaffFrameFlag == 0)
            {
                LumaSampleLoc.X = Mathematics.InverseRasterScan(currMb.Address, 16, 16, globalVariables.PicWidthInSamplesL, 0);
                LumaSampleLoc.Y = Mathematics.InverseRasterScan(currMb.Address, 16, 16, globalVariables.PicWidthInSamplesL, 1);
            }
            return LumaSampleLoc;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] IResTransBypassDec(int nW, int nH, Intra4x4PredModes intra4x4PredMode, int[,] r)
    {
        try
        {
            int[,] f = new int[nW, nH];
            for (int hIndex = 0; hIndex < nH; hIndex++)
            {
                for (int wIndex = 0; wIndex < nW; wIndex++)
                {
                    f[hIndex, wIndex] = r[hIndex, wIndex];
                }
            }

            int rValue = 0;
            if (intra4x4PredMode == Intra4x4PredModes.Intra4x4Vertical)
            {                
                for (int hIndex = 0; hIndex < nH; hIndex++)
                {
                    for (int wIndex = 0; wIndex < nW; wIndex++)
                    {
                        for (int k = 0; k <= hIndex; k++)
                        {
                            rValue += f[k, wIndex]; 
                        }
                        r[hIndex, wIndex] = rValue;                        
                    }
                }
            } else if(intra4x4PredMode == Intra4x4PredModes.Intra4x4Horizontal)
            {
                for (int hIndex = 0; hIndex < nH; hIndex++)
                {
                    for (int wIndex = 0; wIndex < nW; wIndex++)
                    {
                        for (int k = 0; k <= hIndex; k++)
                        {
                            rValue += f[hIndex, k];
                        }
                        r[hIndex, wIndex] = rValue;                        
                    }
                }
            }
            return r;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] ScalingAndTransResidual4x4(ResSampleSource resSampleSource)
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            Extras extras = settingSets.Extras;
            MbAddress? CurrAddress = extras.MbAddresses.Where(mb => mb.Address == globalVariables.CurrMbAddr).FirstOrDefault();
            CurrAddress = CurrAddress != null ? CurrAddress : throw new Exception();
            SynElemSlice synElemSlice = new SynElemSlice();
            synElemSlice.Slicetype = (Slicetype)CurrAddress.SliceType;
            MicroblockTypes macroblockTypes = new MicroblockTypes(codecSettings, synElemSlice);

            uint bitDepth = 0; bool sMbFlag = false; long qP = 0; 
            int[,] d = new int[4, 4], r = new int[4, 4];
            if (resSampleSource.SampleType == SampleType.Y)
            {
                bitDepth = globalVariables.BitDepthY;    
            } else if (resSampleSource.SampleType == SampleType.Cb || resSampleSource.SampleType == SampleType.Cr)
            {
                bitDepth = globalVariables.BitDepthC;
            }
            if ((CurrAddress.SliceType == (int)Slicetype.SI) || (CurrAddress.SliceType == (int)Slicetype.SP))
            {
                sMbFlag = true;
            } else if((CurrAddress.SliceType != (int)Slicetype.SI) && (CurrAddress.SliceType != (int)Slicetype.SP))
            {
                sMbFlag = false;
            }
            if (resSampleSource.SampleType == SampleType.Y && !sMbFlag)
            {
                globalVariables.QPprimeC = GetQprimeC(resSampleSource);
                qP = globalVariables.QPprimeY;
            } else if (resSampleSource.SampleType == SampleType.Y && sMbFlag)
            {
                qP = globalVariables.QPY;
            } else if (resSampleSource.SampleType == SampleType.Cb || resSampleSource.SampleType == SampleType.Cr && !sMbFlag)
            {
                globalVariables.QPprimeC = GetQprimeC(resSampleSource);
                qP = globalVariables.QPprimeC;
            } else if (resSampleSource.SampleType == SampleType.Cb || resSampleSource.SampleType == SampleType.Cr && sMbFlag)
            {
                qP = globalVariables.QPC;
            }

            if (globalVariables.TransformBypassModeFlag)
            {
                for (int row = 0; row < 4 ; row++)
                {
                    for(int col = 0; col < 4; col++)
                    {
                        r[row, col] = resSampleSource.U[row, col];
                    }
                }
                return r;                
            } else
            {
                d = Scaling4x4(bitDepth, qP, resSampleSource);
                r = TransRes4x4(bitDepth, d);
            }   
            return r;         
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] TransRes4x4(uint bitDepth, int[,] d)
    {
        try
        {
            int[, ] r = new int[4, 4], e = new int[4, 4], f = new int[4, 4], 
            g = new int[4, 4], h = new int[4, 4];

            for (int row = 0; row < 4; row++)
            {
                e[row, 0] = d[row, 0] + d[row, 2];
                e[row, 1] = d[row, 0] - d[row, 2];
                e[row, 2] = (d[row, 1] >> 1) - d[row, 3];
                e[row, 3] = d[row, 1] + (d[row, 3] >> 1);

                f[row, 0] = e[row, 0] + e[row, 3];
                f[row, 1] = e[row, 1] + e[row, 2];
                f[row, 2] = e[row, 1] - e[row, 2];
                f[row, 3] = e[row, 0] - e[row, 3];
            }

            for (int col = 0; col < 4; col++)
            {
                 g[0, col] = f[0, col] + f[2, col];
                 g[1, col] = f[0, col] - f[2, col];
                 g[2, col] = (f[1, col] >> 1) - f[3, col];
                 g[3, col] = f[1, col] + (f[3, col] >> 1); 

                 h[0, col] = g[0, col] + g[3, col];
                 h[1, col] = g[1, col] + g[2, col];
                 h[2, col] = g[1, col] - g[2, col];
                 h[3, col] = g[0, col] - g[3, col];
            }

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    r[row, col] = (h[row, col] + (int)Math.Pow(2, 5)) >> 6;
                }
            }
            return r;            
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] Scaling4x4(uint bitDepth, long qP, ResSampleSource resSampleSource)
    {
        try
        {
            int[, ] d = new int[4, 4];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (row == 0 && col == 0 && 
                    resSampleSource.Rows == 16 && resSampleSource.Cols == 16 || 
                    resSampleSource.SampleType == SampleType.Cb || resSampleSource.SampleType == SampleType.Cr)
                    {
                        d[row, col] = resSampleSource.U[row, col];
                    } else
                    {
                        if (qP >= 24)
                        {
                            d[row, col] = (resSampleSource.U[row, col] * LevelScale4x4(qP % 6, row, col, resSampleSource.SampleType)) << (((int)qP / 6) - 4);
                        } else
                        {
                            int LevelScale = LevelScale4x4(qP % 6, row, col, resSampleSource.SampleType);
                            int dValue = resSampleSource.U[row, col];
                            int product = dValue * LevelScale;
                            int qpValue = (int)Math.Pow(2, 3 - (qP / 6));
                            int qpDivisor = 4 - (int)(qP / 6);
                            d[row, col] = (product + qpValue) >> qpDivisor;
                        }
                    }
                }
            }
            return d;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int LevelScale4x4(long m, int row, int col, SampleType sampleType)
    {
        try
        {
            int LevelScale, iYCbCr = 0;
            bool mbIsInterFlag = false;
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            Extras extras = settingSets.Extras;
            PPS Pps = settingSets.GetPPS;
            SPS Sps = settingSets.GetSPS;
            SliceHeader sliceHeader = settingSets.SliceHeader;

            MbAddress? CurrMbAddress = extras.MbAddresses.Where(mb => mb.Address == globalVariables.CurrMbAddr).FirstOrDefault();
            CurrMbAddress = CurrMbAddress != null ? CurrMbAddress : throw new Exception();
            
            if (CurrMbAddress.SliceType == (int)Slicetype.P ||
                CurrMbAddress.SliceType == (int)Slicetype.B)
            {
                mbIsInterFlag = true;
            } else
            {
                mbIsInterFlag = false;
            }

            if (Sps.separate_colour_plane == 1)
            {
                iYCbCr = (int)sliceHeader.colour_plane_id;
            } else
            {
                if (sampleType == SampleType.Y)
                {
                    iYCbCr = 0;
                } else if(sampleType == SampleType.Cb)
                {
                    iYCbCr = 1;
                } else if (sampleType == SampleType.Cr)
                {
                    iYCbCr = 2;  
                }
            }
            int[,] ScalingList4x4 = DeserializeTo2DArray(globalVariables.ScalingList4x4);
            int[,] weightScale4x4 = InverseScanning4x4(ScalingList4x4, iYCbCr + ((mbIsInterFlag == true) ? 3 : 0));
            LevelScale = weightScale4x4[row, col] * normAdjust4x4(m, row, col);

            return LevelScale;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public int normAdjust4x4(long m, int row, int col)
    {
        try
        {
            int[, ] v = {{10, 16, 13}, {11, 18, 14}, {13, 20, 16}, 
            {14, 23, 18}, {16, 25, 20}, {18, 29, 23}};

            if (row % 2 == 0 && col % 2 == 0)
            {
                return v[m, 0];
            } else if (row % 2 == 1 && col % 2 == 1)
            {
                return v[m, 1];
            } else
            {
                return v[m, 2];
            }
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[] GetScale4x4Index(int[,] scalingList4x4, int scaleListIndex)
    {
        try
        {
            int[] scales = new int[16];
            for (int i = 0; i < scales.Length; i++)
            {
                scales[i] = scalingList4x4[scaleListIndex, i];
            }
            return scales;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private long GetQprimeC(ResSampleSource resSampleSource)
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();
            GlobalVariables globalVariables = settingSets.GlobalVariables;
            PPS Pps = settingSets.GetPPS;
            SPS Sps = settingSets.GetSPS;

            int qPOffset = 0;
            if (resSampleSource.SampleType == SampleType.Cb)
            {
                qPOffset = Pps.chroma_qp_index_offset;
            } else if (resSampleSource.SampleType == SampleType.Cr)
            {
                qPOffset = Pps.second_chroma_qp_index_offset;
            }
            int qPi = Mathematics.Clip3((int)-globalVariables.QpBdOffsetC, 51, (int)globalVariables.QPY + qPOffset);
            ChromaQParameter chromaQParameter = globalVariables.Qparameters;
            List<QPTable> qPTables = chromaQParameter.QParameters;

            if (qPi < 30)
            {
                globalVariables.QPC = qPi;
            } else
            {
                QPTable? Qpc = qPTables.Where(qp => qp.QPi == qPi).FirstOrDefault();
                Qpc = Qpc != null ? Qpc : throw new Exception();
                globalVariables.QPC = Qpc.QPc;
            }
            globalVariables.QPprimeC = globalVariables.QPC + globalVariables.QpBdOffsetC;
            return globalVariables.QPprimeC;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    private int[,] InverseScanning4x4(int[,] Coeff, int Luma4x4BlkIdx)
    {
        try
        {
            int[,] c = new int[4, 4];

            c[0, 0] = Coeff[Luma4x4BlkIdx, 0];
            c[0, 1] = Coeff[Luma4x4BlkIdx, 1];
            c[1, 0] = Coeff[Luma4x4BlkIdx, 2];
            c[2, 0] = Coeff[Luma4x4BlkIdx, 3];
            c[1, 1] = Coeff[Luma4x4BlkIdx, 4];
            c[0, 2] = Coeff[Luma4x4BlkIdx, 5];
            c[0, 3] = Coeff[Luma4x4BlkIdx, 6];
            c[1, 2] = Coeff[Luma4x4BlkIdx, 7];
            c[2, 1] = Coeff[Luma4x4BlkIdx, 8];
            c[3, 0] = Coeff[Luma4x4BlkIdx, 9];
            c[3, 1] = Coeff[Luma4x4BlkIdx, 10];
            c[2, 2] = Coeff[Luma4x4BlkIdx, 11];
            c[1, 3] = Coeff[Luma4x4BlkIdx, 12];
            c[2, 3] = Coeff[Luma4x4BlkIdx, 13];
            c[3, 2] = Coeff[Luma4x4BlkIdx, 14];
            c[3, 3] = Coeff[Luma4x4BlkIdx, 15];

            return c;
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
                    MbPred.rem_intra4x4_pred_mode = new int[16];
                    for (int luma4x4BlkIdx = 0; luma4x4BlkIdx < 16; luma4x4BlkIdx++)
                    {
                        MbPred.prev_intra4x4_pred_mode_flag[luma4x4BlkIdx] =  Pps.entropy_coding_mode_flag ? bitStream.ae() == 1: bitStream.u(1) == 1;
                        if (!MbPred.prev_intra4x4_pred_mode_flag[luma4x4BlkIdx])
                        {
                            MbPred.rem_intra4x4_pred_mode[luma4x4BlkIdx] = Pps.entropy_coding_mode_flag ? bitStream.ae(): (int)bitStream.u(3);
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

