using UnityEngine;
using System.Collections;

public static class UtilsFunctions
{
    public delegate void ChildHandler(GameObject child);

    /// <summary>
    /// Iterates all children of a game object
    /// </summary>
    /// <param name="gameObject">A root game object</param>
    /// <param name="childHandler">A function to execute on each child</param>
    /// <param name="recursive">Do it on children? (in depth)</param>
    public static void IterateChildren(GameObject gameObject, ChildHandler childHandler, bool recursive)
    {
        DoIterate(gameObject, childHandler, recursive);
    }

    /// <summary>
    /// NOTE: Recursive!!!
    /// </summary>
    /// <param name="gameObject">Game object to iterate</param>
    /// <param name="childHandler">A handler function on node</param>
    /// <param name="recursive">Do it on children?</param>
    private static void DoIterate(GameObject gameObject, ChildHandler childHandler, bool recursive)
    {
        foreach (Transform child in gameObject.transform)
        {
            childHandler(child.gameObject);
            if (recursive)
                DoIterate(child.gameObject, childHandler, true);
        }
    }

    public static float NoiseLoop(float diameter, float angle, Vector2 center)
    {
        float rad = angle * Mathf.Deg2Rad;
        float xOff = Map(-1, 1, center.x, center.x + diameter, Mathf.Cos(rad));
        float yOff = Map(-1, 1, center.y, center.y + diameter, Mathf.Sin(rad));
        return Mathf.PerlinNoise(xOff, yOff);
    }

    public static float Map(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {

        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }

    public static bool IsBetween(float input, float min, float max)
    {
        return input <= input + max && input >= input - min;
    }

    public static Vector3 ElementwiseMultiply(this Vector3 vector, Vector3 other)
    {
        return new Vector3(vector.x * other.x, vector.y * other.y, vector.z * other.z);
    }

    public static void DrawGizmoString(string text, Vector3 worldPosition, Color textColor, Vector2 anchor, float textSize = 15f)
    {
#if UNITY_EDITOR
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        if (!view)
            return;
        Vector3 screenPosition = view.camera.WorldToScreenPoint(worldPosition);
        if (screenPosition.y < 0 || screenPosition.y > view.camera.pixelHeight || screenPosition.x < 0 || screenPosition.x > view.camera.pixelWidth || screenPosition.z < 0)
            return;
        var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
        UnityEditor.Handles.BeginGUI();
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)textSize,
            normal = new GUIStyleState() { textColor = textColor }
        };
        Vector2 size = style.CalcSize(new GUIContent(text)) * pixelRatio;
        var alignedPosition =
            ((Vector2)screenPosition +
            size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) +
            Vector2.up * view.camera.pixelHeight;
        GUI.Label(new Rect(alignedPosition / pixelRatio, size / pixelRatio), text, style);
        UnityEditor.Handles.EndGUI();
#endif
    }
    
    /// <summary>
    /// Convert linear to decibel volume
    /// </summary>
    /// <param name="input">Value from 0 to 1</param>
    /// <returns></returns>
    public static float LinearToDecibel(float input)
    {
        float dB;
        if (input != 0)
            dB = 20f * Mathf.Log10(input);
        else
            dB = -144f;
        
        return dB;
    }

    public static IEnumerator FadeAudioSource(this AudioSource audioSource, float targetVolume, float fadeDuration, bool destroy = false, float delay = 0)
    {
        float startVolume = audioSource.volume;
        float timer = 0;

        if(delay != 0)
            yield return new WaitForSeconds(delay);

        audioSource.Play();

        while (audioSource.volume != targetVolume && fadeDuration != 0)
        {
            timer += Time.unscaledDeltaTime;

            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);

            yield return null;
        }

        audioSource.volume = targetVolume;

        if (audioSource.volume <= 0)
            audioSource.Stop();

        //you can remove this code that destroys the source
        if (destroy)
            GameObject.Destroy(audioSource);
    }
}