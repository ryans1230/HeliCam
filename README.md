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


### Commands
- `/heli reset` - Triggers leaving the camera, if you are in it.
- `/heli help` - Displays the help text.
- `/heli clear` - Clears all markers, and the speed marker/calculation.


### Config
- `FovMax` - the widest the camera can go
- `FovMin` - the tightest the camera can go
- `ZoomSpeed` - how much the camera zooms in each time
- `SpeedLR` - how fast the camera will move left and right
- `SpeedUD` - how fast the camera will move up and down
- `TextY` - the upper limit of text to be drawn in the box at the bottom of the camera
- `MaxDist` - the maximum distance a lock can be acquired/held
- `AllowCamera` - whether or not to allow entering/using the camera
- `AllowMarkers` - whether or not to allow the setting of custom markers while using the camera
- `AllowPedLocking` - whether or not to allow locking onto peds
- `AllowRappel` - whether or not to allow rappelling from the rear seats
- `AllowSpeedCalculations` - whether or not to allow estimated speed calculations
- `AllowSpotlights` - whether or not to allow the use of the spotlight from the helicopter
- `UseRealisticFLIR` - whether or not to use a more realistic FLIR/thermal camera
- `AircraftHashes` - List of custom aircraft models to support
- `HelicopterHashes` - List of custom helicopter models to support


### PR's Welcome