using System.Data.SqlTypes;
using System.Text.Json;
using System.Text.Json.Nodes;
using decoder.utilities;
using mp4.boxes;
using mp4.utilities;
namespace mp4.segmenter;

public class FactoryMethods
{
    public byte[] GetFileBytes(string filename)
    {
        try
        {
            using (var fileStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                byte[] bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }
        catch (System.Exception)
        {            
            throw;
        }
    }
    public void GetFirstLevelBoxes(string fileName)
    {
        using (var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            byte[] fileBytes = new byte[fileStream.Length];
            fileStream.Read(fileBytes, 0, (int)fileStream.Length);
            int ByteOffset = 0;

            string Filename = Path.GetFileNameWithoutExtension(fileName);
            while (fileBytes.Length > ByteOffset)
            {
                byte[] NameBytes = new byte[4];
                byte[] SizeBytes = new byte[4];

                using (MemoryStream memoryStream = new MemoryStream(fileBytes))
                {
                    memoryStream.Position = ByteOffset;
                    memoryStream.Read(SizeBytes, 0, SizeBytes.Length);
                    memoryStream.Read(NameBytes, 0, NameBytes.Length);

                    Array.Reverse(SizeBytes);

                    int BoxSize = BitConverter.ToInt32(SizeBytes, 0);
                    string BoxName = string.Empty;

                    for (int i = 0; i < NameBytes.Length; i++)
                    {
                        BoxName += string.Format("{0}", Convert.ToChar(NameBytes[i]));
                    }

                    string fileDirectory = string.Format(@".\Data\{0}", Filename);
                    // Console.WriteLine("Directory Exists = {0}, DirectoryPath = {1}", Directory.Exists(fileDirectory), fileDirectory);
                    if (Directory.Exists(fileDirectory))
                    {
                        Directory.Delete(fileDirectory, true);
                    }

                    if (BoxName == "ftyp")
                    {
                        memoryStream.Position = memoryStream.Position - 8;
                        byte[] FtypBox = new byte[BoxSize];
                        memoryStream.Read(FtypBox, 0, FtypBox.Length);

                        string ftypPath = string.Format(@".\Data\{0}\{1}", Filename, BoxName);

                        Utilities.GetDirectory(ftypPath);
                        Utilities.SaveFile(string.Format(@"{0}\", ftypPath), string.Format("{0}.bin", BoxName), FtypBox);

                        // UtilityReaders.GetFtyp(FtypBox);
                    }

                    if (BoxName == "moov")
                    {
                        memoryStream.Position = memoryStream.Position - 8;
                        byte[] moovBox = new byte[BoxSize];
                        memoryStream.Read(moovBox, 0, moovBox.Length);

                        string moovPath = string.Format(@".\Data\{0}\{1}", Filename, BoxName);
                       
                        Utilities.GetDirectory(moovPath);
                        // Utilities.SaveFile(string.Format(@"{0}\", moovPath), string.Format("{0}.bin", BoxName), moovBox);

                        moov(moovBox, Filename);

                        // UtilityReaders.GetMoov(moovBox);
                    }  
                    // Console.Write("Current BoxName: {0}\t", BoxName);
                    // Console.WriteLine("Current BoxSize: {0}", BoxSize);

                    ByteOffset += BoxSize;
                }
            }
        }
    }

    public void moov(byte[] moovbytes, string fileName)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(moovbytes))
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

                List<BoxInf> ChildBoxes = Mp4FileUtilities.GetChildrens(moovbytes);
                BoxInf? MvhdInf = ChildBoxes.Find(cb => cb.Name == "mvhd");
                if (MvhdInf != null)
                {
                    memoryStream.Position = MvhdInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] MvhdBox = new byte[MvhdInf.Size];

                    memoryStream.Read(MvhdBox, 0, MvhdBox.Length);
                    string mvhdPath = string.Format(@".\Data\{0}\moov\{1}", fileName, MvhdInf.Name);

                    Utilities.GetDirectory(mvhdPath);
                    Utilities.SaveFile(string.Format(@"{0}\", mvhdPath), string.Format(@"{0}.bin", MvhdInf.Name), MvhdBox);

                    // Moov.Mvhd = GetMvhd(MvhdBox);
                }

                List<BoxInf>? TraksInf = ChildBoxes.Where(cb => cb.Name == "trak").ToList();
                Moov.Traks = new List<Trak>();
                if (TraksInf != null)
                {
                    string traksPath = string.Format(@".\Data\{0}\moov\traks", fileName);
                    int trakIndex = 0;
                    foreach (var TrakInf in TraksInf)
                    {
                        memoryStream.Position = TrakInf.Offset;

                        byte[] BoxSizeBuffer = new byte[4];
                        byte[] BoxNameBuffer = new byte[4];
                        byte[] TrakBox = new byte[TrakInf.Size];

                        memoryStream.Read(TrakBox, 0, TrakBox.Length);
                        string trakPath = string.Format(@"{0}\{1}", traksPath, string.Format("{0}_{1}", TrakInf.Name, trakIndex + 1));

                        Utilities.GetDirectory(string.Format(@"{0}\", trakPath));
                        // Utilities.SaveFile(string.Format(@"{0}\", trakPath), string.Format(@"{0}.bin", TrakInf.Name), TrakBox);
                        
                        trakIndex++;

                        trak(TrakBox, fileName);

                        // Moov.Traks.Add(GetTrak(TrakBox));
                    }
                }
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public void trak(byte[] trakbytes, string fileName)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(trakbytes))
            {
                byte[] TrakSizeBuffer = new byte[4];
                byte[] TrakNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                string traksDirectory = string.Format(@".\Data\{0}\moov\traks\", fileName);
                var traksDirectories = (from dir in Directory.GetDirectories(traksDirectory)
                                        where dir.Replace(traksDirectory, "").Split("_")[0].Equals("trak")
                                        select dir).ToList();

                int trakIndex = traksDirectories.Count > 0 ? (from dir in traksDirectories
                                                              select int.Parse(dir.Replace(traksDirectory, "").Split("_")[1])).Max() : 1;

                memoryStream.Read(TrakSizeBuffer, 0, TrakSizeBuffer.Length);
                memoryStream.Read(TrakNameBuffer, 0, TrakNameBuffer.Length);

                Array.Reverse(TrakSizeBuffer);

                List<BoxInf> Children =  Mp4FileUtilities.GetChildrens(trakbytes);
                BoxInf? TkhdInf = Children.Find(c => c.Name == "tkhd");

                string BoxName = string.Empty;
                for (int i = 0; i < TrakNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(TrakNameBuffer[i]));
                }
                string currentTrakPath = Mp4FileUtilities.GetCurrentNumberedDirectory(string.Format(@"{0}", traksDirectory), BoxName);
                Trak Trak = new Trak(BitConverter.ToUInt32(TrakSizeBuffer), BoxName);
                if (TkhdInf != null)
                {
                    memoryStream.Position = TkhdInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] TkhdBox = new byte[TkhdInf.Size];
                    memoryStream.Read(TkhdBox, 0, TkhdBox.Length);

                    string tkhdPath = string.Format(@"{0}\{1}", currentTrakPath, TkhdInf.Name);

                    Utilities.GetDirectory(tkhdPath);
                    Utilities.SaveFile(string.Format("{0}", tkhdPath), string.Format("{0}.bin", TkhdInf.Name), TkhdBox);

                    // Trak.Tkhd = GetTkhd(TkhdBox);
                }

                BoxInf? MdiaInf = Children.Find(c => c.Name == "mdia");
                if (MdiaInf != null)
                {
                    memoryStream.Position = MdiaInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] MdiaBox = new byte[MdiaInf.Size];

                    memoryStream.Read(MdiaBox, 0, MdiaBox.Length);

                    mdia(MdiaBox, fileName);

                    // Trak.Mdia = GetMdia(MdiaBox);
                }
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public void mdia(byte[] mdiaBox, string fileName)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(mdiaBox))
            {
                // Get the tracks path
                string traksPath = string.Format(string.Format(@".\Data\{0}\moov\traks\", fileName));
                string currentTrakPath = Mp4FileUtilities.GetCurrentNumberedDirectory(traksPath, "trak");

                // Console.WriteLine(string.Format(@"CurrentTrak: {0}", currentTrakPath));
                byte[] MdiaSizeBuffer = new byte[4];
                byte[] MdiaNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                memoryStream.Read(MdiaSizeBuffer, 0, MdiaSizeBuffer.Length);
                memoryStream.Read(MdiaNameBuffer, 0, MdiaNameBuffer.Length);

                Array.Reverse(MdiaSizeBuffer);

                List<BoxInf> Children = Mp4FileUtilities.GetChildrens(mdiaBox);
                BoxInf? MdhdInf = Children.Find(c => c.Name == "mdhd");

                string BoxName = string.Empty;
                for (int i = 0; i < MdiaNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(MdiaNameBuffer[i]));
                }
                Mdia Mdia = new Mdia(BitConverter.ToUInt32(MdiaSizeBuffer), BoxName);
                string mdiaPath = string.Format(@"{0}\{1}", currentTrakPath, Mdia.BoxName);
                Utilities.GetDirectory(mdiaPath);

                BoxInf? MinfInf = Children.Find(c => c.Name == "minf");
                if (MinfInf != null)
                {
                    memoryStream.Position = MinfInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] MinfBox = new byte[MinfInf.Size];

                    memoryStream.Read(MinfBox, 0, MinfBox.Length);
                    
                    minf(MinfBox, fileName);
                }
            }
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public void minf(byte[] minfBox, string fileName)
    {
         try
        {
            using (MemoryStream memoryStream = new MemoryStream(minfBox))
            {
                byte[] MinfSizeBuffer = new byte[4];
                byte[] MinfNameBuffer = new byte[4];
                byte[] FlagsVersion = new byte[4];

                // Get the tracks path
                string traksPath = string.Format(string.Format(@".\Data\{0}\moov\traks\", fileName));
                string currentTrakPath = Mp4FileUtilities.GetCurrentNumberedDirectory(traksPath, "trak");
                string mdiaPath = string.Format(@"{0}\mdia", currentTrakPath);

                memoryStream.Read(MinfSizeBuffer, 0, MinfSizeBuffer.Length);
                memoryStream.Read(MinfNameBuffer, 0, MinfNameBuffer.Length);

                Array.Reverse(MinfSizeBuffer);

                List<BoxInf> Children = Mp4FileUtilities.GetChildrens(minfBox);
                BoxInf? StblInf = Children.Find(c => c.Name == "stbl");
                string BoxName = string.Empty;
                for (int i = 0; i < MinfNameBuffer.Length; i++)
                {
                    BoxName += string.Format("{0}", Convert.ToChar(MinfNameBuffer[i]));
                }
                Minf Minf = new Minf(BitConverter.ToUInt32(MinfSizeBuffer), BoxName);
                string minfPath = string.Format(@"{0}\{1}", mdiaPath, Minf.BoxName);
                Utilities.GetDirectory(minfPath);
                if (StblInf != null)
                {
                    memoryStream.Position = StblInf.Offset;

                    byte[] BoxSizeBuffer = new byte[4];
                    byte[] BoxNameBuffer = new byte[4];
                    byte[] StblBox = new byte[StblInf.Size];

                    memoryStream.Read(StblBox, 0, StblBox.Length);

                    stbl(StblBox, fileName);
                    
                    // Minf.Stbl = GetStbl(StblBox);
                }
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public void stbl(byte[] stblBox, string fileName)
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

                List<BoxInf> Children = Mp4FileUtilities.GetChildrens(stblBox);
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

                // Get the tracks path
                string traksPath = string.Format(string.Format(@".\Data\{0}\moov\traks\", fileName));
                string currentTrakPath = Mp4FileUtilities.GetCurrentNumberedDirectory(traksPath, "trak");
                string stblPath = string.Format(@"{0}\mdia\minf\stbl", currentTrakPath);

                Utilities.GetDirectory(stblPath);

                if (StsdInf != null)
                {
                    memoryStream.Position = StsdInf.Offset;
                    byte[] StsdBox = new byte[StsdInf.Size];
                    memoryStream.Read(StsdBox, 0, StsdBox.Length);

                    string stsdPath = string.Format(@"{0}\{1}", stblPath, StsdInf.Name);

                    Utilities.GetDirectory(stsdPath);
                    Utilities.SaveFile(stsdPath, string.Format("{0}.bin", StsdInf.Name), StsdBox);

                    Stsd Stsd = Mp4FileUtilities.GetStsd(StsdBox);
                    if (Stsd.HandlerType == Stsd.StreamType.Video)
                    {
                        
                        var stsd = new {
                            name = Stsd.BoxName,
                            handler_name = Stsd.Video.Format,
                            frame_count = Stsd.Video.FrameCount,
                            data_reference = Stsd.Video.DataReferenceIndex,
                            width = Stsd.Video.Width,
                            height = Stsd.Video.Height,
                            horizontal_resolution = Stsd.Video.HorizResolution,
                            vertical_resolution = Stsd.Video.VertResolution,
                             
                            avc_decorder_config = new {
                                config_version = Stsd.GetAVCDecoderConfiguration.ConfigurationVersion,
                                profile_indication = Stsd.GetAVCDecoderConfiguration.AVCProfileIndication,
                                profile_compatibility = Stsd.GetAVCDecoderConfiguration.ProfileCompatibility,
                                level_indication = Stsd.GetAVCDecoderConfiguration.AVCLevelIndication,
                                length_size_minus1 = Stsd.GetAVCDecoderConfiguration.LengthSizeMinusOne
                            } 
                        };
                        string stsdJson = JsonSerializer.Serialize(stsd);
                        File.WriteAllText(string.Format(@"{0}\stsd.json", stsdPath), stsdJson);
                    }                    
                }

                if (SttsInf != null)
                {
                    memoryStream.Position = SttsInf.Offset;
                    byte[] SttsBox = new byte[SttsInf.Size];
                    memoryStream.Read(SttsBox, 0, SttsBox.Length);

                    string sttsdPath = string.Format(@"{0}\{1}", stblPath, SttsInf.Name);

                    Utilities.GetDirectory(sttsdPath);
                    Utilities.SaveFile(sttsdPath, string.Format("{0}.bin", SttsInf.Name), SttsBox);

                    Stts Stts = Mp4FileUtilities.GetStts(SttsBox);

                    var stts = new {
                        entry_count = Stts.EntryCount,
                        time_table = Stts.TimeToSample
                    };
                    string sttsJson = JsonSerializer.Serialize(stts);
                    File.WriteAllText(string.Format(@"{0}\stts.json", sttsdPath), sttsJson);
                }

                if (StscInf != null)
                {
                    memoryStream.Position = StscInf.Offset;
                    byte[] StscBox = new byte[StscInf.Size];
                    memoryStream.Read(StscBox, 0, StscBox.Length);

                    string stscPath = string.Format(@"{0}\{1}", stblPath, StscInf.Name);

                    Utilities.GetDirectory(stscPath);
                    Utilities.SaveFile(stscPath, string.Format("{0}.bin", StscInf.Name), StscBox);

                    Stsc Stsc = Mp4FileUtilities.GetStsc(StscBox);

                    var stsc = new {
                        entry_count = Stsc.EntryCount,
                        chunk_table_entries = Stsc.ChunkTableEntries
                    };
                    string stscJson = JsonSerializer.Serialize(stsc);
                    File.WriteAllText(string.Format(@"{0}\stsc.json", stscPath), stscJson);
                }

                if (StcoInf != null)
                {
                    memoryStream.Position = StcoInf.Offset;
                    byte[] StcoBox = new byte[StcoInf.Size];

                    memoryStream.Read(StcoBox, 0, StcoBox.Length);

                    string stcoPath = string.Format(@"{0}\{1}", stblPath, StcoInf.Name);

                    Utilities.GetDirectory(stcoPath);
                    Utilities.SaveFile(stcoPath, string.Format("{0}.bin", StcoInf.Name), StcoBox);

                    Stco Stco = Mp4FileUtilities.GetStco(StcoBox);

                    var stco = new {
                        entry_count = Stco.ChunkOffsets.Count,
                        chunk_offset = Stco.ChunkOffsets
                    };
                    string stcoJson = JsonSerializer.Serialize(stco);
                    File.WriteAllText(string.Format(@"{0}\stco.json", stcoPath), stcoJson);
                }

                if (CttsInf != null)
                {
                    memoryStream.Position = CttsInf.Offset;
                    byte[] CttsBox = new byte[CttsInf.Size];

                    memoryStream.Read(CttsBox, 0, CttsBox.Length);

                    string cttsPath = string.Format(@"{0}\{1}", stblPath, CttsInf.Name);

                    Utilities.GetDirectory(cttsPath);
                    Utilities.SaveFile(cttsPath, string.Format("{0}.bin", CttsInf.Name), CttsBox);

                    Ctts Ctts = Mp4FileUtilities.GetCtts(CttsBox);

                    var ctts = new {
                        entry_count = Ctts.EntryCount,
                        composition_offset = Ctts.CompositionOffsets
                    };

                    string cttsJson = JsonSerializer.Serialize(ctts);
                    File.WriteAllText(string.Format(@"{0}\ctts.json", cttsPath), cttsJson);
                }

                if (StszInf != null)
                {
                    memoryStream.Position = StszInf.Offset;
                    byte[] StszBox = new byte[StszInf.Size];

                    memoryStream.Read(StszBox, 0, StszBox.Length);

                    string stszPath = string.Format(@"{0}\{1}", stblPath, StszInf.Name);

                    Utilities.GetDirectory(stszPath);
                    Utilities.SaveFile(stszPath, string.Format("{0}.bin", StszInf.Name), StszBox);

                    Stsz Stsz = Mp4FileUtilities.GetStsz(StszBox);

                    var stsz = new {
                        sample_size = Stsz.SampleSize,
                        sample_counter = Stsz.SampleCount,
                        entry_list_size = Stsz.EntrySizeList
                    };

                    string stszJson = JsonSerializer.Serialize(stsz);
                    File.WriteAllText(string.Format(@"{0}\stsz.json", stszPath), stszJson);
                }
                // GetFrame(Stbl);
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}