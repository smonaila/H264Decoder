using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Decoder.H264ArrayParsers;
using h264.NALUnits;
using h264.syntaxstructures;
using H264.Global.Variables;
using H264Utilities.Descriptors;
using static h264.NALUnits.SliceHeader;

namespace H264.Types;


public interface ICoefficients
{
    int[] GetCoefficients(int[] coeffLevels, int startIdx, int endIdx, int maxNumCoeff);
}

public class CoeffTokenMapping
{
    [JsonPropertyName("trailing_ones(coeff_token)")]
    public int TrailingOnes { get; set; }
    [JsonPropertyName("total_coeff(coeff_token)")]
    public int CoeffTokens { get; set; }
    [JsonPropertyName("_0_lessorequal_nC_less_2")]
    public string _0_LessOrEqual_nC_Less_2 { get; set; } = string.Empty;
    [JsonPropertyName("_2_lessorequal_nC_less_4")]
    public string _2_LessorEqual_nC_Less_4 { get; set; } = string.Empty;
    [JsonPropertyName("_4_lessorequal_nC_less_8")]
    public string _4_LessorEqual_nC_Less_8 { get; set; } = string.Empty;
    [JsonPropertyName("_8_lessorequal_nC")]
    public string _8_LessorEqual_nC { get; set; } = string.Empty;
    [JsonPropertyName("nC_equal_minus1")]
    public string nC_Equal_Minus1 { get; set; } = string.Empty;
    [JsonPropertyName("nC_equal_minus2")]
    public string nC_Equal_Minus2 { get; set; } = string.Empty;
}

public class CurrentTokenMapping
{
    public int TrailingOnes { get; set; }
    public int CoeffTokens { get; set; }
    public string Token { get; set; } = string.Empty;
}

// CABAC implementation of ResidualBlock syntax structure
public class ResidualBlockCabac : ICoefficients
{
    private GlobalVariables GlobalVariables = new GlobalVariables();
    private BitList bitStream;
    public ResidualBlockCabac(BitList BitStream)
    {
        bitStream = BitStream;
    }

    private int numCoeff;
    public int[] GetCoefficients(int[] coeffLevels, int startIdx, int endIdx, int maxNumCoeff)
    {
        if (maxNumCoeff != 64 || GlobalVariables.ChromaArrayType == 3)
        {
            // Read coded_block_flag from stream.
            coded_block_flag = (int)bitStream.ue();
        }

        for (int i = 0; i < maxNumCoeff; i++)
        {
            coeffLevels[i] = 0;
        }
        if (coded_block_flag > 0)
        {
            numCoeff = endIdx + 1;
            int i = startIdx;
            significant_coeff_flag = new int[numCoeff - 1];
            last_significant_coeff_flag = new int[numCoeff - 1];
            while (i < numCoeff - 1)
            {
                significant_coeff_flag[i] = 0; // Read from the stream ae discriptor.
                if (significant_coeff_flag[i] > 0)
                {
                    last_significant_coeff_flag[i] = 0; // Read from the stream ae discriptor.
                    if (last_significant_coeff_flag[i] > 0)
                    {
                        numCoeff = i + 1;
                    }
                }
                i++;
            }
            coeff_abs_level_minus1 = new int[numCoeff - 1];
        }
        return coeffLevels;
    }

    public int coded_block_flag { get; set; }
    public int[]? significant_coeff_flag { get; set; }
    public int[]? last_significant_coeff_flag { get; set; }
    public int[]? coeff_abs_level_minus1 { get; set; }
    public int[]? coeff_sign_flag { get; set; }
}


// CAVLC implementation of ResidualBlock syntax structure
public class ResidualBlockCAVLC : ICoefficients
{
    private BitList bitStream;

    private int suffixLength = 0;
    private int[]? levelVal;
    private int levelCode = 0;

    public ResidualBlockCAVLC(BitList BitList)
    {
        bitStream = BitList;
    }

