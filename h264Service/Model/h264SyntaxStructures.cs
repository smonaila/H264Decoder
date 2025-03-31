using System;
using System.Text.Json;
using h264.NALUnits;
using H264.Global.Variables;
using H264.Types;
using static h264.NALUnits.SliceHeader;

namespace h264.syntaxstructures;

public class SliceData
{
    private SliceHeader slice_header;
    private PPS Pps;
    public SliceData()
    {
        slice_header = new SliceHeader();
        Pps = new PPS();
    }
    public SliceData(SliceHeader sliceHeader, PPS Pps)
    {
        this.Pps = Pps;
        slice_header = sliceHeader;
    }

    public string cabac_alignment_one_bit { get; set; } = string.Empty;
    public uint mb_skip_run { get; set; }
    public bool mb_skip_flag { get; set; }
    public bool mb_field_decoding_flag { get; set; }
    public bool end_of_slice_flag { get; set; }
}

public class DecRefPicMarking
{
    public DecRefPicMarking()
    {

    }

    public bool no_output_of_prior_pics_flags { get; set; }
    public bool long_term_reference_flags { get; set; }
    public bool adaptive_ref_pic_marking_mode_flags { get; set; }
    public uint memory_management_control_operation { get; set; }
    public uint different_of_pic_nums_minus1 { get; set; }
    public uint long_term_pic_num { get; set; }
    public uint long_term_frame_idx { get; set; }
    public uint max_long_term_frame_idx_plus1 { get; set; }

    public object Get(string key)
    {
        throw new NotImplementedException();
    }

    public object Set(string key, object value)
    {
        throw new NotImplementedException();
    }
}

public class RefPicListModification
{
    private readonly SliceHeader slice_header;
    public RefPicListModification()
    {
        slice_header = new SliceHeader();
    }
    public RefPicListModification(SliceHeader sliceHeader)
    {
        slice_header = sliceHeader;
    }
    public bool ref_pic_list_modification_flag_l0 { get; set; }
    public uint modification_of_pic_nums_idc { get; set; }
    public uint abs_diff_pic_num_minus1 { get; set; }
    public uint long_term_pic_num { get; set; }
}

public class SubMbPredLayer
{
    private uint mbtype;
    public SubMbPredLayer(uint mb_type)
    {
        mbtype = mb_type;
    }

    public SubMbPredLayer GetSubMbPredLayer()
    {
        return this;
    }

    public List<uint> sub_mb_type { get; set; } = new List<uint>();
    public List<uint> ref_idx_l0 { get; set; } = new List<uint>();
    public List<uint> ref_idx_l1 { get; set; } = new List<uint>();
    public List<uint> mvd_l0 { get; set; } = new List<uint>();
    public List<uint> mvd_l1 { get; set; } = new List<uint>();
}

// The mb_pred Syntax structure.
public class MbPred
{
    public bool[] prev_intra4x4_pred_mode_flag { get; set; } = new bool[16];
    public List<int> rem_intra4x4_pred_mode { get; set; } = new List<int>();
    public bool[] prev_intra8x8_pred_mode_flag { get; set; } = new bool[4];
    public List<int> rem_intra8x8_pred_mode { get; set; } = new List<int>();
    public int intra_chroma_pred_mode { get; set; }
    public List<int> ref_idx_l0 { get; set; } = new List<int>();
    public int[,,] mvd_l0 { get; set; } = default!;
    public int[,,] mvd_l1 { get; set; } = default!; 
}

public class MicroblockTypes
{
    private ICodecSettingsService SettingsService;
    private SettingSets SettingSets;

