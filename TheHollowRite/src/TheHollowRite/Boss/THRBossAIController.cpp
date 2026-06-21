/// The Hollow Rite - ボスAIコントローラ

#include "THRBossAIController.h"
#include "THRBossCharacter.h"
#include "BehaviorTree/BehaviorTree.h"
#include "TheHollowRite.h"

void ATHRBossAIController::OnPossess(APawn* InPawn)
{
	Super::OnPossess(InPawn);

	/* ボスキャラクターに割り当てられた Behavior Tree を起動する */
	if (const ATHRBossCharacter* Boss = Cast<ATHRBossCharacter>(InPawn))
	{
		if (Boss->BossBehaviorTree)
		{
			RunBehaviorTree(Boss->BossBehaviorTree);
			return;
		}
	}

	UE_LOG(LogTheHollowRite, Warning,
		TEXT("[BossAI] %s に BossBehaviorTree が割り当てられていません"), *GetNameSafe(InPawn));
}