    public int[] GetCoefficients(int[] coeffLevels, int startIdx, int endIdx, int maxNumCoeff)
    {
        for (int i = 0; i < maxNumCoeff; i++)
        {
            coeffLevels[i] = 0;
        }
        coeff_token = getCoeffToken(bitStream);
        levelVal = new int[coeff_token.Item1];

        if (coeff_token.Item1 > 0)
        {
            if (coeff_token.Item1 > 10 && coeff_token.Item2 < 3)
            {
                suffixLength = 1;
            }
            else
            {
                suffixLength = 0;
            }

            for (int i = 0; i < coeff_token.Item1; i++)
            {
                if (i < coeff_token.Item2)
                {
                    trailing_ones_sign_flag = bitStream.u(1) == 1;
                    levelVal[i] = 1 - 2 * (!trailing_ones_sign_flag ? 0 : 1);
                }
                else
                {
                    level_prefix = getLevelPrefix(bitStream);
                    levelCode = (Math.Min(15, level_prefix) << suffixLength);
                    if (suffixLength > 0 || level_prefix >= 14)
                    {
                        level_suffix = (int)bitStream.u((uint)suffixLength);
                        levelCode += level_suffix;
                    }
                    if (level_prefix >= 15 && suffixLength == 0)
                    {
                        levelCode += 15;
                    }

                    if (level_prefix >= 16)
                    {
                        levelCode += (1 << (level_prefix - 3)) - 4096;
                    }

                    if (i == coeff_token.Item1 && coeff_token.Item2 < 3)
                    {
                        levelCode += 2;
                    }

                    if (levelCode % 2 == 0)
                    {
                        levelVal[i] = (levelCode + 2) >> 1;
                    }
                    else
                    {
                        levelVal[i] = (-levelCode - 1) >> 1;
                    }

                    if (suffixLength == 0)
                    {
                        suffixLength = 1;
                    }

                    if (Math.Abs(levelVal[i]) > (3 << (suffixLength - 1)) && suffixLength < 6)
                    {
                        suffixLength++;
                    }
                }

                if (coeff_token.Item1 < endIdx - startIdx + 1)
                {

                }
            }
        }
        return coeffLevels;
    }

    private int getLevelPrefix(BitList bitStream)
    {
        throw new NotImplementedException();
    }

