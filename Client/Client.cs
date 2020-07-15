using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace HeliCam
{
    internal class Config
    {
        public float FovMax { get; set; }
        public float FovMin { get; set; }
        public float ZoomSpeed { get; set; }
        public float SpeedLR { get; set; }
        public float SpeedUD { get; set; }
        public float TextY { get; set; }
        public float MaxDist { get; set; }
        public bool AllowCamera { get; set; }
        public bool AllowMarkers { get; set; }
        public bool AllowPedLocking { get; set; }
        public bool AllowRappel { get; set; }
        public bool AllowSpeedCalculations { get; set; }
        public bool AllowSpotlights { get; set; }
        public bool UseRealisticFLIR { get; set; }
        public List<string> AircraftHashes { get; set; }
        public List<string> HelicopterHashes { get; set; }

        internal void LoadBackupConfig()
        {
            FovMax = 80.0f;
            FovMin = 5.0f;
            ZoomSpeed = 3.0f;
            SpeedLR = 5.0f;
            SpeedUD = 5.0f;
            TextY = 1.5f;
            MaxDist = 400f;
            AllowCamera = true;
            AllowMarkers = true;
            AllowPedLocking = true;
            AllowRappel = true;
            AllowSpeedCalculations = true;
            AllowSpotlights = true;
            UseRealisticFLIR = true;
            AircraftHashes = new List<string> { "mammatus", "dodo" };
            HelicopterHashes = new List<string>();
            Debug.WriteLine("loaded backup configuration successfully!");
            Debug.WriteLine(JsonConvert.SerializeObject(this));
        }
    }

    internal class Spotlight
    {
        public int VehicleId { get; set; }
        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }
        public float Radius { get; set; }

        internal Spotlight(int veh, Vector3 start, Vector3 end, float size)
        {
            VehicleId = veh;
            Start = start;
            End = end;
            Radius = size;
        }
    }

    public class Client : BaseScript
    {
        #region Variables
        private const Control CAM_TOGGLE = Control.Context;
        private const Control VISION_TOGGLE = Control.ScriptRRight;
        private const Control REPEL = Control.ParachuteSmoke;
        private const Control TOGGLE_ENTITY_LOCK = Control.CreatorAccept;
        private const Control TOGGLE_SPOTLIGHT = Control.VehicleExit;
        private readonly HashSet<Blip> _markers = new HashSet<Blip>();
        private readonly Config config;

        private bool _helicam, _calculateSpeed, _roadOverlay, _spotlightActive, _shouldRappel;
        private float _fov = 80f;
        private int _visionState = 0; // 0 normal, 1 nightmode, 2 thermal
        private Minimap _playerMap;
        private readonly Dictionary<string, List<Vector3>> _streetOverlay;
        private readonly Dictionary<int, Spotlight> _drawnSpotlights = new Dictionary<int, Spotlight>();
        private double _lastCamHeading, _lastCamTilt;
        private Tuple<int, Vector3> _speedMarker;

        private readonly List<ThermalBone> _thermalBones = new List<ThermalBone>
        {
            new ThermalBone("BONETAG_SPINE1", new Vector3(-0.25f), new Vector3(0.19f, 0.15f, 0.49f)),
            //new ThermalBone("BONETAG_PELVIS", new Vector3(-0.2f, -0.2f, 0.2f), new Vector3(0.2f, 0.2f, 0.6f)),
            //new ThermalBone("BONETAG_HEAD", new Vector3(-0.1f), new Vector3(0.1f, 0.2f, 0.2f)),
            //new ThermalBone("BONETAG_L_UPPERARM", new Vector3(-0.15f, -0.15f, -0.2f), new Vector3(0.15f, 0.15f, 0.2f)),
            //new ThermalBone("BONETAG_R_UPPERARM", new Vector3(-0.15f, -0.15f, -0.2f), new Vector3(0.15f, 0.15f, 0.2f)),
            //new ThermalBone("BONETAG_L_FOREARM", new Vector3(-0.08f, -0.08f, -0.2f), new Vector3(0.08f, 0.08f, 0.1f)),
            //new ThermalBone("BONETAG_R_FOREARM", new Vector3(-0.08f, -0.08f, -0.2f), new Vector3(0.08f, 0.08f, 0.1f)),
            new ThermalBone("wheel_lf", new Vector3(-0.3f), new Vector3(0.3f), ThermalType.WHEEL),
            new ThermalBone("wheel_rf", new Vector3(-0.3f), new Vector3(0.3f), ThermalType.WHEEL),
            new ThermalBone("wheel_lr", new Vector3(-0.3f), new Vector3(0.3f), ThermalType.WHEEL),
            new ThermalBone("wheel_rr", new Vector3(-0.3f), new Vector3(0.3f), ThermalType.WHEEL),
            //new ThermalBone("engine", new Vector3(-0.7f, -0.7f, -0.3f), new Vector3(0.7f, 0.7f, 0.4f), ThermalType.ENGINE),
            new ThermalBone("exhaust", new Vector3(-0.3f, -0.3f, -0.05f), new Vector3(0.3f, 0.3f, 0.2f), ThermalType.ENGINE)
        };
        private Vector3 _dummy = new Vector3(-0.3f);
        #endregion

        public Client()
        {
            string streets = LoadResourceFile(GetCurrentResourceName(), "streets.json") ?? "[]";
            try
            {
                _streetOverlay = JsonConvert.DeserializeObject<Dictionary<string, List<Vector3>>>(streets.Trim());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to read streets file: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine("Disabling map overlay");
                _streetOverlay = new Dictionary<string, List<Vector3>>();
            }

            string cfg = LoadResourceFile(GetCurrentResourceName(), "config.json") ?? "[]";
            try
            {
                config = JsonConvert.DeserializeObject<Config>(cfg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading config file. ");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine("Using safe configuration");
                config = new Config();
                config.LoadBackupConfig();
            }
        }

        [Command("heli")]
        internal void HeliCommand(int src, List<object> args, string raw)
        {
            if (args.Count == 0)
            {
                TriggerEvent("chat:addMessage", new
                {
                    args = new[] { "[^1HeliCam^7] Usage: /heli [option]. Available options: clear, reset, help." }
                });
                return;
            }

            string arg = args[0].ToString().ToLower();
            if (arg == "help")
            {
                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    type = "help"
                }));
            }
            else if (arg == "clear")
            {
                if (_calculateSpeed)
                {
                    Game.SetControlNormal(0, Control.ReplaySnapmaticPhoto, 200f);
                }

                if (_markers.Count != 0)
                {
                    foreach (Blip blip in _markers)
                    {
                        blip.Delete();
                    }
                    _markers.Clear();
                }
                TriggerServerEvent("helicam:removeAllMarkers", Game.PlayerPed.IsSittingInVehicle() ? Game.PlayerPed.CurrentVehicle.NetworkId : 0);
            }
            else if (arg == "reset")
            {
                if (_helicam)
                {
                    PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
                _helicam = false;
            }
        }

        #region Ticks
        [Tick]
        internal async Task EveryTick()
        {
            foreach (KeyValuePair<int, Spotlight> spotlight in _drawnSpotlights.ToArray())
            {
                if (Players[spotlight.Key] == null)
                {
                    _drawnSpotlights.Remove(spotlight.Key);
                    return;
                }

                if (World.GetDistance(Game.PlayerPed.Position, spotlight.Value.Start) < 1000f)
                {
                    DrawSpotLightWithShadow(spotlight.Value.Start.X, spotlight.Value.Start.Y, spotlight.Value.Start.Z, spotlight.Value.End.X, spotlight.Value.End.Y, spotlight.Value.End.Z, 255, 175, 110, 1000f, 10f, 0f, spotlight.Value.Radius, 1f, spotlight.Value.VehicleId);
                }
            }

            foreach (Blip mark in _markers)
            {
                World.DrawMarker(MarkerType.HorizontalCircleSkinny, mark.Position, Vector3.Zero, Vector3.Zero, new Vector3(10f), Color.FromArgb(175, 59, 231));
            }
            await Task.FromResult(0);
        }

        [Tick]
        internal async Task UpdateCache()
        {
            _playerMap = MinimapAnchor.GetMinimapAnchor();
            await Delay(2500);
        }

        [Tick]
        internal async Task ThermalTick()
        {
            if (_visionState == 2)
            {
                World.GetAllVehicles().Where(v => v != Game.PlayerPed.CurrentVehicle && v.IsEngineRunning).ToList().ForEach(v =>
                {
                    foreach (ThermalBone bone in _thermalBones)
                    {
                        if (bone.Type == ThermalType.ENGINE || (bone.Type == ThermalType.WHEEL && !v.IsStopped))
                        {
                            Vector3 bonePos = v.Bones[bone.BoneName].Position;
                            if (!bonePos.IsZero)
                            {
                                DrawThermal(bonePos.X + bone.StartPos.X, bonePos.Y + bone.StartPos.Y, bonePos.Z + bone.StartPos.Z, bonePos.X + bone.EndPos.X, bonePos.Y + bone.EndPos.Y, bonePos.Z + bone.EndPos.Z);
                            }
                        }
                    }
                });

                World.GetAllPeds().Where(p => p != Game.PlayerPed && (!p.IsSittingInVehicle() || p.CurrentVehicle.Model.IsBike || p.CurrentVehicle.Model.IsQuadbike || p.CurrentVehicle.Model.IsBoat || p.CurrentVehicle.Model.IsBicycle) && p.IsHuman).ToList().ForEach(p =>
                {
                    DrawThermal(p.Position.X - 0.3f, p.Position.Y - 0.3f, p.Position.Z - 0.8f, p.Position.X + 0.3f, p.Position.Y + 0.3f, p.Position.Z + 0.8f);

                    // ALTERNATE PED THERMAL - TAKES MORE FRAMES
                    /*foreach (ThermalBone bone in _thermalBones)
                    {
                        Vector3 bonePos = p.Bones[bone.BoneName].Position;
                        if (!bonePos.IsZero)
                        {
                            DrawThermal(bonePos.X + bone.StartPos.X, bonePos.Y + bone.StartPos.Y, bonePos.Z + bone.StartPos.Z, bonePos.X + bone.EndPos.X, bonePos.Y + bone.EndPos.Y, bonePos.Z + bone.EndPos.Z);
                        }
                    }*/
                });
            }
            else
            {
                await Delay(250);
            }
        }

        [Tick]
        internal async Task MainTick()
        {
            Ped player = Game.PlayerPed;

            if (IsPlayerInHeli() && player.CurrentVehicle.HeightAboveGround > 2.5f)
            {
                Vehicle heli = player.CurrentVehicle;

                if (Game.IsControlJustPressed(0, CAM_TOGGLE) && config.AllowCamera && !Game.IsControlPressed(0, Control.Aim))
                {
                    PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                    _helicam = true;
                }

                if (Game.IsControlJustPressed(0, REPEL) && config.AllowRappel)
                {
                    if (heli.GetPedOnSeat(VehicleSeat.LeftRear) == player || heli.GetPedOnSeat(VehicleSeat.RightRear) == player)
                    {
                        if (_shouldRappel)
                        {
                            PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            TaskRappelFromHeli(player.Handle, 1);
                        }
                        else
                        {
                            Screen.ShowNotification("Press again to rappel from helicopter.");
                            _shouldRappel = true;
                        }
                    }
                    else
                    {
                        Screen.ShowNotification("~r~Can't rappel from this seat!", true);
                        PlayManagedSoundFrontend("5_Second_Timer", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS");
                    }
                }
            }
            else
            {
                _shouldRappel = false;
            }

            if (_helicam)
            {
                SetTimecycleModifier("heliGunCam");
                SetTimecycleModifierStrength(0.3f);
                Scaleform scaleform = new Scaleform("HELI_CAM");
                while (!scaleform.IsLoaded)
                {
                    await Delay(1);
                }

                Vehicle heli = player.CurrentVehicle;
                Camera cam = new Camera(CreateCam("DEFAULT_SCRIPTED_FLY_CAMERA", true));
                cam.AttachTo(heli, new Vector3(0f, 0f, -1.5f));
                cam.FieldOfView = _fov;
                cam.Rotation = new Vector3(0f, 0f, heli.Heading);
                RenderScriptCams(true, false, 0, true, false);

                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    shown = true,
                    heli = heli.Model.IsHelicopter,
                    plane = !heli.Model.IsHelicopter
                }));

                TriggerEvent("HideHud");
                Entity lockedEntity = null;

                Vector3 hitPos = Vector3.Zero;
                Vector3 endPos = Vector3.Zero;

                Blip speedBlip = null;
                Blip crosshairs = World.CreateBlip(heli.Position);
                crosshairs.Sprite = (BlipSprite)123;
                crosshairs.Color = BlipColor.Red;
                crosshairs.Scale = 0.5f;
                crosshairs.Name = "Current Crosshair Position";
                crosshairs.Rotation = 0;

                DateTime lastLosTime = DateTime.Now;
                DateTime lockedTime = DateTime.Now;
                DateTime enterTime = DateTime.Now;

                SetNetworkIdExistsOnAllMachines(heli.NetworkId, true);

                while (_helicam && player.IsAlive && player.IsSittingInVehicle() && player.CurrentVehicle == heli && player.CurrentVehicle.HeightAboveGround > 2.5f)
                {
                    float zoomValue = 1.0f / (config.FovMax - config.FovMin) * (_fov - config.FovMin);

                    Game.DisableControlThisFrame(0, Control.NextCamera);
                    Game.DisableControlThisFrame(0, Control.VehicleSelectNextWeapon);
                    Game.DisableControlThisFrame(0, Control.VehicleCinCam);
                    Game.DisableControlThisFrame(0, Control.VehicleHeadlight);
                    Game.DisableControlThisFrame(0, REPEL);
                    Game.DisableControlThisFrame(0, TOGGLE_SPOTLIGHT);
                    Game.DisableControlThisFrame(0, Control.Phone);
                    heli.IsRadioEnabled = false;


                    if (Game.IsControlJustPressed(0, CAM_TOGGLE))
                    {
                        PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        _helicam = false;
                    }

                    if (Game.IsControlJustPressed(0, VISION_TOGGLE))
                    {
                        PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        ChangeVision();
                    }

                    if (Game.IsControlJustPressed(0, Control.ContextSecondary))
                    {
                        cam.Rotation = new Vector3(0f, 0f, heli.Heading);
                        _fov = 80f;
                    }

                    if (Game.IsControlJustPressed(0, Control.NextCamera))
                    {
                        _roadOverlay = !_roadOverlay;
                        if (_roadOverlay && _streetOverlay.Count == 0)
                        {
                            SendNuiMessage(JsonConvert.SerializeObject(new
                            {
                                type = "alert",
                                message = "Street overlay failed to load. Contact the server owner(s)."
                            }));
                        }
                    }

                    if (lockedEntity != null)
                    {
                        if (Entity.Exists(lockedEntity))
                        {
                            if ((lockedEntity.Model.IsPed && !config.AllowPedLocking) || lockedEntity.IsInWater)
                            {
                                lockedEntity = null;
                            }
                            else
                            {
                                if (HasEntityClearLosToEntity(heli.Handle, lockedEntity.Handle, 17))
                                {
                                    lastLosTime = DateTime.Now;
                                }

                                RenderInfo(lockedEntity);
                                hitPos = endPos = lockedEntity.Position;
                                string lockedTimeString = DateTime.Now.Subtract(lockedTime).ToString(@"mm\:ss");
                                RenderText(0.2f, 0.4f, $"~g~Locked ~w~{lockedTimeString}");

                                if (World.GetDistance(lockedEntity.Position, heli.Position) > config.MaxDist || Game.IsControlJustPressed(0, TOGGLE_ENTITY_LOCK) || DateTime.Now.Subtract(lastLosTime).Seconds > 5)
                                {
                                    Debug.WriteLine($"LOS: {DateTime.Now.Subtract(lastLosTime).Seconds}. Dist: {Math.Round(World.GetDistance(lockedEntity.Position, heli.Position))}");
                                    lockedEntity = null;
                                    lockedTime = new DateTime();
                                    cam.StopPointing();
                                    PlayManagedSoundFrontend("5_Second_Timer", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS");
                                }
                            }
                        }
                        else
                        {
                            lockedEntity = null;
                        }
                    }
                    else
                    {
                        RenderText(0.2f, 0.4f, $"~r~Unlocked");
                        CheckInputRotation(cam, zoomValue);
                        Tuple<Entity, Vector3, Vector3> detected = GetEntityInView(cam);
                        endPos = detected.Item3;
                        if (Entity.Exists(detected.Item1))
                        {
                            RenderInfo(detected.Item1);
                            if (Game.IsControlJustPressed(0, TOGGLE_ENTITY_LOCK))
                            {
                                PlayManagedSoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                lockedEntity = detected.Item1;
                                lockedTime = DateTime.Now;
                                cam.PointAt(lockedEntity);
                                lastLosTime = DateTime.Now;
                            }
                        }
                        if (!detected.Item2.IsZero)
                        {
                            hitPos = detected.Item2;
                        }
                    }

                    if (hitPos.IsZero)
                    {
                        crosshairs.Alpha = 0;
                    }
                    else
                    {
                        crosshairs.Alpha = 255;
                        crosshairs.Position = hitPos;
                    }

                    if (Game.IsControlJustPressed(0, Control.ReplaySnapmaticPhoto) && config.AllowSpeedCalculations)
                    {
                        if (hitPos.IsZero)
                        {
                            SendNuiMessage(JsonConvert.SerializeObject(new
                            {
                                type = "alert",
                                message = "You are not aiming at anything!"
                            }));
                        }
                        else
                        {
                            _calculateSpeed = !_calculateSpeed;
                            if (_calculateSpeed)
                            {
                                hitPos.Z += 0.1f;
                                speedBlip = World.CreateBlip(hitPos);
                                speedBlip.Color = BlipColor.MichaelBlue;
                                speedBlip.Sprite = BlipSprite.PoliceCar;
                                speedBlip.Name = $"Speed Marker {DateTime.Now.ToString("HH:MM:SS")}";
                                SetBlipDisplay(speedBlip.Handle, 2);
                                _speedMarker = new Tuple<int, Vector3>(Game.GameTime, speedBlip.Position);
                            }
                            else
                            {
                                if (speedBlip != null)
                                {
                                    speedBlip.Delete();
                                }
                                speedBlip = null;
                                _speedMarker = null;
                            }
                        }
                    }

                    TimeSpan timeInCam = DateTime.Now.Subtract(enterTime);
                    RenderText(0.01f, config.TextY - 0.1f, $"{DateTime.UtcNow.ToString($"MM/dd/yyyy\nHH:mm:ssZ")}\n~y~{timeInCam.ToString(@"mm\:ss")}", 0.3f);

                    float latPos = heli.Position.Y;
                    float lonPos = heli.Position.X;
                    string latText = "N";
                    string lonText = "E";
                    if (latPos < 0f)
                    {
                        latText = "S";
                        latPos = Math.Abs(latPos);
                    }
                    if (lonPos < 0f)
                    {
                        lonText = "W";
                        lonPos = Math.Abs(lonPos);
                    }
                    double aircraftHdg = 360 - Math.Round(heli.Heading);
                    RenderText(0.075f, config.TextY - 0.1f, $"Aircraft:\n{latText} {Math.Round(latPos, 2)}\n{lonText} {Math.Round(lonPos, 2)}\n{aircraftHdg}°  {Math.Ceiling(heli.HeightAboveGround * 3.2808f)}ft", 0.3f);

                    HandleZoom(cam);
                    RenderTargetPosInfo(hitPos);
                    if (config.AllowMarkers)
                    {
                        HandleMarkers(hitPos);
                    }
                    RenderRotation(heli, hitPos.IsZero ? endPos : hitPos, cam.Rotation);

                    if (_roadOverlay && _streetOverlay.Count > 0)
                    {
                        RenderStreetNames(hitPos == Vector3.Zero ? heli.Position : hitPos);
                    }

                    if (Game.IsDisabledControlJustPressed(0, TOGGLE_SPOTLIGHT) && config.AllowSpotlights)
                    {
                        _spotlightActive = !_spotlightActive;

                        if (!_spotlightActive)
                        {
                            TriggerServerEvent("helicam:spotlight:kill");
                        }
                        else
                        {
                            SendNuiMessage(JsonConvert.SerializeObject(new
                            {
                                type = "info",
                                message = $"Spotlight turned on"
                            }));
                        }
                    }

                    if (_spotlightActive && config.AllowSpotlights)
                    {
                        Vector3 spotlightDest = Entity.Exists(lockedEntity)
                            ? lockedEntity.Position - cam.Position
                            : (!hitPos.IsZero ? hitPos - cam.Position : endPos - cam.Position);
                        spotlightDest.Normalize();
                        TriggerServerEvent("helicam:spotlight:draw", heli.NetworkId, cam.Position, spotlightDest, 5f);
                    }

                    scaleform.CallFunction("SET_ALT_FOV_HEADING", heli.Position.Z, zoomValue, cam.Rotation.Z);
                    scaleform.Render2D();

                    hitPos = Vector3.Zero;
                    await Delay(0);
                }

                // No longer in cam
                if (_spotlightActive)
                {
                    _spotlightActive = false;
                    TriggerServerEvent("helicam:spotlight:kill");
                }

                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    shown = false
                }));

                TriggerEvent("ShowHud");
                _helicam = false;
                if (speedBlip != null)
                {
                    speedBlip.Delete();
                }
                crosshairs.Delete();
                _speedMarker = null;
                _calculateSpeed = false;
                ClearTimecycleModifier();
                _visionState = 0;
                _fov = (config.FovMax + config.FovMin) * 0.5f; // Reset to default zoom level
                RenderScriptCams(false, false, 0, true, false);
                scaleform.Dispose();
                cam.Delete();
                Game.Nightvision = false;
            }
        }
        #endregion

        #region Event Handlers
        [EventHandler("onResourceStop")]
        internal void ResourceStopped(string resourceName)
        {
            // Treat it like we are exiting the cam, basically cleanup everything
            if (resourceName == GetCurrentResourceName())
            {
                ClearTimecycleModifier();
                Game.Nightvision = false;
                Game.ThermalVision = false;
                TriggerEvent("ShowHud");
            }
        }

        [EventHandler("helicam:deleteMarker")]
        internal void DeleteMarker(int src, Vector3 pos)
        {
            if (src == Game.Player.ServerId)
            {
                return;
            }

            foreach (Blip b in _markers)
            {
                if (b.Position == pos)
                {
                    _markers.Remove(b);
                    b.Delete();
                    break;
                }
            }
        }

        [EventHandler("helicam:deleteAllMarkers")]
        internal void DeleteAllMarkers(int src, int vehId)
        {
            if (src == Game.Player.ServerId || !Game.PlayerPed.IsSittingInVehicle() || Game.PlayerPed.CurrentVehicle.NetworkId != vehId)
            {
                return;
            }

            if (_markers.Count == 0)
            {
                Debug.WriteLine("ERROR: our marker list is different from the one who sent the event");
                return;
            }

            foreach (Blip b in _markers)
            {
                b.Delete();
            }
            _markers.Clear();
        }

        [EventHandler("helicam:addMarker")]
        internal void AddMarker(int src, int vehId, Vector3 pos, string name)
        {
            if (src == Game.Player.ServerId || !Game.PlayerPed.IsSittingInVehicle() || Game.PlayerPed.CurrentVehicle.NetworkId != vehId)
            {
                return;
            }

            Blip mark = World.CreateBlip(pos);
            mark.Sprite = (BlipSprite)123;
            mark.Name = name;
            mark.Color = (BlipColor)27;
            mark.Rotation = 0;

            _markers.Add(mark);
        }

        [EventHandler("helicam:drawSpotlight")]
        internal void RenderSpotlight(int src, int vehId, Vector3 start, Vector3 end, float size)
        {
            if (_drawnSpotlights.Count > 0)
            {
                foreach (KeyValuePair<int, Spotlight> spotlight in _drawnSpotlights)
                {
                    if (src != spotlight.Key && vehId == spotlight.Value.VehicleId)
                    {
                        Debug.WriteLine("spotlight already drawn!");
                        _spotlightActive = false;
                        return;
                    }
                }
            }
            start.Z -= 5f;
            _drawnSpotlights[src] = new Spotlight(vehId, start, end, size);
        }

        [EventHandler("helicam:killSpotlight")]
        internal void RemoveSpotlight(int src)
        {
            if (_drawnSpotlights.ContainsKey(src))
            {
                _drawnSpotlights.Remove(src);
            }

            if (src == Game.Player.ServerId)
            {
                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    type = "info",
                    message = $"Spotlight turned off"
                }));
            }
        }
        #endregion

        private Tuple<Entity, Vector3, Vector3> GetEntityInView(Camera cam)
        {
            Vector3 pos = cam.Position;
            Vector3 fwdV = RotAnglesToVec(cam.Rotation);
            Vector3 end = pos + (fwdV * (config.MaxDist + 300f));
            RaycastResult ray = World.Raycast(pos, end, IntersectOptions.Everything, Game.PlayerPed.CurrentVehicle);
            if (ray.DitHitEntity && (ray.HitEntity.Model.IsVehicle || ray.HitEntity.Model.IsPed))
            {
                return Tuple.Create(ray.HitEntity, ray.HitPosition, end);
            }
            else if (ray.DitHit)
            {
                return new Tuple<Entity, Vector3, Vector3>(null, ray.HitPosition, end);
            }
            return new Tuple<Entity, Vector3, Vector3>(null, Vector3.Zero, end);
        }

        #region Zoom Vision Markers
        private void ChangeVision()
        {
            if (_visionState == 0)
            {
                ClearTimecycleModifier();
                SetTimecycleModifier("heliGunCam");
                SetTimecycleModifierStrength(0.3f);
                Game.Nightvision = true;
                _visionState = 1;
            }
            else if (_visionState == 1)
            {
                Game.Nightvision = false;
                SetTimecycleModifier("NG_blackout");
                SetTimecycleModifierStrength(0.992f);
                _visionState = 2;
            }
            else
            {
                ClearTimecycleModifier();
                SetTimecycleModifier("heliGunCam");
                SetTimecycleModifierStrength(0.3f);
                _visionState = 0;
            }
        }

        private void HandleZoom(Camera cam)
        {
            if (Game.IsControlJustPressed(0, Control.CursorScrollUp) || Game.IsControlPressed(0, Control.FrontendUp) || (Game.IsControlJustPressed(0, Control.RappelSmashWindow) && Game.PlayerPed.CurrentVehicle.Driver != Game.PlayerPed))
            {
                _fov = Math.Max(_fov - config.ZoomSpeed, config.FovMin);
            }
            if (Game.IsControlJustPressed(0, Control.CursorScrollDown) || Game.IsControlPressed(0, Control.FrontendDown) || (Game.IsControlJustPressed(0, Control.ScriptLT) && Game.PlayerPed.CurrentVehicle.Driver != Game.PlayerPed))
            {
                _fov = Math.Min(_fov + config.ZoomSpeed, config.FovMax);
            }
            float currentFov = cam.FieldOfView;
            if (Math.Abs(_fov - currentFov) < 0.1f)
            {
                // Prevent unneeded zooming
                _fov = currentFov;
            }
            cam.FieldOfView = currentFov + ((_fov - currentFov) * 0.05f);
        }

        private void HandleMarkers(Vector3 cam)
        {
            RenderText(0.125f, config.TextY - 0.1f, $"Markers:  {_markers.Count}", 0.3f);
            if (Game.IsControlJustPressed(0, Control.ReplayHidehud))
            {
                if (_markers.Count > 0)
                {
                    // Delete most recent
                    Blip mark = _markers.LastOrDefault();
                    TriggerServerEvent("helicam:removeMarker", mark.Position);
                    _markers.Remove(mark);
                    mark.Delete();
                }

                if (_markers.Count == 0)
                {
                    TriggerServerEvent("helicam:removeAllMarkers", Game.PlayerPed.CurrentVehicle.NetworkId);
                }
            }
            if (Game.IsControlJustPressed(0, Control.PhoneCameraExpression))
            {
                if (_markers.Count > 9)
                {
                    SendNuiMessage(JsonConvert.SerializeObject(new
                    {
                        type = "alert",
                        message = "You have reached your marker limit!"
                    }));
                    return;
                }

                if (cam.IsZero)
                {
                    SendNuiMessage(JsonConvert.SerializeObject(new
                    {
                        type = "alert",
                        message = "You are not aiming at anything!"
                    }));
                    return;
                }

                string name = $"Marker #{_markers.Count} - {DateTime.Now.ToString("H:mm")}";
                cam.Z += 0.01f;
                Blip mark = World.CreateBlip(cam);
                mark.Sprite = (BlipSprite)123;
                mark.Name = name;
                mark.Color = (BlipColor)27;
                mark.Rotation = 0;

                SetBlipDisplay(mark.Handle, 2);
                _markers.Add(mark);
                TriggerServerEvent("helicam:createMarker", Game.PlayerPed.CurrentVehicle.NetworkId, mark.Position, name);
            }
        }

        private void RenderStreetNames(Vector3 pos)
        {
            foreach (KeyValuePair<string, List<Vector3>> street in _streetOverlay)
            {
                Dictionary<Vector3, double> dists = new Dictionary<Vector3, double>();
                foreach (Vector3 mark in street.Value)
                {
                    dists.Add(mark, World.GetDistance(pos, mark));
                }
                List<KeyValuePair<Vector3, double>> marks = dists.ToList();
                marks.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

                int count = 0;

                foreach (KeyValuePair<Vector3, double> mark in marks)
                {
                    if (mark.Value < 300)
                    {
                        RenderText3D(mark.Key, street.Key);
                        count++;
                    }
                }

                if (count == 0 && marks.First().Value < 500f)
                {
                    RenderText3D(marks.First().Key, street.Key);
                }
            }
        }
        #endregion

        #region Text Rendering
        private void RenderInfo(Entity ent)
        {
            if (ent.Model.IsVehicle)
            {
                Vehicle veh = (Vehicle)ent;
                string model = veh.LocalizedName;
                string plate = veh.Mods.LicensePlate;

                RenderText(0.2f, config.TextY, $"Model: {model}\nPlate: {plate}");

                string heading = veh.Heading < 45 ? "NB" : veh.Heading < 135 ? "WB" : veh.Heading < 225 ? "SB" : veh.Heading < 315 ? "EB" : "NB";
                RenderText(0.61f, config.TextY, heading);
            }
        }

        private void RenderRotation(Vehicle veh, Vector3 target, Vector3 camRotation)
        {
            double rawHdg = 270 - (Math.Atan2(veh.Position.Y - target.Y, veh.Position.X - target.X) * 180 / Math.PI);
            double heading = Math.Round(rawHdg % 360, 0);

            heading += Math.Round(veh.Heading);

            if (heading > 360)
            {
                heading -= 360;
            }

            if (_lastCamHeading != heading)
            {
                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    rotation = heading
                }));
                _lastCamHeading = heading;
            }


            double tilt = Math.Round(camRotation.X, 0);
            if (_lastCamTilt != tilt)
            {
                _lastCamTilt = tilt;
                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    camtilt = tilt
                }));
            }

            double camHeading = Math.Round(camRotation.Z);
            camHeading = camHeading > 0 ? 180 + (180 - camHeading) : Math.Abs(camHeading);

            SendNuiMessage(JsonConvert.SerializeObject(new
            {
                northheading = camHeading
            }));

            float latPos = target.Y;
            float lonPos = target.X;
            string latText = "N";
            string lonText = "E";
            if (latPos < 0f)
            {
                latText = "S";
                latPos = Math.Abs(latPos);
            }
            if (lonPos < 0f)
            {
                lonText = "W";
                lonPos = Math.Abs(lonPos);
            }
            double cameraHdg = 360 - Math.Round(camRotation.Z);
            if (cameraHdg > 360)
            {
                cameraHdg -= 360;
            }

            RenderText(0.55f, config.TextY - 0.1f, $"Map TGT:\n{latText} {Math.Round(latPos, 2)}\n{lonText} {Math.Round(lonPos, 2)}\n{cameraHdg}°", 0.3f);
        }

        private void RenderTargetPosInfo(Vector3 pos)
        {
            if (pos.IsZero)
            {
                // Ignore this, we aren't targetting a road
                return;
            }
            Vector3 outpos = new Vector3();
            GetNthClosestVehicleNode(pos.X, pos.Y, pos.Z, 0, ref outpos, 0, 0, 0);
            uint crossing = 1;
            uint p1 = 1;
            GetStreetNameAtCoord(pos.X, pos.Y, pos.Z, ref p1, ref crossing);
            string crossingName = GetStreetNameFromHashKey(crossing);
            string suffix = (crossingName != "" && crossingName != "NULL" && crossingName != null) ? "~t~ / " + crossingName : "";

            RenderText(0.64f, config.TextY, $"{World.GetStreetName(pos)}\n{suffix}");

            if (_calculateSpeed)
            {
                double distTravelled = World.GetDistance(pos, _speedMarker.Item2);

                int timeDiff = (Game.GameTime - _speedMarker.Item1) / 1000;
                double estSpeed = distTravelled / timeDiff;

                estSpeed *= 2.236936;

                if (double.IsInfinity(estSpeed) || double.IsNaN(estSpeed))
                {
                    RenderText(0.4f, config.TextY, $"Est. Speed: Measuring\nTime: {timeDiff}s", 0.4f);
                }
                else
                {
                    RenderText(0.4f, config.TextY, $"Est. Speed: {Math.Round(estSpeed, 0)}mph\nTime: {timeDiff}s", 0.4f);
                }

                World.DrawMarker(MarkerType.HorizontalCircleSkinny, _speedMarker.Item2, Vector3.Zero, Vector3.Zero, new Vector3(10f), Color.FromArgb(109, 184, 215));

            }
        }

        private void RenderText(float x, float y, string text, float scale = 0.5f)
        {
            SetTextFont(0);
            SetTextProportional(false);
            SetTextScale(0f, scale);
            SetTextColour(255, 255, 255, 255);
            SetTextDropshadow(0, 0, 0, 0, 255);
            SetTextEdge(1, 0, 0, 0, 255);
            SetTextDropShadow();
            SetTextOutline();
            SetTextEntry("STRING");
            AddTextComponentString(text);
            DrawText(_playerMap.RightX + x, _playerMap.BottomY - GetTextScaleHeight(y, 0) - 0.005f);
        }

        private void RenderText3D(Vector3 pos, string text)
        {
            float screenX = 0f, screenY = 0f;
            World3dToScreen2d(pos.X, pos.Y, pos.Z, ref screenX, ref screenY);

            SetTextFont(0);
            SetTextScale(0.25f, 0.25f);
            SetTextProportional(false);
            SetTextColour(255, 255, 255, 255);
            SetTextDropshadow(10, 0, 0, 0, 255);
            SetTextEdge(1, 0, 0, 0, 255);
            SetTextDropShadow();
            SetTextOutline();
            SetTextEntry("STRING");
            AddTextComponentString(text);
            DrawText(screenX, screenY);
        }
        #endregion

        #region Helper Functions
        private Vector3 RotAnglesToVec(Vector3 rot)
        {
            double x = Math.PI * rot.X / 180.0f;
            double z = Math.PI * rot.Z / 180.0f;
            double num = Math.Abs(Math.Cos(x));
            return new Vector3((float)(-Math.Sin(z) * num), (float)(Math.Cos(z) * num), (float)Math.Sin(x));
        }

        private void CheckInputRotation(Camera cam, float zoom)
        {
            float rightAxisX = Game.GetDisabledControlNormal(0, Control.ScriptRightAxisX);
            float rightAxisY = Game.GetDisabledControlNormal(0, Control.ScriptRightAxisY);
            Vector3 rot = cam.Rotation;
            if (rightAxisX != 0f || rightAxisY != 0f)
            {
                float newZ = rot.Z + (rightAxisX * -1.0f * config.SpeedUD * (zoom + 0.1f));
                float newX = (float)Math.Max(Math.Min(20.0f, rot.X + (rightAxisY * -1.0 * config.SpeedLR * (zoom + 0.1))), -89.5f); // Clamping at top and bottom
                cam.Rotation = new Vector3(newX, 0f, newZ);
            }
        }

        /// <summary>
        /// This function plays a game sound, before releasing the soundId when the sound has finished.
        /// </summary>
        private async void PlayManagedSoundFrontend(string soundName, string soundSet = null)
        {
            int soundId = Audio.PlaySoundFrontend(soundName, soundSet);

            while (!Audio.HasSoundFinished(soundId))
            {
                await Delay(200);
            }

            Audio.ReleaseSound(soundId);
        }


        private bool IsPlayerInHeli()
        {
            Vehicle heli = Game.PlayerPed.CurrentVehicle;

            return Entity.Exists(heli) && (Game.PlayerPed.IsInHeli || config.AircraftHashes.Contains(heli.DisplayName.ToLower()) || config.HelicopterHashes.Contains(heli.DisplayName.ToLower()));
        }

        private void DrawThermal(float x1, float y1, float z1, float x2, float y2, float z2) => DrawBox(x1, y1, z1, x2, y2, z2, 255, 255, 255, 90);
        #endregion
    }

    internal class ThermalBone
    {
        internal string BoneName;
        internal Vector3 StartPos;
        internal Vector3 EndPos;
        internal ThermalType Type;

        internal ThermalBone(string name, Vector3 v1, Vector3 v2, ThermalType type = ThermalType.PED)
        {
            BoneName = name;
            StartPos = v1;
            EndPos = v2;
            Type = type;
        }
    }

    internal enum ThermalType
    {
        ENGINE,
        WHEEL,
        PED
    }
}