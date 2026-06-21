/// The Hollow Rite - ボス攻撃選択・実行タスク

#pragma once

#include "CoreMinimal.h"
#include "BehaviorTree/BTTaskNode.h"
#include "THRBTTask_SelectAttack.generated.h"

/**
 * ボスの AttackTable から実行可能な攻撃を選んで実行する Behavior Tree タスク。
 * - 距離帯（MinRange〜MaxRange）とクールダウンを満たす候補を収集
 * - 加重ランダムで1つ選択し、モンタージュを再生
 * - 再生終了まで InProgress を維持し、終了時に Succeeded を返す
 * 候補が1つもなければ即座に Failed（→ Selector が MoveTo へフォールバック）。
 */
UCLASS(meta=(DisplayName="THR Select Attack"))
class UTHRBTTask_SelectAttack final : public UBTTaskNode
{
	GENERATED_BODY()

public:
	UTHRBTTask_SelectAttack();

protected:
	virtual EBTNodeResult::Type ExecuteTask(UBehaviorTreeComponent& OwnerComp, uint8* NodeMemory) override;
	virtual void TickTask(UBehaviorTreeComponent& OwnerComp, uint8* NodeMemory, float DeltaSeconds) override;
	virtual uint16 GetInstanceMemorySize() const override;
};

/* タスクのインスタンスメモリ（モンタージュ残り時間） */
struct FTHRSelectAttackMemory
{
	float RemainingTime = 0.0f;
};