    private Tuple<int, int> getCoeffToken(BitList bitList)
    {
        try
        {
            using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\TotalCoeff(coeff_token).json"))
            {
                List<CoeffTokenMapping>? Tokenmappings = JsonSerializer.Deserialize<List<CoeffTokenMapping>>(streamReader.ReadToEnd());
                Tokenmappings = Tokenmappings != null ? Tokenmappings : new List<CoeffTokenMapping>();
                int nC = 0;
                var currentMappings = new List<CurrentTokenMapping>();

                if (nC >= 0 && nC < 2)
                {
                    currentMappings = (from tm in Tokenmappings
                                        select new CurrentTokenMapping() {
                                            CoeffTokens = tm.CoeffTokens,
                                            TrailingOnes = tm.TrailingOnes,
                                            Token = tm._0_LessOrEqual_nC_Less_2
                                        }).ToList();
                } else if(nC >= 2 && nC < 4)
                {
                    currentMappings = (from tm in Tokenmappings
                                        select new CurrentTokenMapping() {
                                            CoeffTokens = tm.CoeffTokens,
                                            TrailingOnes = tm.TrailingOnes,
                                            Token = tm._2_LessorEqual_nC_Less_4
                                        }).ToList();
                } else if (nC >= 4 && nC < 8)
                {
                    currentMappings = (from tm in Tokenmappings
                                        select new CurrentTokenMapping() {
                                            CoeffTokens = tm.CoeffTokens,
                                            TrailingOnes = tm.TrailingOnes,
                                            Token = tm._4_LessorEqual_nC_Less_8
                                        }).ToList();
                } else if(nC >= 8)
                {
                    currentMappings = (from tm in Tokenmappings
                                        select new CurrentTokenMapping() {
                                            CoeffTokens = tm.CoeffTokens,
                                            TrailingOnes = tm.TrailingOnes,
                                            Token = tm._8_LessorEqual_nC
                                        }).ToList();
                } else if(nC == -1)
                {
                    currentMappings = (from tm in Tokenmappings
                                        select new CurrentTokenMapping() {
                                            CoeffTokens = tm.CoeffTokens,
                                            TrailingOnes = tm.TrailingOnes,
                                            Token = tm.nC_Equal_Minus1
                                        }).ToList();
                } else if (nC == -2)
                {
                    currentMappings = (from tm in Tokenmappings
                                        select new CurrentTokenMapping() {
                                            CoeffTokens = tm.CoeffTokens,
                                            TrailingOnes = tm.TrailingOnes,
                                            Token = tm.nC_Equal_Minus2
                                        }).ToList();
                }
                var coeffTokenMapping = (from tm in currentMappings
                                         where tm.Token == bitStream.next_bits((uint)tm.Token.Length)
                                         select tm).FirstOrDefault();
                coeffTokenMapping = coeffTokenMapping != null ? coeffTokenMapping : 
                                                    new CurrentTokenMapping()
                                                    { 
                                                        CoeffTokens = -1, 
                                                        Token = string.Empty,
                                                        TrailingOnes = -1
                                                    };
                return new Tuple<int, int>(coeffTokenMapping.CoeffTokens, coeffTokenMapping.TrailingOnes);
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public Tuple<int, int> coeff_token { get; set; } = default!;
    public bool trailing_ones_sign_flag { get; set; }
    public int level_prefix { get; set; }
    public int level_suffix { get; set; }
}

// Create a class for ResidualLuma syntax element
public class ResidualLuma
{
    ICodecSettingsService settingsService;
    private ICoefficients Coefficients;
    private MicroblockTypes macroblockType;
    private MacroblockLayer macroblockLayer;
    private PPS PPS;
    private SPS SPS;
    private GlobalVariables GlobalVariables;

    private int StartIdx, EndIdx, MaxNumCoeff;
    private int[] I16x16DCLevel = new int[16];
    private int[,] I16x16ACLevel = new int[16, 15];
    private int[,] Level4x4 = new int[16, 16];
    private int[,] Level8x8 = new int[4, 16];

    public ResidualLuma(ICoefficients coefficients, ICodecSettingsService service)
    {
        settingsService = service;
        SettingSets settingSets = settingsService.GetCodecSettings();
        PPS = settingSets.GetPPS;
        SPS = settingSets.GetSPS;
        GlobalVariables = settingSets.GlobalVariables;
        macroblockLayer = settingSets.Extras.MacroblockLayer;
        SynElemSlice synElemSlice = new SynElemSlice();
        synElemSlice.Slicetype = settingSets.SliceHeader.slice_type;
        synElemSlice.SynElement = SynElement.mb_type;

        macroblockType = new MicroblockTypes(service, synElemSlice);
        Coefficients = coefficients;
    }

    public void GetResidualLuma(out int[] i16x16DCLevel,
    out int[,] i16x16ACLevel, out int[,] level4x4,
    out int[,] level8x8, int startIdx, int endIdx)
    {
        if (startIdx == 0 && macroblockType.MbPartPredMode(macroblockLayer.mb_type, 0) == PredictionModes.Intra_16x16)
        {
            I16x16DCLevel = Coefficients.GetCoefficients(I16x16DCLevel, 0, 15, 16);
        }

        for (int i8x8 = 0; i8x8 < 4; i8x8++)
        {
            if (!macroblockLayer.transform_size_8x8_flag || !PPS.entropy_coding_mode_flag)
            {
                for (int i4x4 = 0; i4x4 < 4; i4x4++)
                {
                    if ((GlobalVariables.CodedBlockPatternLuma & (1 << i8x8)) > 0)
                    {
                        if (macroblockType.MbPartPredMode(macroblockLayer.mb_type, 0) == PredictionModes.Intra_16x16)
                        {
                            int[] tempCoefficient = new int[15];
                            int[] coefficients = Coefficients.GetCoefficients(tempCoefficient,
                            Math.Max(0, startIdx - 1), endIdx - 1, 15);
                            I16x16ACLevel = h264Array.Copy2DArray(I16x16ACLevel, i8x8 * 4 + i4x4, coefficients, 0, coefficients.Length);
                        }
                        else
                        {
                            int[] tempCoefficient = new int[16];
                            int[] coefficients = Coefficients.GetCoefficients(tempCoefficient,
                            startIdx, endIdx, 16);
                            Level4x4 = h264Array.Copy2DArray(Level4x4, i8x8 * 4 + i4x4, coefficients, 0, coefficients.Length);
                        }
                    }
                    else if (macroblockType.MbPartPredMode(macroblockLayer.mb_type, 0) == PredictionModes.Intra_16x16)
                    {
                        for (int i = 0; i < 15; i++)
                        {
                            I16x16ACLevel[i8x8 * 4 + i4x4, i] = 0;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Level4x4[i8x8 * 4 + i4x4, i] = 0;
                        }
                    }

                    if (!PPS.entropy_coding_mode_flag && macroblockLayer.transform_size_8x8_flag)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Level8x8[i8x8, 4 * i + i4x4] = Level4x4[i8x8 * 4 + i4x4, i];
                        }
                    }
                }
            }
            else if (((GlobalVariables.CodedBlockPatternChroma) & (1 << i8x8)) > 0)
            {
                int[] tempCoefficient = new int[64];
                int[] coefficients = Coefficients.GetCoefficients(tempCoefficient, 4 * startIdx, 4 * endIdx + 3, 64);
                h264Array.Copy2DArray(Level8x8, i8x8, coefficients, 0, coefficients.Length);
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    Level8x8[i8x8, i] = 0;
                }
            }
        }
        i16x16DCLevel = I16x16DCLevel;
        i16x16ACLevel = I16x16ACLevel;
        level4x4 = Level4x4;
        level8x8 = Level8x8;
    }
}

