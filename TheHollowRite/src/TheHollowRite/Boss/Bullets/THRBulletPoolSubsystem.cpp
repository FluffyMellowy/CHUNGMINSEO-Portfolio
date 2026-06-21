/// The Hollow Rite - 弾幕オブジェクトプール

#include "THRBulletPoolSubsystem.h"
#include "THRBullet.h"
#include "Engine/World.h"
#include "TimerManager.h"
#include "TheHollowRite.h"

ATHRBullet* UTHRBulletPoolSubsystem::AcquireBullet()
{
	/* 空き弾を再利用 */
	for (ATHRBullet* Bullet : Pool)
	{
		if (Bullet && !Bullet->IsActive())
		{
			return Bullet;
		}
	}

	/* 上限内なら新規スポーン */
	if (Pool.Num() >= MaxBullets)
	{
		return nullptr;
	}

	UWorld* World = GetWorld();
	if (World == nullptr)
	{
		return nullptr;
	}

	FActorSpawnParameters Params;
	Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
	ATHRBullet* NewBullet = World->SpawnActor<ATHRBullet>(
		ATHRBullet::StaticClass(), FVector::ZeroVector, FRotator::ZeroRotator, Params);
	if (NewBullet)
	{
		Pool.Add(NewBullet);
	}
	return NewBullet;
}

void UTHRBulletPoolSubsystem::FirePattern(const FTHRBulletPatternDef& Pattern, const FVector& Origin, AActor* Target)
{
	/* 各波をスケジュール */
	for (int32 WaveIndex = 0; WaveIndex < Pattern.WaveCount; ++WaveIndex)
	{
		const float Delay = WaveIndex * Pattern.WaveInterval;
		if (Delay <= 0.0f)
		{
			FireWave(Pattern, Origin, Target, WaveIndex);
		}
		else
		{
			FTimerHandle Handle;
			TWeakObjectPtr<AActor> WeakTarget = Target;
			GetWorld()->GetTimerManager().SetTimer(
				Handle,
				FTimerDelegate::CreateWeakLambda(this, [this, Pattern, Origin, WeakTarget, WaveIndex]()
				{
					FireWave(Pattern, Origin, WeakTarget, WaveIndex);
				}),
				Delay, false);
		}
	}
}

void UTHRBulletPoolSubsystem::FireWave(FTHRBulletPatternDef Pattern, FVector Origin,
	TWeakObjectPtr<AActor> Target, int32 WaveIndex)
{
	AActor* TargetActor = Target.Get();

	/* 基準角度（度）：Aimed/Wave は標的方向、それ以外はワールド+X 基準 */
	float BaseAngle = 0.0f;
	if ((Pattern.Emitter == ETHRBulletEmitter::Aimed || Pattern.Emitter == ETHRBulletEmitter::Wave)
		&& TargetActor)
	{
		FVector ToTarget = TargetActor->GetActorLocation() - Origin;
		ToTarget.Z = 0.0f;
		BaseAngle = FMath::RadiansToDegrees(FMath::Atan2(ToTarget.Y, ToTarget.X));
	}

	/* Spiral：波ごとに回転 */
	if (Pattern.Emitter == ETHRBulletEmitter::Spiral)
	{
		BaseAngle += Pattern.SpinDegPerWave * WaveIndex;
	}

	/* Wave：波ごとに扇内を往復 */
	float WaveOffset = 0.0f;
	if (Pattern.Emitter == ETHRBulletEmitter::Wave && Pattern.WaveCount > 1)
	{
		const float T = static_cast<float>(WaveIndex) / (Pattern.WaveCount - 1); /* 0..1 */
		WaveOffset = (FMath::Sin(T * PI * 2.0f)) * (Pattern.ArcDeg * 0.5f);
	}

	const int32 Count = FMath::Max(1, Pattern.BulletsPerWave);

	/* 全周（360）か扇かで角度ステップを決める */
	const bool bFullCircle = (Pattern.Emitter == ETHRBulletEmitter::Radial && Pattern.ArcDeg >= 359.0f);
	const float Arc = bFullCircle ? 360.0f : Pattern.ArcDeg;
	const float Step = (Count > 1) ? Arc / (bFullCircle ? Count : (Count - 1)) : 0.0f;
	const float StartAngle = BaseAngle + WaveOffset - (bFullCircle ? 0.0f : Arc * 0.5f);

	for (int32 i = 0; i < Count; ++i)
	{
		const float AngleDeg = StartAngle + Step * i;
		const float AngleRad = FMath::DegreesToRadians(AngleDeg);
		const FVector Dir(FMath::Cos(AngleRad), FMath::Sin(AngleRad), 0.0f);

		ATHRBullet* Bullet = AcquireBullet();
		if (Bullet == nullptr)
		{
			break; /* プール上限 */
		}
		Bullet->Activate(Origin, Dir * Pattern.BulletSpeed,
			Pattern.Damage, Pattern.BulletRadius, Pattern.BulletLifetime, TargetActor);
	}
}
