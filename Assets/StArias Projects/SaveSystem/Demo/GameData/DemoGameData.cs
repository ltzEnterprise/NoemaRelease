/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, you can obtain one at  
 * https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) 2025 StArias - https://github.com/starias  
 */

using System;
using UnityEngine;

namespace StArias.API.SaveLoadSystem
{
    [Serializable]
    [CreateAssetMenu(fileName = "DemoGameData", menuName = "DemoGameData", order = 0)]

    public class DemoGameData : GameData
    {
        [SerializeField]
        [Min(0)]
        public uint health = 0;

        [SerializeField]
        [Min(0)]
        public uint mana = 0;

        [SerializeField]
        public Vector3 position = new Vector3(0, 0, 0);
    }
}
