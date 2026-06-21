/// The Hollow Rite - ボス戦闘状態更新サービス

#include "THRBTService_UpdateCombat.h"
#include "AIController.h"
#include "BehaviorTree/BlackboardComponent.h"
#include "Boss/THRBossCharacter.h"
#include "Kismet/GameplayStatics.h"

UTHRBTService_UpdateCombat::UTHRBTService_UpdateCombat()
{
	NodeName = TEXT("THR Update Combat");

	/* 0.2秒間隔で十分（毎フレーム更新は不要） */
	Interval = 0.2f;
	RandomDeviation = 0.0f;
}

void UTHRBTService_UpdateCombat::TickNode(UBehaviorTreeComponent& OwnerComp, uint8* NodeMemory, float DeltaSeconds)
{
	Super::TickNode(OwnerComp, NodeMemory, DeltaSeconds);

	AAIController* AIC = OwnerComp.GetAIOwner();
	UBlackboardComponent* Blackboard = OwnerComp.GetBlackboardComponent();
	if (AIC == nullptr || Blackboard == nullptr)
	{
		return;
	}

	ATHRBossCharacter* Boss = Cast<ATHRBossCharacter>(AIC->GetPawn());
	if (Boss == nullptr)
	{
		return;
	}

	APawn* Target = UGameplayStatics::GetPlayerPawn(Boss, 0);

	/* Blackboard へ戦闘状態を書き込む */
	Blackboard->SetValueAsObject(TargetActorKey.SelectedKeyName, Target);
	Blackboard->SetValueAsBool(IsDeadKey.SelectedKeyName, Boss->CurrentPhase == ETHRBossPhase::Dead);

	if (Target)
	{
		const float Distance = FVector::Dist(Boss->GetActorLocation(), Target->GetActorLocation());
		Blackboard->SetValueAsFloat(DistanceKey.SelectedKeyName, Distance);

		/* ターゲットを注視（bUseControllerDesiredRotation でボスが滑らかに向き直る） */
		AIC->SetFocus(Target);
	}
}
