
using System.Drawing;
using H264.Global.Variables;
using H264.Types;
using MathExtensionMethods;

namespace MbAddressLocations;

public class MbAddressComputation
{
    // Clause 6.4.3
    public Point Get4x4LumaLocation(int luma4x4BlkIdx)
    {
        try
        {
            int x = Mathematics.InverseRasterScan(luma4x4BlkIdx / 4, 8, 8, 16, 0) +
            Mathematics.InverseRasterScan(luma4x4BlkIdx % 4, 4, 4, 8, 0);
            int y = Mathematics.InverseRasterScan(luma4x4BlkIdx / 4, 8, 8, 16, 1) +
            Mathematics.InverseRasterScan(luma4x4BlkIdx % 4, 4, 4, 8, 1);

            return new Point(x, y);
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    // Clause 6.4.11.4 Derivation process for neighbouring 4x4 luma blocks.
    public Neighbouring4x4LumaBlocks GetNeighbouring4x4LumaBlk(int Luma4x4BlkIdx)
    {
        MbAddress? mbAddressA, mbAddressB;
        BlockIdx luma4x4blkIdxA, luma4x4blkIdxB;
        NeighbouringMbAndAvailability neighbouringMbAndAvailability = GetNeighbouringMbAndAvailability();
        neighbouringMbAndAvailability = neighbouringMbAndAvailability != null ? neighbouringMbAndAvailability : new NeighbouringMbAndAvailability();
        
        mbAddressA = neighbouringMbAndAvailability.MbAddressA;
        mbAddressA = mbAddressA != null ? mbAddressA : new MbAddress();        
        mbAddressA.GetDiffLumaLocation = new Point(-1, 0);       

        mbAddressB = neighbouringMbAndAvailability.MbAddressB;
        mbAddressB = mbAddressB != null ? mbAddressB : new MbAddress();
        mbAddressB.GetDiffLumaLocation = new Point(0, -1);

        var UpperLeftLumaLocation = Get4x4LumaLocation(Luma4x4BlkIdx);
        var LumaLocation = new Point(UpperLeftLumaLocation.X + mbAddressA.GetDiffLumaLocation.X,
        UpperLeftLumaLocation.Y + mbAddressA.GetDiffLumaLocation.Y);

        NeighbouringLocation neighbouringLocation = GetNeighbouringLocation(LumaLocation, IsLuma: true);
        
        luma4x4blkIdxA = new BlockIdx();
        luma4x4blkIdxA.Available = mbAddressA.Available == true ? true : false;
        luma4x4blkIdxA.Idx = luma4x4blkIdxA.Available ? Get4x4LumaBlockIndices(neighbouringLocation.Location) : -1;
        
        LumaLocation = new Point(UpperLeftLumaLocation.X + mbAddressB.GetDiffLumaLocation.X,
        UpperLeftLumaLocation.Y + mbAddressB.GetDiffLumaLocation.Y);

        neighbouringLocation = GetNeighbouringLocation(LumaLocation, true);

        luma4x4blkIdxB = new BlockIdx();
        luma4x4blkIdxB.Available = mbAddressB.Available == true ? true : false;
        luma4x4blkIdxB.Idx = luma4x4blkIdxB.Available ? Get4x4LumaBlockIndices(neighbouringLocation.Location) : -1;

        Neighbouring4x4LumaBlocks neighbouring4X4LumaBlocks = new Neighbouring4x4LumaBlocks();
        neighbouring4X4LumaBlocks.Luma4x4BlkIdxA = luma4x4blkIdxA;
        neighbouring4X4LumaBlocks.MbAddressA = mbAddressA;
        neighbouring4X4LumaBlocks.Luma4x4BlkIdxB = luma4x4blkIdxB;
        neighbouring4X4LumaBlocks.MbAddressB = mbAddressB;

        return neighbouring4X4LumaBlocks;
    }

    private int Get4x4LumaBlockIndices(Point location)
    {
        try
        {
            int luma4x4BlkIdx = 8 * (location.Y / 8) + 4 * (location.X) + 2 * ((location.Y % 8) / 4) + ((location.X % 8) / 4);
            return luma4x4BlkIdx;
        }
        catch (System.Exception)
        {
            throw;
        }
    }


    // Clause 6.4.9 and 6.4.10 Derivation process for neighbouring macroblock addresses and their availability.
    public NeighbouringMbAndAvailability GetNeighbouringMbAndAvailability()
    {
        try
        {
            CodecSettings codecSettings = new CodecSettings();
            SettingSets settingSets = codecSettings.GetCodecSettings();

            GlobalVariables globalVariables = settingSets.GlobalVariables;
            NeighbouringMbAndAvailability neighbouringLocationAndAvailibility = new NeighbouringMbAndAvailability();
            MbAddress MbAddressA, MbAddressB, MbAddressC, MbAddressD;
            MbAddress CurrMbAddr = new MbAddress();
            CurrMbAddr.Address = globalVariables.CurrMbAddr;

            MbAddressA = new MbAddress();
            MbAddressB = new MbAddress();
            MbAddressC = new MbAddress();
            MbAddressD = new MbAddress();

            if (globalVariables.MbaffFrameFlag == 0)
            {
                MbAddressA.Address = globalVariables.CurrMbAddr  - 1;
                MbAddressB.Address = globalVariables.CurrMbAddr - globalVariables.PicWidthInMbs;
                MbAddressC.Address = globalVariables.CurrMbAddr - globalVariables.PicHeightInMbs + 1;
                MbAddressD.Address = globalVariables.CurrMbAddr - globalVariables.PicWidthInMbs - 1;

                MbAddressA = MbAddrAvailable(MbAddressA, CurrMbAddr);
                MbAddressB = MbAddrAvailable(MbAddressB, CurrMbAddr);
                MbAddressC = MbAddrAvailable(MbAddressC, CurrMbAddr);
                MbAddressD = MbAddrAvailable(MbAddressD, CurrMbAddr);

                if (CurrMbAddr.Address % globalVariables.PicWidthInMbs == 0)
                {
                    MbAddressA.Available = false;
                }
                if (CurrMbAddr.Address % globalVariables.PicWidthInMbs == 0)
                {
                    MbAddressC.Available = false;
                }
                if (CurrMbAddr.Address % globalVariables.PicWidthInMbs == 0)
                {
                    MbAddressD.Available = false;
                }
            }
            else if (globalVariables.MbaffFrameFlag == 1)
            {

                MbAddressA.Address = 2 * ((globalVariables.CurrMbAddr / 2) - 1);
                MbAddressB.Address = 2 * ((globalVariables.CurrMbAddr / 2) - globalVariables.PicWidthInMbs);
                MbAddressC.Address = 2 * ((globalVariables.CurrMbAddr / 2) - globalVariables.PicHeightInMbs + 1);
                MbAddressD.Address = 2 * ((globalVariables.CurrMbAddr / 2) - globalVariables.PicWidthInMbs - 1);

                MbAddressA = MbAddrAvailable(MbAddressA, CurrMbAddr);
                MbAddressB = MbAddrAvailable(MbAddressB, CurrMbAddr);
                MbAddressC = MbAddrAvailable(MbAddressC, CurrMbAddr);
                MbAddressD = MbAddrAvailable(MbAddressD, CurrMbAddr);

                if ((CurrMbAddr.Address / 2) % globalVariables.PicWidthInMbs == 0)
                {
                    MbAddressA.Available = false;
                }
                if (((CurrMbAddr.Address / 2) + 1) % globalVariables.PicWidthInMbs == 0)
                {
                    MbAddressC.Available = false;
                }
                if ((CurrMbAddr.Address / 2) % globalVariables.PicWidthInMbs == 0)
                {
                    MbAddressD.Available = false;
                }
            }
            neighbouringLocationAndAvailibility.MbAddressA = MbAddressA;
            neighbouringLocationAndAvailibility.MbAddressB = MbAddressB;
            neighbouringLocationAndAvailibility.MbAddressC = MbAddressC;
            neighbouringLocationAndAvailibility.MbAddressD = MbAddressD;

            return neighbouringLocationAndAvailibility;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public NeighbouringLocation GetNeighbouringLocation(Point Location, bool IsLuma)
    {
        try
        {
            GlobalVariables globalVariables = new GlobalVariables();
            NeighbouringLocation neighbouringLocation = new NeighbouringLocation();
            int maxW, maxH;

            if (IsLuma)
            {
                maxW = maxH = 16;
            }
            else
            {
                maxW = globalVariables.MbWidthC;
                maxH = globalVariables.MbHeightC;
            }
            if (Location.X < 0 && Location.Y < 0)
            {
                neighbouringLocation.MbAddress = MbAddressNeighbour.MbAddressD;
            } else if (Location.X < 0 && (Location.Y >= 0 && Location.Y <= maxH - 1))
            {
                neighbouringLocation.MbAddress = MbAddressNeighbour.MbAddressA;
            } else if((Location.X >= 0 && Location.X <= maxW - 1) && (Location.Y < 0))
            {
                neighbouringLocation.MbAddress = MbAddressNeighbour.MbAddressB;
            } else if ((Location.X >= 0 && Location.X <= maxW - 1) && (Location.Y >= 0 && Location.Y <= maxH - 1))
            {
                neighbouringLocation.MbAddress = MbAddressNeighbour.CurrAddr;
            } else if ((Location.X > maxW - 1) && (Location.Y < 0))
            {
                neighbouringLocation.MbAddress = MbAddressNeighbour.MbAddressC;
            }
            neighbouringLocation.Location = new Point((Location.X + maxW) % maxW, (Location.Y + maxH) % maxH);

            return neighbouringLocation;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public MbAddress MbAddrAvailable(MbAddress mbAddress, MbAddress CurrMbAddr)
    {
        try
        {
            GlobalVariables globalVariables = new GlobalVariables();
            if (mbAddress.Address < 0)
            {
                mbAddress.Available = false;
            }
            else if (mbAddress.Address > CurrMbAddr.Address)
            {
                mbAddress.Available = false;
            }
            return mbAddress;
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public Point Get8x8LumaLocation(int luma4x4BlkIdx)
    {
        try
        {
            int x = Mathematics.InverseRasterScan(luma4x4BlkIdx, 8, 8, 16, 0);
            int y = Mathematics.InverseRasterScan(luma4x4BlkIdx, 8, 8, 16, 1);

            return new Point(x, y);
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}