public class Residual
{
    public int[] Intra16x16DCLevel = new int[16];
    public int[,] Intra16x16ACLevel = new int[16, 15];
    public int[,] LumaLevel4x4 = new int[16, 16];
    public int[,] LumaLevel8x8 = new int[4, 16];
    public int[,] ChromaDCLevel;
    public int[,,] ChromaACLevel;
    public int[] CbIntra16x16DCLevel { get; set; } = new int[16];
    public int[,] CbIntra16x16ACLevel { get; set; } = new int[16, 15];
    public int[,] CbLevel4x4 { get; set; } = new int[16, 16];
    public int[,] CbLevel8x8 { get; set; } = new int[4, 16];
    public int[] CrIntra16x16DCLevel { get; set; } = new int[16];
    public int[,] CrIntra16x16ACLevel { get; set; } = new int[16, 15];
    public int[,] CrLevel4x4 { get; set; } = new int[16, 16];
    public int[,] CrLevel8x8 { get; set; } = new int[4, 16];

    // Propeties for Loading Data from tables
}

public class Extras
{
    [JsonPropertyName("mcrb")]
    public MacroblockLayer MacroblockLayer { get; set; } = default!;
}

public class ChromaArrayIdc
{
    [JsonPropertyName("coded_block_pattern_mapping_chroma_is_1_or_2")]
    public List<CodedBlockArrayType> CodedBlockPatternsChroma1or2 { get; set; } = new List<CodedBlockArrayType>();
    [JsonPropertyName("coded_block_pattern_mapping_chroma_is_0_or_3")]
    public List<CodedBlockArrayType> CodedBlockPatternsChroma0or3 { get; set; } = new List<CodedBlockArrayType>();
}

public class CodedBlockArrayType
{
    [JsonPropertyName("code_num")]
    public int CodeNum { get; set; }
    [JsonPropertyName("code_block_pattern")]
    public CodedBlockPattern CodedBlockPattern { get; set; } = default!;
}
public class CodedBlockPattern
{
    [JsonPropertyName("intra_4x4_intra_8x8")]
    public int Intra4x4_8x8 { get; set; }
    [JsonPropertyName("inter")]
    public int Inter { get; set; }
}
public interface ICodecSettingsService
{
    PropertyInfo Get<T>(T type, string key);
    object Set<T, VT>(T type, string key, VT value);
    void Update<T>(T Updates);
    SettingSets GetCodecSettings();
}
public enum SynElement
{
    mb_type,
    mb_skip_flag,
    sub_mb_type,
    mb_field_decoding_flag,
    transform_8x8_mode_flag
}

public class CtxTable
{
    [JsonPropertyName("CtxIdx")]
    public int CtxIdx { get; set; }
    [JsonPropertyName("m")]
    public int M { get; set; }
    [JsonPropertyName("n")]
    public int N { get; set; }
}

