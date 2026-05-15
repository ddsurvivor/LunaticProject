using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using ProtoPie.Interaction;

// namespace ProtoPie.Interaction
// {
    [CreateAssetMenu(fileName = "ProtoPie", menuName = "ProtoPie/MessageSetup", order = 0)]
    public class Message_Object : ScriptableObject {
        
        // public enum messageDirection:ushort {
        //     None=0,
        //     ProtopieToUnity=1,
            
        //     UnityToProtopie=2,
        //     BothWays=3
        // }

        [Serializable]
        public class MessageList
        {
            /// <summary>
            /// Message ID
            /// </summary>
            public string mappingLabel;

            /// <summary>
            /// Message Value
            /// </summary>
            public string message;
            public MessageInteraction.messageDirection direction;
        }
        
        /// <summary>
        /// List of ControlsList that user will populate
        /// </summary>
        public List<MessageList> MessageMappingList = new List<MessageList>();

        
        
    }
// }