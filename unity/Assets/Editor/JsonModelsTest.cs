using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class NewEditorTest {

    [Test]
    public void EditorTest()
    {
        //Arrange
        var gameObject = new GameObject();

        //Act
        //Try to rename the GameObject
        var newGameObjectName = "My game object";
        gameObject.name = newGameObjectName;

        //Assert
        //The object has a new name
        Assert.AreEqual(newGameObjectName, gameObject.name);
    }

    [Test]
    public void ScoreboardTest()
    {
        var gameObject = new GameObject();
        var svc = gameObject.AddComponent<ScoreboardService>();

        svc._appId = "VD7WEFHF2-9DSFGYEGFE-0DHGSDGSD";
        svc._baseUri = "http://localhost:1337";


    }
}
