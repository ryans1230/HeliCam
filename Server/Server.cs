using CitizenFX.Core;

namespace HeliCam
{
    public class Main : BaseScript
    {
        [EventHandler("helicam:createMarker")]
        internal void CreateMarker([FromSource] Player player, int vehId, Vector3 pos, string mkrName)
        {
            TriggerClientEvent("helicam:addMarker", player.Handle, vehId, pos, mkrName);
        }

        [EventHandler("helicam:removeMarker")]
        internal void RemoveMarker([FromSource] Player player, Vector3 pos)
        {
            TriggerClientEvent("helicam:deleteMarker", player.Handle, pos);
        }

        [EventHandler("helicam:removeAllMarkers")]
        internal void RemoveAllMarkers([FromSource] Player player, int vehId)
        {
            TriggerClientEvent("helicam:deleteAllMarkers", player.Handle, vehId);
        }
    }
}