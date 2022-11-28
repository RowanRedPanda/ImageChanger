# ImageChanger
 C# Unity class for resizing and recolouring images
 
 ImageScaleAndRecolour is a public static running in a Unity 2021.3.3f1 project for the purposes of resizing and recolouring image files and sprites to match an ingame palette.
 
 The colour palette is defined as a static readonly color32 array, so can be amended with any r,g,b,a added. As presented all colours in the palette have an alpha channel of 255
 and have no calculations tied to matching alpha. A check for alpha value is done, but can be ignored safely.
 
 ImageScaleAndRecolour has two public methods, both callable as RescaleImage. RescaleImage always requires a Vector2 to define the box to fit the resize into, but will take either
 a path string to an image file stored locally, or a sprite.
 
 The class includes a static float weightAgainstGrey, which is an optional weighting against true grey colours (any pixel value where the r, g, and b values are all equal) being
 selected as these grey values are potentially easier to match to depending on how restrictive the provided palette is. Setting this value to 1 is the same as no weighting.
 Setting this value to less than 1 is a positive weighting towards selecting grey. Values above 1 weight against selecting a grey.
 
 This class is being used as part of a game project that provides it a string to the user's desktop image cache, though any filepath could be provided as a path. Verifying the
 path before calling the method would be better, however the verification also exists in the method.
 
 Scoring colour similarity is done as a distance between two Vector3, treating the r, g, and b values of both colours as the xyz of the Vector3.
