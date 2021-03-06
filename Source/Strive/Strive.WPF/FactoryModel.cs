﻿using System.Collections.Generic;
using UpdateControls;

namespace Strive.WPF
{
    public class FactoryModel
    {
        private float _x;
        private float _y;
        private float _z;
        private int _producing;
        private int _progress;
        List<int> _queued;

        #region Independent properties
        // Generated by Update Controls --------------------------------
        private Independent _indX = new Independent();
        private Independent _indY = new Independent();
        private Independent _indZ = new Independent();
        private Independent _indProducing = new Independent();
        private Independent _indProgress = new Independent();
        private Independent _indQueued = new Independent();

        public float X
        {
            get { _indX.OnGet(); return _x; }
            set { _indX.OnSet(); _x = value; }
        }

        public float Y
        {
            get { _indY.OnGet(); return _y; }
            set { _indY.OnSet(); _y = value; }
        }

        public float Z
        {
            get { _indZ.OnGet(); return _z; }
            set { _indZ.OnSet(); _z = value; }
        }

        public int Producing
        {
            get { _indProducing.OnGet(); return _producing; }
            set { _indProducing.OnSet(); _producing = value; }
        }

        public int Progress
        {
            get { _indProgress.OnGet(); return _progress; }
            set { _indProgress.OnSet(); _progress = value; }
        }

        public int NewQueued()
        {
            _indQueued.OnSet();
            int queued = new int();
            _queued.Add(queued);
            return queued;
        }

        public void DeleteQueued(int queued)
        {
            _indQueued.OnSet();
            _queued.Remove(queued);
        }

        public IEnumerable<int> Queued
        {
            get { _indQueued.OnGet(); return _queued; }
        }
        // End generated code --------------------------------
        #endregion
    }
}
