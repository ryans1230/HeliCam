# HeliCam
HeliCam is an advanced aerial support camera for FiveM servers. It aims to create a realistic FLIR camera system within the limitations of Grand Theft Auto 5. This is a heavily modified fork of the original script by [mraes](https://forum.cfx.re/t/release-heli-script/24094).

### Features
- Speed estimator. Calculates the speed of a target based on time since it was at a known position.
- Crosshair position. Shows the road (and crossroad) of the current crosshair position.
- Ped locking. Lock on peds to balance out limitations.
- Custom markers. Set custom marks on the map while using the camera for whatever you need. This also get sent to everyone in the vehicle.
- Camera position. On-screen UI to show the camera's position relative to the aircraft's heading.
- Street overlay. Shows street names that are near the crosshair position.
- Better FLIR. Reduces the clarity of the thermal camera for a more realistic FLIR camera.
- Controller support. Xbox 360 and PS4 controller support for all camera operations.
- All the base features of mraes' script, including rappel. 



### Controls
- `E` - toggle the helicam view
- `G` - set a marker at the current crosshair position (limit 10)
- `H` - remove most recent marker
- `Space` - lock on the current entity (vehicle or ped)
- `V` - toggle street overlay
- `Scroll Up` - zoom camera in
- `Scroll Down` - zoom camera out
- `RMB` - change camera mode
- `Q` - reset camera position
- `TAB` - toggle speed calculations
- `F` - toggle spotlight
- `Num9` - increase spotlight size
- `Num7` - decrease spotlight size


### PR's Welcome
- Getting the camera's heading relative to true north.