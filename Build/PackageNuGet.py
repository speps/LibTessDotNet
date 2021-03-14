import xml.etree.ElementTree as ET

def updatespec(name):
	tree = ET.parse(name)
	meta = tree.getroot().find("./metadata")
	meta.find("projectUrl").text = "https://github.com/speps/LibTessDotNet"
	meta.find("releaseNotes").text = "See https://github.com/speps/LibTessDotNet/releases for release notes"
	meta.remove(meta.find("iconUrl"))
	meta.remove(meta.find("tags"))
	meta.remove(meta.find("dependencies"))
	tree.write(name)

updatespec("LibTessDotNet.nuspec")
updatespec("LibTessDotNet.Double.nuspec")
