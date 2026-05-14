using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

namespace ProtoPie.Interaction
{
    /// <summary>
    /// Creates an attribute that is used to create a dropdown menu with a list of data in the inspector
    /// </summary>
    public class ListToPopupAttribute : PropertyAttribute
    {
        public Type MyType;
        public string PropertyName;

        public ListToPopupAttribute(Type myType, string propertyName)
        {
            MyType = myType;
            PropertyName = propertyName;
        }
    }
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

    public class MessageInteraction : MonoBehaviour, ISerializationCallbackReceiver
    {
        public enum messageDirection : ushort
        {
            None = 0,
            ProtopieToUnity = 1,

            UnityToProtopie = 2,
            BothWays = 3
        }

        [Serializable]
        public class EventMessageMapping
        {
            [ListToPopup(typeof(MessageInteraction), "StaticMessageNameList")]
            public string MappingLabelString;
            [ReadOnly] public string MessageString;
            public messageDirection Direction;
            public UnityEvent<string> DesiredAction;

            public GameObject EventObject;
            public string selectedEventName;
            public string messageToSend;
            public GameObject variableSourceObject;
            public string selectedVariableName;

            public EventMessageMapping(GameObject targetObject, string selectedEventName, string messageToSend)
            {
                this.EventObject = targetObject;
                this.selectedEventName = selectedEventName;
                this.messageToSend = messageToSend;
                this.Direction = messageDirection.ProtopieToUnity;
                this.MappingLabelString = "";
                this.MessageString = "";
            }
        }

        public Message_Object MessageData;

        public static List<string> StaticMessageNameList;
        public static List<string> StaticMessageValueList;

        [HideInInspector]
        public List<string> AllMessageNameList = new List<string>();
        public List<string> AllMessageValueList = new List<string>();
        public List<EventMessageMapping> EventMessageMappingList = new List<EventMessageMapping>();

        [DllImport("__Internal")]
        static extern void MessageUnityToReact(string message);
        public bool firstMappingAddedFromEditor = false; // flag for Editor test (MessageIntearctionCustomEditorTests.cs)
        void Reset()
        {
            firstMappingAddedFromEditor = false;
        }

        private void Start()
        {
            UpdateAllMessagesList();
            // Subscribe each mapping for sending messages
            foreach (var mapping in EventMessageMappingList)
            {
                if (mapping.Direction == messageDirection.UnityToProtopie || mapping.Direction == messageDirection.BothWays)
                    SubscribeToEvent(mapping);
            }
#if !UNITY_EDITOR && UNITY_WEBGL
            // disable WebGLInput.captureAllKeyboardInput so elements in web page can handle keyboard inputs
            WebGLInput.captureAllKeyboardInput = false;
#endif
        }
        public void addMapping()
        {
            EventMessageMappingList.Add(new EventMessageMapping(null, null, null));
        }

        public void OnBeforeSerialize()
        {
            if (MessageData != null)
            {
                if (StaticMessageNameList != AllMessageNameList)
                {
                    StaticMessageNameList = AllMessageNameList;
                }

                if (StaticMessageValueList != AllMessageValueList)
                {
                    StaticMessageValueList = AllMessageValueList;
                }
            }
        }

        public void OnAfterDeserialize()
        {
        }

        public void UpdateAllMessagesList()
        {
            if (MessageData != null)
            {
                AllMessageNameList.Clear();
                for (var j = 0; j < MessageData.MessageMappingList.Count; j++)
                {
                    AllMessageNameList.Add(MessageData.MessageMappingList[j].mappingLabel);
                    AllMessageValueList.Add(MessageData.MessageMappingList[j].message);
                }
                for (var i = 0; i < EventMessageMappingList.Count; i++)
                {
                    UpdateOutgoingKeyMappedButton(EventMessageMappingList[i]);
                }
            }
        }

