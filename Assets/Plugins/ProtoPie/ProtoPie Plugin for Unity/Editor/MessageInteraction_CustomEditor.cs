// using HMI.ControlsManager;
using ProtoPie.Interaction;
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

/// <summary>
/// Custom editor that is the type of ControlsManagerScript
/// </summary>
[CustomEditor(typeof(MessageInteraction))]
public class MessageInteraction_CustomEditor : Editor
{
    /// <summary>
    /// ControlsData assignment for the controls manager
    /// </summary>
    private SerializedProperty DataScript;

    /// <summary>
    /// List of Keypresses of the controls manager
    /// </summary>
    private SerializedProperty eventMessagesList;

    private bool showEventMessages = false;
    /// <summary>
    /// On awake, sets the script to the target ControlsManagerScript
    /// Calls to updaet all controls list on the script
    /// </summary>
    public MessageInteraction Script;
    public void Awake()
    {
        // var Script = (MessageInteraction)target;
        Script = (MessageInteraction)target;
        Script.UpdateAllMessagesList();
        if(!Script.firstMappingAddedFromEditor) {
            Script.addMapping();
            Script.firstMappingAddedFromEditor=true;
        }
    }

    /// <summary>
    /// Unity OnEnable callback
    /// </summary>
    private void OnEnable()
    {
        DataScript = serializedObject.FindProperty("MessageData");
        eventMessagesList = serializedObject.FindProperty("EventMessageMappingList");

    }

    /// <summary>
    /// Draws the base inspector
    /// Sets the target to the ControlsManagerScript target
    /// Creates a button UpdateControlMapping that will forcibly update all controls on the list
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(DataScript);
        GUILayout.Space(10);
        // var Script = (MessageInteraction)target;


        if (GUILayout.Button("Refresh Mappings"))
        {
            Debug.Log("Refresh Mappings Button Pressed");
            Script.UpdateAllMessagesList();
        }

