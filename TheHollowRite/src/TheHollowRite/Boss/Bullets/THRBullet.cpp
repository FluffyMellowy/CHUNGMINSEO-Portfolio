/// The Hollow Rite - 弾幕の弾（オブジェクトプール対象）

#include "THRBullet.h"
#include "Combat/THRHealthComponent.h"
#include "Components/StaticMeshComponent.h"
#include "UObject/ConstructorHelpers.h"

ATHRBullet::ATHRBullet()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.bStartWithTickEnabled = false;

	Mesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Mesh"));
	Mesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	Mesh->SetRelativeScale3D(FVector(0.45f, 0.45f, 0.45f));
	SetRootComponent(Mesh);

	static ConstructorHelpers::FObjectFinder<UStaticMesh> SphereMeshFinder(
		TEXT("/Engine/BasicShapes/Sphere.Sphere"));
	if (SphereMeshFinder.Succeeded())
	{
		Mesh->SetStaticMesh(SphereMeshFinder.Object);
	}

	SetActorHiddenInGame(true);
}

void ATHRBullet::Activate(const FVector& InLocation, const FVector& InVelocity,
	float InDamage, float InRadius, float InLifetime, AActor* InTarget)
{
	Velocity = InVelocity;
	Damage = InDamage;
	HitRadius = InRadius;
	RemainingLife = InLifetime;
	Target = InTarget;
	TargetHealth = InTarget ? InTarget->FindComponentByClass<UTHRHealthComponent>() : nullptr;

	SetActorLocation(InLocation);
	SetActorScale3D(FVector(InRadius / 50.0f)); /* 半径に応じて見た目を調整（基準球50cm） */
	bActive = true;
	SetActorHiddenInGame(false);
	SetActorTickEnabled(true);
}

void ATHRBullet::Deactivate()
{
	bActive = false;
	SetActorHiddenInGame(true);
	SetActorTickEnabled(false);
	Velocity = FVector::ZeroVector;
}

void ATHRBullet::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	if (!bActive)
	{
		return;
	}

	/* 移動 */
	SetActorLocation(GetActorLocation() + Velocity * DeltaSeconds);

	/* 寿命 */
	RemainingLife -= DeltaSeconds;
	if (RemainingLife <= 0.0f)
	{
		Deactivate();
		return;
	}

	/* 命中判定：唯一の標的との距離のみ評価（物理コリジョン不使用） */
	AActor* TargetActor = Target.Get();
	if (TargetActor == nullptr)
	{
		return;
	}

	const float DistSq = FVector::DistSquared(GetActorLocation(), TargetActor->GetActorLocation());
	const float HitDist = HitRadius + TargetHitRadius;
	if (DistSq <= HitDist * HitDist)
	{
		if (UTHRHealthComponent* Health = TargetHealth.Get())
		{
			/* 回避無敵中は HealthComponent 側で無効化される */
			Health->ApplyDamage(Damage);
		}
		Deactivate();
	}
}
