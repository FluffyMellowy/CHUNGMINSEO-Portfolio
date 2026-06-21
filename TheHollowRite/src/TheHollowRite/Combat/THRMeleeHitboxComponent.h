/// The Hollow Rite - 近接攻撃ヒットボックスコンポーネント

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "THRMeleeHitboxComponent.generated.h"

/**
 * 近接攻撃の当たり判定ウィンドウを管理するコンポーネント。
 * アニメーション通知（UAnimNotifyState_THRMeleeHit）からウィンドウを開閉し、
 * 開いている間は所有者の前方をスイープして UTHRHealthComponent を持つ対象にダメージを与える。
 * プレイヤー・ボスの双方で再利用する。
 */
UCLASS(ClassGroup=(THR), meta=(BlueprintSpawnableComponent))
class UTHRMeleeHitboxComponent final : public UActorComponent
{
	GENERATED_BODY()

public:
	UTHRMeleeHitboxComponent();

	virtual void TickComponent(float DeltaTime, ELevelTick TickType,
		FActorComponentTickFunction* ThisTickFunction) override;

	/// 攻撃判定ウィンドウを開く（アニメ通知の開始時に呼ぶ）
	UFUNCTION(BlueprintCallable, Category="THR|Combat")
	void OpenWindow(float InDamage, float InRange, float InRadius);

	/// 攻撃判定ウィンドウを閉じる（アニメ通知の終了時に呼ぶ）
	UFUNCTION(BlueprintCallable, Category="THR|Combat")
	void CloseWindow();

	UFUNCTION(BlueprintPure, Category="THR|Combat")
	bool IsWindowOpen() const { return bWindowOpen; }

protected:
	/// 判定スイープをデバッグ描画するか
	UPROPERTY(EditAnywhere, Category="THR|Combat")
	bool bDrawDebug = false;

	/// 判定ウィンドウが開いているか
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Combat")
	bool bWindowOpen = false;

private:
	/* 現在のウィンドウのパラメータ（OpenWindow で設定） */
	float CurrentDamage = 0.0f;
	float CurrentRange = 150.0f;
	float CurrentRadius = 60.0f;

	/* このウィンドウで既にヒットしたアクター（多重ヒット防止） */
	UPROPERTY()
	TArray<TObjectPtr<AActor>> HitActorsThisWindow;
};
