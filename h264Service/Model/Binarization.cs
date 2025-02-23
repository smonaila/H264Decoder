

using System.Text.Json;
using ContextVarTables;
using h264.NALUnits;
using H264.Global.Variables;
using H264.Types;
using MathExtensionMethods;
using static h264.NALUnits.SliceHeader;

namespace SynElem.Binarization
{
    public class CtxIncrements
    {
        public int CtxOffset { get; set; }
        public List<BinIndex> CtxIdxIncrements { get; set; } = new List<BinIndex>();
    }

    public class BinIndex
    {
        public int Idx { get; set; }
        public List<int> Values { get; set; } = new List<int>();
        public int CtxIdx { get; set; }
    }

    public class JsonCtxInc
    {
        public List<CtxIncrements>? CtxIdxIncrements { get; set; }
    }

    // Macroblock address types.
    public class MbAddress
    {
        public int Address { get; set; }
        public bool Available { get; set; }
    }

    public class BinarizationSchemes
    {
        private SliceHeader sliceHeader;
        private ICodecSettingsService settingsService;
        private GlobalVariables GlobalVariables;
        private MbAddress mbAddrA, mbAddrB, mbAddrC, mbAddrD;

        // public BinarizationSchemes(ICodecSettingsService codecSettingsService)
        // {
        //     settingsService = codecSettingsService;
        //     SettingSets settingSets = settingsService.GetCodecSettings();
        //     GlobalVariables = settingSets.GlobalVariables;
        // }

