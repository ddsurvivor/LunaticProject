using UnityEngine;

public partial class PieceController
{
    private AudioManager.ClipLoopHandle movementAudio; // 每次真实移动仅持有一个循环，不能按同名素材停止其他棋子。

    /// <summary>由实际导航驱动脚步循环；静止、暂停、死亡或缺失素材时立即静音，不播放单次尾音。</summary>
    internal void UpdateMovementAudio(bool moving)
    {
        if (!moving || !isActiveAndEnabled || isDead || _pieceData == null)
        {
            StopMovementAudio();
            return;
        }
        // StopAll取消的旧句柄保留到本次移动结束，避免下一帧自动重启被全局停止的音频。
        if (movementAudio != null) return;
        if (_pieceData.actionSounds.TryGetValue(ActionType.Move, out var clip) && clip != null)
            movementAudio = GM.Ins?.AM?.PlayClipLoop(clip);
    }

    /// <summary>释放本棋子的脚步声；到达、中断、复用及退出均可重复调用。</summary>
    internal void StopMovementAudio()
    {
        movementAudio?.Stop();
        movementAudio = null;
    }
}