public class ContextVariable
{
    public int CtxIdx { get; set; }
    public int M { get; set; }
    public int N { get; set; }

    public int PreCtxState { get; set; }
    public int ValMPS { get; set; }
    public int PStateIdx { get; set; }

    public bool IsInitialized { get; set; }
}

public class SynElemSlice
{
    public SynElement SynElement { get; set; }
    public Slicetype Slicetype { get; set; }
    public SettingSets? settingSets { get; set; }
}

public enum SliceGroupMapType
{
    BoxOutClockwise = 0,
    BoxOutCounterClockwise = 1,
    RasterScan = 2,
    ReverseRasterScan = 3,
    WipeRight = 4,
    WipeLeft = 5
}

public enum PredictionModes
{
    Intra_4x4 = 0,
    Intra_8x8 = 1,
    Intra_16x16 = 2,
    NA = 3,
    Invalid = 4,
    Pred_L0 = 5,
    P_L0_8x8 = 6,
    P_L0_8x4 = 7,
    P_L0_4x8 = 8,
    P_L0_4x4 = 9,
    Direct = 10,
    Pred_L1 = 11,
    BiPred = 12,
}


public enum NumMbPartModes
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    NA = 5,
    Defult = 1000
}
public class NumMbPartions
{
    [JsonPropertyName("num_part")]
    public int NumberOfPart { get; set; }
    [JsonPropertyName("mb_part_pred_mode")]
    public PredictionModes PredictionModes { get; set; }
}

// Width and Height Partition modes
public enum WidthHeightPartMode
{
    Four = 4,
    Eight = 8,
    Sixteen = 16,
    NA = 17
}

public class SliceMicroblock
{
    [JsonPropertyName("mb_type")]
    public uint MbType { get; set; }
    [JsonPropertyName("name_of_microblock_type")]
    public string NameOfMb { get; set; } = string.Empty;
    [JsonPropertyName("transform_size_8x8_flag")]
    public TransformFlags TransformSize8x8 { get; set; }

    [JsonPropertyName("mb_partitions")]
    public List<NumMbPartions> NumMbPartions { get; set; } = new List<NumMbPartions>();
}

public class BSliceMacroblock : SliceMicroblock
{
    [JsonPropertyName("num_mb_part")]
    public NumMbPartModes? NumMbPart { get; set; } = NumMbPartModes.Defult;
    [JsonPropertyName("mb_part_width")]
    public WidthHeightPartMode MbPartWidth { get; set; }
    [JsonPropertyName("mb_part_height")]
    public WidthHeightPartMode MbPartHeight { get; set; }
}

/// <summary>
/// The type to represent the B slice sub macroblocks
/// </summary>
public class MacroblockSlice
{
    public List<BSliceMacroblock> BSliceMacroblock { get; set; } = new List<BSliceMacroblock>();
    public List<PandSP_SliceMicroblock> PandSPSliceMacroblock { get; set; } = new List<PandSP_SliceMicroblock>();
    public List<SliceSubMacroblock> BSubMacroblock { get; set; } = new List<SliceSubMacroblock>();
}

public enum TransformFlags
{
    True = 1,
    False = 0,
    NA = 2
}

public enum ChromaFormatIdc
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3
}
public enum ChromaTypes
{
    Monochrome = 0,
    FourTwoZero = 1,
    FourTwoTwo = 2,
    FourFourFour = 3
}
public enum SubWidthHeight
{
    Undefined = 0,
    One = 1,
    Two = 2
}

public enum CodedBlockPatternChromaValue
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    NA = 4,
    Equation = 5
}

public enum Intra16x16PredMode
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    NA = 4
}

public enum CodedBlockPatterLumaValue
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Fourt,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Eleven = 11,
    Twelve = 12,
    Thirteen = 13,
    Fourteen = 14,
    Fifteen = 15,
    Sixteen = 16,
    NA = 17
}

