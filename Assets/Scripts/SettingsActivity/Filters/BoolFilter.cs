using System;

using SettingsActivity.Views;

using UnityEngine;

namespace SettingsActivity.Filters
{
    [Serializable]
    public class BoolFilter : IFilter
    {
        [SerializeField] private string _name;

        [SerializeField] private bool _active;

        [SerializeField] private bool _value;

        [SerializeField] private BoolFilterView _boolFilterPrefab;

        public bool Active => _active;

        public string Name => _name;

        public bool Value => _value;

        public BoolFilterView BoolFilterPrefab => _boolFilterPrefab;

        public void ChangeActiveState(bool state)
        {
            _active = state;
        }
    }
}