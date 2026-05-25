/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, you can obtain one at  
 * https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) 2025 StArias - https://github.com/starias  
 */

using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace StArias.API.SaveLoadSystem
{
    /// <summary>
    /// Manages the saving and loading of the game data
    /// <para></para>
    /// See <see cref="GetInstance"/> to get the singleton instance of the SaveLoadManager
    /// </summary>
    public class SaveLoadManager
    {
        #region Variables

        /// <summary>
        /// The path where the game data is stored
        /// </summary>
        private readonly string _savePath = Path.Combine(Application.persistentDataPath, "save");

        private readonly string _fileExtension = "";

        /// <summary>
        /// Collection of the saved game data. Each element is accesed by the ID of the game data
        /// </summary>
        private Dictionary<string, GameData> _gameDataCollection = new Dictionary<string, GameData>();

        /// <summary>
        /// Singleton instance of the SaveLoadManager
        /// </summary>
        private static SaveLoadManager Instance;

        #endregion

        #region Constructor
        private SaveLoadManager() { }

        /// <summary>
        /// Returns the singleton instance of the SaveLoadManager
        /// </summary>
        public static SaveLoadManager GetInstance()
        {
            if (Instance == null)
            {
                Instance = new SaveLoadManager();
            }

            return Instance;
        }

        #endregion

        #region API

        /// <summary>
        /// Adds a new game data to the collection
        /// <para></para>
        /// If a game data with the same ID already exists, a new one will be created with a different ID.
        /// <para></para>
        /// The collection of the game data is accessible via <see cref="GetGameDataCollection"/>.
        /// </summary>
        /// <param name="dataToSave">The game data to be saved</param>
        /// <param name="overwrite">If true, the data will be overwritten if it already exists</param>
        /// <returns>The final GameData that was stored</returns>
        public GameData SaveNewData(GameData dataToSave, bool overwrite = false)
        {
            try
            {
                string originalID = dataToSave.id;
                GameData gameData = ScriptableObject.Instantiate(dataToSave);
                if (gameData.id == "")
                {
                    gameData.id = "data";
                }

                CreateSavePathDir();

                // 1. Check if the data already exists
                bool dataExist = _gameDataCollection.ContainsKey(originalID);
                int newIDSufix = 0;

                while (!overwrite && dataExist)
                {
                    gameData.id = originalID + "_" + newIDSufix;
                    dataExist = _gameDataCollection.ContainsKey(gameData.id);
                    if (!dataExist)
                        DebugLogger.Log($"The game data {originalID} already exists. " +
                            $"The game data {gameData.id} will be saved instead", DebugColor.Yellow, "SaveNewData - ");
                    newIDSufix++;
                }

                _gameDataCollection[gameData.id] = dataToSave;

                // 2. Save the game data to a file
                string sDataToSave = JsonUtility.ToJson(gameData);
                File.WriteAllText(Path.Combine(_savePath, gameData.id + _fileExtension), sDataToSave);

                return gameData;
            }
            catch (System.Exception e)
            {
                DebugLogger.Log("Error when saving data", color: DebugColor.Red, "SaveNewData - ");
                DebugLogger.Log(e.Message, color: DebugColor.Red, "SaveNewData - ");

                return null;
            }
        }

        /// <summary>
        /// Initializes the game data collection by reading the saved data from the disk
        /// <para></para>
        /// The collection of the game data is accessible via <see cref="GetGameDataCollection"/>.
        /// </summary>
        public void InitGameDataCollection()
        {
            CreateSavePathDir();

            // 1. Get all the files in the save path
            string[] files = Directory.GetFiles(_savePath + "/");
            foreach (string file in files)
            {
                // 2. Read the content of the file
                string dataContent = File.ReadAllText(file);

                // 3. Create a new GameData object and load the content of the file
                GameData loadedData = ScriptableObject.CreateInstance<GameData>();
                JsonUtility.FromJsonOverwrite(dataContent, loadedData);

                // 4. Get the type of the data
                string savedType = loadedData.GetDataType();
                if (string.IsNullOrEmpty(savedType))
                {
                    DebugLogger.Log($"Invalid file: Data type {savedType} is not valid or null", DebugColor.Red, "InitGameDataCollection - ");
                    continue;
                }

                Type dataType = Type.GetType(savedType);

                if (savedType == null || !typeof(GameData).IsAssignableFrom(dataType))
                {
                    DebugLogger.Log("The type of the data is invalid or unknown: " + savedType, DebugColor.Red, "InitGameDataCollection - ");
                    continue;
                }

                // 5. Create a new GameData object of the correct type
                loadedData = ScriptableObject.CreateInstance(savedType) as GameData;
                JsonUtility.FromJsonOverwrite(dataContent, loadedData);

                _gameDataCollection[loadedData.id] = loadedData;
            }
        }

        /// <summary>
        /// Removes the game data from the disk and from the collection if it exists.
        /// </summary>
        /// <param name="gameDataID">The ID of the game data to delete</param>
        public void DeleteDataByID(string gameDataID)
        {
            string fileName = Path.Combine(Path.Combine(_savePath, gameDataID + _fileExtension));
            if (_gameDataCollection.ContainsKey(gameDataID))
                _gameDataCollection.Remove(gameDataID);

            if (!File.Exists(fileName))
            {
                DebugLogger.Log($"The game data with the ID {gameDataID} does not exist in disk", color: DebugColor.Red, "DeleteDataByID - ");
                return;
            }

            File.Delete(fileName);
        }

        /// <summary>
        /// Gets a game data by its ID. Refer to <see cref="GameData.id"/>
        /// <para></para>
        /// Sees also <see cref="GetGameDataCollection"/>
        /// </summary>
        /// <param name="gameDataID">The ID of the game data</param>
        public GameData GetGameDataByID(string gameDataID)
        {
            if (!_gameDataCollection.ContainsKey(gameDataID))
            {
                //DebugLogger.Log($"The game data with the ID {gameDataID} does not exist", color: DebugColor.Red, "GetGameDataByID - ");
                return null;
            }

            return _gameDataCollection[gameDataID];
        }

        /// <summary>
        /// Gets a copy of the collection of the current saved data
        /// </summary>
        public Dictionary<string, GameData> GetGameDataCollection()
        {
            return new Dictionary<string, GameData>(_gameDataCollection);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Creates the directory where the game data will be saved
        /// </summary>
        private void CreateSavePathDir()
        {
            if (Directory.Exists(_savePath))
                return;

            Directory.CreateDirectory(_savePath);
        }

        #endregion
    }
}