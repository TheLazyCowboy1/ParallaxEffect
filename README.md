# Parallax Effect

Overwrites the main level shader to have a slight parallax effect.

This is done by:
1. Assuming that every object extends indefinitely far backwards. This makes poles look like walls. (This is very necessary, unfortunately).
2. Warping the positions and depths of pixels as the Player moves. (<- the parallax part)
3. Essentially "ray-marching" in the direction of the warp to find the best new pixel color. This can be an expensive operation if the warp is strong.