        // ------------------- Display Event-Message mappings -------------------
        // Define foldoutStyle to make the label boldface
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontStyle = FontStyle.Bold;
        // Create a box style for the panel
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10); // Adjust padding as needed

        // Handle the list of event-message mappings
        showEventMessages = EditorGUILayout.Foldout(showEventMessages, "Event(Unity)-Message(ProtoPie) Mappings", true, foldoutStyle);
        EditorGUI.BeginChangeCheck();
        if (EditorGUI.EndChangeCheck())
        {
            Script.UpdateAllMessagesList();
            serializedObject.ApplyModifiedProperties();
            Debug.Log("OnInspectorGUI");
        }
        
        string[] messageNames = Script.GetMessageNames(); // This method should exist in MessageInteraction and return all possible names
        // Dictionary<string, string> messageNameValuePairs = Script.GetMessageNameValuePairs(); // This method should return name-value pairs
        string[] messageValues = Script.GetMessageValues();

        if (showEventMessages)
        {
            EditorGUI.indentLevel++;
            if (GUILayout.Button("Add Mapping"))
            {
                Debug.Log("add mapping");
                Script.addMapping();
            }

            if (eventMessagesList != null && eventMessagesList.isArray)
            {
                for (int i = 0; i < eventMessagesList.arraySize; i++)
                {
                    EditorGUILayout.LabelField($"Mapping {i + 1}", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUI.indentLevel++;
                    SerializedProperty mappingProperty = eventMessagesList.GetArrayElementAtIndex(i);
                    SerializedProperty eventObjectProperty = mappingProperty.FindPropertyRelative("EventObject");
                    SerializedProperty selectedEventNameProperty = mappingProperty.FindPropertyRelative("selectedEventName");
                    SerializedProperty messageToSendProperty = mappingProperty.FindPropertyRelative("messageToSend");
                    SerializedProperty variableSourceObjectProperty = mappingProperty.FindPropertyRelative("variableSourceObject");
                    SerializedProperty selectedVariableNameProperty = mappingProperty.FindPropertyRelative("selectedVariableName");

                    // ---------- display MessageMappingNmae and update Message Value accodringly ------------
                    // Example loop to draw each EventMessage's MessageMappingName and MessageValue
                    SerializedProperty eventMessage = eventMessagesList.GetArrayElementAtIndex(i);
                    SerializedProperty mappingLabelString = eventMessage.FindPropertyRelative("MappingLabelString");
                    SerializedProperty messageString = eventMessage.FindPropertyRelative("MessageString");
                    SerializedProperty msgDir = eventMessage.FindPropertyRelative("Direction");
                    SerializedProperty desiredAction = eventMessage.FindPropertyRelative("DesiredAction");

                    // Convert the current name to an index
                    // int currentIndex = Array.IndexOf(messageNames, mappingLabelString.stringValue);
                    // if (currentIndex == -1) currentIndex = 0; // Default to the first option if not found
                    int currentIndex = 0;
                    if (messageNames != null)
                    {
                        currentIndex = Array.IndexOf(messageNames, mappingLabelString.stringValue);
                        if (currentIndex == -1) currentIndex = 0; // Default to the first option if not found
                    }
                    else
                    {
                        Debug.Log("messageNames is null");
                    }

                    // Draw dropdown and update the selected index
                    int selectedIndex = EditorGUILayout.Popup("Mapping Label", currentIndex, messageNames);
                    if (selectedIndex != currentIndex) // If selection changed
                    {
                        mappingLabelString.stringValue = messageNames[selectedIndex];
                        messageString.stringValue = messageValues[selectedIndex];
                        Debug.Log("selectedIndex:"+selectedIndex+", messageNames[selectedIndex]:"+messageNames[selectedIndex]+", messageValues[selectedIndex]:"+messageValues[selectedIndex]);
                        serializedObject.ApplyModifiedProperties(); // Apply the change
                    }
                    EditorGUILayout.LabelField("Message", messageString.stringValue);
                    EditorGUILayout.PropertyField(msgDir, new GUIContent("Message Direction"));
                    if(msgDir.intValue==1 || msgDir.intValue==3) { // if message direction is ProtoPieToUnity or bothWays 
                        EditorGUILayout.PropertyField(desiredAction, new GUIContent("Desired Action"));
                    }
                    // ---------------------------------------------------------------------------------------
                    if(msgDir.intValue==2 || msgDir.intValue==3) { // if message direction is UnityToProtoPie or bothWays 
                        EditorGUILayout.PropertyField(eventObjectProperty, new GUIContent("Event Object"));
                    if (eventObjectProperty.objectReferenceValue != null)
                    {
                        GameObject targetObject = (GameObject)eventObjectProperty.objectReferenceValue;
                        DisplayUnityEventDropdown(targetObject, selectedEventNameProperty, i);
                    }
                    EditorGUILayout.PropertyField(variableSourceObjectProperty, new GUIContent("Value Source Object"));
                    if (variableSourceObjectProperty.objectReferenceValue != null)
                    {
                        DisplayVariableDropdown((GameObject)variableSourceObjectProperty.objectReferenceValue, selectedVariableNameProperty);
                    }
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("");
                    if (GUILayout.Button("+", GUILayout.Width(30)))
                    {
                        eventMessagesList.InsertArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }
                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        eventMessagesList.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(10); // Add some spacing between mappings
                }
            }
            EditorGUI.indentLevel--;
        }
        // ------------------------------------------------------------------------
        Script.UpdateAllMessagesList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DisplayUnityEventDropdown(GameObject targetObject, SerializedProperty selectedEventNameProperty, int index)
    {
        // Initialize an empty list to hold UnityEvent field names
        List<string> unityEventNames = new List<string>();
        List<string> eventFieldNames = new List<string>(); // Actual field names for reflection

        // Get all MonoBehaviour components attached to the target object
        MonoBehaviour[] components = targetObject.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            // Get all fields declared in the component
            var fields = component.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(f.FieldType));

            foreach (var fieldInfo in fields)
            {
                unityEventNames.Add($"{component.GetType().Name}.{fieldInfo.Name}");
                eventFieldNames.Add(fieldInfo.Name); // Store the actual field name for later
            }
        }

        // Dropdown for selecting UnityEvent
        if (unityEventNames.Count > 0)
        {
            int selectedIndex = eventFieldNames.IndexOf(selectedEventNameProperty.stringValue);
            if (selectedIndex == -1) selectedIndex = 0; // Default to first item if not found

            int newSelectedIndex = EditorGUILayout.Popup($"Event to trigger message", selectedIndex, unityEventNames.ToArray());
            selectedEventNameProperty.stringValue = eventFieldNames.ElementAtOrDefault(newSelectedIndex);
        }
        else
        {
            EditorGUILayout.LabelField("No UnityEvents found.");
        }
    }

    private void DisplayVariableDropdown(GameObject variableSourceObject, SerializedProperty selectedVariableNameProperty)
    {
        if (variableSourceObject == null)
        {
            EditorGUILayout.LabelField("No source object.");
            return;
        }

        List<string> variableNames = new List<string>();
        MonoBehaviour[] components = variableSourceObject.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                variableNames.Add($"{comp.GetType().Name}.{field.Name}");
            }
        }

        if (variableNames.Count > 0)
        {
            int selectedIndex = variableNames.IndexOf(selectedVariableNameProperty.stringValue);
            if (selectedIndex == -1) selectedIndex = 0;
            int newSelectedIndex = EditorGUILayout.Popup("Value to send", selectedIndex, variableNames.ToArray());
            selectedVariableNameProperty.stringValue = variableNames.ElementAtOrDefault(newSelectedIndex);
        }
        else
        {
            EditorGUILayout.LabelField("No public instance variables found.");
        }
    }

    public int GetEventMessageSize()    // a method for EditorTestScripts
    {
        return eventMessagesList.arraySize;
    }


}