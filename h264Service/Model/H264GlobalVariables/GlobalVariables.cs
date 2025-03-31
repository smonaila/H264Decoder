
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using h264.NALUnits;
using h264.syntaxstructures;
using H264.Types;

namespace H264.Global.Variables;

public class SettingSets
{
    [JsonPropertyName("cpps")]
    public PPS GetPPS { get; set; } = default!;
    [JsonPropertyName("csps")]
    public SPS GetSPS { get; set; } = default!;
    [JsonPropertyName("gvar")]
    public GlobalVariables GlobalVariables { get; set; } = default!;
    [JsonPropertyName("slch")]
    public SliceHeader SliceHeader { get; set; } = default!;
    [JsonPropertyName("slcd")]
    public SliceData SliceData { get; set; } = default!;
    [JsonPropertyName("extras")]
    public Extras Extras { get; set; } = default!;
}

public class CodecSettings : ICodecSettingsService
{
    private SettingSets? codecSettings = default!;
    public CodecSettings()
    {
        using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\codecsetting.json"))
        {
            codecSettings = JsonSerializer.Deserialize<SettingSets>(streamReader.ReadToEnd());
        }
    }

    public object SetRange<T>(T type)
    {
        try
        {
            Type currentType = typeof(T);
            PropertyInfo[] properties = currentType.GetProperties();
            return currentType;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public PropertyInfo Get<T>(T type, string key)
    {
        try
        {
            Type currentType = typeof(T);
            PropertyInfo[] properties = currentType.GetProperties();
            PropertyInfo currProp = properties.First(p => p.Name.ToLower() == key.ToLower());
                                    
            return currProp;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public object Set<T, VT>(T type, string key, VT value)
    {
        try
        {
            string JsonCodecs = string.Empty;
            Type currentType = typeof(T);
            PropertyInfo[] properties = currentType.GetProperties();
            PropertyInfo propertyInfo = properties.First(p => p.Name.ToLower() == key.ToLower());
            propertyInfo.SetValue(this, value);

            using (StreamReader streamReader = new StreamReader(@"h264Service\Data\codecsetting.json"))
            {
                codecSettings = JsonSerializer.Deserialize<SettingSets>(streamReader.ReadToEnd());               
                JsonCodecs = JsonSerializer.Serialize<CodecSettings>(this);  
            }
            using (StreamWriter streamWriter = new StreamWriter(@"h264Service\Data\codecsetting.json"))
            {
                streamWriter.Write(JsonCodecs);
            }
            return propertyInfo;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public void Update<T>(T Updates)
    {
        try
        {
            using (StreamWriter streamWriter = new StreamWriter(@"C:\H264Decoder\h264Service\Data\codecsetting.json"))
            {
                if (codecSettings != null)
                {
                    if (Updates is SPS)
                    {
                        SPS Sps = (SPS)Convert.ChangeType(Updates, typeof(SPS));
                        codecSettings.GetSPS = Sps;
                    }
                    else if (Updates is PPS)
                    {
                        PPS Pps = (PPS)Convert.ChangeType(Updates, typeof(PPS));
                        codecSettings.GetPPS = Pps;
                    }
                    else if (Updates is GlobalVariables)
                    {
                        GlobalVariables globalVariables = (GlobalVariables)Convert.ChangeType(Updates, typeof(GlobalVariables));
                        codecSettings.GlobalVariables = globalVariables;
                    }
                    else if(Updates is SliceHeader)
                    {
                        SliceHeader sliceHeader = (SliceHeader)Convert.ChangeType(Updates, typeof(SliceHeader));
                        codecSettings.SliceHeader = sliceHeader;
                    }
                    else if(Updates is SliceData)
                    {
                        SliceData sliceData = (SliceData)Convert.ChangeType(Updates, typeof(SliceData));
                        codecSettings.SliceData = sliceData;
                    }else if (Updates is Extras)
                    {
                        Extras extras = (Extras)Convert.ChangeType(Updates, typeof(Extras));
                        codecSettings.Extras = extras;
                    }
                    string JsonCodecs = JsonSerializer.Serialize<SettingSets>(codecSettings);
                    streamWriter.WriteLine(JsonCodecs);
                }                
            }

        }
        catch (System.Exception)
        {
           
        }
    }
    public SettingSets GetCodecSettings()
    {
        try
        {
            if (codecSettings == null)
            {
                codecSettings = new SettingSets();
            }
            return codecSettings;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }
}

public class GlobalVariables 
{
    public GlobalVariables()
    {
       
    }

    [JsonPropertyName("coded_block_pattern_luma")]
    public ushort CodedBlockPatternLuma { get; set; }
    [JsonPropertyName("coded_block_pattern_chroma")]
    public ushort CodedBlockPatternChroma { get; set; }
    [JsonPropertyName("chroma_array_type")]
    public ushort ChromaArrayType { get; set; }
    [JsonPropertyName("numc8x8")]
    public int NumC8x8 { get; set; }
    [JsonPropertyName("sub_width_c")]
    public SubWidthHeight SubWidthC { get; set; }
    [JsonPropertyName("sub_height_c")]
    public SubWidthHeight SubHeightC { get; set; }
    [JsonPropertyName("curr_mb_addr")]
    public int CurrMbAddr { get; set; }
    [JsonPropertyName("pic_width_in_mbs")]
    public int PicWidthInMbs { get; set; }
    [JsonPropertyName("mbaff_frame_flag")]
    public int MbaffFrameFlag { get; set; }
    [JsonPropertyName("pic_size_in_map_units")]
    public int PicSizeInMapUnits { get; set; }
    [JsonPropertyName("pic_height_in_map_units")]
    public int PicHeightInMapUnits { get; set; }
    [JsonPropertyName("map_units_in_slice_group0")]
    public int MapUnitsInSliceGroup0 { get; set; }
    [JsonPropertyName("pic_size_in_mbs")]
    public int PicSizeInMbs { get; set; }
    [JsonPropertyName("slice_qpy")]
    public int SliceQPY { get; set; }
    [JsonPropertyName("pic_height_in_mbs")]
    public int PicHeightInMbs { get; set; }
    [JsonPropertyName("frame_height_in_mbs")]
    public int FrameHeightInMbs { get; set; }
    [JsonPropertyName("mb_height_c")]
    public int MbHeightC { get; set; }
    [JsonPropertyName("pic_height_in_samples_l")]
    public int PicHeightInSamplesL { get; set; }
    [JsonPropertyName("max_frame_num")]
    public int MaxFrameNum { get; set; }
    [JsonPropertyName("max_pic_num")]
    public int MaxPicNum { get; set; }
    [JsonPropertyName("curr_pic_num")]
    public uint CurrPicNum { get; set; }
    [JsonPropertyName("bit_depth_y")]
    public uint BitDepthY { get; set; }
    [JsonPropertyName("qp_bd_offset_y")]
    public uint QpBdOffsetY { get; set; }
    [JsonPropertyName("bit_depth_c")]
    public uint BitDepthC { get; set; }
    [JsonPropertyName("qp_bd_offset_c")]
    public uint QpBdOffsetC { get; set; }
    [JsonPropertyName("mb_width_c")]
    public int MbWidthC { get; set; }
    [JsonPropertyName("raw_mb_bits")]
    public long RawMbBits { get; set; }
    [JsonPropertyName("max_pic_order_cnt_lsb")]
    public int MaxPicOrderCntLsb { get; set; }
    [JsonPropertyName("expected_delta_per_pic_order_cnt_cycle")]
    public int ExpectedDeltaPerPicOrderCntCycle { get; set; }
    [JsonPropertyName("pic_width_in_samples_l")]
    public int PicWidthInSamplesL { get; set; }
    [JsonPropertyName("pic_width_in_samples_c")]
    public int PicWidthInSamplesC { get; set; }
    [JsonPropertyName("crop_unit_x")]
    public int CropUnitX { get; set; }
    [JsonPropertyName("crop_unit_y")]
    public int CropUnitY { get; set; }
    [JsonPropertyName("qsy")]
    public int QSY { get; set; }

    [JsonPropertyName("filter_offset_a")]
    public int FilterOffsetA { get; set; }
    [JsonPropertyName("filter_offset_b")]
    public int FilterOffsetB { get; internal set; }
    [JsonPropertyName("slice_group_change_rate")]
    public uint SliceGroupChangeRate { get; internal set; }
    public int QPYprev { get; set; }
    public long QPprime { get; internal set; }
    public bool TransformBypassModeFlag { get; set; }
}