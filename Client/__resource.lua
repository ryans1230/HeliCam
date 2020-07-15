-- HeliCam Release Version 1.0
--
-- Copyright (c) 2019, Ryan Z. All rights reserved.
--
-- This project is licensed under the following:
-- Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to use, copy, modify, and merge the software, under the following conditions:
-- The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
-- THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. THE SOFTWARE MAY NOT BE SOLD.
--


resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

ui_page 'nui/nui.html'

files {
	'Newtonsoft.Json.dll',
	-- json data
	'streets.json',
	'config.json',
	-- nui
	'nui/nui.html',
	'nui/style.css',
	'nui/style.css',
	'nui/rotate.css',
	'nui/script.js',
	'nui/images/heli.png',
	'nui/images/plane.png',
	'nui/images/heading.png',
	'nui/images/cam.png',
	'nui/images/arrow.png'
}

client_script 'HeliCam.Client.net.dll'

server_script 'HeliCam.Server.net.dll'
