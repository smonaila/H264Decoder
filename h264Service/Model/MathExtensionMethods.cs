

namespace MathExtensionMethods
{
    public static class Mathematics
    {
        public static int Clip3(int x, int y, int z)
        {
            if (z < x)
            {
                return x;
            }else if (z > y)
            {
                return y;
            }else
            {
                return z;
            }
        }
    }
}