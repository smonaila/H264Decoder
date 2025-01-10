namespace Decoder.Array;

public static class h264Array
{
    public static int[,] Copy2DArray(int[,] dest, int destIndex, int[] source, int sourcIndex, int Length)
    {
        for (int coeffientIndex = 0; coeffientIndex < Length; coeffientIndex++)
        {
            dest[destIndex, coeffientIndex] = source[sourcIndex++];
        }
        return dest;
    }

    public static int[,,] Copy3DArray(int[,,] dest, int iCbCr, int col, int[] source, int sourceIndex, int Length)
    {
        for (int coeffientIndex = 0; coeffientIndex < source.Length; coeffientIndex++)
        {
            dest[iCbCr, col, coeffientIndex] = source[coeffientIndex];
        }
        return dest;
    }
}