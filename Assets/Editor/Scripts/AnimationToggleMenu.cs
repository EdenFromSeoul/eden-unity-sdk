using UnityEngine;
using UnityEditor;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Collections.Generic;
using UnityEditor.Animations;

public class MakeAnimationToggleWindow : EditorWindow
{
    private GameObject selectedObject;
    private VRCExpressionsMenu _expressionsMenu;
    private VRCExpressionsMenu _closet;
    private AnimatorController animatorController;

    [MenuItem("Window/MakeAnimationToggle")]
    public static void ShowWindow()
    {
        GetWindow<MakeAnimationToggleWindow>("MakeAnimationToggle");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select an object to animate", EditorStyles.boldLabel);

        // 오브젝트를 선택할 수 있는 필드
        selectedObject = (GameObject)EditorGUILayout.ObjectField("Animation Target", selectedObject, typeof(GameObject), true);

        if (GUILayout.Button("Make Toggle Animation and start"))
        {
            if (selectedObject)
            {
                CreateAnimation();
                AddComponents();

                // EditorApplication.EnterPlaymode();
                //this.Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject", "OK");
            }
        }
    }

    /*
     * CreatAnimation은 오브젝트의 활성화/비활성화 상태를 bool parameter에 입력 마다 전환되는 애니메이션을 만들고,
     * Assets/MakeToggle/{obj.name}에 저장합니다.
     */
    private void CreateAnimation()
    {
        AnimationClip toggleOnClip = new AnimationClip();
        AnimationClip toggleOffClip = new AnimationClip();
        
        SkinnedMeshRenderer[] renderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            string animPath = renderer.transform.name;
            
            AddAnimationCurve(toggleOnClip, animPath, true);
            AddAnimationCurve(toggleOffClip, animPath, false);
        }
        
