
    using UnityEngine;

    public class BattleManager: MonoBehaviour
    {
        public AIController AIController;

        public PlayerController PlayerController;

        public void Init()
        {
            foreach (var piece in PlayerController.pieces)
            {
                piece.Init();
            }

            foreach (var piece in AIController.pieces)   
            {
                piece.Init();
            }
            PlayerStart();
        }
        public void ChangeTurn()
        {
            if (PlayerController.isInTurn)
            {
                PlayerController.isInTurn = false;
                PlayerController.TurnEnd();
                AIController.isInTurn = true;
                AIController.TurnStart();
                BattleScene.Ins.UM.turnPanel.ShowTurnChange("敌人回合");
            }
            else
            {
                PlayerStart();
            }
        }

        public void PlayerStart()
        {
            AIController.isInTurn = false;
            PlayerController.isInTurn = true;
            AIController.TurnEnd();
            PlayerController.TurnStart();
            BattleScene.Ins.UM.endTurnButton.gameObject.SetActive(true);
            BattleScene.Ins.UM.turnPanel.ShowTurnChange("玩家回合");
        }
    }
