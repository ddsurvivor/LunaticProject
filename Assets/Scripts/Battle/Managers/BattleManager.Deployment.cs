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
