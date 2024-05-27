# Call of Duty map import editor for Unity

*Requires atleast Greyhound v1.46.3.1 and C2M v1.0.4.3

## Greyhound

### Props
-Open any Call of Duty that is supported by Greyhound
-Load in to any map
-Open Greyhound as admin
-Press load game and export all

### Images
*If you only want the specific scenes textures, use C2Ms search string file instead of exporting all of them
-Click open file and navigate to (Call of Duty Directory)>main
-Open each and every iwd file and click export all

## C2M
-Open C2M as admin while you are still in the map you want in Call of Duty
-Click the paper airplane
*It will also save a search string if you want it for the images step in greyhound

## Unity
-Create an Editor folder in your unity project and add Cod2Unity.cs that is provided here
-Go to Window>Cod2Unity
-In the props tab click "Import Assets From Greyhound" and choose the folder (Greyhound Directory)>exported_files>(Call of Duty Game). Then wait for import (Will take awhile)
-In the Map Details tab click "Import Assets From C2M" and choose the folder (Where C2M is)>exported_maps>(Call of Duty Game)>sp (or) mp>(Map Name).Then wait for import
-Then in the Build Settings tab, click "Build and Place"
-And your all done

## Notes
The map will look a little funny. There is keywords in the editor to change shaders but it's kinda tough to put every keyword for every map for every COD game. Some require
custom shaders. I optimized the editor partially but I don't want to spend a ton of time doing optimizing since it's literally just an import editor. This does create prefabs
and automatically sets up LOD's and changes model settings and creates materials and everything like that. But the LOD's are just a basic setting, you will need to go in
and set distances manually because they are just split by LOD# divided by the LOD count
