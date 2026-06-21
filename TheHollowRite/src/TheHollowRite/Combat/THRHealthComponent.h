/// The Hollow Rite - 体力コンポーネント（プレイヤー・ボス共用）

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "THRHealthComponent.generated.h"

/// 死亡時に発火するデリゲート
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FTHROnDeathSignature);

/// 体力変化時に発火するデリゲート（現在値・最大値）
DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FTHROnHealthChangedSignature, float, NewHealth, float, MaxHealth);

/**
 * アクターの体力・ダメージ・死亡を管理する共用コンポーネント。
 * プレイヤーとボスの双方に付与し、HUD やゲームロジックはデリゲート経由で状態を購読する。
 */
UCLASS(ClassGroup=(THR), meta=(BlueprintSpawnableComponent))
class UTHRHealthComponent final : public UActorComponent
{
	GENERATED_BODY()

public:
	UTHRHealthComponent();

	/// 指定量のダメージを適用する。無敵中・死亡済みの場合は無視される。
	UFUNCTION(BlueprintCallable, Category="THR|Health")
	void ApplyDamage(float Amount);

	/// 指定量回復する。死亡済みの場合は無視される。
	UFUNCTION(BlueprintCallable, Category="THR|Health")
	void Heal(float Amount);

	/// 無敵状態を設定する（回避の無敵フレームなどで使用）。
	UFUNCTION(BlueprintCallable, Category="THR|Health")
	void SetInvulnerable(bool bNewInvulnerable);

	/// 最大体力を設定し、必要なら現在値もリセットする（ボスの初期化用）。
	UFUNCTION(BlueprintCallable, Category="THR|Health")
	void SetMaxHealth(float NewMaxHealth, bool bResetCurrent = true);

	UFUNCTION(BlueprintPure, Category="THR|Health")
	float GetHealth() const { return CurrentHealth; }

	UFUNCTION(BlueprintPure, Category="THR|Health")
	float GetMaxHealth() const { return MaxHealth; }

	UFUNCTION(BlueprintPure, Category="THR|Health")
	float GetHealthPercent() const { return MaxHealth > 0.0f ? CurrentHealth / MaxHealth : 0.0f; }

	UFUNCTION(BlueprintPure, Category="THR|Health")
	bool IsDead() const { return bIsDead; }

	/* ── デリゲート ─────────────────────────────────────────── */

	/// 死亡時にブロードキャストされる
	UPROPERTY(BlueprintAssignable, Category="THR|Health")
	FTHROnDeathSignature OnDeath;

	/// 体力が変化するたびにブロードキャストされる
	UPROPERTY(BlueprintAssignable, Category="THR|Health")
	FTHROnHealthChangedSignature OnHealthChanged;

protected:
	virtual void BeginPlay() override;

	/// 最大体力
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Health")
	float MaxHealth = 100.0f;

	/// 現在の体力
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Health")
	float CurrentHealth = 100.0f;

	/// 無敵フラグ（true の間はダメージを受けない）
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Health")
	bool bIsInvulnerable = false;

	/// 死亡フラグ
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Health")
	bool bIsDead = false;
};
