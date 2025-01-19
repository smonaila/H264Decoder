using System;
using System.Collections;
using mp4.boxes;
using h264.NALUnits;

namespace h264.utilities;

public class BitList
{
    private BitArray bitArray;
    public BitList(byte[] bytes) 
    { 
        bitArray = new BitArray(bytes);
    }

    public uint Read(uint BitCounter)
    {       
        string bits = ReadBits(BitCounter);
        Console.WriteLine(@"BitString: {0}", bits);
        int value = Convert.ToInt32(bits, 2);
        return (uint)value;
    }

    public string ReadBits(uint BitCounter)
    {
        string bits = string.Empty;
        try
        {
            if (Position + BitCounter < bitArray.Length && BitCounter > 0)
            {
                for (int bitIndex = (int)Position; bitIndex < (BitCounter + (int)Position); bitIndex++)
                {
                    bits += string.Format("{0}", bitArray.Get(bitIndex) == true ? 1 : 0);
                }
                Position += BitCounter;
            }
            else
            {
                bits = string.Format(@"{0}", 0);
            }
        }
        catch (OverflowException)
        {

        }
        catch (System.Exception)
        {
            throw;
        }
        return bits;
    }
    public uint Position { get; set; }
    public uint Length { get { return (uint)bitArray.Length; } }
}

public class HrdParameters
{
    public HrdParameters()
    {

    }
    public uint cpb_cnt_minus1 { get; set; }
    public uint bit_rate_scale { get; set; }
    public uint cpb_size_scale { get; set; }
    public List<uint> bit_rate_value_minus1 { get; set; } = default!;
    public List<uint> cpb_size_value_minus1 { get; set; } = default!;
    public List<bool> cbr_flag { get; set; } = default!;
    public uint initial_cpb_removal_delay_length_minus1 { get; set; }
    public uint cpb_removal_delay_length_minus1 { get; set; }
    public uint dpb_output_delay_length_minus { get; set; }
    public uint time_offset_length { get; set; }
}

