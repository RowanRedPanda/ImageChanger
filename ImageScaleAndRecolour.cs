using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public static class ImageScaleAndRecolour
{
    /*
    This static class exists to resize and recolour images to meet the palette and pixel density requirements of the game project.
    Callable as a static class it takes either a path string to where it can locate an image (jpg, png, or bmp), or it takes a sprite
    from a game object. It also takes a vector2 that represents the maximal width and height of the resize destination. This may not
    be the final dimensions of the resized image, but the image will be given a best fit within that bounding. Once an image is provided
    this class first converts it to a texture for re-rendering at a new size, then takes the resized texture and recolours each pixel
    against the below palette of allowable colours.

    On recolouring, each unique pixel value of the texture is compared against the available array of pixel values, to find the closest
    fit, and replaces each pixel with its closes fit to recolour the new texture.

    The new texture is then rendered and returned to the calling class for any further processing it may need to be turned into a sprite,
    a texture, or saved, as needed.

    The static paletteColours below can contain any non-0 number of colours, and increasing or decreasing the colours available results in
    images of different colour depth. The project this is contained within is working to a 32 colour palette, of 8 greyscale colours
    (including black and white) and 6 colour families of 4 shades, 4 shades of red, yellow, green, teal, blue, and purple.

    RowanRedPanda
    */
    private static readonly float weightAgainstGrey = 1.5f; //higher numbers weight against grey more. For colour palettes with fewer nongrey colours this number is better higher.

    private static readonly Color32[] paletteColours =
        {
        //The game's entire colour palette as an array of r,g,b,a values
            //Black to white greyscale=====================
            new Color32(0, 0, 0, 255),
            new Color32(36, 36, 36, 255),
            new Color32(73, 73, 73, 255),
            new Color32(109, 109, 109, 255),
            new Color32(146, 146, 146, 255),
            new Color32(182, 182, 182, 255),
            new Color32(219, 219, 219, 255),
            new Color32(255, 255, 255, 255),
            //Red Group====================================
            new Color32(53, 0, 0, 255),
            new Color32(78, 0, 0, 255),
            new Color32(103, 0, 0, 255),
            new Color32(128, 0, 0, 255),
            //Yellow Group=================================
            new Color32(53, 53, 0, 255),
            new Color32(78, 78, 0, 255),
            new Color32(103, 103, 0, 255),
            new Color32(128, 128, 0, 255),
            //Green Group==================================
            new Color32(0, 53, 0, 255),
            new Color32(0, 78, 0, 255),
            new Color32(0, 103, 0, 255),
            new Color32(0, 128, 0, 255),
            //Teal Group===================================
            new Color32(0, 53, 53, 255),
            new Color32(0, 78, 78, 255),
            new Color32(0, 103, 103, 255),
            new Color32(0, 128, 128, 255),
            //Blue Group===================================
            new Color32(0, 0, 53, 255),
            new Color32(0, 0, 78, 255),
            new Color32(0, 0, 103, 255),
            new Color32(0, 0, 128, 255),
            //Purple Group=================================
            new Color32(53, 0, 53, 255),
            new Color32(78, 0, 78, 255),
            new Color32(103, 0, 103, 255),
            new Color32(128, 0, 128, 255)
            //=============================================
        };

    public static Texture2D RescaleImage(string myPath, Vector2 scale)
    {
        /*
        This version of RescaleImage takes a path to a jpg stored on the user's computer, specifically their
        desktop cache. It also takes a vector2 that represents the width and height that the image should be
        scaled to. This is usually given as (285,160)
        */
        if (File.Exists(myPath)) //check for file existing
        {
            Texture2D tex = new(2, 2); //prepare a new texture2D
            byte[] fileData; //prepare byte array
            float scaleFactor;

            fileData = File.ReadAllBytes(myPath); //read bytes of the image at the path, into the byte array
            tex.LoadImage(fileData); //load the bytes into the texture2D, this will create a 1:1 texture representation of the image

            /*
            This if takes the texture witdh and height, and checks that against the required new scale, as provided
            by the vector2. If the width requires a greater amount of scaling to bring in, scale on x,
            otherwise scale on y. This will ensure that the taken image fits within the new screen parameters.

            Then divide the chosen one by the scale to get the scale factor, this is the value that both dimensions
            need to be divided by to get to the desired scale, while maintaining aspect ratio.
            */
            if (tex.width / scale.x > tex.height / scale.y)
            {
                scaleFactor = tex.width / scale.x;
            }
            else
            {
                scaleFactor = tex.height / scale.y;
            }
            /*
            Resize the texture based on the chosen scalefactor, either x or y, and the amount it needed to scale by.
            */
            tex = Resize(tex, (int)Mathf.Round(tex.width / scaleFactor), (int)Mathf.Round(tex.height / scaleFactor));
            tex.filterMode = FilterMode.Point;

            return tex;
        }
        else
        {
            return null;
        }
    }


    public static Sprite RescaleImage(Sprite sprite, Vector2 scale)
    {
        /*
        This version of RescaleImage takes a sprite, usually derived from a GameObject's sprite renderer, and a Vector2 that is calculated based on the
        GameObject's dimensions.

        See above for descriptions of identical processes.
        */
        Texture2D tex = new (2,2);
        byte[] fileData;
        float scaleFactor;

        fileData = sprite.texture.EncodeToPNG(); //the sprite needs to be encoded to a png to be able to be read as byte data into the byte array
        tex.LoadImage(fileData);

        if (tex.width / scale.x > tex.height / scale.y)
        {
            scaleFactor = tex.width / scale.x;
        }
        else
        {
            scaleFactor = tex.height / scale.y;
        }

        tex = Resize(tex, (int)Mathf.Round(tex.width / scaleFactor), (int)Mathf.Round(tex.height / scaleFactor));
        tex.filterMode = FilterMode.Point;

        //after resizing the image, return it to a sprite.
        Sprite result = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);

        return result;
    }

    static Texture2D Resize(Texture2D tex, int targetX, int targetY)
    {
        /*
        The render texture is taken and resized to be the x and y values given. This resizing then needs applying after calculating before returning a result.
        This method also recolours the texture before returning it.
        */
        RenderTexture rt = new RenderTexture(targetX, targetY, 1); //render a new texture of the correct dimensions
        RenderTexture.active = rt;
        Graphics.Blit(tex, rt); //copy the given original texture into the new rendered texture
        Texture2D result = new Texture2D(targetX, targetY); //take the rendered result from above and resize it to the target dimensions
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0); //finalise the rendering, apply a new rect to it based on its new dimensions
        result.Apply(); //render the result to have the new texture saved

        return Recolour(result); //recolour before returning
    }

    static Texture2D Recolour(Texture2D tex)
    {
        /*
        Recolour the image to the working palette, changing out the palette would change the results here.
        This method is provided the resized texture, because the texture is already resized it is less computationally intense to recolour it.
        */
        Color32[] fullImagePixels = tex.GetPixels32(); //create an array of every pixel in the provided texture
        Color32[] uniquePixels = fullImagePixels.Distinct().ToArray(); //strip the array to only unique values, this ensures that pixel values aren't needlessly checked and recalculated.
        Dictionary<Color32, Color32> replacementDictionary = new Dictionary<Color32, Color32>(); //prepare a blank dictionary

        //for each unique pixel, this will score its closeness to a pixel in the paletteColours array
        foreach (Color32 pixel in uniquePixels)
        {
            // if the pixel has a low alpha value, just set it to be transparent.
            if (pixel.a < 128)
            {
                replacementDictionary.Add(pixel, new Color32(0, 0, 0, 0));
            }
            //if a pixel does not have a low alpha value, assume it needs to be recoloured.
            else
            {
                int k = 0; //k is the index of the colour in the array paletteColours iterated within the below foreach
                float lowestScore = 999999f; //an arbitrarily high number to start scoring from, the first checked colour will always be better than this
                int paletteIndex = 0; //a stored value of k
                foreach (Color32 colour in paletteColours)
                {
                    float weight = 1f;
                    /*
                    Loop through all available colours in the chosen palette, and for each one, subtract the r,g,b values from the currently being scored
                    pixel. The aim of this loop is to get to the lowest lowestScore value, as this represents the least amount of variance across all r,g,b
                    values.
                    */

                    //if the image pixel is a true grey, then don't weight against grey, otherwise weight against greys.
                    if (colour.r == colour.g && colour.g == colour.b && (pixel.r != pixel.g || pixel.g != pixel.b))
                    {
                        weight = weightAgainstGrey;
                    }

                    float score = Vector3.Distance(new Vector3(colour.r, colour.g, colour.b), new Vector3(pixel.r, pixel.g, pixel.b)) * weight;
                    //if the score is lower than a previously recorded score (which starts at 999999 so that there will always be something stored) then store this new score and value
                    if (lowestScore > score)
                    {
                        lowestScore = score; //my current best score stored
                        paletteIndex = k; //the index where the best score was found
                    }

                    k++; //iterate k up to track index
                }
                /*
                Once the entire palette has been looped through, a best score will have been found. Add a new entry to the dictionary of the original pixel rgba values, and a corresponding
                palette value
                */
                replacementDictionary.Add(pixel, paletteColours[paletteIndex]); //(original pixel value, palette value at the index of the best scored value)
            }
        }
        /*
        2D for loop to iterate through every pixel of the texture and replace it with the new value. Because all the unique pixels have been scored and given a replacement value, this
        requires no additional calculation, just a key/value return from the dictionary.
        */
        for (int i = 0; i < tex.width; i++) //iterate through the width
        {
            for (int j = 0; j < tex.height; j++) //iterate through the height
            {
                Color32 currentColour = tex.GetPixel(i, j); //at the co-ordinates iterated through check the pixel values of that pixel.

                tex.SetPixel(i, j, replacementDictionary[currentColour]); //use the checked pixel as the key to get the previously calculated replacement colour, and replace with that colour.
            }
        }
        tex.Apply(); //render the new recoloured texture to store it.
        return tex; //return it to the resizer to be returned to the calling method.
    }
}
