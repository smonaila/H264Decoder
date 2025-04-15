


namespace MathExtensionMethods
{
    public static class Mathematics
    {
        public static int Clip1Y(int x, int BitDepthY)
        {
           try
           {
                x = Mathematics.Clip3(0, 1 << BitDepthY - 1, x);
                return x;
           }
           catch (System.Exception)
           {            
                throw;
           }
        }

        public static int Clip3(int x, int y, int z)
        {
            if (z < x)
            {
                return x;
            } else if (z > y)
            {
                return y;
            } else
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