using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    class SharedData
    {
        private static string _savedUsers = null;
        public static string SavedUsers
        {
            get
            {
                return _savedUsers;
            }
            set
            {
                _savedUsers = value;
            }
        }

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
}
