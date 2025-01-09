namespace mp4.boxes

public class SampleEntry : Box
{
    private string _format;
    public SampleEntry(string format)
    {
        _format = format;
    }

    public uint[] Reserved { get; set; } = default!;
    public ushort DataReferenceIndex { get; set; }
}

public class AVCDecoderConfigurationRecord : Box
{
    public AVCDecoderConfigurationRecord()
    {

    }

    public AVCDecoderConfigurationRecord(uint size, string name) : base(size, name)
    {
        BoxName = name;
        BoxSize = size;
    }

    public short ConfigurationVersion { get; set; }
    public short AVCProfileIndication { get; set; }
    public short ProfileCompatibility { get; set; }
    public short AVCLevelIndication { get; set; }

    public short LengthSizeMinusOne { get; set; }
    public short NumberOfSequenceParameterSets { get; set; }
    public short NumberOfParameterSets { get; set; }

    public List<long> SequencyParameterSets { get; set; } = default!;
    public List<long> PictureParameterSets { get; set; } = default!;

    public long SetPPSNALUnit(byte[] Psp)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(Psp))
            {
                this.PictureParameterSets = new List<long>();
                for (int i = 0; i < this.NumberOfParameterSets; i++)
                {
                    byte[] PictureParameterSetLengthBuffer = new byte[2];
                    memoryStream.Read(PictureParameterSetLengthBuffer, 0, PictureParameterSetLengthBuffer.Length);
                    Array.Reverse(PictureParameterSetLengthBuffer);

                    ushort PictureParameterSetLength = BitConverter.ToUInt16(PictureParameterSetLengthBuffer, 0);
                    long PictureParameterSetNALUnit = 8 * PictureParameterSetLength;

                    this.PictureParameterSets.Add(PictureParameterSetNALUnit);
                }
                return this.PictureParameterSets.Sum();
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public long SetSPSNALUnit(byte[] Sps)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(Sps))
            {
                this.SequencyParameterSets = new List<long>();
                for (int i = 0; i < this.NumberOfSequenceParameterSets; i++)
                {
                    byte[] SequencyParameterSetLengthBuffer = new byte[2];
                    memoryStream.Read(SequencyParameterSetLengthBuffer, 0, SequencyParameterSetLengthBuffer.Length);
                    Array.Reverse(SequencyParameterSetLengthBuffer);

                    ushort SequencyParameterSetLength = BitConverter.ToUInt16(SequencyParameterSetLengthBuffer, 0);
                    long SequenceParameterSetNALUnit = (8 * SequencyParameterSetLength);

                    SequencyParameterSets.Add(SequenceParameterSetNALUnit);
                }
                return this.SequencyParameterSets.Sum();
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}

public class VisualSampleEntry : SampleEntry
{
    public VisualSampleEntry(string format) : base(format)
    {

    }

    public ushort Predefined { get; set; }
    public ushort Reserved1 { get; set; }
    public int[] Predefined1 { get; set; } = default!;
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public uint HorizResolution { get; set; }
    public uint VertResolution { get; set; }
    public uint Reserved2 { get; set; }
    public ushort FrameCount { get; set; }
    public string[] CompressorName { get; set; } = default!;
    public ushort Depth { get; set; }
    public short PreDefined { get; set; }
}

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

public class NALUnitHeaderMVCExtension
{
    public uint non_idr_flag { get; set; }
    public uint priority_id { get; set; }
    public uint view_id { get; set; }
    public uint temporal_id { get; set; }
    public uint anchor_pic_flag { get; set; }
    public uint inter_view_flag { get; set; }
    public uint reserved_one_bit { get; set; }
}

public class AudioSampleEntry : SampleEntry
{
    public AudioSampleEntry(string codingName) : base(codingName)
    {
    }

    public uint[] Reserved1 { get; set; } = default!;
    public ushort ChannelCount { get; set; }
    public ushort SampleSize { get; set; }
    public ushort PreDefined { get; set; }
    public uint SampleRate { get; set; }
}

public class Box
{
    public Box() { }

    public Box(uint size, string name)
    {
        BoxSize = size;
        BoxName = name;
    }

    public uint BoxSize { get; set; }
    public string BoxName { get; set; } = default!;
}

// An avci box with configurations
public class Avc1 : Box
{
    public Avc1()
    {

    }

    public Avc1(uint size, string name) : base(size, name)
    {
        BoxName = name;
        BoxSize = size;
    }

    public List<VisualSampleEntry> VisualSampleEntries { get; set; } = default!;
    public List<AudioSampleEntry> AudioSampleEntries { get; set; } = default!;
    public AVCDecoderConfigurationRecord AvcC { get; set; } = default!;
}

