# RoomAlive Toolkit README

The RoomAlive Toolkit calibrates multiple projectors and cameras to enable immersive, dynamic projection mapping experiences such as RoomAlive. It also includes a simple projection mapping sample.

This document has a few things you should know about using the toolkit's projector/camera calibration, and gives a tutorial on how to calibrate one projector and Kinect sensor (AKA 'camera').

# Prerequisites

* Visual Studio 2015 Community Edition (or better)
* Kinect for Windows v2 SDK

The project uses SharpDX and Math.NET Numerics packages. These will be downloaded and installed automatically via NuGet when RoomAlive Toolkit is built.

The 'Shaders' project requires Visual C++. Note that in Visual Studio 2015, Visual C++ is not installed by default. You may be prompted to install the necessary components when building the 'Shaders' project of the RoomAlive Toolkit.

# Tutorial: Calibrating One Camera and One Projector

We outline the procedure for calibrating one projector and one camera. While one of the main features of RoomAlive Toolkit is support for multiple cameras and multiple projectors, this minimal configuration is a good place to start.

## Room Setup

Place the Kinect v2 sensor so that it views most of the projected image, and so that the projected image occupies most of the Kinect's viweable area. Precise alignment is not critical (that's part of the point of calibration, right?), but both the Kinect color camera and Kinect depth camera must observe a good portion of the projected image in order for calibration to succeed. It may be helpful to run the Kinect SDK’s Color Basics sample to line things up.

*Important*: By default, the calibration procedure will attempt to calculate projector focal length and principal point. This will succeed only if the projection surface is not flat! You must, at least once, calibrate with a non-flat surface to recover projector focal length and principal point. To create a non-flat projection surface, it may suffice to place some sizable objects, such as a couch, a few boxes, whatever you have on hand (yes, if this is your first calibration, you must do this!). In subsequent calibrations, if the projection surface is determined to be flat, the previously calculated focal length and principal point will be used.

Configure your projector so that it is in ‘desktop front’ projection mode, and make sure Windows is set to ‘Extend’ its desktop to the projector. Best results will be obtained when the projector is driven at its native resolution. Verify that the projector is not performing any keystone correction. Take a moment to focus the projector. If the projector has a zoom adjustment ring, do not move it after calibration is performed. It is a good idea to set it to one extreme or another, so that later you will know if it has been changed.

Though not mandatory, it is a good idea to remove the task bar from the projected display. To do that, right click the task bar, select Properties, and there find the option to show the taskbar on only the main display.

## KinectServer

Start KinectServer.exe and let it run. If you immediately receive a System.ServiceModel.AddressAccessDeniedException, try running KinectServer.exe as administrator (right click the KinectServer.exe icon).

## ProjectorServer

Start ProjectorServer.exe and let it run. If you immediately receive a System.ServiceModel.AddressAccessDeniedException, try running ProjectorServer.exe as administrator (right click the ProjectorServer.exe icon).

## Configure calibration.xml

Start CalibrateEnsemble.exe. This opens a user interface that allows you to acquire calibration images, perform the actual calibration calculation and inspect the results.

Create a new calibration by selecting File… New. This brings up a dialog that allows you to set the number of cameras and projectors for your setup. This defaults to 1 and 1, which is what we want for now. Hit Ok. Type a file name (say, ‘calibration.xml’). The calibration file is saved immediately. The calibration procedure will later create a number of new directories and files in whatever directory it finds calibration.xml. It is therefore a good idea to put calibration.xml in a new folder somewhere.

Open calibration.xml with a text editor (Visual Studio is a good choice). Note that there are ‘cameras’ and ‘projectors’ sections that list one of each. Feel free to put in a meaningful name for either, but the default names will be fine. Also note that each lists ‘hostnameOrAddress’ as ‘localhost’. Because both KinectServer and ProjectorServer are running locally, this default value is what we want.

