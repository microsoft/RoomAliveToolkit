
# RoomAlive Toolkit for Unity README

RoomAlive Toolkit for Unity contains is a set of Unity scripts and tools that enable immersive, dynamic projection mapping experiences, based on the projection-camera calibration from RoomAlive Toolkit.

Here are a few of the most comon reasons why one might want to use this toolkit:

* To easily get and render Kinect depth + RGB + audio + skeleton data in Unity. Our project includes Unity shaders that *mesh* depth images and texture map them so your CPU is ready for other tasks.
* To perform projection mapping with multiple projectors and/or Kinect cameras. 
* To perform user view-dependent projection mapping on both static and moving scenes. 

In the remainder of this document and in our Unity scripts we use the *“RAT”* acronym as an abbreviation of *“RoomAlive Toolkit”*. 

## Components of This Toolkit:

1. **KinectV2Server** (C#) – a standalone tool that handles obtaining images from Kinect device, and streaming them via TCP sockets to the Unity scenes. There is no dependency on Unity for this particular tool. 
2. **RoomAlive Toolkit scripts and shaders for Unity** (C#) – these provide capabilities to receive Kinect data, create unity scenes based on pre-existing calibration data, as well as perform view-dependent projection mapping for tracked users. These scripts are organized in in two parts:
  - *Assets\RoomAliveToolkit* - These are the core required scripts and shaders of the toolkit. 
  - *Assets\RoomAliveToolkit_Examples* - These are *optional* scripts and scenes that contain the pre-assembled example scenes. This folder can be safely removed in new project if those examples are not required.  

## Prerequisites

* **Unity 5.5** (or better)
* **Visual Studio 2015 Community Edition** (or better)
* **Kinect for Windows v2 SDK**
* **ProCamCalibration** from RoomAlive Toolkit (for obtaining the calibration between projectors and Kinect cameras)

*Please note:* The KinectV2Server project uses SharpDX and Math.NET Numerics packages. These will be downloaded and installed automatically via NuGet when RoomAlive Toolkit is built.
Upon downloading the code, please build KinectV2Server project in Visual Studio. 

## Package vs. Code

A convenient way to import all this code into your Unity project is by importing our pre-compiled [Unity package](..\Packages\RoomAliveToolkit.unitypackage)

## Scene Examples

The tutorials below will guide you how to build up a new Unity scene from scratch, but if you’d like to jump in and see a basic (already pre-configured) example RAT scene in Unity, open up the RoomAliveUnity project and take a look at the scenes in `Assets\RoomAliveToolkit_Examples\Scenes\`. There are 2 example scenes in our project:

* *TestRATScene1x1* - This scene is a complete example with 1 projector + 1 Kinect camera + 1 user with projection mapping enabled. 
* *TestRATScene3x3* - This scene is a complete example with 3 projectors + 3 Kinect cameras + 1 user with projection mapping enabled.

*Please note:* These example scenes use the calibration and obj files from our office spaces, and will therefore not be correct for your space and projection mapping will not look correct. They are a good example of what is possible with our toolkit. 


# Tutorial #1: Setting Up a Basic Scene with RoomAlive Toolkit for Unity

This tutorial demonstrates how to setup a Unity scene given a calibration file from RoomAlive Toolkit. This scene will consists of several game objects representing the following:

* Kinect cameras in the room
* projectors in the room
* users in the room (typically acquired and tracked through Kinect's skeletal tracking)
* static geometry of the room (i.e., the mesh obj file saved during calbration)
* dynamic geometry of the room (i.e., the depth meshes assembled on the fly from streaming Kinect cameras)

There a lot of different connections and interdependencies in setting all projectors and cameras up front so we have created a helper script to automate much of this process *RATSceneSetup*. While useful, this script is not a required component of any scene and all connections could also be done manually in the editor. However, it is definitely nice to click a few buttons to make sure all is done for you.  

## Room Calibration

We first need to obtain the calibration of your room setup. This step uses [CalibrateEnsamble](https://github.com/Kinect/RoomAliveToolkit/tree/master/ProCamCalibration/CalibrateEnsamble) tool from RoomAliveToolkit. 

Please follow the instructions [here](https://github.com/Kinect/RoomAliveToolkit#tutorial-calibrating-one-camera-and-one-projector) from RoomAlive Toolkit on how to calibrate your room setup.  

If you have multiple cameras and/or multiple projectors, see the detailed instructions [here](https://github.com/Kinect/RoomAliveToolkit#calibrating-mutiple-cameras-and-multiple-projectors).

It is important to carefully configure your cameras and projectors in the room to ensure that there is substantial overlap between projectors and cameras. Both the color camera and the depth cameras in each Kinect device must observe a good portion of the projected surface in order for calibration to succeed.

Once you finish your calibration in the CalibrateEnsamble.exe:

1.	Make sure you save the resulting calibration (`File->Save`)
2.	Make sure you save the OBJ file of your configuration (`File->Save to OBJ`).
3.	Copy the resulting calibration and OBJ file (including the .xml, .obj, .jpg, and .mat files) into the Assets\Resources\\{YourCalibrationName}\ directory of your Unity project. 
4.	(optional) Close ProjectorServer.exe, CalibrateEnsamble.exe or KinectServer.exe applications that are running. They are not needed anymore, but (if desired) they can be left running as they do not interfere with Unity.

## Scene Setup

To setup your Unity scene, do the following steps:

1.	Open a new scene and save it. 
2.	Make sure there is a RoomAliveToolkit directory in the Assets directory of your project. 
3.	Make sure that you have copied the calibration file and OBJ files (including the .xml, .obj, .jpg, and .mat files) into the Assets\Resources\{YourCalibrationName}\ directory of your Unity project.  (see *Room Calibration* section above). You could also use our sample calibration data (“office3107”) if you do not have calibration handy.
4.	Create a new empty object in your scene and name it “MyRoom”. This will be the base origin of your room. 
5.	Disable (or delete) MainCamera game object. You won’t need it since each projector (and each user) is a camera.  
6.	Reset the position and orientation of MyRoom object to ensure it is at the origin.
7.	Add the following two components (scripts) to the MyRoom object (you can use `Add Component->RoomAliveToolkit->{ScriptName}` in the Inspector to quickly find the scripts from our toolkit):
  * *RATCalibrationData*
  * *RATSceneSetup*
8.	Find the calibration xml file in your project view and drag it into Calibration field of *RATCalibrationData* component. 
9.	In *RATCalibrationData*, press `Reload Calibration Data` button. You should see `Loaded: True` message right below the button if it all goes well. 
10.	In *RATSceneSetup*, press `Use Default 3D Models` button. You should see two 3D models (*kinect* and *projector*) appear linked right above to the `Kinect Model` and `Projector Model` respectively. 
11.	In *RATSceneSetup*, make sure that the component says `Ready` on the bottom. If not, press `Reload Calibration Data` button again. 
12.	If *RATSceneSetup* says `Ready`, press `Build RoomAlive Scene` button. 
13. You should now have a complete scene created in *MyRoom* object. The scene will show you your Kinects and projectors, at the locations in your room as determiened by the calibration. Our script takes care of correctly connecting all behaviors in the scene. Your MyRoom object inspector should look something like this: ![MyRoom Scripts](Images/MyRoomScripts.png?raw=true)
14.	To add the 3D model of your room into the scene, simply drag the obj file under *MyRoom* object. 
15.	(optional) You should add a directional light above so that you can see the object better. 
16.	You can now inspect your scene hierarchy which should include at least one Kinect object and one projector. In the scene views, both the projector and the Kinect will be visible as a 3D models and they should be located at the exact location where they are physically in your room. 
17.	If you are using our *TestRATScene1x1* calibration (officeTest.xml and officeTest.obj) your scene will look something like this: 

![Test Scene in Unity](Images/TestScene1x1NoUser.png?raw=true) 

## *KinectV2Server*: Streaming Kinect Data to Unity 

Please build *KinectV2Server* project. Then start the executable `KinectV2Server.exe` on each machine connected to the Kinect camera. In contrast to running the simple `KinectServer.exe` in the Calibration Ensamble example, this server has a GUI that lets you controll a bunch of parameters regarding what data is streamed and how it is encoded. 

![KinectV2Server](Images/KinectV2Server.png?raw=true)

Make sure you are using JPEG color compression (from the drop down menu).  

Make sure you are not in view of the Kinect and capture the background of your empty scene (`Background->Acquire Background` menu option). Then press `File->Save Settings` and leave the server running in the background. 

## Running the Scene

If you have followed the above instructions, you should now have setup a simple scene. 

When you run it (from the Unity Editor), the *Game* view should be displaying black since we haven’t setup any projection mapping yet, but the *Scene* view should display both the obj file and the real-time Kinect depth mesh from your Kinect camera. If you walk in front of Kinect at this point, you should be able to see yourself in the *Scene* view. 

# How Does View-Dependent Projection Mapping Work?

*View-Dependent Projection Mapping* is the idea of using the projectors in the room to display virtual content that appears perspectively correct from a single viewpoint (we call that a 'user view'). For this to work, one needs to know the precise calibration of projectors, where the user is looking and what the real geometry of the scene is (the objects and walls of the room) in order to be able to perform the correct distortions of the projected image. 

Here is the quick summary of various steps involved in view-dependent projection mapping:

* A 'user view' off-screen render is performed. This is the 'target' or 'desired' visual the user should see after projection onto a possibly non-flat surface. When rendering 3D virtual objects, this will likely require the user's head position.
* A graphics projection matrix is assembled for each projector in the ensemble. This uses the projector intrinsics, and, because the principal point of the projector is most likely not at the center of the projected image, uses an 'off-center' or 'oblique' style perspective projection matrix.
* The projector's projection matrix is combined with calibrated projector and depth camera pose information to create a transformation matrix mapping a 3D point in the coordinate frame of a given depth camera to a 3D point in the projector's view volume.
* A second transformation matrix is assembled, mapping a point in a given depth camera's coordinate system to the user's view volume. This is used to compute the texture coordinates into the 'user view' (above) associated with each 3D depth camera point.
* Vertex and geometry shaders use the above transformations to render a depth image to transformed vertices and texture coordinates for a given projector and a given depth camera. Essentially, the shaders render the receiving surface of the projected light, with a texture that is calcuated to match the 'user view' from the user's point of view, as projected by the projector.
* A projector's final rendering is performed by rendering each Kinect depth image using the above shaders. This procedure is performed for all projectors in the ensemble. Note that in this process, the depth images may be updated every frame; this is possible because the calibration and projection mapping process is fundamentally 3D in nature. 

# Tutorial #2: User Tracking and View-Dependent Projection Mapping

In Tutorial #1, we created a simple RoomAlive Toolkit scene. This tutorial builds upon that work by extending it to include a single tracked user and perform view-dependent projection mapping.

This tutorial is a direct continuation from the last step in Tutorial #1. 

## Setup a Tracked User

The next step is to setup a node in the scene graph which will represent a user in the scene. This node's position and orientation will be updated by the Kinect. Furthermore, this mode will contain a *Camera* which will enable offscreen rendering of the user's view in the scene. This offscreen render target is then used in the projection mapping step (by each projector) to determine what needs to be projected for the user to perceive the projection as view-dependent content. 

1. In MyScene object, in *RATSceneSetup* component click on `Add User` button. 
2. This will add a new user under MyRoom node, with the following components added (and properly connected):
  * *RATUser*
  * *RATUserViewCamera*
  * *RATProjectionPass* (2x) - this script defines specific shaders to be used in two specific stages of projection mapping for specific real world geometry (e.g., the room 3D mesh or the depth meshes from Kinect). The two stages are *user view rendering* and *projection rendering*. 

## Configuring Unity Layers

In order for projection mapping to work properly, one needs to specify what objects will be visible at what rendering pass. For example, users should typically only be viewing “virtual” objects since they can already see the real world (static room geometry). However; those virtual objects should be rendered (pasted on top of) some real world geometry which can in term be static (pre-acquired) or dynamic (acquired at run time from Kinect cameras).  To enable this selective rendering functionality, Unity uses the concept of layers which allow each *Camera* in the scene to perform culling based on the selected layers. 

Our fully configured scene requires 4 extra layers. Unity packages do not allow you to save and set layers automatically, so this step needs to be repeated in each new Unity project. 

Create 4 new layers in your scene (`Inspector->Layers->Add Layer…`), and name them:

1. **StaticSurfaces** – for existing static room geometry that is loaded from a file (obj mesh for example)
2. **DynamicSurfaces** – for dynamic depth meshes (from Kinect cameras) that represent the physical geometry that gets changed every frame
3. **Virtual3DObjects** – for virtual 3D objects that will be perspective mapped from the perspective of the user
4. **VirtualTextures** – for virtual objects that should be texture mapped onto existing surfaces (these objects will be rendered as flat user-independent layers, kind of like stickers on the physical geometry).

 ![Layers](Images/Layers.png?raw=true)

Then set the objects in your scene to the appropriate layer:

1. Find the root of the 3D scene model file (in our example “officeTest”) and set that object (and all children) to `StaticSurfaces` layer. 
2. Find all *DepthMesh* objects in the scene (basically any object with RATDepthMesh component) and set them (and all children) to the `DynamicSurfaces` layer.
3. Find the objects in the scene that you want projeciton map, i.e., to project as a virtual 3D object from the perspective of the user. Set those objects to `Virtual3DObjects` layer. 
4. Find the objects in the scene that you want to texture map on top of existing geometry without needing them to be perspectively rendered (e.g., a virtual map on the wall). Set those objects to `VirtualTextures` layer. 

## Configure Culling Layers in User's Projection Passes

The final step is to configure the culling masks of all different cameras in the scene (including the user's view and all projectors). 

1. In *RATProjectionManager* (a component of MyScene) set `Texture Layers = VirtualTextures`.
2. (optional) Add one new 3D object to the scene, something that you’d like to projection map. For example, add a 3D cube to the scene and position it somewhere in front of your static geometry. Add this object to the `Virtual3DObjects` layer.  
3. (optional) Add one plane object (sized appropriately and placed in front of some wall in the scene). Add that plane to `VirtualTextures` layer. This is a view independent layer which will appear like a sticker projection in the scene, 
4. Each User object in your scene should have one *RAT User View Camera* component and two *RAT Projection Pass* components. Configure each as follows: 
  * In *RATUserViewCamera* set `Culling Mask = Virtual3DObjects`
  * In first *RATProjectionPass* (first added script) set `Target Surface Layers = StaticSurfaces` (make sure you uncheck `Default`). Then click on `Set Static Defaults` button.
  * In second *RATProjectionPass* (second added script) set `Target Surface Layers = DynamicSurfaces` (make sure you uncheck `Default`). Then click on `Set Dynamic Defaults` button.
5. The User configuration should now look like this:
 
![User Configuration](Images/UserConfiguration.png?raw=true)

## *RATProjectionPass* Explained

*RATProjectionPass* script defines specific shaders to be used in two specific stages of projection mapping for specific real world geometry (e.g., the room 3D mesh or the depth meshes from Kinect). The two stages are *user view rendering* and *projection rendering*. 

I.e., for a specific layer or layers, this script specifies what shaders to use when rendering the user's view into an offscreen render target (`User View Shader`) and also when doing the projection mapping in each projector (`Projection Shader`). 

Each *RATUserView* can have multiple *RATProjectionPass* scripts attached and those can be controlled by inspecting 'ProjectionLayers' list in the *RATUserViewCamera* inspector. 

- Why is *RATProjectionPass* component needed? Why not just use different materials on objects in the scene?

Normally, specifying shaders in Unity for geometry in the scene is done through materials. Materials define the shaders to render that particular object regardless of the camera used to render them. 

However, for projection mapping to work, we need different shaders to be used for the same object when it is rendered by a different camera in the scene. In essence, think of projection passes like camera-dependent materials that operate on entire layers (i.e., multiple objects) and are changing shaders dependent on the camera that happens to be rendering them.  

- Why do we need different materials per camera? 

There are several reasons. For example, we want to see the 3D room geometry in the Scene view with captured textures, but when rendering from the perspective of the user, the desired effect is for everything that is *real world* to be rendered black so that the projectors are not re-projecting the textures of the real objects on top of those real objects.  
Another reason is that in the projection mapping pass, the colors of the geometry will be taken from the user view texture and not from the geometry itself. All of these are accomplished by different cameras, and require different shaders. 

## Running the Scene

Here is the final scene graph for the assembled project (*TestRATScene1x1*), including a test FloorPlan model (*Virtual3DObject* layer) and a World Map texture (*VirtualTextures* layer) on the wall. 

![Test Scene 1x1](Images/TestScene1x1.png?raw=true)

Here is another example (*TestRATScene3x3*), this time with 3 projectors and 3 Kinect cameras. The scene includes the same 3D virtual objects a test FloorPlan model and a World Map texture on the wall. 

![Test Scene 3x3](Images/TestScene3x3.png?raw=true)

If you run the project now, you should see some projection mapped object in your *Game* view. To see how to move the Game window to the target display and thus see the scene projected directly on top of your room, please check out section 7 on Game Window Management. 

If there is no user in front of the Kinect camera, the projection mapping will be done from the perspective of the Kinect camera itself. 

If the user is in front of the Kinect camera (and actively tracked) the projection mapping will be rendered from their perspective. 

There are a few tweaks to try:

1. Each *RATUser* can be given a `Look At` object to specify where the user is looking at. Try setting a small empty object in the scene somewhere in the middle of your captured geometry for the user to always focus on that.
2. Sometimes the arrangement of Kinect + projector is not optimal for tracking the user. I’d suggest you manually rotate the Kinect camera both in the physical world and in your Unity scene by 180 degrees. This way (as long as you do not move your projector or rebuild your RAT scene), you should be able to move and be tracked behind the projector and see the projection mapping projected on the wall. In this case, you might want to disable the DepthMeshes from rendering since they might cause artifacts. 

# Handling the Game Window and Multi-Display Configurations

Handling the output on multiple projectors is different depending on whether you are running in the Unity Editor or as a standalone application (from a compiled executable). 

## Running the Game in the Unity Editor

If you run your scene in the Editor, the output will be displayed in the Game window. 

First, make sure that the *RATProjectionManager* `Screen Setup` is set to *Editor*. Then move the Game window to the desired location on the projection display. 

To make this alignmnet pixel precise, RoomAlive Toolkit for Unity contains a utility called *RATMoveGameWindow* (select `Window->Move Game Window` from the menu). Please dock that tool window in your interface for best performance. 

![RATMoveGameWindow](Images/RATMoveGameWindow.png?raw=true)

In that tool, you can specify the location as well as size of the desired location of the Game window and then simply press `Move Game Window` button to dock it there. These coordinates can be saved (and loaded) for your convenience. 

### Running on Multiple Projectors in the Unity Editor

There is no way (yet) to directly output the Game window over multiple displays, so one needs to create arrange the displays contiguously in Windows, and then simply span the Game window across from them. E.g., if there are 3 HD projectors in a row, one can setup the Game window to dock over

*Please note:* there might be a limitation of the maximum texture size for the Game window in the Unity Editor, so carefully tile the displays in Windows (potentially in multiple rows) to fit them into those bounds. This limitation does not seem to be there when the game is run as a standalone application (outside of the Unity Editor). 

### Setting up the Viewports

If the Game window spans multiple projectors, each projector needs to render only to a portion of that Game window. This is handled by setting the correct *screen viewports* in *RATProjectionManager* (note the values are 0-1). 

Here are the 3 viewports setup for a scene consisting of 3 projectors arranged side by side:
![Projection Viewports](Images/ProjectionViewports.png?raw=true)

## Running as the Standalone Application

In the Editor set the *RATProjectionManager* `Screen Setup` to *Multi Display*. Then build your game. 

Assuming that the configuration of projector displays has not changed between the time you ran your calibration and the time you run your game, each projector should now have a correct portion of the game displayed on it. 

If the arrangement has changed, then you will need to manually edit the calibration XML file to se the desired display numbers according to your current projector display numbers. 


