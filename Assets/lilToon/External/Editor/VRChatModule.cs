#if !LILTOON_VRCSDK3_AVATARS && !LILTOON_VRCSDK3_WORLDS && VRC_SDK_VRCSDK3
    #if UDON
        #define LILTOON_VRCSDK3_WORLDS
    #else
        #define LILTOON_VRCSDK3_AVATARS
    #endif
#endif
#if UNITY_EDITOR && (LILTOON_VRCSDK3_AVATARS || LILTOON_VRCSDK3_WORLDS || VRC_SDK_VRCSDK2)
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRC.SDKBase.Editor.BuildPipeline;

namespace lilToon.External
{
    //------------------------------------------------------------------------------------------------------------------------------
    // VRChat
    public class VRChatModule : IVRCSDKBuildRequestedCallback, IVRCSDKPreprocessAvatarCallback
    #if LILTOON_VRCSDK3_3
    , IVRCSDKPostprocessAvatarCallback
    #endif
    {
        public int callbackOrder { get { return 100; } }

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            try
            {
                #if !LILTOON_VRCSDK3_3
                if(requestedBuildType == VRCSDKRequestedBuildType.Avatar)
                {
                    lilEditorParameters.instance.forceOptimize = true;
                }
                #endif
                if(requestedBuildType == VRCSDKRequestedBuildType.Scene)
                {
                    lilToonSetting.SetShaderSettingBeforeBuild();
                    EditorApplication.delayCall -= lilToonSetting.SetShaderSettingAfterBuild;
                    EditorApplication.delayCall += lilToonSetting.SetShaderSettingAfterBuild;
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                Debug.Log("[lilToon] OnBuildRequested() failed");
            }
            return true;
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                var materials = GetMaterialsFromGameObject(avatarGameObject);
                var clips = GetAnimationClipsFromGameObject(avatarGameObject);
                lilToonSetting.SetShaderSettingBeforeBuild(materials, clips);
                lilMaterialUtils.SetupMultiMaterial(materials, clips);
                #if !LILTOON_VRCSDK3_3
                EditorApplication.delayCall -= lilToonSetting.SetShaderSettingAfterBuild;
                EditorApplication.delayCall += lilToonSetting.SetShaderSettingAfterBuild;
                #endif
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                Debug.Log("[lilToon] OnPreprocessAvatar() failed");
            }
            return true;
        }

        public void OnPostprocessAvatar()
        {
            lilToonSetting.SetShaderSettingAfterBuild();
        }

        #if LILTOON_VRCSDK3_3
        [InitializeOnLoadMethod]
        private static void StartUp()
        {
            VRC.SDKBase.Editor.VRC_SdkBuilder.RunExportAndTestAvatarBlueprint += (_) => {
                lilEditorParameters.instance.forceOptimize = false;
                return true;
            };
            VRC.SDKBase.Editor.VRC_SdkBuilder.RunExportAvatarBlueprint += (_) => {
                lilEditorParameters.instance.forceOptimize = true;
                return true;
            };
        }
        #endif

        private static Material[] GetMaterialsFromGameObject(GameObject gameObject)
        {
            var materials = new List<Material>();
            foreach(var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                materials.AddRange(renderer.sharedMaterials);
            }
            return materials.ToArray();
        }

        private static AnimationClip[] GetAnimationClipsFromGameObject(GameObject gameObject)
        {
            var clips = new List<AnimationClip>();

            foreach(var animator in gameObject.GetComponentsInChildren<Animator>(true))
            {
                if(animator.runtimeAnimatorController != null) clips.AddRange(animator.runtimeAnimatorController.animationClips);
            }

            #if LILTOON_VRCSDK3_AVATARS
                foreach(var descriptor in gameObject.GetComponentsInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true))
                {
                    foreach(var layer in descriptor.specialAnimationLayers)
                    {
                        if(layer.animatorController != null) clips.AddRange(layer.animatorController.animationClips);
                    }
                    if(descriptor.customizeAnimationLayers)
                    {
                        foreach(var layer in descriptor.baseAnimationLayers)
                        {
                            if(layer.animatorController != null) clips.AddRange(layer.animatorController.animationClips);
                        }
                    }
                }
            #elif VRC_SDK_VRCSDK2
                foreach(var descriptor in gameObject.GetComponentsInChildren<VRCSDK2.VRC_AvatarDescriptor>(true))
                {
                    if(descriptor.CustomSittingAnims != null)
                    {
                        var overridesSitting = new List<KeyValuePair<AnimationClip, AnimationClip>>(descriptor.CustomSittingAnims.overridesCount);
                        descriptor.CustomSittingAnims.GetOverrides(overridesSitting);
                        clips.AddRange(overridesSitting.Select(p => p.Key));
                        clips.AddRange(overridesSitting.Select(p => p.Value));
                    }
                    if(descriptor.CustomStandingAnims != null)
                    {
                        var overridesStanding = new List<KeyValuePair<AnimationClip, AnimationClip>>(descriptor.CustomStandingAnims.overridesCount);
                        descriptor.CustomStandingAnims.GetOverrides(overridesStanding);
                        clips.AddRange(overridesStanding.Select(p => p.Key));
                        clips.AddRange(overridesStanding.Select(p => p.Value));
                    }
                }
            #endif

