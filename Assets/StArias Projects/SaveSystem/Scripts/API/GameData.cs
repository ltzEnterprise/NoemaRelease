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
    /// <summary>
    /// Base class for the game data
    /// <para></para>
    /// A new game data class must inherit from this class
    /// </summary>
    [Serializable]
    public class GameData : ScriptableObject
    {
        /// <summary>
        /// The ID of the game data
        /// </summary>
        [SerializeField]
        public string id = "GameData";

        /// <summary>
        /// The type of the game data. This is determined automatically within the constructor
        /// </summary>
        [SerializeField]
        [HideInInspector]
        protected string dataType;

        /// <summary>
        /// Constructor of the class
        /// </summary>
        public GameData()
        {
            dataType = GetType().FullName;
        }

        /// <summary>
        /// Returns the type name of the game data
        /// </summary>
        public string GetDataType()
        {
            return dataType;
        }
    }
}