using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine
{
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) {} }
    public class SerializeField : Attribute {}
    public class MinAttribute : Attribute { public MinAttribute(float value) {} }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x=x; this.y=y; }
        public float sqrMagnitude => x*x+y*y;
    }
    public class Transform
    {
        public Transform parent;
        public GameObject gameObject;
        public bool IsChildOf(Transform other) => this==other || (parent?.IsChildOf(other) ?? false);
    }
    public class GameObject
    {
        public Transform transform;
        public List<object> components = new List<object>();
        public GameObject(Transform parent=null) { transform=new Transform {parent=parent, gameObject=this}; }
        public T GetComponentInParent<T>() where T:class
        {
            for (Transform t=transform; t!=null; t=t.parent)
                foreach (object c in t.gameObject.components) if (c is T found) return found;
            return null;
        }
    }
    public class MonoBehaviour
    {
        public bool isActiveAndEnabled=true;
        public GameObject gameObject=new GameObject();
        public Transform transform=>gameObject.transform;
    }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
    public static class Time { public static float unscaledTime; }
    public static class Input
    {
        public static bool down;
        public static Vector2 mousePosition, mouseScrollDelta;
        public static bool GetMouseButtonDown(int button)=>down;
    }
}
namespace UnityEngine.EventSystems
{
    public interface IScrollHandler {}
    public interface IPointerClickHandler {}
    public interface IPointerDownHandler {}
    public struct RaycastResult { public GameObject gameObject; }
    public class PointerEventData
    {
        public enum InputButton { Left }
        public Vector2 position,scrollDelta;
        public InputButton button;
        public PointerEventData(EventSystem events) {}
    }
    public class EventSystem
    {
        public static EventSystem current=new EventSystem();
        public GameObject hit;
        public void RaycastAll(PointerEventData pointer,List<RaycastResult> results)
        { if(hit!=null)results.Add(new RaycastResult {gameObject=hit}); }
    }
    public static class ExecuteEvents
    {
        public static GameObject GetEventHandler<T>(GameObject obj) where T:class
        {
            for(Transform t=obj.transform;t!=null;t=t.parent)
                foreach(object c in t.gameObject.components)if(c is T)return t.gameObject;
            return null;
        }
    }
}
namespace UnityEngine.UI
{
    public class Selectable {}
    public class ScrollRect : MonoBehaviour, UnityEngine.EventSystems.IScrollHandler
    {
        public int scrollCalls;
        public Vector2 lastDelta;
        public void OnScroll(UnityEngine.EventSystems.PointerEventData pointer)
        { scrollCalls++;lastDelta=pointer.scrollDelta; }
    }
}
public partial class 剧本System : MonoBehaviour
{
    private bool isWaitingForChoice;
    public string[][] 已储存剧本 = new string[100][];
    private int 已阅读;
    public ScrollRect 进度条 = new ScrollRect();
    public int advances;
    public void Next() { advances++; 已阅读++; }
    public bool Tick(bool down, float wheel=0)
    { Input.down=down;Input.mouseScrollDelta=new Vector2(0,wheel);return UpdateReaderInput(); }
    public void EnableGlobal(bool enabled) { enableGlobalReaderInput=enabled; }
    public void Waiting(bool waiting) { isWaitingForChoice=waiting; }
    public void Finish() { 已阅读=已储存剧本.Length; }
}
