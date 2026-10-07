using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

class CustomButton : IPointerDownHandler {}
static class Program
{
    static int checks;
    static void Check(bool condition,string label)
    { checks++;if(!condition)throw new Exception(label); }
    static void Main()
    {
        Time.unscaledTime=0;
        var reader=new 剧本System();reader.EnableGlobal(true);
        reader.SetManualAdvanceInterval(2f);
        Check(reader.Tick(true),"first outside click advances immediately");
        Time.unscaledTime=1.99f;
        Check(!reader.Tick(true),"rapid click rejected");
        Time.unscaledTime=2;
        Check(reader.Tick(true),"exactly two seconds allows advance");
        Check(!reader.Tick(false),"release does not advance");

        var button=new GameObject(reader.transform);button.components.Add(new CustomButton());
        var label=new GameObject(button.transform);EventSystem.current.hit=label;
        Time.unscaledTime=4;
        Check(!reader.Tick(true),"custom toolbar button child blocks advance");
        EventSystem.current.hit=null;
        Check(reader.Tick(true),"blocked toolbar click did not consume cooldown");
        var selectable=new GameObject(reader.transform);selectable.components.Add(new Selectable());
        EventSystem.current.hit=selectable;Time.unscaledTime=6;
        Check(!reader.Tick(true),"scrollbar and standard controls block advance");
        EventSystem.current.hit=new GameObject();
        Check(!reader.Tick(true),"external overlay blank area blocks advance");
        EventSystem.current.hit=null;reader.Waiting(true);
        Check(!reader.Tick(true),"outside click cannot bypass choices");
        reader.Waiting(false);
        Check(reader.Tick(true),"blocked choice click did not consume cooldown");
        reader.SetManualAdvanceInterval(.5f);Time.unscaledTime=6.5f;
        Check(reader.Tick(true),"runtime interval change works");
        reader.SetManualAdvanceInterval(-1);
        Check(reader.ManualAdvanceInterval==0 && reader.Tick(true),"zero disables interval and negative clamps");
        reader.Next();Check(reader.advances==7,"internal and automatic Next bypass manual gate");

        Check(!reader.Tick(false,1) && reader.进度条.scrollCalls==1,"outside wheel scrolls without advance");
        EventSystem.current.hit=label;reader.Tick(false,-1);
        Check(reader.进度条.scrollCalls==2 && reader.进度条.lastDelta.y==-1,"wheel over toolbar forwards direction");
        var scroll=new GameObject(reader.transform);scroll.components.Add(reader.进度条);
        EventSystem.current.hit=new GameObject(scroll.transform);reader.Tick(false,1);
        Check(reader.进度条.scrollCalls==2,"native viewport wheel is not dispatched twice");
        EventSystem.current.hit=new GameObject();reader.Tick(false,1);
        Check(reader.进度条.scrollCalls==2,"external overlay wheel not stolen");
        EventSystem.current.hit=null;reader.EnableGlobal(false);
        Check(!reader.Tick(true,1) && reader.进度条.scrollCalls==2,"other reader prefabs opt out");
        reader.EnableGlobal(true);reader.isActiveAndEnabled=false;
        Check(!reader.TryAdvanceByClick(),"hidden reader cannot advance manually");
        reader.isActiveAndEnabled=true;reader.Finish();
        Check(!reader.Tick(true),"finished script cannot consume manual input");
        Console.WriteLine($"AVG reader input: {checks} checks passed.");
    }
}
