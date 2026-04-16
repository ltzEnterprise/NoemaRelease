/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, you can obtain one at  
 * https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) 2025 StArias - https://github.com/starias  
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StArias.API.SaveLoadSystem
{
    public class SaveLoadHelper
    {
        /// <summary>
        /// Collects the public fields of a certain type class that inherits <see cref="GameData"/>
        /// </summary>
        /// <returns>An array <see cref="FieldInfo"/> with the different fields</returns>
        public static FieldInfo[] GetGameDataFields<T>() where T : GameData
        {
            Type type = typeof(T);

            // Get all public fields
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            return fields;
        }

        /// <summary>
        /// Allows a cast from <see cref="GameData"/> to any type that inherits it
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <param name="dataToCast">The game data to be cast</param>
        public static T CastFromGameData<T>(GameData dataToCast) where T : GameData
        {
            if (dataToCast == null)
            {
                DebugLogger.Log("CastFromGameData - The provided GameData object is null", DebugColor.Red);
                return null;
            }

            if (dataToCast is T castedData)
                return castedData;

            Debug.LogError("CastFromGameData - The provided GameData object cannot be cast to the specified type.");
            return null;  // or throw an exception if you prefer
        }
    }
}