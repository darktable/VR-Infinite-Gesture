﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Edwon.VR;
using Edwon.VR.Input;
using Edwon.VR.Gesture;
using System.IO;
using System;

[CreateAssetMenu(fileName = "Settings", menuName = "VRInfiniteGesture/Settings", order = 1)]
public class GestureSettings : ScriptableObject {

    public VRGestureRig rig
    {
        get;
        set;
    }
    public int gestureRigID = 0;

    [Header("VR Infinite Gesture")]
    [Tooltip("display default gesture trails")]
    public bool displayGestureTrail = true;
    [Tooltip("the button that triggers gesture recognition")]
    public InputOptions.Button gestureButton = InputOptions.Button.Trigger1;
    [Tooltip("the threshold over wich a gesture is considered correctly classified")]
    public double confidenceThreshold = 0.98;
    [Tooltip("Your gesture must have one axis longer than this length in world size")]
    public float minimumGestureAxisLength = 0.10f;
    [Tooltip("use this option for builds when you don't want users to see the VR UI from this plugin")]
    public bool beginInDetectMode = false;
    // whether to track when pressing trigger or all the time
    // continious mode is not supported yet
    // though you're welcome to try it out
    [HideInInspector]
    public VRGestureDetectType vrGestureDetectType;

    [Header("ACTIVE NETWORKS")]
    [Tooltip("the neural net that I am using")]
    public string currentNeuralNet;
    public string lastNeuralNet; // used to know when to refresh gesture bank
    public List<string> neuralNets;
    private List<Gesture> gestures;  // list of gestures already trained in currentNeuralNet
    public List<Gesture> Gestures
    {
        get
        {
            NeuralNetworkStub stub = Utils.ReadNeuralNetworkStub(currentNeuralNet);
            return stub.gestures;
        }
        set
        {
            value = gestures;
        }
    }
    public List<Gesture> gestureBank; // list of recorded gesture for current neural net
    public List<int> gestureBankTotalExamples;

    public Trainer currentTrainer { get; set; }

    public VRGestureManagerState state = VRGestureManagerState.Idle;
    public VRGestureManagerState stateInitial;

    public bool readyToTrain
    {
        get
        {
            if (gestureBank != null)
            {
                if (gestureBank.Count > 0)
                {
                    foreach (int total in gestureBankTotalExamples)
                    {
                        if (total <= 0)
                            return false;
                    }
                    return true;
                }
                else
                    return false;
            }
            return false;
        }
    }
    //Drop Down list of NeuralNetworks.
    //List of Processed Gestures
    //List of New Gestures sitting in data.

    public void OnEnable()
    {
        rig = VRGestureRig.GetPlayerRig(gestureRigID);
    }


    #region NEURAL NETWORK ACTIVE METHODS
    //This should be called directly from UIController via instance
    //Most of these should be moved into RIG as they are just editing vars in RIG.
    [ExecuteInEditMode]
    public void BeginTraining(Action<string> callback)
    {
        rig = VRGestureRig.GetPlayerRig(gestureRigID);
        rig.state = VRGestureManagerState.Training;
        rig.currentTrainer = new Trainer(currentNeuralNet, gestureBank);
        rig.currentTrainer.TrainRecognizer();
        // finish training
        rig.state = VRGestureManagerState.Idle;
        callback(currentNeuralNet);
    }

    [ExecuteInEditMode]
    public void EndTraining(Action<string> callback)
    {
        rig = VRGestureRig.GetPlayerRig(gestureRigID);
        rig.state = VRGestureManagerState.Idle;
        callback(currentNeuralNet);
    }
    #endregion



    #region NEURAL NETWORK EDIT METHODS
    [ExecuteInEditMode]
    public bool CheckForDuplicateNeuralNetName(string neuralNetName)
    {
        // if neuralNetName already exists return true
        if (neuralNets.Contains(neuralNetName))
            return true;
        else
            return false;
    }

    [ExecuteInEditMode]
    public void CreateNewNeuralNet(string neuralNetName)
    {
        // create new neural net folder
        Utils.CreateFolder(neuralNetName);
        // create a gestures folder
        Utils.CreateFolder(neuralNetName + "/Gestures/");

        neuralNets.Add(neuralNetName);
        gestures = new List<Gesture>();
        gestureBank = new List<Gesture>();
        gestureBankPreEdit = new List<Gesture>();
        gestureBankTotalExamples = new List<int>();

        // select the new neural net
        SelectNeuralNet(neuralNetName);
    }

    [ExecuteInEditMode]
    public void RefreshNeuralNetList()
    {
        Debug.Log("IM GETTING CALLED ALL THE TIME");
        neuralNets = new List<string>();
        string path = Config.SAVE_FILE_PATH;
        foreach (string directoryPath in System.IO.Directory.GetDirectories(path))
        {
            string directoryName = Path.GetFileName(directoryPath);
            if (!neuralNets.Contains(directoryName))
            {
                neuralNets.Add(directoryName);
            }
        }
    }

