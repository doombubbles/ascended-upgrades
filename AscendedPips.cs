using System;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Extensions;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AscendedUpgrades;

[RegisterTypeInIl2Cpp(false)]
public class AscendedPips : MonoBehaviour
{
    public UpgradeObject upgradeObject = null!;

    public ModHelperScrollPanel scrollPanel = null!;

    public List<GameObject> pips = null!;

    public GameObject pipPrefab = null!;

    public AscendedPips(IntPtr obj0) : base(obj0)
    {
    }

    public void Init(UpgradeObject obj, ModHelperScrollPanel panel)
    {
        upgradeObject = obj;
        scrollPanel = panel;
        pips = new List<GameObject>();

        scrollPanel.ScrollRect.vertical = false;
        scrollPanel.AddComponent<Image>();
        scrollPanel.GetComponent<Mask>().showMaskGraphic = false;
        var scrollContent = scrollPanel.ScrollContent;
        var scrollTransform = scrollContent.RectTransform;
        scrollTransform.pivot = scrollTransform.anchorMin = scrollTransform.anchorMax = Vector2.zero;


        var gridLayoutGroup = scrollContent.AddComponent<GridLayoutGroup>();
        gridLayoutGroup.cellSize = new Vector2(55, 55);
        gridLayoutGroup.spacing = new Vector2(4, 4);
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayoutGroup.constraintCount = 5;
        gridLayoutGroup.startCorner = GridLayoutGroup.Corner.LowerLeft;
        gridLayoutGroup.childAlignment = TextAnchor.LowerLeft;
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Vertical;

        var fitter = scrollContent.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        pipPrefab = CreatePipPrefab();
    }

    public void SetAmount(int amount)
    {
        for (var i = pips.Count; i < amount; i++)
        {
            AddPip(i);
        }

        var allPips = pips.ToArray();
        
        for (var i = 0; i < pips.Count; i++)
        {
            var pip = allPips[i];
            pip.SetActive(i < amount);
            pip.transform.localScale = Vector3.one;
        }

        scrollPanel.ScrollRect.enabled = amount > 35;
    }

    public GameObject CreatePipPrefab()
    {
        var pip = new GameObject("Pip");
        pip.transform.SetParent(scrollPanel.ScrollContent);
        var image = pip.AddComponent<Image>();
        image.SetSprite(ModContent.GetSpriteReference<AscendedUpgradesMod>("AscendedPip"));
        pip.AddComponent<LayoutElement>();
        pip.SetActive(false);
        return pip;
    }

    public void AddPip(int i)
    {
        var pip = pipPrefab.Duplicate(scrollPanel.ScrollContent);
        pip.name = $"Pip{i}";
        pips.Add(pip);
    }

    private void FixedUpdate()
    {
        transform.localPosition = new Vector3(-370, 6, 0);
    }

    public static AscendedPips Create(UpgradeObject __instance)
    {
        var panel = __instance.gameObject.AddModHelperScrollPanel(new Info("AscendedPips", 500, -40)
        {
            AnchorMin = new Vector2(0, 0),
            AnchorMax = new Vector2(0, 1),
            Pivot = new Vector2(0, 0.5f)
        }, null);
        panel.transform.SetSiblingIndex(1);
        var pips = panel.AddComponent<AscendedPips>();
        pips.Init(__instance, panel);
        return pips;
    }
}