            return clips.ToArray();
        }

        // Debug
        #if LILTOON_VRCSDK3_AVATARS || VRC_SDK_VRCSDK2
            [MenuItem("GameObject/lilToon/[Debug] Generate bug report (VRChat Avatar)", false, 23)]
            public static void GenerateBugReportVRChatAvatar()
            {
                var clips = new List<AnimationClip>();
                #if LILTOON_VRCSDK3_AVATARS
                    foreach(var descriptor in Selection.activeGameObject.GetComponentsInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true))
                    {
                        foreach(var layer in descriptor.specialAnimationLayers)
                        {
                            if(layer.animatorController != null) clips.AddRange(layer.animatorController.animationClips);
                        }
                        if(descriptor.customizeAnimationLayers)
                        {
                            foreach(var layer in descriptor.baseAnimationLayers)
                            {
                                if(layer.animatorController != null) clips.AddRange(layer.animatorController.animationClips);
                            }
                        }
                    }
                #elif VRC_SDK_VRCSDK2
                    foreach(var descriptor in Selection.activeGameObject.GetComponentsInChildren<VRCSDK2.VRC_AvatarDescriptor>(true))
                    {
                        if(descriptor.CustomSittingAnims != null)
                        {
                            var overridesSitting = new List<KeyValuePair<AnimationClip, AnimationClip>>(descriptor.CustomSittingAnims.overridesCount);
                            descriptor.CustomSittingAnims.GetOverrides(overridesSitting);
                            clips.AddRange(overridesSitting.Select(p => p.Key));
                            clips.AddRange(overridesSitting.Select(p => p.Value));
                        }
                        if(descriptor.CustomStandingAnims != null)
                        {
                            var overridesStanding = new List<KeyValuePair<AnimationClip, AnimationClip>>(descriptor.CustomStandingAnims.overridesCount);
                            descriptor.CustomStandingAnims.GetOverrides(overridesStanding);
                            clips.AddRange(overridesStanding.Select(p => p.Key));
                            clips.AddRange(overridesStanding.Select(p => p.Value));
                        }
                    }
                #endif

                Type type = typeof(lilToonEditorUtils);
                var methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                foreach(var method in methods)
                {
                    var methodParams = method.GetParameters();
                    if(method.Name != "GenerateBugReport" || methodParams.Length != 3) continue;
                    method.Invoke(null, new object[]{null, clips, "# VRChat Avatar Debug"});
                    return;
                }
                #pragma warning disable 0162
                if(lilConstants.currentVersionValue < 31) EditorUtility.DisplayDialog("[Debug] Generate bug report (VRChat Avatar)","This version does not support bug reports. Prease import lilToon 1.3.5 or newer.","OK");
                else                                      EditorUtility.DisplayDialog("[Debug] Generate bug report (VRChat Avatar)","Failed to generate bug report.","OK");
                #pragma warning restore 0162
            }

            [MenuItem("GameObject/lilToon/[Debug] Generate bug report (VRChat Avatar)", true, 23)]
            public static bool CheckGenerateBugReportVRChatAvatar()
            {
                #if LILTOON_VRCSDK3_AVATARS
                    return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>() != null;
                #elif VRC_SDK_VRCSDK2
                    return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<VRCSDK2.VRC_AvatarDescriptor>() != null;
                #endif
            }
        #endif
    }
}
#endif
