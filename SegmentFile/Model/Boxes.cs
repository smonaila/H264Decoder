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
                        Utilities.SaveFile(string.Format(@"{0}\", trakPath), string.Format(@"{0}.bin", TrakInf.Name), TrakBox);
                        
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
                                                              select int.Parse(dir.Replace(traksDirectory, "").Split("_")[1])).Max() + 1 : 1;

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
                string currentTrakPath = string.Format(@"{0}\{1}", traksDirectory, string.Format("{0}_{1}", BoxName, trakIndex));
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

                    // Trak.Mdia = GetMdia(MdiaBox);
                }
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}