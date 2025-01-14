using System;
using mp4.boxes;
using h264.NALUnits;
namespace h264.utilities;

public static class H264Utilities
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

    /// <summary>
    /// The method calculate the sum of the sizes of all the sequency parameter set available in your byte stream.
    /// </summary>
    /// <param name="Sps">Bytes with all the Sequency Parameter Set</param>
    /// <returns>The sum of sizes of the SPS</returns>
//     public static long SetSPSNALUnit(byte[] Sps)
//     {
//         try
//         {
//             using (MemoryStream memoryStream = new MemoryStream(Sps))
//             {
//                 this.SequencyParameterSets = new List<long>();
//                 for (int i = 0; i < this.NumberOfSequenceParameterSets; i++)
//                 {
//                     byte[] SequencyParameterSetLengthBuffer = new byte[2];
//                     memoryStream.Read(SequencyParameterSetLengthBuffer, 0, SequencyParameterSetLengthBuffer.Length);
//                     Array.Reverse(SequencyParameterSetLengthBuffer);

//                     ushort SequencyParameterSetLength = BitConverter.ToUInt16(SequencyParameterSetLengthBuffer, 0);
//                     long SequenceParameterSetNALUnit = (8 * SequencyParameterSetLength);

//                     SequencyParameterSets.Add(SequenceParameterSetNALUnit);
//                 }
//                 return this.SequencyParameterSets.Sum();
//             }
//         }
//         catch (Exception ex)
//         {
//             throw new Exception("There was an error calculating the sum of SPS.");
//         }
//     }
}