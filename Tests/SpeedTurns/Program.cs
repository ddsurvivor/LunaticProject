using System;
using System.Linq;

static class Program
{
    static int passed;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        Console.WriteLine("PASS " + name); passed++;
    }
    static void Main()
    {
        var a = new PieceController { TurnSpeed = 20 };
        var b = new EnemyController { TurnSpeed = 10 };
        var queue = new SpeedTurnQueue<PieceController>();
        queue.Sync(new PieceController[] { a, b });
        for (int i = 0; i < 5; i++) queue.Tick(p => p.TurnSpeed);
        Check(queue.TakeReady() == a && queue.TakeReady() == null && queue.Progress(b) == 50,
            "Faster unit acts first without resetting slower unit progress");
        for (int i = 0; i < 5; i++) queue.Tick(p => p.TurnSpeed);
        Check(queue.TakeReady() == a && queue.TakeReady() == b, "2x speed grants two turns per slower turn");
        queue.Clear(); queue.Sync(new PieceController[] { b, a });
        b.TurnSpeed = 110; a.TurnSpeed = 150;
        queue.Tick(p => p.TurnSpeed);
        Check(queue.TakeReady() == a && queue.TakeReady() == b && queue.Progress(a) == 50,
            "Same-tick crossing uses actual arrival time and keeps overflow");
        queue.Clear(); queue.Sync(new PieceController[] { b, a });
        b.TurnSpeed = 100; a.TurnSpeed = 250;
        queue.Tick(p => p.TurnSpeed);
        Check(queue.TakeReady() == a && queue.TakeReady() == a && queue.TakeReady() == b,
            "Speeds above 100 preserve chronological multiple crossings");
        queue.Clear(); queue.Sync(new PieceController[] { a, b });
        a.TurnSpeed = b.TurnSpeed = 100; queue.Tick(p => p.TurnSpeed);
        Check(queue.TakeReady() == a && queue.TakeReady() == b, "Exact ties have stable registration order");
        queue.Tick(p => p.TurnSpeed); queue.Sync(new[] { a });
        Check(queue.TakeReady() == a && queue.TakeReady() == null, "Removed ready unit cannot act");
        queue.Sync(new PieceController[] { a, b, b });
        Check(queue.Progress(b) == 0, "Revival or reinforcement starts empty and registration deduplicates");
        queue.Tick(p => 100);
        Check(queue.TakeReady() == a && queue.TakeReady() == b && queue.TakeReady() == null,
            "Duplicate unit receives only one turn");
        queue.Clear(); queue.Sync(new[] { a }); queue.Tick(p => double.NaN);
        Check(queue.Progress(a) == 10, "Invalid legacy speed uses safe default");

        var battle = BattleManager.Fixture();
        a = new PieceController { TurnSpeed = 100 };
        b = new EnemyController { TurnSpeed = 100 };
        var ally = new PieceController { TurnSpeed = 100 };
        battle.PlayerController.pieces.AddRange(new[] { a, ally });
        battle.AIController.pieces.Add(b);
        battle.StartFixture(); battle.Until(() => battle.ActivePiece == a);
        Check(a.Starts == 1 && ally.Starts == 0 && b.Starts == 0 && battle.buffManager.Starts.SequenceEqual(new[] { a }),
            "Starting a personal turn refreshes and processes buffs only on its owner");
        Check(battle.CanControlPiece(a) && !battle.CanControlPiece(ally) && !battle.CanControlPiece(b),
            "Only current player unit can accept input");
        var frozen = battle.ActionProgress(b);
        for (int i = 0; i < 5; i++) battle.Step();
        Check(battle.ActivePiece == a && battle.ActionProgress(b) == frozen, "Waiting for player input pauses speed clock");
        a.unitAttrCenter.CurMovePoint = 0; battle.HasCombatPresentation = true;
        battle.Step(); battle.Step();
        Check(battle.ActivePiece == a && a.Ends == 0, "Last AP waits for combat presentation before turn-end buffs");
        battle.HasCombatPresentation = false; battle.moveManager.IsMoving = true;
        battle.Step(); Check(a.Ends == 0, "Movement must finish before handoff");
        battle.moveManager.IsMoving = false;
        battle.Until(() => battle.ActivePiece == b);
        Check(a.Ends == 1 && b.Starts == 1 && ally.Starts == 0 && battle.AIController.isInTurn,
            "Equal-speed enemy follows player and only completed owner ends its turn");
        b.isDead = true; battle.Until(() => battle.ActivePiece == ally);
        Check(ally.Starts == 1 && !battle.AIController.isInTurn, "Death of current enemy cannot stall next actor");
        ally.isIdle = true;
        battle.Until(() => battle.ActivePiece == a);
        Check(ally.Ends == 1, "Standby hands off without a global end-turn button");
        battle.StopFixture();
        Check(battle.ActivePiece == null && !battle.PlayerController.isInTurn && !battle.SpeedTurnsActive,
            "Stopping battle removes active ownership and input permission");

        battle = BattleManager.Fixture();
        a = new PieceController { TurnSpeed = 100 };
        b = new EnemyController { TurnSpeed = 100 };
        battle.PlayerController.pieces.Add(a); battle.AIController.pieces.Add(b);
        battle.buffManager.OnStart = p => { if (p == a) p.unitAttrCenter.CurMovePoint = 0; };
        battle.StartFixture(); battle.Until(() => battle.ActivePiece == b);
        Check(a.Starts == 1 && a.Ends == 1 && !battle.CanControlPiece(a),
            "Start-of-turn stun skips input and settles exactly one personal duration");
        Console.WriteLine($"{passed} speed-turn regression tests passed.");
    }
}
