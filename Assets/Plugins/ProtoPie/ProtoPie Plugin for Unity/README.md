# Overview  
ProtoPieUnity is a Unity plug-in that lets you easily establish data communication between a Unity WebGL layer and a pie (React) layer in a web application.

# Package contents  (WIP)
```
ProtoPieUnity
  ├── package.json
  ├── README.md
  ├── CHANGELOG.md
  ├── LICENSE.md
  ├── Editor
  │   ├── protopie.protopieunity.Editor.asmdef
  │   ├── MessageInteraction_CustomEditor.cs
  │   ├── MessageInteractionDrawers.cs
  │   └── WebGLPostProcess.cs
  ├── Runtime
  │   ├── protopie.protopieunity.asmdef
  │   ├── MessageInteraction.cs
  │   ├── Message_Object.cs
  │   └── Editor
  │       └── WebGL
  │           └── React.jslib
  ├── Samples
  │   ├── Resources
  │   │    └── Matarials
  │   │        └── M_brown.mat
  │   ├── Scenes
  │   │    └── SampleScene.unity
  │   └── Scripts
  │       ├── generateCube.cs
  │       └── timer.cs
  ├── Tests
  │   ├── Editor
  │   │   ├── protopie.protopieunity.Editor.Tests.asmdef
  │   │   └── MessageInteractionCustomEditorTests.cs
  │   └── Runtime
  │        ├── protopie.protopieunity.Tests.asmdef
  │        └── MessageInteractionTest.cs
  └── Documentation
       └── instructions_ProtoPieUnityPackage.pdf
```

# Installation instructions 

## Option 1: Using Package Manager
Go open package manager (Window > Package Manager) in your own Unity project or a newly created project. Click '+' button and choose "Add package by name". Enter "[packageNameOnTheAssetStore]" and Click Add. Upon the ProtoPieUnity page appear on the right pane, Click Install button.

## Option 2: Using the custom package file
In your own Unity project or a newly created project, go to Assets > Import Package > Custom Package. Then, locate where you saved  "ProtoPieUnityPackage.unitypackage" file and open it. Upon "Import Unity Package window" pops up, make sure you select all files and click Import. 

# Tutorials 
For detailed step-by-step tutorial, please refer to "Documentation/instructions_ProtoPieUnityPackage.pdf"