using System;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }

        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
        {
            return null;
        }

        public static void Destroy(Object obj)
        {
        }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; set; }

        public Transform transform { get; set; }

        public T GetComponent<T>() where T : Component
        {
            return null;
        }
    }

    public class Transform : Component
    {
        public Vector3 position;

        public void Translate(Vector3 translation)
        {
        }
    }

    public class GameObject : Object
    {
        public bool activeSelf { get; set; }

        public Transform transform { get; set; }

        public T GetComponent<T>() where T : Component
        {
            return null;
        }

        public void SetActive(bool value)
        {
        }

        public static GameObject Find(string name)
        {
            return null;
        }
    }

    public class MonoBehaviour : Component
    {
    }

    public class ScriptableObject : Object
    {
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 up
        {
            get { return new Vector3(0f, 1f, 0f); }
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator *(Vector3 a, float d)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }
    }

    public struct Quaternion
    {
        public static Quaternion identity
        {
            get { return new Quaternion(); }
        }
    }

    public struct Color
    {
        public static Color white
        {
            get { return new Color(); }
        }

        public static Color gray
        {
            get { return new Color(); }
        }
    }

    public struct Rect
    {
        public float width;
        public float height;
    }

    public class RectTransform : Transform
    {
        public Rect rect { get; set; }
    }

    public class Canvas : Component
    {
        public float scaleFactor { get; set; }
    }

    public static class Screen
    {
        public static int width { get; set; }

        public static int height { get; set; }
    }

    public static class Input
    {
        public static Vector3 mousePosition { get; set; }
    }

    public static class Time
    {
        public static float time { get; set; }

        public static float deltaTime { get; set; }
    }

    public static class Random
    {
        public static int Range(int minInclusive, int maxExclusive)
        {
            return minInclusive;
        }

        public static float Range(float minInclusive, float maxInclusive)
        {
            return minInclusive;
        }
    }

    public static class Debug
    {
        public static void Log(object message)
        {
        }
    }

    public static class ColorUtility
    {
        public static string ToHtmlStringRGB(Color color)
        {
            return "FFFFFF";
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CreateAssetMenuAttribute : Attribute
    {
        public string fileName { get; set; }

        public string menuName { get; set; }

        public int order { get; set; }
    }
}

namespace UnityEngine.UI
{
    public class Slider : Component
    {
        public float value { get; set; }
    }

    public class Text : Component
    {
        public string text { get; set; }
    }

    public class Button : Component
    {
        public bool interactable { get; set; }
    }

    public class Image : Component
    {
        public Color color { get; set; }
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot)
        {
        }
    }
}

namespace UnityEngine.EventSystems
{
    public interface IEventSystemHandler
    {
    }

    public class PointerEventData
    {
    }

    public interface IPointerEnterHandler : IEventSystemHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    public interface IPointerExitHandler : IEventSystemHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }

    public interface IPointerClickHandler : IEventSystemHandler
    {
        void OnPointerClick(PointerEventData eventData);
    }
}

namespace TMPro
{
    public class TextMeshProUGUI : UnityEngine.Component
    {
        public string text { get; set; }
    }
}
