# RoomAlive Toolkit README

The RoomAlive Toolkit enables creation of dynamic projection mapping experiences. This toolkit has been in internal use at Microsoft Research for several years and has been used in a variety of interactive projection mapping projects such as [RoomAlive](https://www.youtube.com/watch?v=ILb5ExBzHqw), [IllumiRoom](https://www.youtube.com/watch?v=re1EatGRV0w), [ManoAMano](https://www.youtube.com/watch?v=Df7fZAYVAIE), [Beamatron](https://www.youtube.com/watch?v=Z4bdrG8S1FM) and [Room2Room](https://www.youtube.com/watch?v=tRzOqTRxoek). 

The toolkit consists of two separate projects:

* **[ProCamCalibration](ProCamCalibration/)** - This C# project can be used to calibrate multiple projectors and Kinect cameras in a room to enable immersive, dynamic projection mapping experiences. The codebase also includes a simple projection mapping sample using Direct3D.
* **[RoomAlive Toolkit for Unity](RoomAliveToolkitForUnity/)** - RoomAlive Toolkit for Unity contains is a set of Unity scripts and tools that enable immersive, dynamic projection mapping experiences, based on the projection-camera calibration from ProCamCalibration. This project also includes a tool to stream and render Kinect depth data to Unity. 

Here is an example scene from our *RoomAlive* project to illustrate what is possible (this one uses 6 projectors and 6 Kinect cameras):

![RoomAlive Scene](RoomAliveToolkitForUnity/docs/images/Roomalive.png?raw=true) 

## Development Status

This project is under development. The current release is in beta and all APIs are subject to change. The next major features might include compute shaders for handling all depth meshes as a single unified vertex buffer, radiometric compensation across dynamic scnenes, support for Unity and Unreal Engines. We welcome contributions!

## Citations

The RoomAlive Project was started in the summer of 2013 with a group of superstar interns. If you are looking for a reference to that original work, please cite: 

```
@inproceedings{Jones:2014:RME:2642918.2647383,
 author = {Jones, Brett and Sodhi, Rajinder and Murdock, Michael and Mehra, Ravish and Benko, Hrvoje and Wilson, Andrew and Ofek, Eyal and MacIntyre, Blair and Raghuvanshi, Nikunj and Shapira, Lior},
 title = {RoomAlive: Magical Experiences Enabled by Scalable, Adaptive Projector-camera Units},
 booktitle = {Proceedings of the 27th Annual ACM Symposium on User Interface Software and Technology},
 series = {UIST '14},
 year = {2014},
 isbn = {978-1-4503-3069-5},
 location = {Honolulu, Hawaii, USA},
 pages = {637--644},
 numpages = {8},
 url = {http://doi.acm.org/10.1145/2642918.2647383},
 doi = {10.1145/2642918.2647383},
 acmid = {2647383},
 publisher = {ACM},
 address = {New York, NY, USA},
 keywords = {projection mapping, projector-camera system, spatial augmented reality},
} 
```
## Contribute

We welcome contributions to help advance projection mapping research frontier! 

* [File an issue](https://github.com/Microsoft/RoomAliveToolkit/issues) first so we are aware about change you want to make and possibly guide you. Please include these log files when you report an issue.
* Use [usual steps](https://akrabat.com/the-beginners-guide-to-contributing-to-a-github-project/) to make changes just like other GitHub projects.
* Clean compile your changes on Windows and test basic operations.
* When your pull request is created, you might get prompted to one-time sign Contributor License Agreement (CLA) unless changes are minor. It's very simple and takes less than a minute.
* If your pull request gets a conflict, please resolve it.
* Watch for any comments on your pull request.
* Please try and limit your changes to small number of files. We need to review every line of your change and we can't reasonably do that if you make requests with huge number of changes.
* Do not make just cosmetic changes. We will generally reject pull requests with only cosmetic changes.

## License

This project is licensed under [MIT license](LICENSE). 

