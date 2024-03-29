using System;
using System.Collections.Generic;
using System.Linq;

using CustomerAssistant.SettingsActivity.Configs;
using CustomerAssistant.SettingsActivity.Filters;
using CustomerAssistant.SettingsActivity.Models;
using CustomerAssistant.SettingsActivity.Views;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace CustomerAssistant.SettingsActivity.Controllers
{
    public class ActivityController : IDisposable
    {
        private readonly BackButtonView _backBackButtonView;

        private readonly FiltersContainerView _filtersContainerView;

        private readonly FilterConfig _filterConfig;

        private FiltersModel _filtersModel;

        private RectTransform _containerTransform;

        private List<ViewFilterTransformModel> _filtersData = new List<ViewFilterTransformModel>(8);

        public ActivityController(BackButtonView backBackButtonView, FiltersContainerView filtersContainerView,
            FilterConfig filterConfig)
        {
            _backBackButtonView = backBackButtonView;
            _filtersContainerView = filtersContainerView;
            _filterConfig = filterConfig;

            Initialize();
        }

        void IDisposable.Dispose()
        {
            _backBackButtonView.ButtonClicked -= HandleBackBackButtonClick;
            _filtersContainerView.Dropdown.onValueChanged.RemoveListener(HandleFilterAddition);
        }

        private void Initialize()
        {
            #region filters settings

            _filtersModel = new FiltersModel(_filterConfig.GetFilters());

            _filtersContainerView.Dropdown.options.Clear();

            var inactiveFilters = _filtersModel.GetInactiveFilters();

            AddDropOption("None"); // note: add first "none" option to fix selection bug

            foreach (var item in inactiveFilters)
            {
                AddDropOption(item.Name);
            }

            var filters = _filtersModel.GetFilters();

            _containerTransform = _filtersContainerView.FiltersContainer.GetComponent<RectTransform>();

            Assert.IsNotNull(_containerTransform,
                "FiltersContainerView -> FiltersContainer does not contain RectTransform");

            foreach (var item in filters)
            {
                FilterView filterView = null;

                switch (item.GetType().Name)
                {
                    case "BoolFilter":
                        var boolFilter = item as BoolFilter;

                        Assert.IsNotNull(boolFilter);

                        var boolFilterView = Object.Instantiate(boolFilter.BoolFilterPrefab,
                            _filtersContainerView.FiltersContainer);

                        SetText(boolFilterView, boolFilter.Name);
                        filterView = boolFilterView;

                        boolFilterView.Toggle.onValueChanged.AddListener(x => ChangeFilterValue(item as BoolFilter, x));
                        boolFilterView.Toggle.SetIsOnWithoutNotify(boolFilter.Value);

                        break;

                    case "StringFilter":
                        var stringFilter = (item as StringFilter);

                        Assert.IsNotNull(stringFilter);

                        var stringFilterView = Object.Instantiate(stringFilter.StringFilterPrefab,
                            _filtersContainerView.FiltersContainer);

                        SetText(stringFilterView, stringFilter.Name);
                        filterView = stringFilterView;

                        stringFilterView.InputField.onValueChanged.AddListener(x =>
                            ChangeFilterValue(item as StringFilter, x));
                        stringFilterView.InputField.text = stringFilter.Value;

                        break;

                    case "ToggleFilter":
                        var toggleFilter = (item as ToggleFilter);

                        Assert.IsNotNull(toggleFilter);

                        var toggleFilterView = Object.Instantiate(toggleFilter.ToggleFilterPrefab,
                            _filtersContainerView.FiltersContainer);

                        SetText(toggleFilterView, toggleFilter.Name);
                        filterView = toggleFilterView;

                        foreach (var model in toggleFilterView.ToggleModels)
                        {
                            model.Toggle.onValueChanged.AddListener(x =>
                                ChangeFilterValue(item as ToggleFilter, new StringBoolModel(model.Text.text, x)));

                            var t = toggleFilter.Value.Find(x => x.Text == model.Text.text);
                            model.Toggle.SetIsOnWithoutNotify(t.Value);
                        }

                        break;

                    case "IntRangeFilter":
                        var intRangeFilter = (item as IntRangeFilter);

                        Assert.IsNotNull(intRangeFilter);

                        var intRangeFilterView =
                            Object.Instantiate(intRangeFilter.IntRangeFilterPrefab,
                                _filtersContainerView.FiltersContainer);

                        SetText(intRangeFilterView, intRangeFilter.Name);
                        filterView = intRangeFilterView;

                        var minInputField = intRangeFilterView.MinInputField;
                        var maxInputField = intRangeFilterView.MaxInputField;

                        intRangeFilterView.MinInputField.text = intRangeFilter.Value.x.ToString();
                        intRangeFilterView.MaxInputField.text = intRangeFilter.Value.y.ToString();

                        minInputField.onValueChanged.AddListener(x =>
                            ChangeFilterValue(item as IntRangeFilter,
                                new Vector2Int(ParseToInt(x), intRangeFilter.Value.y)));

                        maxInputField.onValueChanged.AddListener(y =>
                            ChangeFilterValue(item as IntRangeFilter,
                                new Vector2Int(intRangeFilter.Value.x, ParseToInt(y))));

                        break;
                }

                Assert.IsNotNull(filterView, "filterView == null");

                filterView.RemoveBackButton.ButtonClicked += () => HandleFilterRemoving(filterView);

                var viewTransform = filterView.GetComponent<RectTransform>();

                Assert.IsNotNull(viewTransform, "viewTransform == null");

                _filtersData.Add(new ViewFilterTransformModel(filterView, item, viewTransform));

                if (item.Active)
                {
                    viewTransform.anchoredPosition -= new Vector2(0, _containerTransform.rect.height);

                    ChangeContainerHeight(viewTransform.sizeDelta.y, true);
                }
                else
                {
                    filterView.gameObject.SetActive(false);
                }
            }

            #endregion

            _backBackButtonView.ButtonClicked += HandleBackBackButtonClick;
            _filtersContainerView.Dropdown.onValueChanged.AddListener(HandleFilterAddition);
        }

        private int ParseToInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        private void HandleFilterAddition(int value)
        {
            if (value == 0) // note: if "None"
            {
                return;
            }

            var chosenFilter = _filtersContainerView.Dropdown.options[value];

            RemoveDropOption(chosenFilter);

            _filtersContainerView.Dropdown.SetValueWithoutNotify(0); // note: select "None" option

            var item = _filtersData.First(x => x.Filter.Name == chosenFilter.text);

            var transform = item.RectTransform;

            transform.anchoredPosition = new Vector2(0, -_containerTransform.rect.height);

            item.Filter.ChangeActiveState(true);
            item.View.gameObject.SetActive(true);

            ChangeContainerHeight(transform.sizeDelta.y, true);
        }

        private void HandleFilterRemoving(FilterView view)
        {
            ClearContainerHeight();

            foreach (var item in _filtersData)
            {
                if (item.View == view)
                {
                    item.Filter.ChangeActiveState(false);
                    item.View.gameObject.SetActive(false);

                    var viewName = item.Filter.Name;

                    AddDropOption(viewName);

                    continue;
                }

                if (item.Filter.Active)
                {
                    var viewHeight = item.RectTransform.sizeDelta.y;

                    ChangeContainerHeight(viewHeight, true);

                    item.RectTransform.anchoredPosition = new Vector2(0, viewHeight - _containerTransform.sizeDelta.y);
                }
            }
        }

        private void SetText(FilterView view, string text)
        {
            view.TextField.text = text;
        }

        private void ChangeFilterValue<T, T1>(T filter, T1 value) where T : ITypedFilter<T1>
        {
            filter.ChangeValue(value);
        }

        private void HandleBackBackButtonClick()
        {
            ChangeSceneAsync().Forget();
        }

        private async UniTaskVoid ChangeSceneAsync()
        {
            await SceneManager.LoadSceneAsync("Scenes/Dummy_demo");
        }

        private void ChangeContainerHeight(float height, bool increase)
        {
            var y = increase ? height : -height;

            _containerTransform.sizeDelta += new Vector2(0, y);
        }

        private void ClearContainerHeight()
        {
            _containerTransform.sizeDelta = Vector2.zero;
        }

        private void AddDropOption(string name)
        {
            _filtersContainerView.Dropdown.options.Add(new Dropdown.OptionData(name));
        }

        private void RemoveDropOption(Dropdown.OptionData option)
        {
            _filtersContainerView.Dropdown.options.Remove(option);
        }
    }
}