using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DG.Tweening;

static class Program
{
    static int passed;
    static void Assert(bool b,string message) { if(!b) throw new Exception(message); }
    static void Call(object o,string method) => o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    static void Tick(object o,float seconds) { Time.deltaTime=seconds*Time.timeScale;Time.unscaledDeltaTime=seconds;Call(o,"Update"); }
    static void Test(string name,Action body)
    {
        Time.timeScale=1;Time.fixedDeltaTime=0.02f;Time.deltaTime=0;Time.unscaledDeltaTime=0;
        body();passed++;Console.WriteLine("PASS "+name);
    }
    static List<Sprite> Frames(int n)=>Enumerable.Range(0,n).Select(_=>new Sprite()).ToList();
    static PieceDisplay Display(int n)=>new PieceDisplay { pieceSpriteRenderer=new(),meleeSprites=Frames(n),idleSprite=Frames(2),hitSprite=Frames(4),deathSprites=Frames(6) };
    static TimeManager Clock() { var t=new TimeManager();Call(t,"Awake");return t; }
    static void Main()
    {
        Test("Short and long clips release at their own middle frame and complete exactly once",()=>{
            foreach(int count in new[]{1,2,6,18}) {
                var d=Display(count);int releases=0;var p=d.PlayAction(PieceDisplayState.Attack,()=>releases++);
                float before=(count/2)/6f-0.001f;
                if(before>0) { Tick(d,before);Assert(releases==0,"early release"); }
                Tick(d,0.002f);Assert(releases==1,"missing release");
                Tick(d,count/6f+0.1f);Tick(d,2);
                Assert(p.IsDone && !p.IsCancelled && releases==1,"completion/release count");
            }
        });
        Test("Low FPS crossing the whole clip cannot skip or repeat release",()=>{
            var d=Display(8);int n=0;var p=d.PlayAction(PieceDisplayState.Attack,()=>n++);
            Tick(d,5);Tick(d,5);Assert(n==1 && p.IsDone,"skipped or duplicated release");
        });
        Test("A positive legacy duration never truncates a long clip or double-calls finish",()=>{
            var d=Display(18);int n=0;d.ChangeDisplayState(PieceDisplayState.Attack,false,0.2f,()=>n++);
            Tick(d,1);Assert(n==0 && d.IsPlayingOneShot,"truncated");
            Tick(d,2.1f);Tick(d,3);Assert(n==1,"duplicate finish");
        });
        Test("Pause freezes frames and release, hit stop slows them",()=>{
            var d=Display(6);int n=0;var p=d.PlayAction(PieceDisplayState.Attack,()=>n++);
            Time.timeScale=0;Tick(d,20);Assert(n==0 && !p.IsDone,"advanced while paused");
            Time.timeScale=0.1f;Tick(d,1);Assert(n==0,"ignored slow motion");
            Time.timeScale=1;Tick(d,0.41f);Assert(n==1,"did not resume");
        });
        Test("Idle selection cannot overwrite action and a hit waits for attack recovery",()=>{
            var d=Display(6);int n=0;var p=d.PlayAction(PieceDisplayState.Attack,()=>n++);
            d.ChangeDisplayState(PieceDisplayState.Idle);d.ChangeDisplayState(PieceDisplayState.Hit,true);
            Tick(d,0.51f);Assert(n==1 && !p.IsCancelled,"lost attack");
            Tick(d,0.51f);Assert(p.IsDone && d.IsPlayingOneShot && d.pieceSpriteRenderer.sprite==d.hitSprite[0],"lost reaction");
            Tick(d,0.7f);Assert(!d.IsPlayingOneShot,"reaction never finished");
        });
        Test("Dodging incoming fire preserves the outgoing attack",()=>{
            var d=Display(6);d.dodgeSprite=Frames(2);int n=0;
            var p=d.PlayAction(PieceDisplayState.Attack,()=>n++);
            d.ChangeDisplayState(PieceDisplayState.Dodge,true);Tick(d,1.1f);
            Assert(n==1 && p.IsDone && !p.IsCancelled && d.pieceSpriteRenderer.sprite==d.dodgeSprite[0],"dodge cancelled paid attack");
            Tick(d,0.4f);Assert(!d.IsPlayingOneShot,"dodge never finished");
        });
        Test("Repeated hit does not restart the same clip",()=>{
            var d=Display(6);d.ChangeDisplayState(PieceDisplayState.Hit,true);Tick(d,0.5f);
            d.ChangeDisplayState(PieceDisplayState.Hit,true);Tick(d,0.2f);Assert(!d.IsPlayingOneShot,"restarted hit");
        });
        Test("Death cancels pending action and its release callback",()=>{
            var d=Display(6);int n=0;var p=d.PlayAction(PieceDisplayState.Attack,()=>n++);
            d.ChangeDisplayState(PieceDisplayState.Death);Tick(d,3);Assert(p.IsCancelled && n==0,"ghost release");
        });
        Test("Completion callback can safely start another animation",()=>{
            var d=Display(1);int n=0;d.ChangeDisplayState(PieceDisplayState.Attack,true,-1,()=>d.ChangeDisplayState(PieceDisplayState.Hit,true,-1,()=>n++));
            Tick(d,0.2f);Tick(d,0.7f);Assert(n==1,"new callback overwritten");
        });
        Test("Release callback can interrupt animation without invoking its old completion",()=>{
            var d=Display(6);var p=d.PlayAction(PieceDisplayState.Attack,()=>d.ChangeDisplayState(PieceDisplayState.Death));
            Tick(d,2);Assert(p.IsCancelled && d.IsPlayingOneShot,"reentrant release corrupted new clip");
        });
        Test("Missing sprites or renderer complete safely on an unpaused frame",()=>{
            foreach(bool missingRenderer in new[]{false,true}) {
                var d=Display(missingRenderer?6:0);if(missingRenderer)d.pieceSpriteRenderer=null;
                int n=0;var p=d.PlayAction(PieceDisplayState.Attack,()=>n++);Tick(d,0.01f);
                Assert(p.IsDone && n==1,"empty clip hung action");
            }
        });
        Test("Invalid skill index and pooled disable cannot leak callbacks",()=>{
            var d=Display(6);int n=0;var p=d.PlayAction(PieceDisplayState.Skill,()=>n++,99);
            Tick(d,0.01f);Assert(n==1 && p.IsDone,"invalid skill hung");
            p=d.PlayAction(PieceDisplayState.Attack,()=>n++);Call(d,"OnDisable");Tick(d,3);
            Assert(n==1 && p.IsCancelled,"disabled playback survived");
        });
        Test("Burst hit can hold its final sprite without keeping action busy",()=>{
            var d=Display(6);d.ChangeDisplayState(PieceDisplayState.Hit);Tick(d,2);
            Assert(!d.IsPlayingOneShot && d.pieceSpriteRenderer.sprite==d.hitSprite.Last(),"held pose still busy");
        });
        Test("Pause preserves remaining hit stop across repeated pause/resume",()=>{
            var t=Clock();t.RequestHitStop(0.1f,1);Tick(t,0.4f);t.PauseTime();Tick(t,20);
            Assert(Time.timeScale==0 && Time.fixedDeltaTime>0,"pause invalid");
            t.ResumeTime();Assert(Time.timeScale==0.1f,"lost hit stop");Tick(t,0.3f);t.PauseTime();t.ResumeTime();
            Tick(t,0.2f);Assert(t.IsHitStopped,"lost remaining duration");Tick(t,0.11f);Assert(Time.timeScale==1,"never recovered");
        });
        Test("Hit during pause stays paused and overlapping weak hit cannot shorten strong hit",()=>{
            var t=Clock();t.PauseTime();t.RequestHitStop(0.1f,1);t.RequestHitStop(0.5f,0.2f);
            Assert(Time.timeScale==0,"unpaused by hit");t.ResumeTime();Tick(t,0.5f);
            Assert(Time.timeScale==0.1f && t.IsHitStopped,"weaker hit overwrote stop");Tick(t,0.51f);Assert(Time.timeScale==1,"failed restore");
        });
        Test("Zero scale recovers in real time and disabling restores original clock",()=>{
            Time.timeScale=0.5f;Time.fixedDeltaTime=0.01f;var t=Clock();t.RequestHitStop(0,0.1f);
            Tick(t,0.11f);Assert(Time.timeScale==0.5f && Time.fixedDeltaTime==0.01f,"wrong baseline");
            t.PauseTime();Call(t,"OnDisable");Assert(Time.timeScale==0.5f && Time.fixedDeltaTime==0.01f,"leaked clock");
        });
        Test("Pool reuse cancels previous expiry",()=>{
            var life=new LifeTime {lifeTime=1};Call(life,"OnEnable");var old=DOVirtual.Last;
            Call(life,"OnDisable");Call(life,"OnEnable");old.Fire();Assert(life.gameObject.activeInHierarchy,"old expiry hid reuse");
            DOVirtual.Last.Fire();Assert(!life.gameObject.activeInHierarchy,"new expiry failed");
        });
        Test("Projectile can take ownership of its pool lifetime",()=>{
            var life=new LifeTime {lifeTime=1};Call(life,"OnEnable");var expiry=DOVirtual.Last;
            life.CancelExpiry();expiry.Fire();Assert(life.gameObject.activeInHierarchy,"expiry survived flight ownership");
        });
        Console.WriteLine($"{passed} presentation regression tests passed.");
    }
}