        /// <summary>
        /// Initialize all context variable.
        /// </summary>
        /// <returns></returns>
        public List<ContextVariable> Initialization(SynElemSlice synElemSlice)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\ContextVariables.json"))
                {
                    var ctxVarTables = JsonSerializer.Deserialize<VarTables>(streamReader.ReadToEnd());
                    List<ContextVariable> contextVariables = new List<ContextVariable>();                    
                    SettingSets? settingSets = synElemSlice.settingSets;
                    GlobalVariables = settingSets != null ? settingSets.GlobalVariables : new GlobalVariables();

                    if (synElemSlice.Slicetype == Slicetype.P || synElemSlice.Slicetype == Slicetype.SP)
                    {
                        if (synElemSlice.SynElement == SynElement.mb_skip_flag && sliceHeader.cabac_init_idc == 0)
                        {                            
                            // Table9_13.CabacInt.CabacIntOne;
                        }

                        if (synElemSlice.SynElement == SynElement.mb_skip_flag && sliceHeader.cabac_init_idc == 1)
                        {
                            // 

                        }
                    }

                    if (synElemSlice.Slicetype == Slicetype.I)
                    {
                        // First
                        if (synElemSlice.SynElement == SynElement.mb_type)
                        {                           
                            if (ctxVarTables != null)
                            {
                                List<CtxTable> variables = ctxVarTables.Table9_12.CtxTable;
                                for (int i = 0; i < variables.Count; i++)
                                {
                                    ContextVariable contextVariable = new ContextVariable();
                                    contextVariable.CtxIdx = variables[i].CtxIdx;
                                    contextVariable.M = variables[i].M;
                                    contextVariable.N = variables[i].N;
                                    contextVariables.Add(contextVariable);
                                }   
                            }                        
                        }
                    }                    

                    foreach (var variableIdx in contextVariables)
                    {
                        variableIdx.PreCtxState = Mathematics.Clip3(1, 126, ((variableIdx.M * Mathematics.Clip3(0, 51, GlobalVariables.SliceQPY)) >> 4) + variableIdx.N);
                        if (variableIdx.PreCtxState <= 63)
                        {
                            variableIdx.PStateIdx = 63 - variableIdx.PreCtxState;
                            variableIdx.ValMPS = 0;
                        } else
                        {
                            variableIdx.PStateIdx = variableIdx.PreCtxState - 64;
                            variableIdx.ValMPS = 1;
                        }
                        variableIdx.IsInitialized = true;
                    }
                    return contextVariables;
                }
            }
            catch (System.Exception)
            {                
                throw;
            }
        }

        /// <summary>
        /// The method to derive the CtxIdx for the given binIdx as stipulated in clause 9.3.3.1
        /// </summary>
        /// <param name="binIdx">The bin index for which to get the context index.</param>
        /// <param name="maxBinIdxCtx">The maxBinIdxCtx for binIdx.</param>
        /// <param name="ctxIdxOffset"></param>
        /// <returns name="ctxIdx">The context variable value for the given binIdx</returns>
        public int GetCtxIdx(int binIdx, int maxBinIdxCtx, int ctxIdxOffset)
        {
            // Load context increment table
            using (StreamReader streamReader = new StreamReader("SliceMicroblockTables.json"))
            {
                string jsonInc = streamReader.ReadToEnd();
                JsonCtxInc? ctxIdxIncrements = JsonSerializer.Deserialize<JsonCtxInc>(jsonInc);
                ctxIdxIncrements = ctxIdxIncrements == null ? new JsonCtxInc() : ctxIdxIncrements; 

                List<CtxIncrements> _ctxIncrements = ctxIdxIncrements.CtxIdxIncrements == null ? new List<CtxIncrements>() : ctxIdxIncrements.CtxIdxIncrements;
                CtxIncrements? ctxIncrement = (from ctxInc in _ctxIncrements
                                               where ctxInc.CtxOffset == ctxIdxOffset
                                               select ctxInc).FirstOrDefault();
                ctxIncrement = ctxIncrement == null ? new CtxIncrements() : ctxIncrement;
                ctxIncrement.CtxIdxIncrements = ctxIncrement.CtxIdxIncrements == null ? new List<BinIndex>() : ctxIncrement.CtxIdxIncrements;
                
                BinIndex? BinCtxIdx = (from binIdxInc in ctxIncrement.CtxIdxIncrements
                                where binIdxInc.Idx == binIdx
                                select binIdxInc).FirstOrDefault();
                BinCtxIdx = BinCtxIdx == null ? new BinIndex() : BinCtxIdx;

                if (BinCtxIdx.CtxIdx == 276)
                {
                    return 276;
                }
                else
                {
                    if (BinCtxIdx.Values.Count > 1)
                    {
                        // Call the Derive GetctcIdxInc method to derive the CtxIdxInc
                        int CtxIdxInc = GetctxIdxInc(ctxIdxOffset) + ctxIdxOffset;
                        return CtxIdxInc;
                    }
                    else
                    {
                        return BinCtxIdx.Values[0] + ctxIdxOffset;
                    }                    
                }                
            }             
        }

        /// <summary>
        /// Further process the ctxIdxInc for the given ctxIdxOffset.
        /// </summary>
        /// <param name="ctxIdxOffset">The ctxIdxOffset</param>
        /// <returns>ctxIdxInc</returns>
        public int GetctxIdxInc(int ctxIdxOffset)
        {
            int ctxIdxInc = 0;
            int condTermFlagA = 0, condTermFlagB = 0;

            // Clause 6.4.12.1 Specificstion for neighbouring locations in the files and non-MBFF frame.
            Tuple<MbAddress, MbAddress, MbAddress, MbAddress> mbAddrTuple = NeighbouringLocNonMBAFF();
            MbAddress mbAddressA = mbAddrTuple.Item1;
            MbAddress mbAddressB = mbAddrTuple.Item2;

            mbAddressA = GetMbAvailability(mbAddressA);
            mbAddressB = GetMbAvailability(mbAddressB);

            ctxIdxInc = condTermFlagA + condTermFlagB;
            return ctxIdxInc;
        }


        private Tuple<MbAddress, MbAddress, MbAddress, MbAddress> NeighbouringLocNonMBAFF()
        {
            // Clause 6.4.9 Derivation process for neighbouring macroblock addresses and their availability.
            mbAddrA.Address = GlobalVariables.CurrMbAddr - 1;
            mbAddrA.Available = GlobalVariables.CurrMbAddr % GlobalVariables.PicWidthInMbs == 0;

            mbAddrB.Address = GlobalVariables.CurrMbAddr - GlobalVariables.PicWidthInMbs;

            mbAddrC.Address = GlobalVariables.CurrMbAddr - GlobalVariables.PicWidthInMbs - 1;
            mbAddrC.Available = (GlobalVariables.CurrMbAddr + 1) % GlobalVariables.PicWidthInMbs == 0;

            mbAddrD.Address = GlobalVariables.CurrMbAddr - GlobalVariables.PicWidthInMbs - 1;
            mbAddrD.Available = GlobalVariables.CurrMbAddr % GlobalVariables.PicWidthInMbs == 0;

            return Tuple.Create(mbAddrA, mbAddrB, mbAddrC, mbAddrD);
        }

        /// <summary>
        /// This is the method that is going to process the availability of the Macroblock.
        /// </summary>
        /// <param name="mbAddress">The macroblock to process</param>
        /// <returns></returns>
        public MbAddress GetMbAvailability(MbAddress mbAddress)
        {
            // Search the given address if it is in the same Slice
            if (mbAddress.Address < 0 || mbAddress.Address > GlobalVariables.CurrMbAddr)
            {
                mbAddress.Available = false;
            }
            return mbAddress;
        }

        // Clause 9.3.2.1
        public string Unary(int synElVal)
        {
            string binString = "1";
            for (int i = 1; i < synElVal; i++)
            {
                binString = string.Format("{0}{1}", binString, 1);
            }
            binString = string.Format("{0}{1}", binString, 0);
            return binString;
        }

        /// <summary>
        /// Decoding process flow Clause 9.3.3 
        /// </summary>
        /// <param name="binString">binarization of the requested syntax element</param>
        /// <param name="maxBinIdxCtx">Maximum Binary Context Index</param>
        /// <param name="ctxIdxOffset">The offset of the Binary Index</param>
        /// <param name="bypassFlag">The bypass decoding flag</param>
        /// <returns>The value of the syntax element</returns>
        public int GetSynVal(string binString, int maxBinIdxCtx, int ctxIdxOffset, bool bypassFlag)
        {
            int binstrLength = binString.Length;
            string strResult = string.Empty;

            for (int i = 0; i < binstrLength; i++)
            {
                strResult = string.Format("{0}{1}", strResult, binString.Substring(i, 1));
                sliceHeader.slice_type = Slicetype.I;
            }
            return Convert.ToInt32(strResult, 2);
        }
    }
}