#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace lilToon
{
    public static class lilMaterialBaker
    {
        public static void TextureBake(Material material, int bakeType)
        {
            var dics = GetProps(material);
            var mainColor = dics.GetColor("_Color");
            var mainTexHSVG = dics.GetVector("_MainTexHSVG");
            var mainGradationStrength = dics.GetFloat("_MainGradationStrength");
            var useMain2ndTex = dics.GetFloat("_UseMain2ndTex");
            var useMain3rdTex = dics.GetFloat("_UseMain3rdTex");
            var mainTex = dics.GetTexture("_MainTex");
            bool shouldNotBakeColor = (bakeType == 1 || bakeType == 4) && mainColor == Color.white && mainTexHSVG == lilConstants.defaultHSVG && mainGradationStrength == 0.0;
            bool cannotBake1st = mainTex == null;
            bool shouldNotBake2nd = (bakeType == 2 || bakeType == 5) && useMain2ndTex == 0.0;
            bool shouldNotBake3rd = (bakeType == 3 || bakeType == 6) && useMain3rdTex == 0.0;
            bool shouldNotBakeAll = bakeType == 0 && mainColor == Color.white && mainTexHSVG == lilConstants.defaultHSVG && mainGradationStrength == 0.0 && useMain2ndTex == 0.0 && useMain3rdTex == 0.0;
            if(cannotBake1st)
            {
                EditorUtility.DisplayDialog(S("sDialogCannotBake"), S("sDialogSetMainTex"), S("sOK"));
            }
            else if(shouldNotBakeColor)
            {
                EditorUtility.DisplayDialog(S("sDialogNoNeedBake"), S("sDialogNoChange"), S("sOK"));
            }
            else if(shouldNotBake2nd)
            {
                EditorUtility.DisplayDialog(S("sDialogNoNeedBake"), S("sDialogNotUse2nd"), S("sOK"));
            }
            else if(shouldNotBake3rd)
            {
                EditorUtility.DisplayDialog(S("sDialogNoNeedBake"), S("sDialogNotUse3rd"), S("sOK"));
            }
            else if(shouldNotBakeAll)
            {
                EditorUtility.DisplayDialog(S("sDialogNoNeedBake"), S("sDialogNotUseAll"), S("sOK"));
            }
            else
            {
                bool bake2nd = (bakeType == 0 || bakeType == 2 || bakeType == 5) && useMain2ndTex != 0.0;
                bool bake3rd = (bakeType == 0 || bakeType == 3 || bakeType == 6) && useMain3rdTex != 0.0;
                // run bake
                var bufMainTexture = mainTex as Texture2D;
                var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

                string path;

                var srcTexture = new Texture2D(2, 2);
                var srcMain2 = new Texture2D(2, 2);
                var srcMain3 = new Texture2D(2, 2);
                var srcMask2 = new Texture2D(2, 2);
                var srcMask3 = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color", mainColor);
                hsvgMaterial.SetVector("_MainTexHSVG", mainTexHSVG);
                hsvgMaterial.SetFloat("_MainGradationStrength", mainGradationStrength);
                hsvgMaterial.CopyTexture(dics, "_MainGradationTex");
                hsvgMaterial.CopyTexture(dics, "_MainColorAdjustMask");

                path = AssetDatabase.GetAssetPath(mainTex);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                if(bake2nd)
                {
                    hsvgMaterial.CopyFloat(dics, "_UseMain2ndTex");
                    hsvgMaterial.CopyColor(dics, "_MainColor2nd");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexAngle");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsDecal");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsLeftOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsRightOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexShouldCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexShouldFlipMirror");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexShouldFlipCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsMSDF");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexBlendMode");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexAlphaMode");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main2ndTex");
                    hsvgMaterial.CopyTextureScale(dics, "_Main2ndTex");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main2ndBlendMask");
                    hsvgMaterial.CopyTextureScale(dics, "_Main2ndBlendMask");

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main2ndTex"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMain2, path);
                        hsvgMaterial.SetTexture("_Main2ndTex", srcMain2);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main2ndTex", Texture2D.whiteTexture);
                    }

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main2ndBlendMask"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMask2, path);
                        hsvgMaterial.SetTexture("_Main2ndBlendMask", srcMask2);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main2ndBlendMask", Texture2D.whiteTexture);
                    }
                }

                if(bake3rd)
                {
                    hsvgMaterial.CopyFloat(dics, "_UseMain3rdTex");
                    hsvgMaterial.CopyColor(dics, "_MainColor3rd");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexAngle");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsDecal");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsLeftOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsRightOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexShouldCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexShouldFlipMirror");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexShouldFlipCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsMSDF");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexBlendMode");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexAlphaMode");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main3rdTex");
                    hsvgMaterial.CopyTextureScale(dics, "_Main3rdTex");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main3rdBlendMask");
                    hsvgMaterial.CopyTextureScale(dics, "_Main3rdBlendMask");

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main3rdTex"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMain3, path);
                        hsvgMaterial.SetTexture("_Main3rdTex", srcMain3);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main3rdTex", Texture2D.whiteTexture);
                    }

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main3rdBlendMask"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMask3, path);
                        hsvgMaterial.SetTexture("_Main3rdBlendMask", srcMask3);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main3rdBlendMask", Texture2D.whiteTexture);
                    }
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex");
                if(outTexture != mainTex)
                {
                    material.SetVector("_MainTexHSVG", lilConstants.defaultHSVG);
                    material.SetColor("_Color", Color.white);
                    material.SetFloat("_MainGradationStrength", 0.0f);
                    material.SetTexture("_MainGradationTex", null);
                    if(bake2nd)
                    {
                        material.SetFloat("_UseMain2ndTex", 0.0f);
                        material.SetTexture("_Main2ndTex", null);
                    }
                    if(bake3rd)
                    {
                        material.SetFloat("_UseMain3rdTex", 0.0f);
                        material.SetTexture("_Main3rdTex", null);
                    }
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                material.SetTexture("_MainTex", outTexture);

                Object.DestroyImmediate(hsvgMaterial);
                Object.DestroyImmediate(srcTexture);
                Object.DestroyImmediate(srcMain2);
                Object.DestroyImmediate(srcMain3);
                Object.DestroyImmediate(srcMask2);
                Object.DestroyImmediate(srcMask3);
            }
        }

        public static Texture AutoBakeMainTexture(Material material)
        {
            var dics = GetProps(material);
            var mainColor = dics.GetColor("_Color");
            var mainTexHSVG = dics.GetVector("_MainTexHSVG");
            var mainGradationStrength = dics.GetFloat("_MainGradationStrength");
            var useMain2ndTex = dics.GetFloat("_UseMain2ndTex");
            var useMain3rdTex = dics.GetFloat("_UseMain3rdTex");
            var mainTex = dics.GetTexture("_MainTex");
            bool shouldNotBakeAll = mainColor == Color.white && mainTexHSVG == lilConstants.defaultHSVG && mainGradationStrength == 0.0 && useMain2ndTex == 0.0 && useMain3rdTex == 0.0;
            if(!shouldNotBakeAll && EditorUtility.DisplayDialog(S("sDialogRunBake"), S("sDialogBakeMain"), S("sYes"), S("sNo")))
            {
                bool bake2nd = useMain2ndTex != 0.0;
                bool bake3rd = useMain3rdTex != 0.0;
                // run bake
                var bufMainTexture = mainTex as Texture2D;
                var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

                string path;

                var srcTexture = new Texture2D(2, 2);
                var srcMain2 = new Texture2D(2, 2);
                var srcMain3 = new Texture2D(2, 2);
                var srcMask2 = new Texture2D(2, 2);
                var srcMask3 = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color", Color.white);
                hsvgMaterial.SetVector("_MainTexHSVG", mainTexHSVG);
                hsvgMaterial.SetFloat("_MainGradationStrength", mainGradationStrength);
                hsvgMaterial.CopyTexture(dics, "_MainGradationTex");
                hsvgMaterial.CopyTexture(dics, "_MainColorAdjustMask");

                path = AssetDatabase.GetAssetPath(mainTex);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                if(bake2nd)
                {
                    hsvgMaterial.CopyFloat(dics, "_UseMain2ndTex");
                    hsvgMaterial.CopyColor(dics, "_MainColor2nd");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexAngle");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsDecal");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsLeftOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsRightOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexShouldCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexShouldFlipMirror");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexShouldFlipCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexIsMSDF");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexBlendMode");
                    hsvgMaterial.CopyFloat(dics, "_Main2ndTexAlphaMode");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main2ndTex");
                    hsvgMaterial.CopyTextureScale(dics, "_Main2ndTex");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main2ndBlendMask");
                    hsvgMaterial.CopyTextureScale(dics, "_Main2ndBlendMask");

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main2ndTex"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMain2, path);
                        hsvgMaterial.SetTexture("_Main2ndTex", srcMain2);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main2ndTex", Texture2D.whiteTexture);
                    }

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main2ndBlendMask"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMask2, path);
                        hsvgMaterial.SetTexture("_Main2ndBlendMask", srcMask2);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main2ndBlendMask", Texture2D.whiteTexture);
                    }
                }

                if(bake3rd)
                {
                    hsvgMaterial.CopyFloat(dics, "_UseMain3rdTex");
                    hsvgMaterial.CopyColor(dics, "_MainColor3rd");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexAngle");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsDecal");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsLeftOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsRightOnly");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexShouldCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexShouldFlipMirror");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexShouldFlipCopy");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexIsMSDF");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexBlendMode");
                    hsvgMaterial.CopyFloat(dics, "_Main3rdTexAlphaMode");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main3rdTex");
                    hsvgMaterial.CopyTextureScale(dics, "_Main3rdTex");
                    hsvgMaterial.CopyTextureOffset(dics, "_Main3rdBlendMask");
                    hsvgMaterial.CopyTextureScale(dics, "_Main3rdBlendMask");

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main3rdTex"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMain3, path);
                        hsvgMaterial.SetTexture("_Main3rdTex", srcMain3);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main3rdTex", Texture2D.whiteTexture);
                    }

                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Main3rdBlendMask"));
                    if(!string.IsNullOrEmpty(path))
                    {
                        lilTextureUtils.LoadTexture(ref srcMask3, path);
                        hsvgMaterial.SetTexture("_Main3rdBlendMask", srcMask3);
                    }
                    else
                    {
                        hsvgMaterial.SetTexture("_Main3rdBlendMask", Texture2D.whiteTexture);
                    }
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex");
                if(outTexture != mainTex)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                Object.DestroyImmediate(hsvgMaterial);
                Object.DestroyImmediate(srcTexture);
                Object.DestroyImmediate(srcMain2);
                Object.DestroyImmediate(srcMain3);
                Object.DestroyImmediate(srcMask2);
                Object.DestroyImmediate(srcMask3);

                return outTexture;
            }
            else
            {
                return mainTex;
            }
        }

        public static Texture AutoBakeShadowTexture(Material material, Texture bakedMainTex, int shadowType = 0, bool shouldShowDialog = true)
        {
            var dics = GetProps(material);
            var useShadow = dics.GetFloat("_UseShadow");
            var shadowColor = dics.GetColor("_ShadowColor");
            var shadowColorTex = dics.GetTexture("_ShadowColorTex");
            var shadowStrength = dics.GetFloat("_ShadowStrength");
            var shadowStrengthMask = dics.GetTexture("_ShadowStrengthMask");
            var mainTex = dics.GetTexture("_MainTex");

            bool shouldNotBakeAll = useShadow == 0.0 && shadowColor == Color.white && shadowColorTex == null && shadowStrengthMask == null;
            bool shouldBake = true;
            if(shouldShowDialog) shouldBake = EditorUtility.DisplayDialog(S("sDialogRunBake"), S("sDialogBakeShadow"), S("sYes"), S("sNo"));
            if(!shouldNotBakeAll && shouldBake)
            {
                // run bake
                var bufMainTexture = bakedMainTex as Texture2D;
                var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

                string path;

                var srcTexture = new Texture2D(2, 2);
                var srcMain2 = new Texture2D(2, 2);
                var srcMask2 = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color",                 Color.white);
                hsvgMaterial.SetVector("_MainTexHSVG",          lilConstants.defaultHSVG);
                hsvgMaterial.SetFloat("_UseMain2ndTex",         1.0f);
                hsvgMaterial.SetFloat("_UseMain3rdTex",         1.0f);
                hsvgMaterial.SetColor("_MainColor3rd",          new Color(1.0f,1.0f,1.0f, dics.GetFloat("_ShadowMainStrength")));
                hsvgMaterial.SetFloat("_Main3rdTexBlendMode",   3.0f);
                if(shadowType == 2)
                {
                    var shadow2ndColor = dics.GetColor("_Shadow2ndColor");
                    hsvgMaterial.SetColor("_MainColor2nd",                new Color(shadow2ndColor.r, shadow2ndColor.g, shadow2ndColor.b, shadow2ndColor.a * shadowStrength));
                    hsvgMaterial.SetFloat("_Main2ndTexBlendMode",         0.0f);
                    hsvgMaterial.SetFloat("_Main2ndTexAlphaMode",         0.0f);
                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Shadow2ndColorTex"));
                }
                else if(shadowType == 3)
                {
                    var shadow3rdColor = dics.GetColor("_Shadow3rdColor");
                    hsvgMaterial.SetColor("_MainColor3rd",                new Color(shadow3rdColor.r, shadow3rdColor.g, shadow3rdColor.b, shadow3rdColor.a * shadowStrength));
                    hsvgMaterial.SetFloat("_Main3rdTexBlendMode",         0.0f);
                    hsvgMaterial.SetFloat("_Main3rdTexAlphaMode",         0.0f);
                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_Shadow3rdColorTex"));
                }
                else
                {
                    hsvgMaterial.SetColor("_MainColor2nd",                new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowStrength));
                    hsvgMaterial.SetFloat("_Main2ndTexBlendMode",         0.0f);
                    hsvgMaterial.SetFloat("_Main2ndTexAlphaMode",         0.0f);
                    path = AssetDatabase.GetAssetPath(dics.GetTexture("_ShadowColorTex"));
                }

                bool existsShadowTex = !string.IsNullOrEmpty(path);
                if(existsShadowTex)
                {
                    lilTextureUtils.LoadTexture(ref srcMain2, path);
                    hsvgMaterial.SetTexture("_Main2ndTex", srcMain2);
                }

                path = AssetDatabase.GetAssetPath(bakedMainTex);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                    hsvgMaterial.SetTexture("_Main3rdTex", srcTexture);
                    if(!existsShadowTex) hsvgMaterial.SetTexture("_Main2ndTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                    hsvgMaterial.SetTexture("_Main3rdTex", Texture2D.whiteTexture);
                    if(!existsShadowTex) hsvgMaterial.SetTexture("_Main2ndTex", Texture2D.whiteTexture);
                }

                path = AssetDatabase.GetAssetPath(shadowStrengthMask);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcMask2, path);
                    hsvgMaterial.SetTexture("_Main2ndBlendMask", srcMask2);
                    hsvgMaterial.SetTexture("_Main3rdBlendMask", srcMask2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main2ndBlendMask", Texture2D.whiteTexture);
                    hsvgMaterial.SetTexture("_Main3rdBlendMask", Texture2D.whiteTexture);
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                if(shadowType == 0) outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex");
                if(shadowType == 1) outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex", "_shadow_1st");
                if(shadowType == 2) outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex", "_shadow_2nd");
                if(outTexture != mainTex)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                Object.DestroyImmediate(hsvgMaterial);
                Object.DestroyImmediate(srcTexture);
                Object.DestroyImmediate(srcMain2);
                Object.DestroyImmediate(srcMask2);

                return outTexture;
            }
            else
            {
                return (Texture2D)mainTex;
            }
        }

        public static Texture AutoBakeMatCap(Material material)
        {
            var dics = GetProps(material);
            var matcapColor = dics.GetColor("_MatCapColor");
            var matcapTex = dics.GetTexture("_MatCapTex");
            bool shouldNotBakeAll = matcapColor == Color.white;
            if(!shouldNotBakeAll && EditorUtility.DisplayDialog(S("sDialogRunBake"), S("sDialogBakeMatCap"), S("sYes"), S("sNo")))
            {
                // run bake
                var bufMainTexture = matcapTex as Texture2D;
                var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

                string path;

                var srcTexture = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color", matcapColor);
                hsvgMaterial.SetVector("_MainTexHSVG", lilConstants.defaultHSVG);

                path = AssetDatabase.GetAssetPath(bufMainTexture);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MatCapTex");
                if(outTexture != bufMainTexture)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                Object.DestroyImmediate(hsvgMaterial);
                Object.DestroyImmediate(srcTexture);

                return outTexture;
            }
            else
            {
                return matcapTex;
            }
        }

        public static Texture AutoBakeTriMask(Material material)
        {
            var dics = GetProps(material);
            var matcapBlendMask = dics.GetTexture("_MatCapBlendMask") as Texture2D;
            var rimColorTex = dics.GetTexture("_RimColorTex") as Texture2D;
            var emissionBlendMask = dics.GetTexture("_EmissionBlendMask") as Texture2D;
            var mainTex = dics.GetTexture("_MainTex");
            bool shouldNotBakeAll = matcapBlendMask == null && rimColorTex == null && emissionBlendMask == null;
            if(!shouldNotBakeAll && EditorUtility.DisplayDialog(S("sDialogRunBake"), S("sDialogBakeTriMask"), S("sYes"), S("sNo")))
            {
                // run bake
                var bufMainTexture = mainTex as Texture2D;
                var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

                string path;

                var srcTexture = new Texture2D(2, 2);
                var srcMain2 = new Texture2D(2, 2);
                var srcMain3 = new Texture2D(2, 2);

                hsvgMaterial.EnableKeyword("_TRIMASK");

                path = AssetDatabase.GetAssetPath(matcapBlendMask);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                path = AssetDatabase.GetAssetPath(rimColorTex);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcMain2, path);
                    hsvgMaterial.SetTexture("_Main2ndTex", srcMain2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main2ndTex", Texture2D.whiteTexture);
                }

                path = AssetDatabase.GetAssetPath(emissionBlendMask);
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcMain3, path);
                    hsvgMaterial.SetTexture("_Main3rdTex", srcMain3);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main3rdTex", Texture2D.whiteTexture);
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial, bufMainTexture);

                outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex");
                if(outTexture != mainTex && mainTex != null)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                Object.DestroyImmediate(hsvgMaterial);
                Object.DestroyImmediate(srcTexture);

                return outTexture;
            }
            else
            {
                return null;
            }
        }

        public static Texture AutoBakeAlphaMask(Material material)
        {
            var dics = GetProps(material);
            var mainTex = dics.GetTexture("_MainTex");
            // run bake
            var bufMainTexture = mainTex as Texture2D;
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            string path;

            var srcTexture = new Texture2D(2, 2);
            var srcAlphaMask = new Texture2D(2, 2);

            hsvgMaterial.EnableKeyword("_ALPHAMASK");
            hsvgMaterial.SetColor("_Color",           Color.white);
            hsvgMaterial.SetVector("_MainTexHSVG",        lilConstants.defaultHSVG);
            hsvgMaterial.CopyFloat(dics, "_AlphaMaskMode");
            hsvgMaterial.CopyFloat(dics, "_AlphaMaskScale");
            hsvgMaterial.CopyFloat(dics, "_AlphaMaskValue");

            path = AssetDatabase.GetAssetPath(bufMainTexture);
            if(!string.IsNullOrEmpty(path))
            {
                lilTextureUtils.LoadTexture(ref srcTexture, path);
                hsvgMaterial.SetTexture("_MainTex", srcTexture);
            }
            else
            {
                hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            path = AssetDatabase.GetAssetPath(dics.GetTexture("_AlphaMask"));
            if(!string.IsNullOrEmpty(path))
            {
                lilTextureUtils.LoadTexture(ref srcAlphaMask, path);
                hsvgMaterial.SetTexture("_AlphaMask", srcAlphaMask);
            }
            else
            {
                return (Texture2D)mainTex;
            }

            Texture2D outTexture = null;
            RunBake(ref outTexture, srcTexture, hsvgMaterial);

            outTexture = lilTextureUtils.SaveTextureToPng(outTexture, bufMainTexture);
            if(outTexture != bufMainTexture)
            {
                CopyTextureSetting(bufMainTexture, outTexture);
                string savePath = AssetDatabase.GetAssetPath(outTexture);
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(savePath);
                textureImporter.alphaIsTransparency = true;
                AssetDatabase.ImportAsset(savePath);
            }

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);

            return outTexture;
        }

        public static Texture AutoBakeOutlineTexture(Material material)
        {
            var dics = GetProps(material);
            var outlineTexHSVG = dics.GetVector("_OutlineTexHSVG");
            var outlineTex = dics.GetTexture("_OutlineTex");
            var mainTex = dics.GetTexture("_MainTex");
            bool shouldNotBakeOutline = outlineTex == null || outlineTexHSVG == lilConstants.defaultHSVG;
            if(!shouldNotBakeOutline && EditorUtility.DisplayDialog(S("sDialogRunBake"), S("sDialogBakeOutline"), S("sYes"), S("sNo")))
            {
                // run bake
                var bufMainTexture = outlineTex as Texture2D;
                var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

                string path;

                var srcTexture = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color", Color.white);
                hsvgMaterial.SetVector("_MainTexHSVG", outlineTexHSVG);

                path = AssetDatabase.GetAssetPath(dics.GetTexture("_OutlineTex"));
                if(!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                outTexture = lilTextureUtils.SaveTextureToPng(material, outTexture, "_MainTex");
                if(outTexture != mainTex)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                Object.DestroyImmediate(hsvgMaterial);
                Object.DestroyImmediate(srcTexture);

                return outTexture;
            }
            else
            {
                return outlineTex;
            }
        }

        public static void AutoBakeColoredMask(Material material, Texture masktex, Color maskcolor, string propName)
        {
            var dics = GetProps(material);
            var mainTex = dics.GetTexture("_MainTex");
            if(propName.Contains("Shadow"))
            {
                int shadowType = propName.Contains("2nd") ? 2 : 1;
                shadowType = propName.Contains("3rd") ? 3 : shadowType;
                AutoBakeShadowTexture(material, mainTex, shadowType, false);
                return;
            }

            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);
            hsvgMaterial.SetColor("_Color", maskcolor);

            var bufMainTexture = Texture2D.whiteTexture;
            if(masktex != null && masktex is Texture2D) bufMainTexture = (Texture2D)masktex;
            string path = "";

            var srcTexture = new Texture2D(2, 2);

            if(masktex != null) path = AssetDatabase.GetAssetPath(bufMainTexture);
            if(!string.IsNullOrEmpty(path))
            {
                lilTextureUtils.LoadTexture(ref srcTexture, path);
                hsvgMaterial.SetTexture("_MainTex", srcTexture);
            }
            else
            {
                hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            Texture2D outTexture = null;
            RunBake(ref outTexture, srcTexture, hsvgMaterial);

            if(!string.IsNullOrEmpty(path)) path = Path.GetDirectoryName(path) + "/" + material.name + "_" + propName;
            else                            path = "Assets/" + material.name + "_" + propName;
            outTexture = lilTextureUtils.SaveTextureToPng(outTexture, path);
            if(outTexture != bufMainTexture)
            {
                CopyTextureSetting(bufMainTexture, outTexture);
            }

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);
        }

        public static void RunBake(ref Texture2D outTexture, Texture2D srcTexture, Material material, Texture2D referenceTexture = null)
        {
            int width = 4096;
            int height = 4096;
            if(referenceTexture != null)
            {
                width = referenceTexture.width;
                height = referenceTexture.height;
            }
            else if(srcTexture != null)
            {
                width = srcTexture.width;
                height = srcTexture.height;
            }
            outTexture = new Texture2D(width, height);

            var bufRT = RenderTexture.active;
            var dstTexture = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(srcTexture, dstTexture, material);
            RenderTexture.active = dstTexture;
            outTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            outTexture.Apply();
            RenderTexture.active = bufRT;
            RenderTexture.ReleaseTemporary(dstTexture);
        }

        public static void CreateMToonMaterial(Material material, string matPath = null)
        {
            var mtoonMaterial = new Material(lilShaderManager.mtoon);
            if(!string.IsNullOrEmpty(matPath))
            {
                AssetDatabase.CreateAsset(mtoonMaterial, matPath);
            }
            else
            {
                matPath = AssetDatabase.GetAssetPath(material);
                if(!string.IsNullOrEmpty(matPath))  matPath = EditorUtility.SaveFilePanel("Save Material", Path.GetDirectoryName(matPath), Path.GetFileNameWithoutExtension(matPath)+"_MToon", "mat");
                else                                matPath = EditorUtility.SaveFilePanel("Save Material", "Assets", material.name + ".mat", "mat");
                if(!string.IsNullOrEmpty(matPath))  AssetDatabase.CreateAsset(mtoonMaterial, FileUtil.GetProjectRelativePath(matPath));
                else                                return;
            }

            var t = new lilToonInspector.MaterialType(material, false);
            var so = new SerializedObject(material);
            so.Update();
            var props = so.FindProperty("m_SavedProperties");
            var texs = GetProps(props.FindPropertyRelative("m_TexEnvs"));
            var floats = GetProps(props.FindPropertyRelative("m_Floats"));
            var colors = GetProps(props.FindPropertyRelative("m_Colors"));

            mtoonMaterial.SetColor("_Color",                    colors["_Color"].colorValue.Clamp());
            mtoonMaterial.SetFloat("_LightColorAttenuation",    0.0f);
            mtoonMaterial.SetFloat("_IndirectLightIntensity",   0.0f);

            mtoonMaterial.SetFloat("_UvAnimScrollX",            colors["_MainTex_ScrollRotate"].colorValue.r);
            mtoonMaterial.SetFloat("_UvAnimScrollY",            colors["_MainTex_ScrollRotate"].colorValue.g);
            mtoonMaterial.SetFloat("_UvAnimRotation",           colors["_MainTex_ScrollRotate"].colorValue.a / Mathf.PI * 0.5f);
            mtoonMaterial.SetFloat("_MToonVersion",             35.0f);
            mtoonMaterial.SetFloat("_DebugMode",                0.0f);
            mtoonMaterial.SetFloat("_CullMode",                 floats["_Cull"].floatValue);

            var bakedMainTex = AutoBakeMainTexture(material);
            mtoonMaterial.SetTexture("_MainTex", bakedMainTex);

            mtoonMaterial.SetTextureScale("_MainTex", texs["_MainTex"].FindPropertyRelative("m_Scale").vector2Value);
            mtoonMaterial.SetTextureOffset("_MainTex", texs["_MainTex"].FindPropertyRelative("m_Offset").vector2Value);
            var bumpMap = texs["_BumpMap"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;

            if(floats["_UseBumpMap"].floatValue == 1.0f && bumpMap != null)
            {
                mtoonMaterial.SetFloat("_BumpScale", floats["_BumpScale"].floatValue);
                mtoonMaterial.SetTexture("_BumpMap", bumpMap);
                mtoonMaterial.EnableKeyword("_NORMALMAP");
            }

            if(floats["_UseShadow"].floatValue == 1.0f)
            {
                var shadowBorder = floats["_ShadowBorder"].floatValue;
                var shadowBlur = floats["_ShadowBlur"].floatValue;
                float shadeShift = (Mathf.Clamp01(shadowBorder - (shadowBlur * 0.5f)) * 2.0f) - 1.0f;
                float shadeToony = (2.0f - (Mathf.Clamp01(shadowBorder + (shadowBlur * 0.5f)) * 2.0f)) / (1.0f - shadeShift);
                if(texs["_ShadowStrengthMask"].FindPropertyRelative("m_Texture").objectReferenceValue != null || floats["_ShadowMainStrength"].floatValue != 0.0f)
                {
                    var bakedShadowTex = AutoBakeShadowTexture(material, bakedMainTex);
                    mtoonMaterial.SetColor("_ShadeColor",               Color.white);
                    mtoonMaterial.SetTexture("_ShadeTexture",           bakedShadowTex);
                }
                else
                {
                    var shadowColor = colors["_ShadowColor"].colorValue;
                    var shadowStrength = floats["_ShadowStrength"].floatValue;
                    var shadeColorStrength = new Color(
                        1.0f - ((1.0f - shadowColor.r) * shadowStrength),
                        1.0f - ((1.0f - shadowColor.g) * shadowStrength),
                        1.0f - ((1.0f - shadowColor.b) * shadowStrength),
                        1.0f
                    );
                    mtoonMaterial.SetColor("_ShadeColor",               shadeColorStrength);
                    var shadowColorTex = texs["_ShadowColorTex"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;
                    if(shadowColorTex != null)
                    {
                        mtoonMaterial.SetTexture("_ShadeTexture",           shadowColorTex);
                    }
                    else
                    {
                        mtoonMaterial.SetTexture("_ShadeTexture",           bakedMainTex);
                    }
                }
                mtoonMaterial.SetFloat("_ReceiveShadowRate",        1.0f);
                mtoonMaterial.SetTexture("_ReceiveShadowTexture",   null);
                mtoonMaterial.SetFloat("_ShadingGradeRate",         1.0f);
                mtoonMaterial.SetTexture("_ShadingGradeTexture",    texs["_ShadowBorderTex"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D);
                mtoonMaterial.SetFloat("_ShadeShift",               shadeShift);
                mtoonMaterial.SetFloat("_ShadeToony",               shadeToony);
            }
            else
            {
                mtoonMaterial.SetColor("_ShadeColor",               Color.white);
                mtoonMaterial.SetTexture("_ShadeTexture",           bakedMainTex);
            }

            var emissionMap = texs["_EmissionMap"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;
            if(floats["_UseEmission"].floatValue == 1.0f && emissionMap != null)
            {
                mtoonMaterial.SetColor("_EmissionColor",            colors["_EmissionColor"].colorValue);
                mtoonMaterial.SetTexture("_EmissionMap",            emissionMap);
            }

            if(floats["_UseRim"].floatValue == 1.0f)
            {
                mtoonMaterial.SetColor("_RimColor",                 floats["_RimColor"].colorValue);
                mtoonMaterial.SetTexture("_RimTexture",             texs["_RimColorTex"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D);
                mtoonMaterial.SetFloat("_RimLightingMix",           floats["_RimEnableLighting"].floatValue);

                var rimBorder = floats["_RimBorder"].floatValue;
                var rimBlur = floats["_RimBlur"].floatValue;
                var rimFresnelPower = floats["_RimFresnelPower"].floatValue;
                float rimFP = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
                float rimLift = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);
                mtoonMaterial.SetFloat("_RimFresnelPower",          rimFP);
                mtoonMaterial.SetFloat("_RimLift",                  rimLift);
            }
            else
            {
                mtoonMaterial.SetColor("_RimColor",                 Color.black);
            }

            var matcapTex = texs["_MatCapTex"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;
            if(floats["_UseMatCap"].floatValue == 1.0f && floats["_MatCapBlendMode"].floatValue != 3.0f && matcapTex != null)
            {
                var bakedMatCap = AutoBakeMatCap(material);
                mtoonMaterial.SetTexture("_SphereAdd", bakedMatCap);
            }

            if(t.isOutl)
            {
                mtoonMaterial.SetTexture("_OutlineWidthTexture",    texs["_OutlineWidthMask"].FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D);
                mtoonMaterial.SetFloat("_OutlineWidth",             floats["_OutlineWidth"].floatValue);
                mtoonMaterial.SetColor("_OutlineColor",             colors["_OutlineColor"].colorValue);
                mtoonMaterial.SetFloat("_OutlineLightingMix",       1.0f);
                mtoonMaterial.SetFloat("_OutlineWidthMode",         1.0f);
                mtoonMaterial.SetFloat("_OutlineColorMode",         1.0f);
                mtoonMaterial.SetFloat("_OutlineCullMode",          1.0f);
                mtoonMaterial.EnableKeyword("MTOON_OUTLINE_WIDTH_WORLD");
                mtoonMaterial.EnableKeyword("MTOON_OUTLINE_COLOR_MIXED");
            }

            var zwrite = floats["_ZWrite"].floatValue;
            if(t.isCutout)
            {
                mtoonMaterial.SetFloat("_Cutoff", floats["_Cutoff"].floatValue);
                mtoonMaterial.SetFloat("_BlendMode", 1.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                mtoonMaterial.SetFloat("_ZWrite", 1.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 1.0f);
                mtoonMaterial.EnableKeyword("_ALPHATEST_ON");
                mtoonMaterial.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else if(t.isTransparent && zwrite == 0.0f)
            {
                mtoonMaterial.SetFloat("_BlendMode", 2.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mtoonMaterial.SetFloat("_ZWrite", 0.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 0.0f);
                mtoonMaterial.EnableKeyword("_ALPHABLEND_ON");
                mtoonMaterial.renderQueue = (int)RenderQueue.Transparent;
            }
            else if(t.isTransparent && zwrite != 0.0f)
            {
                mtoonMaterial.SetFloat("_BlendMode", 3.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mtoonMaterial.SetFloat("_ZWrite", 1.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 0.0f);
                mtoonMaterial.EnableKeyword("_ALPHABLEND_ON");
                mtoonMaterial.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mtoonMaterial.SetFloat("_BlendMode", 0.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "Opaque");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                mtoonMaterial.SetFloat("_ZWrite", 1.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 0.0f);
                mtoonMaterial.renderQueue = -1;
            }
        }

        public static void CreateLiteMaterial(Material material, string matPath = null)
        {
            var liteMaterial = new Material(lilShaderManager.ltsl);
            if(!string.IsNullOrEmpty(matPath))
            {
                AssetDatabase.CreateAsset(liteMaterial, matPath);
            }
            else
            {
                matPath = AssetDatabase.GetAssetPath(material);
                if(!string.IsNullOrEmpty(matPath))  matPath = EditorUtility.SaveFilePanel("Save Material", Path.GetDirectoryName(matPath), Path.GetFileNameWithoutExtension(matPath)+"_Lite", "mat");
                else                                matPath = EditorUtility.SaveFilePanel("Save Material", "Assets", material.name + ".mat", "mat");
                if(!string.IsNullOrEmpty(matPath))  AssetDatabase.CreateAsset(liteMaterial, FileUtil.GetProjectRelativePath(matPath));
                else                                return;
            }
            var dics = GetProps(material);
            var t = new lilToonInspector.MaterialType(material, false);

            var so = new SerializedObject(liteMaterial);
            so.Update();
            var dics2 = GetProps(so);

            dics2.CopyFloat(dics, "_Invisible");
            dics2.CopyFloat(dics, "_Cutoff");
            dics2.CopyFloat(dics, "_SubpassCutoff");
            dics2.CopyFloat(dics, "_Cull");
            dics2.CopyFloat(dics, "_FlipNormal");
            dics2.CopyFloat(dics, "_BackfaceForceShadow");

            dics2.CopyColor(dics, "_Color");
            dics2.CopyColor(dics, "_MainTex_ScrollRotate");

            var bakedMainTex = AutoBakeMainTexture(material);
            dics2.SetTexture("_MainTex", bakedMainTex);

            dics2.CopyTextureScale(dics, "_MainTex");
            dics2.CopyTextureOffset(dics, "_MainTex");

            dics2.CopyFloat(dics, "_UseShadow");
            if(dics.GetFloat("_UseShadow") == 1.0f)
            {
                var bakedShadowTex = AutoBakeShadowTexture(material, bakedMainTex, 1, false);
                dics2.CopyFloat(dics, "_ShadowBorder");
                dics2.CopyFloat(dics, "_ShadowBlur");
                dics2.SetTexture("_ShadowColorTex", bakedShadowTex);
                if(dics.GetColor("_Shadow2ndColor").a != 0.0f)
                {
                    var bakedShadow2ndTex = AutoBakeShadowTexture(material, bakedMainTex, 2, false);
                    dics2.CopyFloat(dics, "_Shadow2ndBorder");
                    dics2.CopyFloat(dics, "_Shadow2ndBlur");
                    dics2.SetTexture("_Shadow2ndColorTex", bakedShadow2ndTex);
                }
                dics2.CopyFloat(dics, "_ShadowEnvStrength");
                dics2.CopyColor(dics, "_ShadowBorderColor");
                dics2.CopyFloat(dics, "_ShadowBorderRange");
            }

            if(t.isOutl)
            {
                var bakedOutlineTex = AutoBakeOutlineTexture(material);
                dics2.CopyColor(dics, "_OutlineColor");
                dics2.SetTexture("_OutlineTex", bakedOutlineTex);
                dics2.CopyColor(dics, "_OutlineTex_ScrollRotate");
                dics2.CopyTexture(dics, "_OutlineWidthMask");
                dics2.CopyFloat(dics, "_OutlineWidth");
                dics2.CopyFloat(dics, "_OutlineFixWidth");
                dics2.CopyFloat(dics, "_OutlineVertexR2Width");
                dics2.CopyFloat(dics, "_OutlineDeleteMesh");
                dics2.CopyFloat(dics, "_OutlineEnableLighting");
                dics2.CopyFloat(dics, "_OutlineZBias");
                dics2.CopyFloat(dics, "_OutlineSrcBlend");
                dics2.CopyFloat(dics, "_OutlineDstBlend");
                dics2.CopyFloat(dics, "_OutlineBlendOp");
                dics2.CopyFloat(dics, "_OutlineSrcBlendFA");
                dics2.CopyFloat(dics, "_OutlineDstBlendFA");
                dics2.CopyFloat(dics, "_OutlineBlendOpFA");
                dics2.CopyFloat(dics, "_OutlineZWrite");
                dics2.CopyFloat(dics, "_OutlineZTest");
                dics2.CopyFloat(dics, "_OutlineAlphaToMask");
                dics2.CopyFloat(dics, "_OutlineStencilRef");
                dics2.CopyFloat(dics, "_OutlineStencilReadMask");
                dics2.CopyFloat(dics, "_OutlineStencilWriteMask");
                dics2.CopyFloat(dics, "_OutlineStencilComp");
                dics2.CopyFloat(dics, "_OutlineStencilPass");
                dics2.CopyFloat(dics, "_OutlineStencilFail");
                dics2.CopyFloat(dics, "_OutlineStencilZFail");
            }

            var bakedMatCap = AutoBakeMatCap(material);
            if(bakedMatCap != null)
            {
                dics2.SetTexture("_MatCapTex", bakedMatCap);
                dics2.CopyFloat(dics, "_UseMatCap");
                dics2.CopyFloat(dics, "_MatCapBlendUV1");
                dics2.CopyFloat(dics, "_MatCapZRotCancel");
                dics2.CopyFloat(dics, "_MatCapPerspective");
                dics2.CopyFloat(dics, "_MatCapVRParallaxStrength");
                if(dics.GetFloat("MatCapBlendMode") == 3) dics2.SetFloat("_MatCapMul", 1);
                else                                      dics2.SetFloat("_MatCapMul", 0);
            }

            dics2.CopyFloat(dics, "_UseRim");
            if(dics.GetFloat("_UseRim") == 1.0f)
            {
                dics2.CopyColor(dics, "_RimColor");
                dics2.CopyFloat(dics, "_RimBorder");
                dics2.CopyFloat(dics, "_RimBlur");
                dics2.CopyFloat(dics, "_RimFresnelPower");
                dics2.CopyFloat(dics, "_RimShadowMask");
            }

            if(dics.GetFloat("_UseEmission") == 1.0f)
            {
                dics2.CopyFloat(dics, "_UseEmission");
                dics2.CopyColor(dics, "_EmissionColor");
                dics2.CopyTexture(dics, "_EmissionMap");
                dics2.CopyFloat(dics, "_EmissionMap_UVMode");
                dics2.CopyColor(dics, "_EmissionMap_ScrollRotate");
                dics2.CopyColor(dics, "_EmissionBlink");
            }

            var bakedTriMask = AutoBakeTriMask(material);
            if(bakedTriMask != null) dics2.SetTexture("_TriMask", bakedTriMask);

            dics2.CopyFloat(dics, "_SrcBlend");
            dics2.CopyFloat(dics, "_DstBlend");
            dics2.CopyFloat(dics, "_BlendOp");
            dics2.CopyFloat(dics, "_SrcBlendFA");
            dics2.CopyFloat(dics, "_DstBlendFA");
            dics2.CopyFloat(dics, "_BlendOpFA");
            dics2.CopyFloat(dics, "_ZClip");
            dics2.CopyFloat(dics, "_ZWrite");
            dics2.CopyFloat(dics, "_ZTest");
            dics2.CopyFloat(dics, "_AlphaToMask");
            dics2.CopyFloat(dics, "_StencilRef");
            dics2.CopyFloat(dics, "_StencilReadMask");
            dics2.CopyFloat(dics, "_StencilWriteMask");
            dics2.CopyFloat(dics, "_StencilComp");
            dics2.CopyFloat(dics, "_StencilPass");
            dics2.CopyFloat(dics, "_StencilFail");
            dics2.CopyFloat(dics, "_StencilZFail");
            so.FindProperty("m_CustomRenderQueue").intValue = lilMaterialUtils.GetTrueRenderQueue(material);

            so.ApplyModifiedProperties();

            var renderingMode = t.renderingModeBuf;
            if(renderingMode == RenderingMode.Refraction)       renderingMode = RenderingMode.Transparent;
            if(renderingMode == RenderingMode.RefractionBlur)   renderingMode = RenderingMode.Transparent;
            if(renderingMode == RenderingMode.Fur)              renderingMode = RenderingMode.Transparent;
            if(renderingMode == RenderingMode.FurCutout)        renderingMode = RenderingMode.Cutout;
            if(renderingMode == RenderingMode.FurTwoPass)       renderingMode = RenderingMode.Transparent;

            bool isonepass      = material.shader.name.Contains("OnePass");
            bool istwopass      = material.shader.name.Contains("TwoPass");

            var           transparentMode = TransparentMode.Normal;
            if(isonepass) transparentMode = TransparentMode.OnePass;
            if(istwopass) transparentMode = TransparentMode.TwoPass;

            SetupMaterialWithRenderingMode(so, dics2, renderingMode, transparentMode, t.isOutl, true, t.isStWr, false);
            so.ApplyModifiedProperties();
        }

        // TODO : Support other rendering mode
        public static void CreateMultiMaterial(Material material, bool useClippingCanceller)
        {
            var t = new lilToonInspector.MaterialType(material, false);
            material.shader = lilShaderManager.ltsm;
            if(t.isOutl)  material.shader = lilShaderManager.ltsmo;
            else        material.shader = lilShaderManager.ltsm;
            t.isMulti = true;

            if(t.renderingModeBuf == RenderingMode.Cutout)            material.SetFloat("_TransparentMode", 1.0f);
            else if(t.renderingModeBuf == RenderingMode.Transparent)  material.SetFloat("_TransparentMode", 2.0f);
            else                                                      material.SetFloat("_TransparentMode", 0.0f);
            material.SetFloat("_UseClippingCanceller", useClippingCanceller ? 1.0f : 0.0f);

            lilMaterialUtils.SetupMaterialWithRenderingMode(material, t.renderingModeBuf, TransparentMode.Normal, t.isOutl, false, t.isStWr, true);
            lilMaterialUtils.SetupMultiMaterial(material);
        }

        private static void CopyTextureSetting(Texture2D fromTexture, Texture2D toTexture)
        {
            if(fromTexture == null || toTexture == null) return;
            string fromPath = AssetDatabase.GetAssetPath(fromTexture);
            string toPath = AssetDatabase.GetAssetPath(toTexture);
            var fromTextureImporter = (TextureImporter)AssetImporter.GetAtPath(fromPath);
            var toTextureImporter = (TextureImporter)AssetImporter.GetAtPath(toPath);
            if(fromTextureImporter == null || toTextureImporter == null) return;

            var fromTextureImporterSettings = new TextureImporterSettings();
            fromTextureImporter.ReadTextureSettings(fromTextureImporterSettings);
            toTextureImporter.SetTextureSettings(fromTextureImporterSettings);
            toTextureImporter.SetPlatformTextureSettings(fromTextureImporter.GetDefaultPlatformTextureSettings());
            AssetDatabase.ImportAsset(toPath);
        }

        private static void DoAllElements(this SerializedProperty property, Action<SerializedProperty,int> function)
        {
            if(property.arraySize == 0) return;
            int i = 0;
            var prop = property.GetArrayElementAtIndex(0);
            var end = property.GetEndProperty();
            function.Invoke(prop,i);
            while(prop.NextVisible(false) && !SerializedProperty.EqualContents(prop, end))
            {
                i++;
                function.Invoke(prop,i);
            }
        }

        // Get
        private static float GetFloat(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            if(dics.fs.ContainsKey(key)) return dics.fs[key].floatValue;
            else return 0f;
        }

        private static Color GetColor(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            if(dics.cs.ContainsKey(key)) return dics.cs[key].colorValue;
            else return Color.black;
        }

        private static Vector4 GetVector(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            return dics.GetColor(key);
        }

        private static Texture GetTexture(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            if(dics.ts.ContainsKey(key)) return dics.ts[key].FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            else return null;
        }

        private static Vector2 GetTextureScale(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            if(dics.ts.ContainsKey(key)) return dics.ts[key].FindPropertyRelative("m_Scale").vector2Value;
            else return Vector2.one;
        }

        private static Vector2 GetTextureOffset(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            if(dics.ts.ContainsKey(key)) return dics.ts[key].FindPropertyRelative("m_Offset").vector2Value;
            else return Vector2.zero;
        }

        // Set
        private static void SetFloat(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key, float value)
        {
            if(dics.fs.ContainsKey(key)) dics.fs[key].floatValue = value;
        }

        private static void SetColor(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key, Color value)
        {
            if(dics.cs.ContainsKey(key)) dics.cs[key].colorValue = value;
        }

        private static void SetVector(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key, Vector4 value)
        {
            dics.SetColor(key, value);
        }

        private static void SetTexture(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key, Texture value)
        {
            if(dics.ts.ContainsKey(key)) dics.ts[key].FindPropertyRelative("m_Texture").objectReferenceValue = value;
        }

        private static void SetTextureScale(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key, Vector2 value)
        {
            if(dics.ts.ContainsKey(key)) dics.ts[key].FindPropertyRelative("m_Scale").vector2Value = value;
        }

        private static void SetTextureOffset(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key, Vector2 value)
        {
            if(dics.ts.ContainsKey(key)) dics.ts[key].FindPropertyRelative("m_Offset").vector2Value = value;
        }

        // Copy To Material
        private static void CopyFloat(this Material material, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            material.SetFloat(key, dics.GetFloat(key));
        }

        private static void CopyColor(this Material material, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            material.SetColor(key, dics.GetColor(key));
        }

        private static void CopyTexture(this Material material, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            material.SetTexture(key, dics.GetTexture(key));
        }

        private static void CopyTextureScale(this Material material, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            material.SetTextureScale(key, dics.GetTextureScale(key));
        }

        private static void CopyTextureOffset(this Material material, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            material.SetTextureOffset(key, dics.GetTextureOffset(key));
        }

        // Copy To Dictionary
        private static void CopyFloat(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics2, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            dics2.SetFloat(key, dics.GetFloat(key));
        }

        private static void CopyColor(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics2, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            dics2.SetColor(key, dics.GetColor(key));
        }

        private static void CopyTexture(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics2, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            dics2.SetTexture(key, dics.GetTexture(key));
        }

        private static void CopyTextureScale(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics2, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            dics2.SetTextureScale(key, dics.GetTextureScale(key));
        }

        private static void CopyTextureOffset(this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics2, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, string key)
        {
            dics2.SetTextureOffset(key, dics.GetTextureOffset(key));
        }

        private static (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) GetProps(Material material)
        {
            var so = new SerializedObject(material);
            so.Update();
            var props = so.FindProperty("m_SavedProperties");
            return (GetProps(props.FindPropertyRelative("m_TexEnvs")),
                GetProps(props.FindPropertyRelative("m_Floats")),
                GetProps(props.FindPropertyRelative("m_Colors")));
        }

        private static (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) GetProps(SerializedObject so)
        {
            var props = so.FindProperty("m_SavedProperties");
            return (GetProps(props.FindPropertyRelative("m_TexEnvs")),
                GetProps(props.FindPropertyRelative("m_Floats")),
                GetProps(props.FindPropertyRelative("m_Colors")));
        }

        private static Dictionary<string, SerializedProperty> GetProps(SerializedProperty props)
        {
            var dic = new Dictionary<string, SerializedProperty>();
            props.DoAllElements((p,i) => {
                dic[p.FindPropertyRelative("first").stringValue] = p.FindPropertyRelative("second");
            });
            return dic;
        }

        private static Color Clamp(this Color color)
        {
            return new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), Mathf.Clamp01(color.a));
        }

        private static string S(string value)
        {
            return Localization.S(value);
        }

        private static void SetupMaterialWithRenderingMode(SerializedObject so, (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs, Dictionary<string, SerializedProperty> cs) dics, RenderingMode renderingMode, TransparentMode transparentMode, bool isoutl, bool islite, bool istess, bool ismulti)
        {
            var m_CustomRenderQueue = so.FindProperty("m_CustomRenderQueue");
            var m_Shader = so.FindProperty("m_Shader");
            int renderQueue = m_CustomRenderQueue.intValue;
            RenderingMode rend = renderingMode;
            lilRenderPipeline RP = lilRenderPipelineReader.GetRP();
            if(ismulti)
            {
                float tpmode = dics.GetFloat("_TransparentMode");
                switch((int)tpmode)
                {
                    case 1  : rend = RenderingMode.Cutout; break;
                    case 2  : rend = RenderingMode.Transparent; break;
                    case 3  : rend = RenderingMode.Refraction; break;
                    case 4  : rend = RenderingMode.Fur; break;
                    case 5  : rend = RenderingMode.FurCutout; break;
                    case 6  : rend = RenderingMode.Gem; break;
                    default : rend = RenderingMode.Opaque; break;
                }
            }

            switch(rend)
            {
                case RenderingMode.Opaque:
                    if(islite)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltslo;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltsl;
                    }
                    else if(ismulti)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsmo;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltsm;
                        so.SetOverrideTag("RenderType", "");
                        m_CustomRenderQueue.intValue = -1;
                    }
                    else if(istess)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltstesso;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltstess;
                    }
                    else
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltso;
                        else        m_Shader.objectReferenceValue = lilShaderManager.lts;
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    dics.SetFloat("_AlphaToMask", 0);
                    if(isoutl)
                    {
                        dics.SetFloat("_OutlineSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        dics.SetFloat("_OutlineDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        dics.SetFloat("_OutlineAlphaToMask", 0);
                    }
                    break;
                case RenderingMode.Cutout:
                    if(islite)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltslco;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltslc;
                    }
                    else if(ismulti)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsmo;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltsm;
                        so.SetOverrideTag("RenderType", "TransparentCutout");
                        m_CustomRenderQueue.intValue = 2450;
                    }
                    else if(istess)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltstessco;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltstessc;
                    }
                    else
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsco;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltsc;
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    dics.SetFloat("_AlphaToMask", 1);
                    if(isoutl)
                    {
                        dics.SetFloat("_OutlineSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        dics.SetFloat("_OutlineDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        dics.SetFloat("_OutlineAlphaToMask", 1);
                    }
                    break;
                case RenderingMode.Transparent:
                    if(ismulti)
                    {
                        if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsmo;
                        else        m_Shader.objectReferenceValue = lilShaderManager.ltsm;
                        so.SetOverrideTag("RenderType", "TransparentCutout");
                        m_CustomRenderQueue.intValue = RP == lilRenderPipeline.HDRP ? 3000 : 2460;
                    }
                    else
                    {
                        switch (transparentMode)
                        {
                            case TransparentMode.Normal:
                                if(islite)
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltslto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltslt;
                                }
                                else if(istess)
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltstessto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltstesst;
                                }
                                else
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltst;
                                }
                                break;
                            case TransparentMode.OnePass:
                                if(islite)
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsloto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltslot;
                                }
                                else if(istess)
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltstessoto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltstessot;
                                }
                                else
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsoto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltsot;
                                }
                                break;
                            case TransparentMode.TwoPass:
                                if(islite)
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltsltto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltsltt;
                                }
                                else if(istess)
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltstesstto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltstesstt;
                                }
                                else
                                {
                                    if(isoutl)  m_Shader.objectReferenceValue = lilShaderManager.ltstto;
                                    else        m_Shader.objectReferenceValue = lilShaderManager.ltstt;
                                }
                                break;
                        }
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    dics.SetFloat("_AlphaToMask", 0);
                    if(isoutl)
                    {
                        dics.SetFloat("_OutlineSrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        dics.SetFloat("_OutlineDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        dics.SetFloat("_OutlineAlphaToMask", 0);
                    }
                    break;
                case RenderingMode.Refraction:
                    if(ismulti)
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsmref;
                        so.SetOverrideTag("RenderType", "");
                        m_CustomRenderQueue.intValue = -1;
                    }
                    else
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsref;
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    dics.SetFloat("_AlphaToMask", 0);
                    break;
                case RenderingMode.RefractionBlur:
                    m_Shader.objectReferenceValue = lilShaderManager.ltsrefb;
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    dics.SetFloat("_AlphaToMask", 0);
                    break;
                case RenderingMode.Fur:
                    if(ismulti)
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsmfur;
                        so.SetOverrideTag("RenderType", "TransparentCutout");
                        m_CustomRenderQueue.intValue = 3000;
                    }
                    else
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsfur;
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    dics.SetFloat("_AlphaToMask", 0);
                    dics.SetFloat("_FurSrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    dics.SetFloat("_FurDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    dics.SetFloat("_FurZWrite", 0);
                    dics.SetFloat("_FurAlphaToMask", 0);
                    break;
                case RenderingMode.FurCutout:
                    if(ismulti)
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsmfur;
                        so.SetOverrideTag("RenderType", "TransparentCutout");
                        m_CustomRenderQueue.intValue = 2450;
                    }
                    else
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsfurc;
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    dics.SetFloat("_AlphaToMask", 1);
                    dics.SetFloat("_FurSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_FurDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    dics.SetFloat("_FurZWrite", 1);
                    dics.SetFloat("_FurAlphaToMask", 1);
                    break;
                case RenderingMode.FurTwoPass:
                    m_Shader.objectReferenceValue = lilShaderManager.ltsfurtwo;
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    dics.SetFloat("_AlphaToMask", 0);
                    dics.SetFloat("_FurSrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    dics.SetFloat("_FurDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    dics.SetFloat("_FurZWrite", 0);
                    dics.SetFloat("_FurAlphaToMask", 0);
                    break;
                case RenderingMode.Gem:
                    if(ismulti)
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsmgem;
                        so.SetOverrideTag("RenderType", "");
                        m_CustomRenderQueue.intValue = -1;
                    }
                    else
                    {
                        m_Shader.objectReferenceValue = lilShaderManager.ltsgem;
                    }
                    dics.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    dics.SetFloat("_AlphaToMask", 0);
                    break;
            }
            if(!ismulti) m_CustomRenderQueue.intValue = renderQueue;
            lilMaterialUtils.FixTransparentRenderQueue(m_CustomRenderQueue, renderingMode);
            if(rend == RenderingMode.Gem)
            {
                dics.SetFloat("_Cull", 0);
                dics.SetFloat("_ZWrite", 0);
            }
            else
            {
                dics.SetFloat("_ZWrite", 1);
            }
            if(transparentMode != TransparentMode.TwoPass)
            {
                dics.SetFloat("_ZTest", 4);
            }
            dics.SetFloat("_OffsetFactor", 0.0f);
            dics.SetFloat("_OffsetUnits", 0.0f);
            dics.SetFloat("_ColorMask", 15);
            dics.SetFloat("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            dics.SetFloat("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            dics.SetFloat("_BlendOp", (int)BlendOp.Add);
            dics.SetFloat("_BlendOpAlpha", (int)BlendOp.Add);
            dics.SetFloat("_SrcBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
            dics.SetFloat("_DstBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
            dics.SetFloat("_SrcBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.Zero);
            dics.SetFloat("_DstBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.One);
            dics.SetFloat("_BlendOpFA", (int)BlendOp.Max);
            dics.SetFloat("_BlendOpAlphaFA", (int)BlendOp.Max);
            if(isoutl)
            {
                dics.SetFloat("_OutlineCull", 1);
                dics.SetFloat("_OutlineZWrite", 1);
                dics.SetFloat("_OutlineZTest", 2);
                dics.SetFloat("_OutlineOffsetFactor", 0.0f);
                dics.SetFloat("_OutlineOffsetUnits", 0.0f);
                dics.SetFloat("_OutlineColorMask", 15);
                dics.SetFloat("_OutlineSrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_OutlineDstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                dics.SetFloat("_OutlineBlendOp", (int)BlendOp.Add);
                dics.SetFloat("_OutlineBlendOpAlpha", (int)BlendOp.Add);
                dics.SetFloat("_OutlineSrcBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_OutlineDstBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_OutlineSrcBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.Zero);
                dics.SetFloat("_OutlineDstBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_OutlineBlendOpFA", (int)BlendOp.Max);
                dics.SetFloat("_OutlineBlendOpAlphaFA", (int)BlendOp.Max);
            }
            if(renderingMode == RenderingMode.Fur || renderingMode == RenderingMode.FurCutout || renderingMode == RenderingMode.FurTwoPass)
            {
                dics.SetFloat("_FurZTest", 4);
                dics.SetFloat("_FurOffsetFactor", 0.0f);
                dics.SetFloat("_FurOffsetUnits", 0.0f);
                dics.SetFloat("_FurColorMask", 15);
                dics.SetFloat("_FurSrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_FurDstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                dics.SetFloat("_FurBlendOp", (int)BlendOp.Add);
                dics.SetFloat("_FurBlendOpAlpha", (int)BlendOp.Add);
                dics.SetFloat("_FurSrcBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_FurDstBlendFA", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_FurSrcBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.Zero);
                dics.SetFloat("_FurDstBlendAlphaFA", (int)UnityEngine.Rendering.BlendMode.One);
                dics.SetFloat("_FurBlendOpFA", (int)BlendOp.Max);
                dics.SetFloat("_FurBlendOpAlphaFA", (int)BlendOp.Max);
            }
        }

        private static void SetOverrideTag(this SerializedObject so, string tag, string value)
        {
            var stringTagMap = so.FindProperty("stringTagMap");

            if(stringTagMap.arraySize != 0)
            {
                var prop = stringTagMap.GetArrayElementAtIndex(0);
                var end = stringTagMap.GetEndProperty();
                if(prop.FindPropertyRelative("first").stringValue == tag)
                {
                    prop.FindPropertyRelative("second").stringValue = value;
                    return;
                }
                while(prop.NextVisible(false) && !SerializedProperty.EqualContents(prop, end))
                {
                    if(prop.FindPropertyRelative("first").stringValue == tag)
                    {
                        prop.FindPropertyRelative("second").stringValue = value;
                        return;
                    }
                }
            }

            stringTagMap.arraySize++;
            var p = stringTagMap.GetArrayElementAtIndex(stringTagMap.arraySize - 1);
            p.FindPropertyRelative("first").stringValue = tag;
            p.FindPropertyRelative("second").stringValue = value;
        }
    }
}
#endif
