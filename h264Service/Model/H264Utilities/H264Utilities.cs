using System;
using System.Collections;
using mp4.boxes;
using h264.NALUnits;
using H264Utilities.Descriptors;
using Decoder.H264ArrayParsers;
using H264Utilities.Parsers;
using h264.syntaxstructures;
using H264.Global.Variables;
using System.Text.Json;
using H264.Types;

namespace h264.utilities;
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
/// This part of the H264Utilities contains the methods are related to reading from the stream and descriptors.
/// </summary>
public static partial class Descriptors
{
    /// <summary>
    /// The scaling list method
    /// </summary>
    /// <param name="bitStream"></param>
    /// <param name="scalingList"></param>
    /// <param name="sizeOfScalingList"></param>
    /// <param name="useDefaultScalingMatrixFlag"></param>
    /// <returns>An Array of Scaling list values.</returns>
    public static int[,] scaling_list(this BitList bitStream, int[,] scalingList, int scaleIndex, int sizeOfScalingList, bool useDefaultScalingMatrixFlag)
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
                scalingList[scaleIndex, j] = (nextScale == 0) ? lastScale : nextScale;
                lastScale = scalingList[scaleIndex, j];
            }
            return scalingList;
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
    public static (bool isStartCode, int NumberOfBytes) GetStartCode(byte[] StartCode)
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

    private static void CreateSettingsFile(AVCDecoderConfigurationRecord aVCDecoderRecords)
    {
        try
        {
            if (!Directory.Exists(@"C:\H264Decoder\h264Service\Data\codecsetting.json"))
            {
                using (StreamWriter streamWriter = new StreamWriter(@"C:\H264Decoder\h264Service\Data\codecsetting.json"))
                {
                    SettingSets settingSets = new SettingSets();
                    settingSets.GetPPS = aVCDecoderRecords.GetPPS;
                    settingSets.GetSPS = aVCDecoderRecords.GetSPS;
                    settingSets.GlobalVariables = new GlobalVariables();

                    string jsonSettings = JsonSerializer.Serialize(settingSets);
                    streamWriter.Write(jsonSettings);
                }
            }
            else
            {
                string currentSetting = string.Empty;
                using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\codecsetting.json"))
                {
                    SettingSets? settingSets = JsonSerializer.Deserialize<SettingSets>(streamReader.ReadToEnd());
                    if (settingSets != null)
                    {
                        if (!settingSets.GetPPS.Equals(aVCDecoderRecords.GetPPS))
                        {
                            settingSets.GetPPS = aVCDecoderRecords.GetPPS;
                        }

                        if (!settingSets.GetSPS.Equals(aVCDecoderRecords.GetSPS))
                        {
                            settingSets.GetSPS = aVCDecoderRecords.GetSPS;
                        }
                        currentSetting = JsonSerializer.Serialize(settingSets);
                    } else
                    {
                        settingSets = new SettingSets();
                        settingSets.GetPPS = aVCDecoderRecords.GetPPS;
                        settingSets.GetSPS = aVCDecoderRecords.GetSPS;
                        settingSets.GlobalVariables = new GlobalVariables();
                    }
                }

                using (StreamWriter streamWriter = new StreamWriter(@"C:\H264Decoder\h264Service\Data\codecsetting.json"))
                {
                    streamWriter.Write(currentSetting);
                }
            }
        }
        catch (System.Exception)
        {            
            throw;
        }
    }


    /// <summary>
    /// This method to grab the frame from the byte stream.
    /// </summary>
    /// <param name="sampleTable">This sample table that contains the information related to locating the samples in a stream.</param>
    public static void GetFrame(Stbl sampleTable)
    {
        try
        {
            int FrameNumber = 1;
            List<uint> SizesList = sampleTable.Stsz.EntrySizeList.Take(FrameNumber).ToList();
            List<uint> ChunkAddressList = sampleTable.Stco.ChunkOffsets;
            List<Stsc.ChunkTable> FramesinChunkList = sampleTable.Stsc.ChunkTableEntries;

            int ChunkNo = 0;
            uint totalFrames = 0;

            while (totalFrames < FrameNumber)
            {
                // int NumberOfChunks = (int)FramesinChunkList[ChunkNo + 1 < FramesinChunkList.ToArray().Length ? 
                // ChunkNo + 1 : FramesinChunkList.ToArray().Length - 1].FirstChunk - 
                // (int)FramesinChunkList[ChunkNo - 1 < 0 ? 0 : ChunkNo].FirstChunk;

                totalFrames = totalFrames + FramesinChunkList[ChunkNo].SamplePerChunk;

                // for (int ChunkIndex = 0; ChunkIndex < NumberOfChunks; ChunkIndex++)
                // {
                //     totalFrames = totalFrames + (int)(FramesinChunkList[ChunkNo].SamplePerChunk);
                // }                  
                ChunkNo++;
            }

            uint NumOfFramesInChunk = FramesinChunkList[ChunkNo - 1].SamplePerChunk;
            uint FirstFrameInChunk = totalFrames - NumOfFramesInChunk;
            uint StartAddress = ChunkAddressList[ChunkNo - 1];

            for (uint i = 1; i <= NumOfFramesInChunk; i++)
            {
                if (FirstFrameInChunk + i == FrameNumber)
                {
                    break;
                }
                else
                {
                    StartAddress = StartAddress + SizesList[(int)(FirstFrameInChunk + (i - 1))];
                }
            }

            string FileName = string.Format(@"C:\H264Decoder\SegmentFile\Data\ftype.mp4");
            using (FileStream fileStream = new FileStream(FileName, FileMode.Open,
            FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Position = StartAddress;
                byte[] FrameBuffer = new byte[SizesList[FrameNumber - 1]];
                fileStream.Read(FrameBuffer, 0, FrameBuffer.Length);
                List<NALUnit> NalUnitsList = new List<NALUnit>();
                H264Parsers h264Parsers = new H264Parsers();
                AVCDecoderConfigurationRecord aVCDecoderRecords = sampleTable.Stsd.GetAVCDecoderConfiguration;
                CreateSettingsFile(aVCDecoderRecords);

                // Now we have the NAL Unit.
                MemoryStream memoryStream = new MemoryStream(FrameBuffer);

                NALUnit NalUnit;
                while (memoryStream.Position < memoryStream.Length)
                {
                    byte[] SizeBuffer = new byte[4];
                    memoryStream.Read(SizeBuffer, 0, SizeBuffer.Length);
                    Array.Reverse(SizeBuffer);
                    int SizeInt = BitConverter.ToInt32(SizeBuffer, 0);                    
                    byte[] NalUnitBuffer = new byte[SizeInt];
                    memoryStream.Read(NalUnitBuffer, 0, NalUnitBuffer.Length);

                    NalUnit = H264Parsers.nal_unit(NalUnitBuffer, NalUnitBuffer.Length);

                    if (NalUnit.NalUnitType == 9)
                    {
                        AccessUnitDelimiter accessUnitDelimiter = H264Parsers.access_unit_delimiter_rbsp(NalUnit.rbsp_byte);
                    }
                    if (NalUnit.NalUnitType == 1 || NalUnit.NalUnitType == 5)
                    {
                        BitList bitStream = new BitList(NalUnit.rbsp_byte);
                        SliceHeader sliceHeader = h264Parsers.get_slice_header(bitStream, NalUnit);
                        SliceData sliceData = h264Parsers.get_slice_data(bitStream, sliceHeader);
                    }
                    NalUnitsList.Add(NalUnit);
                }
                string waitHere = string.Empty;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}