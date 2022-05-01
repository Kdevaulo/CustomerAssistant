using System;
using System.Collections.Generic;
using System.Linq;

using SettingsActivity.Models;
using SettingsActivity.Views;

using UnityEngine;

namespace SettingsActivity.Filters
{
    [Serializable]
    public class ToggleFilter : ITypedFilter<StringBoolModel>
    {
        [SerializeField] private string _name;

        [SerializeField] private bool _active;

        [SerializeField] private List<StringBoolModel> _value = new List<StringBoolModel>();

        [SerializeField] private ToggleFilterView _toggleFilterPrefab;

        void IFilter.ChangeActiveState(bool state)
        {
            _active = state;
        }

        void ITypedFilter<StringBoolModel>.ChangeValue(StringBoolModel item)
        {
            var model = _value.First(x => x.Text == item.Text);

            model.SetValue(item.Value);
        }

        public bool Active => _active;

        public string Name => _name;

        public List<StringBoolModel> Value => _value;

        public ToggleFilterView ToggleFilterPrefab => _toggleFilterPrefab;
    }
}