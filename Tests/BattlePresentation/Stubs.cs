using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    public class GameObject
    {
        public bool activeInHierarchy = true;
        public readonly Transform transform = new();
        public readonly Dictionary<Type,object> components = new();
        public void SetActive(bool b) { activeInHierarchy = b; }
    }
    public class MonoBehaviour
    {
        public GameObject gameObject = new();
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T:class => gameObject.components.GetValueOrDefault(typeof(T)) as T;
    }
    public class Transform { public Vector3 position, localScale; }
    public class Sprite { }
    public class SpriteRenderer { public Sprite sprite; public Transform transform = new(); }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public static class Time { public static float deltaTime,unscaledDeltaTime,timeScale=1f,fixedDeltaTime=0.02f; }
    public static class Mathf
    {
        public static int FloorToInt(float f)=>(int)Math.Floor(f);
        public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float f,float a,float b)=>Math.Clamp(f,a,b);
        public static float Abs(float f)=>Math.Abs(f);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Math.Clamp(t,0f,1f);
    }
    public static class Random { public static float Range(float a,float b)=>(a+b)/2; }
}
namespace UnityEngine.Events { public delegate void UnityAction(); }
namespace UnityEngine.Serialization { }
namespace UnityEngine.UI { public class Text { public string text; } }
namespace Sirenix.OdinInspector { public class SerializedMonoBehaviour : UnityEngine.MonoBehaviour { } }
namespace Sirenix.Serialization { public class OdinSerializeAttribute : Attribute { } }
namespace DG.Tweening
{
    // Only the external tween scheduler is simulated; production lifecycle code is linked unchanged.
    public class Tween { public bool active=true; public Action callback; public void Kill() { active=false; } public void Fire() { if(active) { active=false;callback?.Invoke(); } } }
    public static class DOVirtual
    {
        public static Tween Last;
        public static Tween DelayedCall(float t, Action callback,bool independent) => Last=new Tween{callback=callback};
    }
    public static class Extensions { public static Tween DOFade(this UnityEngine.SpriteRenderer r,float a,float t)=>new(); }
}
