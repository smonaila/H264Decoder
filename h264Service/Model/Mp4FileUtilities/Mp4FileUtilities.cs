using System;
using h264.NALUnits;
using h264.utilities;
using H264Utilities.Descriptors;
using H264Utilities.Parsers;
using mp4.boxes;
using H264_Utilities = h264.utilities.H264Utilities;

namespace mp4.utilities;

public static class Mp4FileUtilities
{
    public static string GetCurrentNumberedDirectory(string path, string boxName)
    {
        string currentMultiDirectory = string.Empty;
        try
        {
            string traksDirectory = string.Format("{0}", path);
            var traksDirectories = (from dir in Directory.GetDirectories(traksDirectory)
                                    where dir.Replace(traksDirectory, "").Split("_")[0].Equals(boxName)
                                    select dir).ToList();

            int trakIndex = traksDirectories.Count > 0 ? (from dir in traksDirectories
                                                          select int.Parse(dir.Replace(traksDirectory, "").Split("_")[1])).Max() : 1;
            currentMultiDirectory = string.Format(@"{0}\{1}", traksDirectory, string.Format("{0}_{1}", boxName, trakIndex));
        }
        catch (System.Exception)
        {
            throw;
        }
        return currentMultiDirectory;
    }

    public static List<BoxInf> GetChildrens(byte[] Atom)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(Atom))
            {
                int ByteOffset = 8;
                List<BoxInf> ChildBoxes = new List<BoxInf>();
                while (memoryStream.Length > ByteOffset)
                {
                    memoryStream.Position = ByteOffset;
                    byte[] NameBuffer = new byte[4];
                    byte[] SizeBuffer = new byte[4];

                    memoryStream.Read(SizeBuffer, 0, SizeBuffer.Length);
                    memoryStream.Read(NameBuffer, 0, NameBuffer.Length);

                    Array.Reverse(SizeBuffer);
                    int Size = BitConverter.ToInt32(SizeBuffer, 0);
                    string BoxName = string.Empty;

                    for (int i = 0; i < NameBuffer.Length; i++)
                    {
                        BoxName += string.Format("{0}", Convert.ToChar(NameBuffer[i]));
                    }

                    BoxInf BoxInf = new BoxInf();
                    BoxInf.Name = BoxName;
                    BoxInf.Size = Size;
                    BoxInf.Offset = ByteOffset;

                    ByteOffset += BoxInf.Size;
                    ChildBoxes.Add(BoxInf);
                }
                return ChildBoxes;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static Ftyp GetFtyp(byte[] FtypBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(FtypBox))
            {
                byte[] SizeBuffer = new byte[4];
                byte[] NameBuffer = new byte[4];
                byte[] _MajorBrand = new byte[4];
                byte[] _MinorBrand = new byte[4];

                string BoxName = string.Empty;

                memoryStream.Read(SizeBuffer, 0, SizeBuffer.Length);
                memoryStream.Read(NameBuffer, 0, NameBuffer.Length);

                for (int i = 0; i < NameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(NameBuffer[i]));
                }

                Array.Reverse(SizeBuffer);
                Ftyp ftyp = new Ftyp(BitConverter.ToUInt32(SizeBuffer, 0), BoxName);
                memoryStream.Read(_MajorBrand, 0, _MajorBrand.Length);
                memoryStream.Read(_MinorBrand, 0, _MinorBrand.Length);

                string MajorBrandName = string.Empty;

                for (int i = 0; i < _MajorBrand.Length; i++)
                {
                    MajorBrandName += Convert.ToChar(_MajorBrand[i]);
                }
                int MinorBrandVersion = BitConverter.ToInt32(_MinorBrand, 0);

                string[] CompatibleBrands = new string[BitConverter.ToInt32(SizeBuffer, 0) - memoryStream.Position];

                for (int i = 0; i < CompatibleBrands.Length / 4; i += 4)
                {
                    byte[] Brand = new byte[4];
                    memoryStream.Read(Brand, 0, Brand.Length);
                    string currentBrand = string.Empty;
                    for (int j = 0; j < Brand.Length; j++)
                    {
                        currentBrand += Convert.ToChar(Brand[j]);
                    }
                    CompatibleBrands[i] = currentBrand;
                }
                ftyp.CompatibleBrands = CompatibleBrands;
                return ftyp;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static Mvhd GetMvhd(byte[] MvhdBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(MvhdBox))
            {
                byte[] BoxSizeBuffer = new byte[4];
                byte[] BoxNameBuffer = new byte[4];
                byte[] FlagsVersionBuffer = new byte[4];

                int Version = 0;
                int Flags = 0;
                string BoxName = string.Empty;

                memoryStream.Read(BoxSizeBuffer, 0, BoxSizeBuffer.Length);
                memoryStream.Read(BoxNameBuffer, 0, BoxNameBuffer.Length);
                memoryStream.Read(FlagsVersionBuffer, 0, FlagsVersionBuffer.Length);

                Array.Reverse(BoxSizeBuffer);
                for (int i = 0; i < BoxNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(BoxNameBuffer[i]));
                }

                Version = BitConverter.ToInt32(FlagsVersionBuffer, 0) >> 24;
                Flags = BitConverter.ToInt32(FlagsVersionBuffer, 0) << 8;
                Mvhd Mvhd = new Mvhd(BitConverter.ToUInt32(BoxSizeBuffer, 0), BoxName, BitConverter.ToInt32(FlagsVersionBuffer, 0));

                byte[] CreationTimeBuffer;

                DateTime CreationDate = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                if (Version == 0)
                {
                    CreationTimeBuffer = new byte[4];
                    memoryStream.Read(CreationTimeBuffer, 0, CreationTimeBuffer.Length);
                    Array.Reverse(CreationTimeBuffer);
                    uint CreationTimeStamp = BitConverter.ToUInt32(CreationTimeBuffer, 0);
                    CreationDate = CreationDate.AddSeconds(CreationTimeStamp);
                }
                else
                {
                    CreationTimeBuffer = new byte[8];
                    memoryStream.Read(CreationTimeBuffer, 0, CreationTimeBuffer.Length);

                    ulong CreationTimeStamp = BitConverter.ToUInt64(CreationTimeBuffer, 0);
                    CreationDate = CreationDate.AddSeconds(CreationTimeStamp);
                }

                return Mvhd;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }
    public static Moov GetMoov(byte[] MoovBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(MoovBox))
            {
                byte[] MoovBoxSizeBuffer = new byte[4];
                byte[] MoovBoxNameBuffer = new byte[4];

                string MoovBoxName = string.Empty;

                memoryStream.Read(MoovBoxSizeBuffer, 0, MoovBoxSizeBuffer.Length);
                memoryStream.Read(MoovBoxNameBuffer, 0, MoovBoxNameBuffer.Length);

                Array.Reverse(MoovBoxSizeBuffer);
                for (int i = 0; i < MoovBoxNameBuffer.Length; i++)
                {
                    MoovBoxName += string.Format("{0}", Convert.ToChar(MoovBoxNameBuffer[i]));
                }

                Moov Moov = new Moov(BitConverter.ToUInt32(MoovBoxSizeBuffer, 0), MoovBoxName);

                List<BoxInf> ChildBoxes = GetChildrens(MoovBox);
                BoxInf? MvhdInf = ChildBoxes.Find(cb => cb.Name == "mvhd");
                if (MvhdInf != null)
                {
                    memoryStream.Position = MvhdInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] MvhdBox = new byte[MvhdInf.Size];

                    memoryStream.Read(MvhdBox, 0, MvhdBox.Length);
                    Moov.Mvhd = GetMvhd(MvhdBox);
                }

                List<BoxInf>? TraksInf = ChildBoxes.Where(cb => cb.Name == "trak").ToList();
                Moov.Traks = new List<Trak>();
                if (TraksInf != null)
                {
                    foreach (var TrakInf in TraksInf)
                    {
                        memoryStream.Position = TrakInf.Offset;

                        byte[] BoxSizeBuffer = new byte[4];
                        byte[] BoxNameBuffer = new byte[4];
                        byte[] TrakBox = new byte[TrakInf.Size];

                        memoryStream.Read(TrakBox, 0, TrakBox.Length);
                        Moov.Traks.Add(GetTrak(TrakBox));
                    }
                }
                return Moov;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static Trak GetTrak(byte[] trakBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(trakBox))
            {
                byte[] TrakSizeBuffer = new byte[4];
                byte[] TrakNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                memoryStream.Read(TrakSizeBuffer, 0, TrakSizeBuffer.Length);
                memoryStream.Read(TrakNameBuffer, 0, TrakNameBuffer.Length);

                Array.Reverse(TrakSizeBuffer);

                List<BoxInf> Children = GetChildrens(trakBox);
                BoxInf? TkhdInf = Children.Find(c => c.Name == "tkhd");

                string BoxName = string.Empty;
                for (int i = 0; i < TrakNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(TrakNameBuffer[i]));
                }

                Trak Trak = new Trak(BitConverter.ToUInt32(TrakSizeBuffer), BoxName);
                if (TkhdInf != null)
                {
                    memoryStream.Position = TkhdInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] TkhdBox = new byte[TkhdInf.Size];
                    memoryStream.Read(TkhdBox, 0, TkhdBox.Length);

                    Trak.Tkhd = GetTkhd(TkhdBox);
                }

                BoxInf? MdiaInf = Children.Find(c => c.Name == "mdia");
                if (MdiaInf != null)
                {
                    memoryStream.Position = MdiaInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] MdiaBox = new byte[MdiaInf.Size];

                    memoryStream.Read(MdiaBox, 0, MdiaBox.Length);
                    Trak.Mdia = GetMdia(MdiaBox);
                }
                return Trak;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    private static Mdia GetMdia(byte[] mdiaBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(mdiaBox))
            {
                byte[] MdiaSizeBuffer = new byte[4];
                byte[] MdiaNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                memoryStream.Read(MdiaSizeBuffer, 0, MdiaSizeBuffer.Length);
                memoryStream.Read(MdiaNameBuffer, 0, MdiaNameBuffer.Length);

                Array.Reverse(MdiaSizeBuffer);

                List<BoxInf> Children = GetChildrens(mdiaBox);
                BoxInf? MdhdInf = Children.Find(c => c.Name == "mdhd");

                string BoxName = string.Empty;
                for (int i = 0; i < MdiaNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(MdiaNameBuffer[i]));
                }
                Mdia Mdia = new Mdia(BitConverter.ToUInt32(MdiaSizeBuffer), BoxName);
                BoxInf? MinfInf = Children.Find(c => c.Name == "minf");
                if (MinfInf != null)
                {
                    memoryStream.Position = MinfInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] MinfBox = new byte[MinfInf.Size];

                    memoryStream.Read(MinfBox, 0, MinfBox.Length);
                    Mdia.Minf = GetMinf(MinfBox);
                }
                return Mdia;
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    private static Minf GetMinf(byte[] minfBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(minfBox))
            {
                byte[] MinfSizeBuffer = new byte[4];
                byte[] MinfNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                memoryStream.Read(MinfSizeBuffer, 0, MinfSizeBuffer.Length);
                memoryStream.Read(MinfNameBuffer, 0, MinfNameBuffer.Length);

                Array.Reverse(MinfSizeBuffer);

                List<BoxInf> Children = GetChildrens(minfBox);
                BoxInf? StblInf = Children.Find(c => c.Name == "stbl");
                string BoxName = string.Empty;
                for (int i = 0; i < MinfNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(MinfNameBuffer[i]));
                }

                Minf Minf = new Minf(BitConverter.ToUInt32(MinfSizeBuffer), BoxName);
                if (StblInf != null)
                {
                    memoryStream.Position = StblInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] StblBox = new byte[StblInf.Size];

                    memoryStream.Read(StblBox, 0, StblBox.Length);
                    Minf.Stbl = GetStbl(StblBox);
                }

                return Minf;
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public static Stbl GetStbl(byte[] stblBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(stblBox))
            {
                byte[] StblSizeBuffer = new byte[4];
                byte[] StblNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                memoryStream.Read(StblSizeBuffer, 0, StblSizeBuffer.Length);
                memoryStream.Read(StblNameBuffer, 0, StblNameBuffer.Length);

                Array.Reverse(StblSizeBuffer);

                List<BoxInf> Children = GetChildrens(stblBox);
                string BoxName = string.Empty;
                for (int i = 0; i < StblNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(StblNameBuffer[i]));
                }

                Stbl Stbl = new Stbl(BitConverter.ToUInt32(StblSizeBuffer), BoxName);
                BoxInf? StsdInf = Children.Find(c => c.Name == "stsd");
                BoxInf? SttsInf = Children.Find(c => c.Name == "stts");
                BoxInf? StscInf = Children.Find(c => c.Name == "stsc");
                BoxInf? StcoInf = Children.Find(c => c.Name == "stco");
                BoxInf? CttsInf = Children.Find(c => c.Name == "ctts");
                BoxInf? StszInf = Children.Find(c => c.Name == "stsz");
                BoxInf? Stz2Inf = Children.Find(c => c.Name == "Stz2");

                if (StsdInf != null)
                {
                    memoryStream.Position = StsdInf.Offset;
                    byte[] StsdBox = new byte[StsdInf.Size];
                    memoryStream.Read(StsdBox, 0, StsdBox.Length);
                    Stbl.Stsd = GetStsd(StsdBox);
                }

                if (SttsInf != null)
                {
                    memoryStream.Position = SttsInf.Offset;
                    byte[] SttsBox = new byte[SttsInf.Size];
                    memoryStream.Read(SttsBox, 0, SttsBox.Length);
                    Stbl.Stts = GetStts(SttsBox);
                }

                if (StscInf != null)
                {
                    memoryStream.Position = StscInf.Offset;
                    byte[] StscBox = new byte[StscInf.Size];
                    memoryStream.Read(StscBox, 0, StscBox.Length);
                    Stbl.Stsc = GetStsc(StscBox);
                }

                if (StcoInf != null)
                {
                    memoryStream.Position = StcoInf.Offset;
                    byte[] StcoBox = new byte[StcoInf.Size];

                    memoryStream.Read(StcoBox, 0, StcoBox.Length);
                    Stbl.Stco = GetStco(StcoBox);
                }

                if (CttsInf != null)
                {
                    memoryStream.Position = CttsInf.Offset;
                    byte[] CttsBox = new byte[CttsInf.Size];

                    memoryStream.Read(CttsBox, 0, CttsBox.Length);
                    Stbl.Ctts = GetCtts(CttsBox);
                }

                if (StszInf != null)
                {
                    memoryStream.Position = StszInf.Offset;
                    byte[] StszBox = new byte[StszInf.Size];

                    memoryStream.Read(StszBox, 0, StszBox.Length);
                    Stbl.Stsz = GetStsz(StszBox);
                }
                H264_Utilities.GetFrame(Stbl);
                return Stbl;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

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
    public static Stsz GetStsz(byte[] stszBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(stszBox))
            {
                byte[] Infor = new byte[12];
                byte[] SampleCountBuffer = new byte[4];
                byte[] SampleSizeBuffer = new byte[4];

                uint SampleSize = 0;
                uint SampleCount = 0;

                memoryStream.Read(Infor, 0, Infor.Length);
                UintSizeBoxInfo StszInfo = GetBoxInfo(Infor);

                memoryStream.Position = 12;
                memoryStream.Read(SampleSizeBuffer, 0, SampleSizeBuffer.Length);
                memoryStream.Read(SampleCountBuffer, 0, SampleCountBuffer.Length);

                Array.Reverse(SampleCountBuffer);
                Array.Reverse(SampleSizeBuffer);
                SampleSize = BitConverter.ToUInt32(SampleSizeBuffer, 0);
                SampleCount = BitConverter.ToUInt32(SampleCountBuffer, 0);

                Stsz Stsz = new Stsz(StszInfo.BoxSize, StszInfo.Name!);
                Stsz.EntrySizeList = new List<uint>();
                if (SampleSize == 0)
                {
                    for (int i = 0; i < SampleCount; i++)
                    {
                        byte[] EntrySizeBuffer = new byte[4];
                        uint EntrySize = 0;

                        memoryStream.Read(EntrySizeBuffer, 0, EntrySizeBuffer.Length);
                        Array.Reverse(EntrySizeBuffer);
                        EntrySize = BitConverter.ToUInt32(EntrySizeBuffer, 0);
                        Stsz.EntrySizeList.Add(EntrySize);
                    }
                }
                return Stsz;
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public static Stsd GetStsd(byte[] stsdBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(stsdBox))
            {
                byte[] Infor = new byte[12];
                byte[] EntryCountBuffer = new byte[4];
                uint EntryCount = 0;

                memoryStream.Read(Infor, 0, Infor.Length);
                UintSizeBoxInfo StsdInfo = GetBoxInfo(Infor);

                memoryStream.Position = 12;
                memoryStream.Read(EntryCountBuffer, 0, EntryCountBuffer.Length);
                Array.Reverse(EntryCountBuffer);
                EntryCount = BitConverter.ToUInt32(EntryCountBuffer, 0);

                string[] HandlerType = new string[] { "avc1", "mp4a" };

                Stsd Stsd = new Stsd(StsdInfo.BoxSize, StsdInfo.Name!);

                for (int i = 0; i < EntryCount; i++)
                {
                    byte[] SampleDescriptionSizeBuffer = new byte[4];
                    byte[] DataFormatBuffer = new byte[4];

                    memoryStream.Read(SampleDescriptionSizeBuffer, 0, SampleDescriptionSizeBuffer.Length);
                    memoryStream.Read(DataFormatBuffer, 0, DataFormatBuffer.Length);

                    memoryStream.Position += 6;

                    Array.Reverse(SampleDescriptionSizeBuffer);

                    int SampleDescriptionSize = BitConverter.ToInt32(SampleDescriptionSizeBuffer, 0);
                    string DataFormat = string.Empty;

                    for (int formatIndex = 0; formatIndex < DataFormatBuffer.Length; formatIndex++)
                    {
                        DataFormat += string.Format("{0}", Convert.ToChar(DataFormatBuffer[formatIndex]));
                    }

                    switch (DataFormat)
                    {
                        case "avc1":
                            byte[] WidthBuffer = new byte[2];
                            byte[] HeightBuffer = new byte[2];
                            byte[] DataReferenceIndexBuffer = new byte[2];
                            byte[] HorizResBuffer = new byte[4];
                            byte[] VertResBuffer = new byte[4];
                            byte[] FrameCountBuffer = new byte[2];

                            memoryStream.Read(DataReferenceIndexBuffer, 0, DataReferenceIndexBuffer.Length);
                            Array.Reverse(DataReferenceIndexBuffer);
                            ushort DataReferenceIndex = BitConverter.ToUInt16(DataReferenceIndexBuffer, 0);

                            memoryStream.Position += 2;
                            memoryStream.Position += 2;
                            memoryStream.Position += 12;

                            memoryStream.Read(WidthBuffer, 0, WidthBuffer.Length);
                            memoryStream.Read(HeightBuffer, 0, HeightBuffer.Length);
                            memoryStream.Read(HorizResBuffer, 0, HorizResBuffer.Length);
                            memoryStream.Read(VertResBuffer, 0, VertResBuffer.Length);

                            Array.Reverse(WidthBuffer);
                            Array.Reverse(HeightBuffer);

                            ushort Width = BitConverter.ToUInt16(WidthBuffer, 0);
                            ushort Height = BitConverter.ToUInt16(HeightBuffer, 0);
                            ushort HorizResolution = BitConverter.ToUInt16(HorizResBuffer, 1);
                            ushort VertResolution = BitConverter.ToUInt16(VertResBuffer, 1);

                            memoryStream.Position += 4;
                            memoryStream.Read(FrameCountBuffer, 0, FrameCountBuffer.Length);
                            Array.Reverse(FrameCountBuffer);
                            ushort FrameCount = BitConverter.ToUInt16(FrameCountBuffer, 0);

                            memoryStream.Position += 32;
                            memoryStream.Position += 2;
                            memoryStream.Position += 2;

                            Stsd.Video = new VisualSampleEntry("avc1");
                            Stsd.Video.FrameCount = FrameCount;
                            Stsd.Video.DataReferenceIndex = DataReferenceIndex;
                            Stsd.HandlerType = Stsd.StreamType.Video;
                            Stsd.Video.Height = Height;
                            Stsd.Video.Width = Width;
                            Stsd.Video.HorizResolution = HorizResolution;
                            Stsd.Video.VertResolution = VertResolution;

                            if (memoryStream.Position < memoryStream.Length)
                            {
                                byte[] AvcCSizeBuffer = new byte[4];
                                byte[] AvcCNameBuffer = new byte[4];

                                memoryStream.Read(AvcCSizeBuffer, 0, AvcCSizeBuffer.Length);
                                memoryStream.Read(AvcCNameBuffer, 0, AvcCNameBuffer.Length);

                                Array.Reverse(AvcCSizeBuffer);
                                uint AvcCSize = BitConverter.ToUInt32(AvcCSizeBuffer, 0);

                                string Name = string.Empty;
                                for (int conIndex = 0; conIndex < AvcCNameBuffer.Length; conIndex++)
                                {
                                    Name += string.Format("{0}", Convert.ToChar(AvcCNameBuffer[conIndex]));
                                }

                                byte[] DecorderConfigurationRecord = new byte[4];
                                byte[] ParameterSetsBuffer = new byte[2];
                                byte[] SequenceSetsBuffer = new byte[2];
                                byte[] LengthMinusSizeBuffer = new byte[2];

                                LengthMinusSizeBuffer[1] = 0;
                                SequenceSetsBuffer[1] = 0;
                                ParameterSetsBuffer[1] = 0;

                                memoryStream.Read(DecorderConfigurationRecord, 0, DecorderConfigurationRecord.Length);
                                memoryStream.Read(LengthMinusSizeBuffer, 0, 1);
                                memoryStream.Read(SequenceSetsBuffer, 0, 1);

                                Array.Reverse(DecorderConfigurationRecord);

                                int DecorderConfigurations = BitConverter.ToInt32(DecorderConfigurationRecord, 0);

                                short SequenceSets = BitConverter.ToInt16(SequenceSetsBuffer, 0);
                                short LengthMinusSize = BitConverter.ToInt16(LengthMinusSizeBuffer, 0);

                                AVCDecoderConfigurationRecord aVCDecoderConfiguration = new AVCDecoderConfigurationRecord(AvcCSize, Name);
                                aVCDecoderConfiguration.ConfigurationVersion = (short)(((DecorderConfigurations >> 24)) & 255);
                                aVCDecoderConfiguration.AVCProfileIndication = (short)(((DecorderConfigurations << 8) >> 24) & 255);
                                aVCDecoderConfiguration.ProfileCompatibility = (short)(((((DecorderConfigurations << 16) >> 24))) & 255);
                                aVCDecoderConfiguration.AVCLevelIndication = (short)((((DecorderConfigurations << 24) >> 24) & 255));
                                aVCDecoderConfiguration.LengthSizeMinusOne = (short)((LengthMinusSize & 3));
                                aVCDecoderConfiguration.NumberOfSequenceParameterSets = (short)(SequenceSets & 31);

                                // Sequence Parameter Sets
                                byte[] SequenceSetsBytes = new byte[2 * aVCDecoderConfiguration.NumberOfSequenceParameterSets];

                                memoryStream.Read(SequenceSetsBytes, 0, SequenceSetsBytes.Length);
                                long NalUnitLength = aVCDecoderConfiguration.SetSPSNALUnit(SequenceSetsBytes) / 8;

                                byte[] NalUnitRBSP = new byte[NalUnitLength];
                                memoryStream.Read(NalUnitRBSP, 0, NalUnitRBSP.Length);

                                NALUnit NalUnit = H264Parsers.nal_unit(NalUnitRBSP, NalUnitLength);
                                H264Parsers h264Parsers = new H264Parsers();
                                SPS? SPS = null;
                                PPS? PPS = null;

                                if ((NalUnit.NalUnitType & 31) == 7)
                                {
                                    SPS = h264Parsers.seq_parameter_set_rbsp(NalUnit.rbsp_byte);
                                    aVCDecoderConfiguration.GetSPS = SPS;
                                }                                

                                memoryStream.Read(ParameterSetsBuffer, 0, 1);
                                short ParameterSets = BitConverter.ToInt16(ParameterSetsBuffer, 0);
                                aVCDecoderConfiguration.NumberOfParameterSets = ParameterSets;

                                byte[] ParameterSetsBytes = new byte[2 * aVCDecoderConfiguration.NumberOfParameterSets];
                                memoryStream.Read(ParameterSetsBytes, 0, ParameterSetsBytes.Length);
                                NalUnitLength = aVCDecoderConfiguration.SetPPSNALUnit(ParameterSetsBytes) / 8;

                                NalUnitRBSP = new byte[NalUnitLength];
                                memoryStream.Read(NalUnitRBSP, 0, NalUnitRBSP.Length);

                                NalUnit = H264Parsers.nal_unit(NalUnitRBSP, NalUnitLength);
                                if (NalUnit.NalUnitType == 8)
                                {
                                    PPS = h264Parsers.pic_parameter_set_rbsp(NalUnit.rbsp_byte);
                                    aVCDecoderConfiguration.GetPPS = PPS;
                                }
                                Stsd.GetAVCDecoderConfiguration = aVCDecoderConfiguration;
                            }
                            break;
                    }
                }
                return Stsd;
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public static Ctts GetCtts(byte[] cttsBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(cttsBox))
            {
                byte[] Infor = new byte[12];
                byte[] EntryCountBuffer = new byte[4];
                uint EntryCount = 0;

                memoryStream.Read(Infor, 0, Infor.Length);
                UintSizeBoxInfo CttsInfo = GetBoxInfo(Infor);

                memoryStream.Position = 12;
                memoryStream.Read(EntryCountBuffer, 0, EntryCountBuffer.Length);
                Array.Reverse(EntryCountBuffer);
                EntryCount = BitConverter.ToUInt32(EntryCountBuffer, 0);
                Ctts Ctts = new Ctts(CttsInfo.BoxSize, CttsInfo.Name!);
                Ctts.CompositionOffsets = new List<Ctts.CompositionOffsetTable>();

                for (int i = 0; i < EntryCount; i++)
                {
                    byte[] SampleCountBuffer = new byte[4];
                    byte[] SampleOffsetBuffer = new byte[4];

                    Ctts.CompositionOffsetTable compositionOffsetTable = new Ctts.CompositionOffsetTable();
                    memoryStream.Read(SampleCountBuffer, 0, SampleCountBuffer.Length);
                    memoryStream.Read(SampleOffsetBuffer, 0, SampleOffsetBuffer.Length);
                    Array.Reverse(SampleCountBuffer);
                    Array.Reverse(SampleOffsetBuffer);
                    compositionOffsetTable.SampleCount = BitConverter.ToUInt32(SampleCountBuffer, 0);
                    compositionOffsetTable.SampleOffset = BitConverter.ToUInt32(SampleOffsetBuffer, 0);

                    Ctts.CompositionOffsets.Add(compositionOffsetTable);
                }
                return Ctts;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static Stco GetStco(byte[] stcoBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(stcoBox))
            {
                byte[] Infor = new byte[12];
                byte[] EntryCountBuffer = new byte[4];
                uint EntryCount = 0;

                memoryStream.Read(Infor, 0, Infor.Length);
                UintSizeBoxInfo StcoInfo = GetBoxInfo(Infor);

                memoryStream.Position = 12;
                memoryStream.Read(EntryCountBuffer, 0, EntryCountBuffer.Length);
                Array.Reverse(EntryCountBuffer);
                EntryCount = BitConverter.ToUInt32(EntryCountBuffer, 0);
                Stco Stco = new Stco(StcoInfo.BoxSize, StcoInfo.Name!);
                Stco.ChunkOffsets = new List<uint>();
                for (int i = 0; i < EntryCount; i++)
                {
                    byte[] ChunkOffsetBuffer = new byte[4];
                    memoryStream.Read(ChunkOffsetBuffer, 0, ChunkOffsetBuffer.Length);
                    Array.Reverse(ChunkOffsetBuffer);
                    uint ChunkOffset = BitConverter.ToUInt32(ChunkOffsetBuffer, 0);
                    Stco.ChunkOffsets.Add(ChunkOffset);
                }
                return Stco;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static Stsc GetStsc(byte[] stscBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(stscBox))
            {
                byte[] Infor = new byte[12];
                byte[] EntryCountBuffer = new byte[4];
                uint EntryCount = 0;

                memoryStream.Read(Infor, 0, Infor.Length);
                UintSizeBoxInfo StscInfo = GetBoxInfo(Infor);

                memoryStream.Position = 12;
                memoryStream.Read(EntryCountBuffer, 0, EntryCountBuffer.Length);

                Array.Reverse(EntryCountBuffer);
                EntryCount = BitConverter.ToUInt32(EntryCountBuffer);

                Stsc Stsc = new Stsc(StscInfo.BoxSize, StscInfo.Name!);
                Stsc.EntryCount = EntryCount;
                Stsc.ChunkTableEntries = new List<Stsc.ChunkTable>();
                for (int i = 0; i < EntryCount; i++)
                {
                    byte[] FirstChunkBuffer = new byte[4];
                    byte[] SamplePerChunkBuffer = new byte[4];
                    byte[] SampleDescriptionIndexBuffer = new byte[4];

                    memoryStream.Read(FirstChunkBuffer, 0, FirstChunkBuffer.Length);
                    memoryStream.Read(SamplePerChunkBuffer, 0, SamplePerChunkBuffer.Length);
                    memoryStream.Read(SampleDescriptionIndexBuffer, 0, SampleDescriptionIndexBuffer.Length);

                    Array.Reverse(FirstChunkBuffer);
                    Array.Reverse(SamplePerChunkBuffer);
                    Array.Reverse(SampleDescriptionIndexBuffer);

                    Stsc.ChunkTable ChunkTableEntries = new Stsc.ChunkTable();
                    ChunkTableEntries.FirstChunk = BitConverter.ToUInt32(FirstChunkBuffer, 0);
                    ChunkTableEntries.SamplePerChunk = BitConverter.ToUInt32(SamplePerChunkBuffer, 0);
                    ChunkTableEntries.SampleDescriptionIndex = BitConverter.ToUInt32(SampleDescriptionIndexBuffer, 0);

                    Stsc.ChunkTableEntries.Add(ChunkTableEntries);
                }
                return Stsc;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public static Stts GetStts(byte[] sttsBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(sttsBox))
            {
                byte[] Infor = new byte[12];
                byte[] EntryCountBuffer = new byte[4];
                uint EntryCount = 0;

                memoryStream.Read(Infor, 0, Infor.Length);
                UintSizeBoxInfo SttsInfo = GetBoxInfo(Infor);

                memoryStream.Position = 12;
                memoryStream.Read(EntryCountBuffer, 0, EntryCountBuffer.Length);

                Array.Reverse(EntryCountBuffer);
                EntryCount = BitConverter.ToUInt32(EntryCountBuffer, 0);

                Stts Stts = new Stts(SttsInfo.BoxSize, SttsInfo.Name!);
                Stts.EntryCount = EntryCount;
                Stts.TimeToSample = new List<Stts.TimeToSampleTable>();
                for (int i = 0; i < EntryCount; i++)
                {
                    byte[] SampleCountBuffer = new byte[4];
                    byte[] SampleDeltaBuffer = new byte[4];

                    memoryStream.Read(SampleCountBuffer, 0, SampleCountBuffer.Length);
                    memoryStream.Read(SampleDeltaBuffer, 0, SampleDeltaBuffer.Length);

                    Array.Reverse(SampleCountBuffer);
                    Array.Reverse(SampleDeltaBuffer);

                    Stts.TimeToSampleTable sampleTable = new Stts.TimeToSampleTable();
                    sampleTable.SampleCount = BitConverter.ToUInt32(SampleCountBuffer, 0);
                    sampleTable.SampleDelta = BitConverter.ToUInt32(SampleDeltaBuffer, 0);

                    Stts.TimeToSample.Add(sampleTable);
                }
                return Stts;
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    private static UintSizeBoxInfo GetBoxInfo(byte[] infor)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(infor))
            {
                byte[] BoxSizeBuffer = new byte[4];
                byte[] BoxNameBuffer = new byte[4];

                string BoxName = string.Empty;

                memoryStream.Read(BoxSizeBuffer, 0, BoxSizeBuffer.Length);
                memoryStream.Read(BoxNameBuffer, 0, BoxNameBuffer.Length);

                Array.Reverse(BoxSizeBuffer);

                for (int i = 0; i < BoxNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(BoxNameBuffer[i]));
                }
                UintSizeBoxInfo boxInfo = new UintSizeBoxInfo();
                boxInfo.Name = BoxName;
                boxInfo.BoxSize = BitConverter.ToUInt32(BoxSizeBuffer, 0);
                boxInfo.Offset = -1;

                return boxInfo;
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public static Tkhd GetTkhd(byte[] tkhdBox)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(tkhdBox))
            {
                byte[] TkhdSizeBuffer = new byte[4];
                byte[] TkhdNameBuffer = new byte[4];
                byte[] FlagsVersionBuffer = new byte[4];

                memoryStream.Read(TkhdSizeBuffer, 0, TkhdSizeBuffer.Length);
                memoryStream.Read(TkhdNameBuffer, 0, TkhdNameBuffer.Length);
                memoryStream.Read(FlagsVersionBuffer, 0, FlagsVersionBuffer.Length);

                int Version = BitConverter.ToInt32(FlagsVersionBuffer) >> 24;
                int Flags = BitConverter.ToInt32(FlagsVersionBuffer) << 8;

                DateTime CreationTime = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                byte[] CreationTimeBuffer;

                string BoxName = string.Empty;
                for (int i = 0; i < TkhdNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(TkhdNameBuffer[i]));
                }
                Tkhd Tkhd = new Tkhd(BitConverter.ToUInt32(TkhdSizeBuffer, 0), BoxName, BitConverter.ToInt32(FlagsVersionBuffer));
                if (Version == 0)
                {
                    CreationTimeBuffer = new byte[4];
                    memoryStream.Read(CreationTimeBuffer, 0, CreationTimeBuffer.Length);
                    uint CreationTimeStamp = BitConverter.ToUInt32(CreationTimeBuffer, 0);
                    CreationTime = CreationTime.AddSeconds(CreationTimeStamp);
                    Tkhd.CreationTime = CreationTime;
                }
                else
                {
                    CreationTimeBuffer = new byte[8];
                    memoryStream.Read(CreationTimeBuffer, 0, CreationTimeBuffer.Length);
                    ulong CreationTimeStamp = BitConverter.ToUInt64(CreationTimeBuffer, 0);
                    // CreationTime = CreationTime.AddSeconds(CreationTimeStamp);
                    Tkhd.CreationTime = CreationTime;
                }
                return Tkhd;
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}