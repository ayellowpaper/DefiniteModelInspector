using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ZeludeEditor
{
    public static class AnimationDatabase
    {
        private static Dictionary<Avatar, List<AnimationClip>> _avatarLookup = new Dictionary<Avatar, List<AnimationClip>>();
        private static List<AnimationClip> _clipsWithNoAvatar = new List<AnimationClip>();
        private static List<AnimationClip> _legacyClips = new List<AnimationClip>();

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

        private static void AddAvatarClip(AnimationClip info, Avatar avatar)
        {
            if (avatar != null)
            {
                if (!_avatarLookup.ContainsKey(avatar))
                    _avatarLookup.Add(avatar, new List<AnimationClip>());

                _avatarLookup[avatar].Add(info);
            }
            else
            {
                _clipsWithNoAvatar.Add(info);
            }
        }

        private static void AddLegacyClip(AnimationClip info)
        {
            _legacyClips.Add(info);
        }

        public static IEnumerable<AnimationClip> GetLegacyClips() => _legacyClips;

        public static IEnumerable<AnimationClip> GetClipsWithoutAvatar() => _clipsWithNoAvatar;

        public static IEnumerable<AnimationClip> GetHumanClips()
        {
            foreach (var kvp in _avatarLookup)
            {
                if (!kvp.Key.isHuman) continue;
                foreach (var clip in kvp.Value)
                    yield return clip;
            }
        }

        public static IEnumerable<AnimationClip> GetClipsForAvatar(Avatar avatar)
        {
            if (_avatarLookup.TryGetValue(avatar, out var list))
                return list;
            return null;
        }

        public static IEnumerable<AnimationClip> GetAllClips()
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
}