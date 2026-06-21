/// The Hollow Rite - ボスAIコントローラ

#pragma once

#include "CoreMinimal.h"
#include "AIController.h"
#include "THRBossAIController.generated.h"

/**
 * ボスを制御するAIコントローラ。
 * ポーズしたボスキャラクターの BossBehaviorTree を起動して戦闘パターンを駆動する。
 * BT アセットの参照はボスキャラクター側（配置インスタンス）で設定する。
 */
UCLASS()
class ATHRBossAIController final : public AAIController
{
	GENERATED_BODY()

protected:
	virtual void OnPossess(APawn* InPawn) override;
};
