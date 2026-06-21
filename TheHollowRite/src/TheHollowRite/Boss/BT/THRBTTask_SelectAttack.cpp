/// The Hollow Rite - ボス攻撃選択・実行タスク

#include "THRBTTask_SelectAttack.h"
#include "AIController.h"
#include "Boss/THRBossCharacter.h"
#include "Boss/THRBossAttackDef.h"
#include "Kismet/GameplayStatics.h"

UTHRBTTask_SelectAttack::UTHRBTTask_SelectAttack()
{
	NodeName = TEXT("THR Select Attack");

	/* モンタージュ終了まで TickTask で待機する */
	bNotifyTick = true;
}

uint16 UTHRBTTask_SelectAttack::GetInstanceMemorySize() const
{
	return sizeof(FTHRSelectAttackMemory);
}

EBTNodeResult::Type UTHRBTTask_SelectAttack::ExecuteTask(UBehaviorTreeComponent& OwnerComp, uint8* NodeMemory)
{
	AAIController* AIC = OwnerComp.GetAIOwner();
	ATHRBossCharacter* Boss = AIC ? Cast<ATHRBossCharacter>(AIC->GetPawn()) : nullptr;
	if (Boss == nullptr || Boss->CurrentPhase == ETHRBossPhase::Dead)
	{
		return EBTNodeResult::Failed;
	}

	const APawn* Target = UGameplayStatics::GetPlayerPawn(Boss, 0);
	if (Target == nullptr)
	{
		return EBTNodeResult::Failed;
	}

	const float Distance = FVector::Dist(Boss->GetActorLocation(), Target->GetActorLocation());

	/* 距離帯・クールダウンを満たす候補を収集 */
	TArray<const FTHRBossAttackDef*> Candidates;
	float TotalWeight = 0.0f;

	const int32 CurrentPhaseInt = Boss->GetPhaseAsInt();

	for (const FTHRBossAttackDef& Def : Boss->AttackTable)
	{
		if (Def.Montage == nullptr)
		{
			continue;
		}
		if (Def.MinPhase > CurrentPhaseInt)
		{
			continue;
		}
		if (Distance < Def.MinRange || Distance > Def.MaxRange)
		{
			continue;
		}
		if (Boss->IsAttackOnCooldown(Def))
		{
			continue;
		}

		Candidates.Add(&Def);
		TotalWeight += FMath::Max(Def.Weight, 0.0f);
	}

	if (Candidates.IsEmpty() || TotalWeight <= 0.0f)
	{
		return EBTNodeResult::Failed;
	}

	/* 加重ランダムで1つ選択 */
	const FTHRBossAttackDef* Chosen = Candidates.Last();
	float Roll = FMath::FRandRange(0.0f, TotalWeight);
	for (const FTHRBossAttackDef* Candidate : Candidates)
	{
		Roll -= FMath::Max(Candidate->Weight, 0.0f);
		if (Roll <= 0.0f)
		{
			Chosen = Candidate;
			break;
		}
	}

	const float Duration = Boss->PlayAttack(*Chosen);
	if (Duration <= 0.0f)
	{
		return EBTNodeResult::Failed;
	}

	/* モンタージュ終了まで待機 */
	FTHRSelectAttackMemory* Memory = reinterpret_cast<FTHRSelectAttackMemory*>(NodeMemory);
	Memory->RemainingTime = Duration;
	return EBTNodeResult::InProgress;
}

void UTHRBTTask_SelectAttack::TickTask(UBehaviorTreeComponent& OwnerComp, uint8* NodeMemory, float DeltaSeconds)
{
	Super::TickTask(OwnerComp, NodeMemory, DeltaSeconds);

	FTHRSelectAttackMemory* Memory = reinterpret_cast<FTHRSelectAttackMemory*>(NodeMemory);
	Memory->RemainingTime -= DeltaSeconds;

	if (Memory->RemainingTime <= 0.0f)
	{
		FinishLatentTask(OwnerComp, EBTNodeResult::Succeeded);
	}
}
