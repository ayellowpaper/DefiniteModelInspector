using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor.UIElements;

namespace ZeludeEditor
{
    public class AnimationExplorer : VisualElement
    {
        public const int ItemHeight = 16;

        public UnityEngine.Object Asset {
            get => _asset;
            set {
                if (_asset == value) return;
                _asset = value;
                UpdateCategoriesState();
                UpdateList();
            }
        }

        public bool OpenAssetsOnDoubleClick {
            get => _openAssetsOnDoubleClick;
            set {
                if (_openAssetsOnDoubleClick == value) return;
                _openAssetsOnDoubleClick = value;
                UpdateDoubleClickBinding();
            }
        }

        public bool HasContextClick { get; set; } = true;

        public ListView ListView { get; private set; }

        private List<AnimationClipInfo> _finalClips;
        private IEnumerable<AnimationClipInfo> _prefilteredList;
        private Texture _animationIcon;
        private TextElement _searchCountLabel;
        private VisualElement _noAnimationsFoundElement;
        private string _currentSearchString = null;
        private Toggle _activeToggle;
        private List<FilterCategory> _filterCategories;
        private UnityEngine.Object _asset;
        private Dictionary<VisualElement, AssetDragAndDropManipulator> _dragAndDropManipulatorLookup = new Dictionary<VisualElement, AssetDragAndDropManipulator>();
        private bool _openAssetsOnDoubleClick = true;

        public AnimationExplorer(UnityEngine.Object asset) : this()
        {
            Asset = asset;
        }

        public AnimationExplorer() : base()
        {
            _animationIcon = EditorGUIUtility.Load("d_AnimationClip Icon") as Texture;

            string path = "Packages/com.zelude.meshpreview/Assets/UXML/AnimationExplorer.uxml";
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            var uxml = template.Instantiate();
            uxml.style.flexGrow = 1;
            this.style.flexGrow = 1;

            _searchCountLabel = uxml.Q<TextElement>("search-count");
            _noAnimationsFoundElement = uxml.Q("no-animations-found");
            _noAnimationsFoundElement.style.display = DisplayStyle.None;
            var humanFilter = new FilterCategory(uxml.Q<Toggle>("toggle-human"), () => Asset == null ^ (AnimationDatabase.GetAvatarFromAsset(Asset) is Avatar avatar && avatar.isHuman), AnimationDatabase.GetHumanClips);
            var avatarFilter = new FilterCategory(uxml.Q<Toggle>("toggle-avatar"), () => AnimationDatabase.GetAvatarFromAsset(Asset) != null, () => AnimationDatabase.GetClipsForAvatar(AnimationDatabase.GetAvatarFromAsset(Asset)));
            var assetFilter = new FilterCategory(uxml.Q<Toggle>("toggle-asset"), () => Asset != null && GetClipsInAsset().Count() > 0, GetClipsInAsset);
            var allFilter = new FilterCategory(uxml.Q<Toggle>("toggle-all"), () => true, AnimationDatabase.GetAllClips);

            SetCategoryImage(humanFilter.Toggle, "HumanTemplate Icon");
            SetCategoryImage(avatarFilter.Toggle, "d_AvatarSelector");
            SetCategoryImage(assetFilter.Toggle, "d_GameObject Icon");
            SetCategoryImage(allFilter.Toggle, "d_AnimationClip Icon");

            _filterCategories = new List<FilterCategory>() { humanFilter, avatarFilter, assetFilter, allFilter };

            foreach (var category in _filterCategories)
            {
                category.Toggle.RegisterValueChangedCallback(evt => HandleCategoryToggleChanged(evt, category));
            }

            SetFirstPossibleToggleActive();

            ListView = new ListView(_finalClips, ItemHeight, MakeItem, BindItem);
            ListView.unbindItem += UnbindItem;
            UpdateDoubleClickBinding();
            ListView.selectionType = SelectionType.Single;
            uxml.Q("content").Add(ListView);
            uxml.Q<ToolbarSearchField>().RegisterValueChangedCallback(HandleToolbarSearchChanged);
            Add(uxml);

            UpdateCategoriesState();
            UpdateList();
        }

        private void UpdateDoubleClickBinding()
        {
            if (_openAssetsOnDoubleClick == true)
            {
                ListView.onItemsChosen -= OpenAssets;
                ListView.onItemsChosen += OpenAssets;
            }
            else
                ListView.onItemsChosen -= OpenAssets;
        }

        private void OpenAssets(IEnumerable<object> objects)
        {
            foreach (var obj in objects)
            {
                var info = (AnimationClipInfo) obj;
                AssetDatabase.OpenAsset(info.AnimationClip.instanceID);
            }
        }

