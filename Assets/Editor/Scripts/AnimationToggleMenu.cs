using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using VRC.SDKBase;

public class MakeAnimationToggleWindow : EditorWindow
{
    private GameObject selectedObject; // 선택한 오브젝트
    private VRCExpressionsMenu _expressionsMenu; // VRC 표현 메뉴
    private VRCExpressionsMenu _closet; // 옷장 서브메뉴
    private AnimatorController animatorController; // 애니메이터 컨트롤러

    // Unity Editor 메뉴에 "EasyMakeCloset" 항목을 추가
    [MenuItem("Window/EasyMakeCloset")]
    public static void ShowWindow()
    {
        GetWindow<MakeAnimationToggleWindow>("EasyMakeCloset");
    }

    // Editor 윈도우 GUI 생성
    private void OnGUI()
    {
        GUILayout.Label("Select an object to Make Closet", EditorStyles.boldLabel);

        // 타겟 오브젝트 필드
        selectedObject = (GameObject)EditorGUILayout.ObjectField("Target(Clothes)", selectedObject, typeof(GameObject), true);

        if (GUILayout.Button("Make Closet"))
        {
            GameObject avatarObject = FindAvatarDescriptor();
            
            if (selectedObject && avatarObject)
            {
                GameObject clothes = CheckAlreadyExists(avatarObject);
                if (!clothes)
                {
                    GameObject instance = Instantiate(selectedObject, avatarObject.transform);
                    instance.name = selectedObject.name; // 인스턴스 이름을 원본 프리팹 이름과 동일하게 설정

                    // MenuCommand를 생성하여 SetupOutfit 메서드에 전달

                    Selection.activeObject = instance;
                    MenuCommand menuCommand = new MenuCommand(instance);

                    // 리플렉션을 사용하여 내부 메서드 호출
                    InvokeInternalSetupOutfit(menuCommand);
                    Selection.activeObject = null;

                    CreateAnimation(instance);
                    AddComponents(instance);
                }
                else
                {
                    DestroyImmediate(clothes);
                }
            }
            else if (!selectedObject)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject", "OK");
            }
            else if (!avatarObject)
            {
                EditorUtility.DisplayDialog("Error", "Please make avatar instance in level", "OK");
            }
        }
    }

    // 아바타 오브젝트에 이미 동일한 이름의 옷이 있는지 확인
    private GameObject CheckAlreadyExists(GameObject avatarObject)
    {
        Transform foundTransform = avatarObject.transform.Find(selectedObject.name);
        return foundTransform ? foundTransform.gameObject : null;
    }
    
    // 내부 SetupOutfit 메서드를 리플렉션을 사용하여 호출
    private void InvokeInternalSetupOutfit(MenuCommand menuCommand)
    {
        // 어셈블리 로드
        Assembly assembly = Assembly.LoadFrom("Library\\ScriptAssemblies\\nadena.dev.modular-avatar.core.editor.dll");
        // EasySetupOutfit 타입 가져오기
        var type = assembly.GetType("nadena.dev.modular_avatar.core.editor.EasySetupOutfit");
        
        // SetupOutfit 메서드 가져오기
        MethodInfo methodInfo = type.GetMethod("SetupOutfit", BindingFlags.Static | BindingFlags.NonPublic);
        if (methodInfo == null)
        {
            Debug.LogError("메서드를 찾을 수 없습니다: SetupOutfit");
            return;
        }

        // 메서드 호출
        methodInfo.Invoke(null, new object[] { menuCommand });
        Console.ReadKey(); 
    }
    
    // 씬 내에서 아바타 디스크립터를 찾음
    private GameObject FindAvatarDescriptor()
    {
        VRC_AvatarDescriptor[] avatars = FindObjectsOfType<VRC_AvatarDescriptor>();
        if (avatars.Length > 0)
        {
            return avatars[0].gameObject;
        }
        return null;
    }

    // 애니메이션 클립을 생성
    private void CreateAnimation(GameObject targetObject)
    {
        // 애니메이션 클립 생성
        AnimationClip toggleOnClip = new AnimationClip();
        AnimationClip toggleOffClip = new AnimationClip();
        
        // 타겟 오브젝트의 자식들 중 SkinnedMeshRenderer를 포함한 모든 오브젝트 가져오기
        SkinnedMeshRenderer[] renderers = targetObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        // 각 렌더러에 대해 애니메이션 커브 추가
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            string animPath = renderer.transform.name;
            
            AddAnimationCurve(toggleOnClip, animPath, true);
            AddAnimationCurve(toggleOffClip, animPath, false);
        }
        
        // 애니메이션 파일을 저장할 폴더 경로 설정
        string folderPath = "Assets/EasyCloset";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "EasyCloset");
        }

        // 타겟 오브젝트 이름으로 하위 폴더 생성
        folderPath += $"/{targetObject.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/EasyCloset", targetObject.name);
        }
        // 애니메이션 클립을 에셋으로 저장
        AssetDatabase.CreateAsset(toggleOnClip, $"{folderPath}/{targetObject.name}_ToggleOn.anim");
        AssetDatabase.CreateAsset(toggleOffClip, $"{folderPath}/{targetObject.name}_ToggleOff.anim");
        
        // 애니메이터 컨트롤러 생성
        animatorController = AnimatorController.CreateAnimatorControllerAtPath($"{folderPath}/{targetObject.name}_ToggleAnimatorController.controller");
        
        // 애니메이터 컨트롤러에 파라미터 추가
        animatorController.AddParameter($"{targetObject.name}Toggle", AnimatorControllerParameterType.Bool);
        
        // 애니메이션 상태 생성 및 연결
        AnimatorState toggleOnState = animatorController.AddMotion(toggleOnClip);
        AnimatorState toggleOffState = animatorController.AddMotion(toggleOffClip);
        
        // 기본 상태를 toggleOnState로 설정
        animatorController.layers[0].stateMachine.defaultState = toggleOnState;
        
        // 상태 머신에서 상태 전환 설정
        AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
        AnimatorStateTransition toOffTransition = toggleOnState.AddTransition(toggleOffState);
        toOffTransition.AddCondition(AnimatorConditionMode.IfNot, 0, $"{targetObject.name}Toggle");
        toOffTransition.hasExitTime = false;
        
        AnimatorStateTransition toOnTransition = toggleOffState.AddTransition(toggleOnState);
        toOnTransition.AddCondition(AnimatorConditionMode.If, 0, $"{targetObject.name}Toggle");
        toOnTransition.hasExitTime = false;
        
        // 타겟 오브젝트에 애니메이터 컴포넌트 추가
        Animator animator = targetObject.GetComponent<Animator>();
        if (!animator)
        {
            animator = targetObject.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;
        
        // 에셋 데이터베이스 저장 및 갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // 애니메이션 클립에 애니메이션 커브를 추가
    private void AddAnimationCurve(AnimationClip clip, string path, bool isActive)
    {
        // 애니메이션 커브 생성 (활성화 여부에 따라 값 설정)
        AnimationCurve curve = AnimationCurve.Constant(0, 0, isActive ? 1f : 0f);
        clip.SetCurve(path, typeof(GameObject), "m_IsActive", curve);
    }

    // 타겟 오브젝트에 필요한 컴포넌트를 추가
    private void AddComponents(GameObject targetObject)
    {
        // 1. Menu 생성
        CreateVRCExpressionsMenu(targetObject);

        // 2. MA Menu Installer 컴포넌트 추가
        ModularAvatarMenuInstaller installer = targetObject.GetComponent<ModularAvatarMenuInstaller>();
        if (!installer)
        {
            installer = targetObject.AddComponent<ModularAvatarMenuInstaller>();
        }
        else
        {
            //Menu Installer가 있으면 이미 해당 오브젝트는 진행했다 가정
            return;
        }
        
        // rootMenu를 찾아서 installTargetMenu로 설정
        VRCAvatarDescriptor avatarDescriptor = targetObject.GetComponentInParent<VRCAvatarDescriptor>();
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
        ModularAvatarParameters parameters = targetObject.GetComponent<ModularAvatarParameters>();
        if (!parameters)
        {
            parameters = targetObject.AddComponent<ModularAvatarParameters>();
        }

        // 4. bool 타입의 파라미터 생성 -> animation controller에 등록된 파라미터
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
        ModularAvatarMergeAnimator mergeAnimator = targetObject.GetComponent<ModularAvatarMergeAnimator>();
        if (!mergeAnimator)
        {
            mergeAnimator = targetObject.AddComponent<ModularAvatarMergeAnimator>();
        }

        // 기본 설정 추가
        mergeAnimator.animator = targetObject.GetComponent<Animator>().runtimeAnimatorController;
        mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        mergeAnimator.deleteAttachedAnimator = true;
        mergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
        mergeAnimator.matchAvatarWriteDefaults = false;
        mergeAnimator.relativePathRoot = new AvatarObjectReference();
        mergeAnimator.layerPriority = 0;
    }

    // Closet 메뉴가 존재하는지 확인
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

    // VRC 표현 메뉴 생성
    private void CreateVRCExpressionsMenu(GameObject targetObject)
    {
        //Assets/MakeToggle 확인
        string folderPath = "Assets/EasyCloset";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "EasyCloset");
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
        folderPath += $"/{targetObject.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("EasyCloset", targetObject.name);
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
            name = targetObject.name,
            type = VRCExpressionsMenu.Control.ControlType.Toggle,
            parameter = new VRCExpressionsMenu.Control.Parameter
            {
                name = targetObject.name + "Toggle"
            }
        };
        _expressionsMenu.controls.Add(control);
        
        AssetDatabase.CreateAsset(_expressionsMenu, path);
        AssetDatabase.SaveAssets();
    }
}