/// <summary>
/// NAL Unit parsers.
/// </summary>
public static partial class H264Utilities
{
    public static HrdParameters set_hrd_parameters(BitList bitStream)
    {
        try
        {
            HrdParameters hrdParameters = new HrdParameters();

            hrdParameters.cpb_cnt_minus1 = bitStream.ue() + 1;
            hrdParameters.bit_rate_scale = bitStream.read_bits(4);
            hrdParameters.cpb_size_scale = bitStream.read_bits(4);
            hrdParameters.bit_rate_value_minus1 = new List<uint>();
            hrdParameters.cpb_size_value_minus1 = new List<uint>();
            hrdParameters.cbr_flag = new List<bool>();

            for (int SchedSelIdx = 0; SchedSelIdx <= hrdParameters.cpb_cnt_minus1; SchedSelIdx++)
            {
                hrdParameters.bit_rate_value_minus1.Add(bitStream.ue());
                hrdParameters.cpb_size_value_minus1.Add(bitStream.ue());
                hrdParameters.cbr_flag.Add(bitStream.read_bits(1) == 1);
            }
            hrdParameters.initial_cpb_removal_delay_length_minus1 = bitStream.read_bits(5);
            hrdParameters.cpb_removal_delay_length_minus1 = bitStream.read_bits(5);
            hrdParameters.dpb_output_delay_length_minus = bitStream.read_bits(5);
            hrdParameters.time_offset_length = bitStream.read_bits(5);
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
            vuiParameters.aspect_ratio_info_present_flag = bitStream.read_bits(1) == 1;
            byte Extended_SAR = 255;
            if (vuiParameters.aspect_ratio_info_present_flag)
            {
                vuiParameters.aspect_ratio_idc = bitStream.read_bits(8);
                if (vuiParameters.aspect_ratio_idc == Extended_SAR)
                {
                    vuiParameters.sar_width = bitStream.read_bits(16);
                    vuiParameters.sar_height = bitStream.read_bits(16);
                }
            }
            vuiParameters.overscan_info_present_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.overscan_info_present_flag)
            {
                vuiParameters.overscan_appropriate_flag = bitStream.read_bits(1) == 1;
            }
            vuiParameters.video_signal_type_present_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.video_signal_type_present_flag)
            {
                vuiParameters.video_format = bitStream.read_bits(3);
                vuiParameters.video_full_range_flag = bitStream.read_bits(1) == 1;
                vuiParameters.colour_discription_present_flag = bitStream.read_bits(1) == 1;

                if (vuiParameters.colour_discription_present_flag)
                {
                    vuiParameters.colour_primaries = bitStream.read_bits(8);
                    vuiParameters.transfer_characteristics = bitStream.read_bits(8);
                    vuiParameters.matrix_coefficients = bitStream.read_bits(8);
                }
            }
            vuiParameters.chroma_loc_info_present_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.chroma_loc_info_present_flag)
            {
                vuiParameters.chroma_sample_loc_top_field = bitStream.ue();
                vuiParameters.chroma_sample_loc_type_bottom_field = bitStream.ue();
            }
            vuiParameters.timing_info_present_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.timing_info_present_flag)
            {
                vuiParameters.num_units_in_stick = bitStream.read_bits(32);
                vuiParameters.time_scale = bitStream.read_bits(32);
                vuiParameters.fixed_frame_rate_flag = bitStream.read_bits(1) == 1;
            }
            vuiParameters.nal_hrd_parameters_present_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.nal_hrd_parameters_present_flag)
            {
                // Hrd_Parameters.
                HrdParameters hrdParameters = H264Utilities.set_hrd_parameters(bitStream);
            }
            vuiParameters.vcl_hrd_parameters_present_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.vcl_hrd_parameters_present_flag)
            {
                // Hrd_Parameters.
                HrdParameters hrdParameters = H264Utilities.set_hrd_parameters(bitStream);
            }
            if (vuiParameters.nal_hrd_parameters_present_flag || vuiParameters.vcl_hrd_parameters_present_flag)
            {
                vuiParameters.low_delay_hrd_flag = bitStream.read_bits(1) == 1;
            }
            vuiParameters.pic_struct_present_flag = bitStream.read_bits(1) == 1;
            vuiParameters.bitstream_restriction_flag = bitStream.read_bits(1) == 1;
            if (vuiParameters.bitstream_restriction_flag)
            {
                vuiParameters.motion_vectors_over_pic_boundaries_flag = bitStream.read_bits(1) == 1;
                vuiParameters.max_bytes_per_pic_denom = bitStream.ue();
                vuiParameters.max_bits_per_mb_denom = bitStream.ue();
                vuiParameters.log2_max_mv_length_horizontal = bitStream.ue();
                vuiParameters.log2_max_mv_length_vertical = bitStream.ue();
                vuiParameters.max_num_reorder_frames = bitStream.ue();
                vuiParameters.max_dec_frame_buffering = bitStream.ue();
            }
            return vuiParameters;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static SPS seq_parameter_set_rbsp(byte[] spsBytes)
    {
        using (MemoryStream memoryStream = new MemoryStream(spsBytes))
        {
            BitList bitStream = new BitList(spsBytes);

            SPS Sps = new SPS();
            
            Console.WriteLine(@"bitStream: Length = {0}, Position: {1}", bitStream.Length, bitStream.Position);

            Sps.profile_Idc = bitStream.read_bits(8);
            uint constraint_value = bitStream.read_bits(8);

            Sps.constraint_set0_flag = (constraint_value & 255) == 128;
            Sps.constraint_set1_flag = (constraint_value & 255) == 64;
            Sps.constraint_set2_flag = (constraint_value & 255) == 32;
            Sps.constraint_set3_flag = (constraint_value & 255) == 16;
            Sps.constraint_set4_flag = (constraint_value & 255) == 8;
            Sps.constraint_set5_flag = (constraint_value & 255) == 4;
            Sps.reserved_zero_2bits = constraint_value & 3;

            Sps.level_idc = bitStream.read_bits(8);

            Console.WriteLine(@"After Reading: bitStream: Length = {0}, Position: {1}", bitStream.Length, bitStream.Position);
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
                reserved_zero_2bits = Sps.reserved_zero_2bits
            };
            Console.WriteLine(@"JsonSPS: {0}", sps.ToString());

            Sps.seq_parameter_set_id = bitStream.ue();
            
            if (Sps.profile_Idc == 100 || Sps.profile_Idc == 110 || Sps.profile_Idc == 122 ||
                Sps.profile_Idc == 244 || Sps.profile_Idc == 44 || Sps.profile_Idc == 83 ||
                Sps.profile_Idc == 86 || Sps.profile_Idc == 118 || Sps.profile_Idc == 138 ||
                Sps.profile_Idc == 139 || Sps.profile_Idc == 134 || Sps.profile_Idc == 135)
            {
                Sps.chroma_format_idc = bitStream.ue();
                if (Sps.chroma_format_idc == 3)
                {
                    Sps.separate_colour_plane = bitStream.read_bits(1);
                }
                Sps.bit_depth_luma_minus8 = bitStream.ue();
                Sps.bit_depth_chroma_minus8 = bitStream.ue();
                Sps.qpprime_y_zero_transform_bypass_flag = bitStream.read_bits(1);
                Sps.seq_scaling_matrix_present_flag = bitStream.read_bits(1);

                if (Sps.seq_scaling_matrix_present_flag == 1)
                {
                    for (int i = 0; i < ((Sps.chroma_format_idc != 3) ? 8 : 12); i++)
                    {
                        Sps.seq_scaling_list_present_flag[i] = bitStream.read_bits(1);
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
                Sps.delta_pic_order_always_zero_flag = bitStream.read_bits(1) == 1;
                // Sps.offset_for_non_ref_pic = memoryStream.se(memoryStream.ue());
            }
            Sps.max_num_ref_frames = bitStream.ue();
            Sps.gaps_in_frame_num_value_allowed_flag = bitStream.read_bits(1);
            Sps.pic_width_in_mbs_minus1 = bitStream.ue();
            Sps.pic_height_in_map_units_minus1 = bitStream.ue();
            Sps.frame_mbs_only_flag = bitStream.read_bits(1) == 1;

            if (!Sps.frame_mbs_only_flag)
            {
                Sps.mb_adaptive_frame_field_flag = bitStream.read_bits(1);
            }
            Sps.direct_8x8_inference_flag = bitStream.read_bits(1);
            Sps.frame_cropping_flag = bitStream.read_bits(1);

            if (Sps.frame_cropping_flag == 1)
            {
                Sps.frame_crop_left_offset = bitStream.ue();
                Sps.frame_crop_right_offset = bitStream.ue();
                Sps.frame_crop_top_offset = bitStream.ue();
                Sps.fram_crop_bottom_offset = bitStream.ue();
            }
            Sps.vui_parameters_present_flag = bitStream.read_bits(1);

            if (Sps.vui_parameters_present_flag == 1)
            {
                // Vui parameters.
                VuiParameters vuiParameters = H264Utilities.vui_parameters(bitStream);
            }
            return Sps;
        }
    }
}