        public void UpdateOutgoingKeyMappedButton(EventMessageMapping CurrentClass)
        {
            if (MessageData != null)
            {
                for (var i = 0; i < MessageData.MessageMappingList.Count; i++)
                {
                    if (CurrentClass.MappingLabelString == MessageData.MessageMappingList[i].mappingLabel)
                    {
                        CurrentClass.MessageString = MessageData.MessageMappingList[i].message;
                        CurrentClass.Direction = (messageDirection)MessageData.MessageMappingList[i].direction;
                    }
                }
            }
        }


        public void SubscribeToEvent(EventMessageMapping mapping)
        {
            if (mapping.EventObject == null || string.IsNullOrEmpty(mapping.selectedEventName))
                return;

            MonoBehaviour[] components = mapping.EventObject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                var field = component.GetType().GetField(mapping.selectedEventName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null && typeof(UnityEngine.Events.UnityEvent).IsAssignableFrom(field.FieldType))
                {
                    UnityEvent unityEvent = field.GetValue(component) as UnityEvent;
                    if (unityEvent != null)
                    {
                        string variableValue = GetVariableValueAsString(mapping.variableSourceObject, mapping.selectedVariableName);
                        if(variableValue.Length > 0)
                        {
                            unityEvent.AddListener(() =>
                            {
                                SendMessageToReact($"{mapping.MessageString}/{variableValue}");
                            });
                        }
                        else {
                            unityEvent.AddListener(() =>
                            {
                                SendMessageToReact(mapping.MessageString);
                            });
                        }
                        field.SetValue(component, unityEvent);

                        break;
                    }
                }
            }
        }

        private string GetVariableValueAsString(GameObject sourceObject, string variableName)
        {
            if (sourceObject == null || string.IsNullOrEmpty(variableName))
                return "";

            // Split the variableName into className and fieldName
            string[] parts = variableName.Split('.');
            if (parts.Length != 2) // Expecting exactly two parts: ClassName.FieldName
                return "";

            string className = parts[0];
            string fieldName = parts[1];

            MonoBehaviour[] components = sourceObject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                // Check if the component's type name matches the className part of variableName
                if (component.GetType().Name == className)
                {
                    var field = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        object value = field.GetValue(component);
                        return value?.ToString() ?? "";
                    }
                }
            }
            return "";
        }

        public string[] GetMessageNames()
        {
            if (StaticMessageNameList == null)
                return null;
            return StaticMessageNameList.ToArray();
        }

        public string[] GetMessageValues()
        {
            if (StaticMessageValueList == null)
                return null;
            return StaticMessageValueList.ToArray();
        }

        public void UpdateMessageValue(string MappingLabelString, string newValue)
        {
            foreach (var eventMessage in EventMessageMappingList)
            {
                if (eventMessage.MappingLabelString == MappingLabelString)
                {
                    eventMessage.MessageString = newValue;
                    break; // Assuming unique names, break after finding the first match
                }
            }
        }



        public void SendMessageToReact(string message)
        {
            UnityEngine.Debug.Log($"Message sent to ProtoPie Connect: \"{message}\"");
#if UNITY_WEBGL && !UNITY_EDITOR
            MessageUnityToReact(message);
#endif
        }

        public void SendMessageToUnity(string value)
        {
            string[] messageItems = value.Split('/'); // split function name with parameters, e.g., moveCube/100,100,100
            for (int i = 0; i < EventMessageMappingList.Count; i++)
            {
                if (EventMessageMappingList[i].Direction == messageDirection.ProtopieToUnity || EventMessageMappingList[i].Direction == messageDirection.BothWays)
                {
                    if (String.Equals((String)messageItems[0], EventMessageMappingList[i].MessageString))    // (string)value has to be casted to "S"tring for comparison
                    {
                        if (messageItems.Length < 2) // when there is no argument
                        {
                            EventMessageMappingList[i].DesiredAction.Invoke("");
                        }
                        else // when there is argument
                        {
                            EventMessageMappingList[i].DesiredAction.Invoke(messageItems[1]);
                        }
                        break;
                    }
                }
            }
        }

    }
}
