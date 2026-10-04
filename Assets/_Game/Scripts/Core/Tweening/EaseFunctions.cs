using System;

namespace ShaurmaTycoon.Core.Tweening
{
    public static class EaseFunctions
    {
        private const double BackOvershoot = 1.70158;

        /// <summary>Значение кривой для t в [0, 1]; на концах — ровно 0 и 1.</summary>
        public static float Evaluate(Ease ease, float t)
        {
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;

            switch (ease)
            {
                case Ease.InQuad:
                    return t * t;
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.InOutSine:
                    return (float)(0.5 - 0.5 * Math.Cos(Math.PI * t));
                case Ease.OutBack:
                    double u = t - 1.0;
                    return (float)(1.0 + (BackOvershoot + 1.0) * u * u * u + BackOvershoot * u * u);
                default:
                    return t;
            }
        }
    }
}
