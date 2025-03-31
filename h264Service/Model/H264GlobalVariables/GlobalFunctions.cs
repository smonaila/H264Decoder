
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using h264.NALUnits;
using H264.Global.Variables;
using H264.Types;

namespace H264.Global.Methods;
public class GlobalFunctions
{
    private ICodecSettingsService settingsService;
    private SliceHeader SliceHeader;
    private PPS Pps;
    private SPS Sps;
    private GlobalVariables GlobalVariables;

    public GlobalFunctions()
    {
        settingsService = new CodecSettings();
        SliceHeader = new SliceHeader();
        Pps = new PPS();
        Sps = new SPS();
        GlobalVariables = new GlobalVariables();
    }

    public GlobalFunctions(ICodecSettingsService service, SliceHeader SliceHeader)
    {
        settingsService = service;
        SettingSets codecSettings = settingsService.GetCodecSettings();
        this.Pps = codecSettings.GetPPS;
        this.GlobalVariables = codecSettings.GlobalVariables;
        this.Sps = codecSettings.GetSPS;
        this.SliceHeader = SliceHeader;
    }

     public CAVLCSettings GetCAVLCSettings()
    {
        try
        {
            using (StreamReader streamReader = new StreamReader(@"C:\H264Decoder\h264Service\Data\currmbsettings.json"))
            {
                CAVLCSettings? CavlcSettings = JsonSerializer.Deserialize<CAVLCSettings>(streamReader.ReadToEnd());
                CavlcSettings = CavlcSettings != null ? CavlcSettings : new CAVLCSettings();
                
                return CavlcSettings;
            }
        }
        catch (System.Exception)
        {
            
            throw;
        }
    }

    public int NextMbAddress(int CurrMbAddr)
    {
        int[] mapUnitToSliceGroupMap = new int[GlobalVariables.PicSizeInMbs - 1];

        if (Pps.num_slice_groups_minus1 == 0)
        {
            for (int i = 0; i < mapUnitToSliceGroupMap.Length; i++)
            {
                mapUnitToSliceGroupMap[i] = 0;
            }
        }

        if (Pps.num_slice_groups_minus1 != 0 && Pps.slice_group_map_type == 0)
        {
            mapUnitToSliceGroupMap = GetMapUnitsToSliceGroup0(Pps, SliceHeader);
        } else if (Pps.num_slice_groups_minus1 != 0 && Pps.slice_group_map_type == 1)
        {
            mapUnitToSliceGroupMap = GetMapUnitsToSliceGroup1(Pps, SliceHeader);
        } else if (Pps.num_slice_groups_minus1 != 0 && Pps.slice_group_map_type == 2)
        {
            mapUnitToSliceGroupMap = GetMapUnitsToSliceGroup2(Pps, SliceHeader);
        } else if (Pps.num_slice_groups_minus1 != 0 && Pps.slice_group_map_type == 3)
        {
            mapUnitToSliceGroupMap = GetMapUnitsToSliceGroup3(Pps, SliceHeader);
        } else if (Pps.num_slice_groups_minus1 == 1 && (Pps.slice_group_map_type == 4 || Pps.slice_group_map_type == 5))
        {
            mapUnitToSliceGroupMap = GetMapUnitsToSliceGroup4(Pps, SliceHeader);
        }   

        int[] MbToSliceGroupMap = GetMbToSliceGroupMap(mapUnitToSliceGroupMap);
        int index = CurrMbAddr + 1;
        while (index < GlobalVariables.PicSizeInMbs && MbToSliceGroupMap[index] != MbToSliceGroupMap[CurrMbAddr])
        {
            index++;
        }
        int NextMbAddress = index;
        return NextMbAddress;
    }