The one thing you will need to change is the ‘displayIndex’ value under the single projector. If there is only one main display and the projector attached to the PC, the projector displayIndex probably should be ‘1’ (the main display being ‘0’). To verify this, return to CalibrateEnsemble.exe and select Setup… Show Projector Server Connected Displays. This will open a window on all available displays showing the displayIndex value for each (note that these values may not match the values reported in the Display Settings dialog in Windows). Select Setup… Hide Projector Server Connected Displays when you have noted the displayIndex associated with the projector.

*Important*: ProjectorServer and the calibration process is not High-DPI aware. If you find that the display showing the display index does not fill the entire projected display area, turn off DPI scaling (set to 100%), and try again.

Change the displayIndex value (again, probably ‘1’) and save the file.

## Acquire Calibration Images

Return to CalibrateEnsemble.exe and select File… Reload. 

With KinectServer and ProjectorServer still running, select Calibrate… Acquire. This should trigger the projection of various images, most importantly a series of black and white stripes called Gray codes. If you don’t see the stripes in the projected image, something is wrong. Double check the displayIndex value in calibration.xml. Also verify that ProjectorServer and KinectServer are still running. Did you remember to save calibration.xml above?

While this procedure is running the Calibrate menu will be disabled. When finished, the Calibrate menu will be re-enabled. 

Once the acquisition has completed, take a moment to browse the directory with calibrate.xml. Note that there are a few new directories with images of the Gray code projections and other information. In fact these files are everything the calibration procedure needs to proceed, and the program no longer requires KinectServer and ProjectorServer and you may close them.

## Run the Calibration

Select Calibrate… Solve to begin the calibration process. Watch as a bunch of debug text and numbers scroll by. The final RMS error reported should hopefully be some value less than 1.0.

Keep in mind the "important" note above regarding non-flat projection surfaces! You will need to calibrate each projector against a non-flat surface first, to recover focal length and principal point, before any subsequent calibration against planar scenes. Also, note that the projector section in the .xml file features a "lockIntrinsics" field which when true, ensures that calibration leaves focal length and principal point unchanged during calibration.

When the calibration solve is completed select File… Save. You are done with calibration.

## Inspect the Results

If you are still running CalibrateEnsemble.exe, select File…Reload, else start it, select File Open… and find calibration.xml. Now the left pane of the window should show the depth and color rendered as a textured mesh. Use W, A, S, D, E, C keys and drag with the mouse to move the graphics camera around to inspect the quality of the mesh. Note that the color image used resembles the first thing the projector shows in the Acquire phase of calibration. That’s because it is the color image acquired by the Kinect color camera when that image was shown by the projector.

The projection matrix of the graphics camera can be changed with the View menu. It defaults to an ordinary perspective projection. Selecting the second option in the View menu (‘Projector name’, where name is the name found in the .xml file) sets the projection and view matrices to match that of the projector, as calculated in the calibration procedure. If the calibration was successful, the white projected frame should fall just inside the border of the rendered frame. If you don’t understand why that should be the case, consider that a projector lens has a focal length and principal point (or optical center) just like a camera, and setting the graphics projection and view matrix to that of the projector essentially renders the scene from the projector’s precise point of view, as if it were a camera. It should therefore ‘see’ precisely what it projected, as it was projected. Meditate on this if necessary, because it is a fundamental point.

# ProjectionMapping Sample

The projection mapping sample included in the RoomAlive Toolkit uses the calibration information. Pass the path of your calibration .xml as a command line argument so that the sample can find it. The main window shows the target rendering used in projection mapping.

In Visual Studio, look at the Settings.settings file under the Properties folder in the ProjectionMappingSample project. There are a few settings that control how the sample runs:

- ThreeDObjectEnabled: A simple 3D object (see FloorPlan.obj) is rendered.
- WobbleEffectEnabled: An edge-enhanced color image of the scene is rendered. When the main window is clicked, a momentary sinusoidal distortion of the view is applied. This effect is similar to that of the IllumiRoom Wobble effect. If LocalHeadTrackingEnabled is true, the effect may also be triggered when the tracked user brings their hands together.
- LocalHeadTrackingEnabled: If a local Kinect sensor is detected, the head position of nearest tracked body is used in rendering the user’s view. With ThreeDObjectEnabled this creates a viewpoint dependent rendering effect, where the object appears to hover in front of the user. If you have only one Kinect camera, a good practice is to perform calibration with the camera viewing the projection. Then, when running the sample, turn the camera in place 180 degrees to view the user (in this case LiveDepthEnabled should be set to false). You may need to tweak how the head position is translated in the code sample.
- LiveDepthEnabled: The depth image used in projection mapping is updated in real time (KinectServer must be running on the host listed in calibration.xml), otherwise the static, pre-recorded depth data from the calibration procedure is used. If this option is enabled, you should be able to move objects in the scene and the projection mapping should keep up with those changes.
- FullScreenEnabled: Opens the rendered graphics window full screen on the projectors specified in calibration.xml. When false, the windows are instead opened in smaller windows with the main UI. This can be useful if you are away from your projector.
- DesktopDuplicationEnabled: Uses the Desktop Duplication API to copy the contents of some other window on the desktop as the user's view. This allows the use of any program to perform rendering; for example, Unity or Processing. At the moment, the window is found by matching a hardcoded window title, and adjustments to the copied rectangle dimensions are hard coded.

# Calibrating Mutiple Cameras and Multiple Projectors

The RoomAlive Toolkit projector/camera calibration tool has been designed from the ground up to handle multiple simultaneous projector and camera combinations. Immersive experiences such as those illustrated by RoomAlive are possible.

To calibrate a multiple camera/multiple projector setup, keep the following in mind:

- Each Kinect v2 must be hosted on a separate PC. Each PC must be running KinectServer to make its depth and color available to CalibrateEnsemble. You may use Remote Desktop to ease launching KinectServer on multiple machines, but beware that there is a known issue with the Kinect for Windows v2 SDK whereby you must tweak the RDP connection audio settings to "play on remote computer" to obtain reliable performance.
- ProjectorServer.exe has similarly been designed to address possibly multiple machines attached to multiple projectors and calibration will support this configuration. However, the projection mapping sample today supports local rendering on the same adapter only. If you want to render to more than 3 or 4 locally attached projectors you may need a distributed rendering framework.
- Create an .xml file from the New dialog, selecting the right number of projectors and cameras from before hitting OK. Edit the hostNameOrAddress fields of each of the cameras and projectors.
- The frustums of the projectors and cameras must overlap enough so that the calibration procedure can infer the overall arrangement of all cameras and projectors. In the case where 3 projectors and 3 cameras are lined up in a row, for example, the center camera must observe some portion of the left and right projections, and some portion of the center projection must overlap with the left and right projections (furthermore, each of these overlapping regions must be not be planar). We use the term ‘ensemble’ to denote the set of cameras and projectors that are calibrated together. We refer to a projector and the set of cameras that can see some part of the projector’s image as a ‘projector group’. An ensemble is thus a set of projector groups that share cameras.
- The first camera in the .xml is special in that it establishes the coordinate system for the ensemble. When creating a multi-camera .xml file from the user interface, the 4x4 pose matrix for the first camera is set to the identity. This places the first camera in a coordinate frame external to the ensemble. If you already have a global coordinate frame you favor, set the first camera’s pose matrix to its pose in this coordinate frame before running calibration. Calibration will not change it. The pose of the other cameras and projectors will be reported in the same coordinate frame. 
- The projection mapping sample handles multiple projector and multiple camera configurations (again, only local rendering). The sample picks up the configuration from the .xml file, supplied as a command line argument.

# How Does Calibration Work?

A full description of how the calibration works is beyond the sope of this README, but, briefly:

- During the Acquire phase of CalibrateEnsemble, each projector projects a series of Gray code patterns in turn. These are captured and saved by all Kinect color cameras. Gray code patterns are used to map from a given pixel coordinate in the Kinect color image to a pixel coordinate in the projector. All cameras observe the Gray code patterns in order to establish which cameras belong to a given 'projector group' (see above). Additionally, the depth image from each Kinect depth camera is saved.
-  CalibrateEnsemble recovers Kinect camera calibration information. This is used to compute the precise 3D coordinate of a given point in the depth image, and to map this 3D point to color camera coordinates.
-  At this point  CalibrateEnsemble has all the information it needs to perform its calibration, even if the cameras and projector are off or disconnected.
- During the Solve phase of CalibrateEnsemble, for each camera in the projector group, points in the saved depth camera image are transformed to 3D points. The 2D color camera coordinate is computed via the saved Kinect calibration information. These color camera coordinates are then assocated with projector coordinates by way of the Gray code mapping. The end result is a set of 3D points for each depth camera in the group, and their associated 2D projector coordinates. Now we are ready to solve for calibration parameters.
- Projector intrinsics (focal length, principle point) and camera extrinsics (depth camera pose, in the projector coordinate frame) are computed by minimizing the error in the projection of 3D points to projector points. Because this projection is nonlinear, a standard Levenberg-Marquardt optimization procedure is used. This is performed for each projector in turn.
- The 'ensemble' necessarily includes cameras that belong to multiple projector groups. These cameras can be used to put all camera and projector poses in the coordinate frame of the depth camera of the first Kinect listed in the .xml file. This is done via successive matrix compositions.
- The 'ensemble' can be thought of as a graph of projectors, where each projector is a node, and cameras that belong in more than one group establish an edge between projectors. The previous step of unifying the coordinate systems is done in a greedy fashion, with the consequence that there may be multiple possible estimates of a camera or projector's pose if this graph includes a cycle (consider a ring of projectors, or a 2x2 grid of projectors). To address this possible source of error, a final optimization is performed over all projectors, solving for a single pose for each projector and camera.

# How Does Projection Mapping Work?

Briefly:

- A 'user view' off-screen render is peformed. This is the 'target' or 'desired' visual the user should see after projection  onto a possibly non-flat surface. When rendering 3D virtual objects, this will likely require the user's head position.
- A graphics projection matrix is assembled for each projector in the ensemble. This uses the projector intrinsics, and, because the principal point of the projector is most likely not at the center of the projected image, uses an 'off-center' or 'oblique' style perspective projection matrix. 
- The projector's projection matrix is combined with calibrated projector and depth camera pose information to create a transformation matrix mapping a 3D point in the coordinate frame of a given depth camera to a 3D point in the projector's view volume.
- A second transformation matrix is assembled, mapping a point in a given depth camera's coordinate system to the user's view volume. This is used to compute the texture coordinates into the 'user view' (above) associated with each 3D depth camera point.
- Vertex and geometry shaders use the above transformations to render a depth image to transformed vertices and texture coordinates for a given projector and a given depth camera. Essentially, the shaders render the receiving surface of the projected light, with a texture that is calcuated to match the 'user view' from the user's point of view, as projected by the projector.
- A projector's final rendering is perfomed by rendering each Kinect depth image using the above shaders. This procedure is performed for all projectors in the ensemble. Note that in this process, the depth images may be updated every frame; this is possible because the calibration and projection mapping process is fundamentally 3D in nature.


# More Online Resources

[Channel9 pre-recorded talk introducing the RoomAlive Toolkit with Ben Lower and Andy Wilson] (http://channel9.msdn.com/Events/Build/2015/3-87)

[video of 3 camera/3 projector calibration](https://www.youtube.com/watch?v=9Ifnt3xM1J0)

The resulting calibration set can be downloaded [here](http://research.microsoft.com/~awilson/RoomAliveToolkit/calibration3x3 v2.zip). Try opening this in CalibrateEnsemble.exe.

[video of projection mapping sample, showing view dependent rendering of 3D object] (https://www.youtube.com/watch?v=9A18AxfC2tM)

[video of projection mapping sample, showing wobble effect] (https://www.youtube.com/watch?v=VzncaJcaTF0)

[RoomAlive video] (https://www.youtube.com/watch?v=ILb5ExBzHqw)
 