// A type that defines an object that's going to store the 
// state of the ftyp box.
public class Ftyp : Box
{
    public Ftyp()
    {

    }

    public Ftyp(uint size, string name) : base(size, name)
    {
        BoxSize = size;
        BoxName = name;
    }

    public uint MajorBrand { get; set; }
    public uint MinorBrand { get; set; }
    public string[] CompatibleBrands { get; set; } = default!;
}

public class FullBox : Box
{
    public FullBox(uint size, string name, int flagsVersion = 0) : base(size, name)
    {
        BoxSize = size;
        BoxName = name;
        FlagsVersion = flagsVersion;
    }
    public int FlagsVersion { get; set; }
}

public class BoxInf
{
    public int Offset { get; set; }
    public int Size { get; set; }

    public string? Name { get; set; }
}

public class UintSizeBoxInfo : BoxInf
{
    public uint BoxSize { get; set; }
}

public class Moov : Box
{
    public Moov(uint size, string name) : base(size, name)
    {

    }

    public Mvhd Mvhd { get; set; } = default!;
    public List<Trak> Traks { get; set; } = default!;
}

public class Mvhd : FullBox
{
    public Mvhd(uint size, string name, int flagsVersion) : base(size, name, flagsVersion)
    {
    }

    public int CreationTime { get; set; }
    public int ModificationTime { get; set; }
    public int TimeScale { get; set; }
    public int Duration { get; set; }
    public float PreferredRate { get; set; }
    public float PreferredVolume { get; set; }

    // Reserved bytes.
    public byte[] Reserved { get; set; } = default!;

    // More props to come.
}

public class Trak : FullBox
{
    public Trak(uint size, string name, int flagsVersion = 0) : base(size, name)
    {

    }
    public Tkhd Tkhd { get; set; } = default!;
    public Mdia Mdia { get; set; } = default!;
}

public class Tkhd : FullBox
{
    public Tkhd(uint size, string name, int flagsVersion = 0) : base(size, name)
    {

    }

    public DateTime CreationTime { get; set; }
    public DateTime ModificationTime { get; set; }
}

public class Mdia : FullBox
{
    public Mdia(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public Minf Minf { get; set; } = default!;
}

public class Minf : FullBox
{
    public Minf(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    // Sample table box
    public Stbl Stbl { get; set; } = default!;
}

public class Stbl : FullBox
{
    public Stbl(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }
    public Stsc Stsc { get; set; } = default!;
    public Stco Stco { get; set; } = default!;
    public Stts Stts { get; set; } = default!;
    public Ctts Ctts { get; set; } = default!;
    public Stsd Stsd { get; set; } = default!;
    public Stsz Stsz { get; set; } = default!;
    public Stz2 Stz2 { get; set; } = default!;
}

public class Stz2 : FullBox
{
    public Stz2(uint size, string name, int flagsVersion) : base(size, name, flagsVersion)
    {

    }

    public uint ReservedFieldSize { get; set; } = default!;
    public uint SampleCount { get; set; }
}

public class Stsz : FullBox
{
    public Stsz(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public uint SampleSize { get; set; }
    public uint SampleCount { get; set; }

    public List<uint> EntrySizeList { get; set; } = default!;
}

public class Stsd : FullBox
{
    public Stsd(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public StreamType HandlerType { get; set; } = default!;
    public AudioSampleEntry Audio { get; set; } = default!;
    public VisualSampleEntry Video { get; set; } = default!;

    public enum StreamType
    {
        Video = 1,
        Audio = 2,
        Hint = 3
    }
}

public class Ctts : FullBox
{
    public Ctts(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public uint EntryCount { get; set; }
    public List<CompositionOffsetTable> CompositionOffsets { get; set; } = default!;

    public class CompositionOffsetTable
    {
        public uint SampleCount { get; set; }
        public uint SampleOffset { get; set; }
    }
}

public class Stts : FullBox
{
    public class TimeToSampleTable
    {
        public uint SampleCount { get; set; }
        public uint SampleDelta { get; set; }
    }

    public Stts(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public uint EntryCount { get; set; }
    public List<TimeToSampleTable> TimeToSample { get; set; } = default!;
}

public class Stco : FullBox
{
    public Stco(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public List<uint> ChunkOffsets { get; set; } = default!;
}

public class Stsc : FullBox
{
    public class ChunkTable
    {
        public uint FirstChunk { get; set; }
        public uint SamplePerChunk { get; set; }
        public uint SampleDescriptionIndex { get; set; }
    }

    public Stsc(uint size, string name, int flagsVersion = 0) : base(size, name, flagsVersion)
    {

    }

    public List<ChunkTable> ChunkTableEntries { get; set; } = default!;
}