    private int[] GetMbToSliceGroupMap(int[] mapUnitToSliceGroupMap)
    {
        try
        {
            int[] MbToSliceGroupMap = new int[GlobalVariables.PicSizeInMbs - 1];
            for (int i = 0; i < MbToSliceGroupMap.Length; i++)
            {
                if (Sps.frame_mbs_only_flag || SliceHeader.field_pic_flag)
                {
                    MbToSliceGroupMap[i] = mapUnitToSliceGroupMap[i];
                } else if(GlobalVariables.MbaffFrameFlag == 1)
                {
                    MbToSliceGroupMap[i] = mapUnitToSliceGroupMap[ i / 2];
                } else if (!Sps.frame_mbs_only_flag && Sps.mb_adaptive_frame_field_flag == 0 && !SliceHeader.field_pic_flag)
                {
                    MbToSliceGroupMap[i] = mapUnitToSliceGroupMap[i / (2 * GlobalVariables.PicWidthInMbs) * GlobalVariables.PicWidthInMbs + (i % GlobalVariables.PicWidthInMbs)];
                }
            }
            return MbToSliceGroupMap;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    /// Clause 8.2.2.1
    public int[] GetMapUnitsToSliceGroup0(PPS Pps, SliceHeader sliceHeader)
    {
        try
        {
            int[] mapUnitToSliceGroupMap = new int[Pps.NalUnitLength];
            int i = 0;
            do
            {
                for (int iGroup = 0; iGroup <= Pps.num_slice_groups_minus1 && i < GlobalVariables.PicSizeInMapUnits; 
                i += (int)Pps.run_length_minus1[iGroup++] + 1)
                {
                    for (int j = 0; j < Pps.run_length_minus1[iGroup] && i + j < GlobalVariables.PicSizeInMapUnits; j++)
                    {
                        mapUnitToSliceGroupMap[i + j] = iGroup;
                    }
                }
            } while (i < GlobalVariables.PicSizeInMapUnits);
            return mapUnitToSliceGroupMap;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    /// Clause 8.2.2.2
    public int[] GetMapUnitsToSliceGroup1(PPS Pps, SliceHeader sliceHeader)
    {
        try
        {
            int[] mapUnitToSliceGroupMap = new int[Pps.NalUnitLength];
            GlobalVariables globalVariables = new GlobalVariables();
            for (int i = 0; i < globalVariables.PicSizeInMapUnits; i++)
            {
                mapUnitToSliceGroupMap[i] = (int)(((i % globalVariables.PicWidthInMbs) + 
                                            (i / globalVariables.PicWidthInMbs * (Pps.num_slice_groups_minus1 + 1) / 2))
                                            % (Pps.num_slice_groups_minus1 + 1));
            }
            return mapUnitToSliceGroupMap;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    /// Clause 8.2.2.2
    public int[] GetMapUnitsToSliceGroup2(PPS Pps, SliceHeader sliceHeader)
    {
        try
        {
            int[] mapUnitToSliceGroupMap = new int[Pps.NalUnitLength];
            GlobalVariables globalVariables = new GlobalVariables();
           if (Pps.num_slice_groups_minus1 != 0 && Pps.slice_group_map_type == 2)
           {
                for (int i = 0; i < globalVariables.PicSizeInMapUnits; i++)
                {
                    mapUnitToSliceGroupMap[i] = (int)Pps.num_slice_groups_minus1;
                    for (int iGroup = (int)Pps.num_slice_groups_minus1 - 1; iGroup >= 0; iGroup--)
                    {
                        var yTopLeft = (int)Pps.top_left[iGroup] / globalVariables.PicWidthInMbs;
                        var xTopLeft = (int)Pps.top_left[iGroup] % globalVariables.PicWidthInMbs;
                        var yBottomRight = (int)Pps.bottom_right[iGroup] / globalVariables.PicWidthInMbs;
                        var xBottomRight = (int)Pps.bottom_right[iGroup] % globalVariables.PicWidthInMbs;

                        for (int y = yTopLeft; y <= yBottomRight; y++)
                        {
                            for (int x = xTopLeft; x <= xBottomRight; xBottomRight++)
                            {
                                mapUnitToSliceGroupMap[y * globalVariables.PicWidthInMbs + x] = iGroup;
                            }
                        }
                    }
                }
            }
            return mapUnitToSliceGroupMap;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public int[] GetMapUnitsToSliceGroup3(PPS Pps, SliceHeader sliceHeader)
    {
        try
        {
            int[] mapUnitToSliceGroupMap = new int[Pps.NalUnitLength];
            GlobalVariables globalVariables = new GlobalVariables();

            for (int i = 0; i < globalVariables.PicSizeInMapUnits; i++)
            {
                mapUnitToSliceGroupMap[i] = 1;
            }
            int short_flagVal = Pps.slice_group_change_direction_flag == true ? 1 : 0;
            int x = (globalVariables.PicWidthInMbs - short_flagVal) / 2; 
            int y = (globalVariables.PicHeightInMapUnits - short_flagVal) / 2;

            var (leftBound, topBound) = (x, y);
            var (rightBound, bottomBound) = (x, y);
            var (xDir, yDir) = (short_flagVal - 1, short_flagVal);
            for (int k = 0; k < globalVariables.MapUnitsInSliceGroup0; k++)
            {
                bool mapUnitVacant = (mapUnitToSliceGroupMap[y * globalVariables.PicWidthInMbs + x] == 1);
                if (mapUnitVacant)
                {
                    mapUnitToSliceGroupMap[y * globalVariables.PicWidthInMbs + x] = 0;
                    if (xDir == -1 && x == leftBound)
                    {
                        leftBound = Math.Max(leftBound - 1, 0);
                        x = leftBound;
                        (xDir, yDir) = (0, 2 *  - short_flagVal - 1);
                    } else if (xDir == 1 && x == rightBound)
                    {
                        rightBound = Math.Min(rightBound + 1, globalVariables.PicWidthInMbs - 1);
                        x = rightBound;
                        (xDir, yDir) = (0, 1 - 2 * short_flagVal);
                    } else if (yDir == -1 && y == topBound)
                    {
                        topBound = Math.Max(topBound - 1, 0);
                        y = topBound;
                        (xDir, yDir) = (1 - 2 * short_flagVal, 0);
                    } else if (yDir == 1 && y == bottomBound)
                    {
                        bottomBound = Math.Min(bottomBound + 1, globalVariables.PicHeightInMapUnits - 1);
                        y = bottomBound;
                        (xDir, yDir) = (2 * short_flagVal - 1, 0);
                    } else
                    {
                        (x, y) = (x + xDir, y + yDir);
                    }
                }
            }
            return mapUnitToSliceGroupMap;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public int[] GetMapUnitsToSliceGroup4(PPS Pps, SliceHeader sliceHeader)
    {
        try
        {
            int[] mapUnitToSliceGroupMap = new int[Pps.NalUnitLength];
            GlobalVariables globalVariables = new GlobalVariables();
            int sizeOfUpperLeftGroup = 0;
            if (Pps.num_slice_groups_minus1 == 1 && (Pps.slice_group_map_type == 4 || Pps.slice_group_map_type == 5))
            {
                sizeOfUpperLeftGroup = Pps.slice_group_change_direction_flag ? globalVariables.PicSizeInMapUnits - globalVariables.MapUnitsInSliceGroup0 
                                        : globalVariables.MapUnitsInSliceGroup0;
            }
            
            if (Pps.num_slice_groups_minus1 == 0)
            {
                for (int i = 0; i <= globalVariables.PicSizeInMapUnits - 1; i++)
                {
                    mapUnitToSliceGroupMap[i] = 0;
                }
            }
            return mapUnitToSliceGroupMap;
        }
        catch (System.Exception)
        {            
            throw;
        }
    }

    public void SetCoeffLevelType(CAVLCSettings cAVLCSettings)
    {
        try
        {
            using (StreamWriter streamWriter = new StreamWriter(@"C:\H264Decoder\h264Service\Data\currmbsettings.json"))
            {
                string calvlcSettingsjson = JsonSerializer.Serialize<CAVLCSettings>(cAVLCSettings); 
                streamWriter.WriteLine(calvlcSettingsjson);
            }
        }
        catch (System.Exception)
        {            
            throw;
        }
    }
}