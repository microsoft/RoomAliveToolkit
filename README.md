# RoomAlive Toolkit README

The RoomAlive Toolkit is a set of projects that enable creation of dynamic projection mapping experiences. This toolkit has been in internal use at Microsoft Research for several years and has been used in a variety of interactive projection mapping projects such as [RoomAlive](https://www.youtube.com/watch?v=ILb5ExBzHqw), [IllumiRoom](https://www.youtube.com/watch?v=re1EatGRV0w), [ManoAMano](https://www.youtube.com/watch?v=Df7fZAYVAIE), [Beamatron](https://www.youtube.com/watch?v=Z4bdrG8S1FM) and [Room2Room](https://www.youtube.com/watch?v=tRzOqTRxoek). 

The core functionality of this toolkit consists of two separate projects:

* **[ProCamCalibration](ProCamCalibration/)** - This C# project can be used to calibrate multiple projectors and Kinect cameras in a room to enable immersive, dynamic projection mapping experiences. The codebase also includes a simple projection mapping sample.
* **[RoomAlive Toolkit for Unity](RoomAliveToolkitForUnity/)** - RoomAlive Toolkit for Unity contains is a set of Unity scripts and tools that enable immersive, dynamic projection mapping experiences, based on the projection-camera calibration from ProCamCalibration. This project also includes a tool to stream and render Kinect depth data to Unity. 

Here is an example scene from our *RoomAlive* project to illustrate what is possible (this one uses 6 projectors and 6 Kinect cameras):
![RoomAlive Scene](RoomAliveToolkitforUnity/docs/Images/RoomAlive.png?raw=true) 
