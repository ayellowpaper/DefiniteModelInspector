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

        public UnityEngine.Object Asset
        {
            get => _asset;
            set {
                if (_asset == value) return;
                _asset = value;
                UpdateCategoriesState();
                UpdateList();
            }
        }

        public ListView ListView { get; private set; }

        private List<AnimationClip> _finalClips;
        private IEnumerable<AnimationClip> _prefilteredList;
        private Texture _animationIcon;
        private TextElement _searchCountLabel;
        private VisualElement _noAnimationsFoundElement;
        private string _currentSearchString = null;
        private Toggle _activeToggle;
        private List<FilterCategory> _filterCategories;
        private UnityEngine.Object _asset;

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
            var humanFilter = new FilterCategory(uxml.Q<Toggle>("toggle-human"), () => Asset == null ^ (GetAvatarFromAsset() is Avatar avatar && avatar.isHuman), AnimationDatabase.GetHumanClips);
            var avatarFilter = new FilterCategory(uxml.Q<Toggle>("toggle-avatar"), () => GetAvatarFromAsset() != null, () => AnimationDatabase.GetClipsForAvatar(GetAvatarFromAsset()));
            var assetFilter = new FilterCategory(uxml.Q<Toggle>("toggle-asset"), () => Asset != null, GetClipsOnAsset);
            var allFilter = new FilterCategory(uxml.Q<Toggle>("toggle-all"), () => true, AnimationDatabase.GetAllClips);

            SetCategoryImage(humanFilter.Toggle, "HumanTemplate Icon");
            SetCategoryImage(avatarFilter.Toggle, "d_AvatarSelector");
            SetCategoryImage(assetFilter.Toggle, "d_GameObject Icon");
            SetCategoryImage(allFilter.Toggle, "d_AnimationClip Icon");

            _filterCategories = new List<FilterCategory>() { humanFilter, avatarFilter, assetFilter, allFilter };

            SetCategoryActive(allFilter);

            foreach (var info in _filterCategories)
            {
                info.Toggle.RegisterValueChangedCallback(evt => { _prefilteredList = info.GetClipsFunc(); Callback(evt); });
            }

            ListView = new ListView(_finalClips, ItemHeight, MakeItem, BindItem);
            ListView.selectionType = SelectionType.Single;
            uxml.Q("content").Add(ListView);

            uxml.Q<ToolbarSearchField>().RegisterValueChangedCallback(ToolbarSearchChanged);

            Add(uxml);

            UpdateCategoriesState();
            UpdateList();
        }

        private void UpdateCategoriesState()
        {
            foreach (var category in _filterCategories)
            {
                category.Toggle.SetEnabled(category.ValidationFunc());
            }
        }

        private Avatar GetAvatarFromAsset()
        {
            if (Asset == null) return null;
            if (Asset is Avatar avatar) return avatar;

            var path = AssetDatabase.GetAssetPath(Asset);
            var embeddedAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            if (embeddedAvatar != null) return embeddedAvatar;

            var importer = AssetImporter.GetAtPath(path);
            if (importer is ModelImporter modelImporter && modelImporter.sourceAvatar != null) return modelImporter.sourceAvatar;
            return null;
        }

        private void SetCategoryActive(FilterCategory category)
        {
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

        private IEnumerable<AnimationClip> GetClipsOnAsset()
        {
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(Asset));
            foreach (var subAsset in subAssets)
                if (subAsset is AnimationClip clip)
                    yield return clip;
        }

        private void ToolbarSearchChanged(ChangeEvent<string> evt)
        {
            _currentSearchString = evt.newValue;
            UpdateList();
        }

        private void UpdateList()
        {
            if (string.IsNullOrWhiteSpace(_currentSearchString))
                _finalClips = _prefilteredList.ToList();
            else
                _finalClips = _prefilteredList.Where(x => x.name.IndexOf(_currentSearchString, StringComparison.InvariantCultureIgnoreCase) != -1).ToList();
            ListView.itemsSource = _finalClips;
            _searchCountLabel.text = $"{_finalClips.Count}/{_prefilteredList.Count()}";

            ListView.style.display = _finalClips.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _noAnimationsFoundElement.style.display = _finalClips.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Callback(ChangeEvent<bool> evt)
        {
            if (evt.newValue == false)
            {
                (evt.target as Toggle).SetValueWithoutNotify(true);
                return;
            }

            _activeToggle.SetValueWithoutNotify(false);
            _activeToggle = evt.target as Toggle;
            UpdateList();
        }

        private void BindItem(VisualElement element, int index)
        {
            element.UnregisterCallback<MouseDownEvent, int>(StartDrag);
            element.RegisterCallback<MouseDownEvent, int>(StartDrag, index);
            element.Q<Label>().text = _finalClips[index].name;
        }

        private void StartDrag(MouseDownEvent evt, int index)
        {
            DragAndDrop.objectReferences = new UnityEngine.Object[] { _finalClips[index] };
            DragAndDrop.StartDrag($"Drag {_finalClips[index]}");
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

        private class FilterCategory
        {
            public Toggle Toggle;
            public Func<bool> ValidationFunc;
            public Func<IEnumerable<AnimationClip>> GetClipsFunc;

            public FilterCategory(Toggle toggle, Func<bool> validationFunc, Func<IEnumerable<AnimationClip>> getClipsFunc)
            {
                Toggle = toggle;
                ValidationFunc = validationFunc;
                GetClipsFunc = getClipsFunc;
            }
        }
    }
}