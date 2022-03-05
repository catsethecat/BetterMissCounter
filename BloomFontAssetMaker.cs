﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace EnhancedMissCounter
{
    internal class BloomFontAssetMaker:PersistentSingleton<BloomFontAssetMaker>
    {
        public TMP_FontAsset BloomFontAsset()
        {
            TMP_FontAsset customFontAsset = TMP_FontAsset.CreateFontAsset(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name.Contains(
                "Teko-Medium SDF")).sourceFontFile);
            customFontAsset.name = "Teko-Medium SDF Bloom";

            customFontAsset.material.shader = Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name.Contains(
                "TextMeshPro/Distance Field"));

            return customFontAsset;
        }
    }
}
