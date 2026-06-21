/// The Hollow Rite - 弾幕の弾（オブジェクトプール対象）

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "THRBullet.generated.h"

class UStaticMeshComponent;
class UTHRHealthComponent;

/**
 * 弾幕を構成する1発の弾。オブジェクトプールで再利用される。
 *
 * パフォーマンス設計：1対1ゲームのため弾ごとの物理コリジョンは使わず、
 * 唯一の標的（プレイヤー）との距離計算のみで命中判定する（O(弾数) の純粋計算）。
 * これにより数百発でも物理オーバーヘッドが発生しない。
 */
UCLASS()
class ATHRBullet final : public AActor
{
	GENERATED_BODY()

public:
	ATHRBullet();

	virtual void Tick(float DeltaSeconds) override;

	/// プールから取り出して発射する
	void Activate(const FVector& InLocation, const FVector& InVelocity,
		float InDamage, float InRadius, float InLifetime, AActor* InTarget);

	/// プールへ戻す（非表示・Tick停止）
	void Deactivate();

	/// 現在使用中か
	bool IsActive() const { return bActive; }

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UStaticMeshComponent> Mesh;

private:
	bool bActive = false;
	FVector Velocity = FVector::ZeroVector;
	float Damage = 0.0f;
	float HitRadius = 24.0f;
	float RemainingLife = 0.0f;

	TWeakObjectPtr<AActor> Target;
	TWeakObjectPtr<UTHRHealthComponent> TargetHealth;

	/* 標的の当たり半径の目安（プレイヤーカプセル相当） */
	static constexpr float TargetHitRadius = 45.0f;
};
