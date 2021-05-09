using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ZeludeEditor
{
    public static class AnimationDatabase
    {
        private static Dictionary<Avatar, List<AnimationClipInfo>> _avatarLookup = new Dictionary<Avatar, List<AnimationClipInfo>>();
        private static List<AnimationClipInfo> _clipsWithNoAvatar = new List<AnimationClipInfo>();
        private static List<AnimationClipInfo> _legacyClips = new List<AnimationClipInfo>();

        static AnimationDatabase()
        {
            _avatarLookup.Clear();
            _clipsWithNoAvatar.Clear();
            _legacyClips.Clear();

            var animations = AssetDatabase.FindAssets("t:Animation");
            
            foreach (var anim in animations)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(anim);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                if (asset is AnimationClip clip)
                {
                    AddClip(clip, importer);
                }
                else
                {
                    var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                    foreach (var subAsset in subAssets)
                        if (subAsset is AnimationClip subClip)
                            AddClip(subClip, importer);
                }
            }
        }

        private static void AddClip(AnimationClip clip, ModelImporter importer)
        {
            if (importer != null)
            {
                if (importer.animationType == ModelImporterAnimationType.Generic || importer.animationType == ModelImporterAnimationType.Human)
                {
                    var avatar = importer.sourceAvatar;
                    AddAvatarClip(clip, avatar);
                }
                else
                {
                    AddLegacyClip(clip);
                }
            }
            else
            {
                AddLegacyClip(clip);
            }
        }

        private static void AddAvatarClip(AnimationClip clip, Avatar avatar)
        {
            if (avatar != null)
            {
                if (!_avatarLookup.ContainsKey(avatar))
                    _avatarLookup.Add(avatar, new List<AnimationClipInfo>());

                _avatarLookup[avatar].Add(new AnimationClipInfo(clip, avatar));
            }
            else
            {
                _clipsWithNoAvatar.Add(new AnimationClipInfo(clip));
            }
        }

        private static void AddLegacyClip(AnimationClip clip)
        {
            _legacyClips.Add(new AnimationClipInfo(clip));
        }

        public static IEnumerable<AnimationClipInfo> GetLegacyClips() => _legacyClips;

        public static IEnumerable<AnimationClipInfo> GetClipsWithoutAvatar() => _clipsWithNoAvatar;

        public static IEnumerable<AnimationClipInfo> GetHumanClips()
        {
            foreach (var kvp in _avatarLookup)
            {
                if (!kvp.Key.isHuman) continue;
                foreach (var clip in kvp.Value)
                    yield return clip;
            }
        }

        public static IEnumerable<AnimationClipInfo> GetClipsForAvatar(Avatar avatar)
        {
            if (_avatarLookup.TryGetValue(avatar, out var list))
                return list;
            return null;
        }

        public static IEnumerable<AnimationClipInfo> GetAllClips()
        {
            foreach (var kvp in _avatarLookup)
                foreach (var clip in kvp.Value)
                    yield return clip;

            foreach (var clip in _clipsWithNoAvatar)
                yield return clip;

            foreach (var clip in _legacyClips)
                yield return clip;
        }
    }

    public readonly struct AnimationClipInfo
    {
        public readonly string AvatarName;
        public readonly string AnimationClipName;
        public readonly LazyLoadReference<Avatar> Avatar;
        public readonly LazyLoadReference<AnimationClip> AnimationClip;

        public AnimationClipInfo(AnimationClip animationClip)
        {
            AvatarName = "";
            Avatar = new LazyLoadReference<Avatar>();
            AnimationClipName = animationClip.name;
            AnimationClip = new LazyLoadReference<AnimationClip>(animationClip);
        }

        public AnimationClipInfo(AnimationClip animationClip, Avatar avatar)
        {
            AvatarName = avatar.name;
            AnimationClipName = animationClip.name;
            Avatar = new LazyLoadReference<Avatar>(avatar);
            AnimationClip = new LazyLoadReference<AnimationClip>(animationClip);
        }
    }
}