public enum MicroblockType
{
    I_NxN = 0,
    I_16x16 = 1,
    I_16x16_1_0_0 = 2,
    I_16x16_2_0_0 = 3,
    I_16x16_3_0_0 = 4,
    I_16x16_0_1_0 = 5,
    I_16x16_1_1_0 = 6,
    I_16x16_2_1_0 = 7,
    I_16x16_3_1_0 = 8,
    I_16x16_0_2_0 = 9,
    I_16x16_1_2_0 = 10,
    I_16x16_2_2_0 = 11,
    I_16x16_3_2_0 = 14,
    I_16x16_0_0_1 = 13,
    I_16x16_1_0_1 = 14,
    I_16x16_2_0_1 = 15,
    I_16x16_3_0_1 = 16,
    I_16x16_0_1_1 = 17,
    I_16x16_1_1_1 = 18,
    I_16x16_2_1_1 = 19,
    I_16x16_3_1_1 = 20,
    I_16x16_0_2_1 = 21,
    I_16x16_1_2_1 = 22,
    I_16x16_2_2_1 = 23,
    I_16x16_3_2_1 = 24,
    I_PCM = 25,
    P_L0_16x16 = 26,
    P_L0_L0_16x8 = 27,
    P_L0_L0_8x16 = 28,
    P_8x8 = 29,
    P_8x8ref0 = 30,
    P_Skip = 31
}

public class I_SliceMicroblock : SliceMicroblock
{
    [JsonPropertyName("intra16x16_pred_mode")]
    public Intra16x16PredMode Intra16x16PredMode { get; set; }
    [JsonPropertyName("coded_block_pattern_chroma")]
    public CodedBlockPatternChromaValue CodedBlockPatternChroma { get; set; }
    [JsonPropertyName("coded_block_pattern_luma")]
    public CodedBlockPatterLumaValue CodedBlockPatternLuma { get; set; }
}

public class SI_SliceMicroblock : I_SliceMicroblock
{

}

public class SliceSubMacroblock
{
    [JsonPropertyName("sub_mb_type")]
    public uint SubMbType { get; set; }
    [JsonPropertyName("name_of_microblock_type")]
    public string? SubMacroblockName { get; set; }
    [JsonPropertyName("num_sub_mb_part")]
    public NumMbPartModes NumMbPart { get; set; }
    [JsonPropertyName("sub_mb_pred_mode")]
    public PredictionModes SubMbPredMode { get; set; }
    [JsonPropertyName("sub_mb_part_width")]
    public WidthHeightPartMode SubMbPartWidth { get; set; }
    [JsonPropertyName("sub_mb_part_height")]
    public WidthHeightPartMode SubMbPartHeight { get; set; }
}

public class PandSP_SliceMicroblock : SliceMicroblock
{
    [JsonPropertyName("mb_part_width")]
    public WidthHeightPartMode MbPartWidth { get; set; }
    [JsonPropertyName("mb_part_height")]
    public WidthHeightPartMode MbPartHeight { get; set; }
    [JsonPropertyName("num_mb_part")]
    public NumMbPartModes NumMbPart { get; set; }
}

// A type that will be used to get the MicroblockTables
public class SliceTypeMicroblock
{
    public List<BSliceMacroblock> BSliceMacroblocks { get; set; } = new List<BSliceMacroblock>();
    public List<I_SliceMicroblock> ISliceMicroblocks { get; set; } = new List<I_SliceMicroblock>();
    public List<PandSP_SliceMicroblock> PandSPSliceMacroblocks { get; set; } = new List<PandSP_SliceMicroblock>();
    public List<SliceSubMacroblock> PSubMacroblocks { get; set; } = new List<SliceSubMacroblock>();
    public List<SliceSubMacroblock> BSubMacroblocks { get; set; } = new List<SliceSubMacroblock>();
}

public class ChromaFormat
{
    [JsonPropertyName("chroma_format_idc")]
    public ChromaFormatIdc ChromaFormatIdc { get; set; }
    [JsonPropertyName("separate_colour_plane_flag")]
    public uint SeparateColorPlaneFlag { get; set; }
    [JsonPropertyName("chroma_format")]
    public ChromaFormatIdc Format { get; set; }
    [JsonPropertyName("sub_width_c")]
    public SubWidthHeight SubWidthC { get; set; }
    [JsonPropertyName("sub_height_c")]
    public SubWidthHeight SubHeightC { get; set; }
}