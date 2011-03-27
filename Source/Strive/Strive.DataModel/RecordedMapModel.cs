﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using UpdateControls;

namespace Strive.DataModel
{
    /// <summary>
    /// Objects stored in this Map need to be immutable to ensure they are not modified externally,
    /// so that all changes are recorded in the history.
    /// History is stored in an ordered map of version to state,
    /// where the state is a map of keys to values.
    /// </summary>
    public class RecordedMapModel<TKeyType, TValueType> : IEnumerable<TValueType>
    {
        private FSharpMap<int, FSharpMap<TKeyType, TValueType>> _history
            = new FSharpMap<int, FSharpMap<TKeyType, TValueType>>(
                Enumerable.Empty<Tuple<int, FSharpMap<TKeyType, TValueType>>>());
        private FSharpMap<TKeyType, TValueType> _currentMap
            = new FSharpMap<TKeyType, TValueType>(Enumerable.Empty<Tuple<TKeyType, TValueType>>());
        private int _currentVersion;
        private int _maxVersion;

        public int CurrentVersion
        {
            get { _indHistory.OnGet(); return _currentVersion; }
            set { _indHistory.OnSet(); _currentVersion = value; _currentMap = _history[value]; }
        }
        public int MaxVersion { get { _indHistory.OnGet(); return _maxVersion; } }

        public RecordedMapModel()
        {
            _currentVersion = -1;
            Clear();
        }

        public FSharpMap<TKeyType, TValueType> Map
        {
            get
            {
                _indHistory.OnGet();
                return _currentMap;
            }
            set
            {
                _indHistory.OnSet();
                _currentMap = value;
                _history = _history.Add(++_currentVersion, value);
                _maxVersion = Math.Max(_maxVersion, _currentVersion);
            }
        }

        public RecordedMapModel(IEnumerable<KeyValuePair<TKeyType, TValueType>> keyValuePairs)
            : this()
        {
            Contract.Requires<ArgumentNullException>(keyValuePairs != null);

            foreach (var e in keyValuePairs)
                Map = Map.Add(e.Key, e.Value);
        }

        #region Independent properties
        // Generated by Update Controls --------------------------------
        private readonly Independent _indHistory = new Independent();

        public void Set(TKeyType key, TValueType value)
        {
            _indHistory.OnSet();
            Map = Map.Add(key, value);
        }

        public void Remove(TKeyType key)
        {
            _indHistory.OnSet();
            Map = Map.Remove(key);
        }

        public TValueType Get(TKeyType id)
        {
            _indHistory.OnGet();
            var option = Map.TryFind(id);
            return option == FSharpOption<TValueType>.None ? default(TValueType) : option.Value;
        }

        public IEnumerable<TValueType> Values
        {
            get { _indHistory.OnGet(); return Map.Select(x => x.Value); }
        }

        public int Count
        {
            get { _indHistory.OnGet(); return Map.Count; }
        }

        public void Clear()
        {
            _indHistory.OnSet();
            Map = new FSharpMap<TKeyType, TValueType>(Enumerable.Empty<Tuple<TKeyType, TValueType>>());
        }

        public bool ContainsKey(TKeyType key)
        {
            _indHistory.OnGet();
            return Map.ContainsKey(key);
        }

        // End generated code --------------------------------
        #endregion

        #region IEnumerable<TValueType> Members

        public IEnumerator<TValueType> GetEnumerator()
        {
            return Map.Select(x => x.Value).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            // TODO: make an extension Values method for FSharpMap?
            return Map.Select(x => x.Value).GetEnumerator();
        }

        #endregion
    }
}