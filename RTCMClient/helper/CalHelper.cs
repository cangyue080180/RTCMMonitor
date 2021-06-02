using System;

namespace RTCMClient.helper
{
    public class CalHelper
    {
        private const double Mae = 6378065.301;
        private const double Tee = (1.124133935 * 0.00001);
        private const double Ta = 6378137.0;

        //h 平均纬度,hx 纬度差
        public static double CalY(double h, double hx)
        {
            double x, y;
            float s;
            s = (float)Math.Sin(h);
            x = s * s;
            x = 1.0 - Tee * x;
            y = Math.Pow(x, 0.5);
            y = Mae / y;
            y = y / x;
            y = y * hx;
            return y;
        }

        //B 纬度,Bx 经度差
        public static double CalX(double B, double Bx)
        {
            float c;
            double x, y;
            c = (float)Math.Sin(B);
            c = c * c;
            x = Tee * c;
            x = 1 - x;
            x = Math.Pow(x, 0.5);
            c = (float)Math.Cos(B);
            y = c;
            y = y * 6378137.0;
            y = y / x;
            y = y * Bx;
            return y;
        }
    }
}