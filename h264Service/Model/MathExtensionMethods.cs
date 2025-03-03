

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

        public static int InverseRasterScan(float a, int b, int c, int d, int e)
        {
            if (e == 0)
            {
                return (int)((a % (d / b)) * b);
            }
            else if (e == 1)
            {
                return (int)((a / (d / b)) * c);
            }
            return -1;
        }

        public static (int, int) GetLocation(int x, int y)
        {
            return (x, y);
        }
    }
}