    // The Constructor to be called when a type of Microblock is created.
    public MicroblockTypes(ICodecSettingsService settingsService, SynElemSlice synElemSlice)
    {
        SynElement = synElemSlice;
        SettingsService = settingsService;
        SettingSets = SettingsService.GetCodecSettings();

        using (StreamReader jsonReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\SliceMicroblockTables.json"))
        {
            string jsonString = jsonReader.ReadToEnd();
            SliceTypeMicroblock? SliceType = JsonSerializer.Deserialize<SliceTypeMicroblock>(jsonString);
            ISliceTypeTable = SliceType != null ? SliceType.ISliceMicroblocks : new List<I_SliceMicroblock>();
            BSliceMacroblocks = SliceType != null ? SliceType.BSliceMacroblocks : new List<BSliceMacroblock>();
            PandSPSliceMacroblocks = SliceType != null ? SliceType.PandSPSliceMacroblocks : new List<PandSP_SliceMicroblock>();
            PSubMacroblocks = SliceType != null ? SliceType.PSubMacroblocks : new List<SliceSubMacroblock>();
            BSubMacroblocks = SliceType != null ? SliceType.BSubMacroblocks : new List<SliceSubMacroblock>();
        }
    }

    
    public SliceMicroblock? GetMbType(uint mb_type)
    {
        try
        {
            if (SynElement.Slicetype == Slicetype.I || SynElement.Slicetype == Slicetype.SI)
            {
                I_SliceMicroblock? macroblock = (from mb in ISliceTypeTable
                                                 where mb.MbType == mb_type
                                                 select mb).FirstOrDefault();
                return macroblock;
            }
            else if (SynElement.Slicetype == Slicetype.B)
            {
                // Search the table for Microblocks of type B
                BSliceMacroblock? macroblock = (from bsliceMB in BSliceMacroblocks
                                                where bsliceMB.MbType == mb_type
                                                select bsliceMB).FirstOrDefault();
                return macroblock;
            }
            else if (SynElement.Slicetype == Slicetype.P || SynElement.Slicetype == Slicetype.SP)
            {
                PandSP_SliceMicroblock? macroblock = (from mb in PandSPSliceMacroblocks
                                                      where mb.MbType == mb_type
                                                      select mb).FirstOrDefault();
                return macroblock;
            }
            return new SliceMicroblock();
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    // This is the method that is going go through the process of determining the type of slice and the 
    // prediction mode an return the prediction mode.
    public PredictionModes MbPartPredMode(uint mb_type, uint part)
    {
        PredictionModes predictionMode = PredictionModes.NA;    
        MacroblockLayer macroblockLayer = SettingSets.Extras.MacroblockLayer;

        if (SynElement.Slicetype == Slicetype.I || SynElement.Slicetype == Slicetype.SI)
        {
            if (mb_type != 0)
            {
                I_SliceMicroblock? microblock = (from mb in ISliceTypeTable
                                                 where mb.MbType == mb_type
                                                 select mb).FirstOrDefault();
                NumMbPartions? numMbPartions = microblock != null ?
                (from mbp in microblock.NumMbPartions
                 where mbp.NumberOfPart == part
                 select mbp).FirstOrDefault() : new NumMbPartions();

                predictionMode = numMbPartions != null ? numMbPartions.PredictionModes : PredictionModes.Invalid;
            }
            else if(mb_type == 0)
            {
                if (macroblockLayer.transform_size_8x8_flag)
                {
                    predictionMode = PredictionModes.Intra_8x8;
                } else
                {
                    predictionMode = PredictionModes.Intra_4x4;
                }                
            }
        }
        else if (SynElement.Slicetype == Slicetype.B)
        {
            // Search the table for Microblocks of type B
            BSliceMacroblock? macroblock = (from bsliceMB in BSliceMacroblocks
                                            where bsliceMB.MbType == mb_type
                                            select bsliceMB).FirstOrDefault();
            NumMbPartions? MbPartitions = macroblock != null ? 
            (from Part in macroblock.NumMbPartions
             where Part.NumberOfPart == part
             select Part).FirstOrDefault() : new NumMbPartions();
            
            predictionMode = MbPartitions != null ? MbPartitions.PredictionModes : PredictionModes.Invalid;
        }
        else if (SynElement.Slicetype == Slicetype.P || SynElement.Slicetype == Slicetype.SP)
        {
            PandSP_SliceMicroblock? macroblock = (from mb in PandSPSliceMacroblocks
                                                  where mb.MbType == mb_type
                                                  select mb).FirstOrDefault();
            NumMbPartions? mbPartitions = macroblock != null ? 
            (from Part in macroblock.NumMbPartions
            where Part.NumberOfPart == part
            select Part).FirstOrDefault() : new NumMbPartions();
            predictionMode = mbPartitions != null ? mbPartitions.PredictionModes : PredictionModes.Invalid;
        }
        else
        {
            predictionMode = PredictionModes.NA;
        }
        return predictionMode;
    }

    public List<BSliceMacroblock> GetBSliceMacroblock()
    {
        return new List<BSliceMacroblock>();
    }

    public NumMbPartModes NumMbPart(uint mb_type)
    {
        try
        {
            NumMbPartModes numMbPartModes = NumMbPartModes.Defult;
            if (SynElement.Slicetype == Slicetype.P || SynElement.Slicetype == Slicetype.SP)
            {
                var macroblock = (from PandSPMb in PandSPSliceMacroblocks
                                  where PandSPMb.MbType == mb_type
                                  select PandSPMb).FirstOrDefault();                
                numMbPartModes = macroblock != null ? macroblock.NumMbPart : NumMbPartModes.Defult;
            }else if (SynElement.Slicetype == Slicetype.B || SynElement.Slicetype == Slicetype.BS)
            {
                var macroblock = (from bSliceMb in BSliceMacroblocks
                                  where bSliceMb.MbType == mb_type
                                  select bSliceMb).FirstOrDefault();
                numMbPartModes = macroblock != null ? macroblock.NumMbPart : NumMbPartModes.Defult;
            }
            return numMbPartModes;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public NumMbPartModes NumSubMbPart(uint subMbType)
    {
        try
        {
            NumMbPartModes numMbPartModes = NumMbPartModes.Defult;
            if (SynElement.Slicetype == Slicetype.P)
            {
                var macroblock = (from psubMb in PSubMacroblocks
                                  where psubMb.SubMbType == subMbType
                                  select psubMb).FirstOrDefault();
                numMbPartModes = macroblock != null ? macroblock.NumMbPart : NumMbPartModes.Defult;
            }

            if (SynElement.Slicetype == Slicetype.B)
            {
                var macroblock = (from bsubMb in BSubMacroblocks
                                  where bsubMb.SubMbType == subMbType
                                  select bsubMb).FirstOrDefault();
                numMbPartModes = macroblock != null ? macroblock.NumMbPart : NumMbPartModes.Defult;
            }

            return numMbPartModes;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    // Microblock types properties.
    public SynElemSlice SynElement { get; set; }
    public List<I_SliceMicroblock> ISliceTypeTable { get; set; } = new List<I_SliceMicroblock>();
    public List<BSliceMacroblock> BSliceMacroblocks { get; set; } = new List<BSliceMacroblock>();
    public List<PandSP_SliceMicroblock> PandSPSliceMacroblocks { get; set; } = new List<PandSP_SliceMicroblock>();
    public List<SliceSubMacroblock> PSubMacroblocks { get; set; } = new List<SliceSubMacroblock>();
    public List<SliceSubMacroblock> BSubMacroblocks { get; set; } = new List<SliceSubMacroblock>();
}



/// <summary>
/// The MicroblockLayer syntax structure.
/// </summary>
public class MacroblockLayer
{
    public uint mb_type { get; set; }
    public uint pcm_alignment_zero_bit { get; set; }
    public uint[] pcm_sample_luma { get; set; } = new uint[256];
    public List<uint> pcm_sample_chroma { get; set; } = new List<uint>();
    public bool transform_size_8x8_flag { get; set; }
    public uint coded_block_pattern { get; set; }
    public uint mb_qp_delta { get; set; }
}