    [ExecuteInEditMode]
    public void RefreshGestureBank(bool checkNeuralNetChanged)
    {
        //Debug.Log("REFRESH GESTURE BANK");

        if (checkNeuralNetChanged)
        {
            if (currentNeuralNet == lastNeuralNet)
            {
                return;
            }
        }

        if (currentNeuralNet != null && currentNeuralNet != "" && Utils.GetGestureBank(currentNeuralNet) != null)
        {
            gestureBank = Utils.GetGestureBank(currentNeuralNet);
            gestureBankPreEdit = new List<Gesture>(gestureBank);
            gestureBankTotalExamples = Utils.GetGestureBankTotalExamples(gestureBank, currentNeuralNet);
        }
        else
        {
            gestureBank = new List<Gesture>();
            gestureBankPreEdit = new List<Gesture>();
            gestureBankTotalExamples = new List<int>();
        }
    }

    [ExecuteInEditMode]
    public void DeleteNeuralNet(string neuralNetName)
    {
        // get this neural nets index so we know which net to select next
        int deletedNetIndex = neuralNets.IndexOf(neuralNetName);

        // delete the net and gestures
        neuralNets.Remove(neuralNetName); // remove from list
        gestureBank.Clear(); // clear the gestures list
        gestureBankPreEdit.Clear();
        gestureBankTotalExamples.Clear();
        Utils.DeleteNeuralNetFiles(neuralNetName); // delete all the files

        if (neuralNets.Count > 0)
            SelectNeuralNet(neuralNets[0]);
    }

    [ExecuteInEditMode]
    public void SelectNeuralNet(string neuralNetName)
    {
        
        lastNeuralNet = currentNeuralNet;
        currentNeuralNet = neuralNetName;
        RefreshGestureBank(true);
    }

    [ExecuteInEditMode]
    public void CreateGesture(string gestureName)
    {
        Gesture newGesture = new Gesture();
        newGesture.name = gestureName;
        newGesture.hand = HandType.Right;
        newGesture.isSynchronous = false;
        newGesture.exampleCount = 0;


        gestureBank.Add(newGesture);
        gestureBankTotalExamples.Add(0);
        Utils.CreateGestureFile(gestureName, currentNeuralNet);
        gestureBankPreEdit = new List<Gesture>(gestureBank);
    }

    [ExecuteInEditMode]
    public Gesture FindGesture(string gestureName)
    {
        //int index = gestureBank.IndexOf(gestureName);
        Predicate<Gesture> gestureFinder = (Gesture g) => { return g.name == gestureName; };
        Gesture gest = gestureBank.Find(gestureFinder);
        return gest;
    }


    [ExecuteInEditMode]
    public void DeleteGesture(string gestureName)
    {
        //int index = gestureBank.IndexOf(gestureName);
        Predicate<Gesture> gestureFinder = (Gesture g) => { return g.name == gestureName; };
        int index = gestureBank.FindIndex(gestureFinder);
        gestureBank.RemoveAt(index);
        gestureBankTotalExamples.RemoveAt(index);
        Utils.DeleteGestureFile(gestureName, currentNeuralNet);
        gestureBankPreEdit = new List<Gesture>(gestureBank);
    }

    List<Gesture> gestureBankPreEdit;

    bool CheckForDuplicateGestures(string newName)
    {
        bool dupeCheck = true;
        int dupeCount = -1;
        foreach (Gesture gesture in gestureBank)
        {
            if (newName == gesture.name)
            {
                dupeCount++;
            }
        }
        if (dupeCount > 0)
        {
            dupeCheck = false;
        }

        return dupeCheck;
    }

#if UNITY_EDITOR
    [ExecuteInEditMode]
    public GestureSettingsEditor.VRGestureRenameState RenameGesture(int gestureIndex)
    {
        //check to make sure the name has actually changed.
        string newName = gestureBank[gestureIndex].name;
        string oldName = gestureBankPreEdit[gestureIndex].name;
        GestureSettingsEditor.VRGestureRenameState renameState = GestureSettingsEditor.VRGestureRenameState.Good;

        if (oldName != newName)
        {
            if (CheckForDuplicateGestures(newName))
            {
                //ACTUALLY RENAME THAT SHIZZ
                Utils.RenameGestureFile(oldName, newName, currentNeuralNet);
                gestureBankPreEdit = new List<Gesture>(gestureBank);

            }
            else
            {
                //reset gestureBank
                gestureBank = new List<Gesture>(gestureBankPreEdit);
                renameState = GestureSettingsEditor.VRGestureRenameState.Duplicate;
            }
        }
        else
        {
            renameState = GestureSettingsEditor.VRGestureRenameState.NoChange;
        }

        return renameState;
    }
#endif

    #endregion

}