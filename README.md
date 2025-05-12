# Parallax Effect

Uses 1.10's new LevelTexCombiner to easily add a shader pass (in effect, not in actuality) to the level texture.

(This can be done without LevelTexCombiner, but doing so basically involves copying its functionality.
Doing this through LevelTexCombiner makes it compatible with most shaders and other mods, because the Watcher uses LevelTexCombiner.)

This is done by:
1. Assuming that every object extends indefinitely far backwards. This makes poles look like walls. (This is very necessary, unfortunately. There is a setting to disable this and instead find the nearest pixel, but it has artifacts.).
2. Warping the positions and depths of pixels as the Player moves. (<- the parallax part)
3. Essentially "ray-marching" in the direction of the warp to find the best new pixel color. This can be an expensive operation if the warp is strong.