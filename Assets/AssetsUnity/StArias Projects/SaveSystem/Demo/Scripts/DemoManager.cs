/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, you can obtain one at  
 * https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) 2025 StArias - https://github.com/starias  
 */

using UnityEngine;
using StArias.API.SaveLoadSystem;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System;

namespace StArias.API.Demo
{

    /// <summary>
    /// Wrapper created to display all the attributes related to <see cref="DemoGameData"/> on the Editor individually
    /// </summary>
    [Serializable]
    struct MyGameDataWrapper
    {
        public string ID;
        [Min(0)]
        public uint health;
        [Min(0)]
        public uint mana;
        public Vector3 position;
    }

    public class DemoManager : MonoBehaviour
    {
        #region Private Variables

        /// <summary>
        /// The Save Load Manager Instance
        /// </summary>
        private SaveLoadManager _saveLoadManager;

        /// <summary>
        /// List of the objects to delete in the next <see cref="RemoveDataFromUICollection"/> iteration
        /// </summary>
        private List<GameObject> _objectsToDelete = new List<GameObject>();

        /// <summary>
        /// Determines if the loop from <see cref="RemoveDataFromUICollection"/> is running
        /// </summary>
        private bool _isRemovingObjectsRunning = true;

        #endregion

        #region Editor Variables

        [Header("Save Load Variables")]
        [SerializeField]
        [Tooltip("Allows to display the logs from DebugLogger.cs")]
        private bool _displayLogs = true;

        [SerializeField]
        [Tooltip("Allows to overwrite the existing data")]
        private bool _overWriteData = false;

        [Header("Data Creation")]
        [SerializeField]
        [Tooltip("Reference to the Button to save the Demo Game Data")]
        private Button _saveDemoGameDataButton;

        [SerializeField]
        [Tooltip("Reference to the Demo Game Data")]
        private GameData _demoGameData;

        [SerializeField]
        [Tooltip("Reference to the Button to save custom Game Data")]
        private Button _saveCustomGameDataButton;

        [SerializeField]
        [Tooltip("Variables to store in the custom Game Data")]
        private MyGameDataWrapper _customGameData;

        [Header("Data Loader")]
        [SerializeField]
        [Tooltip("Reference to the RectTransform of the ScrollContent GO. " +
            "\nIt is the GO in charge of the ScrollRect behaviour")]
        private RectTransform _scrollContentRT;

        [SerializeField]
        [Tooltip("Reference to the RectTransform of the VerticalLoadGroup GO." +
            "\nIt is the parent of the Data To Load Buttons")]
        private RectTransform _verticalLoadGroupRT;

        [SerializeField]
        [Tooltip("Reference to the RectTransform of the DataToLoad Prefab")]
        private RectTransform _dataToLoadPrefabRT;

        [SerializeField]
        [Tooltip("Reference to the text to display the current loaded data")]
        private TextMeshProUGUI currentDataText;

        #endregion

        private void Awake()
        {
            DebugLogger.EnableDebugLog = _displayLogs;
            StartCoroutine(RemoveDataFromUICollection());

            _saveLoadManager = SaveLoadManager.GetInstance();
            _saveLoadManager.InitGameDataCollection();

            InitSaveDataButtons();
            InitLoadDataButtons();
        }

        #region Init

        /// <summary>
        /// Initializes the buttons related with saving new data
        /// </summary>
        private void InitSaveDataButtons()
        {
            if (_saveCustomGameDataButton != null)
                _saveCustomGameDataButton.onClick.AddListener(SaveCustomGameData);
            else
                DebugLogger.Log("Save Custom Game Data Button is null", DebugColor.Red);

            if (_saveDemoGameDataButton != null)
                _saveDemoGameDataButton.onClick.AddListener(SaveDemoGameData);
            else
                DebugLogger.Log("Demo Editor Game Data Button is null", DebugColor.Red);
        }

