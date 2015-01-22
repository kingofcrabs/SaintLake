﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SaintX.Data
{
    class GlobalVars
    {
        private static GlobalVars _instance;
        private int _sampleCnt = 16;

        public GlobalVars()
        {
            SampleLayoutInfos = new SampleLayoutInfos();
        }
        
        static public GlobalVars Instance
        {
            get
            {
                if(_instance == null )
                {
                    _instance = new GlobalVars();
                }
                return _instance;
            }
        }

        public int SampleCount 
        { 
            get 
            { 
                return _sampleCnt;
            }
            set 
            { 
                _sampleCnt = value;
            }
        }

        public SampleLayoutInfos SampleLayoutInfos { get; set; }

    }
    
}
