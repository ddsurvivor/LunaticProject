using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProtoPie.Interaction;

public class MessageInteractionTest
{
    private GameObject testGameObject;
    private MessageInteraction testComponent;
    // A Test behaves as an ordinary method

    [SetUp]
    public void SetUp()
    {
        // Create a GameObject and add MessageInteraction component once for all tests
        testGameObject = new GameObject("TestObject");
        testComponent = testGameObject.AddComponent<MessageInteraction>();
    }

    [Test]
    public void TestEventMessageSizeAfterAdding()
    {
        // Assign
        int currEventMessageSize = testComponent.EventMessageMappingList.Count;
        // Act
        testComponent.addMapping();
        // Assert
        Assert.AreEqual(currEventMessageSize + 1, testComponent.EventMessageMappingList.Count);
    }

    [TearDown]
    public void GlobalTearDown()
    {
        // Clean up the GameObject after all tests are run
        Object.DestroyImmediate(testGameObject);
    }
}
