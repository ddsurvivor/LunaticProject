using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private BattleDeploymentController deploymentController;
    private bool deploymentConfirmed;
    public bool IsDeploying => startupPhase == StartupPhase.Deployment;

    private bool BeginDeploymentIfAvailable()
    {
        if (deploymentConfirmed) return false;
        var areas = FindObjectsOfType<BattleDeploymentArea>()
            .Where(area => area.isActiveAndEnabled && area.gameObject.scene == gameObject.scene).ToArray();
        // 旧关卡没有部署区域时，重开仍在原出生点附近提供可调整且经正式导航验证的区域。
        if (areas.Length == 0 && GM.Ins.IsRestartedBattle)
            areas = PlayerController.pieces.Where(p => p != null && p.gameObject.activeInHierarchy &&
                p.isPlayerPiece && !p.isDead && !p.cantControl)
                .Select(piece =>
                {
                    var area = new GameObject("RestartDeploymentArea").AddComponent<BattleDeploymentArea>();
                    area.transform.SetParent(transform, false);
                    var agent = piece.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    area.transform.position = piece.transform.position - Vector3.up * (agent != null ? agent.baseOffset : 0f);
                    float radius = agent != null ? agent.radius : 0.5f;
                    area.size = Vector2.one * Mathf.Max(6f, radius * 4f + 2f);
                    return area;
                }).ToArray();
        if (areas.Length == 0) return false;
        startupPhase = StartupPhase.Deployment;
        deploymentController = GetComponent<BattleDeploymentController>();
        if (deploymentController == null) deploymentController = gameObject.AddComponent<BattleDeploymentController>();
        deploymentController.Begin(this, areas);
        return true;
    }

    public void ConfirmDeployment()
    {
        if (!IsDeploying || deploymentController == null || !deploymentController.ValidateAll()) return;
        deploymentConfirmed = true;
        deploymentController.End();
        startupPhase = StartupPhase.Ready;
        StartBattle();
    }

    private void EndDeploymentSession()
    {
        if (deploymentController != null) deploymentController.End();
        deploymentConfirmed = false;
    }
}
