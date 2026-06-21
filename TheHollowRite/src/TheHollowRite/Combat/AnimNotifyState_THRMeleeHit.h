/// The Hollow Rite - 近接攻撃ヒット用アニメーション通知ステート

#pragma once

#include "CoreMinimal.h"
#include "Animation/AnimNotifies/AnimNotifyState.h"
#include "AnimNotifyState_THRMeleeHit.generated.h"

/**
 * 近接攻撃の当たり判定ウィンドウをアニメーションから駆動する通知ステート。
 * 通知区間の開始で所有者の UTHRMeleeHitboxComponent を開き、終了で閉じる。
 * 攻撃ごとのダメージ・リーチ・半径はこの通知で設定する。
 */
UCLASS(meta=(DisplayName="THR Melee Hit"))
class UAnimNotifyState_THRMeleeHit final : public UAnimNotifyState
{
	GENERATED_BODY()

public:
	virtual void NotifyBegin(USkeletalMeshComponent* MeshComp, UAnimSequenceBase* Animation,
		float TotalDuration, const FAnimNotifyEventReference& EventReference) override;

	virtual void NotifyEnd(USkeletalMeshComponent* MeshComp, UAnimSequenceBase* Animation,
		const FAnimNotifyEventReference& EventReference) override;

protected:
	/// この攻撃のダメージ
	UPROPERTY(EditAnywhere, Category="THR|Combat")
	float Damage = 25.0f;

	/// 前方リーチ（cm）
	UPROPERTY(EditAnywhere, Category="THR|Combat")
	float Range = 200.0f;

	/// 判定半径（cm）
	UPROPERTY(EditAnywhere, Category="THR|Combat")
	float Radius = 70.0f;
};