        string folderPath = "Assets/MakeToggle";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "MakeToggle");
        }

        folderPath += $"/{selectedObject.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/MakeToggle", selectedObject.name);
        }
        AssetDatabase.CreateAsset(toggleOnClip, $"{folderPath}/{selectedObject.name}_ToggleOn.anim");
        AssetDatabase.CreateAsset(toggleOffClip, $"{folderPath}/{selectedObject.name}_ToggleOff.anim");
        
        animatorController = AnimatorController.CreateAnimatorControllerAtPath($"{folderPath}/{selectedObject.name}_ToggleAnimatorController.controller");
        
        animatorController.AddParameter($"{selectedObject.name}Toggle", AnimatorControllerParameterType.Bool);

        AnimatorState toggleOnState = animatorController.AddMotion(toggleOnClip);
        AnimatorState toggleOffState = animatorController.AddMotion(toggleOffClip);
        
        animatorController.layers[0].stateMachine.defaultState = toggleOnState;
        
        AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
        AnimatorStateTransition toOffTransition = toggleOnState.AddTransition(toggleOffState);
        toOffTransition.AddCondition(AnimatorConditionMode.IfNot, 0, $"{selectedObject.name}Toggle");
        toOffTransition.hasExitTime = false;

        AnimatorStateTransition toOnTransition = toggleOffState.AddTransition(toggleOnState);
        toOnTransition.AddCondition(AnimatorConditionMode.If, 0, $"{selectedObject.name}Toggle");
        toOnTransition.hasExitTime = false;
        
        Animator animator = selectedObject.GetComponent<Animator>();
        if (!animator)
        {
            animator = selectedObject.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void AddAnimationCurve(AnimationClip clip, string path, bool isActive)
    {
        // AnimationCurve를 사용하여 키프레임을 설정
        AnimationCurve curve = AnimationCurve.Constant(0, 0, isActive ? 1f : 0f);
        clip.SetCurve(path, typeof(GameObject), "m_IsActive", curve);
    }

    /*
     * AddComponents는 필요한 컴포넌트들을 추가하고, 각 컴포넌트에 적절한 값을 넣는 작업을 수행합니다.
     */
    private void AddComponents()
    {
        // 1. Menu 생성
        CreateVRCExpressionsMenu();

        // 2. MA Menu Installer 컴포넌트 추가
        ModularAvatarMenuInstaller installer = selectedObject.GetComponent<ModularAvatarMenuInstaller>();
        if (!installer)
        {
            installer = selectedObject.AddComponent<ModularAvatarMenuInstaller>();
        }
        else
        {
            //Menu Installer가 있으면 이미 해당 오브젝트는 진행했다 가정
            return;
        }
        
        // rootMenu를 찾아서 installTargetMenu로 설정
        VRCAvatarDescriptor avatarDescriptor = selectedObject.GetComponentInParent<VRCAvatarDescriptor>();
        if (avatarDescriptor)
        {
            installer.installTargetMenu = avatarDescriptor.expressionsMenu;
        }
        
        VRCExpressionsMenu rootMenu = installer.installTargetMenu;
        if (!findClosetMenuControl(rootMenu))
        {
            var control = new VRCExpressionsMenu.Control
            {
                name = "Closet",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = _closet
            };
            rootMenu.controls.Add(control);
        }
        
        installer.installTargetMenu = _closet;
        installer.menuToAppend = _expressionsMenu;
        installer.menuToAppend.controls[0].parameter.name = animatorController.parameters[0].name;
        
        // 3. MA Parameters 컴포넌트 추가
        ModularAvatarParameters parameters = selectedObject.GetComponent<ModularAvatarParameters>();
        if (!parameters)
        {
            parameters = selectedObject.AddComponent<ModularAvatarParameters>();
        }

        // 4. bool 타입의 파라미터 생성 -> animation cotroller에 등록된 파라미터
        parameters.parameters.Add(new ParameterConfig
        {
            internalParameter = false,
            nameOrPrefix = animatorController.parameters[0].name,
            isPrefix = false,
            remapTo = "",
            syncType = ParameterSyncType.Bool,
            saved = true,
        });
        

        // 5. MA Merge Animator 컴포넌트 추가
        ModularAvatarMergeAnimator mergeAnimator = selectedObject.GetComponent<ModularAvatarMergeAnimator>();
        if (!mergeAnimator)
        {
            mergeAnimator = selectedObject.AddComponent<ModularAvatarMergeAnimator>();
        }

        // 기본 설정 추가
        mergeAnimator.animator = selectedObject.GetComponent<Animator>().runtimeAnimatorController;
        mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        mergeAnimator.deleteAttachedAnimator = true;
        mergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
        mergeAnimator.matchAvatarWriteDefaults = false;
        mergeAnimator.relativePathRoot = new AvatarObjectReference();
        mergeAnimator.layerPriority = 0;
    }

    bool findClosetMenuControl(VRCExpressionsMenu rootMenu)
    {
        if (!rootMenu) return false;
        foreach (var control in rootMenu.controls)
        {
            if (control.name == "Closet" && control.subMenu == _closet)
                return true;
            else if (control.name == "Closet")
            {
                rootMenu.controls.Remove(control);
            }
        }

        return false; 
    }

    private void CreateVRCExpressionsMenu()
    {
        //Assets/MakeToggle 확인
        string folderPath = "Assets/MakeToggle";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "MakeToggle");
        }

        //Assets/MakeToggle/ClosetMenu.asset 확인
        string closetPath = folderPath + "/ClosetMenu.asset";
        _closet = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(closetPath);
        if (!_closet)
        {
            _closet = CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(_closet, closetPath);
            AssetDatabase.SaveAssets();
        }
        
        //Assets/MakeToggle/selectObject.name 폴더 확인
        folderPath += $"/{selectedObject.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("MakeToggle", selectedObject.name);
        }
        
        //Asset/MakeToggle/selectObject.name/NewExpressionMenu.asset 확인
        string path = folderPath + "/NewExpressionsMenu.asset";
        _expressionsMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
        if (_expressionsMenu)
        {
            return;
        }
        
        _expressionsMenu = CreateInstance<VRCExpressionsMenu>();
        var control = new VRCExpressionsMenu.Control
        {
            name = selectedObject.name,
            type = VRCExpressionsMenu.Control.ControlType.Toggle,
            parameter = new VRCExpressionsMenu.Control.Parameter
            {
                name = selectedObject.name + "Toggle"
            }
        };
        _expressionsMenu.controls.Add(control);
        
        AssetDatabase.CreateAsset(_expressionsMenu, path);
        AssetDatabase.SaveAssets();
    }
}