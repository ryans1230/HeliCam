resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

ui_page 'nui/nui.html'

files {
	'Newtonsoft.Json.dll',
	-- json data
	'streets.json',
	'config.json',
	-- nui
	'nui/nui.html',
	'nui/rotate.css',
	'nui/heli.png',
	'nui/plane.png',
	'nui/heading.png',
}

client_script 'HeliCam.Client.net.dll'

server_script 'HeliCam.Server.net.dll'