        private void UpdateCategoriesState()
        {
            foreach (var category in _filterCategories)
            {
                category.Toggle.SetEnabled(category.ValidationFunc());
            }

            if (!_activeToggle.enabledSelf)
                SetFirstPossibleToggleActive();

            var avatar = AnimationDatabase.GetAvatarFromAsset(Asset);
            this.Q<Toggle>("toggle-avatar").text = avatar != null ? avatar.name : "Avatar";
        }
        
        private void SetFirstPossibleToggleActive()
        {
            foreach (var category in _filterCategories)
            {
                if (category.Toggle.enabledSelf)
                {
                    SetCategoryActive(category);
                    break;
                }
            }
        }

        private void SetCategoryActive(FilterCategory category)
        {
            if (_activeToggle != null) _activeToggle.SetValueWithoutNotify(false);
            category.Toggle.SetValueWithoutNotify(true);
            _prefilteredList = category.GetClipsFunc();
            _activeToggle = category.Toggle;
        }

        private void SetCategoryImage(Toggle toggle, string imagePath)
        {
            var image = toggle.Q<Image>();
            image.image = EditorGUIUtility.LoadRequired(imagePath) as Texture;
            image.scaleMode = ScaleMode.ScaleToFit;
        }

        private IEnumerable<AnimationClipInfo> GetClipsInAsset() => AnimationDatabase.GetClipsInAsset(Asset).Select(clip => new AnimationClipInfo(clip));

        private void UpdateList()
        {
            if (string.IsNullOrWhiteSpace(_currentSearchString))
                _finalClips = _prefilteredList.ToList();
            else
                _finalClips = _prefilteredList.Where(x => x.AnimationClipName.IndexOf(_currentSearchString, StringComparison.InvariantCultureIgnoreCase) != -1).ToList();
            ListView.itemsSource = _finalClips;
            _searchCountLabel.text = $"{_finalClips.Count}/{_prefilteredList.Count()}";

            ListView.style.display = _finalClips.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _noAnimationsFoundElement.style.display = _finalClips.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleToolbarSearchChanged(ChangeEvent<string> evt)
        {
            _currentSearchString = evt.newValue;
            UpdateList();
        }

        private void HandleCategoryToggleChanged(ChangeEvent<bool> evt, FilterCategory category)
        {
            if (evt.newValue == false)
            {
                (evt.target as Toggle).SetValueWithoutNotify(true);
                return;
            }

            SetCategoryActive(category);
            UpdateList();
        }

        private VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            var image = new Image();
            image.style.width = ItemHeight;
            image.image = _animationIcon;
            container.Add(image);
            container.Add(new Label());

            return container;
        }

        private void BindItem(VisualElement element, int index)
        {
            element.RegisterCallback<ContextClickEvent, int>(ContextClick, index);
            var info = _finalClips[index];
            element.Q<Label>().text = info.AnimationClipName;
            AddDragAndDropManipulator(element, info.AnimationClip.instanceID);
        }

        private void UnbindItem(VisualElement element, int index)
        {
            element.UnregisterCallback<ContextClickEvent, int>(ContextClick);
            if (_dragAndDropManipulatorLookup.TryGetValue(element, out var manipulator))
                element.RemoveManipulator(manipulator);
            _dragAndDropManipulatorLookup.Remove(element);
        }

        private void AddDragAndDropManipulator(VisualElement element, int instanceID)
        {
            var manipulator = new AssetDragAndDropManipulator(instanceID);
            _dragAndDropManipulatorLookup.Add(element, manipulator);
            element.AddManipulator(manipulator);
        }

        private void ContextClick(ContextClickEvent evt, int index)
        {
            var info = _finalClips[index];
            ListView.selectedIndex = index;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Select in Project"), false, () => Selection.activeInstanceID = info.AnimationClip.instanceID);
            menu.AddItem(EditorGUIUtility.TrTextContent("Ping in Project"), false, () => EditorGUIUtility.PingObject(info.AnimationClip.instanceID));
            menu.ShowAsContext();
        }

        public AnimationClipInfo GetClipInfoByIndex(int index) => _finalClips[index];

        private class FilterCategory
        {
            public Toggle Toggle;
            public Func<bool> ValidationFunc;
            public Func<IEnumerable<AnimationClipInfo>> GetClipsFunc;

            public FilterCategory(Toggle toggle, Func<bool> validationFunc, Func<IEnumerable<AnimationClipInfo>> getClipsFunc)
            {
                Toggle = toggle;
                ValidationFunc = validationFunc;
                GetClipsFunc = getClipsFunc;
            }
        }
    }
}