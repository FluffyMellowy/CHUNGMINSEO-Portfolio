/// The Hollow Rite - ボス戦闘状態更新サービス

#pragma once

#include "CoreMinimal.h"
#include "BehaviorTree/BTService.h"
#include "THRBTService_UpdateCombat.generated.h"

/**
 * ボスの戦闘状態を定期更新する Behavior Tree サービス。
 * - TargetActor: プレイヤーポーンを書き込む
 * - Distance: ボス〜ターゲット間距離（cm）を書き込む
 * - IsDead: ボスの死亡状態を書き込む
 * あわせて AIController の Focus をターゲットに設定し、ボスが常にプレイヤーの方を向くようにする。
 */
UCLASS(meta=(DisplayName="THR Update Combat"))
class UTHRBTService_UpdateCombat final : public UBTService
{
	GENERATED_BODY()

public:
	UTHRBTService_UpdateCombat();

protected:
	virtual void TickNode(UBehaviorTreeComponent& OwnerComp, uint8* NodeMemory, float DeltaSeconds) override;

	/// プレイヤーポーンを書き込む Blackboard キー（Object 型）
	UPROPERTY(EditAnywhere, Category="THR|Blackboard")
	FBlackboardKeySelector TargetActorKey;

	/// ターゲットまでの距離を書き込む Blackboard キー（Float 型）
	UPROPERTY(EditAnywhere, Category="THR|Blackboard")
	FBlackboardKeySelector DistanceKey;

	/// ボスの死亡状態を書き込む Blackboard キー（Bool 型）
	UPROPERTY(EditAnywhere, Category="THR|Blackboard")
	FBlackboardKeySelector IsDeadKey;
};