        /// <summary>
        /// Initializes the collection of the data on disk ready to be loaded
        /// </summary>
        private void InitLoadDataButtons()
        {
            Dictionary<string, GameData> gameCollection = _saveLoadManager.GetGameDataCollection();
            Vector2 sizeDelta = Vector2.zero;

            foreach (KeyValuePair<string, GameData> collectionData in gameCollection)
            {
                Vector2 pos = new Vector2(0, sizeDelta.y);

                sizeDelta.y += _dataToLoadPrefabRT.sizeDelta.y * 1.05f;
                RectTransform obj = Instantiate(_dataToLoadPrefabRT, _verticalLoadGroupRT);
                obj.SetAsFirstSibling();

                InitializeDataToLoad(obj, collectionData.Value);
            }

            _scrollContentRT.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// Initializes a certain data slot. The slot belongs to the list of data to load visible on the UI
        /// <para></para>
        /// Each slot will have a button to display/use the data and another one to delete the respective data
        /// </summary>
        /// <param name="dataRectTr">The RectTransform component attached to the data slot</param>
        /// <param name="gameData">The GameData attached to the data slot</param>
        private void InitializeDataToLoad(RectTransform dataRectTr, GameData gameData)
        {
            Transform loadDataObject = dataRectTr.GetChild(0);
            Transform deleteDataButton = dataRectTr.GetChild(1);

            loadDataObject.GetComponentInChildren<TextMeshProUGUI>().text = gameData.id;
            Button loadDataButton = loadDataObject.GetComponent<Button>();
            loadDataButton.onClick.AddListener(() =>
            {
                DemoGameData data = SaveLoadHelper.CastFromGameData<DemoGameData>(gameData);

                currentDataText.text = "";
                FieldInfo[] fields = SaveLoadHelper.GetGameDataFields<DemoGameData>();
                foreach (FieldInfo field in fields)
                {
                    currentDataText.text += $"{field.Name}: {field.GetValue(data)}\n";
                }
            });

            Button objButton1 = deleteDataButton.GetComponent<Button>();
            objButton1.onClick.AddListener(() =>
            {
                _saveLoadManager.DeleteDataByID(gameData.id);
                _objectsToDelete.Add(dataRectTr.gameObject);
            });
        }

        #endregion

        /// <summary>
        /// Saves a new custom data created via script
        /// </summary>
        private void SaveCustomGameData()
        {
            if (_saveLoadManager == null)
            {
                DebugLogger.Log("SaveLoadManager is null", DebugColor.Red);
                return;
            }

            DemoGameData gameData = ScriptableObject.CreateInstance<DemoGameData>();
            gameData.name = gameData.id = _customGameData.ID;
            gameData.health = _customGameData.health;
            gameData.mana = _customGameData.mana;
            gameData.position = _customGameData.position;

            bool wasAlreadyExisting = _saveLoadManager.GetGameDataByID(gameData.id);

            gameData = SaveLoadHelper.CastFromGameData<DemoGameData>(_saveLoadManager.SaveNewData(gameData, _overWriteData));

            if (_overWriteData && wasAlreadyExisting)
                return;

            AddGameDataToTheCollection(gameData);
        }

        /// <summary>
        /// Saves the demo game data using the Scriptable Object created on the Editor
        /// </summary>
        private void SaveDemoGameData()
        {
            if (_saveLoadManager == null)
            {
                DebugLogger.Log("SaveLoadManager is null", DebugColor.Red);
                return;
            }

            if (_demoGameData == null)
            {
                DebugLogger.Log("Demo Game Data is null", DebugColor.Red);
                return;
            }

            bool wasAlreadyExisting = _saveLoadManager.GetGameDataByID(_demoGameData.id);

            GameData gameData = _saveLoadManager.SaveNewData(_demoGameData, _overWriteData);

            if (_overWriteData && wasAlreadyExisting)
                return;

            AddGameDataToTheCollection(gameData);
        }

        /// <summary>
        /// Given the gameDataID, it adds a new element to the UI-list to show a new data slot ready to be loaded
        /// </summary>
        /// <param name="gameDataID">The ID of the GameData</param>
        private void AddGameDataToTheCollection(GameData gameData)
        {
            // 1. Creation of the object
            RectTransform gameDataObj = Instantiate(_dataToLoadPrefabRT, _verticalLoadGroupRT);
            gameDataObj.SetAsFirstSibling();

            // 2. Update the size of the parent
            Vector2 sizeDelta = _scrollContentRT.sizeDelta;
            sizeDelta.y += _dataToLoadPrefabRT.sizeDelta.y * 1.05f;
            _scrollContentRT.sizeDelta = sizeDelta;

            // 3. Display the data in the UI
            InitializeDataToLoad(gameDataObj, gameData);
        }

        /// <summary>
        /// Coroutine dedicated to remove the selected data. 
        /// <para></para>
        /// It runs until this.gameObject is deleted or <see cref="_isRemovingObjectsRunning"/> is false.
        /// </summary>
        IEnumerator RemoveDataFromUICollection()
        {
            while (_isRemovingObjectsRunning)
            {
                yield return new WaitUntil(() => _objectsToDelete.Count > 0);

                for (int i = _objectsToDelete.Count - 1; i >= 0; i--)
                {
                    Destroy(_objectsToDelete[i]);
                }

                UpdateSizeGameDataCollection();
            }
        }

        /// <summary>
        /// Update the UI of the GameData Collection
        /// </summary>
        private void UpdateSizeGameDataCollection()
        {
            Vector2 sizeDelta = Vector2.zero;
            sizeDelta.y = _dataToLoadPrefabRT.sizeDelta.y * _verticalLoadGroupRT.childCount * 1.05f;
            _scrollContentRT.sizeDelta = sizeDelta;
        }
    }
}