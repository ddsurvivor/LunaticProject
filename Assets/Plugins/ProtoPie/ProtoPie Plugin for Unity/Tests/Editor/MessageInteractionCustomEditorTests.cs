// using System.Collections;
// using System.Collections.Generic;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;
// using ProtoPie.Interaction;

// public class EditorTestScripts
// {
//     // A Test behaves as an ordinary method
//     [Test]
//     public void EditorTestScriptSimplePasses()
//     {
//         // Assign
//         var mi_customEditor = ScriptableObject.CreateInstance<MessageInteraction_CustomEditor>();
//         // var messageInteraction = Substitute.For<MessageInteraction>();
//         // mi_customEditor.target = messageInteraction;
//         // Act
//         int currEventMessageSize = mi_customEditor.GetEventMessageSize();
//         mi_customEditor.Script.addMapping();
//         // Assert
//         Assert.AreEqual(currEventMessageSize + 1, mi_customEditor.GetEventMessageSize());
//     }
// }
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ProtoPie.Interaction; // Assuming this namespace contains MessageInteraction

[TestFixture]
public class MessageInteractionCustomEditorTests
{
    private GameObject testGameObject;
    private MessageInteraction testComponent;
    private MessageInteraction_CustomEditor editor;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Create a GameObject and add MessageInteraction component once for all tests
        testGameObject = new GameObject("TestObject");
        testComponent = testGameObject.AddComponent<MessageInteraction>();

    }

    [SetUp]
    public void SetUp()
    {
        testComponent.firstMappingAddedFromEditor = false; // Reset the flag to ensure it's false each time

        // Creating the custom editor instance
        editor = (MessageInteraction_CustomEditor)Editor.CreateEditor(testComponent);
    }

    [Test]
    public void TestInitialSize()
    {
        editor.Awake();  // Manually triggering the Awake since it won't automatically call in tests
        // Checking the size after setup and potentially adding a mapping
        int initialSize = editor.GetEventMessageSize();
        Assert.AreEqual(1, initialSize, "Expected initial size is one.");
    }

    // Additional tests can be added here

    [TearDown]
    public void TearDown()
    {
        // Clean up any state specific to each test if necessary
        Object.DestroyImmediate(editor);
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        // Clean up the GameObject after all tests are run
        Object.DestroyImmediate(testGameObject);
    }
}