/// <summary>
/// This part of the H264Utilities contains the methods are related to reading from the stream and descriptors.
/// </summary>
public static partial class H264Utilities
{
    public static uint read_bits(this BitList bitList, uint BitCounter)
    {
        uint value = BitCounter > 0 ? bitList.Read(BitCounter) : 0;
        return value;
    }

    public static uint ue(this BitList bitStream)
    {
        uint CodeNum = 0;
        try
        {
            int leadingZeroBits = -1;
            for (bool b = false; !b; leadingZeroBits++)
            {
                Console.WriteLine(@"leadingZero: {0}, bitStream.Length: {1}, bitStream.Position: {2}", leadingZeroBits, bitStream.Length, bitStream.Position);
                if (!bitStream.more_rbsp_data())
                {
                    leadingZeroBits = 0;
                    break;
                }
                uint byteValue = bitStream.read_bits(1);
                Console.WriteLine(@"After: {0}, bitStream.Length: {1}, bitStream.Position: {2}", leadingZeroBits, bitStream.Length * 8, bitStream.Position);
                Console.WriteLine(@"byteValue: {0}", byteValue);
                b = byteValue == 1;
            }
            Console.WriteLine(@"leadingZero After: {0}", leadingZeroBits);
            uint leadingZerosInt = bitStream.more_rbsp_data() == true ? bitStream.read_bits((uint)leadingZeroBits) : 0;
            CodeNum = (uint)((uint)Math.Pow(2, leadingZeroBits) - 1 + leadingZerosInt);
        }
        catch (System.Exception)
        {
            throw;
        }
        return CodeNum;
    }

