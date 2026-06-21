/// The Hollow Rite - プレイヤー投射体（ブラスター弾）

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "THRProjectile.generated.h"

class USphereComponent;
class UProjectileMovementComponent;

/**
 * ブラスターが発射するエネルギー弾。
 * 前方へ直進し、UTHRHealthComponent を持つ発射者以外のポーンに命中するとダメージを与えて消滅する。
 * ワールドスタティックに当たっても消滅。一定時間で自動消滅。
 */
UCLASS()
class ATHRProjectile final : public AActor
{
	GENERATED_BODY()

public:
	ATHRProjectile();

	/// 発射直後に呼び、ダメージと発射者を設定する
	void InitProjectile(float InDamage, AActor* InShooter);

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<USphereComponent> CollisionSphere;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UProjectileMovementComponent> ProjectileMovement;

	/// 弾速（cm/秒）
	UPROPERTY(EditAnywhere, Category="THR|Projectile")
	float Speed = 3000.0f;

	/// 自動消滅までの寿命（秒）
	UPROPERTY(EditAnywhere, Category="THR|Projectile")
	float Lifetime = 3.0f;

	virtual void BeginPlay() override;

	UFUNCTION()
	void OnSphereOverlap(UPrimitiveComponent* OverlappedComp, AActor* OtherActor,
		UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& Sweep);

private:
	float Damage = 12.0f;
	TWeakObjectPtr<AActor> Shooter;
};
