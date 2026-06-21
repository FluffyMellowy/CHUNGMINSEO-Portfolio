/// The Hollow Rite - プレイヤー投射体（ブラスター弾）

#include "THRProjectile.h"
#include "Combat/THRHealthComponent.h"
#include "Components/SphereComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/ProjectileMovementComponent.h"
#include "UObject/ConstructorHelpers.h"

ATHRProjectile::ATHRProjectile()
{
	PrimaryActorTick.bCanEverTick = false;

	/* 当たり判定スフィア（ルート） */
	CollisionSphere = CreateDefaultSubobject<USphereComponent>(TEXT("CollisionSphere"));
	CollisionSphere->InitSphereRadius(24.0f);
	CollisionSphere->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
	CollisionSphere->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	SetRootComponent(CollisionSphere);

	/* 可視化用の小さなスフィアメッシュ（グレイボックス） */
	UStaticMeshComponent* Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
	Visual->SetupAttachment(CollisionSphere);
	Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	Visual->SetRelativeScale3D(FVector(0.4f, 0.4f, 0.4f));
	static ConstructorHelpers::FObjectFinder<UStaticMesh> SphereMeshFinder(
		TEXT("/Engine/BasicShapes/Sphere.Sphere"));
	if (SphereMeshFinder.Succeeded())
	{
		Visual->SetStaticMesh(SphereMeshFinder.Object);
	}

	/* 直進移動 */
	ProjectileMovement = CreateDefaultSubobject<UProjectileMovementComponent>(TEXT("ProjectileMovement"));
	ProjectileMovement->bRotationFollowsVelocity = true;
	ProjectileMovement->ProjectileGravityScale = 0.0f;
}

void ATHRProjectile::InitProjectile(float InDamage, AActor* InShooter)
{
	Damage = InDamage;
	Shooter = InShooter;
	if (InShooter)
	{
		CollisionSphere->IgnoreActorWhenMoving(InShooter, true);
	}
}

void ATHRProjectile::BeginPlay()
{
	Super::BeginPlay();

	ProjectileMovement->InitialSpeed = Speed;
	ProjectileMovement->MaxSpeed = Speed;
	ProjectileMovement->Velocity = GetActorForwardVector() * Speed;

	CollisionSphere->OnComponentBeginOverlap.AddDynamic(this, &ATHRProjectile::OnSphereOverlap);
	SetLifeSpan(Lifetime);
}

void ATHRProjectile::OnSphereOverlap(UPrimitiveComponent* OverlappedComp, AActor* OtherActor,
	UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& Sweep)
{
	if (OtherActor == nullptr || OtherActor == this || OtherActor == Shooter.Get())
	{
		return;
	}

	/* 体力コンポーネントを持つ対象に命中：ダメージ後に消滅 */
	if (UTHRHealthComponent* TargetHealth = OtherActor->FindComponentByClass<UTHRHealthComponent>())
	{
		TargetHealth->ApplyDamage(Damage);
		Destroy();
		return;
	}

	/* 地形（WorldStatic）に命中した場合も消滅 */
	if (OtherComp && OtherComp->GetCollisionObjectType() == ECC_WorldStatic)
	{
		Destroy();
	}
}