    /// <summary>
    /// The scaling list method
    /// </summary>
    /// <param name="bitStream"></param>
    /// <param name="scalingList"></param>
    /// <param name="sizeOfScalingList"></param>
    /// <param name="useDefaultScalingMatrixFlag"></param>
    /// <returns>An Array of Scaling list values.</returns>
    public static int[] scaling_list(this BitList bitStream, int[] scalingList, int sizeOfScalingList, bool useDefaultScalingMatrixFlag)
    {
        try
        {
            int lastScale = 8;
            int nextScale = 8;

            for (int j = 0; j < sizeOfScalingList; j++)
            {
                if (nextScale != 0)
                {
                    int delta_scale = bitStream.se();
                    nextScale = (lastScale + delta_scale + 256) % 256;
                    useDefaultScalingMatrixFlag = j == 0 && nextScale == 0;
                }
                scalingList[j] = (nextScale == 0) ? lastScale : nextScale;
                lastScale = scalingList[j];
            }
            return scalingList;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static bool more_rbsp_data(this BitList bitStream)
    {
        try
        {
            if (bitStream.Position < bitStream.Length)
            {
                return true;
            }
            return false;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static int se(this BitList bitStream)
    {
        try
        {
            uint CodeNum = bitStream.ue();
            int SignedNum = (int)Math.Pow(-1, CodeNum + 1) * (int)Math.Ceiling((decimal)(CodeNum / 2));
            return SignedNum;
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}

public static partial class H264Utilities
{
    /// <summary>
    /// The method that will search for the start code prefixex
    /// </summary>
    /// <param name="StartCode">The bytes to be tested to determine whether its a start code or not.</param>
    /// <returns></returns>
    private static (bool isStartCode, int NumberOfBytes) GetStartCode(byte[] StartCode)
    {
        using (MemoryStream memoryStream = new MemoryStream(StartCode))
        {
            bool IsStartCode = false;
            int NumberOfBytes = 0;
            if ((StartCode[2] == 0x01 || StartCode[2] == 0x02))
            {
                if (StartCode[0] == 0x00 && StartCode[1] == 0x00)
                {
                    NumberOfBytes = 3;
                    IsStartCode = true;

                    return (IsStartCode, NumberOfBytes);
                }
            }
            else if ((StartCode[2] == 0x00) || (StartCode[2] == 0x03))
            {
                if (StartCode[0] == 0x00 && StartCode[1] == 0x00 &&
                ((StartCode[3] == 0x00) || (StartCode[3] == 0x01) ||
                (StartCode[3] == 0x02) || (StartCode[3] == 0x03)))
                {
                    NumberOfBytes = 4;
                    IsStartCode = true;

                    return (IsStartCode, NumberOfBytes);
                }
            }
            return (IsStartCode, NumberOfBytes);
        }
    }

    /// <summary>
    /// This method to grab the frame from the byte stream.
    /// </summary>
    /// <param name="sampleTable">This sample table that contains the information related to locating the samples in a stream.</param>
    // private static void GetFrame(Stbl sampleTable)
    // {
    //     try
    //     {
    //         int FrameNumber = 1;
    //         List<uint> SizesList = sampleTable.Stsz.EntrySizeList.Take(FrameNumber).ToList();
    //         List<uint> ChunkAddressList = sampleTable.Stco.ChunkOffsets;
    //         List<Stsc.ChunkTable> FramesinChunkList = sampleTable.Stsc.ChunkTableEntries;

    //         int ChunkNo = 0;
    //         uint totalFrames = 0;

    //         while (totalFrames < FrameNumber)
    //         {
    //             // int NumberOfChunks = (int)FramesinChunkList[ChunkNo + 1 < FramesinChunkList.ToArray().Length ? 
    //             // ChunkNo + 1 : FramesinChunkList.ToArray().Length - 1].FirstChunk - 
    //             // (int)FramesinChunkList[ChunkNo - 1 < 0 ? 0 : ChunkNo].FirstChunk;

    //             totalFrames = totalFrames + FramesinChunkList[ChunkNo].SamplePerChunk;

    //             // for (int ChunkIndex = 0; ChunkIndex < NumberOfChunks; ChunkIndex++)
    //             // {
    //             //     totalFrames = totalFrames + (int)(FramesinChunkList[ChunkNo].SamplePerChunk);
    //             // }                  
    //             ChunkNo++;
    //         }

    //         uint NumOfFramesInChunk = FramesinChunkList[ChunkNo - 1].SamplePerChunk;
    //         uint FirstFrameInChunk = totalFrames - NumOfFramesInChunk;
    //         uint StartAddress = ChunkAddressList[ChunkNo - 1];

    //         for (uint i = 0; i < NumOfFramesInChunk; i++)
    //         {
    //             if (FirstFrameInChunk + i == FrameNumber)
    //             {
    //                 break;
    //             }
    //             else
    //             {
    //                 StartAddress = StartAddress + SizesList[(int)(FirstFrameInChunk + i)];
    //             }
    //         }

    //         string FileName = Path.Combine(Environment.CurrentDirectory, @"Mp4File\Program.cs - ReadingMp4 File - Visual Studio Code 2023-05-02 11-09-25.mp4");
    //         using (FileStream fileStream = new FileStream(FileName, FileMode.Open,
    //         FileAccess.Read, FileShare.ReadWrite))
    //         {
    //             fileStream.Position = StartAddress;
    //             byte[] FrameBuffer = new byte[SizesList[FrameNumber - 1]];
    //             fileStream.Read(FrameBuffer, 0, FrameBuffer.Length);
    //             List<NALUnit> NalUnitsList = new List<NALUnit>();

    //             // Now we have the NAL Unit.
    //             byte[] NALUnitHeaderBuffer;
    //             byte[] StartCode;
    //             MemoryStream memoryStream = new MemoryStream(FrameBuffer);

    //             while (memoryStream.Position < memoryStream.Length)
    //             {
    //                 StartCode = new byte[4];
    //                 memoryStream.Read(StartCode, 0, StartCode.Length);
    //                 (bool IsStartCode, int NumberOfBytes) CheckStartCode = GetStartCode(StartCode);
    //                 if (CheckStartCode.IsStartCode)
    //                 {
    //                     NALUnit NalUnit = new NALUnit();
    //                     NalUnit.Start = memoryStream.Position;
    //                     NALUnit? previousNal = NalUnitsList.Find(nal => nal.End == 0);

    //                     if (previousNal != null)
    //                     {
    //                         previousNal.End = memoryStream.Position - 1;
    //                         previousNal.NalUnitLength = (ulong)(previousNal.End - previousNal.Start);
    //                         long CurrentPos = memoryStream.Position;

    //                         if (previousNal.NalUnitType == 9)
    //                         {
    //                             AccessUnitDelimiter Aud = new AccessUnitDelimiter();
    //                             byte[] AudBuffer = new byte[previousNal.NalUnitLength];
    //                             memoryStream.Position = previousNal.Start;
    //                             memoryStream.Read(AudBuffer, 0, AudBuffer.Length);
    //                             memoryStream.Position = CurrentPos;
    //                             BitArray bitStream = new BitArray(AudBuffer);
    //                             Aud.access_unit_delimiter_rbsp(bitStream);
    //                         }

    //                         if (previousNal.NalUnitType == 4)
    //                         {
    //                             DataPartitionC dataPartitionC = new DataPartitionC();
    //                             byte[] partionCBuffer = new byte[previousNal.NalUnitLength];
    //                             memoryStream.Position = previousNal.Start;
    //                             memoryStream.Read(partionCBuffer, 0, partionCBuffer.Length);
    //                             memoryStream.Position = CurrentPos;
    //                             BitArray bitStream = new BitArray(partionCBuffer);
    //                             dataPartitionC.slice_data_partition_c_layer_rbsp(bitStream);
    //                         }
    //                     }
    //                     NALUnitHeaderBuffer = new byte[2];
    //                     NALUnitHeaderBuffer[0] = 0;
    //                     memoryStream.Read(NALUnitHeaderBuffer, 1, 1);

    //                     memoryStream.more_data_in_bystream();
    //                     NalUnit.ForbiddenZeroBit = (BitConverter.ToUInt16(NALUnitHeaderBuffer) >> 8) & 128;
    //                     NalUnit.NalRefIdc = (BitConverter.ToUInt16(NALUnitHeaderBuffer) >> 8) & 96;
    //                     NalUnit.NalUnitType = (BitConverter.ToUInt16(NALUnitHeaderBuffer) >> 8) & 31;
    //                     NalUnitsList.Add(NalUnit);
    //                 }
    //                 else
    //                 {
    //                     memoryStream.Position -= memoryStream.Position < memoryStream.Length ? 3 : 0;
    //                 }
    //             }
    //             string waitHere = string.Empty;
    //         }
    //     }
    //     catch (System.Exception)
    //     {
    //         throw;
    //     }
    // }

    /// <summary>
    /// The method calculate the sum of sizes of Picture Parameter Set.
    /// </summary>
    /// <param name="Psp">The bytes with all the sizes of PPS</param>
    /// <returns>The sum of PPS</returns>
    // public static long SetPPSNALUnit(byte[] Psp, int PPSCounter)
    // {
    //     try
    //     {
    //         using (MemoryStream memoryStream = new MemoryStream(Psp))
    //         {
    //             List<long> PictureParameterSets = new List<long>();
    //             for (int i = 0; i < PPSCounter; i++)
    //             {
    //                 byte[] PictureParameterSetLengthBuffer = new byte[2];
    //                 memoryStream.Read(PictureParameterSetLengthBuffer, 0, PictureParameterSetLengthBuffer.Length);
    //                 Array.Reverse(PictureParameterSetLengthBuffer);

    //                 ushort PictureParameterSetLength = BitConverter.ToUInt16(PictureParameterSetLengthBuffer, 0);
    //                 long PictureParameterSetNALUnit = 8 * PictureParameterSetLength;

    //                 PictureParameterSets.Add(PictureParameterSetNALUnit);
    //             }
    //             return this.PictureParameterSets.Sum();
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         throw new Exception("Error computing PPS size or length.");
    //     }
    // }
}