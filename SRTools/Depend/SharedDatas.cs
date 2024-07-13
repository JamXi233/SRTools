using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    class SharedDatas
    {
        public static class Gacha
        {
            private static double _fiveStarPity = 0;
            public static double FiveStarPity
            {
                get
                {
                    return _fiveStarPity;
                }
                set
                {
                    _fiveStarPity = value;
                }
            }
        }

        public static class UpdateSRGF
        {
            private static string _updateSRGFFilePath = null;
            public static string UpdateSRGFFilePath
            {
                get
                {
                    return _updateSRGFFilePath;
                }
                set
                {
                    _updateSRGFFilePath = value;
                }
            }

            private static string _updateSRGFUID = null;
            public static string UpdateSRGFUID
            {
                get
                {
                    return _updateSRGFUID;
                }
                set
                {
                    _updateSRGFUID = value;
                }
            }
        }
        

        public static class ScreenShotData
        {
            private static string _screenShotPath = null;
            public static string ScreenShotPath
            {
                get
                {
                    return _screenShotPath;
                }
                set
                {
                    _screenShotPath = value;
                }
            }
        }
    }
}
